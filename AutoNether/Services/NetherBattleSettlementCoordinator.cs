#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Native battle task observation is distinct from settlement authority. Clear/close terminal
/// evidence starts exactly one GET-only refresh. Its target-specific snapshot settles immediately;
/// an unchanged entry Battle snapshot instead waits for the native result owner, whose fresh
/// datastore-backed transition snapshot settles the retained contract before Code mutation.
/// </summary>
internal interface INetherBattleSettlementDriver
{
    NetherNativeActionResult PollBattleLifecycle();

    bool TryConsumeBattleClear();

    bool TryConsumeBattleClose();
}

/// <summary>
/// Reads the exact live possession/master projection after the GET-only settlement refresh.
/// It has no mutation capability; unknown code semantics are therefore evidence to pause, not
/// a reason to reuse the pre-battle fingerprint.
/// </summary>
internal interface INetherBattleProjectionSnapshotDriver
{
    NetherActiveCodeErosionProjection TryCaptureActiveCodeErosionProjection();
}

internal enum NetherBattleSettlementStepKind
{
    AwaitingBattle,
    AwaitingSettlement,
    /// <summary>
    /// The clear/close task and its one GET-only refresh completed before the native result
    /// controller published its authoritative transition snapshot. The pending battle contract
    /// remains live, but no second request is issued while the result-owned Code Offer appears.
    /// </summary>
    AwaitingResultView,
    Settled,
    Unchanged,
    WrongTarget,
    ProjectionUnknown,
    ProjectionDrift,
    BindingUnavailable,
    Faulted,
    Canceled,
    SceneLost,
}

internal readonly record struct NetherBattleSettlementStep(
    NetherBattleSettlementStepKind Kind,
    NetherActionOutcome Outcome,
    NetherSnapshot? Snapshot,
    string Detail,
    NetherPauseReason PauseReason = NetherPauseReason.None
)
{
    public static NetherBattleSettlementStep Create(
        NetherBattleSettlementStepKind kind,
        NetherActionOutcome outcome = NetherActionOutcome.Ambiguous,
        NetherSnapshot? snapshot = null,
        string detail = "",
        NetherPauseReason pauseReason = NetherPauseReason.None
    ) => new(kind, outcome, snapshot, detail, pauseReason);
}

internal sealed class NetherBattleSettlementCoordinator
{
    private readonly INetherBattleSettlementDriver _battle;
    private readonly NetherReadOnlyReconcileCoordinator _reconcile;
    private readonly INetherBattleProjectionSnapshotDriver _projectionSnapshot;
    private readonly NetherBattleProjectionCalibration _projectionCalibration = new();
    private NetherPlannedAction? _action;
    private NetherSnapshot? _before;
    private bool _settlementObserved;
    private bool _awaitingResultView;

    public NetherBattleSettlementCoordinator(
        INetherBattleSettlementDriver battle,
        INetherReadOnlyReconcileDriver readOnly,
        INetherBattleProjectionSnapshotDriver projectionSnapshot
    )
    {
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
        _reconcile = new NetherReadOnlyReconcileCoordinator(readOnly ?? throw new ArgumentNullException(nameof(readOnly)));
        _projectionSnapshot = projectionSnapshot ?? throw new ArgumentNullException(nameof(projectionSnapshot));
    }

    public bool IsActive => _action != null;

    /// <summary>
    /// True only after the one permitted post-clear GET proved that the presentation model is
    /// still the entry Battle snapshot. The controller must wait for the result owner, then pass
    /// its fresh datastore-backed transition snapshot to <see cref="SettleFromResultView"/>.
    /// </summary>
    public bool IsAwaitingResultView => _awaitingResultView;

    public bool Begin(NetherPlannedAction action, NetherSnapshot before)
    {
        if (_action != null || before == null || action.Kind != NetherActionKind.BattleSettlement)
            return false;
        NetherBattleSettlementContract? contract = action.BattleSettlement;
        if (contract == null
            || contract.EntryStatus != NetherSessionStatus.Battle
            || before.Status != contract.EntryStatus
            || before.MapId != contract.EntryMapId
            || before.CurrentFloorId != contract.EntryFloorId
            || contract.ExpectedStatus == NetherSessionStatus.Unknown
            || contract.ExpectedMapId <= 0
            || contract.ExpectedFloorId <= 0
            || contract.EntryProjection == null)
        {
            return false;
        }

        _action = action;
        _before = before;
        _settlementObserved = false;
        _awaitingResultView = false;
        _reconcile.Reset();
        return true;
    }

    public NetherBattleSettlementStep Pump()
    {
        if (_action is not NetherPlannedAction action || _before == null)
            return NetherBattleSettlementStep.Create(NetherBattleSettlementStepKind.BindingUnavailable, detail: "missing-battle-settlement-contract");

        if (_awaitingResultView)
        {
            return NetherBattleSettlementStep.Create(
                NetherBattleSettlementStepKind.AwaitingResultView,
                NetherActionOutcome.NotApplied,
                detail: "battle-settlement-unchanged-awaiting-native-result-view"
            );
        }

        if (_settlementObserved)
            return PumpSettlement(action, _before);

        NetherNativeActionResult lifecycle = _battle.PollBattleLifecycle();
        if (lifecycle.Kind == NetherNativeActionResultKind.Started)
            return NetherBattleSettlementStep.Create(NetherBattleSettlementStepKind.AwaitingBattle, detail: lifecycle.Detail);
        if (lifecycle.Kind == NetherNativeActionResultKind.BindingUnavailable)
            return Terminate(NetherBattleSettlementStepKind.BindingUnavailable, detail: lifecycle.Detail);
        if (lifecycle.Kind == NetherNativeActionResultKind.UnknownOutcome)
        {
            return lifecycle.Detail.IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0
                ? Terminate(NetherBattleSettlementStepKind.Canceled, detail: lifecycle.Detail)
                : Terminate(NetherBattleSettlementStepKind.Faulted, detail: lifecycle.Detail);
        }
        if (lifecycle.Kind != NetherNativeActionResultKind.Completed)
            return Terminate(NetherBattleSettlementStepKind.Faulted, detail: lifecycle.Detail);

        if (!_battle.TryConsumeBattleClear() && !_battle.TryConsumeBattleClose())
            return NetherBattleSettlementStep.Create(NetherBattleSettlementStepKind.AwaitingBattle, detail: "battle-parent-not-settled");

        _settlementObserved = true;
        return NetherBattleSettlementStep.Create(NetherBattleSettlementStepKind.AwaitingSettlement, detail: "battle-parent-terminal-observed");
    }

    public NetherBattleSettlementStep TerminateForSceneLoss() =>
        Terminate(NetherBattleSettlementStepKind.SceneLost, detail: "nether-battle-scene-lost");

    /// <summary>
    /// Settles the retained battle contract from the exact result-owned transition snapshot.
    /// This path is used only after the fresh result controller has registered; it never starts
    /// another request and runs before a result-owned Code Offer can mutate the Code portfolio.
    /// </summary>
    public NetherBattleSettlementStep SettleFromResultView(NetherSnapshot? snapshot)
    {
        if (!_awaitingResultView
            || _action is not NetherPlannedAction action
            || _before == null)
        {
            return NetherBattleSettlementStep.Create(
                NetherBattleSettlementStepKind.BindingUnavailable,
                detail: "missing-deferred-battle-settlement-contract"
            );
        }
        if (snapshot == null)
        {
            return NetherBattleSettlementStep.Create(
                NetherBattleSettlementStepKind.AwaitingResultView,
                NetherActionOutcome.NotApplied,
                detail: "missing-result-owned-settlement-snapshot"
            );
        }

        _awaitingResultView = false;
        NetherActionOutcome outcome = NetherActionReconcilePolicy.Evaluate(action, _before, snapshot);
        return outcome switch
        {
            NetherActionOutcome.Applied => SettleAuthoritativeProjection(action, _before, snapshot, outcome),
            NetherActionOutcome.NotApplied => Terminate(NetherBattleSettlementStepKind.Unchanged, outcome, snapshot),
            _ => Terminate(NetherBattleSettlementStepKind.WrongTarget, outcome, snapshot),
        };
    }

    private NetherBattleSettlementStep PumpSettlement(NetherPlannedAction action, NetherSnapshot before)
    {
        NetherReadOnlyReconcileStep refresh = _reconcile.Pump();
        if (refresh.Kind == NetherReadOnlyReconcileStepKind.Pending)
            return NetherBattleSettlementStep.Create(NetherBattleSettlementStepKind.AwaitingSettlement, detail: refresh.Detail);
        if (refresh.Kind == NetherReadOnlyReconcileStepKind.BindingUnavailable)
            return Terminate(NetherBattleSettlementStepKind.BindingUnavailable, detail: refresh.Detail);
        if (refresh.Kind != NetherReadOnlyReconcileStepKind.Applied || refresh.Snapshot == null)
            return Terminate(NetherBattleSettlementStepKind.Faulted, detail: refresh.Detail);

        NetherActionOutcome outcome = NetherActionReconcilePolicy.Evaluate(action, before, refresh.Snapshot);
        return outcome switch
        {
            NetherActionOutcome.Applied => SettleAuthoritativeProjection(action, before, refresh.Snapshot, outcome),
            // A real battle result is published asynchronously after the clear/close task. A
            // byte-for-byte unchanged Battle snapshot therefore proves only that this GET won
            // the race. Keep the immutable contract until the result-owned transition snapshot
            // exists; never replay the battle or issue a speculative second refresh.
            NetherActionOutcome.NotApplied => AwaitResultView(refresh.Snapshot),
            _ => Terminate(NetherBattleSettlementStepKind.WrongTarget, outcome, refresh.Snapshot),
        };
    }

    private NetherBattleSettlementStep AwaitResultView(NetherSnapshot preResultSnapshot)
    {
        _awaitingResultView = true;
        _reconcile.Reset();
        return NetherBattleSettlementStep.Create(
            NetherBattleSettlementStepKind.AwaitingResultView,
            NetherActionOutcome.NotApplied,
            preResultSnapshot,
            "battle-settlement-unchanged-awaiting-native-result-view"
        );
    }

    private NetherBattleSettlementStep SettleAuthoritativeProjection(
        NetherPlannedAction action,
        NetherSnapshot before,
        NetherSnapshot after,
        NetherActionOutcome outcome
    )
    {
        NetherBattleProjectionCalibrationObservation calibration = _projectionCalibration.Observe(
            action.BattleSettlement,
            before,
            after,
            _projectionSnapshot.TryCaptureActiveCodeErosionProjection()
        );
        if (calibration.IsAccepted)
        {
            return Terminate(
                NetherBattleSettlementStepKind.Settled,
                outcome,
                after,
                calibration.Detail
            );
        }

        NetherBattleSettlementStepKind kind = calibration.PauseReason == NetherPauseReason.BattleProjectionDrift
            ? NetherBattleSettlementStepKind.ProjectionDrift
            : NetherBattleSettlementStepKind.ProjectionUnknown;
        return Terminate(kind, outcome, after, calibration.Detail, calibration.PauseReason);
    }

    private NetherBattleSettlementStep Terminate(
        NetherBattleSettlementStepKind kind,
        NetherActionOutcome outcome = NetherActionOutcome.Ambiguous,
        NetherSnapshot? snapshot = null,
        string detail = "",
        NetherPauseReason pauseReason = NetherPauseReason.None
    )
    {
        _action = null;
        _before = null;
        _settlementObserved = false;
        _awaitingResultView = false;
        _reconcile.Reset();
        return NetherBattleSettlementStep.Create(kind, outcome, snapshot, detail, pauseReason);
    }
}
