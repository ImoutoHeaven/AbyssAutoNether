using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherRuntimeFlowCoordinatorTests
{
    [Fact]
    public void Floor_parent_stays_pending_while_its_owned_event_modal_is_driven()
    {
        var driver = new FakeDriver();
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        var floor = new NetherPlannedAction(NetherActionKind.SelectFloor) { FloorId = 42, FloorLevel = 2, FloorIndex = 1 };

        Assert.True(coordinator.BeginFloorParent(floor));
        driver.Popup = new NetherRuntimePopupContext
        {
            Kind = NetherRuntimePopupKind.Event,
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = coordinator.Generation,
            Sequence = 7,
        };
        driver.ParentPoll = NetherNativeActionResult.Started("parent-pending");

        int dispatches = 0;
        NetherRuntimeParentPollResult first = coordinator.Poll(
            popup =>
            {
                dispatches++;
                return new NetherNativeActionResult(NetherNativeActionResultKind.Started, "event-dispatched");
            }
        );

        Assert.Equal(NetherRuntimeParentPollKind.Pending, first.Kind);
        Assert.Equal(1, dispatches);
        Assert.True(coordinator.HasPendingParent);

        driver.Popup = null;
        driver.ParentPoll = NetherNativeActionResult.Completed("parent-terminal");
        NetherRuntimeParentPollResult terminal = coordinator.Poll(_ => throw new Xunit.Sdk.XunitException("must not dispatch twice"));

        Assert.Equal(NetherRuntimeParentPollKind.Completed, terminal.Kind);
        Assert.False(coordinator.HasPendingParent);
    }

    [Fact]
    public void Stale_popup_from_prior_generation_is_never_dispatched()
    {
        var driver = new FakeDriver();
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        var first = new NetherPlannedAction(NetherActionKind.SelectFloor) { FloorId = 1, FloorLevel = 1 };
        var second = new NetherPlannedAction(NetherActionKind.SelectFloor) { FloorId = 2, FloorLevel = 2 };

        Assert.True(coordinator.BeginFloorParent(first));
        long staleGeneration = coordinator.Generation;
        coordinator.TerminateParent();
        Assert.True(coordinator.BeginFloorParent(second));

        driver.Popup = new NetherRuntimePopupContext
        {
            Kind = NetherRuntimePopupKind.CodeOffer,
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = staleGeneration,
            Sequence = 1,
        };
        driver.ParentPoll = NetherNativeActionResult.Started("parent-pending");

        int dispatches = 0;
        NetherRuntimeParentPollResult result = coordinator.Poll(_ =>
        {
            dispatches++;
            return new NetherNativeActionResult(NetherNativeActionResultKind.Started, "must-not-run");
        });

        Assert.Equal(NetherRuntimeParentPollKind.Pending, result.Kind);
        Assert.Equal(0, dispatches);
    }

    [Fact]
    public void Successful_popup_without_a_registered_sequence_fails_closed_as_an_owner_mismatch()
    {
        var driver = new FakeDriver();
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        Assert.True(coordinator.BeginFloorParent(
            new NetherPlannedAction(NetherActionKind.SelectFloor)
            {
                FloorId = 9,
                FloorLevel = 2,
            }
        ));
        driver.Popup = new NetherRuntimePopupContext
        {
            Kind = NetherRuntimePopupKind.Event,
            RuntimeGeneration = 1,
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = coordinator.Generation,
            Sequence = 0,
        };
        driver.ParentPoll = NetherNativeActionResult.Completed("must-not-poll-parent");

        NetherRuntimeParentPollResult result = coordinator.Poll(
            _ => throw new Xunit.Sdk.XunitException("unregistered popup must not dispatch")
        );

        Assert.Equal(NetherRuntimeParentPollKind.Faulted, result.Kind);
        Assert.Equal(
            "owned-popup-unavailable:success-popup-owner-mismatch",
            result.Detail
        );
        Assert.Equal(0, driver.ParentPollCount);
        Assert.False(coordinator.HasPendingParent);
    }

    [Fact]
    public void Parent_terminal_is_not_consumed_on_the_same_tick_as_owned_modal_dispatch()
    {
        var driver = new FakeDriver();
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        var floor = new NetherPlannedAction(NetherActionKind.SelectFloor) { FloorId = 4, FloorLevel = 4 };
        Assert.True(coordinator.BeginFloorParent(floor));
        driver.Popup = new NetherRuntimePopupContext
        {
            Kind = NetherRuntimePopupKind.Treasure,
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = coordinator.Generation,
            Sequence = 2,
        };
        driver.ParentPoll = NetherNativeActionResult.Completed("premature-parent-terminal");

        NetherRuntimeParentPollResult first = coordinator.Poll(_ => NetherNativeActionResult.Started("treasure-click"));

        Assert.Equal(NetherRuntimeParentPollKind.Pending, first.Kind);
        Assert.True(coordinator.HasPendingParent);

        driver.Popup = null;
        NetherRuntimeParentPollResult second = coordinator.Poll(_ => throw new Xunit.Sdk.XunitException("no second popup"));

        Assert.Equal(NetherRuntimeParentPollKind.Completed, second.Kind);
    }

    [Fact]
    public void Same_live_code_offer_is_redispatched_only_after_a_monotonic_reload_epoch()
    {
        var driver = new FakeDriver();
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        var floor = new NetherPlannedAction(NetherActionKind.SelectFloor) { FloorId = 4, FloorLevel = 4 };
        Assert.True(coordinator.BeginFloorParent(floor));
        driver.Popup = new NetherRuntimePopupContext
        {
            Kind = NetherRuntimePopupKind.CodeOffer,
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = coordinator.Generation,
            Sequence = 8,
            DecisionEpoch = 0,
        };

        int dispatches = 0;
        Assert.Equal(
            NetherRuntimeParentPollKind.Pending,
            coordinator.Poll(_ =>
            {
                dispatches++;
                return NetherNativeActionResult.Started("reload");
            }).Kind
        );

        // The popup instance/sequence deliberately remains live while RerollAsync rebuilds
        // its model.  A new decision epoch is the only allowed re-dispatch identity.
        driver.Popup = driver.Popup with { DecisionEpoch = 1 };
        Assert.Equal(
            NetherRuntimeParentPollKind.Pending,
            coordinator.Poll(_ =>
            {
                dispatches++;
                return NetherNativeActionResult.Started("select-after-reload");
            }).Kind
        );
        Assert.Equal(2, dispatches);

        Assert.Equal(
            NetherRuntimeParentPollKind.Pending,
            coordinator.Poll(_ => throw new Xunit.Sdk.XunitException("same epoch must not replay")) .Kind
        );
        Assert.Equal(2, dispatches);
    }

    [Fact]
    public void Same_epoch_keep_popup_is_stale_and_cannot_replay_the_cancel_mutation()
    {
        var driver = new FakeDriver();
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        var floor = new NetherPlannedAction(NetherActionKind.SelectFloor) { FloorId = 4, FloorLevel = 4 };
        Assert.True(coordinator.BeginFloorParent(floor));
        driver.Popup = new NetherRuntimePopupContext
        {
            Kind = NetherRuntimePopupKind.CodeOffer,
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = coordinator.Generation,
            Sequence = 8,
            DecisionEpoch = 0,
        };
        driver.ParentPoll = NetherNativeActionResult.Started("parent-pending");

        int keepDispatches = 0;
        Assert.Equal(
            NetherRuntimeParentPollKind.Pending,
            coordinator.Poll(_ =>
            {
                keepDispatches++;
                return NetherNativeActionResult.Started("keep-cancel-started");
            }).Kind
        );
        Assert.Equal(
            NetherRuntimeParentPollKind.Pending,
            coordinator.Poll(_ => throw new Xunit.Sdk.XunitException("same epoch keep must not replay")).Kind
        );
        Assert.Equal(1, keepDispatches);
    }

    [Fact]
    public void Failed_owned_popup_dispatch_releases_parent_before_the_next_floor_owner_is_registered()
    {
        var driver = new FakeDriver();
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        var failedFloor = new NetherPlannedAction(NetherActionKind.SelectFloor)
        {
            FloorId = 19,
            FloorLevel = 5,
        };
        var successorFloor = new NetherPlannedAction(NetherActionKind.SelectFloor)
        {
            FloorId = 23,
            FloorLevel = 6,
        };

        Assert.True(coordinator.BeginFloorParent(failedFloor));
        driver.Popup = new NetherRuntimePopupContext
        {
            Kind = NetherRuntimePopupKind.Shop,
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = coordinator.Generation,
            Sequence = 4,
        };

        NetherRuntimeParentPollResult faulted = coordinator.Poll(
            _ => NetherNativeActionResult.BindingUnavailable(
                "owned-popup-policy:UnknownMasterData:invalid-shop-content"
            )
        );

        Assert.Equal(NetherRuntimeParentPollKind.Faulted, faulted.Kind);
        Assert.Equal(
            "owned-popup:owned-popup-policy:UnknownMasterData:invalid-shop-content",
            faulted.Detail
        );
        Assert.Equal(0, driver.ParentPollCount);
        Assert.False(coordinator.HasPendingParent);
        Assert.True(coordinator.BeginFloorParent(successorFloor));
    }

    [Fact]
    public void Registered_popup_mapping_failure_is_terminal_and_preserves_the_exact_reason()
    {
        var driver = new FakeDriver
        {
            PopupFailure = "invalid-native-shop-content:content-id-zero",
            ParentPoll = NetherNativeActionResult.Started("parent-pending"),
        };
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        var floor = new NetherPlannedAction(NetherActionKind.SelectFloor) { FloorId = 18, FloorLevel = 5 };

        Assert.True(coordinator.BeginFloorParent(floor));
        NetherRuntimeParentPollResult result = coordinator.Poll(
            _ => throw new Xunit.Sdk.XunitException("unmappable popup must not dispatch")
        );

        Assert.Equal(NetherRuntimeParentPollKind.Faulted, result.Kind);
        Assert.Equal(
            "owned-popup-unavailable:invalid-native-shop-content:content-id-zero",
            result.Detail
        );
        Assert.False(coordinator.HasPendingParent);
    }

    [Fact]
    public void Registered_code_offer_waits_for_its_native_model_before_polling_the_parent()
    {
        var driver = new FakeDriver
        {
            PopupFailure = "code-offer-model-not-ready",
            PopupFailureIsPending = true,
            // A native event parent may already look terminal while the Code Offer controller
            // is still completing its next-frame model initialization.  Consuming it here would
            // lose the only owner for the live popup.
            ParentPoll = NetherNativeActionResult.Completed("premature-parent-terminal"),
        };
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        var floor = new NetherPlannedAction(NetherActionKind.SelectFloor)
        {
            FloorId = 389,
            FloorLevel = 83,
        };

        Assert.True(coordinator.BeginFloorParent(floor));
        NetherRuntimeParentPollResult waiting = coordinator.Poll(
            _ => throw new Xunit.Sdk.XunitException("uninitialized Code Offer must not dispatch")
        );

        Assert.Equal(NetherRuntimeParentPollKind.Pending, waiting.Kind);
        Assert.True(coordinator.HasPendingParent);

        driver.Popup = new NetherRuntimePopupContext
        {
            Kind = NetherRuntimePopupKind.CodeOffer,
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = coordinator.Generation,
            Sequence = 128,
        };
        int dispatches = 0;
        NetherRuntimeParentPollResult initialized = coordinator.Poll(_ =>
        {
            dispatches++;
            return NetherNativeActionResult.Started("code-offer-dispatched");
        });

        Assert.Equal(NetherRuntimeParentPollKind.Pending, initialized.Kind);
        Assert.Equal(1, dispatches);
        Assert.True(coordinator.HasPendingParent);
    }

    [Fact]
    public void Native_code_replace_continuation_polls_its_parent_without_redispatching_the_popup()
    {
        var driver = new FakeDriver();
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        var floor = new NetherPlannedAction(NetherActionKind.SelectFloor)
        {
            FloorId = 389,
            FloorLevel = 83,
        };

        Assert.True(coordinator.BeginFloorParent(floor));
        driver.PopupResultOverride = NetherRuntimePopupResult.NativeContinuation(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                RuntimeGeneration = 86,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = coordinator.Generation,
                Sequence = 154,
            },
            "owned-code-list-native-replacement-continuation"
        );
        driver.ParentPoll = NetherNativeActionResult.Started(
            "awaiting-native-code-replace-popup-task"
        );

        NetherRuntimeParentPollResult pending = coordinator.Poll(
            _ => throw new Xunit.Sdk.XunitException("Replace(2) is native continuation, not a new CodeOffer dispatch")
        );

        Assert.Equal(NetherRuntimeParentPollKind.Pending, pending.Kind);
        Assert.Equal("awaiting-native-code-replace-popup-task", pending.Detail);
        Assert.Equal(1, driver.ParentPollCount);
        Assert.True(coordinator.HasPendingParent);

        driver.PopupResultOverride = NetherRuntimePopupResult.Failure("missing-owned-floor-popup");
        driver.ParentPoll = NetherNativeActionResult.Completed("native-code-replacement-terminal");
        NetherRuntimeParentPollResult completed = coordinator.Poll(
            _ => throw new Xunit.Sdk.XunitException("native continuation must never redispatch")
        );

        Assert.Equal(NetherRuntimeParentPollKind.Completed, completed.Kind);
        Assert.Equal(2, driver.ParentPollCount);
        Assert.False(coordinator.HasPendingParent);
    }

    [Fact]
    public void Malformed_native_continuation_fails_closed_without_dispatching_or_polling_parent()
    {
        var driver = new FakeDriver();
        var coordinator = new NetherRuntimeFlowCoordinator(driver);
        Assert.True(coordinator.BeginFloorParent(
            new NetherPlannedAction(NetherActionKind.SelectFloor)
            {
                FloorId = 389,
                FloorLevel = 83,
            }
        ));
        driver.PopupResultOverride = NetherRuntimePopupResult.NativeContinuation(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeTransform,
                RuntimeGeneration = 86,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = coordinator.Generation,
                Sequence = 154,
            },
            "forged-native-continuation"
        );
        driver.ParentPoll = NetherNativeActionResult.Completed("must-not-poll-parent");

        NetherRuntimeParentPollResult result = coordinator.Poll(
            _ => throw new Xunit.Sdk.XunitException("malformed continuation must not dispatch")
        );

        Assert.Equal(NetherRuntimeParentPollKind.Faulted, result.Kind);
        Assert.Equal(
            "owned-popup-unavailable:native-continuation-owner-mismatch",
            result.Detail
        );
        Assert.Equal(0, driver.ParentPollCount);
        Assert.False(coordinator.HasPendingParent);
    }

    private sealed class FakeDriver : INetherRuntimeParentDriver
    {
        public NetherRuntimePopupContext? Popup { get; set; }
        public NetherRuntimePopupResult? PopupResultOverride { get; set; }
        public string PopupFailure { get; set; } = "missing-owned-floor-popup";
        public bool PopupFailureIsPending { get; set; }
        public NetherRuntimePopupContext PendingPopup { get; set; } = new()
        {
            Kind = NetherRuntimePopupKind.CodeOffer,
            RuntimeGeneration = 1,
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = 1,
            Sequence = 1,
        };
        public NetherNativeActionResult ParentPoll { get; set; } = NetherNativeActionResult.Started("pending");
        public int ParentPollCount { get; private set; }
        public int DispatchCount { get; private set; }

        public NetherRuntimePopupResult TryGetOwnedPopup(NetherPlannedAction parent) => PopupResultOverride ?? (Popup == null
            ? PopupFailureIsPending
                ? NetherRuntimePopupResult.Pending(PendingPopup, PopupFailure)
                : NetherRuntimePopupResult.Failure(PopupFailure)
            : NetherRuntimePopupResult.Success(Popup with
            {
                RuntimeGeneration = Popup.RuntimeGeneration > 0 ? Popup.RuntimeGeneration : 1,
            }));

        public NetherNativeActionResult PollFloorParent()
        {
            ParentPollCount++;
            return ParentPoll;
        }

        public void ObserveDispatch() => DispatchCount++;
    }
}
