// Copyright Pumpkin Games Ltd. All Rights Reserved.

using System;

namespace Pong.Engine;

public static class Logger
{
    public static void WriteLine(object value) => System.Diagnostics.Debug.WriteLine(value);
    public static void WriteLine(string value) => System.Diagnostics.Debug.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff} - {value}");
}
