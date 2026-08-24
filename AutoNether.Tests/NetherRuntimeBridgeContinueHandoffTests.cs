#nullable enable

using System.Reflection;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherRuntimeBridgeContinueHandoffTests
{
    [Fact]
    public void Exact_owner_terminal_fault_preserves_continue_handoff_in_production_bridge()
    {
        var bridge = new NetherRuntimeBridge();
        try
        {
            SetPrivateField(
                bridge,
                "_pendingCheckpointAction",
                (NetherPlannedAction?)new NetherPlannedAction(NetherActionKind.Continue)
            );
            NetherContinueSceneTransitionEvidence evidence = GetPrivateField<
                NetherContinueSceneTransitionEvidence
            >(bridge, "_continueSceneTransition");
            Assert.True(evidence.Begin(ownerGeneration: 10));
            evidence.ObserveFloorOwnerTerminated();

            NetherNativeActionResult result = InvokeTerminalCheckpointFailure(
                bridge,
                NetherNativeActionResult.UnknownOutcome(
                    "native-start-status-terminal-faulted"
                )
            );

            Assert.Equal(NetherNativeActionResultKind.Completed, result.Kind);
            Assert.Contains("after-owner-transition", result.Detail);
            Assert.True(bridge.FloorOwnerTerminated);
            Assert.False(evidence.NativeParentPending);
            Assert.False(evidence.IsSettledBySceneTransition);
            Assert.Null(GetPrivateField<NetherPlannedAction?>(
                bridge,
                "_pendingCheckpointAction"
            ));
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    private static NetherNativeActionResult InvokeTerminalCheckpointFailure(
        NetherRuntimeBridge bridge,
        NetherNativeActionResult result
    ) => Assert.IsType<NetherNativeActionResult>(
        typeof(NetherRuntimeBridge)
            .GetMethod(
                "TerminalCheckpointFailure",
                BindingFlags.Instance | BindingFlags.NonPublic
            )!
            .Invoke(bridge, [result])
    );

    private static T GetPrivateField<T>(object instance, string name) =>
        (T)instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!;

    private static void SetPrivateField(object instance, string name, object? value) =>
        instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(instance, value);
}
