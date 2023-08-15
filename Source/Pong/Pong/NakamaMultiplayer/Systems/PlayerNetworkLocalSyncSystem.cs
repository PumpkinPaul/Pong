// Copyright Pumpkin Games Ltd. All Rights Reserved.

//Based on code from the FishGame Unity sample from Herioc Labs.
//https://github.com/heroiclabs/fishgame-unity/blob/main/FishGame/Assets/Entities/Player/PlayerNetworkLocalSync.cs

using MoonTools.ECS;
using Pong.Gameplay.Components;
using Pong.NakamaMultiplayer;
using Pong.NakamaMultiplayer.Players;
using System;

namespace Pong.Gameplay.Systems;

/// <summary>
/// Syncs the local player's state across the network by sending frequent network packets containing relevent 
/// information such as velocity, position and game actions (jump, shoot, crouch, etc).
/// </summary>
public sealed class PlayerNetworkLocalSyncSystem : MoonTools.ECS.System
{
    readonly NetworkGameManager _gameManager;

    // How often to send the player's velocity and position across the network, in seconds.
    const int UPDATES_PER_SECOND = 20;
    readonly float StateFrequency = 1.0f / UPDATES_PER_SECOND;
    float _stateSyncTimer;

    readonly Filter _filter;

    public PlayerNetworkLocalSyncSystem(
        World world,
        NetworkGameManager gameManager
    ) : base(world)
    {
        _gameManager = gameManager;

        _filter = FilterBuilder
            .Include<PositionComponent>()
            .Include<VelocityComponent>()
            .Include<PlayerActionsComponent>()
            .Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in _filter.Entities)
        {
            ref readonly var position = ref Get<PositionComponent>(entity);
            ref readonly var velocity = ref Get<VelocityComponent>(entity);

            // Send the players current velocity and position every StateFrequency seconds.
            if (_stateSyncTimer <= 0)
            {
                // Send a network packet containing the player's velocity and position.
                _gameManager.SendMatchState(
                    OpCodes.VelocityAndPosition,
                    MatchDataJson.VelocityAndPosition(velocity.Value, position.Value));

                _stateSyncTimer = StateFrequency;
            }

            _stateSyncTimer -= (float)delta.TotalSeconds;

            ref readonly var playerActions = ref Get<PlayerActionsComponent>(entity);

            // If the players input hasn't changed, return early.
            if (!playerActions.MoveUp && !playerActions.MoveDown)
                continue;

            // Send network packet with the player's current actions.
            _gameManager.SendMatchState(
                OpCodes.Input,
                MatchDataJson.Input(playerActions.MoveUp, playerActions.MoveDown)
            );
        }
    }
}
