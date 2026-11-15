#nullable enable

using System;
using System.Reflection;
using System.Reflection.Emit;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherTreasureConfirmLeaseTests
{
    [Fact]
    public void Exact_treasure_owner_requires_skip_completion_and_a_resume_pump_before_confirm_tap()
    {
        var popup = new object();
        var lease = new NetherTreasureConfirmLease();
        var owner = new NetherTreasureConfirmOwner(
            popup,
            NetherActionKind.SelectFloor,
            OwnerGeneration: 78,
            RuntimeGeneration: 12,
            Sequence: 138
        );

        Assert.True(lease.Begin(owner));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingSkipInvocation, lease.Stage);
        Assert.True(lease.TryClaimSkip(owner));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingSkipTask, lease.Stage);
        Assert.False(lease.TryClaimConfirm(owner));

        Assert.True(lease.ObserveSkipTaskCompleted(owner));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingResumePump, lease.Stage);
        Assert.False(lease.TryClaimConfirm(owner));

        Assert.True(lease.AdvanceResumePump(owner));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingConfirmTap, lease.Stage);
        Assert.True(lease.TryClaimConfirm(owner));
        Assert.Equal(NetherTreasureConfirmStage.Completed, lease.Stage);
        Assert.False(lease.TryClaimConfirm(owner));
    }

    [Fact]
    public void Stale_popup_runtime_owner_or_sequence_cannot_advance_the_treasure_flow()
    {
        var popup = new object();
        var lease = new NetherTreasureConfirmLease();
        var owner = new NetherTreasureConfirmOwner(
            popup,
            NetherActionKind.None,
            OwnerGeneration: 0,
            RuntimeGeneration: 9,
            Sequence: 41
        );
        Assert.True(lease.Begin(owner));

        Assert.False(lease.TryClaimSkip(owner with { Popup = new object() }));
        Assert.False(lease.TryClaimSkip(owner with { RuntimeGeneration = 10 }));
        Assert.False(lease.TryClaimSkip(owner with { Sequence = 42 }));
        Assert.False(lease.TryClaimSkip(owner with
        {
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = 1,
        }));

        Assert.Equal(NetherTreasureConfirmStage.AwaitingSkipInvocation, lease.Stage);
        Assert.True(lease.TryClaimSkip(owner));
    }

    [Fact]
    public void Only_the_exact_popup_invalidation_resets_the_one_shot_flow()
    {
        var popup = new object();
        var lease = new NetherTreasureConfirmLease();
        var owner = new NetherTreasureConfirmOwner(
            popup,
            NetherActionKind.SelectFloor,
            OwnerGeneration: 5,
            RuntimeGeneration: 7,
            Sequence: 11
        );
        Assert.True(lease.Begin(owner));

        Assert.False(lease.InvalidatePopup(new object()));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingSkipInvocation, lease.Stage);
        Assert.True(lease.InvalidatePopup(popup));
        Assert.Equal(NetherTreasureConfirmStage.Idle, lease.Stage);
        Assert.False(lease.TryGetOwner(out _));
    }

    [Theory]
    [InlineData((int)NetherActionKind.None, 1)]
    [InlineData((int)NetherActionKind.SelectFloor, 0)]
    [InlineData((int)NetherActionKind.SelectEventOption, 1)]
    public void Invalid_owner_shapes_fail_closed(int rawOwnerAction, long ownerGeneration)
    {
        var lease = new NetherTreasureConfirmLease();
        Assert.False(lease.Begin(new NetherTreasureConfirmOwner(
            new object(),
            (NetherActionKind)rawOwnerAction,
            ownerGeneration,
            RuntimeGeneration: 1,
            Sequence: 1
        )));
        Assert.Equal(NetherTreasureConfirmStage.Idle, lease.Stage);
    }

    [Fact]
    public void Confirm_tap_succeeds_when_popup_close_hook_synchronously_invalidates_treasure_lease()
    {
        var bridge = new NetherRuntimeBridge();
        try
        {
            InitializeOwnedFloorParent(bridge);
            var parentTask = new MutableNativeTask { Status = "Pending" };
            ObserveFloorEventSequenceTask(bridge, parentTask);

            object popup = CreateSyntheticTreasurePopup(onSubmit: p =>
            {
                ((SyntheticTreasurePopup)p).IsClosing = true;
                ObservePopupClosed(bridge, p);
            });
            RegisterTreasurePopup(bridge, popup, ownerAction: NetherActionKind.SelectFloor, ownerGeneration: 7);

            NetherTreasureConfirmLease lease = GetPrivateField<NetherTreasureConfirmLease>(bridge, "_treasureConfirmLease");
            var owner = new NetherTreasureConfirmOwner(
                popup,
                NetherActionKind.SelectFloor,
                OwnerGeneration: 7,
                RuntimeGeneration: GetPrivateField<long>(bridge, "_runtimeGeneration"),
                Sequence: GetPrivateField<long>(bridge, "_popupSequence")
            );
            Assert.True(lease.Begin(owner));
            Assert.True(lease.TryClaimSkip(owner));
            Assert.True(lease.ObserveSkipTaskCompleted(owner));
            Assert.True(lease.AdvanceResumePump(owner));

            NetherNativeActionResult tap = bridge.PollFloorParent();
            Assert.Equal(NetherNativeActionResultKind.Started, tap.Kind);
            Assert.Equal("native-treasure-confirm-tap-invoked", tap.Detail);
            Assert.Equal(NetherTreasureConfirmStage.Idle, lease.Stage);

            parentTask.Status = "Succeeded";
            NetherNativeActionResult completed = bridge.PollFloorParent();
            Assert.Equal(NetherNativeActionResultKind.Completed, completed.Kind);
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    [Fact]
    public void Recovered_treasure_confirm_failure_resets_recovered_floor_event_flow()
    {
        var bridge = new NetherRuntimeBridge();
        try
        {
            RegisterFloorSelection(bridge);
            object popup = new object();
            RegisterTreasurePopup(bridge, popup, ownerAction: NetherActionKind.None, ownerGeneration: 0);

            NetherFloorEventSequenceTaskFlow recoveredFlow = GetPrivateField<NetherFloorEventSequenceTaskFlow>(
                bridge,
                "_recoveredFloorEventSequenceTaskFlow"
            );
            Assert.True(recoveredFlow.BeginRecovered(new MutableNativeTask { Status = "Pending" }));

            NetherTreasureConfirmLease lease = GetPrivateField<NetherTreasureConfirmLease>(bridge, "_treasureConfirmLease");
            var owner = new NetherTreasureConfirmOwner(
                popup,
                NetherActionKind.None,
                OwnerGeneration: 0,
                RuntimeGeneration: GetPrivateField<long>(bridge, "_runtimeGeneration"),
                Sequence: GetPrivateField<long>(bridge, "_popupSequence")
            );
            Assert.True(lease.Begin(owner));

            NetherNativeActionResult failed = bridge.PollNativeFlow();
            Assert.Equal(NetherNativeActionResultKind.BindingUnavailable, failed.Kind);
            Assert.Equal("missing-treasure-popup-animator", failed.Detail);
            Assert.False(recoveredFlow.IsActive);
            Assert.Equal(NetherTreasureConfirmStage.Idle, lease.Stage);

            NetherNativeActionResult idle = bridge.PollNativeFlow();
            Assert.Equal(NetherNativeActionResultKind.Completed, idle.Kind);
            Assert.Equal("no-pending-native-flow", idle.Detail);
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    private static void InitializeOwnedFloorParent(NetherRuntimeBridge bridge)
    {
        RegisterFloorSelection(bridge);
        Assert.True(bridge.BeginFloorParent(new NetherPlannedAction(NetherActionKind.SelectFloor), generation: 7));
    }

    private static void RegisterFloorSelection(NetherRuntimeBridge bridge) =>
        GetBridgeMethod("RegisterFloorSelectionCore").Invoke(
            bridge,
            [new Project.Nether.FloorSelection.SubViewController(), "direct-registration"]
        );

    private static void ObserveFloorEventSequenceTask(NetherRuntimeBridge bridge, object task)
    {
        NetherFloorEventSequenceTaskFlow flow = GetPrivateField<NetherFloorEventSequenceTaskFlow>(
            bridge,
            "_floorEventSequenceTaskFlow"
        );
        Assert.True(flow.ObserveClickTask(new MutableNativeTask { Status = "Succeeded" }));
        Assert.True(flow.ObserveEventSequenceTask(task));
    }

    private static void RegisterTreasurePopup(
        NetherRuntimeBridge bridge,
        object popup,
        NetherActionKind ownerAction,
        long ownerGeneration
    )
    {
        SetPrivateField(bridge, "_popupSequence", 10L);
        Type registrationType = typeof(NetherRuntimeBridge).GetNestedType(
            "PopupRegistration",
            BindingFlags.NonPublic
        )!;
        object registration = Activator.CreateInstance(
            registrationType,
            [
                new object(),
                popup,
                null,
                10L,
                ownerAction,
                ownerGeneration,
                GetPrivateField<long>(bridge, "_runtimeGeneration"),
                true,
            ]
        )!;
        SetPrivateField(bridge, "_treasurePopup", registration);
    }

    private static void ObservePopupClosed(NetherRuntimeBridge bridge, object popup) =>
        GetBridgeMethod("InvalidatePopupCore").Invoke(bridge, [popup]);

    private static object CreateSyntheticTreasurePopup(Action<object> onSubmit)
    {
        object button = SyntheticSubmitButtonFactory.Create();
        var popup = new SyntheticTreasurePopup(button);
        SyntheticSubmitButtonFactory.Bind(button, () => onSubmit(popup));
        return popup;
    }

    private static MethodInfo GetBridgeMethod(string name) =>
        typeof(NetherRuntimeBridge).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static T GetPrivateField<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    private static void SetPrivateField(object target, string name, object? value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private sealed class MutableNativeTask
    {
        public string Status { get; set; } = "Pending";
    }

    private sealed class SyntheticTreasurePopup
    {
        public SyntheticTreasurePopup(object button)
        {
            SkipAndConfirmButton = button;
        }

        public bool IsClosing { get; set; }
        public object SkipAndConfirmButton { get; }
    }

    private static class SyntheticSubmitButtonFactory
    {
        private static readonly Type ButtonType = BuildButtonType();

        public static object Create() => Activator.CreateInstance(ButtonType)!;

        public static void Bind(object button, Action onSubmit) =>
            ButtonType.GetField("Callback")!.SetValue(button, onSubmit);

        private static Type BuildButtonType()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("AutoNetherTreasureButtonFixtures"),
                AssemblyBuilderAccess.Run
            );
            ModuleBuilder module = assembly.DefineDynamicModule("Main");
            TypeBuilder baseEventData = module.DefineType(
                "UnityEngine.EventSystems.BaseEventData",
                TypeAttributes.Public | TypeAttributes.Class
            );
            Type baseEventDataType = baseEventData.CreateType()!;

            TypeBuilder button = module.DefineType(
                "Project.AppButton",
                TypeAttributes.Public | TypeAttributes.Class
            );
            FieldBuilder callback = button.DefineField(
                "Callback",
                typeof(Action),
                FieldAttributes.Public
            );
            MethodBuilder onSubmit = button.DefineMethod(
                "OnSubmit",
                MethodAttributes.Public,
                typeof(void),
                [baseEventDataType]
            );
            ILGenerator il = onSubmit.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, callback);
            il.Emit(OpCodes.Callvirt, typeof(Action).GetMethod(nameof(Action.Invoke))!);
            il.Emit(OpCodes.Ret);
            return button.CreateType()!;
        }
    }
}
