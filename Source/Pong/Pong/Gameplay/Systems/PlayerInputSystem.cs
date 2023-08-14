// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MoonTools.ECS;
using Pong.Gameplay.Components;
using System;

namespace Pong.Gameplay.Systems;

/// <summary>
/// Responsible for checking imput devices, converting button presses into game actions.
/// </summary>
/// <example>
/// Check the state of the 'Q' key and turn it into a 'move up' command if it is pressed.
/// </example>
public sealed class PlayerInputSystem : MoonTools.ECS.System
{
    readonly Filter _filter;

    public PlayerInputSystem(World world) : base(world)
    {
        _filter = FilterBuilder
            .Include<PlayerInputComponent>()
            .Build();
    }

    public override void Update(TimeSpan delta)
    {
        var keyBoardState = Keyboard.GetState();

        foreach (var entity in _filter.Entities)
        {
            ref readonly var playerInput = ref Get<PlayerInputComponent>(entity);

            const int PADDLE_SPEED = 5;

            var moveUp = keyBoardState.IsKeyDown(playerInput.MoveUpKey)
                ? PADDLE_SPEED : 0;

            var moveDown = keyBoardState.IsKeyDown(playerInput.MoveDownKey)
                ? -PADDLE_SPEED : 0;

            Set(entity,
                new VelocityComponent(new Vector2(0, moveUp + moveDown)));
        }
    }
}
