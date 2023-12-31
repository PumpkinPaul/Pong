// Copyright Pumpkin Games Ltd. All Rights Reserved.

namespace Pong.NakamaMultiplayer.GamePhases;

/// <summary>
/// Base class for all the different game phases (e.g. main menu, playing the game, game over, etc).
/// </summary>
public abstract class GamePhase
{
    /// <summary>The number of ticks since the state was created.</summary>
    protected int ElapsedTicks;

    public virtual void Initialise() { }

    public void Create()
    {
        ElapsedTicks = 0;

        OnCreate();
    }

    public virtual bool SupportsPause => false;

    public void Update()
    {
        ElapsedTicks++;

        OnUpdate();
    }

    public void Draw() => OnDraw();

    public void Destroy() => OnDestroy();

    protected virtual void OnCreate() { }
    protected virtual void OnUpdate() { }
    protected virtual void OnDraw() { }
    protected virtual void OnDestroy() { }
}
