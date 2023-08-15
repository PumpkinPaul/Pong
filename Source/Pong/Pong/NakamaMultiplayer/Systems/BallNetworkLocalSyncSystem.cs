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
public sealed class BallNetworkLocalSyncSystem : MoonTools.ECS.System
{
    readonly NetworkGameManager _networkGameManager;

    // How often to send the player's velocity and position across the network, in seconds.
    const int UPDATES_PER_SECOND = 30;
    readonly float StateFrequency = 1.0f / UPDATES_PER_SECOND;
    float _stateSyncTimer;

    readonly Filter _filter;

    public BallNetworkLocalSyncSystem(
        World world,
        NetworkGameManager networkGameManager
    ) : base(world)
    {
        _networkGameManager = networkGameManager;

        _filter = FilterBuilder
            .Include<PositionComponent>()
            .Include<DirectionalSpeedComponent>()
            .Build();
    }

    public override void Update(TimeSpan delta)
    {
        //Only the host should send shared world state
        if (!_networkGameManager.IsHost)
            return;

        foreach (var entity in _filter.Entities)
        {
            ref readonly var position = ref Get<PositionComponent>(entity);
            ref readonly var direction = ref Get<DirectionalSpeedComponent>(entity);

            // Send the players current velocity and position every StateFrequency seconds.
            if (_stateSyncTimer <= 0)
            {
                // Send a network packet containing the player's velocity and position.
                _networkGameManager.SendMatchState(
                    OpCodes.DirectionAndPosition,
                    MatchDataJson.DirectionAndPosition(direction.DirectionInRadians, position.Value));

                _stateSyncTimer = StateFrequency;
            }

            _stateSyncTimer -= (float)delta.TotalSeconds;
        }
    }
}
