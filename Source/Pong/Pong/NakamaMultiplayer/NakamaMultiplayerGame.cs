// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nakama;
using Pong.Engine;
using Pong.NakamaMultiplayer.GamePhases;

namespace Pong.NakamaMultiplayer;

/// <summary>
/// Very simple multiplayer implementation of the game, PONG using the Nakama framework.
/// </summary>
/// <remarks>
/// Basing a solution using the Nakama documentation...
/// https://dotnet.docs.heroiclabs.com/html/index.html
/// https://heroiclabs.com/blog/unity-fishgame/
/// </remarks>
public class NakamaMultiplayerGame : PongGame
{
    public new static NakamaMultiplayerGame Instance => (NakamaMultiplayerGame)PongGame.Instance;

    public readonly GamePhaseManager GamePhaseManager;

    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //Player stuff
    PlayerProfile _playerProfile;

    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //Multiplayer
    readonly NakamaConnection _nakamaConnection;
    readonly NetworkGameManager _networkGameManager;

    public NakamaMultiplayerGame()
    {
        Window.Title = "Pong - Multiplayer";

        _playerProfile = PlayerProfile.LoadOrCreate(LocalApplicationDataPath);

        _nakamaConnection = new NakamaConnection(_playerProfile);
        _networkGameManager = new NetworkGameManager(_nakamaConnection);

        GamePhaseManager = new GamePhaseManager();
        GamePhaseManager.Add(new MainMenuPhase(_nakamaConnection));
        GamePhaseManager.Add(new PlayGamePhase(_networkGameManager));
    }

    protected async override void Initialize()
    {
        base.Initialize();

        await _networkGameManager.Connect();
        _nakamaConnection.Socket.ReceivedMatchmakerMatched += OnReceivedMatchmakerMatched;

        GamePhaseManager.Initialise();
        GamePhaseManager.ChangePhase<MainMenuPhase>();
    }

    /// <summary>
    /// Called when a MatchmakerMatched event is received from the Nakama server.
    /// </summary>
    /// <param name="matched">The MatchmakerMatched data.</param>
    public void OnReceivedMatchmakerMatched(IMatchmakerMatched matched)
    {
        Logger.WriteLine($"NakamaMultiplayerGame.OnReceivedMatchmakerMatched");
        Logger.WriteLine($"Changing game phase to begin a new play session");

        GamePhaseManager.ChangePhase<PlayGamePhase>();
    }

    protected override void BeginRun()
    {
        base.BeginRun();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (KeyboardState.IsKeyDown(Keys.Escape) && PreviousKeyboardState.IsKeyUp(Keys.Escape))
            Exit();

        GamePhaseManager.Update();
    }

    protected override void OnDraw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        GamePhaseManager.Draw();
    }
}
