// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MoonTools.ECS;
using Pong.Gameplay.Components;
using System;

namespace Pong.NakamaMultiplayer.Systems;

public readonly record struct LocalPlayerSpawnMessage(
    PlayerIndex PlayerIndex,
    Keys MoveUpKey,
    Keys MoveDownKey,
    Vector2 Position,
    Color Color,
    int BounceDirection
);

/// <summary>
/// Responsible for spawning Player entities with the correct components.
/// </summary>
public class LocalPlayerSpawnSystem : MoonTools.ECS.System
{
    readonly PlayerEntityMapper _playerEntityMapper;

    public LocalPlayerSpawnSystem(
        World world,
        PlayerEntityMapper playerEntityMapper
    ) : base(world)
    {
        _playerEntityMapper = playerEntityMapper;
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var message in ReadMessages<LocalPlayerSpawnMessage>())
        {
            var entity = CreateEntity();

            _playerEntityMapper.MapEntity(message.PlayerIndex, entity);

            Set(entity, new PlayerInputComponent(message.PlayerIndex, message.MoveUpKey, message.MoveDownKey));
            Set(entity, new PositionComponent(message.Position));
            Set(entity, new ScaleComponent(new Vector2(16, 64)));
            Set(entity, new ColorComponent(Color.Red));
            Set(entity, new VelocityComponent());
            Set(entity, new CausesBounceComponent(message.BounceDirection));
        }
    }
}