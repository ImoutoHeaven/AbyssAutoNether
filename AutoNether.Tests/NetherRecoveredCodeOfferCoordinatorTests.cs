#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherRecoveredCodeOfferCoordinatorTests
{
    [Fact]
    public void Recovered_owner_is_valid_for_terminal_reload_and_keep_native_stages()
    {
        var owner = new NetherOwnedPopupStageOwner(
            NetherActionKind.RecoveredCodeOffer,
            Generation: 17,
            Sequence: 23,
            DecisionEpoch: 0
        );
        Assert.True(owner.IsValid);

        var reload = new NetherCodeReloadEpochCoordinator(maximumPendingPumps: 2);
        Assert.True(reload.Begin(
            owner.ReloadOwner,
            reloadCount: 1,
            new NetherRuntimeCodeCandidatesResult(
                new[]
                {
                    NetherCodeRuntimeSemanticMapper.MapCandidate(
                        30024,
                        (int)NetherCodeCategory.ErosionResistance,
                        effectType: 1,
                        level: 1,
                        rarity: 1
                    ),
                },
                IsMasterComplete: true,
                Detail: string.Empty
            )
        ));

        var keep = new NetherCodeKeepCancelCoordinator(maximumPendingPumps: 2);
        Assert.True(keep.Begin(owner.KeepOwner));
    }

    [Fact]
    public void Recovered_offer_finishes_code_then_original_parent_then_one_read_only_refresh()
    {
        var driver = new Driver();
        var flow = new NetherRecoveredCodeOfferCoordinator(maximumPopupPolls: 3);

        NetherRecoveredCodeOfferStep invoked = flow.Pump(
            driver,
            Settings(),
            lockedLane: null,
            allowInvoke: true
        );
        Assert.Equal(NetherRecoveredCodeOfferStepKind.AwaitingNative, invoked.Kind);
        Assert.Equal(NetherActionKind.SelectCode, Assert.Single(driver.InvokedActions).Kind);
        Assert.Equal(0, driver.ParentPolls);
        Assert.Equal(0, driver.RefreshStarts);

        driver.NativeSteps.Enqueue(NetherBattleResultCodeNativeStep.Completed("code-terminal"));
        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingParent,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        Assert.Equal(0, driver.ParentPolls);

        driver.ParentSteps.Enqueue(NetherNativeActionResult.Started("parent-pending"));
        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingParent,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        Assert.Equal(1, driver.ParentPolls);
        Assert.Equal(0, driver.RefreshStarts);

        driver.ParentSteps.Enqueue(NetherNativeActionResult.Completed("parent-terminal"));
        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingRefresh,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        Assert.Equal(1, driver.RefreshStarts);

        driver.RefreshSteps.Enqueue(NetherNativeActionResult.Completed("get-terminal"));
        NetherRecoveredCodeOfferStep completed = flow.Pump(
            driver,
            Settings(),
            null,
            allowInvoke: true
        );
        Assert.Equal(NetherRecoveredCodeOfferStepKind.Completed, completed.Kind);
        Assert.NotNull(completed.Snapshot);
        Assert.Equal(1, driver.CompletedOwners);
        Assert.Single(driver.InvokedActions);
    }

    [Fact]
    public void Pending_start_status_parent_hands_existing_sleep_checkpoint_to_continue_without_get_or_completion()
    {
        var driver = new Driver
        {
            RecoveredCheckpoint = NetherRecoveredCheckpointObservation.Ready(
                AppliedCodeSnapshot() with
                {
                    Status = NetherSessionStatus.Sleep,
                    FloorLevel = 20,
                    FloorIndex = 20,
                    TicketCount = 3,
                    LockReward = 0,
                    ContinuationTarget = new NetherContinuationTarget(2, 200, 20),
                    MapHash = "sleep-checkpoint",
                },
                "existing-continue-popup"
            ),
        };
        var flow = new NetherRecoveredCodeOfferCoordinator(maximumPopupPolls: 3);

        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingNative,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        driver.NativeSteps.Enqueue(NetherBattleResultCodeNativeStep.Completed("code-terminal"));
        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingParent,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        driver.ParentSteps.Enqueue(NetherNativeActionResult.Started("combined-parent-awaiting-continue"));

        NetherRecoveredCodeOfferStep handoff = flow.Pump(
            driver,
            Settings(),
            null,
            allowInvoke: true
        );

        Assert.Equal(NetherRecoveredCodeOfferStepKind.CheckpointReady, handoff.Kind);
        Assert.Equal(NetherSessionStatus.Sleep, handoff.Snapshot!.Status);
        Assert.False(flow.IsActive);
        Assert.Equal(0, driver.RefreshStarts);
        Assert.Equal(0, driver.CompletedOwners);
        Assert.Equal(1, driver.RecoveredCheckpointPolls);
    }

    [Fact]
    public void F12_off_before_recovered_offer_mutation_leaves_native_popup_for_user()
    {
        var driver = new Driver();
        var flow = new NetherRecoveredCodeOfferCoordinator(maximumPopupPolls: 3);

        NetherRecoveredCodeOfferStep step = flow.Pump(
            driver,
            Settings(),
            lockedLane: null,
            allowInvoke: false
        );

        Assert.Equal(NetherRecoveredCodeOfferStepKind.CanceledBeforeInvoke, step.Kind);
        Assert.Empty(driver.InvokedActions);
        Assert.Equal(0, driver.ParentPolls);
        Assert.Equal(0, driver.RefreshStarts);
        Assert.Equal(0, driver.CompletedOwners);
    }

    [Fact]
    public void F12_off_after_recovered_offer_mutation_drains_without_replay()
    {
        var driver = new Driver();
        var flow = new NetherRecoveredCodeOfferCoordinator(maximumPopupPolls: 3);

        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingNative,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        driver.NativeSteps.Enqueue(NetherBattleResultCodeNativeStep.Completed("code-terminal"));
        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingParent,
            flow.Pump(driver, Settings(), null, allowInvoke: false).Kind
        );

        driver.ParentSteps.Enqueue(NetherNativeActionResult.Completed("parent-terminal"));
        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingRefresh,
            flow.Pump(driver, Settings(), null, allowInvoke: false).Kind
        );
        driver.RefreshSteps.Enqueue(NetherNativeActionResult.Completed("get-terminal"));
        NetherRecoveredCodeOfferStep drained = flow.Pump(
            driver,
            Settings(),
            null,
            allowInvoke: false
        );

        Assert.Equal(NetherRecoveredCodeOfferStepKind.CanceledAfterDrain, drained.Kind);
        Assert.Single(driver.InvokedActions);
        Assert.Equal(1, driver.CompletedOwners);
    }

    [Fact]
    public void Consumed_parent_fault_status_after_a_successful_code_child_is_resolved_by_exact_get_reconcile()
    {
        var driver = new Driver();
        var flow = new NetherRecoveredCodeOfferCoordinator(maximumPopupPolls: 3);

        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingNative,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        driver.NativeSteps.Enqueue(NetherBattleResultCodeNativeStep.Completed("code-terminal"));
        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingParent,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );

        driver.ParentSteps.Enqueue(NetherNativeActionResult.UnknownOutcome(
            "native-start-status-terminal-faulted"
        ));
        NetherRecoveredCodeOfferStep reconcile = flow.Pump(
            driver,
            Settings(),
            null,
            allowInvoke: true
        );
        Assert.Equal(NetherRecoveredCodeOfferStepKind.AwaitingRefresh, reconcile.Kind);
        Assert.Contains("parent-unknown", reconcile.Detail);
        Assert.Equal(1, driver.RefreshStarts);

        driver.RefreshSteps.Enqueue(NetherNativeActionResult.Completed("get-terminal"));
        NetherRecoveredCodeOfferStep completed = flow.Pump(
            driver,
            Settings(),
            null,
            allowInvoke: true
        );
        Assert.Equal(NetherRecoveredCodeOfferStepKind.Completed, completed.Kind);
        Assert.Equal(1, driver.CompletedOwners);
        Assert.Single(driver.InvokedActions);
        NetherRecoveredCodeReconcileDiagnostic diagnostic = Assert.IsType<NetherRecoveredCodeReconcileDiagnostic>(
            completed.ReconcileDiagnostic
        );
        Assert.Equal(NetherActionOutcome.Applied, diagnostic.Outcome);
        Assert.Equal(NetherActionKind.SelectCode, diagnostic.ActionKind);
        Assert.Equal(30024, diagnostic.TargetCodeId);
        Assert.Equal(0, diagnostic.ReplaceCodeId);
        Assert.Equal(0, diagnostic.ReloadActions);
        Assert.Equal(1, diagnostic.BeforeReloadCount);
        Assert.Equal(1, diagnostic.ExpectedReloadCount);
        Assert.Equal(1, diagnostic.AfterReloadCount);
        Assert.False(diagnostic.TargetPresentBefore);
        Assert.True(diagnostic.TargetPresentAfter);
        Assert.Equal("none", diagnostic.BeforeCodeIds);
        Assert.Equal("30024", diagnostic.AfterCodeIds);
    }

    [Fact]
    public void Parent_unknown_get_reconcile_still_fails_closed_when_selected_code_is_absent()
    {
        var driver = new Driver
        {
            AppliedSnapshot = Snapshot(),
        };
        var flow = new NetherRecoveredCodeOfferCoordinator(maximumPopupPolls: 3);

        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingNative,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        driver.NativeSteps.Enqueue(NetherBattleResultCodeNativeStep.Completed("code-terminal"));
        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingParent,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        driver.ParentSteps.Enqueue(NetherNativeActionResult.UnknownOutcome(
            "native-start-status-terminal-faulted"
        ));
        Assert.Equal(
            NetherRecoveredCodeOfferStepKind.AwaitingRefresh,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );

        driver.RefreshSteps.Enqueue(NetherNativeActionResult.Completed("get-terminal"));
        NetherRecoveredCodeOfferStep faulted = flow.Pump(
            driver,
            Settings(),
            null,
            allowInvoke: true
        );
        Assert.Equal(NetherRecoveredCodeOfferStepKind.Faulted, faulted.Kind);
        Assert.Contains("recovered-code-reconcile", faulted.Detail);
        Assert.Equal(0, driver.CompletedOwners);
        Assert.Single(driver.InvokedActions);
        NetherRecoveredCodeReconcileDiagnostic diagnostic = Assert.IsType<NetherRecoveredCodeReconcileDiagnostic>(
            faulted.ReconcileDiagnostic
        );
        Assert.Equal(NetherActionOutcome.NotApplied, diagnostic.Outcome);
        Assert.False(diagnostic.TargetPresentBefore);
        Assert.False(diagnostic.TargetPresentAfter);
        Assert.Equal("none", diagnostic.BeforeCodeIds);
        Assert.Equal("none", diagnostic.AfterCodeIds);
    }

    [Fact]
    public void Popup_from_any_other_owner_is_rejected_before_mutation()
    {
        var driver = new Driver
        {
            Popup = Popup() with { OwnerAction = NetherActionKind.None },
        };
        var flow = new NetherRecoveredCodeOfferCoordinator(maximumPopupPolls: 1);

        NetherRecoveredCodeOfferStep step = flow.Pump(
            driver,
            Settings(),
            null,
            allowInvoke: true
        );

        Assert.Equal(NetherRecoveredCodeOfferStepKind.BindingUnavailable, step.Kind);
        Assert.Contains("popup-owner-mismatch", step.Detail);
        Assert.Empty(driver.InvokedActions);
        Assert.Equal(0, driver.ParentPolls);
    }

    private static NetherRuntimePopupContext Popup() => new()
    {
        Kind = NetherRuntimePopupKind.CodeOffer,
        OwnerAction = NetherActionKind.RecoveredCodeOffer,
        OwnerGeneration = 17,
        Sequence = 23,
        DecisionEpoch = 0,
    };

    private static NetherSnapshot Snapshot() => new()
    {
        Status = NetherSessionStatus.Play,
        NetherId = 1,
        MapId = 1,
        CurrentFloorId = 27,
        CurrentNodeId = 38654705666,
        FloorLevel = 8,
        FloorIndex = 1,
        CodeReloadCount = 1,
        CodeCapacity = 28,
        Characters = new[] { new NetherCharacterState(1001, 900) },
        Codes = Array.Empty<NetherCodeState>(),
        Floors = Array.Empty<NetherFloorNode>(),
        CharacterHpHash = "1001:900:1",
        CodeHash = string.Empty,
        MapHash = "map",
    };

    private static NetherSnapshot AppliedCodeSnapshot() => Snapshot() with
    {
        Codes = new[]
        {
            new NetherCodeState(30024, NetherCodeEffectKind.Safe, 1)
            {
                Category = NetherCodeCategory.ErosionResistance,
                Rarity = 1,
            },
        },
        CodeHash = "30024:1:1",
    };

    private static NetherAutoClimbSettings Settings() => new()
    {
        CombatLane = NetherCombatLane.Auto,
        CodeReloadReserve = 1,
    };

    private sealed class Driver : INetherRecoveredCodeOfferDriver
    {
        public bool HasRecoveredCodeOffer { get; set; } = true;
        public NetherRuntimePopupContext? Popup { get; set; } = NetherRecoveredCodeOfferCoordinatorTests.Popup();
        public List<NetherPlannedAction> InvokedActions { get; } = new();
        public Queue<NetherBattleResultCodeNativeStep> NativeSteps { get; } = new();
        public Queue<NetherNativeActionResult> ParentSteps { get; } = new();
        public Queue<NetherNativeActionResult> RefreshSteps { get; } = new();
        public NetherSnapshot AppliedSnapshot { get; set; } = AppliedCodeSnapshot();
        public NetherRecoveredCheckpointObservation RecoveredCheckpoint { get; set; } =
            NetherRecoveredCheckpointObservation.Waiting("checkpoint-popup-not-yet-registered");
        public int ParentPolls { get; private set; }
        public int RecoveredCheckpointPolls { get; private set; }
        public int RecoveredCheckpointHandoffs { get; private set; }
        public int RefreshStarts { get; private set; }
        public int CompletedOwners { get; private set; }

        public NetherRuntimeSnapshotResult TryCaptureRecoveredCodeSnapshot() =>
            NetherRuntimeSnapshotResult.Success(Snapshot());

        public NetherRuntimeCodeCandidatesResult TryGetRecoveredCodeCandidates() => new(
            new[]
            {
                NetherCodeRuntimeSemanticMapper.MapCandidate(
                    30024,
                    (int)NetherCodeCategory.ErosionResistance,
                    effectType: 1,
                    level: 1,
                    rarity: 1
                ),
            },
            IsMasterComplete: true,
            Detail: string.Empty
        );

        public NetherRuntimePopupResult TryGetRecoveredCodePopup() => Popup == null
            ? NetherRuntimePopupResult.Failure("popup-not-yet-registered")
            : NetherRuntimePopupResult.Success(Popup);

        public NetherNativeActionResult InvokeRecoveredCode(
            NetherRuntimePopupContext popup,
            NetherPlannedAction action
        )
        {
            InvokedActions.Add(action);
            return NetherNativeActionResult.Started("recovered-code-invoked");
        }

        public NetherBattleResultCodeNativeStep PollRecoveredCodeNative() =>
            NativeSteps.Count == 0
                ? NetherBattleResultCodeNativeStep.Pending("code-pending")
                : NativeSteps.Dequeue();

        public NetherNativeActionResult PollRecoveredCodeParent()
        {
            ParentPolls++;
            return ParentSteps.Count == 0
                ? NetherNativeActionResult.Started("parent-pending")
                : ParentSteps.Dequeue();
        }

        public NetherRecoveredCheckpointObservation ObserveRecoveredCheckpoint()
        {
            RecoveredCheckpointPolls++;
            return RecoveredCheckpoint;
        }

        public NetherNativeActionResult PrepareRecoveredCheckpointHandoff()
        {
            RecoveredCheckpointHandoffs++;
            HasRecoveredCodeOffer = false;
            return NetherNativeActionResult.Completed("checkpoint-handoff-prepared");
        }

        public NetherNativeActionResult BeginRecoveredCodeRefresh()
        {
            RefreshStarts++;
            return NetherNativeActionResult.Started("get-started");
        }

        public NetherNativeActionResult PollRecoveredCodeRefresh() =>
            RefreshSteps.Count == 0
                ? NetherNativeActionResult.Started("get-pending")
                : RefreshSteps.Dequeue();

        public NetherRuntimeSnapshotResult TryCaptureRecoveredCodeAppliedSnapshot() =>
            NetherRuntimeSnapshotResult.Success(AppliedSnapshot);

        public void CompleteRecoveredCodeOffer() => CompletedOwners++;
    }
}
