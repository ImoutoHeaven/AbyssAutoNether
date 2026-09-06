#nullable enable

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using AutoNether.Patches;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherNativeCompatibilityPreflightTests
{
    [Fact]
    public void Missing_generic_task_status_rejects_load_while_non_generic_tasks_still_resolve()
    {
        using var packaged = PackagedInteropAssemblies.Load(renameGenericTaskStatus: true);
        Assembly tasks = packaged.Assemblies.Single(assembly => assembly.GetName().Name == "UniTask");
        Assert.NotNull(tasks.GetType("Cysharp.Threading.Tasks.UniTask")!.GetProperty("Status"));
        Assert.Null(tasks.GetType("Cysharp.Threading.Tasks.UniTask`1")!.GetProperty("Status"));

        NetherNativeCompatibilityReport report =
            NetherNativeCompatibilityPreflight.ValidateMetadata(packaged.Assemblies, packaged.PluginAssembly);

        Assert.False(report.IsCompatible);
        Assert.Contains(report.Failures, failure => failure.Contains("Status", StringComparison.Ordinal));
    }

    [Fact]
    public void Renamed_direct_game_call_rejects_load_before_its_runtime_path_is_jitted()
    {
        using var packaged = PackagedInteropAssemblies.Load(renamedMethod: "CalculateUnitParametersMap");

        NetherNativeCompatibilityReport report =
            NetherNativeCompatibilityPreflight.ValidateMetadata(packaged.Assemblies, packaged.PluginAssembly);

        Assert.False(report.IsCompatible);
        Assert.Contains(report.Failures, failure => failure.Contains("CalculateUnitParametersMap", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, "generated-singleton:null")]
    [InlineData(true, "generated-singleton-read:NullReferenceException")]
    public void Unavailable_generated_singleton_rejects_load_despite_valid_callback_metadata(
        bool throws,
        string expectedError
    )
    {
        using var packaged = PackagedInteropAssemblies.Load(singletonThrows: throws);

        NetherNativeCompatibilityReport report =
            NetherNativeCompatibilityPreflight.Validate(packaged.Assemblies);

        Assert.False(report.IsCompatible);
        Assert.True(report.Failures.Any(failure =>
            failure.Contains("AbyssCodeSelectPopupController", StringComparison.Ordinal)
            && failure.Contains(expectedError, StringComparison.Ordinal)), string.Join(Environment.NewLine, report.Failures));
    }

    [Theory]
    [InlineData("_partyModel")]
    [InlineData("_cancellationToken")]
    [InlineData("__4__this")]
    [InlineData("__t__builder")]
    [InlineData("__1__state")]
    [InlineData("_netherModel")]
    [InlineData("_mIds")]
    [InlineData("_model")]
    [InlineData("_onCompleted")]
    [InlineData("_tabGroupView")]
    [InlineData("_contentModelList")]
    [InlineData("_mNetherEventPartsArray")]
    [InlineData("_mNetherFloorShopContentsArray")]
    [InlineData("MNetherId")]
    [InlineData("CharacterModels")]
    [InlineData("CharacterAbilityEffectModels")]
    [InlineData("EquipmentAbilityEffectModels")]
    [InlineData("NetherCodeCategoryType")]
    public void Renamed_required_member_rejects_load_even_when_patch_methods_are_unchanged(
        string memberName
    )
    {
        using var packaged = PackagedInteropAssemblies.Load(memberName);

        NetherNativeCompatibilityReport report =
            NetherNativeCompatibilityPreflight.ValidateMetadata(packaged.Assemblies, packaged.PluginAssembly);

        Assert.False(report.IsCompatible);
        Assert.Contains(report.Failures, failure => failure.Contains(memberName, StringComparison.Ordinal));
    }

    [Fact]
    public void Current_packaged_game_passes_required_metadata_contracts_before_patching()
    {
        using var packaged = PackagedInteropAssemblies.Load();

        NetherNativeCompatibilityReport report =
            NetherNativeCompatibilityPreflight.ValidateMetadata(packaged.Assemblies, packaged.PluginAssembly);

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
        public Assembly PluginAssembly => _context.Assemblies.Single(assembly => assembly.GetName().Name == "AutoNether");

        public static PackagedInteropAssemblies Load(
            string? renamedMember = null,
            bool? singletonThrows = null,
            string? renamedMethod = null,
            bool renameGenericTaskStatus = false
        )
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
            if (renamedMember == null && singletonThrows == null && renamedMethod == null && !renameGenericTaskStatus)
                context.LoadFromAssemblyPath(Path.Combine(interopDirectory, "Project.dll"));
            else
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(interopDirectory, renameGenericTaskStatus ? "UniTask.dll" : "Project.dll"));
                using var pe = new PEReader(new MemoryStream(bytes, writable: false));
                MetadataReader metadata = pe.GetMetadataReader();
                if (renamedMember != null)
                {
                    StringHandle[] names = metadata.PropertyDefinitions
                        .Select(metadata.GetPropertyDefinition)
                        .Where(property => metadata.GetString(property.Name) == renamedMember)
                        .SelectMany(property => new[]
                        {
                            property.Name,
                            metadata.GetMethodDefinition(property.GetAccessors().Getter).Name,
                        })
                        .Distinct()
                        .ToArray();
                    Assert.NotEmpty(names);
                    int heap = pe.PEHeaders.MetadataStartOffset + metadata.GetHeapMetadataOffset(HeapIndex.String);
                    // Preserve PE layout and method bodies; only the interop member identities drift.
                    foreach (StringHandle name in names)
                        bytes[heap + MetadataTokens.GetHeapOffset(name)] = (byte)'X';
                }
                if (singletonThrows.HasValue)
                {
                    TypeDefinition controller = metadata.TypeDefinitions
                        .Select(metadata.GetTypeDefinition)
                        .Single(type => metadata.GetString(type.Namespace) == "Project.Nether.AbyssCodeSelectPopup"
                            && metadata.GetString(type.Name) == "AbyssCodeSelectPopupController");
                    TypeDefinition holder = controller.GetNestedTypes().Select(metadata.GetTypeDefinition)
                        .Single(type => metadata.GetString(type.Name) == "__c");
                    MethodDefinition[] methods = holder.GetMethods().Select(metadata.GetMethodDefinition).ToArray();
                    // These isolated interop fixtures can exercise the live gate without a native process.
                    StubMethod(methods.Single(method => metadata.GetString(method.Name) == ".cctor"), new byte[] { 0x2a });
                    StubMethod(methods.Single(method => metadata.GetString(method.Name) == "get___9"),
                        new byte[] { 0x14, singletonThrows.Value ? (byte)0x7a : (byte)0x2a });
                }
                if (renamedMethod != null)
                {
                    StringHandle[] names = metadata.MethodDefinitions.Select(metadata.GetMethodDefinition)
                        .Where(method => metadata.GetString(method.Name) == renamedMethod)
                        .Select(method => method.Name).Distinct().ToArray();
                    Assert.NotEmpty(names);
                    int heap = pe.PEHeaders.MetadataStartOffset + metadata.GetHeapMetadataOffset(HeapIndex.String);
                    foreach (StringHandle name in names)
                        bytes[heap + MetadataTokens.GetHeapOffset(name)] = (byte)'X';
                }
                if (renameGenericTaskStatus)
                {
                    TypeDefinition task = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
                        .Single(type => metadata.GetString(type.Namespace) == "Cysharp.Threading.Tasks"
                            && metadata.GetString(type.Name) == "UniTask`1");
                    PropertyDefinitionHandle status = task.GetProperties().Single(handle =>
                        metadata.GetString(metadata.GetPropertyDefinition(handle).Name) == "Status");
                    MethodDefinitionHandle getter = metadata.GetPropertyDefinition(status).GetAccessors().Getter;
                    StringHandle replacement = metadata.MethodDefinitions.Select(metadata.GetMethodDefinition)
                        .Select(method => method.Name).First(name => metadata.GetString(name) == "ToString");
                    // Name heap entries can be shared by generic and non-generic tasks. Redirect only
                    // these two rows, leaving the non-generic Status contract and all IL intact.
                    RenameRow(TableIndex.Property, MetadataTokens.GetRowNumber(status), 2, replacement);
                    RenameRow(TableIndex.MethodDef, MetadataTokens.GetRowNumber(getter), 8, replacement);
                }
                using var stream = new MemoryStream(bytes, writable: false);
                context.LoadFromStream(stream);
                if (renameGenericTaskStatus)
                    context.LoadFromAssemblyPath(Path.Combine(interopDirectory, "Project.dll"));

                void RenameRow(TableIndex table, int row, int nameOffset, StringHandle name)
                {
                    int offset = pe.PEHeaders.MetadataStartOffset + metadata.GetTableMetadataOffset(table)
                        + (row - 1) * metadata.GetTableRowSize(table) + nameOffset;
                    int index = MetadataTokens.GetHeapOffset(name);
                    byte[] encoded = metadata.GetHeapSize(HeapIndex.String) > ushort.MaxValue
                        ? BitConverter.GetBytes(index)
                        : BitConverter.GetBytes(checked((ushort)index));
                    encoded.CopyTo(bytes, offset);
                }

                void StubMethod(MethodDefinition method, byte[] code)
                {
                    int rva = method.RelativeVirtualAddress;
                    SectionHeader section = pe.PEHeaders.SectionHeaders.Single(section =>
                        rva >= section.VirtualAddress && rva < section.VirtualAddress + section.VirtualSize);
                    int header = section.PointerToRawData + rva - section.VirtualAddress;
                    int body = header + ((bytes[header] & 3) == 2 ? 1 : (bytes[header + 1] >> 4) * 4);
                    int length = pe.GetMethodBody(rva).GetILBytes()!.Length;
                    Assert.True(length >= code.Length);
                    Array.Clear(bytes, body, length);
                    code.CopyTo(bytes, body + length - code.Length);
                }
            }
            context.LoadFromAssemblyPath(Path.Combine(interopDirectory, "Absf.dll"));
            context.LoadFromAssemblyPath(typeof(Plugin).Assembly.Location);
            return new PackagedInteropAssemblies(context);
        }

        public void Dispose() => _context.Unload();
    }
}
