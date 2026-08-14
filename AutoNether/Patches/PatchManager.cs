using AutoNether.Services;
using HarmonyLib;

namespace AutoNether.Patches;

public static class PatchManager
{
    public static void Initialize()
    {
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbPatch));
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbBattleSettingsDestroyPrefixPatch));
        Harmony.CreateAndPatchAll(typeof(NetherBattleStartTaskCapturePatch));
        Harmony.CreateAndPatchAll(typeof(NetherBattleTerminalPatch));
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbStartStatusLifecyclePatch));
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbStartStatusTaskPatch));
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbResultPatch));
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbBattleResultLifecyclePatch));
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbFloorEventSequenceLifecyclePatch));
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbCodeListInitializationLifecyclePatch));
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbCodeKeepCancelLifecyclePatch));
        Harmony.CreateAndPatchAll(typeof(NetherAutoClimbCodeTransformLifecyclePatch));

        NetherAutoClimbController.LogDiagnostic(
            "patch-manager",
            new NetherAutoClimbDiagnosticField("outcome", "autonether-patches-installed")
        );
    }
}
