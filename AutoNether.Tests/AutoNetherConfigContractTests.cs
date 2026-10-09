#nullable enable

using Xunit;

namespace AutoNether.Tests;

public sealed class AutoNetherConfigContractTests
{
    [Fact]
    public void Controller_reads_checkpoint_ids_from_AutoNether_config()
    {
        string root = FindRepositoryRoot();
        string controller = File.ReadAllText(
            Path.Combine(root, "AutoNether", "Services", "NetherAutoClimbController.cs")
        );

        Assert.Contains("Config.CheckpointPreserveItemIds.Value", controller);
        Assert.DoesNotContain("Config.BattleSessionAutoSLNetherPreserveItemIds.Value", controller);
    }

    [Fact]
    public void Controller_builds_explicit_strategy_mode_and_research_family_settings()
    {
        string root = FindRepositoryRoot();
        string controller = File.ReadAllText(
            Path.Combine(root, "AutoNether", "Services", "NetherAutoClimbController.cs")
        );

        Assert.Contains("StrategyMode = Config.NetherAutoClimbStrategyMode.Value", controller);
        Assert.Contains("ResearchPrimaryFamily = Config.NetherAutoClimbResearchPrimaryFamily.Value", controller);
        Assert.Contains("ResearchSecondaryFamily = Config.NetherAutoClimbResearchSecondaryFamily.Value", controller);
        Assert.DoesNotContain("DetectStrategyMode", controller);
    }

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
