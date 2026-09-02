#nullable enable

using System;
using System.Reflection;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests
{
    [Collection("nether-controller-runtime")]
    public sealed class NetherErosionPointNotificationOrderingTests
    {
        [Fact]
        public void Owned_floor_parent_confirms_the_erosion_notification_through_its_native_callback_once()
        {
            var bridge = new NetherRuntimeBridge();
            object close = new Il2CppSystem.Action();
            ErosionNotificationCallbackProbe.Reset();

            try
            {
                RegisterFloorSelection(bridge);
                Assert.True(bridge.BeginFloorParent(
                    new NetherPlannedAction(NetherActionKind.SelectFloor),
                    generation: 7
                ));
                RegisterErosionNotification(bridge, close);

                NetherNativeActionResult first = bridge.PollFloorParent();
                NetherNativeActionResult second = bridge.PollFloorParent();

                Assert.Equal(NetherNativeActionResultKind.Started, first.Kind);
                Assert.Equal("native-erosion-point-notification-confirm", first.Detail);
                Assert.Equal(1, ErosionNotificationCallbackProbe.InvocationCount);
                Assert.Equal(NetherNativeActionResultKind.Started, second.Kind);
                Assert.NotEqual("native-erosion-point-notification-confirm", second.Detail);
                Assert.Equal(1, ErosionNotificationCallbackProbe.InvocationCount);
            }
            finally
            {
                bridge.ClearRegistrations();
            }
        }

        [Fact]
        public void Late_ownerless_erosion_notification_is_confirmed_only_for_paused_automation()
        {
            var bridge = new NetherRuntimeBridge();
            object close = new Il2CppSystem.Action();
            ErosionNotificationCallbackProbe.Reset();

            using IDisposable controllerScope =
                NetherAutoClimbController.PushRuntimeBridgeForTests(bridge);
            try
            {
                RegisterFloorSelection(bridge);
                NetherAutoClimbStateMachine state = GetControllerState();
                state.Toggle(isInNether: true);
                state.Pause(NetherPauseReason.NoSafeRoute, "route:no-safe-frontier");

                RegisterErosionNotification(bridge, close);

                Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
                Assert.Equal(1, ErosionNotificationCallbackProbe.InvocationCount);

                bridge.PollNativeFlow();
                state.Toggle(isInNether: true);
                RegisterErosionNotification(bridge, close);

                Assert.False(NetherAutoClimbController.IsEnabled);
                Assert.Equal(1, ErosionNotificationCallbackProbe.InvocationCount);
            }
            finally
            {
                bridge.ClearRegistrations();
            }
        }

        private static void RegisterFloorSelection(NetherRuntimeBridge bridge) =>
            GetPrivateMethod("RegisterFloorSelectionCore").Invoke(
                bridge,
                [new Project.Nether.FloorSelection.SubViewController(), "direct-registration"]
            );

        private static void RegisterErosionNotification(NetherRuntimeBridge bridge, object close) =>
            GetPrivateMethod("RegisterPopupCore").Invoke(
                bridge,
                [new Project.Nether.ErosionPointNotificationPopupController(), new object(), close]
            );

        private static MethodInfo GetPrivateMethod(string name) =>
            typeof(NetherRuntimeBridge).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static NetherAutoClimbStateMachine GetControllerState() =>
            (NetherAutoClimbStateMachine)typeof(NetherAutoClimbController)
                .GetField("State", BindingFlags.Static | BindingFlags.NonPublic)!
                .GetValue(null)!;
    }

    public static class ErosionNotificationCallbackProbe
    {
        public static int InvocationCount { get; private set; }

        public static void Reset() => InvocationCount = 0;

        public static void MarkInvoked() => InvocationCount++;
    }
}

namespace UniRx
{
    public readonly struct Unit
    {
    }
}

namespace Il2CppSystem
{
    public sealed class Action
    {
    }
}

// The production bridge classifies popups by the exact native full name.  This test-only
// managed fixture reproduces the current BepInEx-sanitized callback holder/signature without
// calling GameAssembly.  Fresh 2026-08-25 ISIL proves that b__4_0 receives
// (Unit, Action, controller), conditionally persists only the checked preference, then closes.
namespace Project.Nether
{
    public sealed class ErosionPointNotificationPopupController
    {
        public sealed class __c
        {
            public static __c __9 { get; } = new();

            public void _SetupPopupEvent_b__4_0(
                UniRx.Unit _,
                Il2CppSystem.Action action,
                ErosionPointNotificationPopupController self
            ) => AutoNether.Tests.ErosionNotificationCallbackProbe.MarkInvoked();
        }
    }
}
