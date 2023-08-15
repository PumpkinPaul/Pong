// Copyright Pumpkin Games Ltd. All Rights Reserved.

using MoonTools.ECS;

namespace Pong.Gameplay.Components;

public readonly record struct AngledBounceResponseComponent(
    Entity BouncedBy
);