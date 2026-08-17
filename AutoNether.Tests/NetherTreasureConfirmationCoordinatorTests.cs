#nullable enable

using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherTreasureConfirmationCoordinatorTests
{
    [Fact]
    public void Floor_46_waits_for_native_open_and_button_subscription_then_emits_one_native_button_tap()
    {
        // Fresh current-game recovery (GameAssembly SHA-256 573fa800...) proves the controller
        // awaits its Open sequence and then awaits SkipAndConfirmButton.OnTap. Its button owns
        // native skip behavior; the deployed direct Skip invocation faulted at LogOutput line 1768.
        var port = new FakePort();
        port.OpenObservations.Enqueue(
            NetherTreasureOpenAnimationObservation.Waiting(
                "awaiting-native-treasure-open-animation-task"
            )
        );
        port.OpenObservations.Enqueue(NetherTreasureOpenAnimationObservation.Ready());
        port.NativeButtonSubscriptionObservations.Enqueue(
            NetherTreasureOpenAnimationObservation.Ready()
        );
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
        Assert.Equal(0, port.DirectSkipInvocations);

        NetherNativeActionResult open = flow.Pump();
        Assert.Equal(NetherNativeActionResultKind.Started, open.Kind);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingNativeButtonSubscription, flow.Stage);
        Assert.Equal(2, port.OpenObservationCount);
        Assert.Equal(0, port.DirectSkipInvocations);
        Assert.Equal(0, port.ConfirmInvocations);

        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingResumePump, flow.Stage);
        Assert.Equal(0, port.ConfirmInvocations);
        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingConfirmTap, flow.Stage);
        Assert.Equal(0, port.ConfirmInvocations);
        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherTreasureConfirmStage.Completed, flow.Stage);
        Assert.Equal(0, port.DirectSkipInvocations);
        Assert.Equal(1, port.ConfirmInvocations);

        Assert.Equal(NetherNativeActionResultKind.Completed, flow.Pump().Kind);
        Assert.Equal(0, port.DirectSkipInvocations);
        Assert.Equal(1, port.ConfirmInvocations);
    }

    [Fact]
    public void Floor_46_treasure_never_reflectively_invokes_direct_skip_before_native_button_tap()
    {
        // Fresh recovery from the installed current game (GameAssembly SHA-256 573fa800...):
        // NetherTreasurePopup.SkipOpenTreasureAnimationAsync directly reads _animator, while
        // NetherTreasurePopupController binds SkipAndConfirmButton.OnTap to the native skip
        // behavior and awaits that same tap after PlayOpenTreasureAnimationSequenceAsync.
        // The deployed previous fix observed Open then called this direct method and the fresh
        // LogOutput.log captures its synchronous NullReferenceException at floor 46.
        var port = new FakePort();
        port.OpenObservations.Enqueue(NetherTreasureOpenAnimationObservation.Ready());
        var flow = new NetherTreasureConfirmationCoordinator(port);
        Assert.True(flow.Begin(Owner()));

        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);

        // Open readiness authorizes delayed native button taps; it never authorizes a direct
        // reflection call to SkipOpenTreasureAnimationAsync.
        Assert.Equal(0, port.DirectSkipInvocations);
        Assert.Equal(0, port.ConfirmInvocations);
    }

    [Fact]
    public void Floor_46_treasure_does_not_tap_until_native_button_subscription_is_observed()
    {
        // Fresh Cpp2IL recovery: UniRx.OnClickAsObservable adds a UnityEvent listener to the
        // button's m_OnClick, and the controller adds its awaited tap listener only after Open.
        // A fixed number of automation pumps is not evidence that this listener now exists.
        var port = new FakePort();
        port.OpenObservations.Enqueue(NetherTreasureOpenAnimationObservation.Ready());
        port.NativeButtonSubscriptionObservations.Enqueue(
            NetherTreasureOpenAnimationObservation.Waiting("awaiting-native-button-subscription")
        );
        port.NativeButtonSubscriptionObservations.Enqueue(
            NetherTreasureOpenAnimationObservation.Waiting("awaiting-native-button-subscription")
        );
        port.NativeButtonSubscriptionObservations.Enqueue(
            NetherTreasureOpenAnimationObservation.Waiting("awaiting-native-button-subscription")
        );
        var flow = new NetherTreasureConfirmationCoordinator(port);
        Assert.True(flow.Begin(Owner()));

        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);

        Assert.Equal(NetherTreasureConfirmStage.AwaitingNativeButtonSubscription, flow.Stage);
        Assert.Equal(3, port.NativeButtonSubscriptionObservationCount);
        Assert.Equal(0, port.ConfirmInvocations);
    }

    [Fact]
    public void Missing_native_button_subscription_is_bounded_and_never_emits_a_native_button_tap()
    {
        // Fresh UniRx recovery proves the expected listener is runtime-added after Open. If that
        // exact delta never appears, fail closed instead of guessing that a fixed frame delay won.
        var port = new FakePort();
        port.OpenObservations.Enqueue(NetherTreasureOpenAnimationObservation.Ready());
        port.NativeButtonSubscriptionObservations.Enqueue(
            NetherTreasureOpenAnimationObservation.Waiting("awaiting-native-button-subscription")
        );
        port.NativeButtonSubscriptionObservations.Enqueue(
            NetherTreasureOpenAnimationObservation.Waiting("awaiting-native-button-subscription")
        );
        var flow = new NetherTreasureConfirmationCoordinator(
            port,
            maximumOpenAnimationPolls: 1
        );
        Assert.True(flow.Begin(Owner()));

        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        NetherNativeActionResult timeout = flow.Pump();

        Assert.Equal(NetherNativeActionResultKind.BindingUnavailable, timeout.Kind);
        Assert.Equal("native-treasure-button-subscription-timeout", timeout.Detail);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingNativeButtonSubscription, flow.Stage);
        Assert.Equal(0, port.DirectSkipInvocations);
        Assert.Equal(0, port.ConfirmInvocations);
    }

    [Fact]
    public void Unreadable_native_button_subscription_fails_closed_without_a_tap()
    {
        var port = new FakePort();
        port.OpenObservations.Enqueue(NetherTreasureOpenAnimationObservation.Ready());
        port.NativeButtonSubscriptionObservations.Enqueue(
            NetherTreasureOpenAnimationObservation.BindingUnavailable(
                "treasure-native-button-runtime-listeners-unavailable"
            )
        );
        var flow = new NetherTreasureConfirmationCoordinator(port);
        Assert.True(flow.Begin(Owner()));

        Assert.Equal(NetherNativeActionResultKind.Started, flow.Pump().Kind);
        NetherNativeActionResult unavailable = flow.Pump();

        Assert.Equal(NetherNativeActionResultKind.BindingUnavailable, unavailable.Kind);
        Assert.Equal("treasure-native-button-runtime-listeners-unavailable", unavailable.Detail);
        Assert.Equal(NetherTreasureConfirmStage.AwaitingNativeButtonSubscription, flow.Stage);
        Assert.Equal(0, port.DirectSkipInvocations);
        Assert.Equal(0, port.ConfirmInvocations);
    }

    [Fact]
    public void Missing_open_animation_is_bounded_and_never_emits_a_native_button_tap()
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
        Assert.Equal(0, port.DirectSkipInvocations);
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
        public Queue<NetherTreasureOpenAnimationObservation> NativeButtonSubscriptionObservations { get; } = new();
        public int OpenObservationCount { get; private set; }
        public int NativeButtonSubscriptionObservationCount { get; private set; }
        public int DirectSkipInvocations { get; private set; }
        public int ConfirmInvocations { get; private set; }

        public NetherTreasureOpenAnimationObservation ObserveOpenAnimation(
            NetherTreasureConfirmOwner owner
        )
        {
            OpenObservationCount++;
            return OpenObservations.Dequeue();
        }

        // This member deliberately is not on INetherTreasureConfirmationPort. It is a regression
        // spy for the removed unsafe direct Skip reflection path.
        public NetherNativeActionResult InvokeSkip(NetherTreasureConfirmOwner owner)
        {
            DirectSkipInvocations++;
            return NetherNativeActionResult.Started("fake-direct-skip-started");
        }

        // Added to the production port by the subscription-readiness fix. Before that fix this
        // still compiles as an extra spy method, making the test red against the fixed-pump logic.
        public NetherTreasureOpenAnimationObservation ObserveNativeButtonSubscription(
            NetherTreasureConfirmOwner owner
        )
        {
            NativeButtonSubscriptionObservationCount++;
            return NativeButtonSubscriptionObservations.Dequeue();
        }

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
