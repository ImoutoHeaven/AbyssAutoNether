#nullable enable

using System;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Exact runtime seam for a code offer opened by
/// FloorSelection.HandleStartEventByStatusAsync before F12 owns a floor click.  The parent
/// task is the game's existing resume/start-status sequence; automation may drive only its
/// owned code child and a final GET-only datastore refresh.
/// </summary>
internal interface INetherRecoveredCodeOfferDriver
{
    bool HasRecoveredCodeOffer => false;

    NetherRuntimeSnapshotResult TryCaptureRecoveredCodeSnapshot() =>
        NetherRuntimeSnapshotResult.Failure("recovered-code-driver-unavailable");

    NetherRuntimeCodeCandidatesResult TryGetRecoveredCodeCandidates() =>
        NetherRuntimeCodeCandidatesResult.Failure("recovered-code-driver-unavailable");

    NetherRuntimePopupResult TryGetRecoveredCodePopup() =>
        NetherRuntimePopupResult.Failure("recovered-code-driver-unavailable");

    NetherRuntimeCodePolicyEvidenceResult TryCaptureRecoveredCodePolicyEvidence(
        NetherSnapshot snapshot,
        NetherRuntimeCodeCandidatesResult candidates,
        NetherAutoClimbSettings settings
    ) => NetherRuntimeCodePolicyEvidenceResult.Failure("recovered-code-policy-evidence-unavailable");

    NetherNativeActionResult InvokeRecoveredCode(
        NetherRuntimePopupContext popup,
        NetherPlannedAction action
    ) => NetherNativeActionResult.BindingUnavailable("recovered-code-driver-unavailable");

    NetherBattleResultCodeNativeStep PollRecoveredCodeNative() =>
        NetherBattleResultCodeNativeStep.BindingUnavailable("recovered-code-driver-unavailable");

    NetherNativeActionResult PollRecoveredCodeParent() =>
        NetherNativeActionResult.BindingUnavailable("recovered-code-driver-unavailable");

    NetherRecoveredCheckpointObservation ObserveRecoveredCheckpoint() =>
        NetherRecoveredCheckpointObservation.NotObserved(
            "existing-checkpoint-parent-not-observed"
        );

    NetherNativeActionResult PrepareRecoveredCheckpointHandoff() =>
        NetherNativeActionResult.BindingUnavailable(
            "recovered-checkpoint-handoff-driver-unavailable"
        );

    NetherNativeActionResult BeginRecoveredCodeRefresh() =>
        NetherNativeActionResult.BindingUnavailable("recovered-code-driver-unavailable");

    NetherNativeActionResult PollRecoveredCodeRefresh() =>
        NetherNativeActionResult.BindingUnavailable("recovered-code-driver-unavailable");

    NetherRuntimeSnapshotResult TryCaptureRecoveredCodeAppliedSnapshot() =>
        NetherRuntimeSnapshotResult.Failure("recovered-code-driver-unavailable");

    void CompleteRecoveredCodeOffer() { }
}

internal enum NetherRecoveredCheckpointObservationKind
{
    NotObserved,
    Waiting,
    Ready,
    BindingUnavailable,
}

/// <summary>
/// Read-only evidence that a recovered HandleStartEventByStatusAsync parent has advanced past
/// its Code child into the native Sleep checkpoint UI.  Ready requires both the authoritative
/// Sleep snapshot and the already-open Continue popup owned by that same still-pending parent.
/// </summary>
internal readonly record struct NetherRecoveredCheckpointObservation(
    NetherRecoveredCheckpointObservationKind Kind,
    NetherSnapshot? Snapshot,
    string Detail
)
{
    public static NetherRecoveredCheckpointObservation NotObserved(string detail) =>
        new(NetherRecoveredCheckpointObservationKind.NotObserved, null, detail);

    public static NetherRecoveredCheckpointObservation Waiting(string detail) =>
        new(NetherRecoveredCheckpointObservationKind.Waiting, null, detail);

    public static NetherRecoveredCheckpointObservation Ready(
        NetherSnapshot snapshot,
        string detail
    ) => new(NetherRecoveredCheckpointObservationKind.Ready, snapshot, detail);

    public static NetherRecoveredCheckpointObservation BindingUnavailable(string detail) =>
        new(NetherRecoveredCheckpointObservationKind.BindingUnavailable, null, detail);
}

internal enum NetherRecoveredCodeOfferStepKind
{
    NoOffer,
    AwaitingPopup,
    AwaitingSnapshot,
    AwaitingNative,
    ReloadReady,
    AwaitingParent,
    CheckpointReady,
    AwaitingRefresh,
    Completed,
    CanceledBeforeInvoke,
    CanceledAfterDrain,
    BindingUnavailable,
    Faulted,
}

internal readonly record struct NetherRecoveredCodeOfferStep(
    NetherRecoveredCodeOfferStepKind Kind,
    string Detail,
    NetherSnapshot? Snapshot = null,
    NetherCombatLane? LockedLane = null,
    NetherPlannedAction? Action = null,
    NetherRecoveredCodeReconcileDiagnostic? ReconcileDiagnostic = null,
    NetherAutoClimbSettings? CapturedSettings = null
);

internal readonly record struct NetherRecoveredCodeReconcileDiagnostic(
    NetherActionOutcome Outcome,
    NetherActionKind ActionKind,
    long TargetCodeId,
    long ReplaceCodeId,
    int ReloadActions,
    int BeforeReloadCount,
    int ExpectedReloadCount,
    int AfterReloadCount,
    bool TargetPresentBefore,
    bool TargetPresentAfter,
    bool ReplacementPresentBefore,
    bool ReplacementPresentAfter,
    bool PortfolioUnchanged,
    string BeforeCodeIds,
    string AfterCodeIds,
    string Reason
);

/// <summary>
/// Serializes one recovered foreground code offer as:
/// code child terminal → original HandleStartEventByStatusAsync terminal → one GET-only sync.
/// It deliberately does not enter the ordinary SelectFloor transaction state, because this
/// native parent predates F12 and has its own exact Harmony-captured UniTask.
/// </summary>
internal sealed class NetherRecoveredCodeOfferCoordinator
{
    private enum Stage
    {
        Idle,
        Code,
        Parent,
        Refresh,
    }

    private sealed class CodeDriverAdapter : INetherBattleResultCodeDriver
    {
        public INetherRecoveredCodeOfferDriver? Driver { get; set; }

        private INetherRecoveredCodeOfferDriver Current => Driver
            ?? throw new InvalidOperationException("missing-recovered-code-driver");

        public NetherRuntimeSnapshotResult TryCaptureBattleResultCodeSnapshot() =>
            Current.TryCaptureRecoveredCodeSnapshot();

        public NetherRuntimeCodeCandidatesResult TryGetCodeCandidates() =>
            Current.TryGetRecoveredCodeCandidates();

        public NetherRuntimePopupResult TryGetBattleResultCodePopup() =>
            Current.TryGetRecoveredCodePopup();

        public NetherRuntimeCodePolicyEvidenceResult TryCaptureCodePolicyEvidence(
            NetherSnapshot snapshot,
            NetherRuntimeCodeCandidatesResult candidates,
            NetherAutoClimbSettings settings
        ) => Current.TryCaptureRecoveredCodePolicyEvidence(snapshot, candidates, settings);

        public NetherNativeActionResult InvokeBattleResultCode(
            NetherRuntimePopupContext popup,
            NetherPlannedAction action
        ) => Current.InvokeRecoveredCode(popup, action);

        public NetherBattleResultCodeNativeStep PollBattleResultCodeNative() =>
            Current.PollRecoveredCodeNative();
    }

    private readonly CodeDriverAdapter _adapter = new();
    private readonly NetherBattleResultCodeCoordinator _codeFlow;
    private readonly NetherNativeWaitGate _checkpointWait;
    private Stage _stage;
    private bool _mutationStarted;
    private bool _cancelAfterDrain;
    private bool _reconcileUnknownParent;
    private int _reloadActions;
    private NetherCombatLane? _lockedLane;
    private NetherAutoClimbSettings? _settings;
    private NetherSnapshot? _beforeMutation;
    private NetherPlannedAction? _terminalAction;

    public NetherRecoveredCodeOfferCoordinator(int maximumPopupPolls = 600)
    {
        _codeFlow = new NetherBattleResultCodeCoordinator(
            maximumPopupPolls,
            NetherActionKind.RecoveredCodeOffer
        );
        _checkpointWait = new NetherNativeWaitGate(maximumPopupPolls);
    }

    public bool IsActive => _stage != Stage.Idle;

    public bool HasMutationInFlight => _mutationStarted;

    public NetherRecoveredCodeOfferStep Pump(
        INetherRecoveredCodeOfferDriver driver,
        NetherAutoClimbSettings settings,
        NetherCombatLane? lockedLane,
        bool allowInvoke
    )
    {
        if (driver == null)
            throw new ArgumentNullException(nameof(driver));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        if (_stage == Stage.Idle)
        {
            if (!driver.HasRecoveredCodeOffer)
                return new(NetherRecoveredCodeOfferStepKind.NoOffer, "no-recovered-code-owner");
            _adapter.Driver = driver;
            _lockedLane = lockedLane;
            _settings = settings with { };
            NetherRuntimeSnapshotResult before = driver.TryCaptureRecoveredCodeSnapshot();
            if (!before.IsSuccess || before.Snapshot == null)
            {
                return Terminate(
                    NetherRecoveredCodeOfferStepKind.BindingUnavailable,
                    "recovered-code-before-snapshot:" + before.Detail
                );
            }
            _beforeMutation = before.Snapshot;
            _stage = Stage.Code;
        }
        else if (!ReferenceEquals(_adapter.Driver, driver))
        {
            return Terminate(
                NetherRecoveredCodeOfferStepKind.BindingUnavailable,
                "recovered-code-driver-changed"
            );
        }

        if (!allowInvoke && _mutationStarted)
            _cancelAfterDrain = true;

        switch (_stage)
        {
            case Stage.Code:
                return PumpCode(driver, settings, allowInvoke);
            case Stage.Parent:
                return PumpParent(driver, allowInvoke);
            case Stage.Refresh:
                return PumpRefresh(driver);
            default:
                return Terminate(
                    NetherRecoveredCodeOfferStepKind.Faulted,
                    "invalid-recovered-code-stage"
                );
        }
    }

    public void Reset()
    {
        _codeFlow.Reset();
        _adapter.Driver = null;
        _stage = Stage.Idle;
        _mutationStarted = false;
        _cancelAfterDrain = false;
        _reconcileUnknownParent = false;
        _reloadActions = 0;
        _lockedLane = null;
        _settings = null;
        _beforeMutation = null;
        _terminalAction = null;
        _checkpointWait.Clear();
    }

    private NetherRecoveredCodeOfferStep PumpCode(
        INetherRecoveredCodeOfferDriver driver,
        NetherAutoClimbSettings settings,
        bool allowInvoke
    )
    {
        NetherBattleResultCodeStep code = _codeFlow.Pump(
            _adapter,
            _settings ?? settings,
            _lockedLane,
            allowInvoke
        );
        if (code.Action != null)
        {
            _mutationStarted = true;
            NetherPlannedAction observedAction = code.Action.Value;
            if (observedAction.Kind == NetherActionKind.ReloadCode)
                _reloadActions = checked(_reloadActions + 1);
            else if (observedAction.Kind is NetherActionKind.SelectCode or NetherActionKind.KeepCode)
                _terminalAction = observedAction;
        }
        _lockedLane = code.LockedLane ?? _lockedLane;

        switch (code.Kind)
        {
            case NetherBattleResultCodeStepKind.AwaitingPopup:
                return Step(NetherRecoveredCodeOfferStepKind.AwaitingPopup, code);
            case NetherBattleResultCodeStepKind.AwaitingSnapshot:
                return Step(NetherRecoveredCodeOfferStepKind.AwaitingSnapshot, code);
            case NetherBattleResultCodeStepKind.AwaitingNative:
                return Step(NetherRecoveredCodeOfferStepKind.AwaitingNative, code);
            case NetherBattleResultCodeStepKind.ReloadReady:
                return Step(NetherRecoveredCodeOfferStepKind.ReloadReady, code);
            case NetherBattleResultCodeStepKind.Completed:
                _stage = Stage.Parent;
                return Step(
                    NetherRecoveredCodeOfferStepKind.AwaitingParent,
                    code with { Detail = "recovered-code-child-terminal:" + code.Detail }
                );
            case NetherBattleResultCodeStepKind.CanceledBeforeInvoke:
                if (!_mutationStarted)
                {
                    Reset();
                    return new(
                        NetherRecoveredCodeOfferStepKind.CanceledBeforeInvoke,
                        code.Detail
                    );
                }
                _cancelAfterDrain = true;
                _stage = Stage.Parent;
                return Step(
                    NetherRecoveredCodeOfferStepKind.AwaitingParent,
                    code with { Detail = "recovered-code-cancel-draining-parent:" + code.Detail }
                );
            case NetherBattleResultCodeStepKind.BindingUnavailable:
                return Terminate(NetherRecoveredCodeOfferStepKind.BindingUnavailable, code.Detail);
            default:
                return Terminate(NetherRecoveredCodeOfferStepKind.Faulted, code.Detail);
        }
    }

    private NetherRecoveredCodeOfferStep PumpParent(
        INetherRecoveredCodeOfferDriver driver,
        bool allowInvoke
    )
    {
        NetherNativeActionResult parent = driver.PollRecoveredCodeParent();
        if (parent.Kind == NetherNativeActionResultKind.Started)
        {
            NetherRecoveredCheckpointObservation checkpoint =
                driver.ObserveRecoveredCheckpoint();
            if (checkpoint.Kind == NetherRecoveredCheckpointObservationKind.Ready)
            {
                if (checkpoint.Snapshot == null
                    || checkpoint.Snapshot.Status != NetherSessionStatus.Sleep)
                {
                    return Terminate(
                        NetherRecoveredCodeOfferStepKind.BindingUnavailable,
                        "recovered-checkpoint-ready-without-sleep-snapshot"
                    );
                }

                NetherActionOutcome codeOutcome = EvaluateUnknownParentMutation(
                    checkpoint.Snapshot,
                    out NetherRecoveredCodeReconcileDiagnostic diagnostic
                );
                if (codeOutcome != NetherActionOutcome.Applied)
                {
                    return Terminate(
                        NetherRecoveredCodeOfferStepKind.Faulted,
                        "recovered-checkpoint-code-reconcile:"
                            + codeOutcome + ":" + diagnostic.Reason,
                        diagnostic
                    );
                }

                NetherNativeActionResult handoff = driver.PrepareRecoveredCheckpointHandoff();
                if (handoff.Kind != NetherNativeActionResultKind.Completed)
                {
                    return Terminate(
                        handoff.Kind == NetherNativeActionResultKind.BindingUnavailable
                            ? NetherRecoveredCodeOfferStepKind.BindingUnavailable
                            : NetherRecoveredCodeOfferStepKind.Faulted,
                        "recovered-checkpoint-handoff:" + handoff.Detail
                    );
                }

                NetherSnapshot snapshot = checkpoint.Snapshot;
                NetherCombatLane? lane = _lockedLane;
                NetherAutoClimbSettings? capturedSettings = _settings;
                bool canceled = !allowInvoke || _cancelAfterDrain;
                Reset();
                return new(
                    canceled
                        ? NetherRecoveredCodeOfferStepKind.CanceledAfterDrain
                        : NetherRecoveredCodeOfferStepKind.CheckpointReady,
                    (canceled
                        ? "recovered-checkpoint-ready-after-disable:"
                        : "recovered-checkpoint-ready:") + checkpoint.Detail,
                    snapshot,
                    lane,
                    ReconcileDiagnostic: diagnostic,
                    CapturedSettings: capturedSettings
                );
            }
            if (checkpoint.Kind == NetherRecoveredCheckpointObservationKind.BindingUnavailable)
            {
                return Terminate(
                    NetherRecoveredCodeOfferStepKind.BindingUnavailable,
                    "recovered-checkpoint-observation:" + checkpoint.Detail
                );
            }

            NetherNativeActionResult wait = _checkpointWait.AwaitRegistration(
                "recovered-checkpoint-popup"
            );
            if (wait.Kind != NetherNativeActionResultKind.Started)
            {
                return Terminate(
                    NetherRecoveredCodeOfferStepKind.BindingUnavailable,
                    "recovered-checkpoint-wait:" + wait.Detail
                );
            }
            return new(
                NetherRecoveredCodeOfferStepKind.AwaitingParent,
                parent.Detail + ":" + checkpoint.Detail,
                LockedLane: _lockedLane
            );
        }
        if (parent.Kind == NetherNativeActionResultKind.UnknownOutcome
            && _mutationStarted
            && _beforeMutation != null
            && _terminalAction != null)
        {
            // A consumed pooled UniTask can expose Faulted after its synchronous continuation
            // has already closed the code popup.  The child mutation is known to be terminal,
            // so resolve the parent's ambiguous status with the same GET-only authority used
            // by every other non-idempotent Nether action.  The refresh is accepted only when
            // the exact reload delta and terminal Select/Keep portfolio are proven below.
            _reconcileUnknownParent = true;
        }
        else if (parent.Kind != NetherNativeActionResultKind.Completed)
        {
            return Terminate(
                parent.Kind == NetherNativeActionResultKind.BindingUnavailable
                    ? NetherRecoveredCodeOfferStepKind.BindingUnavailable
                    : NetherRecoveredCodeOfferStepKind.Faulted,
                "recovered-code-parent:" + parent.Detail
            );
        }

        NetherNativeActionResult refresh = driver.BeginRecoveredCodeRefresh();
        if (refresh.Kind is not (
                NetherNativeActionResultKind.Started
                or NetherNativeActionResultKind.Completed
            ))
        {
            return Terminate(
                refresh.Kind == NetherNativeActionResultKind.BindingUnavailable
                    ? NetherRecoveredCodeOfferStepKind.BindingUnavailable
                    : NetherRecoveredCodeOfferStepKind.Faulted,
                "recovered-code-refresh-start:" + refresh.Detail
            );
        }
        _stage = Stage.Refresh;
        return new(
            NetherRecoveredCodeOfferStepKind.AwaitingRefresh,
            _reconcileUnknownParent
                ? "recovered-code-parent-unknown:get-reconcile:" + refresh.Detail
                : refresh.Detail,
            LockedLane: _lockedLane
        );
    }

    private NetherRecoveredCodeOfferStep PumpRefresh(INetherRecoveredCodeOfferDriver driver)
    {
        NetherNativeActionResult refresh = driver.PollRecoveredCodeRefresh();
        if (refresh.Kind == NetherNativeActionResultKind.Started)
        {
            return new(
                NetherRecoveredCodeOfferStepKind.AwaitingRefresh,
                refresh.Detail,
                LockedLane: _lockedLane
            );
        }
        if (refresh.Kind != NetherNativeActionResultKind.Completed)
        {
            return Terminate(
                refresh.Kind == NetherNativeActionResultKind.BindingUnavailable
                    ? NetherRecoveredCodeOfferStepKind.BindingUnavailable
                    : NetherRecoveredCodeOfferStepKind.Faulted,
                "recovered-code-refresh:" + refresh.Detail
            );
        }

        NetherRuntimeSnapshotResult snapshot = driver.TryCaptureRecoveredCodeAppliedSnapshot();
        if (!snapshot.IsSuccess)
        {
            return Terminate(
                NetherRecoveredCodeOfferStepKind.BindingUnavailable,
                "recovered-code-applied-snapshot:" + snapshot.Detail
            );
        }

        NetherRecoveredCodeReconcileDiagnostic? reconcileDiagnostic = null;
        if (_reconcileUnknownParent)
        {
            NetherActionOutcome outcome = EvaluateUnknownParentMutation(
                snapshot.Snapshot!,
                out NetherRecoveredCodeReconcileDiagnostic diagnostic
            );
            reconcileDiagnostic = diagnostic;
            if (outcome != NetherActionOutcome.Applied)
            {
                return Terminate(
                    NetherRecoveredCodeOfferStepKind.Faulted,
                    "recovered-code-reconcile:" + outcome + ":" + diagnostic.Reason,
                    diagnostic
                );
            }
        }

        bool canceled = _cancelAfterDrain;
        NetherCombatLane? lane = _lockedLane;
        driver.CompleteRecoveredCodeOffer();
        Reset();
        return new(
            canceled
                ? NetherRecoveredCodeOfferStepKind.CanceledAfterDrain
                : NetherRecoveredCodeOfferStepKind.Completed,
            canceled ? "recovered-code-drain-completed" : "recovered-code-completed",
            snapshot.Snapshot,
            lane,
            ReconcileDiagnostic: reconcileDiagnostic
        );
    }

    private NetherRecoveredCodeOfferStep Step(
        NetherRecoveredCodeOfferStepKind kind,
        NetherBattleResultCodeStep code
    ) => new(kind, code.Detail, LockedLane: _lockedLane, Action: code.Action);

    private NetherActionOutcome EvaluateUnknownParentMutation(
        NetherSnapshot after,
        out NetherRecoveredCodeReconcileDiagnostic diagnostic
    )
    {
        NetherSnapshot? before = _beforeMutation;
        NetherPlannedAction? terminal = _terminalAction;
        if (before == null || terminal == null || _reloadActions < 0)
        {
            diagnostic = CreateReconcileDiagnostic(
                NetherActionOutcome.Ambiguous,
                before,
                terminal,
                after,
                expectedReloadCount: -1,
                "missing-reconcile-contract"
            );
            return NetherActionOutcome.Ambiguous;
        }

        int expectedReloadCount;
        try
        {
            expectedReloadCount = checked(before.CodeReloadCount - _reloadActions);
        }
        catch (OverflowException)
        {
            diagnostic = CreateReconcileDiagnostic(
                NetherActionOutcome.Ambiguous,
                before,
                terminal,
                after,
                expectedReloadCount: -1,
                "reload-arithmetic-overflow"
            );
            return NetherActionOutcome.Ambiguous;
        }
        if (expectedReloadCount < 0 || after.CodeReloadCount != expectedReloadCount)
        {
            diagnostic = CreateReconcileDiagnostic(
                NetherActionOutcome.Ambiguous,
                before,
                terminal,
                after,
                expectedReloadCount,
                expectedReloadCount < 0 ? "negative-expected-reload" : "reload-count-mismatch"
            );
            return NetherActionOutcome.Ambiguous;
        }

        // SelectCode/KeepCode independently require an unchanged reload count.  Project the
        // already-proven reload consumption into their before snapshot, then reuse the exact
        // portfolio postcondition rather than duplicating a weaker code-ID check here.
        NetherSnapshot terminalBefore = before with
        {
            CodeReloadCount = expectedReloadCount,
        };
        NetherActionOutcome outcome = NetherActionReconcilePolicy.Evaluate(
            terminal.Value,
            terminalBefore,
            after
        );
        diagnostic = CreateReconcileDiagnostic(
            outcome,
            before,
            terminal,
            after,
            expectedReloadCount,
            DescribeReconcileReason(outcome, terminal.Value, before, after)
        );
        return outcome;
    }

    private NetherRecoveredCodeReconcileDiagnostic CreateReconcileDiagnostic(
        NetherActionOutcome outcome,
        NetherSnapshot? before,
        NetherPlannedAction? terminal,
        NetherSnapshot after,
        int expectedReloadCount,
        string reason
    )
    {
        NetherPlannedAction action = terminal ?? new NetherPlannedAction(NetherActionKind.None);
        return new(
            outcome,
            action.Kind,
            action.CodeId,
            action.ReplaceCodeId,
            _reloadActions,
            before?.CodeReloadCount ?? -1,
            expectedReloadCount,
            after.CodeReloadCount,
            before != null && ContainsCode(before, action.CodeId),
            ContainsCode(after, action.CodeId),
            before != null && ContainsCode(before, action.ReplaceCodeId),
            ContainsCode(after, action.ReplaceCodeId),
            before != null && HasSameCodePortfolio(before, after),
            FormatCodeIds(before),
            FormatCodeIds(after),
            reason
        );
    }

    private static string DescribeReconcileReason(
        NetherActionOutcome outcome,
        NetherPlannedAction action,
        NetherSnapshot before,
        NetherSnapshot after
    )
    {
        if (outcome == NetherActionOutcome.Applied)
            return "verified";
        if (action.Kind == NetherActionKind.SelectCode)
        {
            if (action.CodeId <= 0)
                return "invalid-target-code";
            if (ContainsCode(before, action.CodeId))
                return "target-already-present-before";
            if (action.ReplaceCodeId > 0 && !ContainsCode(before, action.ReplaceCodeId))
                return "replacement-missing-before";
            if (!ContainsCode(after, action.CodeId))
                return "target-missing-after";
            if (action.ReplaceCodeId > 0 && ContainsCode(after, action.ReplaceCodeId))
                return "replacement-still-present-after";
        }
        else if (action.Kind == NetherActionKind.KeepCode && !HasSameCodePortfolio(before, after))
        {
            return "kept-portfolio-changed";
        }
        return "policy-" + outcome.ToString().ToLowerInvariant();
    }

    private static bool ContainsCode(NetherSnapshot snapshot, long codeId) =>
        codeId > 0 && snapshot.Codes.Any(code => code.CodeId == codeId);

    private static bool HasSameCodePortfolio(NetherSnapshot before, NetherSnapshot after) =>
        !string.IsNullOrWhiteSpace(before.CodeHash)
        && string.Equals(before.CodeHash, after.CodeHash, StringComparison.Ordinal)
        && string.Equals(CreateCodeIdentity(before), CreateCodeIdentity(after), StringComparison.Ordinal);

    private static string CreateCodeIdentity(NetherSnapshot snapshot) =>
        NetherCodeIdentity.CreatePortfolio(snapshot.Codes);

    private static string FormatCodeIds(NetherSnapshot? snapshot)
    {
        if (snapshot == null || snapshot.Codes.Count == 0)
            return "none";
        long[] ids = snapshot.Codes.Select(code => code.CodeId).OrderBy(id => id).ToArray();
        return string.Join(",", ids);
    }

    private NetherRecoveredCodeOfferStep Terminate(
        NetherRecoveredCodeOfferStepKind kind,
        string detail,
        NetherRecoveredCodeReconcileDiagnostic? reconcileDiagnostic = null
    )
    {
        NetherCombatLane? lane = _lockedLane;
        Reset();
        return new(
            kind,
            detail ?? string.Empty,
            LockedLane: lane,
            ReconcileDiagnostic: reconcileDiagnostic
        );
    }
}
