using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherAutoClimbStateMachineTests
{
    [Fact]
    public void Toggle_outside_nether_stays_disabled_with_not_in_nether_reason()
    {
        var machine = new NetherAutoClimbStateMachine();

        machine.Toggle(isInNether: false);

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.Disabled, machine.Phase);
        Assert.Equal(NetherPauseReason.NotInNether, machine.PauseReason);
    }

    [Fact]
    public void One_in_flight_action_rejects_a_second_action()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Play, 10);

        Assert.True(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), fingerprint));
        Assert.False(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), fingerprint));
        Assert.Equal(NetherAutoClimbPhase.ExecutingNativeAction, machine.Phase);
    }

    [Fact]
    public void Unknown_outcome_requires_reconcile_before_another_action()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint before = Fingerprint(NetherSessionStatus.Play, 10);

        Assert.True(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), before));
        machine.ObserveUnknownOutcome();

        Assert.Equal(NetherAutoClimbPhase.Reconciling, machine.Phase);
        Assert.False(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), before));
        Assert.Equal(NetherActionKind.SelectFloor, machine.PendingAction!.Value.Kind);
    }

    [Fact]
    public void Owned_popup_can_enrich_only_its_current_floor_parent_without_losing_pre_snapshot()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint before = Fingerprint(NetherSessionStatus.Play, 10);
        NetherPlannedAction parent = new(NetherActionKind.SelectFloor)
        {
            FloorId = 22,
            FloorLevel = 11,
            FloorIndex = 2,
            ExpectedBeforeStatus = NetherSessionStatus.Play,
            ExpectedAfterStatus = NetherSessionStatus.Play,
        };
        NetherPlannedAction composed = parent with
        {
            OwnedPopupKind = NetherRuntimePopupKind.Event,
            OwnedPopupActionKind = NetherActionKind.SelectEventOption,
            OptionNumber = 1,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.NetherGoldGain, 1) },
        };

        Assert.True(machine.TryBegin(parent, before));
        Assert.True(machine.TryReplacePendingFloorTransaction(parent, composed));
        Assert.Equal(before, machine.PreActionFingerprint);
        Assert.Equal(NetherRuntimePopupKind.Event, machine.PendingAction!.Value.OwnedPopupKind);
        Assert.False(machine.TryReplacePendingFloorTransaction(
            parent with { FloorId = 23 },
            composed
        ));
    }

    [Fact]
    public void Changed_fingerprint_confirms_action_and_returns_stable()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint before = Fingerprint(NetherSessionStatus.Play, 10);
        NetherSnapshotFingerprint after = Fingerprint(NetherSessionStatus.Play, 11);

        Assert.True(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), before));

        machine.ObserveActionResult(after, NetherActionOutcome.Applied);

        Assert.Equal(NetherAutoClimbPhase.Stable, machine.Phase);
        Assert.Null(machine.PendingAction);
    }

    [Fact]
    public void Unchanged_fingerprint_after_known_failure_returns_stable_without_retrying()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Play, 10);

        Assert.True(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), fingerprint));

        machine.ObserveActionResult(fingerprint, NetherActionOutcome.NotApplied);

        Assert.Equal(NetherAutoClimbPhase.Stable, machine.Phase);
        Assert.Null(machine.PendingAction);
        Assert.False(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), fingerprint));
    }

    [Fact]
    public void Ambiguous_fingerprint_pauses_instead_of_replaying_action()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint before = Fingerprint(NetherSessionStatus.Play, 10);
        NetherSnapshotFingerprint after = Fingerprint(NetherSessionStatus.Play, 12);

        Assert.True(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), before));

        machine.ObserveActionResult(after, NetherActionOutcome.Ambiguous);

        Assert.Equal(NetherAutoClimbPhase.Paused, machine.Phase);
        Assert.Equal(NetherPauseReason.AmbiguousServerOutcome, machine.PauseReason);
        Assert.False(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), after));
    }

    [Fact]
    public void Clear_is_not_completed_until_result_response()
    {
        var machine = new NetherAutoClimbStateMachine();

        machine.Toggle(isInNether: true);
        machine.ObserveStable(Fingerprint(NetherSessionStatus.Clear, 20));

        Assert.Equal(NetherAutoClimbPhase.AwaitingSceneChange, machine.Phase);
        Assert.NotEqual(NetherAutoClimbPhase.Completed, machine.Phase);

        machine.Complete();

        Assert.Equal(NetherAutoClimbPhase.Completed, machine.Phase);
    }

    [Fact]
    public void Finish_parent_terminal_preserves_pending_evidence_until_result_terminal()
    {
        var machine = new NetherAutoClimbStateMachine();
        NetherSnapshotFingerprint sleep = Fingerprint(NetherSessionStatus.Sleep, 100);

        machine.Toggle(isInNether: true);
        machine.ObserveStable(sleep);
        Assert.True(machine.TryBegin(
            new NetherPlannedAction(NetherActionKind.FinishAtCheckpoint),
            sleep
        ));

        Assert.True(machine.BeginFinishResultHandoff());
        Assert.Equal(NetherAutoClimbPhase.AwaitingSceneChange, machine.Phase);
        Assert.Equal(NetherActionKind.FinishAtCheckpoint, machine.PendingAction!.Value.Kind);

        Assert.True(machine.Complete());
        Assert.Equal(NetherAutoClimbPhase.Completed, machine.Phase);
        Assert.Null(machine.PendingAction);
    }

    [Fact]
    public void Prior_finish_evidence_retires_at_the_exact_pristine_new_run_snapshot()
    {
        NetherAutoClimbStateMachine machine = DisabledMachineWithPendingFinish(out NetherSnapshot finished);
        NetherSnapshot freshRun = PristineNewRunAfter(finished);

        Assert.True(machine.TryRetireFinishEvidenceForNewRun(freshRun));

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.Disabled, machine.Phase);
        Assert.Null(machine.PendingAction);
        Assert.Null(machine.PreActionSnapshot);
        Assert.Equal("prior-finish-retired-at-authoritative-new-run", machine.PauseDetail);
    }

    [Fact]
    public void Prior_finish_evidence_survives_every_incomplete_new_run_snapshot_contract()
    {
        NetherAutoClimbStateMachine machine = DisabledMachineWithPendingFinish(out NetherSnapshot finished);
        NetherSnapshot freshRun = PristineNewRunAfter(finished);
        NetherSnapshot[] incomplete =
        {
            freshRun with { Status = NetherSessionStatus.Sleep },
            freshRun with { FloorLevel = 1 },
            freshRun with { CurrentFloorId = 0 },
            freshRun with { CurrentNodeId = 0 },
            freshRun with { Floors = System.Array.Empty<NetherFloorNode>() },
            freshRun with { ErosionPoint = 1 },
            freshRun with { TicketCount = finished.TicketCount },
            freshRun with { MapHash = finished.MapHash },
        };

        foreach (NetherSnapshot snapshot in incomplete)
            Assert.False(machine.TryRetireFinishEvidenceForNewRun(snapshot));

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.Disabled, machine.Phase);
        Assert.Equal(NetherActionKind.FinishAtCheckpoint, machine.PendingAction!.Value.Kind);
        Assert.Same(finished, machine.PreActionSnapshot);
    }

    [Fact]
    public void Lose_pauses_for_user_control_and_never_waits_forever_for_a_result_request()
    {
        var machine = new NetherAutoClimbStateMachine();

        machine.Toggle(isInNether: true);
        machine.ObserveStable(Fingerprint(NetherSessionStatus.Lose, 20));

        Assert.Equal(NetherAutoClimbPhase.Paused, machine.Phase);
        Assert.Equal(NetherPauseReason.Lose, machine.PauseReason);
    }

    [Fact]
    public void Battle_wait_remains_awaiting_battle_until_final_task_settles()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Battle, 30);

        Assert.True(machine.TryBegin(BattleSettlementAction(), fingerprint));
        Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, machine.Phase);

        Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, machine.Phase);
    }

    [Fact]
    public void Native_binding_selector_requires_one_exact_full_signature_match()
    {
        NetherNativeMethodDescriptor expected = new(
            "OnFloorClickedEventAsync",
            new[] { "System.Int32", "System.Int32" },
            "Cysharp.Threading.Tasks.UniTask");
        NetherNativeMethodDescriptor selected = expected;
        NetherNativeMethodDescriptor wrongReturn = new(
            "OnFloorClickedEventAsync",
            new[] { "System.Int32", "System.Int32" },
            "System.Void");

        NetherNativeBindingSelection selection = NetherNativeMethodBindingSelector.Select(
            expected,
            new[] { selected, wrongReturn });

        Assert.Equal(NetherNativeActionResultKind.Started, selection.ResultKind);
        Assert.Equal(selected, selection.Method);
    }

    [Fact]
    public void Native_binding_selector_fails_closed_for_zero_or_ambiguous_matches()
    {
        NetherNativeMethodDescriptor expected = new(
            "OnFloorClickedEventAsync",
            new[] { "System.Int32", "System.Int32" },
            "Cysharp.Threading.Tasks.UniTask");
        NetherNativeMethodDescriptor wrongArity = new(
            "OnFloorClickedEventAsync",
            new[] { "System.Int32" },
            "Cysharp.Threading.Tasks.UniTask");

        NetherNativeBindingSelection none = NetherNativeMethodBindingSelector.Select(expected, new[] { wrongArity });
        NetherNativeBindingSelection many = NetherNativeMethodBindingSelector.Select(expected, new[] { expected, expected });

        Assert.Equal(NetherNativeActionResultKind.BindingUnavailable, none.ResultKind);
        Assert.Null(none.Method);
        Assert.Equal(NetherNativeActionResultKind.BindingUnavailable, many.ResultKind);
        Assert.Null(many.Method);
    }

    [Fact]
    public void Invalid_live_config_pauses_instead_of_clamping_to_a_dangerous_default()
    {
        var gate = new NetherAutoClimbSettingsSnapshotGate();

        bool accepted = gate.TryCapture(
            new NetherAutoClimbSettings { MaxDepth = 0, SoftErosionLimit = 90 },
            NetherAutoClimbPhase.Stable,
            out _,
            out NetherPauseReason reason,
            out string detail);

        Assert.False(accepted);
        Assert.Equal(NetherPauseReason.InvalidConfiguration, reason);
        Assert.Equal("invalid-max-depth", detail);
    }

    [Fact]
    public void Zero_soft_erosion_limit_is_invalid_instead_of_disabling_the_safety_boundary()
    {
        var gate = new NetherAutoClimbSettingsSnapshotGate();

        bool accepted = gate.TryCapture(
            new NetherAutoClimbSettings { SoftErosionLimit = 0 },
            NetherAutoClimbPhase.Stable,
            out _,
            out NetherPauseReason reason,
            out string detail);

        Assert.False(accepted);
        Assert.Equal(NetherPauseReason.InvalidConfiguration, reason);
        Assert.Equal("invalid-soft-erosion-limit", detail);
    }

    [Fact]
    public void Positive_max_depth_above_the_default_is_valid_and_later_clamped_by_server_limits()
    {
        var gate = new NetherAutoClimbSettingsSnapshotGate();

        bool accepted = gate.TryCapture(
            new NetherAutoClimbSettings { MaxDepth = 500 },
            NetherAutoClimbPhase.Stable,
            out NetherAutoClimbSettings settings,
            out _,
            out _);

        Assert.True(accepted);
        Assert.Equal(500, settings.MaxDepth);
    }

    [Fact]
    public void Reloaded_settings_apply_only_after_the_current_action_reconciles()
    {
        var gate = new NetherAutoClimbSettingsSnapshotGate();
        var original = new NetherAutoClimbSettings { MaxDepth = 130, SoftErosionLimit = 90 };
        var reloaded = new NetherAutoClimbSettings { MaxDepth = 70, SoftErosionLimit = 80 };

        Assert.True(gate.TryCapture(original, NetherAutoClimbPhase.Stable, out NetherAutoClimbSettings active, out _, out _));
        Assert.True(gate.TryCapture(reloaded, NetherAutoClimbPhase.ExecutingNativeAction, out NetherAutoClimbSettings duringAction, out _, out _));
        Assert.True(gate.TryCapture(reloaded, NetherAutoClimbPhase.Stable, out NetherAutoClimbSettings afterReconcile, out _, out _));

        Assert.Same(original, active);
        Assert.Same(original, duringAction);
        Assert.Same(reloaded, afterReconcile);
    }

    [Fact]
    public void F12_disable_stops_new_actions_but_preserves_unknown_outcome_reconciliation()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Play, 10);

        Assert.True(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), fingerprint));
        machine.ObserveUnknownOutcome();
        machine.Toggle(isInNether: true);

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.Reconciling, machine.Phase);
        Assert.Equal(NetherActionKind.SelectFloor, machine.PendingAction!.Value.Kind);
        Assert.False(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), fingerprint));
    }

    [Fact]
    public void F12_disable_while_native_action_is_still_in_flight_keeps_polling_before_reconcile()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Play, 10);

        Assert.True(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), fingerprint));
        machine.Toggle(isInNether: true);

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.ExecutingNativeAction, machine.Phase);
        Assert.Equal(NetherActionKind.SelectFloor, machine.PendingAction!.Value.Kind);
    }

    [Fact]
    public void F12_cannot_reenable_over_a_still_in_flight_native_action()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Play, 10);

        Assert.True(machine.TryBegin(new NetherPlannedAction(NetherActionKind.SelectFloor), fingerprint));
        machine.Toggle(isInNether: true); // off
        machine.Toggle(isInNether: true); // attempted re-enable before task observation

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.ExecutingNativeAction, machine.Phase);
        Assert.Equal(NetherActionKind.SelectFloor, machine.PendingAction!.Value.Kind);
    }

    [Fact]
    public void F12_off_awaiting_final_start_task_preserves_settlement_evidence_and_blocks_reenable()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Battle, 30);

        Assert.True(machine.TryBegin(BattleSettlementAction(), fingerprint));
        machine.Toggle(isInNether: true); // off
        machine.Toggle(isInNether: true); // attempted re-enable before final task/battle terminal

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, machine.Phase);
        Assert.Equal(NetherActionKind.BattleSettlement, machine.PendingAction!.Value.Kind);
    }

    [Fact]
    public void F12_off_awaiting_battle_preserves_battle_settlement_evidence_and_blocks_reenable()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Battle, 30);

        Assert.True(machine.TryBegin(BattleSettlementAction(), fingerprint));
        machine.Toggle(isInNether: true); // off
        machine.Toggle(isInNether: true); // attempted re-enable before clear/close terminal

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, machine.Phase);
        Assert.Equal(NetherActionKind.BattleSettlement, machine.PendingAction!.Value.Kind);
    }

    [Fact]
    public void Combat_floor_parent_enters_scene_handoff_and_F12_off_keeps_draining_it()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Play, 10);
        var action = new NetherPlannedAction(NetherActionKind.SelectFloor)
        {
            FloorId = 30,
            FloorLevel = 11,
            FloorIndex = 0,
            ExpectedBeforeStatus = NetherSessionStatus.Play,
            ExpectedAfterStatus = NetherSessionStatus.Battle,
            BattleProjection = new NetherBattleProjectionPayload(
                200, 30, 20, 5, 5, 25, 25, "30024", "battle-ingress-30"
            ),
        };

        Assert.True(machine.TryBegin(action, fingerprint));
        Assert.True(machine.BeginBattleSceneHandoff());
        Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSceneHandoff, machine.Phase);

        machine.Toggle(isInNether: true);
        machine.Toggle(isInNether: true);

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSceneHandoff, machine.Phase);
        Assert.Equal(NetherActionKind.SelectFloor, machine.PendingAction!.Value.Kind);
    }

    [Fact]
    public void F12_off_awaiting_battle_settlement_preserves_evidence_and_blocks_reenable()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint fingerprint = Fingerprint(NetherSessionStatus.Battle, 30);

        Assert.True(machine.TryBegin(BattleSettlementAction(), fingerprint));
        machine.BeginBattleSettlement();
        machine.Toggle(isInNether: true); // off
        machine.Toggle(isInNether: true); // attempted re-enable before GET reconcile

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSettlement, machine.Phase);
        Assert.Equal(NetherActionKind.BattleSettlement, machine.PendingAction!.Value.Kind);
    }

    [Fact]
    public void Settled_battle_keeps_its_contract_until_result_next_rebinds_floor_selection()
    {
        var machine = StableMachine();
        NetherSnapshotFingerprint before = Fingerprint(NetherSessionStatus.Battle, 30);
        NetherSnapshotFingerprint after = Fingerprint(NetherSessionStatus.Wait, 31);

        Assert.True(machine.TryBegin(BattleSettlementAction(), before));
        Assert.True(machine.BeginBattleSettlement());
        Assert.True(machine.BeginBattleResultContinuation());
        Assert.Equal(NetherAutoClimbPhase.AwaitingBattleResultContinuation, machine.Phase);
        Assert.Equal(NetherActionKind.BattleSettlement, machine.PendingAction!.Value.Kind);

        Assert.True(machine.CompleteBattleResultContinuation(after));
        Assert.Equal(NetherAutoClimbPhase.Stable, machine.Phase);
        Assert.Null(machine.PendingAction);
    }

    [Fact]
    public void F12_can_enable_from_confirmed_nether_battle_result_and_resume_at_rebound_map()
    {
        var machine = new NetherAutoClimbStateMachine();

        Assert.True(machine.EnableFromBattleResult());
        Assert.True(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.AwaitingBattleResultContinuation, machine.Phase);
        Assert.Null(machine.PendingAction);

        Assert.True(machine.CompleteBattleResultContinuation(
            Fingerprint(NetherSessionStatus.Wait, 31)
        ));
        Assert.Equal(NetherAutoClimbPhase.Stable, machine.Phase);
    }

    [Fact]
    public void F12_off_awaiting_result_scene_keeps_result_observation_and_blocks_reenable()
    {
        var machine = new NetherAutoClimbStateMachine();
        NetherSnapshotFingerprint clear = Fingerprint(NetherSessionStatus.Clear, 30);

        machine.Toggle(isInNether: true);
        machine.ObserveStable(clear);
        machine.Toggle(isInNether: true); // off
        machine.Toggle(isInNether: true); // attempted re-enable before result terminal

        Assert.False(machine.IsEnabled);
        Assert.Equal(NetherAutoClimbPhase.AwaitingSceneChange, machine.Phase);
    }

    private static NetherAutoClimbStateMachine StableMachine()
    {
        var machine = new NetherAutoClimbStateMachine();
        machine.Toggle(isInNether: true);
        machine.ObserveStable(Fingerprint(NetherSessionStatus.Play, 1));
        return machine;
    }

    private static NetherAutoClimbStateMachine DisabledMachineWithPendingFinish(
        out NetherSnapshot finished
    )
    {
        finished = new NetherSnapshot
        {
            Status = NetherSessionStatus.Sleep,
            NetherId = 1,
            MapId = 1,
            CurrentFloorId = 474,
            CurrentNodeId = 474,
            FloorLevel = 100,
            FloorIndex = 10,
            ErosionPoint = 30,
            TicketCount = 2,
            Floors = new[]
            {
                new NetherFloorNode(474, 100, 10, NetherFloorNodeType.Boss)
                {
                    NodeId = 474,
                    IsUnlocked = true,
                },
            },
            CharacterHpHash = "finished-hp",
            CodeHash = "finished-codes",
            MapHash = "finished-run-map",
        };
        var machine = new NetherAutoClimbStateMachine();
        machine.Toggle(isInNether: true);
        machine.ObserveStable(finished.Fingerprint);
        Assert.True(machine.TryBegin(
            new NetherPlannedAction(NetherActionKind.FinishAtCheckpoint),
            finished
        ));
        Assert.True(machine.BeginFinishResultHandoff());
        machine.Pause(NetherPauseReason.BindingUnavailable, "native-result-task-timeout");
        machine.Toggle(isInNether: true);
        Assert.Equal(NetherAutoClimbPhase.Disabled, machine.Phase);
        return machine;
    }

    private static NetherSnapshot PristineNewRunAfter(NetherSnapshot finished) => new()
    {
        Status = NetherSessionStatus.Play,
        NetherId = finished.NetherId,
        MapId = finished.MapId,
        CurrentFloorId = 46,
        CurrentNodeId = 4294967299,
        FloorLevel = 0,
        FloorIndex = 2,
        ErosionPoint = 0,
        TicketCount = finished.TicketCount - 1,
        Floors = new[]
        {
            new NetherFloorNode(46, 1, 2, NetherFloorNodeType.Recovery)
            {
                NodeId = 4294967299,
                IsUnlocked = true,
            },
        },
        CharacterHpHash = "new-run-hp",
        CodeHash = "nether-codes:none",
        MapHash = "new-run-floor-zero-map",
    };

    private static NetherSnapshotFingerprint Fingerprint(NetherSessionStatus status, int floorLevel) => new(
        status,
        netherId: 100,
        mapId: 200,
        floorLevel,
        floorIndex: 1,
        erosionPoint: 20,
        characterHpHash: "1000:1000",
        codeHash: "30024",
        mapHash: $"map-{floorLevel}"
    );

    private static NetherPlannedAction BattleSettlementAction() => new(NetherActionKind.BattleSettlement)
    {
        BattleSettlement = new NetherBattleSettlementContract(
            EntryMapId: 200,
            EntryFloorId: 30,
            EntryStatus: NetherSessionStatus.Battle,
            ExpectedMapId: 200,
            ExpectedFloorId: 30,
            ExpectedStatus: NetherSessionStatus.Play,
            ProjectionIdentity: "battle-30"
        ),
    };
}
