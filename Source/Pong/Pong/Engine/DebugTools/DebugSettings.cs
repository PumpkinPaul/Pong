﻿// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Newtonsoft.Json;
using System.IO;

namespace Pong.Engine.DebugTools;

public class DebugSettings
{
    [JsonIgnore]
    public static string FullPath => Path.Combine(PongGame.LocalApplicationDataPath, "DebugSettings.json");

    public static DebugSettings Create()
    {
        DebugSettings settings = null;
        try
        {
            if (File.Exists(FullPath))
            {
                var json = File.ReadAllText(FullPath);
                return JsonConvert.DeserializeObject<DebugSettings>(json);
            }
        }
        catch
        {

        }

        return settings ?? new DebugSettings();

    }

    public bool ShowEntityPositions;
    public bool ShowEntityCollisionBounds;
    public bool ShowEntityCollisionRadius;

    public bool ShowGcCounter;
    public bool ShowFpsCounter;
    public bool ShowTimeRuler;
    public bool ShowPlots;

    public void Save()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(FullPath, json);
    }
}