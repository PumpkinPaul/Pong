// Copyright Pumpkin Games Ltd. All Rights Reserved.

//Based on code from the FishGame Unity sample from Herioc Labs.
//https://github.com/heroiclabs/fishgame-unity/blob/main/FishGame/Assets/Entities/Player/PlayerNetworkLocalSync.cs

using Microsoft.Xna.Framework.Input;
using MoonTools.ECS;
using Pong.Gameplay.Components;
using Pong.NakamaMultiplayer;
using Pong.NakamaMultiplayer.Players;
using System;

namespace Pong.Gameplay.Systems;

/// <summary>
/// Syncs the local player's state across the network by sending frequent network packets containing relevent 
/// information such as velocity, position and inputs.
/// </summary>
public sealed class PlayerNetworkLocalSyncSystem : MoonTools.ECS.System
{
    readonly NetworkGameManager _gameManager;

    // How often to send the player's velocity and position across the network, in seconds.
    readonly float StateFrequency = 1 / 20f; //20 updates per second
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
            .Include<PlayerInputComponent>()
            .Build();
    }

    public override void Update(TimeSpan delta)
    {
        var keyBoardState = Keyboard.GetState();

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

            //TODO: add something exciting into the input - rockets? :-)
            continue;

            //TODO: Should use a GameInput wrapper here instead of PlayerInput directly
            ref readonly var playerInput = ref Get<PlayerInputComponent>(entity);

            // If the players input hasn't changed, return early.
            var moveUp = keyBoardState.IsKeyDown(playerInput.MoveUpKey);
            var moveDown = keyBoardState.IsKeyDown(playerInput.MoveDownKey);

            if (!moveUp && !moveDown)
                continue;

            // Send network packet with the player's current input.
            _gameManager.SendMatchState(
                OpCodes.Input,
                MatchDataJson.Input(moveUp, moveDown)
            );
        }
    }
}
