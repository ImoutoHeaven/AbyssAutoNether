#nullable enable

using Xunit;

namespace AutoNether.Tests;

public sealed class AutoNetherConfigContractTests
{
    [Fact]
    public void Config_contains_only_F12_settings_and_owns_checkpoint_preserve_ids()
    {
        string root = FindRepositoryRoot();
        string config = File.ReadAllText(Path.Combine(root, "AutoNether", "Core", "Config.cs"));

        Assert.Contains("CheckpointPreserveItemIds", config);
        Assert.Contains("MaximumDepth", config);
        Assert.Contains("SoftErosionLimit", config);
        Assert.Contains("MinimumCharacterHpPermille", config);
        Assert.Contains("CombatLane", config);
        Assert.Contains("CodeReloadReserve", config);
        Assert.Contains("TreasureMode", config);
        Assert.Contains("ShopMode", config);
        Assert.Contains("DetailedLogging", config);
        Assert.Contains("StrategyMode", config);
        Assert.Contains("NetherStrategyMode.Equipment", config);
        Assert.Contains("ResearchPrimaryFamily", config);
        Assert.Contains("ResearchSecondaryFamily", config);
        Assert.DoesNotContain("BattleSessionAutoSL", config);
        Assert.DoesNotContain("MachineTranslation", config);
        Assert.DoesNotContain("TranslationCDN", config);
        Assert.DoesNotContain("FontBundlePath", config);
    }

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

    [Fact]
    public void Documents_define_explicit_modes_research_validation_and_native_boundaries()
    {
        string root = FindRepositoryRoot();
        string readme = File.ReadAllText(Path.Combine(root, "README.md"));
        string strategySpec = File.ReadAllText(Path.Combine(
            root,
            "docs",
            "specs",
            "evidence-backed-strategy-modes.md"
        ));

        Assert.Contains("`StrategyMode` | `Equipment`", readme);
        Assert.Contains("`ResearchPrimaryFamily` | `Unknown`", readme);
        Assert.Contains("`ResearchSecondaryFamily` | `Unknown`", readme);
        Assert.Contains("Research starts from floor zero", strategySpec);
        Assert.Contains("Equipment starts from the highest unlocked native checkpoint", strategySpec);
        Assert.Contains("Configuration is rejected before a run mutation", strategySpec);
        Assert.Contains("reroll exactly once", strategySpec);
        Assert.Contains("At Code Capacity, Research does not replace", strategySpec);
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
