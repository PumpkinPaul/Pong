// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Pong.Engine.Collections;
using System;

namespace Pong.NakamaMultiplayer.GamePhases;

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

    public void ChangePhase<T>() where T : GamePhase
    {
        ActivePhase?.Destroy();

        var newPhase = Get<T>();

        ActivePhase = newPhase;
        ActivePhase.Create();
    }

    public T Get<T>() where T : GamePhase => (T)_gamePhases[typeof(T)];

    public void Update() => ActivePhase?.Update();

    public void Draw() => ActivePhase?.Draw();
}

