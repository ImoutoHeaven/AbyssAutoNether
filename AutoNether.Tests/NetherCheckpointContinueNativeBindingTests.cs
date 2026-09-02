#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherCheckpointContinueNativeBindingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Exact_continue_callback_enters_skip_decision_for_both_canBoost_branches(bool canBoost)
    {
        NetherNativeMethodDescriptor callback = NetherCheckpointContinueNativeBinding.ContinueCallback;
        var flow = new NetherCheckpointNativeFlow();
        Assert.True(flow.Begin(new NetherPlannedAction(NetherActionKind.Continue)));

        Assert.Equal("<SetupPopupEvent>b__10_2", callback.Name);
        Assert.Equal("_SetupPopupEvent_b__10_2", NetherCheckpointContinueNativeBinding.ContinueCallbackInterop.ManagedName);
        Assert.Equal("<SetupPopupEvent>b__10_2", NetherCheckpointContinueNativeBinding.ContinueCallbackInterop.ObfuscatedName);
        Assert.True(NetherCheckpointContinueNativeBinding.SubmitContinue(flow, canBoost));
        Assert.Equal(NetherCheckpointNativeStage.AwaitingSkipDecision, flow.Stage);
    }

    [Fact]
    public void Skip_decline_uses_the_exact_native_do_not_execute_callback()
    {
        NetherCodePopupInteropMethodBinding callback =
            NetherCheckpointContinueNativeBinding.SkipDeclineCallbackInterop;

        Assert.Equal("_SetupPopupEvent_b__7_0", callback.ManagedName);
        Assert.Equal("<SetupPopupEvent>b__7_0", callback.ObfuscatedName);
        Assert.Equal(new[] { "UniRx.Unit", "Il2CppSystem.Action" }, callback.ParameterTypeNames);
    }

    [Fact]
    public void Boost_confirmation_uses_the_exact_one_ticket_count_before_its_confirm_callback()
    {
        Assert.Equal(1, NetherCheckpointContinueNativeBinding.ExactTicketCount);
        Assert.Equal("<SetupPopupEvent>b__7_2", NetherCheckpointContinueNativeBinding.BoostSetCount.Name);
        Assert.Equal("<SetupPopupEvent>b__7_1", NetherCheckpointContinueNativeBinding.BoostConfirm.Name);
    }
}
