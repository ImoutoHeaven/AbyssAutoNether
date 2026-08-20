#nullable enable

using System;
using System.Reflection;
using System.Reflection.Emit;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherFloorEventHintOrderingTests
{
    [Fact]
    public void Pending_code_keep_cancel_must_settle_before_a_floor_event_hint_is_claimed()
    {
        var bridge = new NetherRuntimeBridge();
        try
        {
            RegisterFloorSelection(bridge);
            NetherPlannedAction parent = new(NetherActionKind.SelectFloor);
            Assert.True(bridge.BeginFloorParent(parent, generation: 7));
            RegisterCodeOffer(bridge);

            var codeKeepTask = new MutableNativeTask { Status = "Pending" };
            BeginPendingCodeKeepCancel(bridge, codeKeepTask);
            RegisterFloorEventHint(bridge);

            NetherNativeActionResult whileCodeKeepIsPending = bridge.PollFloorParent();

            Assert.True(
                whileCodeKeepIsPending.Kind == NetherNativeActionResultKind.Started,
                whileCodeKeepIsPending.Kind + ":" + whileCodeKeepIsPending.Detail
            );
            Assert.Equal("code-keep-task-terminal-pending", whileCodeKeepIsPending.Detail);

            codeKeepTask.Status = "Succeeded";
            NetherNativeActionResult afterCodeKeepSettles = bridge.PollFloorParent();

            Assert.Equal(NetherNativeActionResultKind.BindingUnavailable, afterCodeKeepSettles.Kind);
            Assert.Contains("generated-holder:no-exact", afterCodeKeepSettles.Detail, StringComparison.Ordinal);
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    private static void BeginPendingCodeKeepCancel(
        NetherRuntimeBridge bridge,
        MutableNativeTask codeKeepTask
    )
    {
        var entry = new NetherOwnedPopupStageBridgeEntry((INetherOwnedPopupNativeStagePort)bridge);
        typeof(NetherOwnedPopupStageBridgeAdapter)
            .GetField("_entry", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(bridge, entry);

        NetherOwnedPopupNativeStageRuntime runtime = GetPrivateField<NetherOwnedPopupNativeStageRuntime>(
            entry,
            "_runtime"
        );
        NetherCodeKeepCancelCoordinator coordinator = GetPrivateField<NetherCodeKeepCancelCoordinator>(
            runtime,
            "_codeKeepCancel"
        );
        var owner = new NetherCodeKeepCancelOwner(
            NetherActionKind.SelectFloor,
            Generation: 7,
            Sequence: 1,
            DecisionEpoch: 0
        );
        Assert.True(coordinator.Begin(owner));
        Assert.True(coordinator.ObserveTask(owner));
        SetPrivateField(bridge, "_codeKeepCancelTask", codeKeepTask);
    }

    private static void RegisterFloorSelection(NetherRuntimeBridge bridge) =>
        GetPrivateMethod("RegisterFloorSelectionCore").Invoke(
            bridge,
            [new Project.Nether.FloorSelection.SubViewController(), "direct-registration"]
        );

    private static void RegisterCodeOffer(NetherRuntimeBridge bridge) =>
        GetPrivateMethod("RegisterPopupCore").Invoke(
            bridge,
            [Activator.CreateInstance(CodeOfferControllerType.Value)!, new object(), null]
        );

    private static void RegisterFloorEventHint(NetherRuntimeBridge bridge) =>
        GetPrivateMethod("RegisterPopupCore").Invoke(
            bridge,
            [Activator.CreateInstance(HintControllerType.Value)!, new object(), new object()]
        );

    private static T GetPrivateField<T>(object instance, string name) =>
        Assert.IsType<T>(instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance));

    private static void SetPrivateField(object instance, string name, object value) =>
        instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(instance, value);

    private static MethodInfo GetPrivateMethod(string name) =>
        typeof(NetherRuntimeBridge).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly Lazy<Type> CodeOfferControllerType = new(() =>
        CreateControllerType("Project.Nether.AbyssCodeSelectPopup.AbyssCodeSelectPopupController")
    );

    private static readonly Lazy<Type> HintControllerType = new(() =>
        CreateControllerType(
            "Project.Nether.NetherFloorEventHintBox.NetherFloorEventHintBoxPopupController"
        )
    );

    private static Type CreateControllerType(string fullName)
    {
        AssemblyName name = new("AutoNether.FloorEventHintOrderingFixture." + Guid.NewGuid().ToString("N"));
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule(name.Name!);
        TypeBuilder type = module.DefineType(fullName, TypeAttributes.Public | TypeAttributes.Class);
        type.DefineDefaultConstructor(MethodAttributes.Public);
        return type.CreateType()!;
    }

    private sealed class MutableNativeTask
    {
        public string Status { get; set; } = "Pending";
    }
}
