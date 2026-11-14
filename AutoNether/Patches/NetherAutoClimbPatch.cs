#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using AutoNether.Services;
using BepInEx.Unity.IL2CPP.Hook;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.MethodInfo;

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
    private static MethodBase TargetMethod() =>
        NetherNativeBindingCatalog.ResolveRequiredMethod(NetherNativeBindingCatalog.ResultTask);

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
/// Hooks the generated FloorSelection startup/resume value-type state machine directly at its
/// native function pointer. Harmony's Il2CppDetourMethodPatcher boxes Il2CppSystem.ValueType
/// receivers without copying mutations back to the caller's struct pointer, which freezes
/// multi-await state transitions across Elevator Skip and recovered Code-offer resumptions.
/// </summary>
internal static unsafe class NetherAutoClimbStartStatusLifecycleDetour
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void MoveNextDelegate(IntPtr thisPtr, IntPtr methodInfo);

    private static INativeDetour? _detour;
    private static MoveNextDelegate? _detourCallback;
    private static MoveNextDelegate? _trampoline;
    private static IntPtr _stateMachineClassPtr;
    private static int _stateMachineValueSize;
    private static ConstructorInfo? _wrapperConstructor;

    internal static void Install()
    {
        if (_detour != null)
            return;

        if (NetherRuntimeBridge.GetStartStatusStateMachinePatchTarget() is not MethodInfo moveNext
            || moveNext.DeclaringType == null)
        {
            throw new MissingMethodException(
                "Missing required StartStatus state-machine MoveNext target."
            );
        }

        Type stateMachineType = moveNext.DeclaringType;
        FieldInfo methodInfoPtrField =
            Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(moveNext)
            ?? throw new MissingFieldException(
                stateMachineType.FullName,
                "NativeMethodInfoPtr_MoveNext"
            );
        IntPtr methodInfoPtr = (IntPtr)(methodInfoPtrField.GetValue(null) ?? IntPtr.Zero);
        if (methodInfoPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Null native MethodInfo pointer for StartStatus state-machine MoveNext."
            );
        }

        IntPtr classPtr = Il2CppClassPointerStore.GetNativeClassPointer(stateMachineType);
        uint align = 0;
        int valueSize = classPtr != IntPtr.Zero
            ? IL2CPP.il2cpp_class_value_size(classPtr, ref align)
            : 0;
        ConstructorInfo? wrapperCtor = stateMachineType.GetConstructor(new[] { typeof(IntPtr) });
        if (classPtr == IntPtr.Zero || valueSize <= 0 || wrapperCtor == null)
        {
            throw new InvalidOperationException(
                "Invalid IL2CPP value-type metadata for " + stateMachineType.FullName
            );
        }

        INativeMethodInfoStruct nativeMethod =
            UnityVersionHandler.Wrap((Il2CppMethodInfo*)methodInfoPtr);
        if (nativeMethod.MethodPointer == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Null native MethodPointer for StartStatus state-machine MoveNext."
            );
        }

        _stateMachineClassPtr = classPtr;
        _stateMachineValueSize = valueSize;
        _wrapperConstructor = wrapperCtor;
        _detourCallback = DetourMoveNext;
        _detour = INativeDetour.CreateAndApply(
            nativeMethod.MethodPointer,
            _detourCallback,
            out _trampoline
        );
    }

    private static void DetourMoveNext(IntPtr thisPtr, IntPtr methodInfo)
    {
        MoveNextDelegate? trampoline = _trampoline;
        if (trampoline == null)
            return;
        if (thisPtr == IntPtr.Zero)
        {
            trampoline(thisPtr, methodInfo);
            return;
        }

        IntPtr boxedSnapshot = IntPtr.Zero;
        object? stateMachineSnapshot = null;
        try
        {
            boxedSnapshot = IL2CPP.il2cpp_value_box(_stateMachineClassPtr, thisPtr);
            if (boxedSnapshot != IntPtr.Zero && _wrapperConstructor != null)
            {
                stateMachineSnapshot = _wrapperConstructor.Invoke(new object[] { boxedSnapshot });
                NetherRuntimeBridge.ObserveStartStatusStateMachineEnter(stateMachineSnapshot);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("[F12][AutoNether] start-status state-machine enter observation failed: " + ex);
        }

        trampoline(thisPtr, methodInfo);

        if (stateMachineSnapshot == null || boxedSnapshot == IntPtr.Zero)
            return;

        try
        {
            IntPtr unboxedSnapshot = IL2CPP.il2cpp_object_unbox(boxedSnapshot);
            Buffer.MemoryCopy(
                thisPtr.ToPointer(),
                unboxedSnapshot.ToPointer(),
                _stateMachineValueSize,
                _stateMachineValueSize
            );
            NetherRuntimeBridge.ObserveStartStatusStateMachineExit(stateMachineSnapshot);
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
    private static MethodBase TargetMethod() =>
        NetherNativeBindingCatalog.ResolveRequiredMethod(
            NetherNativeBindingCatalog.BattleResultViewInitialization
        );

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
    private static MethodBase TargetMethod() =>
        NetherNativeBindingCatalog.ResolveRequiredMethod(NetherNativeBindingCatalog.FloorEventSequence);

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
