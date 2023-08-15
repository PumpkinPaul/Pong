// Copyright Pumpkin Games Ltd. All Rights Reserved.

using MoonTools.ECS;
using Pong.NakamaMultiplayer;
using Pong.NakamaMultiplayer.Players;
using System;

namespace Pong.Gameplay.Systems;

public readonly record struct GoalScoredMessage(
    int Player1Increment,
    int Player2Increment
);

/// <summary>
/// Updates local game state and send updates to the other clients.
/// </summary>
public class GoalScoredSystem : MoonTools.ECS.System
{
    readonly GameState _gameState;
    
    public GoalScoredSystem(
        World world,
        GameState gameState
    ) : base(world)
    {
        _gameState = gameState;
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var message in ReadMessages<GoalScoredMessage>())
        {
            _gameState.Player1Score += message.Player1Increment;
            _gameState.Player2Score += message.Player2Increment;
        }
    }
}