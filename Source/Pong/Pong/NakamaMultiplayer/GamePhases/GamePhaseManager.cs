// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Pong.Engine.Collections;
using System;

namespace Pong.NakamaMultiplayer;

/// <summary>
/// Manages the game's distinct phases - Splash, Intro, MainMenu, Play, etc.
/// </summary>
public class GamePhaseManager
{
    public GamePhase ActivePhase { get; private set; }

    private readonly SimpleHashList<Type, GamePhase> _gamePhases = new();

    public void Add(GamePhase gamePhase)
    {
        if (gamePhase == null)
            throw new ArgumentNullException(nameof(gamePhase));

        var key = gamePhase.GetType();

        _gamePhases[key] = gamePhase;
    }

    public void Initialise()
    {
        foreach (var phase in _gamePhases)
            phase.Initialise();
    }

    public void ChangePhase<T>() where T : GamePhase => ChangePhase(typeof(T));

    public void ChangePhase(Type type)
    {
        ActivePhase?.Destroy();

        var newPhase = Get(type);

        ActivePhase = newPhase;
        ActivePhase.Create();
    }

    public GamePhase Get(Type key)
    {
        return _gamePhases[key];
    }

    public T Get<T>() where T : GamePhase
    {
        var type = typeof(T);
        return (T)Get(type);
    }

    public void Update()
    {
        ActivePhase.Update();
    }

    public void Draw()
    {
        ActivePhase.Draw();
    }
}

