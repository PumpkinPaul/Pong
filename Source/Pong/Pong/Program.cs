// Copyright Pumpkin Games Ltd. All Rights Reserved.

using System;

namespace Pong;

static class Program
{
    [STAThread]
    static void Main()
    {
        new SinglePlayer.SinglePlayerGame().Run();
    }
}
