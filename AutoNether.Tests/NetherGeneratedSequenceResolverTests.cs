#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherGeneratedSequenceResolverTests
{
    [Fact]
    public void Packaged_sequences_resolve_by_source_local_function_name_and_state_machine_shape()
    {
        if (!Directory.Exists("/game/BepInEx/interop"))
            return; // Hosted checkouts have no packaged interop; the container suite owns this.

        using var packaged = PackagedProjectAssembly.Load();
        Type utility = packaged.RequireType(NetherGeneratedSequenceResolver.UtilityTypeName);

        Assert.True(
            NetherGeneratedSequenceResolver.TryResolveAll(utility, out IReadOnlyList<NetherGeneratedSequenceTarget> targets, out IReadOnlyList<string> failures),
            string.Join(Environment.NewLine, failures)
        );

        Assert.Equal(3, targets.Count);
        Assert.Equal(
            new[]
            {
                NetherGeneratedSequenceFamily.Transform,
                NetherGeneratedSequenceFamily.Confirm,
                NetherGeneratedSequenceFamily.Cancel,
            },
            targets.Select(target => target.Contract.Family).ToArray()
        );

        foreach (NetherGeneratedSequenceTarget target in targets)
        {
            Assert.Equal("MoveNext", target.MoveNext.Name);
            Assert.Equal(typeof(void), target.MoveNext.ReturnType);
            Assert.Empty(target.MoveNext.GetParameters());
            Assert.True(target.MoveNext.DeclaringType!.IsNested);
            Assert.Equal(utility, target.MoveNext.DeclaringType!.DeclaringType);
            Assert.Contains(
                target.Contract.SourceMarker,
                NetherGeneratedSequenceResolver.OriginalName(target.StateMachineType) ?? string.Empty,
                StringComparison.Ordinal
            );
            Assert.Equal(
                "Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder",
                target.StateMachineType
                    .GetProperty(NetherGeneratedSequenceResolver.BuilderFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                    .PropertyType.FullName
            );
            Assert.Equal(
                typeof(int),
                target.StateMachineType
                    .GetProperty(NetherGeneratedSequenceResolver.StateFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                    .PropertyType
            );
            Assert.True(target.Factory.IsStatic);
            Assert.Equal("Cysharp.Threading.Tasks.UniTask", target.Factory.ReturnType.FullName);
        }
    }

    [Fact]
    public void Missing_utility_type_fails_closed_per_family_with_the_source_marker()
    {
        Assert.False(
            NetherGeneratedSequenceResolver.TryResolve(
                null,
                NetherGeneratedSequenceResolver.For(NetherGeneratedSequenceFamily.Transform),
                out _,
                out string error
            )
        );
        Assert.Contains("binding-unavailable:transform:missing-type", error, StringComparison.Ordinal);
        Assert.Contains(NetherGeneratedSequenceResolver.UtilityTypeName, error, StringComparison.Ordinal);
    }

    [Fact]
    public void Utility_type_without_the_generated_source_name_fails_closed_precisely()
    {
        Assert.False(
            NetherGeneratedSequenceResolver.TryResolve(
                typeof(string),
                NetherGeneratedSequenceResolver.For(NetherGeneratedSequenceFamily.Confirm),
                out _,
                out string error
            )
        );
        Assert.Equal(
            "binding-unavailable:confirm:state-machine:no-source-method:>g__HandleConfirmSequenceAsync|",
            error
        );
    }

    [Fact]
    public void State_machine_shape_rejects_a_nested_type_whose_move_next_is_missing()
    {
        Type stateMachine = typeof(NetherGeneratedSequenceResolverTests).GetNestedTypes(
            BindingFlags.Public | BindingFlags.NonPublic
        ).Single(type => type.Name == nameof(FakeStateMachineWithoutMoveNext));

        Assert.False(
            NetherGeneratedSequenceResolver.TryResolveMoveNext(
                stateMachine,
                NetherGeneratedSequenceResolver.For(NetherGeneratedSequenceFamily.Transform),
                out _,
                out string error
            )
        );
        Assert.Equal(
            "binding-unavailable:transform:move-next:missing:" + stateMachine.Name,
            error
        );
    }

    private sealed class FakeStateMachineWithoutMoveNext
    {
        public int __1__state { get; set; }
    }

    private sealed class PackagedProjectAssembly : IDisposable
    {
        private readonly AssemblyLoadContext _context;

        private PackagedProjectAssembly(AssemblyLoadContext context) => _context = context;

        public Assembly Assembly => _context.Assemblies.Single(assembly => assembly.GetName().Name == "Project");

        public Type RequireType(string name) =>
            Assembly.GetType(name, throwOnError: true)!;

        public static PackagedProjectAssembly Load()
        {
            const string interopDirectory = "/game/BepInEx/interop";
            const string coreDirectory = "/game/BepInEx/core";

            var context = new AssemblyLoadContext("generated-sequence", isCollectible: true);
            context.Resolving += (_, name) =>
            {
                string candidate = Path.Combine(interopDirectory, name.Name + ".dll");
                if (File.Exists(candidate))
                    return context.LoadFromAssemblyPath(candidate);
                candidate = Path.Combine(coreDirectory, name.Name + ".dll");
                return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
            };
            context.LoadFromAssemblyPath(Path.Combine(interopDirectory, "Project.dll"));
            return new PackagedProjectAssembly(context);
        }

        public void Dispose() => _context.Unload();
    }
}
