// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Pong.Engine;
using System;
using System.Threading.Tasks;

namespace Pong.NakamaMultiplayer;

/// <summary>
/// Playing the game phase
/// </summary>
public class PlayGamePhase : GamePhase
{
    //Multiplayer networking
    readonly NetworkGameManager _networkGameManager;
    //ECS
    ECSManager _ecsManager;
    //Mapping between networking and ECS
    readonly PlayerEntityMapper _playerEntityMapper = new();

    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //Gameplay
    public event EventHandler ExitedMatch;

    const int PLAYER_OFFSET_X = 32;

    readonly Vector2[] _playerSpawnPoints = new[] {
        new Vector2(PLAYER_OFFSET_X, PongGame.SCREEN_HEIGHT / 2),
        new Vector2(PongGame.SCREEN_WIDTH - PLAYER_OFFSET_X, PongGame.SCREEN_HEIGHT / 2)
    };
    
    int _playerSpawnPointsIdx = 0;
    int _bounceDirection = -1;

    public PlayGamePhase(
        NetworkGameManager networkGameManager)
    {
        _networkGameManager = networkGameManager;
    }

    public override void Initialise()
    {
        base.Initialise();

        _ecsManager = new ECSManager(_networkGameManager, _playerEntityMapper);

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

        _ecsManager.Update();
    }

    protected override void OnDraw()
    {
        base.OnDraw();

        _ecsManager.Draw();
    }

    /// <summary>
    /// Quits the current match.
    /// </summary>
    public async Task QuitMatch()
    {
        Logger.WriteLine($"PlayGamePhase.QuitMatch");

        await _networkGameManager.QuitMatch();

        ExitedMatch?.Invoke(this, EventArgs.Empty);
    }

    void OnSpawnedLocalPlayer(object sender, EventArgs e)
    {
        var position = _playerSpawnPoints[_playerSpawnPointsIdx];

        _ecsManager.SpawnLocalPlayer(position, _bounceDirection);

        PrepareNextPlayer();
    }

    void OnSpawnedRemotePlayer(object sender, SpawnedRemotePlayerEventArgs e)
    {
        var position = _playerSpawnPoints[_playerSpawnPointsIdx];

        _ecsManager.SpawnRemotePlayer(position, _bounceDirection);

        _playerEntityMapper.AddPlayer(PlayerIndex.Two, e.SessionId);

        PrepareNextPlayer();
    }

    void PrepareNextPlayer()
    {
        //Cycle through the spawn points so that players are located in the correct postions and flipping the bounce direction
        _playerSpawnPointsIdx = (_playerSpawnPointsIdx + 1) % _playerSpawnPoints.Length;
        _bounceDirection = -_bounceDirection;
    }

    void OnReceivedRemotePlayerPosition(object sender, ReceivedRemotePlayerPositionEventArgs e)
    {
        _ecsManager.ReceivedMatchDataVelocityAndPosition(e.Position, e.SessionId);
    }

    void OnRemovedPlayer(object sender, RemovedPlayerEventArgs e)
    {
        _ecsManager.DestroyEntity(e.SessionId);
    }
}
