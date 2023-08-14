// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Nakama;
using Pong.Engine;
using System;
using System.Threading.Tasks;

namespace Pong.NakamaMultiplayer;

public class NakamaConnection
{
    public string Scheme = "http";
    public string Host = "localhost";
    public int Port = 7350;
    public string ServerKey = "defaultkey";

    readonly PlayerProfile _playerProfile;

    public IClient Client;
    public ISession Session;
    public ISocket Socket;

    private string currentMatchmakingTicket;
    private string currentMatchId;

    public NakamaConnection(
        PlayerProfile playerProfile)
    {
        _playerProfile = playerProfile;
    }

    /// <summary>
    /// Connects to the Nakama server using device authentication and opens socket for realtime communication.
    /// </summary>
    public async Task Connect()
    {
        Logger.WriteLine("==================================================");
        Logger.WriteLine($"Nakama Connection");
        Logger.WriteLine("==================================================");
        Logger.WriteLine($"Create Client...");

        // Connect to the Nakama server.
        Client = new Client(Scheme, Host, Port, ServerKey);

        // Attempt to restore an existing user session.
        var authToken = _playerProfile.SessionToken;
        if (!string.IsNullOrEmpty(authToken))
        {
            Logger.WriteLine($"Restore Session");
            var session = Nakama.Session.Restore(authToken);
            if (!session.IsExpired)
            {
                Session = session;
            }
        }

        // If we weren't able to restore an existing session, authenticate to create a new user session.
        if (Session == null)
        {
            // If we've already stored a device identifier in PlayerPrefs then use that.
            if (string.IsNullOrWhiteSpace(_playerProfile.DeviceIdentifier))
            {
                // Store the device identifier to ensure we use the same one each time from now on.
                _playerProfile.DeviceIdentifier = Guid.NewGuid().ToString();
            }

            Logger.WriteLine($"Authenticate Device...");
            // Use Nakama Device authentication to create a new session using the device identifier.
            Session = await Client.AuthenticateDeviceAsync(_playerProfile.DeviceIdentifier);

            // Store the auth token that comes back so that we can restore the session later if necessary.
            _playerProfile.SessionToken = Session.AuthToken;

            _playerProfile.Save();
        }

        Logger.WriteLine("==================================================");
        Logger.WriteLine("Nakama Session Details");
        Logger.WriteLine("==================================================");
        Logger.WriteLine($"AuthToken: {Session.AuthToken}");       // raw JWT token
        Logger.WriteLine($"RefreshToken: {Session.RefreshToken}"); // raw JWT token.
        Logger.WriteLine($"UserId: {Session.UserId}");
        Logger.WriteLine($"Username: {Session.Username}");
        Logger.WriteLine($"Session has expired: {Session.IsExpired}");
        Logger.WriteLine($"Session expires at: {Session.ExpireTime}");

        // Open a new Socket for realtime communication.
        Logger.WriteLine($"Connect a Socket...");
        Socket = Nakama.Socket.From(Client);
        await Socket.ConnectAsync(Session, true);
    }
}
