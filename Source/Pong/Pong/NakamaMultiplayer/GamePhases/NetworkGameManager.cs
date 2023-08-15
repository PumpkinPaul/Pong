// Copyright Pumpkin Games Ltd. All Rights Reserved.

//https://github.com/heroiclabs/fishgame-unity/blob/main/FishGame/Assets/Managers/GameManager.cs

using Microsoft.Xna.Framework;
using Nakama;
using Newtonsoft.Json;
using Pong.Engine;
using Pong.NakamaMultiplayer.Players;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pong.NakamaMultiplayer;

public record class SpawnedRemotePlayerEventArgs(
    string SessionId
);

public record ReceivedRemotePlayerPositionEventArgs(
    Vector2 Position,
    string SessionId
);

public record RemovedPlayerEventArgs(
    string SessionId
);

/// <summary>
/// Responsible for managing a networked game
/// </summary>
public class NetworkGameManager
{
    public event EventHandler SpawnedLocalPlayer;
    public event EventHandler<SpawnedRemotePlayerEventArgs> SpawnedRemotePlayer;
    public event EventHandler<ReceivedRemotePlayerPositionEventArgs> ReceivedRemotePlayerPosition;
    public event EventHandler<RemovedPlayerEventArgs> RemovedPlayer;

    //Multiplayer
    readonly NakamaConnection _nakamaConnection;

    IUserPresence _localUser;
    IMatch _currentMatch;

    readonly IDictionary<string, Player> _players;
    Player _localPlayer;

    public NetworkGameManager(
        NakamaConnection nakamaConnection)
    {
        _nakamaConnection = nakamaConnection;

        _players = new Dictionary<string, Player>();
    }

    public async Task Connect()
    {
        await _nakamaConnection.Connect();

        _nakamaConnection.Socket.ReceivedMatchmakerMatched += OnReceivedMatchmakerMatched;
        _nakamaConnection.Socket.ReceivedMatchPresence += OnReceivedMatchPresence;
        _nakamaConnection.Socket.ReceivedMatchState += OnReceivedMatchState;
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
            SpawnPlayer(match.Id, user);

        // Cache a reference to the current match.
        _currentMatch = match;
    }

    /// <summary>
    /// Called when a player/s joins or leaves the match.
    /// </summary>
    /// <param name="matchPresenceEvent">The MatchPresenceEvent data.</param>
    public void OnReceivedMatchPresence(IMatchPresenceEvent matchPresenceEvent)
    {
        Logger.WriteLine($"OnReceivedMatchPresence");

        // For each new user that joins, spawn a player for them.
        foreach (var user in matchPresenceEvent.Joins)
            SpawnPlayer(matchPresenceEvent.MatchId, user);

        // For each player that leaves, despawn their player.
        foreach (var user in matchPresenceEvent.Leaves)
            RemovePlayer(user.SessionId);
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
            player.Destroy();

        // Clear the players array.
        _players.Clear();
    }

    void SpawnPlayer(string matchId, IUserPresence userPresence)
    {
        Logger.WriteLine($"SpawnPlayer: {userPresence}");

        // If the player has already been spawned, return early.
        if (_players.ContainsKey(userPresence.SessionId))
        {
            return;
        }

        // Set a variable to check if the player is the local player or not based on session ID.
        var isLocal = userPresence.SessionId == _localUser.SessionId;

        Player player;

        // Setup the appropriate network data values if this is a remote player.
        // If this is our local player, add a listener for the PlayerDied event.
        if (isLocal)
        {
            player = new LocalPlayer();
            _localPlayer = player;

            SpawnedLocalPlayer?.Invoke(this, EventArgs.Empty);
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

            SpawnedRemotePlayer?.Invoke(this, new SpawnedRemotePlayerEventArgs(userPresence.SessionId));
        }

        // Add the player to the players array.
        _players.Add(userPresence.SessionId, player);
    }

    void RemovePlayer(string sessionId)
    {
        if (!_players.ContainsKey(sessionId))
            return;

        _players.Remove(sessionId);

        RemovedPlayer?.Invoke(this, new RemovedPlayerEventArgs(sessionId));
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

        ReceivedRemotePlayerPosition?.Invoke(
            this,
            new ReceivedRemotePlayerPositionEventArgs(position, networkPlayer.NetworkData.User.SessionId));
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
