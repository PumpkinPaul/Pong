// Copyright Pumpkin Games Ltd. All Rights Reserved.

namespace Pong.NakamaMultiplayer;

/// <summary>
/// Defines the various network operations that can be sent/received.
/// </summary>
public class OpCodes
{
    public const long VelocityAndPosition = 1;
    public const long Input = 2;
    public const long Died = 3;
    public const long Respawned = 4;
    public const long NewRound = 5;
}
