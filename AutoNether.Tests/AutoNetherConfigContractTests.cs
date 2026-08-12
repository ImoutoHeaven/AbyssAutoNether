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
