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
    private bool _initialized;

    public const string PluginGuid = "Abyss.AutoNether";
    public const string PluginName = "Abyss AutoNether";
    public const string PluginVersion = "0.1.0";

    public static ConfigFile ConfigFile { get; private set; } = null!;
    public static new ManualLogSource Log { get; private set; } = null!;
    public static MonoBehaviour Instance { get; private set; } = null!;

    public override void Load()
    {
        Log = base.Log;
        Logger.Bind(Log);

        NetherNativeCompatibilityReport compatibility =
            NetherNativeCompatibilityPreflight.Validate(AppDomain.CurrentDomain.GetAssemblies());
        if (!compatibility.IsCompatible)
        {
            foreach (string failure in compatibility.Failures)
                Log.LogError("[AutoNether precheck] " + failure);
            Logger.Unbind(Log);
            throw new InvalidOperationException(
                $"AutoNether native compatibility precheck failed: "
                + string.Join(" | ", compatibility.Failures)
            );
        }
        Log.LogInfo(
            $"AutoNether native compatibility precheck passed: "
            + $"methods={compatibility.CheckedMethodCount}, "
            + $"generated={compatibility.CheckedGeneratedMethodCount}, "
            + $"members={compatibility.CheckedMemberCount}, "
            + $"compiled={compatibility.CheckedCompiledReferenceCount}."
        );

        ConfigFile = base.Config;
        AutoNether.Config.Initialize();
        Instance = AddComponent<Hotkey>();
        PatchManager.Initialize();
        // The production startup seam accepts only a snapshot-scoped authoritative provider.
        // This standalone build has no native-backed semantic provider, so null is explicit and
        // raw item/battle fields remain Unknown/fail-closed until an adapter is registered.
        NetherAutoClimbController.Initialize(typedSemanticProviderFactory: null);
        _initialized = true;

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
        if (_initialized)
            NetherAutoClimbController.OnPluginUnload();
        Logger.Unbind(Log);
        return base.Unload();
    }
}
