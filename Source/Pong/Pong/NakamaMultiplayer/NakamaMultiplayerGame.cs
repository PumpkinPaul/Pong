// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MoonTools.ECS;
using Pong.Engine;
using Pong.Engine.Extensions;
using Pong.Gameplay.Renderers;
using Pong.Gameplay.Systems;
using System;

namespace Pong.NakamaMultiplayer;

/// <summary>
/// Simple multiplayer implementation of the game, PONG using the Nakama framework.
/// </summary>
/// <remarks>
/// Basing a solution using the Nakama documentation...
/// https://dotnet.docs.heroiclabs.com/html/index.html
/// https://heroiclabs.com/blog/unity-fishgame/
/// </remarks>
public class NakamaMultiplayerGame : BaseGame
{
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //Player stuff
    PlayerProfile _playerProfile;

    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //ECS
    World _world;

    //Systems
    MoonTools.ECS.System[] _systems;

    //Renderers
    SpriteRenderer _spriteRenderer;

    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //Multiplayer
    NakamaConnection _nakamaConnection;
    MultiplayerGameState _gameState;

    public NakamaMultiplayerGame()
    {
        Window.Title = "Pong - Single Player";
    }

    protected async override void OnLoadContent()
    {
        _playerProfile = PlayerProfile.LoadOrCreate(LocalApplicationDataPath);

        _nakamaConnection = new NakamaConnection(_playerProfile);

        await _nakamaConnection.Connect();
    }

    protected override void BeginRun()
    {
        base.BeginRun();

        _world = new World();
        _gameState = new MultiplayerGameState();

        _systems = new MoonTools.ECS.System[]
        {
            //Spawn the entities into the game world
            new PlayerSpawnSystem(_world),
            new BallSpawnSystem(_world),
            new ScoreSpawnSystem(_world),

            new PlayerInputSystem(_world),

            //Turn directions into velocity!
            new DirectionalSpeedSystem(_world),

            //Collisions processors
            new WorldCollisionSystem(_world, _gameState, new Point(SCREEN_WIDTH, SCREEN_HEIGHT)),
            new EntityCollisionSystem(_world, SCREEN_WIDTH),

            //Move the entities in the world
            new MovementSystem(_world),
            new BounceSystem(_world),
            new AngledBounceSystem(_world),
        };

        _spriteRenderer = new SpriteRenderer(_world, SpriteBatch);

        const int PLAYER_OFFSET_X = 32;
        var color = Color.Red;

        _world.Send(new PlayerSpawnMessage(
            PlayerIndex: PlayerIndex.One,
            MoveUpKey: Keys.Q,
            MoveDownKey: Keys.A,
            Position: new Vector2(PLAYER_OFFSET_X, SCREEN_HEIGHT / 2),
            color,
            BounceDirection: -1
        ));

        _world.Send(new PlayerSpawnMessage(
            PlayerIndex: PlayerIndex.Two,
            MoveUpKey: Keys.P,
            MoveDownKey: Keys.L,
            Position: new Vector2(SCREEN_WIDTH - PLAYER_OFFSET_X, SCREEN_HEIGHT / 2),
            color,
            BounceDirection: 1
        ));

        _world.Send(new BallSpawnMessage(
            Position: new Vector2(SCREEN_WIDTH, SCREEN_HEIGHT) / 2,
            color
        ));

        _world.Send(new ScoreSpawnMessage(
            PlayerIndex: PlayerIndex.One,
            Position: new Vector2(SCREEN_WIDTH * 0.25f, 21)
        ));

        _world.Send(new ScoreSpawnMessage(
            PlayerIndex: PlayerIndex.Two,
            Position: new Vector2(SCREEN_WIDTH * 0.75f, 21)
        ));
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (KeyboardState.IsKeyDown(Keys.Escape) && PreviousKeyboardState.IsKeyUp(Keys.Escape))
            Exit();

        var delta = TimeSpan.FromSeconds(1000 / 60.0f);
        foreach (var system in _systems)
            system.Update(delta);

        _world.FinishUpdate();
    }

    protected override void OnDraw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        //Draw the world
        SpriteBatch.Begin(0, BlendState.AlphaBlend, null, null, RasterizerState.CullClockwise, BasicEffect);

        //...all the entities
        _spriteRenderer.Draw();

        //Centre line
        SpriteBatch.DrawLine(new Vector2(SCREEN_WIDTH / 2, 0), new Vector2(SCREEN_WIDTH / 2, SCREEN_HEIGHT), Color.Red);
        SpriteBatch.End();

        //Draw the UI
        SpriteBatch.Begin();
        //_scoreRenderer.Draw();
        SpriteBatch.DrawString(Resources.GameFont, _gameState.Player1Score.ToString(), new Vector2(SCREEN_WIDTH * 0.25f, 21), Color.Red);
        SpriteBatch.DrawString(Resources.GameFont, _gameState.Player2Score.ToString(), new Vector2(SCREEN_WIDTH * 0.75f, 21), Color.Red);
        SpriteBatch.End();
    }
}
