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
