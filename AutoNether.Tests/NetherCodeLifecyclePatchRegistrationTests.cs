using System.Linq;
using System.Text.RegularExpressions;
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

        const string selection = "Harmony.CreateAndPatchAll(typeof(NetherAutoClimbCodeSelectionLifecyclePatch));";
        const string listInitialization =
            "Harmony.CreateAndPatchAll(typeof(NetherAutoClimbCodeListInitializationLifecyclePatch));";
        const string keepCancel = "Harmony.CreateAndPatchAll(typeof(NetherAutoClimbCodeKeepCancelLifecyclePatch));";
        const string transform = "Harmony.CreateAndPatchAll(typeof(NetherAutoClimbCodeTransformLifecyclePatch));";
        Assert.Empty(Regex.Matches(source, Regex.Escape(selection)).Cast<Match>());
        Assert.Single(Regex.Matches(source, Regex.Escape(listInitialization)).Cast<Match>());
        Assert.Empty(Regex.Matches(source, Regex.Escape(keepCancel)).Cast<Match>());
        Assert.Single(Regex.Matches(source, Regex.Escape(transform)).Cast<Match>());
        Assert.True(source.IndexOf(listInitialization, StringComparison.Ordinal) < source.IndexOf(transform, StringComparison.Ordinal));
    }

    [Fact]
    public void Patch_manager_registers_exact_start_status_state_machine_observer()
    {
        string root = FindRepositoryRoot();
        string manager = File.ReadAllText(Path.Combine(root, "AutoNether", "Patches", "PatchManager.cs"));
        string patch = File.ReadAllText(Path.Combine(root, "AutoNether", "Patches", "NetherAutoClimbPatch.cs"));

        const string registration =
            "Harmony.CreateAndPatchAll(typeof(NetherAutoClimbStartStatusLifecyclePatch));";
        Assert.Single(Regex.Matches(manager, Regex.Escape(registration)).Cast<Match>());
        Assert.Contains("GetStartStatusStateMachinePatchTarget()", patch);
        Assert.Contains("ObserveStartStatusStateMachineEnter(__instance)", patch);
        Assert.Contains("ObserveStartStatusStateMachineExit(__instance)", patch);
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
        string transformTarget = ExtractMethod(bridge, "internal static MethodBase? GetCodeTransformTaskPatchTarget()");
        Assert.DoesNotContain("GetCodeSelectionTaskPatchTarget", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("GetCodeKeepCancelTaskPatchTarget", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("NetherAutoClimbCodeSelectionLifecyclePatch", patch, StringComparison.Ordinal);
        Assert.DoesNotContain("NetherAutoClimbCodeKeepCancelLifecyclePatch", patch, StringComparison.Ordinal);
        Assert.Contains("NetherCodeConfirmTaskInvoker.TryInvoke", bridge, StringComparison.Ordinal);
        Assert.Contains("NetherCodeCancelTaskInvoker.TryInvoke", bridge, StringComparison.Ordinal);
        Assert.Contains("NetherLifecycleInteropBindings.CodeListInitializationTask", listInitializationTarget);
        Assert.Contains("NetherCodePopupInteropResolver.TryResolveStaticMethod", transformTarget);
        Assert.Contains("NetherCodeTransformNativeBinding.TransformTaskBinding", transformTarget);
        Assert.DoesNotContain("System.Threading.CancellationToken", transformTarget);

        Assert.Contains(
            "TargetMethod() => NetherRuntimeBridge.GetCodeListInitializationTaskPatchTarget()",
            patch
        );
        Assert.Contains(
            "TargetMethod() => NetherRuntimeBridge.GetCodeTransformTaskPatchTarget()",
            patch
        );
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
            "new NetherNativeMethodDescriptor(\"OnClickReplace\"",
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
