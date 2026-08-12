using System;
using Absf;
using AutoNether.Services;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Project.Api;
using Project.BattleResult;
using Project.Ingame;

namespace AutoNether.Patches;

/// <summary>
/// Observes the authoritative Nether battle terminal and clear-response party HP. It does
/// not submit or rewrite settlement data.
/// </summary>
[HarmonyPatch]
internal static class NetherBattleTerminalPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BattleResultUtility), nameof(BattleResultUtility.CreateBattleResultModel))]
    private static void Prefix(
        BattleResultType resultType,
        ISceneTransitionParam startParam,
        IFinishQuestResponseEntity entity
    )
    {
        try
        {
            int questType = entity == null ? 0 : (int)entity.QuestType;
            NetherBattleTerminalKind terminal = NetherBattleTerminalObservationPolicy.Classify(
                questType,
                (int)resultType
            );
            NetherClearBattleResponseEntity clearResponse = terminal == NetherBattleTerminalKind.Clear
                ? entity?.TryCast<NetherClearBattleResponseEntity>()
                : null;

            if (clearResponse?.t_nether_characters != null)
                NetherRuntimeBridge.ObserveBattleResultCharacters(clearResponse.t_nether_characters);

            switch (terminal)
            {
                case NetherBattleTerminalKind.Clear:
                    NetherRuntimeBridge.ObserveBattleClear();
                    break;
                case NetherBattleTerminalKind.Close:
                    NetherRuntimeBridge.ObserveBattleClose();
                    break;
            }

            if (questType == NetherBattleTerminalObservationPolicy.NetherBattleQuestType)
            {
                NetherAutoClimbController.LogDiagnostic(
                    "runtime-lifecycle",
                    new("action", "battle-result-terminal-observed"),
                    new("source", "BattleResultUtility.CreateBattleResultModel"),
                    new("resultType", resultType.ToString()),
                    new("resultTypeValue", ((int)resultType).ToString()),
                    new("terminal", terminal.ToString()),
                    new("startParamType", startParam?.GetType().FullName ?? "none"),
                    new("responseType", entity?.GetType().FullName ?? "none")
                );
            }
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] battle terminal observation failed: " + ex);
        }
    }
}
