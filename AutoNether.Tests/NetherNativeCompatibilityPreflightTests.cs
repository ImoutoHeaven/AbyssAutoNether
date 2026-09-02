#nullable enable

using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using AutoNether.Patches;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherNativeCompatibilityPreflightTests
{
    [Fact]
    public void Current_packaged_game_passes_every_required_native_binding_before_patching()
    {
        using var packaged = PackagedInteropAssemblies.Load();

        NetherNativeCompatibilityReport report =
            NetherNativeCompatibilityPreflight.Validate(packaged.Assemblies);

        Assert.True(report.IsCompatible, string.Join(Environment.NewLine, report.Failures));
        Assert.Equal(62, report.CheckedMethodCount);
        Assert.Equal(18, report.CheckedGeneratedMethodCount);
    }

    [Fact]
    public void Missing_game_assemblies_fail_closed_with_aggregate_evidence()
    {
        NetherNativeCompatibilityReport report =
            NetherNativeCompatibilityPreflight.Validate(Array.Empty<Assembly>());

        Assert.False(report.IsCompatible);
        Assert.True(report.Failures.Count > 10);
        Assert.Contains(report.Failures, failure => failure.Contains("missing-type", StringComparison.Ordinal));
    }

    [Fact]
    public void Patch_catalog_contains_every_Harmony_patch_class_exactly_once()
    {
        string patchSource = string.Join(
            Environment.NewLine,
            Directory.GetFiles(
                    Path.Combine(FindRepositoryRoot(), "AutoNether", "Patches"),
                    "*.cs"
                )
                .Select(File.ReadAllText)
        );
        string[] declared = Regex.Matches(
                patchSource,
                @"\[HarmonyPatch\]\s*(?:internal|public)\s+static\s+class\s+(\w+)"
            )
            .Select(match => match.Groups[1].Value)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        string[] cataloged = PatchManager.PatchTypes
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(declared, cataloged);
        Assert.Equal(cataloged.Length, cataloged.Distinct().Count());
    }

    [Fact]
    public void Plugin_runs_preflight_before_any_AutoNether_initialization_or_patch()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Core",
            "Plugin.cs"
        ));
        int gate = source.IndexOf("NetherNativeCompatibilityPreflight.Validate", StringComparison.Ordinal);

        Assert.True(gate >= 0, "missing native compatibility preflight");
        Assert.True(gate < source.IndexOf("AutoNether.Config.Initialize()", StringComparison.Ordinal));
        Assert.True(gate < source.IndexOf("AddComponent<Hotkey>()", StringComparison.Ordinal));
        Assert.True(gate < source.IndexOf("PatchManager.Initialize()", StringComparison.Ordinal));
        Assert.True(gate < source.IndexOf("NetherAutoClimbController.Initialize", StringComparison.Ordinal));
        Assert.Contains("throw new InvalidOperationException", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_bridge_cannot_introduce_an_uncataloged_reflection_method_descriptor()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Services",
            "NetherRuntimeBridge.cs"
        ));

        Assert.DoesNotContain("new NetherNativeMethodDescriptor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NetherNativeMethodDescriptor descriptor = new", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AutoNether.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("repository root not found");
    }

    private sealed class PackagedInteropAssemblies : IDisposable
    {
        private readonly AssemblyLoadContext _context;

        private PackagedInteropAssemblies(AssemblyLoadContext context)
        {
            _context = context;
        }

        public IReadOnlyList<Assembly> Assemblies => _context.Assemblies.ToArray();

        public static PackagedInteropAssemblies Load()
        {
            const string interopDirectory = "/game/BepInEx/interop";
            const string coreDirectory = "/game/BepInEx/core";
            Assert.True(Directory.Exists(interopDirectory), "game interop must be mounted at /game");

            var context = new AssemblyLoadContext("native-preflight-packaged", isCollectible: true);
            context.Resolving += (_, name) =>
            {
                string candidate = Path.Combine(interopDirectory, name.Name + ".dll");
                if (File.Exists(candidate))
                    return context.LoadFromAssemblyPath(candidate);
                candidate = Path.Combine(coreDirectory, name.Name + ".dll");
                return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
            };
            context.LoadFromAssemblyPath(Path.Combine(interopDirectory, "Project.dll"));
            context.LoadFromAssemblyPath(Path.Combine(interopDirectory, "Absf.dll"));
            return new PackagedInteropAssemblies(context);
        }

        public void Dispose() => _context.Unload();
    }
}
