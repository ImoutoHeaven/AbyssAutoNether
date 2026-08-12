#nullable enable

using Xunit;

namespace AutoNether.Tests;

public sealed class AutoNetherPluginContractTests
{
    [Fact]
    public void Plugin_has_independent_identity_and_optional_AbyssMod_dependency()
    {
        string plugin = Read("AutoNether", "Core", "Plugin.cs");

        Assert.Contains("Abyss.AutoNether", plugin);
        Assert.Contains("Abyss AutoNether", plugin);
        Assert.Contains("0.1.0", plugin);
        Assert.Contains("BepInDependency", plugin);
        Assert.Contains("AbyssMod", plugin);
        Assert.Contains("DependencyFlags.SoftDependency", plugin);
        Assert.Contains("AppDomain.CurrentDomain", plugin);
        Assert.Contains("assembly.GetName().Name", plugin);
        Assert.Contains("final-task-capture", plugin);
        Assert.DoesNotContain("TranslationManager", plugin);
        Assert.DoesNotContain("MachineTranslator", plugin);
        Assert.DoesNotContain("Toast", plugin);
        Assert.DoesNotContain("HttpClient", plugin);
    }

    [Fact]
    public void Diagnostics_and_lease_use_AutoNether_identity_and_paths()
    {
        string diagnostics = Read("AutoNether", "Services", "NetherAutoClimbDiagnostics.cs");
        string controller = Read("AutoNether", "Services", "NetherAutoClimbController.cs");
        string lease = Read("AutoNether", "Services", "NetherBattleSettingsLease.cs");

        Assert.Contains("[F12][AutoNether][Diag]", diagnostics);
        Assert.Contains("[F12][AutoNether]", controller);
        Assert.Contains("Abyss.AutoNether", lease);
        Assert.Contains("battle-settings-lease.json", lease);
        Assert.DoesNotContain("[F12][NetherClimb]", diagnostics);
        Assert.DoesNotContain("[F12][NetherClimb]", controller);
    }

    [Fact]
    public void Hotkey_owns_only_F12_and_does_not_drive_F11_or_translation()
    {
        string hotkey = Read("AutoNether", "Core", "Hotkey.cs");

        Assert.Contains("KeyCode.F12", hotkey);
        Assert.DoesNotContain("KeyCode.F11", hotkey);
        Assert.DoesNotContain("KeyCode.F10", hotkey);
        Assert.DoesNotContain("KeyCode.F9", hotkey);
        Assert.DoesNotContain("KeyCode.F8", hotkey);
        Assert.DoesNotContain("BattleSessionAutoSL", hotkey);
        Assert.DoesNotContain("Translation", hotkey);
    }

    [Fact]
    public void Patch_manager_registers_only_AutoNether_lifecycle_patches()
    {
        string manager = Read("AutoNether", "Patches", "PatchManager.cs");

        Assert.Contains("NetherAutoClimbPatch", manager);
        Assert.Contains("NetherBattleStartTaskCapturePatch", manager);
        Assert.Contains("NetherBattleTerminalPatch", manager);
        Assert.DoesNotContain("TranslationPatch", manager);
        Assert.DoesNotContain("BattleSessionAutoSLPatch", manager);
        Assert.DoesNotContain("EnhancePatch", manager);
        Assert.DoesNotContain("ItemPatch", manager);
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
