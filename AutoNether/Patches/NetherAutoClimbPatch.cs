#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using AutoNether.Services;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace AutoNether.Patches;

/// <summary>
/// Lifecycle-only instrumentation for F12.  It never calls bridge actions from a Harmony
/// callback; the main-thread coordinator consumes these registrations on a later Update.
/// </summary>
[HarmonyPatch]
internal static class NetherAutoClimbPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => NetherRuntimeBridge.GetPatchTargets();

    [HarmonyPostfix]
    private static void Postfix(MethodBase __originalMethod, object __instance, object[] __args)
    {
        try
        {
            NetherRuntimeBridge.ObservePatchedCall(__originalMethod, __instance, __args);
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] native lifecycle observation failed: " + ex);
        }
    }
}

/// <summary>
/// Result construction itself is the native Result request flow.  Keeping this patch separate
/// lets Harmony pass the returned UniTask so F12 can wait for success rather than completing
/// merely because the Result scene was opened.
/// </summary>
[HarmonyPatch]
internal static class NetherAutoClimbResultPatch
{
    private static MethodBase? TargetMethod()
    {
        Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        Type? type = NetherLifecycleInteropBindings.ResolveType(
            loadedAssemblies,
            "Project.NetherTop.Result.SubViewController"
        );
        Type? partyCharacterType = NetherLifecycleInteropBindings.ResolveType(
            loadedAssemblies,
            "Project.NetherTop.Result.NetherResultPartyCharacterModel"
        );
        if (type == null || partyCharacterType == null)
        {
            NetherAutoClimbController.LogDiagnostic(
                "binding",
                new("family", "result-task"),
                new("outcome", "missing-type"),
                new("controllerType", type?.FullName ?? "Project.NetherTop.Result.SubViewController"),
                new("partyType", partyCharacterType?.FullName ?? "Project.NetherTop.Result.NetherResultPartyCharacterModel")
            );
            return null;
        }
        Type partyCharacterArrayType = typeof(Il2CppReferenceArray<>).MakeGenericType(partyCharacterType);

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!string.Equals(method.Name, "CreateNetherResultModelAsync", StringComparison.Ordinal))
                continue;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 2
                || parameters[0].ParameterType != typeof(bool)
                || parameters[1].ParameterType != partyCharacterArrayType
                || method.ReturnType != typeof(UniTask))
            {
                continue;
            }
            NetherAutoClimbController.LogDiagnostic(
                "binding",
                new("family", "result-task"),
                new("outcome", "resolved"),
                new("type", type.FullName ?? type.Name),
                new("method", method.Name)
            );
            return method;
        }
        NetherAutoClimbController.LogDiagnostic(
            "binding",
            new("family", "result-task"),
            new("outcome", "missing-method"),
            new("type", type.FullName ?? type.Name),
            new("method", "CreateNetherResultModelAsync")
        );
        return null;
    }

    [HarmonyPostfix]
    private static void Postfix(ref UniTask __result)
    {
        try
        {
            // Observation only: the coordinator polls this task later from Hotkey.Update.
            NetherRuntimeBridge.ObserveResult(__result);
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] native result observation failed: " + ex);
        }
    }
}

/// <summary>
/// Restores the battle-scoped Auto/speed lease while BottomRightView still owns its exact
/// IIngameUserSettings instance. The ordinary lifecycle postfix unbinds it after OnDestroy.
/// </summary>
[HarmonyPatch]
internal static class NetherAutoClimbBattleSettingsDestroyPrefixPatch
{
    private static MethodBase? TargetMethod()
    {
        bool resolved = NetherLifecycleInteropBindings.TryResolve(
            AppDomain.CurrentDomain.GetAssemblies(),
            NetherLifecycleInteropBindings.BattleSettingsOwnerDestroy,
            out string error,
            out MethodInfo? method
        );
        NetherAutoClimbController.LogDiagnostic(
            "binding",
            new("family", "battle-settings-destroy-prefix"),
            new("outcome", resolved ? "resolved" : "missing-method"),
            new("type", NetherLifecycleInteropBindings.BattleSettingsOwnerDestroy.TypeName),
            new("method", NetherLifecycleInteropBindings.BattleSettingsOwnerDestroy.Method.Name),
            new("detail", resolved ? "exact-signature" : error)
        );
        return method;
    }

    [HarmonyPrefix]
    private static void Prefix(MethodBase __originalMethod, object __instance)
    {
        try
        {
            NetherRuntimeBridge.ObservePatchedCallEntering(__originalMethod, __instance);
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] battle settings destroy prefix failed: " + ex);
        }
    }
}

/// <summary>
/// Captures the actual generated FloorSelection startup/resume state machine. Native OnEntered
/// invokes MoveNext directly and therefore bypasses the public async wrapper. The prefix owns
/// popups created during MoveNext; the postfix exposes the builder's pollable parent UniTask.
/// </summary>
[HarmonyPatch]
internal static class NetherAutoClimbStartStatusLifecyclePatch
{
    private static MethodBase? TargetMethod() =>
        NetherRuntimeBridge.GetStartStatusStateMachinePatchTarget();

    [HarmonyPrefix]
    private static void Prefix(object __instance)
    {
        try
        {
            NetherRuntimeBridge.ObserveStartStatusStateMachineEnter(__instance);
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] start-status state-machine enter observation failed: " + ex);
        }
    }

    [HarmonyPostfix]
    private static void Postfix(object __instance)
    {
        try
        {
            NetherRuntimeBridge.ObserveStartStatusStateMachineExit(__instance);
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] start-status state-machine exit observation failed: " + ex);
        }
    }
}

/// <summary>
/// Current scene entry bypasses this wrapper, but later native refreshes may call it directly.
/// Capture its returned UniTask so both entry paths share the same recovered-code owner.
/// </summary>
[HarmonyPatch]
internal static class NetherAutoClimbStartStatusTaskPatch
{
    private static MethodBase? TargetMethod() =>
        NetherRuntimeBridge.GetStartStatusTaskPatchTarget();

    [HarmonyPostfix]
    private static void Postfix(object __instance, ref UniTask __result)
    {
        try
        {
            NetherRuntimeBridge.ObserveStartStatusTask(__instance, __result);
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] start-status wrapper task observation failed: " + ex);
        }
    }
}

/// <summary>
/// Captures the exact result-view initialization task that installs the visible Next button.
/// The main-thread coordinator waits for this task, invokes the game's generated Next callback
/// once, and then waits for a strictly newer FloorSelection owner before resuming automation.
/// </summary>
[HarmonyPatch]
internal static class NetherAutoClimbBattleResultLifecyclePatch
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static MethodBase? TargetMethod()
    {
        Type? type = NetherLifecycleInteropBindings.ResolveType(
            AppDomain.CurrentDomain.GetAssemblies(),
            NetherBattleResultNextNativeBinding.ControllerTypeName
        );
        if (type == null)
        {
            NetherAutoClimbController.LogDiagnostic(
                "binding",
                new("family", "battle-result-next"),
                new("outcome", "missing-type"),
                new("type", NetherBattleResultNextNativeBinding.ControllerTypeName),
                new("method", NetherBattleResultNextNativeBinding.InitializeViewDescriptor.Name)
            );
            return null;
        }

        bool resolved = NetherLifecycleInteropBindings.TryResolveExactMethod(
            type,
            NetherBattleResultNextNativeBinding.InitializeViewDescriptor,
            InstanceFlags,
            out string error,
            out MethodInfo? method
        );
        NetherAutoClimbController.LogDiagnostic(
            "binding",
            new("family", "battle-result-next"),
            new("outcome", resolved ? "resolved" : "missing-method"),
            new("type", type.FullName ?? type.Name),
            new("method", NetherBattleResultNextNativeBinding.InitializeViewDescriptor.Name),
            new("detail", resolved ? "exact-signature" : error)
        );
        return method;
    }

    [HarmonyPostfix]
    private static void Postfix(object __instance, ref UniTask __result)
    {
        try
        {
            NetherRuntimeBridge.ObserveBattleResultView(__instance, __result);
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] battle result view observation failed: " + ex);
        }
    }
}

/// <summary>
/// Captures the exact interactive-floor sequence task.  OnFloorClickedEventAsync schedules
/// the movement/event branch through UniTask.Void and returns too early; this task remains
/// pending until the Event/Recovery/Treasure flow has actually finished.
/// </summary>
[HarmonyPatch]
internal static class NetherAutoClimbFloorEventSequenceLifecyclePatch
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static MethodBase? TargetMethod()
    {
        Type? type = NetherLifecycleInteropBindings.ResolveType(
            AppDomain.CurrentDomain.GetAssemblies(),
            NetherFloorEventSequenceNativeBinding.ControllerTypeName
        );
        if (type == null)
        {
            NetherAutoClimbController.LogDiagnostic(
                "binding",
                new("family", "floor-event-sequence"),
                new("outcome", "missing-type"),
                new("type", NetherFloorEventSequenceNativeBinding.ControllerTypeName),
                new("method", NetherFloorEventSequenceNativeBinding.SequenceDescriptor.Name)
            );
            return null;
        }

        bool resolved = NetherLifecycleInteropBindings.TryResolveExactMethod(
            type,
            NetherFloorEventSequenceNativeBinding.SequenceDescriptor,
            InstanceFlags,
            out string error,
            out MethodInfo? method
        );
        NetherAutoClimbController.LogDiagnostic(
            "binding",
            new("family", "floor-event-sequence"),
            new("outcome", resolved ? "resolved" : "missing-method"),
            new("type", type.FullName ?? type.Name),
            new("method", NetherFloorEventSequenceNativeBinding.SequenceDescriptor.Name),
            new("detail", resolved ? "exact-signature" : error)
        );
        return method;
    }

    [HarmonyPostfix]
    private static void Postfix(object __instance, ref UniTask __result)
    {
        try
        {
            NetherRuntimeBridge.ObserveFloorEventSequenceTask(__instance, __result);
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] floor event sequence observation failed: " + ex);
        }
    }
}

/// <summary>
/// Captures the exact asynchronous initialization of a code-list popup. SetupPopupEvent can
/// register the replacement UI before its model dictionary is populated, so the returned task
/// is the native readiness boundary for replacement selection.
/// </summary>
[HarmonyPatch]
internal static class NetherAutoClimbCodeListInitializationLifecyclePatch
{
    private static MethodBase? TargetMethod() => NetherRuntimeBridge.GetCodeListInitializationTaskPatchTarget();

    [HarmonyPostfix]
    private static void Postfix(object __instance, object[] __args, ref UniTask __result)
    {
        try
        {
            if (__args != null && __args.Length == 1 && __args[0] != null)
            {
                // InitializePopupAsync returns a pooled, token-versioned UniTask. The native
                // caller awaits it while AutoNether polls Status, so both consumers must share
                // the memoized task returned by Preserve rather than the raw single-use source.
                __result = __result.Preserve();
                NetherRuntimeBridge.ObserveCodeListInitializationTask(
                    __instance,
                    __args[0],
                    __result
                );
            }
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] native code-list initialization observation failed: " + ex);
        }
    }
}

/// <summary>
/// Observes the exact generated UniTask created by a Change-list OnClickChange.  The task
/// owns the confirmation popup, server update, completion popup, and final native terminal;
/// F12 never synthesizes or replays that request.
/// </summary>
[HarmonyPatch]
internal static class NetherAutoClimbCodeTransformLifecyclePatch
{
    private static MethodBase? TargetMethod() => NetherRuntimeBridge.GetCodeTransformTaskPatchTarget();

    [HarmonyPostfix]
    private static void Postfix(object[] __args, ref UniTask __result)
    {
        try
        {
            if (__args != null && __args.Length == 6 && __args[0] != null && __args[3] != null)
                NetherRuntimeBridge.ObserveCodeTransformTask(__args[0], __args[3], __result);
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] native code transform task observation failed: " + ex);
        }
    }
}
