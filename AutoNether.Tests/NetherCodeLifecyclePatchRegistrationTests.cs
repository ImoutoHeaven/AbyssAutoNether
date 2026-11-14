using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

/// <summary>
/// Keeps only the lifecycle observers that cannot be captured from their initiating call.
/// Selection and keep/cancel own their exact returned UniTasks directly; registering a Harmony
/// observer for either task creates a reflection/native callback boundary that is not guaranteed
/// to re-enter the managed interop wrapper.
/// </summary>
public class NetherCodeLifecyclePatchRegistrationTests
{
    [Fact]
    public void Patch_manager_does_not_detour_directly_owned_code_offer_tasks()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "AutoNether", "Patches", "PatchManager.cs"));

        const string selection = "typeof(NetherAutoClimbCodeSelectionLifecyclePatch)";
        const string listInitialization =
            "typeof(NetherAutoClimbCodeListInitializationLifecyclePatch)";
        const string keepCancel = "typeof(NetherAutoClimbCodeKeepCancelLifecyclePatch)";
        const string transform = "typeof(NetherAutoClimbCodeTransformLifecyclePatch)";
        const string generatedSequence = "typeof(NetherAutoClimbGeneratedSequenceLifecyclePatch)";
        Assert.Empty(Regex.Matches(source, Regex.Escape(selection)).Cast<Match>());
        Assert.Single(Regex.Matches(source, Regex.Escape(listInitialization)).Cast<Match>());
        Assert.Empty(Regex.Matches(source, Regex.Escape(keepCancel)).Cast<Match>());
        Assert.Empty(Regex.Matches(source, Regex.Escape(transform)).Cast<Match>());
        Assert.Empty(Regex.Matches(source, Regex.Escape(generatedSequence)).Cast<Match>());
    }

    [Fact]
    public void Patch_manager_registers_exact_start_status_state_machine_observer()
    {
        string root = FindRepositoryRoot();
        string manager = File.ReadAllText(Path.Combine(root, "AutoNether", "Patches", "PatchManager.cs"));
        string patch = File.ReadAllText(Path.Combine(root, "AutoNether", "Patches", "NetherAutoClimbPatch.cs"));

        const string legacyHarmonyPatch =
            "typeof(NetherAutoClimbStartStatusLifecyclePatch)";
        const string nativeDetourInstall =
            "NetherAutoClimbStartStatusLifecycleDetour.Install();";
        Assert.Empty(Regex.Matches(manager, Regex.Escape(legacyHarmonyPatch)).Cast<Match>());
        Assert.Single(Regex.Matches(manager, Regex.Escape(nativeDetourInstall)).Cast<Match>());
        Assert.DoesNotContain("NetherAutoClimbStartStatusLifecyclePatch", patch, StringComparison.Ordinal);
        Assert.Contains("GetStartStatusStateMachinePatchTarget()", patch);
        Assert.Contains("INativeDetour.CreateAndApply(", patch);
        Assert.Contains("ObserveStartStatusStateMachineEnter(", patch);
        Assert.Contains("ObserveStartStatusStateMachineExit(", patch);
    }

    [Fact]
    public void Lifecycle_patch_targets_delegate_to_the_same_versioned_packaged_bindings()
    {
        string root = FindRepositoryRoot();
        string bridge = File.ReadAllText(Path.Combine(root, "AutoNether", "Services", "NetherRuntimeBridge.cs"));
        string patch = File.ReadAllText(Path.Combine(root, "AutoNether", "Patches", "NetherAutoClimbPatch.cs"));

        string listInitializationTarget = ExtractMethod(
            bridge,
            "internal static MethodBase? GetCodeListInitializationTaskPatchTarget()"
        );
        Assert.DoesNotContain("GetCodeSelectionTaskPatchTarget", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("GetCodeKeepCancelTaskPatchTarget", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("GetCodeTransformTaskPatchTarget", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("GetGeneratedSequencePatchTargets", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("NetherAutoClimbCodeSelectionLifecyclePatch", patch, StringComparison.Ordinal);
        Assert.DoesNotContain("NetherAutoClimbCodeKeepCancelLifecyclePatch", patch, StringComparison.Ordinal);
        Assert.DoesNotContain("NetherAutoClimbCodeTransformLifecyclePatch", patch, StringComparison.Ordinal);
        Assert.DoesNotContain("NetherAutoClimbGeneratedSequenceLifecyclePatch", patch, StringComparison.Ordinal);
        Assert.Contains("NetherCodeConfirmTaskInvoker.TryInvoke", bridge, StringComparison.Ordinal);
        Assert.Contains("NetherCodeCancelTaskInvoker.TryInvoke", bridge, StringComparison.Ordinal);
        Assert.Contains("NetherCodeTransformTaskInvoker.TryInvoke", bridge, StringComparison.Ordinal);
        Assert.Contains("NetherLifecycleInteropBindings.CodeListInitializationTask", listInitializationTarget);

        Assert.Contains(
            "TargetMethod() => NetherRuntimeBridge.GetCodeListInitializationTaskPatchTarget()",
            patch
        );
    }

    [Fact]
    public void Code_list_initialization_patch_preserves_shared_unitask_before_observation()
    {
        string patch = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Patches",
            "NetherAutoClimbPatch.cs"
        ));
        int patchStart = patch.IndexOf(
            "internal static class NetherAutoClimbCodeListInitializationLifecyclePatch",
            StringComparison.Ordinal
        );
        Assert.True(patchStart >= 0, "unable to bound initialization patch");
        string initializationPatch = patch.Substring(patchStart);

        const string preserve = "__result = __result.Preserve();";
        const string observe = "NetherRuntimeBridge.ObserveCodeListInitializationTask(";
        int preserveIndex = initializationPatch.IndexOf(preserve, StringComparison.Ordinal);
        int observeIndex = initializationPatch.IndexOf(observe, StringComparison.Ordinal);

        Assert.True(preserveIndex >= 0, "raw UniTask is shared without Preserve()");
        Assert.True(
            observeIndex > preserveIndex,
            "the retained task must replace __result before native and plugin consumers share it"
        );
        Assert.Single(Regex.Matches(initializationPatch, Regex.Escape(preserve)).Cast<Match>());
    }

    [Fact]
    public void Keep_cancel_starts_and_retains_the_exact_task_without_callback_observer()
    {
        string bridge = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Services",
            "NetherRuntimeBridge.cs"
        ));
        int start = bridge.IndexOf(
            "NetherNativeActionResult INetherOwnedPopupNativeStagePort.InvokeCodeKeepCancel(",
            StringComparison.Ordinal
        );
        int end = bridge.IndexOf(
            "NetherNativeActionResult INetherOwnedPopupNativeStagePort.PollCodeKeepCancelTask(",
            start,
            StringComparison.Ordinal
        );
        Assert.True(start >= 0 && end > start, "unable to bound keep/cancel invocation");
        string method = bridge.Substring(start, end - start);

        Assert.Contains("NetherCodeCancelTaskInvoker.TryInvoke(", method, StringComparison.Ordinal);
        Assert.Contains("ObserveOwnedPopupKeepCancelTask(owner)", method, StringComparison.Ordinal);
        Assert.Contains("_codeKeepCancelTask = cancelTask", method, StringComparison.Ordinal);
        Assert.DoesNotContain("CancelCallbackBinding", method, StringComparison.Ordinal);
        Assert.DoesNotContain("TryInvokeVersionedGeneratedCallback", method, StringComparison.Ordinal);
    }

    [Fact]
    public void Replacement_waits_for_initialized_owned_list_before_reading_models_or_advancing()
    {
        string bridge = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Services",
            "NetherRuntimeBridge.cs"
        ));

        const string readiness =
            "NetherNativeActionResult initialization = PollCodeListInitializationTask(registration);";
        const string modelLookup = "TryFindCodeListSelection(registration.Controller, removeCodeId";
        int readinessIndex = bridge.IndexOf(readiness, StringComparison.Ordinal);
        int modelLookupIndex = bridge.IndexOf(modelLookup, StringComparison.Ordinal);
        Assert.True(readinessIndex >= 0, "missing code-list initialization gate");
        Assert.True(modelLookupIndex > readinessIndex, "model lookup ran before native initialization evidence");
        Assert.Contains("\"NetherCodeCategoryType\"", bridge, StringComparison.Ordinal);
        Assert.Contains(
            "NetherCodeListSelectionMapping.TryResolveTabIndex(",
            bridge,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "!= NetherCodeSelectionNativeStage.AwaitingReplacementConfirmation",
            bridge,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "private NetherNativeActionResult ConfirmCodeReplacement()",
            bridge,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Project.Nether.AbyssCodeReplacePopup.AbyssCodeReplacePopupController",
            bridge,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Project.Nether.AbyssCodeReplaceCompletePopup.AbyssCodeReplaceCompletePopupController",
            bridge,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "TryReadMember(confirmation.Value.Controller, \"_onCompleted\"",
            bridge,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "TryInvokeBooleanDelegate(",
            bridge,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "_codeSelectionFlow.DismissReplacementComplete(",
            bridge,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "TryInvokeNoArgumentDelegate(",
            bridge,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("RegisterCodeChangeConfirmationCore", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("_codeChangeConfirmLease", bridge, StringComparison.Ordinal);

        int selectStart = bridge.IndexOf(
            "private NetherNativeActionResult SelectCodeReplacement(",
            StringComparison.Ordinal
        );
        int selectEnd = bridge.IndexOf(
            "private NetherNativeActionResult PollCodeListInitializationTask(",
            selectStart,
            StringComparison.Ordinal
        );
        Assert.True(selectStart >= 0 && selectEnd > selectStart, "unable to bound replacement selection");
        string selectMethod = bridge.Substring(selectStart, selectEnd - selectStart);
        int confirmPreparationIndex = selectMethod.IndexOf(
            "_codeReplacementConfirmPopupWait.Clear();",
            StringComparison.Ordinal
        );
        int replaceClickIndex = selectMethod.IndexOf(
            "NetherNativeBindingCatalog.CodeListReplace.Method",
            StringComparison.Ordinal
        );
        Assert.True(confirmPreparationIndex >= 0, "missing replacement confirmation preparation");
        Assert.True(
            replaceClickIndex > confirmPreparationIndex,
            "replacement confirmation evidence must be reset before the native click"
        );

        // Recovery-floor target_type=7 remains a distinct Change flow.
        Assert.Contains(
            "CodeTransformConfirmPopupControllerTypeName",
            bridge,
            StringComparison.Ordinal
        );
        Assert.Contains("InvokeCodeChangeConfirmation(", bridge, StringComparison.Ordinal);
    }

    [Fact]
    public void Harmony_patch_targets_never_detour_il2cpp_value_type_instance_methods()
    {
        if (!Directory.Exists("/game/BepInEx/interop"))
            return;

        var context = new AssemblyLoadContext("value-type-patch-guard", isCollectible: true);
        try
        {
            context.Resolving += (_, name) =>
            {
                string interop = Path.Combine("/game/BepInEx/interop", name.Name + ".dll");
                if (File.Exists(interop))
                    return context.LoadFromAssemblyPath(interop);
                string core = Path.Combine("/game/BepInEx/core", name.Name + ".dll");
                return File.Exists(core) ? context.LoadFromAssemblyPath(core) : null;
            };
            Assembly[] interopAssemblies = Directory
                .GetFiles("/game/BepInEx/interop", "*.dll")
                .Select(context.LoadFromAssemblyPath)
                .ToArray();

            // Il2CppDetourMethodPatcher boxes Il2CppSystem.ValueType instance receivers without
            // copying mutations back to the native struct pointer. Any value-type state machine
            // observer must use an in-place native detour instead of [HarmonyPatch].
            NetherInteropPatchBinding[] harmonyBindings = NetherLifecycleInteropBindings.All
                .Concat(new[]
                {
                    NetherLifecycleInteropBindings.BattleSettingsOwnerDestroy,
                    NetherLifecycleInteropBindings.StartStatusTask,
                    NetherLifecycleInteropBindings.CodeListInitializationTask,
                    NetherNativeBindingCatalog.BattleStartTask,
                    NetherNativeBindingCatalog.BattleTerminal,
                    NetherNativeBindingCatalog.ResultTask,
                    NetherNativeBindingCatalog.BattleResultViewInitialization,
                    NetherNativeBindingCatalog.FloorEventSequence,
                })
                .ToArray();

            foreach (NetherInteropPatchBinding binding in harmonyBindings)
            {
                Type? targetType = interopAssemblies
                    .Select(assembly => assembly.GetType(binding.TypeName, throwOnError: false))
                    .FirstOrDefault(type => type != null);
                Assert.True(targetType != null, "Missing interop type: " + binding.TypeName);
                if (binding.Method.IsStatic != true)
                {
                    Assert.NotEqual(
                        "Il2CppSystem.ValueType",
                        targetType!.BaseType?.FullName
                    );
                }
            }
        }
        finally
        {
            context.Unload();
        }
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, "missing method: " + signature);
        int next = source.IndexOf("\n    internal static", start + signature.Length, StringComparison.Ordinal);
        Assert.True(next > start, "unable to bound method: " + signature);
        return source.Substring(start, next - start);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AutoNether.Tests", "AutoNether.Tests.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
