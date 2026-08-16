#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Narrow production seam for the destructive boundary after a one-ticket Sleep continuation.
/// The native Continue parent must either finish or be superseded by its exact scene transition.
/// Its FloorSelection owner must disappear, and a strictly newer NetherTop runtime must register
/// before one GET-only refresh is allowed. Registration can precede the new controller's model
/// injection, so the datastore mapping gap is awaited for a bounded period. Once the mutation is
/// authoritative, ownership is retained without retry or timeout until presentation converges.
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

    /// <summary>
    /// Reads the datastore-backed state applied by the completed GET-only refresh.  The rebound
    /// FloorSelection controller can retain its pre-Continue presentation model while this
    /// authoritative state has already advanced.
    /// </summary>
    NetherReadOnlySnapshotResult TryCaptureContinueAppliedSnapshot();
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
    WaitForPresentation,
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

    public static NetherContinueSceneStep WaitForPresentation(string detail) =>
        new(NetherContinueSceneStepKind.WaitForPresentation, null, detail);

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
        AwaitingPresentationConvergence,
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
    private NetherSnapshot? _appliedSettlement;
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
        _appliedSettlement = null;
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
            Stage.AwaitingPresentationConvergence => PumpPresentationConvergence(),
            _ => TerminalPause("continue-scene-invalid-stage:" + _stage),
        };
    }

    public void Reset()
    {
        _stage = Stage.Idle;
        _before = null;
        _appliedSettlement = null;
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

        if (parent.Kind == NetherNativeActionResultKind.UnknownOutcome
            && parent.Detail.IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Current native HandleGameClearedIfNeededAsync changes to the next NetherTop scene
            // and then awaits WaitWhile with the old SubViewController's
            // GetCancellationTokenOnDestroy token. The exact owner teardown therefore cancels
            // its generated HandleStartEventByStatusAsync parent after the transition was
            // submitted. Teardown is not settlement: move only to the existing rebind gate,
            // which still requires a newer controller, matching SubScene.OnEntered, an
            // authoritative snapshot, and the exact one-ticket postcondition before reconcile.
            if (_driver.FloorOwnerTerminated)
            {
                _parentTerminalObserved = true;
                _teardownWait.ObserveRegistration();
                _stage = Stage.AwaitingRebind;
                return NetherContinueSceneStep.WaitForRebind(
                    "continue-parent-canceled-after-owner-transition:" + parent.Detail
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
        if (!ready.IsReady)
            return AwaitAppliedSnapshot(ready.Detail);

        // Ready proves that the exact newer scene/controller still owns this handoff; its
        // presentation snapshot is not settlement authority.  Continue writes the GET response
        // into NetherDataStore before a newly registered controller is guaranteed to rebuild its
        // private NetherModel, so validate and re-poll the datastore-backed transition snapshot.
        NetherReadOnlySnapshotResult applied = _driver.TryCaptureContinueAppliedSnapshot();
        return applied.IsSuccess
            ? ValidateSettlement(applied.Snapshot!, ready.Snapshot!)
            : AwaitAppliedSnapshot("continue-transition-snapshot:" + applied.Detail);
    }

    private NetherContinueSceneStep PumpPresentationConvergence()
    {
        if (_appliedSettlement == null)
            return TerminalPause("continue-presentation-fence-missing-settlement");

        NetherFloorSceneSnapshotResult ready =
            _driver.TryCaptureReadyFloorSceneSnapshot(_ownerGeneration);
        if (!ready.IsReady)
        {
            return NetherContinueSceneStep.WaitForPresentation(
                "continue-settlement-applied-awaiting-presentation:" + ready.Detail
            );
        }

        return CompleteWhenPresentationConverges(ready.Snapshot!);
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

    private NetherContinueSceneStep ValidateSettlement(
        NetherSnapshot after,
        NetherSnapshot presentation
    )
    {
        NetherSnapshot before = _before!;
        if (IsExactSettlement(before, after))
        {
            _snapshotWait.ObserveRegistration();
            _appliedSettlement = after;
            _stage = Stage.AwaitingPresentationConvergence;
            return CompleteWhenPresentationConverges(presentation);
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
        else if (!HasValidServerAssignedDestination(after))
        {
            return TerminalPause("continue-settlement-wrong-destination");
        }
        if (after.FloorLevel != _contract.ExpectedSegmentFloorLevel)
            return TerminalPause("continue-settlement-wrong-segment");
        if (after.Status != _contract.ExpectedStatus)
            return TerminalPause("continue-settlement-wrong-status");

        return TerminalPause("continue-settlement-incomplete");
    }

    private NetherContinueSceneStep CompleteWhenPresentationConverges(
        NetherSnapshot presentation
    )
    {
        NetherSnapshot applied = _appliedSettlement!;
        if (HasConvergedPresentation(applied, presentation))
        {
            return TerminalComplete(
                presentation,
                "continue-settlement-exact:presentation-converged"
            );
        }

        // The paid mutation is already proven by the datastore. Keep the original action owner
        // without another GET, timeout, or native invocation until the rebound controller stops
        // presenting the pre-Continue Sleep model. Releasing it early would plan Continue twice.
        return NetherContinueSceneStep.WaitForPresentation(
            "continue-settlement-applied-awaiting-presentation:status="
                + presentation.Status
                + ":map=" + presentation.MapId
                + ":floor=" + presentation.CurrentFloorId
                + ":level=" + presentation.FloorLevel
                + ":api-index=" + presentation.FloorIndex
                + ":ticket=" + presentation.TicketCount
        );
    }

    private static bool HasConvergedPresentation(
        NetherSnapshot applied,
        NetherSnapshot presentation
    )
    {
        bool floorIdentityConverged = applied.CurrentFloorId > 0
            ? presentation.CurrentFloorId == applied.CurrentFloorId
            : HasResolvedCoordinateOnlyPresentation(applied, presentation);
        return presentation.NetherId == applied.NetherId
            && presentation.Status == applied.Status
            && presentation.MapId == applied.MapId
            && floorIdentityConverged
            && presentation.FloorLevel == applied.FloorLevel
            && presentation.FloorIndex == applied.FloorIndex
            && presentation.ErosionPoint == applied.ErosionPoint
            && presentation.TreasureKeyCount == applied.TreasureKeyCount
            && presentation.NetherGold == applied.NetherGold
            && presentation.TicketCount == applied.TicketCount;
    }

    private static bool HasResolvedCoordinateOnlyPresentation(
        NetherSnapshot applied,
        NetherSnapshot presentation
    )
    {
        if (!IsCoordinateOnlyPlayEntry(applied)
            || presentation.CurrentFloorId <= 0
            || presentation.CurrentNodeId <= 0
            || presentation.Floors.Count == 0)
        {
            return false;
        }

        int matchingCurrentNodes = 0;
        foreach (NetherFloorNode floor in presentation.Floors)
        {
            if (floor.NodeId != presentation.CurrentNodeId)
                continue;
            matchingCurrentNodes++;
            if (floor.FloorId != presentation.CurrentFloorId
                || floor.FloorLevel != presentation.FloorLevel
                || floor.ApiFloorIndex != presentation.FloorIndex)
            {
                return false;
            }
        }
        return matchingCurrentNodes == 1;
    }

    private bool IsExactSettlement(NetherSnapshot before, NetherSnapshot after)
    {
        bool destinationMatches = HasPredictedDestination(_contract)
            ? after.MapId == _contract.ExpectedMapId
                && after.CurrentFloorId == _contract.ExpectedFloorId
            : HasValidServerAssignedDestination(after);
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
        // propagate. A Play response can also be the coordinate-only new-segment boundary before
        // any node is selected; the Continue-specific cache proves that narrow zero-ID shape.
        return HasValidServerAssignedDestination(after);
    }

    private NetherContinueSceneStep TerminalComplete(NetherSnapshot snapshot, string detail)
    {
        _stage = Stage.Terminal;
        _before = null;
        _appliedSettlement = null;
        _snapshotWait.Clear();
        _reconcile.Reset();
        _terminal = NetherContinueSceneStep.Complete(snapshot, detail);
        return _terminal.Value;
    }

    private NetherContinueSceneStep TerminalPause(string detail)
    {
        _stage = Stage.Terminal;
        _before = null;
        _appliedSettlement = null;
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

    private static bool HasValidServerAssignedDestination(NetherSnapshot snapshot) =>
        snapshot.MapId > 0
        && (snapshot.CurrentFloorId > 0 || IsCoordinateOnlyPlayEntry(snapshot));

    private static bool IsCoordinateOnlyPlayEntry(NetherSnapshot snapshot) =>
        snapshot.Status == NetherSessionStatus.Play
        && snapshot.CurrentFloorId == 0
        && snapshot.CurrentNodeId == 0
        && snapshot.FloorLevel > 0
        && snapshot.FloorIndex >= 0
        && snapshot.Floors.Count == 0;

    private static bool HasValidDestinationContract(NetherContinueSceneContract contract) =>
        HasPredictedDestination(contract)
        || (contract.ExpectedMapId == 0 && contract.ExpectedFloorId == 0);
}
