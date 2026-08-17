#nullable enable

using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherTreasureConfirmationCoordinatorTests
{
    [Fact]
    public void Floor_26_waits_for_native_open_then_invokes_skip_and_confirm_exactly_once()
    {
        // Fresh GameAssembly SHA-256 573fa800...: HandleEventConfirmedAsync awaits the
        // server update before it starts PlayOpenTreasureAnimationSequenceAsync. The captured
        // line-2402 NRE came from invoking Skip during this pre-Open gap.
        var port = new FakePort();
        port.OpenObservations.Enqueue(
            NetherTreasureOpenAnimationObservation.Waiting(
                "awaiting-native-treasure-open-animation-task"
            )
        );
        port.OpenObservations.Enqueue(NetherTreasureOpenAnimationObservation.Ready());
        var flow = new NetherTreasureConfirmationCoordinator(
            port,
            maximumOpenAnimationPolls: 2
        );
        NetherTreasureConfirmOwner owner = Owner();
        Assert.True(flow.Begin(owner));

        NetherNativeActionResult waiting = flow.Pump();
        Assert.Equal(NetherNativeActionResultKind.Started, waiting.Kind);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingOpenAnimation, flow.Stage);
        Assert.Equal(1, port.OpenObservationCount);
        Assert.Equal(0, port.SkipInvocations);

        NetherNativeActionResult skip = flow.Pump();
        Assert.Equal(NetherNativeActionResultKind.Started, skip.Kind);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingSkipTask, flow.Stage);
        Assert.Equal(2, port.OpenObservationCount);
        Assert.Equal(1, port.SkipInvocations);

        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingResumePump, flow.Stage);
        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingConfirmTap, flow.Stage);
        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherTreasureConfirmStage.Completed, flow.Stage);
        Assert.Equal(1, port.SkipInvocations);
        Assert.Equal(1, port.ConfirmInvocations);

        Assert.Equal(NetherNativeActionResultKind.Completed, flow.Pump().Kind);
        Assert.Equal(1, port.SkipInvocations);
        Assert.Equal(1, port.ConfirmInvocations);
    }

    [Fact]
    public void Missing_open_animation_is_bounded_and_never_invokes_skip()
    {
        var port = new FakePort();
        port.OpenObservations.Enqueue(NetherTreasureOpenAnimationObservation.Waiting("not-open"));
        port.OpenObservations.Enqueue(NetherTreasureOpenAnimationObservation.Waiting("not-open"));
        var flow = new NetherTreasureConfirmationCoordinator(
            port,
            maximumOpenAnimationPolls: 1
        );
        Assert.True(flow.Begin(Owner()));

        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        NetherNativeActionResult timeout = flow.Pump();

        Assert.Equal(NetherNativeActionResultKind.BindingUnavailable, timeout.Kind);
        Assert.Equal("native-treasure-open-animation-task-timeout", timeout.Detail);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingOpenAnimation, flow.Stage);
        Assert.Equal(0, port.SkipInvocations);
        Assert.Equal(0, port.ConfirmInvocations);
    }

    private static NetherTreasureConfirmOwner Owner() => new(
        new object(),
        NetherActionKind.SelectFloor,
        OwnerGeneration: 26,
        RuntimeGeneration: 30,
        Sequence: 48
    );

    private sealed class FakePort : INetherTreasureConfirmationPort
    {
        public Queue<NetherTreasureOpenAnimationObservation> OpenObservations { get; } = new();
        public int OpenObservationCount { get; private set; }
        public int SkipInvocations { get; private set; }
        public int ConfirmInvocations { get; private set; }

        public NetherTreasureOpenAnimationObservation ObserveOpenAnimation(
            NetherTreasureConfirmOwner owner
        )
        {
            OpenObservationCount++;
            return OpenObservations.Dequeue();
        }

        public NetherNativeActionResult InvokeSkip(NetherTreasureConfirmOwner owner)
        {
            SkipInvocations++;
            return NetherNativeActionResult.Started("fake-skip-started");
        }

        public NetherNativeActionResult PollSkip(NetherTreasureConfirmOwner owner) =>
            NetherNativeActionResult.Completed("fake-skip-completed");

        public NetherNativeActionResult InvokeConfirm(NetherTreasureConfirmOwner owner)
        {
            ConfirmInvocations++;
            return NetherNativeActionResult.Started("fake-confirm-started");
        }

        public void LogStage(
            NetherTreasureConfirmOwner owner,
            string stage,
            string outcome
        )
        {
        }
    }
}
