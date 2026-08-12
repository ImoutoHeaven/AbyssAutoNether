using System;
using AutoNether.Services;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Project.Api;
using Project.Ingame.Exploration;

namespace AutoNether.Patches;

/// <summary>
/// Observes the final StartQuest task returned to the native battle-scene caller. When
/// AbyssMod is installed, Priority.Last runs this postfix after its default-priority F11
/// wrapper, so F12 waits for the final reroll task without linking against AbyssMod.dll.
/// </summary>
[HarmonyPatch]
internal static class NetherBattleStartTaskCapturePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(
        typeof(ExplorationQuestPreserveAPIService),
        "Project_Ingame_Exploration_IExplorationQuestAPIService_StartQuestAsync"
    )]
    private static void Postfix(
        ExplorationQuestPreserveAPIService __instance,
        ref UniTask<BattleSessionStatusResponseEntity> __result
    )
    {
        try
        {
            if (__instance?._apiService == null)
                return;

            NetherAPIService netherApi = __instance._apiService.TryCast<NetherAPIService>();
            if (netherApi == null)
                return;

            NetherRuntimeBridge.ObserveBattleStartTask(__result);
            NetherAutoClimbController.LogDiagnostic(
                "runtime-lifecycle",
                new("action", "battle-task-captured"),
                new("source", "final-preserved-start-task"),
                new("interop", "optional-AbyssMod-ordering")
            );
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] final battle task observation failed: " + ex);
        }
    }
}
