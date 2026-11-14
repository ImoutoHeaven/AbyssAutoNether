using System;
using System.Collections.Generic;
using AutoNether.Services;
using HarmonyLib;

namespace AutoNether.Patches;

public static class PatchManager
{
    public static IReadOnlyList<Type> PatchTypes { get; } = new[]
    {
        typeof(NetherAutoClimbPatch),
        typeof(NetherAutoClimbBattleSettingsDestroyPrefixPatch),
        typeof(NetherBattleStartTaskCapturePatch),
        typeof(NetherBattleTerminalPatch),
        typeof(NetherAutoClimbStartStatusTaskPatch),
        typeof(NetherAutoClimbResultPatch),
        typeof(NetherAutoClimbBattleResultLifecyclePatch),
        typeof(NetherAutoClimbFloorEventSequenceLifecyclePatch),
        typeof(NetherAutoClimbCodeListInitializationLifecyclePatch),
    };

    public static void Initialize()
    {
        foreach (Type patchType in PatchTypes)
            Harmony.CreateAndPatchAll(patchType);
        NetherAutoClimbStartStatusLifecycleDetour.Install();

        NetherAutoClimbController.LogDiagnostic(
            "patch-manager",
            new NetherAutoClimbDiagnosticField("outcome", "autonether-patches-installed")
        );
    }
}
