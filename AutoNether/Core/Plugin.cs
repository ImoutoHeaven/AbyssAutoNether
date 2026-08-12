using System;
using System.Linq;
using AutoNether.Patches;
using AutoNether.Services;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace AutoNether;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("AbyssMod", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "Abyss.AutoNether";
    public const string PluginName = "Abyss AutoNether";
    public const string PluginVersion = "0.1.0";

    public static ConfigFile ConfigFile { get; private set; } = null!;
    public static new ManualLogSource Log { get; private set; } = null!;
    public static MonoBehaviour Instance { get; private set; } = null!;

    public override void Load()
    {
        Log = base.Log;
        ConfigFile = base.Config;

        AutoNether.Config.Initialize();
        Instance = AddComponent<Hotkey>();
        PatchManager.Initialize();
        NetherAutoClimbController.Initialize();

        Log.LogInfo($"{PluginName} {PluginVersion} loaded; F12 controls Nether auto-climb.");
        bool abyssModDetected = AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(assembly => string.Equals(
                assembly.GetName().Name,
                "AbyssMod",
                StringComparison.Ordinal
            ));
        NetherAutoClimbController.LogDiagnostic(
            "build",
            new("pluginGuid", PluginGuid),
            new("version", PluginVersion),
            new("profile", "standalone-autonether"),
            new("abyssModDetected", abyssModDetected.ToString()),
            new("interop", "final-task-capture")
        );
    }

    public override bool Unload()
    {
        NetherAutoClimbController.OnPluginUnload();
        return base.Unload();
    }
}
