// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MoonTools.ECS;
using Pong.Engine;
using Pong.Engine.Extensions;
using Pong.Gameplay.Renderers;
using Pong.Gameplay.Systems;
using Pong.NakamaMultiplayer.GamePhases;
using Pong.NakamaMultiplayer.Systems;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pong.NakamaMultiplayer;

/// <summary>
/// Playing the game
/// </summary>
public class PlayGamePhase : GamePhase
{
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //Multiplayer
    readonly NakamaConnection _nakamaConnection;
    readonly NetworkGameManager _networkGameManager;

    readonly Queue<LocalPlayerSpawnMessage> _localPlayerSpawnMessages = new();
    readonly Queue<RemotePlayerSpawnMessage> _remotePlayerSpawnMessages = new();
    readonly Queue<MatchDataVelocityAndPositionMessage> _matchDataVelocityAndPositionMessage = new();
    readonly Queue<DestroyEntityMessage> _destroyEntityMessage = new();

    MultiplayerGameState _gameState;

    const int PLAYER_OFFSET_X = 32;
    Vector2[] _playerSpawnPoints = new[] {
        new Vector2(PLAYER_OFFSET_X, PongGame.SCREEN_HEIGHT / 2),
        new Vector2(PongGame.SCREEN_WIDTH - PLAYER_OFFSET_X, PongGame.SCREEN_HEIGHT / 2)
    };
    int _playerSpawnPointsIdx = 0;
    int _bounceDirection = -1;

    readonly PlayerEntityMapper _playerEntityMapper = new();

    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //ECS
    World _world;

    //Systems
    MoonTools.ECS.System[] _systems;

    //Renderers
    SpriteRenderer _spriteRenderer;

    public PlayGamePhase(
        NetworkGameManager networkGameManager)
    {
        _networkGameManager = networkGameManager;
    }

    public override void Initialise()
    {
        base.Initialise();

        _world = new World();
        _gameState = new MultiplayerGameState();

        _systems = new MoonTools.ECS.System[]
        {
            //Spawn the entities into the game world
            new LocalPlayerSpawnSystem(_world),
            new RemotePlayerSpawnSystem(_world, _playerEntityMapper),
            new BallSpawnSystem(_world),
            new ScoreSpawnSystem(_world),

            new PlayerInputSystem(_world),

            //Turn directions into velocity!
            new DirectionalSpeedSystem(_world),

            //Collisions processors
            new WorldCollisionSystem(_world, _gameState, new Point(PongGame.SCREEN_WIDTH, PongGame.SCREEN_HEIGHT)),
            new EntityCollisionSystem(_world, PongGame.SCREEN_WIDTH),

            //Move the entities in the world
            new MovementSystem(_world),
            new BounceSystem(_world),
            new AngledBounceSystem(_world),

            //LateUpdate
            new PlayerNetworkLocalSyncSystem(_world, _networkGameManager),
            new PlayerNetworkRemoteSyncSystem(_world),
            new LerpPositionSystem(_world),

            //Remove the dead entities
            new DestroyEntitySystem(_world)
        };

        _spriteRenderer = new SpriteRenderer(_world, PongGame.Instance.SpriteBatch);

        var color = Color.Red;

        _world.Send(new BallSpawnMessage(
            Position: new Vector2(PongGame.SCREEN_WIDTH, PongGame.SCREEN_HEIGHT) / 2,
            color
        ));

        _world.Send(new ScoreSpawnMessage(
            PlayerIndex: PlayerIndex.One,
            Position: new Vector2(PongGame.SCREEN_WIDTH * 0.25f, 21)
        ));

        _world.Send(new ScoreSpawnMessage(
            PlayerIndex: PlayerIndex.Two,
            Position: new Vector2(PongGame.SCREEN_WIDTH * 0.75f, 21)
        ));

        _networkGameManager.SpawnedLocalPlayer += OnSpawnedLocalPlayer;
        _networkGameManager.SpawnedRemotePlayer += OnSpawnedRemotePlayer;
        _networkGameManager.ReceivedRemotePlayerPosition += OnReceivedRemotePlayerPosition;
        _networkGameManager.RemovedPlayer += OnRemovedPlayer;
    }

    protected async override void OnUpdate()
    {
        base.OnUpdate();

        if (PongGame.Instance.KeyboardState.IsKeyDown(Keys.Space) && PongGame.Instance.PreviousKeyboardState.IsKeyUp(Keys.Space))
            await QuitMatch();

        SendMessages();

        foreach (var system in _systems)
            system.Update(PongGame.Instance.TargetElapsedTime);

        _world.FinishUpdate();
    }

    private void SendMessages()
    {
        while (_localPlayerSpawnMessages.Count > 0)
            _world.Send(_localPlayerSpawnMessages.Dequeue());

        while (_remotePlayerSpawnMessages.Count > 0)
            _world.Send(_remotePlayerSpawnMessages.Dequeue());

        while (_matchDataVelocityAndPositionMessage.Count > 0)
            _world.Send(_matchDataVelocityAndPositionMessage.Dequeue());

        while (_destroyEntityMessage.Count > 0)
            _world.Send(_destroyEntityMessage.Dequeue());
    }

    protected override void OnDraw()
    {
        base.OnDraw();

        var spriteBatch = PongGame.Instance.SpriteBatch;

        //Draw the world
        spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, RasterizerState.CullClockwise, PongGame.Instance.BasicEffect);

        //...all the entities
        _spriteRenderer.Draw();

        //...play area
        spriteBatch.DrawLine(new Vector2(PongGame.SCREEN_WIDTH / 2, 0), new Vector2(PongGame.SCREEN_WIDTH / 2, PongGame.SCREEN_HEIGHT), Color.Red);
        spriteBatch.End();

        //...game UI
        spriteBatch.Begin();
        spriteBatch.DrawString(Resources.GameFont, _gameState.Player1Score.ToString(), new Vector2(PongGame.SCREEN_WIDTH * 0.25f, 21), Color.Red);
        spriteBatch.DrawString(Resources.GameFont, _gameState.Player2Score.ToString(), new Vector2(PongGame.SCREEN_WIDTH * 0.75f, 21), Color.Red);
        spriteBatch.End();
    }

    /// <summary>
    /// Quits the current match.
    /// </summary>
    public async Task QuitMatch()
    {
        Logger.WriteLine($"PlayGamePhase.QuitMatch");

        await _networkGameManager.QuitMatch();

        // Show the main menu, hide the in-game menu.
        NakamaMultiplayerGame.Instance.GamePhaseManager.ChangePhase<MainMenuPhase>();
    }

    void OnSpawnedLocalPlayer(object sender, EventArgs e)
    {
        var position = _playerSpawnPoints[_playerSpawnPointsIdx];

        //Queue entity creation in the ECS
        _localPlayerSpawnMessages.Enqueue(new LocalPlayerSpawnMessage(
            PlayerIndex: PlayerIndex.One,
            MoveUpKey: Keys.Q,
            MoveDownKey: Keys.A,
            Position: position,
            Color.Red,
            BounceDirection: _bounceDirection
        ));

        PrepareNextPlayer();
    }

    void OnSpawnedRemotePlayer(object sender, SpawnedRemotePlayerEventArgs e)
    {
        var position = _playerSpawnPoints[_playerSpawnPointsIdx];

        //Queue entity creation in the ECS
        _remotePlayerSpawnMessages.Enqueue(new RemotePlayerSpawnMessage(
            PlayerIndex: PlayerIndex.Two,
            Position: position,
            Color.Blue,
            BounceDirection: _bounceDirection
        ));

        _playerEntityMapper.AddPlayer(PlayerIndex.Two, e.SessionId);

        PrepareNextPlayer();
    }

    private void PrepareNextPlayer()
    {
        //Cycle through the spawn points so that players are located in the correct postions and flipping the bounce direction
        _playerSpawnPointsIdx = (_playerSpawnPointsIdx + 1) % _playerSpawnPoints.Length;
        _bounceDirection = -_bounceDirection;
    }

    void OnReceivedRemotePlayerPosition(object sender, ReceivedRemotePlayerPositionEventArgs e)
    {
        var entity = _playerEntityMapper.GetEntityFromSessionId(e.SessionId);

        if (entity == PlayerEntityMapper.INVALID_ENTITY)
            return;

        //Queue entity to begin lerping to the corrected position.
        _matchDataVelocityAndPositionMessage.Enqueue(new MatchDataVelocityAndPositionMessage(
            LerpToPosition: e.Position,
            Entity: entity
        ));
    }

    void OnRemovedPlayer(object sender, RemovedPlayerEventArgs e)
    {
        var entity = _playerEntityMapper.GetEntityFromSessionId(e.SessionId);

        if (entity == PlayerEntityMapper.INVALID_ENTITY)
            return;

        _playerEntityMapper.RemovePlayerBySessionId(e.SessionId);

        //Queue entity to begin lerping to the corrected position.
        _destroyEntityMessage.Enqueue(new DestroyEntityMessage(
            Entity: entity
        ));
    }
}
