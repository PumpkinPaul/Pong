// Copyright Pumpkin Games Ltd. All Rights Reserved.

namespace Pong.Gameplay.Components;

public readonly record struct PlayerActionsComponent(
    bool MoveUp,
    bool MoveDown
);