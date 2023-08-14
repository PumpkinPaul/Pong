// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using MoonTools.ECS;
using Pong.Gameplay.Components;
using System;

namespace Pong.NakamaMultiplayer.Systems;

public readonly record struct RemotePlayerSpawnMessage(
    Vector2 Position,
    Color Color,
    int BounceDirection
);

/// <summary>
/// Responsible for spawning Player entities with the correct components.
/// </summary>
public class RemotePlayerSpawnSystem : MoonTools.ECS.System
{
    public RemotePlayerSpawnSystem(World world) : base(world)
    {
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var message in ReadMessages<RemotePlayerSpawnMessage>())
        {
            var entity = CreateEntity();

            Set(entity, new PositionComponent(message.Position));
            Set(entity, new ScaleComponent(new Vector2(16, 64)));
            Set(entity, new ColorComponent(message.Color));
            Set(entity, new VelocityComponent());
            Set(entity, new CausesBounceComponent(message.BounceDirection));
        }
    }
}