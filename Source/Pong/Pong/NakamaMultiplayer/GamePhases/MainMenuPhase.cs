// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Pong.Engine;

namespace Pong.NakamaMultiplayer.GamePhases;

/// <summary>
/// Main Menu Processing
/// </summary>
public class MainMenuPhase : GamePhase
{
    enum Phase
    {
        Ready,
        FindMatch
    }

    Phase _phase = Phase.Ready;

    readonly NakamaConnection _nakamaConnection;

    public MainMenuPhase(
        NakamaConnection nakamaConnection)
    {
        _nakamaConnection = nakamaConnection;
    }

    protected async override void OnUpdate()
    {
        base.OnUpdate();

        if (PongGame.Instance.KeyboardState.IsKeyDown(Keys.Space) && PongGame.Instance.PreviousKeyboardState.IsKeyUp(Keys.Space))
        {
            if (_phase == Phase.Ready)
            {
                _phase = Phase.FindMatch;
                await _nakamaConnection.FindMatch();
            }
            else
            {
                _phase = Phase.Ready;
                await _nakamaConnection.CancelMatchmaking();
            }
        }
    }


    protected override void OnDraw()
    {
        base.OnDraw();

        //Draw the UI
        PongGame.Instance.SpriteBatch.Begin();
        //_scoreRenderer.Draw();
        PongGame.Instance.SpriteBatch.DrawString(Resources.GameFont, "Pong", new Vector2(300, PongGame.SCREEN_HEIGHT * 0.25f), Color.Red);
        
        switch(_phase)
        {
            case Phase.Ready:
                PongGame.Instance.SpriteBatch.DrawString(Resources.SmallFont, "Press SPACE to play!", new Vector2(250, 220), Color.Red);
                break;

            case Phase.FindMatch:
                PongGame.Instance.SpriteBatch.DrawString(Resources.SmallFont, "Searching for match...", new Vector2(250, 220), Color.Red);
                PongGame.Instance.SpriteBatch.DrawString(Resources.SmallFont, "Press SPACE to cancel", new Vector2(240, 260), Color.Red);
                break;
        }

        PongGame.Instance.SpriteBatch.End();
    }
}
