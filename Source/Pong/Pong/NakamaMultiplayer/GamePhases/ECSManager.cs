// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MoonTools.ECS;
using Pong.Engine;
using Pong.Engine.Extensions;
using Pong.Gameplay.Renderers;
using Pong.Gameplay.Systems;
using Pong.NakamaMultiplayer.Systems;
using System;
using System.Collections.Generic;

namespace Pong.NakamaMultiplayer;

/// <summary>
/// Encapsulates management of the ECS
/// </summary>
public class ECSManager
{
    readonly World _world;

    //Systems
    readonly MoonTools.ECS.System[] _systems;

    //Renderers
    readonly SpriteRenderer _spriteRenderer;

    readonly PlayerEntityMapper _playerEntityMapper;
    readonly NetworkGameManager _networkGameManager;
    readonly MultiplayerGameState _gameState;

    readonly Queue<LocalPlayerSpawnMessage> _localPlayerSpawnMessages = new();
    readonly Queue<RemotePlayerSpawnMessage> _remotePlayerSpawnMessages = new();
    readonly Queue<MatchDataVelocityAndPositionMessage> _matchDataVelocityAndPositionMessage = new();
    readonly Queue<DestroyEntityMessage> _destroyEntityMessage = new();

    public ECSManager(
        NetworkGameManager networkGameManager,
        PlayerEntityMapper playerEntityMapper)
    {
        _networkGameManager = networkGameManager;
        _playerEntityMapper = playerEntityMapper;

        _world = new World();
        _gameState = new MultiplayerGameState();

        _systems = new MoonTools.ECS.System[]
        {
            //Spawn the entities into the game world
            new LocalPlayerSpawnSystem(_world),
            new RemotePlayerSpawnSystem(_world, _playerEntityMapper),
            new BallSpawnSystem(_world),
            new ScoreSpawnSystem(_world),

            new PlayerInputSystem(_world),   //Get input from devices and turn into game actions...
            new PlayerActionsSystem(_world), //...then process the actions (e.g. do a jump, fire a gun, etc)

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
    }

    public void SpawnLocalPlayer(Vector2 position, int bounceDirection)
    {
        //Queue entity creation in the ECS
        _localPlayerSpawnMessages.Enqueue(new LocalPlayerSpawnMessage(
            PlayerIndex: PlayerIndex.One,
            MoveUpKey: Keys.Q,
            MoveDownKey: Keys.A,
            Position: position,
            Color.Red,
            BounceDirection: bounceDirection
        ));
    }

    public void SpawnRemotePlayer(Vector2 position, int bounceDirection)
    {
        //Queue entity creation in the ECS
        _remotePlayerSpawnMessages.Enqueue(new RemotePlayerSpawnMessage(
            PlayerIndex: PlayerIndex.Two,
            Position: position,
            Color.Blue,
            BounceDirection: bounceDirection
        ));
    }

    public void ReceivedMatchDataVelocityAndPosition(Vector2 position, string sessionId)
    {
        var entity = _playerEntityMapper.GetEntityFromSessionId(sessionId);

        if (entity == PlayerEntityMapper.INVALID_ENTITY)
            return;

        //Queue entity to begin lerping to the corrected position.
        _matchDataVelocityAndPositionMessage.Enqueue(new MatchDataVelocityAndPositionMessage(
            LerpToPosition: position,
            Entity: entity
        ));
    }

    public void DestroyEntity(string sessionId)
    {
        var entity = _playerEntityMapper.GetEntityFromSessionId(sessionId);

        if (entity == PlayerEntityMapper.INVALID_ENTITY)
            return;

        _playerEntityMapper.RemovePlayerBySessionId(sessionId);

        //Queue entity to begin lerping to the corrected position.
        _destroyEntityMessage.Enqueue(new DestroyEntityMessage(
            Entity: entity
        ));
    }

    public void Update()
    {
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

    public void Draw()
    {
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
}
