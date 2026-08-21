#nullable enable

using Xunit;

namespace AutoNether.Tests;

public sealed class AutoNetherBattleInteropContractTests
{
    [Fact]
    public void Start_task_patch_observes_final_task_after_optional_AbyssMod_wrapper()
    {
        string patch = Read("AutoNether", "Patches", "NetherBattleStartTaskCapturePatch.cs");

        Assert.Contains("HarmonyPriority(Priority.Last)", patch);
        Assert.Contains("ObserveBattleStartTask(__result)", patch);
        Assert.DoesNotContain("RunNether", patch);
        Assert.DoesNotContain("HasActiveNetherOperation", patch);
    }

    [Fact]
    public void Runtime_and_settlement_have_no_direct_F11_dependency()
    {
        string runtime = Read("AutoNether", "Services", "NetherRuntimeBridge.cs");
        string coordinator = Read("AutoNether", "Services", "NetherBattleSettlementCoordinator.cs");
        string controller = Read("AutoNether", "Services", "NetherAutoClimbController.cs");

        Assert.DoesNotContain("BattleSessionAutoSL", runtime);
        Assert.DoesNotContain("IsF11Busy", runtime);
        Assert.DoesNotContain("IsF11Busy", coordinator);
        Assert.DoesNotContain("AwaitingF11", coordinator);
        Assert.DoesNotContain("IsF11Busy", controller);
        Assert.DoesNotContain("AwaitingF11", controller);
        Assert.DoesNotContain("ObserveF11Busy", controller);
    }

    [Fact]
    public void Code_offer_registration_waits_for_initialized_native_model_before_detail_callback()
    {
        string runtime = Read("AutoNether", "Services", "NetherRuntimeBridge.cs");

        Assert.Contains(
            "CodeSelectPopupControllerTypeName => TryMapCodeSelectPopup(registration)",
            runtime
        );
        Assert.Contains(
            "TryReadMember(registration.Controller, \"_model\", out object? rawModel)",
            runtime
        );
        Assert.Contains("NetherCodePopupReadiness.Evaluate(", runtime);
    }

    [Fact]
    public void Authoritative_floor_scene_registration_primes_the_transition_cache_for_result_owned_code()
    {
        // Fresh current-game Cpp2IL: FloorSelection.SubViewController owns
        // HandleStartEventByStatusAsync, while NetherQuestBattleResultViewController and
        // AbyssCodeSelectPopupController are separate result-owned native controllers. The graph
        // must therefore be captured at the authoritative FloorSelection scene boundary.
        string runtime = Read("AutoNether", "Services", "NetherRuntimeBridge.cs");
        string coordinator = Read("AutoNether", "Services", "NetherBattleResultCodeCoordinator.cs");

        Assert.Contains("if (authoritativeSceneRegistration)", runtime, StringComparison.Ordinal);
        Assert.Contains(
            "PrimeTransitionSnapshotCacheFromAuthoritativeScene(generation, source);",
            runtime,
            StringComparison.Ordinal
        );
        Assert.Contains("floor-selection-authoritative-prime", runtime, StringComparison.Ordinal);
        Assert.Contains("missing-cached-floor-selection-snapshot", coordinator, StringComparison.Ordinal);
    }

    [Fact]
    public void Battle_result_code_policy_uses_the_result_owned_popup_party_not_a_torn_down_floor_scene()
    {
        // Fresh current-game Cpp2IL: AbyssCodeSelectPopupController.InitializeView receives and
        // stores NetherPartyModel, while FloorSelection.TransitionNetherResultScene changes scene.
        // The result owner must therefore be independently gated and leave route horizons unknown
        // until FloorSelection rebinds, rather than pretending OnEntered still exists.
        string runtime = Read("AutoNether", "Services", "NetherRuntimeBridge.cs");

        Assert.Contains("HasActiveBattleResultCodeOwner()", runtime);
        Assert.Contains("TryCaptureBattleResultCodeStrategyEvidence", runtime);
        Assert.Contains("TryMapStrategyPartyModel", runtime);
        Assert.Contains(
            "battle-result-code-route-horizon-unavailable-before-floor-scene-rebind",
            runtime
        );
    }

    [Fact]
    public void Return_popup_owns_its_already_initialized_nested_scroll_without_waiting_for_a_wrapper_hook()
    {
        string runtime = Read("AutoNether", "Services", "NetherRuntimeBridge.cs");

        Assert.Contains(
            "TryBindReturnScrollFromCurrentPopupCore(\"checkpoint-poll\")",
            runtime,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "TryBindReturnScrollFromPopupCore(registration, \"popup-registration\")",
            runtime,
            StringComparison.Ordinal
        );
        Assert.Contains("ParentPopup", runtime, StringComparison.Ordinal);
    }

    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(path).ToArray()));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AutoNether.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
