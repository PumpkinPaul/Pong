// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Pong.Engine;

namespace Pong.Gameplay.Components;

public readonly record struct BounceResponseComponent(
    CollisionEdge CollisionEdge
);