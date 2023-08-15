// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MoonTools.ECS;
using Nakama;
using Newtonsoft.Json;
using Pong.Engine;
using Pong.Engine.Extensions;
using Pong.Gameplay.Renderers;
using Pong.Gameplay.Systems;
using Pong.NakamaMultiplayer.GamePhases;
using Pong.NakamaMultiplayer.Players;
using Pong.NakamaMultiplayer.Systems;
using System;
using System.Collections.Generic;
using System.Text;
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

    IUserPresence _localUser;

    //TODO: better - stop this from being public
    public IMatch _currentMatch;

    readonly Queue<LocalPlayerSpawnMessage> _localPlayerSpawnMessages = new();
    readonly Queue<RemotePlayerSpawnMessage> _remotePlayerSpawnMessages = new();
    readonly Queue<MatchDataVelocityAndPositionMessage> _matchDataVelocityAndPositionMessage = new();
    
    MultiplayerGameState _gameState;
    
    const int PLAYER_OFFSET_X = 32;
    Vector2[] _playerSpawnPoints = new[] {
        new Vector2(PLAYER_OFFSET_X, PongGame.SCREEN_HEIGHT / 2),
        new Vector2(PongGame.SCREEN_WIDTH - PLAYER_OFFSET_X, PongGame.SCREEN_HEIGHT / 2)
    };
    int _playerSpawnPointsIdx = 0;
    int _bounceDirection = -1;

    readonly IDictionary<string, Player> _players;
    Player _localPlayer;

    PlayerEntityMapper _playerEntityMapper = new();

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
        NakamaConnection nakamaConnection)
    {
        _nakamaConnection = nakamaConnection;

        _players = new Dictionary<string, Player>();
    }

    public override void Initialise()
    {
        base.Initialise();

        _world = new World();
        _gameState = new MultiplayerGameState();

        _systems = new MoonTools.ECS.System[]
        {
            //Spawn the entities into the game world
            new LocalPlayerSpawnSystem(_world, _playerEntityMapper),
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
            new PlayerNetworkLocalSyncSystem(_world),
            new PlayerNetworkRemoteSyncSystem(_world),
            new LerpPositionSystem(_world)
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

    protected async override void OnUpdate()
    {
        base.OnUpdate();

        if (PongGame.Instance.KeyboardState.IsKeyDown(Keys.Space) && PongGame.Instance.PreviousKeyboardState.IsKeyUp(Keys.Space))
            await QuitMatch();

        while (_localPlayerSpawnMessages.Count > 0)
            _world.Send(_localPlayerSpawnMessages.Dequeue());

        while (_remotePlayerSpawnMessages.Count > 0)
            _world.Send(_remotePlayerSpawnMessages.Dequeue());

        while (_matchDataVelocityAndPositionMessage.Count > 0)
            _world.Send(_matchDataVelocityAndPositionMessage.Dequeue());

        var delta = TimeSpan.FromMilliseconds(1000 / 60.0f);
        foreach (var system in _systems)
            system.Update(delta);

        _world.FinishUpdate();
    }

    protected override void OnDraw()
    {
        base.OnDraw();

        //Draw the world
        PongGame.Instance.SpriteBatch.Begin(0, BlendState.AlphaBlend, null, null, RasterizerState.CullClockwise, PongGame.Instance.BasicEffect);

        //...all the entities
        _spriteRenderer.Draw();

        //...play area
        PongGame.Instance.SpriteBatch.DrawLine(new Vector2(PongGame.SCREEN_WIDTH / 2, 0), new Vector2(PongGame.SCREEN_WIDTH / 2, PongGame.SCREEN_HEIGHT), Color.Red);
        PongGame.Instance.SpriteBatch.End();

        //...game UI
        PongGame.Instance.SpriteBatch.Begin();
        PongGame.Instance.SpriteBatch.DrawString(Resources.GameFont, _gameState.Player1Score.ToString(), new Vector2(PongGame.SCREEN_WIDTH * 0.25f, 21), Color.Red);
        PongGame.Instance.SpriteBatch.DrawString(Resources.GameFont, _gameState.Player2Score.ToString(), new Vector2(PongGame.SCREEN_WIDTH * 0.75f, 21), Color.Red);
        PongGame.Instance.SpriteBatch.End();
    }

    /// <summary>
    /// Called when a MatchmakerMatched event is received from the Nakama server.
    /// </summary>
    /// <param name="matched">The MatchmakerMatched data.</param>
    public async void OnReceivedMatchmakerMatched(IMatchmakerMatched matched)
    {
        Logger.WriteLine($"OnReceivedMatchmakerMatched");

        // Cache a reference to the local user.
        _localUser = matched.Self.Presence;

        // Join the match.
        var match = await _nakamaConnection.Socket.JoinMatchAsync(matched);

        // Spawn a player instance for each connected user.
        foreach (var user in match.Presences)
        {
            SpawnPlayer(match.Id, user);
        }

        // Cache a reference to the current match.
        _currentMatch = match;
    }

    /// <summary>
    /// Called when a player/s joins or leaves the match.
    /// </summary>
    /// <param name="matchPresenceEvent">The MatchPresenceEvent data.</param>
    public async void OnReceivedMatchPresence(IMatchPresenceEvent matchPresenceEvent)
    {
        Logger.WriteLine($"OnReceivedMatchPresence");

        // For each new user that joins, spawn a player for them.
        foreach (var user in matchPresenceEvent.Joins)
        {
            SpawnPlayer(matchPresenceEvent.MatchId, user);
        }

        // For each player that leaves, despawn their player.
        foreach (var user in matchPresenceEvent.Leaves)
        {
            if (_players.ContainsKey(user.SessionId))
            {
                //TODO: sync players leaving with the ECS
                //QueueDestoryPlayer(matchPresenceEvent.MatchId, user);
                _playerEntityMapper.RemovePlayerBySessionId(user.SessionId);
                _players.Remove(user.SessionId);
            }
        }

        await Task.Yield();
    }

    /// <summary>
    /// Called when new match state is received.
    /// </summary>
    /// <param name="matchState">The MatchState data.</param>
    public void OnReceivedMatchState(IMatchState matchState)
    {
        Logger.WriteLine($"OnReceivedMatchState: {matchState.OpCode}");

        if (!_players.TryGetValue(matchState.UserPresence.SessionId, out var player))
            return;

        // If the incoming data is not related to this remote player, ignore it and return early.
        var networkPlayer = player as NetworkPlayer;
        if (matchState.UserPresence.SessionId != networkPlayer?.NetworkData?.User?.SessionId)
            return;

        // Decide what to do based on the Operation Code of the incoming state data as defined in OpCodes.
        switch (matchState.OpCode)
        {
            case OpCodes.VelocityAndPosition:
                UpdateVelocityAndPositionFromState(matchState.State, networkPlayer);
                break;
            //case OpCodes.Input:
            //    SetInputFromState(matchState.State);
            //    break;
            default:
                break;
        }
    }

    /// <summary>
    /// Quits the current match.
    /// </summary>
    public async Task QuitMatch()
    {
        Logger.WriteLine($"QuitMatch");

        // Ask Nakama to leave the match.
        await _nakamaConnection.Socket.LeaveMatchAsync(_currentMatch);

        // Reset the currentMatch and localUser variables.
        _currentMatch = null;
        _localUser = null;

        // Destroy all existing player.
        foreach (var player in _players.Values)
        {
            player.Destroy();
        }

        //TODO: Reset the ECS

        // Clear the players array.
        _players.Clear();

        // Show the main menu, hide the in-game menu.
        NakamaMultiplayerGame.Instance.GamePhaseManager.ChangePhase<MainMenuPhase>();
    }

    void SpawnPlayer(string matchId, IUserPresence userPresence)
    {
        Logger.WriteLine($"SpawnPlayer: {userPresence}");

        // If the player has already been spawned, return early.
        if (_players.ContainsKey(userPresence.SessionId))
        {
            return;
        }

        var position = _playerSpawnPoints[_playerSpawnPointsIdx];

        // Set a variable to check if the player is the local player or not based on session ID.
        var isLocal = userPresence.SessionId == _localUser.SessionId;

        Player player;

        // Setup the appropriate network data values if this is a remote player.
        // If this is our local player, add a listener for the PlayerDied event.
        if (isLocal)
        {
            player = new LocalPlayer();
            _localPlayer = player;

            //Queue entity creation in the ECS
            _localPlayerSpawnMessages.Enqueue(new LocalPlayerSpawnMessage(
                PlayerIndex: PlayerIndex.One,
                MoveUpKey: Keys.Q,
                MoveDownKey: Keys.A,
                Position: position,
                Color.Red,
                BounceDirection: _bounceDirection
            ));

            _playerEntityMapper.AddPlayer(PlayerIndex.One, userPresence.SessionId);
        }
        else
        {
            player = new NetworkPlayer
            {
                NetworkData = new RemotePlayerNetworkData
                {
                    MatchId = matchId,
                    User = userPresence
                }
            };

            //Queue entity creation in the ECS
            _remotePlayerSpawnMessages.Enqueue(new RemotePlayerSpawnMessage(
                PlayerIndex: PlayerIndex.Two,
                Position: position,
                Color.Blue,
                BounceDirection: _bounceDirection
            ));

            _playerEntityMapper.AddPlayer(PlayerIndex.Two, userPresence.SessionId);
        }

        // Add the player to the players array.
        _players.Add(userPresence.SessionId, player);

        //Cycle through the spawn points so that players are located in the correct postions and flipping the bounce direction
        _playerSpawnPointsIdx = (_playerSpawnPointsIdx + 1) % _playerSpawnPoints.Length;
        _bounceDirection = -_bounceDirection;
    }

    /// <summary>
    /// Updates the player's velocity and position based on incoming state data.
    /// </summary>
    /// <param name="state">The incoming state byte array.</param>
    private void UpdateVelocityAndPositionFromState(byte[] state, NetworkPlayer networkPlayer)
    {
        var stateDictionary = GetStateAsDictionary(state);

        var position = new Vector2(
            float.Parse(stateDictionary["position.x"]),
            float.Parse(stateDictionary["position.y"]));

        //Queue entity to begin lerping to the corrected position.
        _matchDataVelocityAndPositionMessage.Enqueue(new MatchDataVelocityAndPositionMessage(
            LerpToPosition: position,
            Entity: GetEntityFromNetworkPlayer(networkPlayer)
        ));
    }

    Entity GetEntityFromNetworkPlayer(NetworkPlayer networkPlayer)
    {
        return _playerEntityMapper.GetEntityFromSessionId(networkPlayer.NetworkData.User.SessionId);
    }

    /// <summary>
    /// Converts a byte array of a UTF8 encoded JSON string into a Dictionary.
    /// </summary>
    /// <param name="state">The incoming state byte array.</param>
    /// <returns>A Dictionary containing state data as strings.</returns>
    static IDictionary<string, string> GetStateAsDictionary(byte[] state)
    {
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(state));
    }

    /// <summary>
    /// Sends a match state message across the network.
    /// </summary>
    /// <param name="opCode">The operation code.</param>
    /// <param name="state">The stringified JSON state data.</param>
    public async Task SendMatchStateAsync(long opCode, string state)
    {
        await _nakamaConnection.Socket.SendMatchStateAsync(_currentMatch.Id, opCode, state);
    }

    /// <summary>
    /// Sends a match state message across the network.
    /// </summary>
    /// <param name="opCode">The operation code.</param>
    /// <param name="state">The stringified JSON state data.</param>
    public void SendMatchState(long opCode, string state)
    {
        _nakamaConnection.Socket.SendMatchStateAsync(_currentMatch.Id, opCode, state);
    }
}
