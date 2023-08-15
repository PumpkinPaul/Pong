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
    // How often to send the player's velocity and position across the network, in seconds.
    public float StateFrequency = 0.05f; //0.1f;
    float _stateSyncTimer;

    readonly Filter _filter;

    //TODO:
    PlayGamePhase gameManager;

    public PlayerNetworkLocalSyncSystem(World world) : base(world)
    {
        _filter = FilterBuilder
            .Include<PositionComponent>()
            .Include<VelocityComponent>()
            .Include<PlayerInputComponent>()
            .Build();

        //TODO:
        gameManager = NakamaMultiplayerGame.Instance.GamePhaseManager.Get<PlayGamePhase>();
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
                gameManager.SendMatchState(
                    OpCodes.VelocityAndPosition,
                    MatchDataJson.VelocityAndPosition(velocity.Value, position.Value));

                _stateSyncTimer = StateFrequency;
            }

            _stateSyncTimer -= (float)delta.TotalSeconds;

            //TODO: Should use a GameInput wrapper here instead of PlayerInput directly
            ref readonly var playerInput = ref Get<PlayerInputComponent>(entity);

            // If the players input hasn't changed, return early.
            var moveUp = keyBoardState.IsKeyDown(playerInput.MoveUpKey);
            var moveDown = keyBoardState.IsKeyDown(playerInput.MoveDownKey);

            if (!moveUp && !moveDown)
                continue;

            // Send network packet with the player's current input.
            gameManager.SendMatchState(
                OpCodes.Input,
                MatchDataJson.Input(moveUp, moveDown)
            );
        }
    }
}
