// Copyright Pumpkin Games Ltd. All Rights Reserved.

//Based on code from the FishGame Unity sample from Herioc Labs.
//https://github.com/heroiclabs/fishgame-unity/blob/main/FishGame/Assets/Entities/Player/PlayerNetworkLocalSync.cs

using Microsoft.Xna.Framework;
using MoonTools.ECS;
using Pong.Gameplay.Components;
using System;

namespace Pong.Gameplay.Systems;

public readonly record struct MatchDataVelocityAndPositionMessage(
    Entity Entity,
    Vector2 LerpToPosition
);

/// <summary>
/// 
/// </summary>
public sealed class PlayerNetworkRemoteSyncSystem : MoonTools.ECS.System
{
    public PlayerNetworkRemoteSyncSystem(World world) : base(world)
    {
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var message in ReadMessages<MatchDataVelocityAndPositionMessage>())
        {
            ref readonly var position = ref Get<PositionComponent>(message.Entity);

            Set(message.Entity, new LerpPositionComponent
            {
                ToPosition = message.LerpToPosition,
                FromPosition = position.Value
            });
        }
    }
}
