#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Narrow production seam for the destructive boundary after a one-ticket Sleep continuation.
/// The native Continue parent must either finish or be superseded by its exact scene transition.
/// Its FloorSelection owner must disappear, and a strictly newer NetherTop runtime must register
/// before one GET-only refresh is allowed.  Registration can precede the new controller's model
/// injection, so that exact post-GET snapshot gap is also awaited for a bounded period.
/// Nothing in this component starts, resumes, or repeats a Nether action.
/// </summary>
internal interface INetherContinueSceneDriver : INetherReadOnlyReconcileDriver,
    INetherFloorSceneReadinessDriver
{
    /// <summary>Polls only the already-started native Continue parent task.</summary>
    NetherNativeActionResult PollContinueParent();

    /// <summary>
    /// Exact lifecycle evidence for the FloorSelection owner that initiated Continue.  This is
    /// deliberately separate from an incidental absence of a UI controller.
    /// </summary>
    bool FloorOwnerTerminated { get; }

    /// <summary>
    /// Monotonically increasing registration generation for a Nether runtime.  Zero means no
    /// replacement runtime has registered yet.
    /// </summary>
    long CurrentRuntimeGeneration { get; }

    /// <summary>True only for the expected NetherTop/new-segment scene binding.</summary>
    bool IsExpectedNetherTopScene { get; }
}

/// <summary>
/// Immutable postcondition required after a one-ticket continuation. Positive map/floor IDs are
/// an exact packaged-master prediction; a zero/zero pair means that the Continue endpoint assigns
/// the destination. Server-assigned identifiers may be reused across paid segment boundaries, so
/// the strictly newer, fully-entered authoritative scene is the identity proof in that case.
/// ExpectedSegmentFloorLevel is the completed checkpoint floor: choosing the first floor in the
/// new segment is a later, independent mutation.
/// </summary>
internal readonly record struct NetherContinueSceneContract(
    long ExpectedMapId,
    long ExpectedFloorId,
    int ExpectedSegmentFloorLevel,
    int TicketCost,
    NetherSessionStatus ExpectedStatus
);

internal enum NetherContinueSceneStepKind
{
    WaitForTeardown,
    WaitForRebind,
    Reconcile,
    Complete,
    Pause,
}

internal readonly record struct NetherContinueSceneStep(
    NetherContinueSceneStepKind Kind,
    NetherSnapshot? Snapshot,
    string Detail
)
{
    public static NetherContinueSceneStep WaitForTeardown(string detail) =>
        new(NetherContinueSceneStepKind.WaitForTeardown, null, detail);

    public static NetherContinueSceneStep WaitForRebind(string detail) =>
        new(NetherContinueSceneStepKind.WaitForRebind, null, detail);

    public static NetherContinueSceneStep Reconcile(string detail) =>
        new(NetherContinueSceneStepKind.Reconcile, null, detail);

    public static NetherContinueSceneStep Complete(NetherSnapshot snapshot, string detail) =>
        new(NetherContinueSceneStepKind.Complete, snapshot, detail);

    public static NetherContinueSceneStep Pause(string detail) =>
        new(NetherContinueSceneStepKind.Pause, null, detail);
}

/// <summary>
/// Owns the Continue post-parent scene handoff.  Its terminal step is cached, so polling it
/// after success/failure cannot issue a second GET or accidentally turn a stable observation
/// into another mutation.
/// </summary>
internal sealed class NetherContinueSceneCoordinator
{
    private enum Stage
    {
        Idle,
        AwaitingParent,
        AwaitingTeardown,
        AwaitingRebind,
        Reconciling,
        AwaitingAppliedSnapshot,
        Terminal,
    }

    private const string MissingNetherModelSnapshot = "missing-floor-selection-nether-model";
    private const string MissingNetherModelRefresh =
        "read-only-refresh-snapshot:" + MissingNetherModelSnapshot;

    private readonly INetherContinueSceneDriver _driver;
    private readonly NetherReadOnlyReconcileCoordinator _reconcile;
    private readonly NetherNativeWaitGate _teardownWait;
    private readonly NetherNativeWaitGate _rebindWait;
    private readonly NetherNativeWaitGate _snapshotWait;
    private Stage _stage;
    private NetherContinueSceneContract _contract;
    private NetherSnapshot? _before;
    private long _ownerGeneration;
    private bool _parentTerminalObserved;
    private NetherContinueSceneStep? _terminal;

    public NetherContinueSceneCoordinator(
        INetherContinueSceneDriver driver,
        int maximumMissingTicks = 600
    )
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _reconcile = new NetherReadOnlyReconcileCoordinator(driver);
        _teardownWait = new NetherNativeWaitGate(maximumMissingTicks);
        _rebindWait = new NetherNativeWaitGate(maximumMissingTicks);
        _snapshotWait = new NetherNativeWaitGate(maximumMissingTicks);
    }

    public bool IsActive => _stage is not (Stage.Idle or Stage.Terminal);

    /// <summary>
    /// The native Continue parent has reached a terminal state, or its exact teardown/rebind has
    /// proved that native execution already crossed that boundary.  This is the precise point at
    /// which the controller may enter its explicit handoff phase.
    /// </summary>
    public bool ParentTerminalObserved => _parentTerminalObserved;

    /// <summary>
    /// Captures the immutable pre-mutation evidence before the native Continue parent is
    /// invoked.  Invalid/missing target information cannot be deferred into a mutation.
    /// </summary>
    public bool Begin(
        NetherContinueSceneContract contract,
        NetherSnapshot before,
        long ownerGeneration
    )
    {
        if (IsActive || before == null || !IsValidContract(contract, before, ownerGeneration))
            return false;

        _contract = contract;
        _before = before;
        _ownerGeneration = ownerGeneration;
        _parentTerminalObserved = false;
        _terminal = null;
        _teardownWait.Clear();
        _rebindWait.Clear();
        _snapshotWait.Clear();
        _reconcile.Reset();
        _stage = Stage.AwaitingParent;
        return true;
    }

    public NetherContinueSceneStep Pump()
    {
        if (_terminal is NetherContinueSceneStep terminal)
            return terminal;
        if (_before == null)
            return NetherContinueSceneStep.Pause("continue-scene-not-started");

        return _stage switch
        {
            Stage.AwaitingParent => PumpParent(),
            Stage.AwaitingTeardown => PumpTeardown(),
            Stage.AwaitingRebind => PumpRebind(),
            Stage.Reconciling => PumpReconcile(),
            Stage.AwaitingAppliedSnapshot => PumpAppliedSnapshot(),
            _ => TerminalPause("continue-scene-invalid-stage:" + _stage),
        };
    }

    public void Reset()
    {
        _stage = Stage.Idle;
        _before = null;
        _ownerGeneration = 0;
        _parentTerminalObserved = false;
        _terminal = null;
        _teardownWait.Clear();
        _rebindWait.Clear();
        _snapshotWait.Clear();
        _reconcile.Reset();
    }

    private NetherContinueSceneStep PumpParent()
    {
        if (TryObserveExactSceneTransition(out long generation))
        {
            _parentTerminalObserved = true;
            _stage = Stage.AwaitingTeardown;
            return NetherContinueSceneStep.WaitForTeardown(
                "continue-parent-settled-by-scene-transition:generation=" + generation
            );
        }

        NetherNativeActionResult parent = _driver.PollContinueParent();
        if (parent.Kind == NetherNativeActionResultKind.Started)
            return NetherContinueSceneStep.WaitForTeardown("continue-parent-pending:" + parent.Detail);
        if (parent.Kind == NetherNativeActionResultKind.Completed)
        {
            _parentTerminalObserved = true;
            _stage = Stage.AwaitingTeardown;
            return NetherContinueSceneStep.WaitForTeardown("continue-parent-terminal:" + parent.Detail);
        }

        bool parentCanceled = parent.Kind == NetherNativeActionResultKind.UnknownOutcome
            && parent.Detail.IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0;
        bool ownerTeardownFaulted = parent.Kind == NetherNativeActionResultKind.UnknownOutcome
            && _driver.FloorOwnerTerminated
            // The generated MoveNext postfix distinguishes a real builder exception from this
            // exact source-less terminal observation. The latter occurred in the live log after
            // the old owner was destroyed, while native Continue had already been submitted.
            && string.Equals(
                parent.Detail,
                "native-start-status-terminal-faulted",
                StringComparison.Ordinal
            );
        if (parentCanceled || ownerTeardownFaulted)
        {
            // Fresh native ISIL shows HandleGameClearedIfNeededAsync submits
            // RequestNetherContinueAsync and later awaits WaitWhile with the old
            // SubViewController's GetCancellationTokenOnDestroy token. The observed parent may
            // therefore be Canceled, or report this exact pooled terminal Faulted status, after
            // the owner transition. Neither observation is settlement: require the existing
            // newer-controller, matching OnEntered, authoritative-snapshot, and exact one-ticket
            // postcondition gates before a GET-only reconciliation.
            if (_driver.FloorOwnerTerminated)
            {
                _parentTerminalObserved = true;
                _teardownWait.ObserveRegistration();
                _stage = Stage.AwaitingRebind;
                return NetherContinueSceneStep.WaitForRebind(
                    "continue-parent-"
                        + (ownerTeardownFaulted ? "faulted" : "canceled")
                        + "-after-owner-transition:"
                        + parent.Detail
                );
            }
            return TerminalPause("continue-parent-canceled:" + parent.Detail);
        }

        return parent.Kind == NetherNativeActionResultKind.BindingUnavailable
            ? TerminalPause("continue-parent-binding:" + parent.Detail)
            : TerminalPause("continue-parent-fault:" + parent.Detail);
    }

    private bool TryObserveExactSceneTransition(out long generation)
    {
        NetherFloorSceneSnapshotResult ready =
            _driver.TryCaptureReadyFloorSceneSnapshot(_ownerGeneration);
        generation = ready.RuntimeGeneration;
        return _driver.FloorOwnerTerminated && ready.IsReady;
    }

    private NetherContinueSceneStep PumpTeardown()
    {
        if (_driver.FloorOwnerTerminated)
        {
            _teardownWait.ObserveRegistration();
            _stage = Stage.AwaitingRebind;
            return NetherContinueSceneStep.WaitForRebind("continue-floor-owner-terminated");
        }

        NetherNativeActionResult wait = _teardownWait.AwaitRegistration("continue-floor-teardown");
        return wait.Kind == NetherNativeActionResultKind.Started
            ? NetherContinueSceneStep.WaitForTeardown(wait.Detail)
            : TerminalPause("continue-floor-teardown-timeout:" + wait.Detail);
    }

    private NetherContinueSceneStep PumpRebind()
    {
        long generation = _driver.CurrentRuntimeGeneration;
        if (generation == 0)
        {
            NetherNativeActionResult wait = _rebindWait.AwaitRegistration("continue-runtime-rebind");
            return wait.Kind == NetherNativeActionResultKind.Started
                ? NetherContinueSceneStep.WaitForRebind(wait.Detail)
                : TerminalPause("continue-runtime-rebind-timeout:" + wait.Detail);
        }
        if (generation <= _ownerGeneration)
        {
            return TerminalPause(
                "continue-runtime-rebind-wrong-generation:owner=" + _ownerGeneration + ":observed=" + generation
            );
        }
        if (!_driver.IsExpectedNetherTopScene)
            return TerminalPause("continue-runtime-rebind-wrong-scene");

        NetherFloorSceneSnapshotResult ready =
            _driver.TryCaptureReadyFloorSceneSnapshot(_ownerGeneration);
        if (!ready.IsReady)
        {
            NetherNativeActionResult wait = _rebindWait.AwaitRegistration(
                "continue-runtime-readiness"
            );
            return wait.Kind == NetherNativeActionResultKind.Started
                ? NetherContinueSceneStep.WaitForRebind(
                    "continue-runtime-readiness:" + ready.Detail
                )
                : TerminalPause(
                    "continue-runtime-readiness-timeout:"
                        + wait.Detail
                        + ":"
                        + ready.Detail
                );
        }

        _rebindWait.ObserveRegistration();
        _stage = Stage.Reconciling;
        return NetherContinueSceneStep.Reconcile(
            "continue-runtime-rebound:" + ready.RuntimeGeneration
        );
    }

    private NetherContinueSceneStep PumpReconcile()
    {
        NetherReadOnlyReconcileStep refresh = _reconcile.Pump();
        if (refresh.Kind == NetherReadOnlyReconcileStepKind.Pending)
            return NetherContinueSceneStep.Reconcile("continue-read-only-refresh-pending:" + refresh.Detail);
        if (refresh.Kind == NetherReadOnlyReconcileStepKind.BindingUnavailable)
        {
            if (string.Equals(refresh.Detail, MissingNetherModelRefresh, StringComparison.Ordinal))
            {
                _stage = Stage.AwaitingAppliedSnapshot;
                return AwaitAppliedSnapshot();
            }
            return TerminalPause("continue-read-only-refresh-binding:" + refresh.Detail);
        }
        if (refresh.Kind != NetherReadOnlyReconcileStepKind.Applied || refresh.Snapshot == null)
            return TerminalPause("continue-read-only-refresh-fault:" + refresh.Detail);

        _stage = Stage.AwaitingAppliedSnapshot;
        return PumpAppliedSnapshot();
    }

    private NetherContinueSceneStep PumpAppliedSnapshot()
    {
        NetherFloorSceneSnapshotResult ready =
            _driver.TryCaptureReadyFloorSceneSnapshot(_ownerGeneration);
        return ready.IsReady
            ? ValidateSettlement(ready.Snapshot!)
            : AwaitAppliedSnapshot(ready.Detail);
    }

    private NetherContinueSceneStep AwaitAppliedSnapshot(
        string detail = MissingNetherModelSnapshot
    )
    {
        NetherNativeActionResult wait = _snapshotWait.AwaitRegistration(
            "continue-applied-snapshot"
        );
        return wait.Kind == NetherNativeActionResultKind.Started
            ? NetherContinueSceneStep.Reconcile(
                "continue-read-only-refresh-awaiting-snapshot:" + detail
            )
            : TerminalPause(
                "continue-read-only-refresh-snapshot-timeout:"
                    + wait.Detail
                    + ":"
                    + detail
            );
    }

    private NetherContinueSceneStep ValidateSettlement(NetherSnapshot after)
    {
        NetherSnapshot before = _before!;
        if (IsExactSettlement(before, after))
        {
            _snapshotWait.ObserveRegistration();
            return TerminalComplete(after, "continue-settlement-exact");
        }
        if (IsPlausiblePartialPropagation(before, after))
        {
            _stage = Stage.AwaitingAppliedSnapshot;
            return AwaitAppliedSnapshot(
                "awaiting-applied-snapshot:status="
                    + after.Status
                    + ":ticket="
                    + after.TicketCount
                    + ":map="
                    + after.MapId
                    + ":floor="
                    + after.CurrentFloorId
            );
        }

        if (after.TicketCount != before.TicketCount - _contract.TicketCost)
            return TerminalPause("continue-settlement-wrong-ticket");
        if (HasPredictedDestination(_contract))
        {
            if (after.MapId != _contract.ExpectedMapId)
                return TerminalPause("continue-settlement-wrong-map");
            if (after.CurrentFloorId != _contract.ExpectedFloorId)
                return TerminalPause("continue-settlement-wrong-floor");
        }
        else if (after.MapId <= 0 || after.CurrentFloorId <= 0)
        {
            return TerminalPause("continue-settlement-wrong-destination");
        }
        if (after.FloorLevel != _contract.ExpectedSegmentFloorLevel)
            return TerminalPause("continue-settlement-wrong-segment");
        if (after.Status != _contract.ExpectedStatus)
            return TerminalPause("continue-settlement-wrong-status");

        return TerminalPause("continue-settlement-incomplete");
    }

    private bool IsExactSettlement(NetherSnapshot before, NetherSnapshot after)
    {
        bool destinationMatches = HasPredictedDestination(_contract)
            ? after.MapId == _contract.ExpectedMapId
                && after.CurrentFloorId == _contract.ExpectedFloorId
            : after.MapId > 0
                && after.CurrentFloorId > 0;
        return after.TicketCount == before.TicketCount - _contract.TicketCost
            && destinationMatches
            && after.FloorLevel == _contract.ExpectedSegmentFloorLevel
            && after.Status == _contract.ExpectedStatus;
    }

    private bool IsPlausiblePartialPropagation(NetherSnapshot before, NetherSnapshot after)
    {
        int expectedTicket = before.TicketCount - _contract.TicketCost;
        bool ticketCanConverge = after.TicketCount == before.TicketCount
            || after.TicketCount == expectedTicket;
        bool statusCanConverge = after.Status == before.Status
            || after.Status == _contract.ExpectedStatus;
        bool segmentCanConverge = after.FloorLevel == before.FloorLevel
            || after.FloorLevel == _contract.ExpectedSegmentFloorLevel;
        if (!ticketCanConverge || !statusCanConverge || !segmentCanConverge)
            return false;

        if (HasPredictedDestination(_contract))
        {
            bool mapCanConverge = after.MapId == before.MapId
                || after.MapId == _contract.ExpectedMapId;
            bool floorCanConverge = after.CurrentFloorId == before.CurrentFloorId
                || after.CurrentFloorId == _contract.ExpectedFloorId;
            return mapCanConverge && floorCanConverge;
        }

        // TryCaptureReadyFloorSceneSnapshot already proved a strictly newer generation, the
        // current controller, its matching SubScene.OnEntered, and an authoritative snapshot.
        // The server is therefore free to reuse map/floor identifiers while ticket/status fields
        // propagate; positive identifiers are the only destination constraint available here.
        return after.MapId > 0 && after.CurrentFloorId > 0;
    }

    private NetherContinueSceneStep TerminalComplete(NetherSnapshot snapshot, string detail)
    {
        _stage = Stage.Terminal;
        _before = null;
        _snapshotWait.Clear();
        _reconcile.Reset();
        _terminal = NetherContinueSceneStep.Complete(snapshot, detail);
        return _terminal.Value;
    }

    private NetherContinueSceneStep TerminalPause(string detail)
    {
        _stage = Stage.Terminal;
        _before = null;
        _snapshotWait.Clear();
        _reconcile.Reset();
        _terminal = NetherContinueSceneStep.Pause(detail);
        return _terminal.Value;
    }

    private static bool IsValidContract(
        NetherContinueSceneContract contract,
        NetherSnapshot before,
        long ownerGeneration
    ) => HasValidDestinationContract(contract)
        && contract.ExpectedSegmentFloorLevel > 0
        && contract.ExpectedSegmentFloorLevel == before.FloorLevel
        && contract.TicketCost == 1
        && contract.ExpectedStatus != NetherSessionStatus.Unknown
        && before.Status == NetherSessionStatus.Sleep
        && before.TicketCount >= contract.TicketCost
        && ownerGeneration > 0;

    private static bool HasPredictedDestination(NetherContinueSceneContract contract) =>
        contract.ExpectedMapId > 0 && contract.ExpectedFloorId > 0;

    private static bool HasValidDestinationContract(NetherContinueSceneContract contract) =>
        HasPredictedDestination(contract)
        || (contract.ExpectedMapId == 0 && contract.ExpectedFloorId == 0);
}
