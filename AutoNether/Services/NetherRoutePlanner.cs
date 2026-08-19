#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal sealed record NetherRouteSafetyContext
{
    /// <summary>Effective target resolved from configuration, server, and master limits.</summary>
    public int MaximumFloorLevel { get; init; } = int.MaxValue;
    public IReadOnlyDictionary<long, int> MinimumWorstCaseErosionToTerminal { get; init; } = new Dictionary<long, int>();
    public IReadOnlyDictionary<long, bool> HpSafeByFloorId { get; init; } = new Dictionary<long, bool>();
    public IReadOnlyDictionary<long, bool> KnownNodeByFloorId { get; init; } = new Dictionary<long, bool>();
    public IReadOnlyDictionary<long, bool> HardSafeByFloorId { get; init; } = new Dictionary<long, bool>();
    public IReadOnlyDictionary<long, int> SafeCodeOpportunityByFloorId { get; init; } = new Dictionary<long, int>();
    public IReadOnlyDictionary<long, int> ProjectedErosionDeltaByFloorId { get; init; } = new Dictionary<long, int>();
    public IReadOnlyDictionary<long, int> ProjectedHpDeltaByFloorId { get; init; } = new Dictionary<long, int>();
    public IReadOnlyDictionary<long, string> UnknownDetailByFloorId { get; init; } = new Dictionary<long, string>();
    /// <summary>
    /// Component-level unknown identity for each route node. The detail string remains diagnostic
    /// text; route policy consumes this typed value so party, master-data, inventory, and
    /// transaction failures cannot collapse into one generic UnknownEvidence bucket.
    /// </summary>
    public IReadOnlyDictionary<long, NetherStrategyUnknownReasonCode> UnknownReasonCodeByFloorId { get; init; } =
        new Dictionary<long, NetherStrategyUnknownReasonCode>();
    public IReadOnlyDictionary<long, int> PeakErosionByFloorId { get; init; } = new Dictionary<long, int>();
    public IReadOnlyDictionary<long, int> MinimumActiveCharacterHpPermilleByFloorId { get; init; } = new Dictionary<long, int>();
    public IReadOnlyDictionary<long, string> HorizonRejectionByFloorId { get; init; } = new Dictionary<long, string>();
    public IReadOnlyDictionary<long, bool> RequiresUserPauseByFloorId { get; init; } = new Dictionary<long, bool>();
    public IReadOnlyDictionary<long, NetherRouteHorizonSafetyEvaluation> HorizonEvaluationByFloorId
        { get; init; } = new Dictionary<long, NetherRouteHorizonSafetyEvaluation>();
    /// <summary>
    /// Strategy facts used only after the complete visible horizon passes the hard safety gate.
    /// A null research state is deliberately not guessed from mode or displayed points.
    /// </summary>
    public NetherStrategyMode StrategyMode { get; init; } = NetherStrategyMode.Equipment;
    public NetherCodeFamily PrimaryResearchFamily { get; init; } = NetherCodeFamily.Unknown;
    public bool? ResearchIncomplete { get; init; }
    /// <summary>
    /// The explicit settings used by the same Event policy that will execute a selected popup.
    /// Route analysis may narrow only the mode; it must not reconstruct a different objective.
    /// </summary>
    public NetherAutoClimbSettings StrategySettings { get; init; } = new();
    /// <summary>
    /// Exact procurement budgets already owned by the current route lifecycle. A missing key is
    /// not a zero budget and never authorizes a speculative resource commitment.
    /// </summary>
    public IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
        EventProcurementCommitments { get; init; } =
        new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
    public NetherStrategyVisibleMapEvidence? VisibleMap { get; init; }
    /// <summary>
    /// Snapshot-scoped interactive capture used to bind an Event battle route candidate to the
    /// typed projected HP/erosion/ownership proof. A missing or stale result never authorizes a
    /// semantic battle tier.
    /// </summary>
    public NetherRuntimeInteractivePreEntryInputsResult? InteractivePreEntry { get; init; }
    /// <summary>
    /// Explicit unit/compatibility seam for callers that intentionally exercise the pre-visible
    /// legacy comparator. Production coordinators leave this false; missing or malformed visible
    /// branch evidence therefore pauses instead of silently selecting from erosion/HP/reward
    /// fallback fields.
    /// </summary>
    public bool AllowLegacyComparatorCompatibility { get; init; }
    /// <summary>
    /// Exact visible rank-five Treasure nodes proven on a current branch. The planner only treats
    /// an objective as reachable when the selected candidate's already-safe horizon contains it;
    /// this prevents an alternate branch's Treasure from creating a global priority.
    /// </summary>
    public IReadOnlySet<long> MandatoryRankFiveKeyObjectiveNodeIds { get; init; } = new HashSet<long>();

    public bool IsHpSafe(long floorId) => !HpSafeByFloorId.TryGetValue(floorId, out bool safe) || safe;
    public bool IsKnown(long floorId) => !KnownNodeByFloorId.TryGetValue(floorId, out bool known) || known;
    public bool IsHardSafe(long floorId) => !HardSafeByFloorId.TryGetValue(floorId, out bool safe) || safe;
    public int MinimumWorstCaseErosion(long floorId) => MinimumWorstCaseErosionToTerminal.TryGetValue(floorId, out int value) ? value : 0;
    public int SafeCodeOpportunity(long floorId) => SafeCodeOpportunityByFloorId.TryGetValue(floorId, out int value) ? value : 0;
    public int ProjectedErosionDelta(long floorId) => ProjectedErosionDeltaByFloorId.TryGetValue(floorId, out int value) ? value : 0;
    public int ProjectedHpDelta(long floorId) => ProjectedHpDeltaByFloorId.TryGetValue(floorId, out int value) ? value : 0;
    public string UnknownDetail(long floorId) => UnknownDetailByFloorId.TryGetValue(floorId, out string? value)
        ? value
        : "missing-context-entry";
    public NetherStrategyUnknownReasonCode UnknownReasonCode(long floorId) =>
        UnknownReasonCodeByFloorId.TryGetValue(floorId, out NetherStrategyUnknownReasonCode code)
            ? code
            : NetherStrategyUnknownReasonCodes.FromDetail(UnknownDetail(floorId));
    public int PeakErosion(long floorId) => PeakErosionByFloorId.TryGetValue(floorId, out int value)
        ? value
        : int.MaxValue;
    public int MinimumActiveCharacterHpPermille(long floorId) =>
        MinimumActiveCharacterHpPermilleByFloorId.TryGetValue(floorId, out int value)
            ? value
            : int.MinValue;
    public string HorizonRejection(long floorId) => HorizonRejectionByFloorId.TryGetValue(floorId, out string? value)
        ? value
        : "missing-horizon-evidence";
    public bool RequiresUserPause(long floorId) =>
        RequiresUserPauseByFloorId.TryGetValue(floorId, out bool value) && value;
    public NetherRouteHorizonSafetyEvaluation? HorizonEvaluation(long floorId) =>
        HorizonEvaluationByFloorId.TryGetValue(floorId, out NetherRouteHorizonSafetyEvaluation? value)
            ? value
            : null;

    public bool HasMandatoryRankFiveKeyObjective(long floorId)
    {
        NetherRouteHorizonSafetyEvaluation? horizon = HorizonEvaluation(floorId);
        return horizon?.Steps != null
            && horizon.Steps.Any(step => MandatoryRankFiveKeyObjectiveNodeIds.Contains(step.NodeId));
    }

    public string DiagnosticDetail(long floorId)
    {
        if (!KnownNodeByFloorId.TryGetValue(floorId, out bool known))
            return "missing-context-entry";
        if (!known)
            return UnknownDetail(floorId);
        if (!HardSafeByFloorId.TryGetValue(floorId, out bool hardSafe))
            return "missing-hard-safety-entry";
        return hardSafe ? "known-terminal-path" : "known-no-terminal-path";
    }
}

internal readonly record struct NetherRouteCandidateAudit(long FloorId, string Reason)
{
    public bool IsCandidate { get; init; } = true;
    public bool IsSelected { get; init; }
    public string Detail { get; init; } = string.Empty;
    public NetherRouteCandidateHardGate FirstFailingHardGate { get; init; }
    public NetherRouteSemanticTier SemanticTier { get; init; }
    public NetherStrategyUnknownReasonCode UnknownReasonCode { get; init; }
    public NetherRouteEncounterVector? SemanticVector { get; init; }
    public bool SemanticVectorKnown { get; init; }
    public string SemanticVectorUnknownReason { get; init; } = string.Empty;
    public bool SafetyProjectionKnown { get; init; }
    public bool HardSafe { get; init; }
    public bool HpSafe { get; init; }
    public int TerminalWorstCaseErosion { get; init; }
    public int ProjectedErosionDelta { get; init; }
    public int ProjectedHpDelta { get; init; }
    public int ProcurementCommitmentCount { get; init; }
    public string TieBreakOrder { get; init; } = string.Empty;
    public string ComparisonRationale { get; init; } = string.Empty;
}

/// <summary>
/// Immutable identity for the route branch whose exact pre-entry evidence is allowed to produce
/// procurement commitments.  The path fingerprint is derived from the existing horizon audit;
/// it is not inferred from graph reachability or from an alternate safe branch.
/// </summary>
internal readonly record struct NetherRouteBranchIdentity(
    NetherSnapshotFingerprint SnapshotFingerprint,
    long CurrentNodeId,
    long SelectedNodeId,
    string SelectedPathFingerprint
)
{
    public bool IsValid => SnapshotFingerprint.MapId > 0
        && CurrentNodeId > 0
        && SelectedNodeId > 0
        && !string.IsNullOrEmpty(SelectedPathFingerprint);

    public bool Matches(NetherSnapshot snapshot, IReadOnlyList<long> selectedPathNodeIds) =>
        snapshot != null
        && IsValid
        && SnapshotFingerprint == snapshot.Fingerprint
        && CurrentNodeId == snapshot.CurrentNodeId
        && selectedPathNodeIds != null
        && selectedPathNodeIds.Count > 1
        && selectedPathNodeIds[0] == CurrentNodeId
        && selectedPathNodeIds[1] == SelectedNodeId
        && string.Equals(
            SelectedPathFingerprint,
            CreatePathFingerprint(selectedPathNodeIds),
            StringComparison.Ordinal
        );

    public static string CreatePathFingerprint(IReadOnlyList<long> pathNodeIds) =>
        pathNodeIds == null ? string.Empty : string.Join(">", pathNodeIds);
}

internal sealed record NetherRoutePlan
{
    public NetherFloorNode? SelectedNode { get; init; }
    public NetherSnapshotFingerprint? SourceSnapshotFingerprint { get; init; }
    public IReadOnlyList<long> SelectedPathNodeIds { get; init; } = Array.Empty<long>();
    public NetherRouteBranchIdentity? BranchIdentity { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string PauseDetail { get; init; } = string.Empty;
    public IReadOnlyList<NetherRouteCandidateAudit> Audit { get; init; } = Array.Empty<NetherRouteCandidateAudit>();
    public NetherRouteSelectionEvidence? SelectionEvidence { get; init; }
    public bool HasSelection => SelectedNode != null;
}

internal sealed class NetherRoutePlanner
{
    public NetherRoutePlan Plan(NetherSnapshot snapshot, NetherRouteSafetyContext context)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var audit = new List<NetherRouteCandidateAudit>();
        if (!TryCreateNodeIndex(snapshot.Floors, out Dictionary<long, NetherFloorNode>? nodes, out string indexError))
            return Pause(NetherPauseReason.InvalidGraph, indexError, audit);
        long currentNodeId = snapshot.CurrentNodeId > 0 ? snapshot.CurrentNodeId : snapshot.CurrentFloorId;
        if (!nodes.TryGetValue(currentNodeId, out NetherFloorNode? current))
            return Pause(NetherPauseReason.InvalidGraph, "missing-current-floor", audit);
        if (!HasOnlyKnownPredecessors(nodes, out string predecessorError))
            return Pause(NetherPauseReason.InvalidGraph, predecessorError, audit);

        HashSet<long> terminalReachable = FindTerminalReachable(nodes);
        if (terminalReachable.Count == 0)
            return Pause(NetherPauseReason.InvalidGraph, "missing-segment-terminal", audit);

        List<NetherFloorNode> candidates = nodes.Values
            .Where(node => node.NodeId != current.NodeId && node.PreviousFloorIds.Contains(current.NodeId))
            .ToList();
        if (candidates.Count == 0)
            return Pause(NetherPauseReason.NoSafeRoute, "no-current-frontier", audit);

        // Create one stable audit slot before any hard gate can short-circuit the frontier. This
        // is intentionally candidate-local: an unknown sibling must not erase the records for
        // candidates which were never reached by the selection loop.
        foreach (NetherFloorNode candidate in candidates)
            audit.Add(CreateCandidateAudit(candidate, context, useVisibleBranchVector: true));

        foreach (NetherFloorNode candidate in candidates)
        {
            if (candidate.NodeType is NetherFloorNodeType.Unknown or NetherFloorNodeType.Default)
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "unknown-floor",
                    NetherRouteCandidateHardGate.NativeNodeSemantics,
                    "unknown-frontier-floor",
                    context.UnknownReasonCode(candidate.NodeId)
                ));
                // The unknown node is a local branch rejection. Continue evaluating every sibling
                // whose native node identity is known; only the no-safe-frontier result below may
                // pause the controller.
                continue;
            }
        }

        bool useVisibleBranchVector = HasUsableVisibleBranchVector(snapshot, context.VisibleMap);
        foreach (NetherFloorNode candidate in candidates)
        {
            UpdateCandidateAudit(audit, new NetherRouteCandidateAudit(candidate.NodeId, string.Empty)
            {
                IsCandidate = true,
                SemanticVectorUnknownReason = useVisibleBranchVector
                    ? "visible-route-vector-not-yet-evaluated"
                    : "legacy-comparator-vector-not-captured",
                TieBreakOrder = TieBreakOrder(useVisibleBranchVector),
            });
        }
        if (!useVisibleBranchVector && !context.AllowLegacyComparatorCompatibility)
        {
            foreach (NetherFloorNode candidate in candidates)
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "unknown-node",
                    NetherRouteCandidateHardGate.VisibleSemanticVector,
                    "visible-route-vector-unavailable-for-production",
                    NetherStrategyUnknownReasonCode.RouteVectorInputUnavailable
                ));
            }
            return Pause(
                NetherPauseReason.UnknownMasterData,
                "visible-route-vector-unavailable-for-production",
                audit
            );
        }

        var safeCandidates = new List<Candidate>();
        foreach (NetherFloorNode candidate in candidates)
        {
            if (candidate.NodeType is NetherFloorNodeType.Unknown or NetherFloorNodeType.Default)
                continue;
            if (candidate.FloorLevel > context.MaximumFloorLevel)
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "above-target-depth",
                    NetherRouteCandidateHardGate.TargetDepth
                ));
                continue;
            }
            if (!candidate.IsUnlocked)
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "locked",
                    NetherRouteCandidateHardGate.Locked
                ));
                continue;
            }
            if (!terminalReachable.Contains(candidate.NodeId))
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "dead-end",
                    NetherRouteCandidateHardGate.TerminalReachability
                ));
                continue;
            }
            if (!context.IsKnown(candidate.NodeId))
            {
                string detail = context.UnknownDetail(candidate.NodeId);
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "unknown-node",
                    NetherRouteCandidateHardGate.NativeNodeSemantics,
                    detail,
                    context.UnknownReasonCode(candidate.NodeId)
                ));
                continue;
            }
            if (!context.IsHardSafe(candidate.NodeId))
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "unsafe",
                    NetherRouteCandidateHardGate.HardSafety
                ));
                continue;
            }
            if (!context.IsHpSafe(candidate.NodeId))
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "unsafe-hp",
                    NetherRouteCandidateHardGate.HpSafety
                ));
                continue;
            }
            if (!IsBelowHardErosionLimit(snapshot.ErosionPoint, context.MinimumWorstCaseErosion(candidate.NodeId)))
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "terminal-erosion-100",
                    NetherRouteCandidateHardGate.TerminalErosion
                ));
                continue;
            }

            NetherRouteHorizonSafetyEvaluation? horizon = context.HorizonEvaluation(candidate.NodeId);
            if (useVisibleBranchVector && horizon == null)
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "unknown-node",
                    NetherRouteCandidateHardGate.VisibleHorizon,
                    "visible-route-horizon-unavailable",
                    NetherStrategyUnknownReasonCode.ErosionHorizonUnavailable
                ));
                continue;
            }
            if (useVisibleBranchVector && !HasCompleteVisibleHorizon(candidate.NodeId, horizon))
            {
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "unknown-node",
                    NetherRouteCandidateHardGate.VisibleHorizon,
                    "visible-route-vector-unavailable",
                    NetherStrategyUnknownReasonCode.RouteVectorUnknown
                ));
                continue;
            }

            NetherRouteEncounterVector? vector = useVisibleBranchVector
                ? NetherRouteEncounterVectorPolicy.Build(snapshot, context, candidate, horizon!)
                : null;
            if (useVisibleBranchVector && vector is not { IsKnown: true })
            {
                string detail = string.IsNullOrWhiteSpace(vector?.UnknownReason)
                    ? "visible-route-vector-unknown"
                    : vector.UnknownReason;
                UpdateCandidateAudit(audit, Reject(
                    candidate.NodeId,
                    "unknown-node",
                    NetherRouteCandidateHardGate.VisibleSemanticVector,
                    detail,
                    vector?.UnknownReasonCode is NetherStrategyUnknownReasonCode code
                        && code != NetherStrategyUnknownReasonCode.None
                        ? code
                        : NetherStrategyUnknownReasonCodes.FromDetail(detail)
                ));
                continue;
            }
            UpdateCandidateAudit(audit, CreateEligibleCandidateAudit(
                candidate,
                context,
                horizon,
                vector,
                useVisibleBranchVector
            ));
            safeCandidates.Add(new Candidate(
                candidate,
                true,
                true,
                context.ProjectedErosionDelta(candidate.NodeId),
                context.ProjectedHpDelta(candidate.NodeId),
                context.SafeCodeOpportunity(candidate.NodeId),
                horizon,
                vector
            ));
        }

        if (safeCandidates.Count == 0)
        {
            NetherPauseReason reason = audit.Any(item => item.Reason == "unknown-floor")
                ? NetherPauseReason.UnknownFloor
                : audit.Any(item => item.Reason == "terminal-erosion-100")
                ? NetherPauseReason.UnsafeErosion
                : audit.Any(item => item.Reason == "unsafe-hp")
                    ? NetherPauseReason.UnsafeHp
                : audit.Any(item => item.Reason == "unknown-node")
                    ? NetherPauseReason.UnknownMasterData
                    : audit.Any(item => item.Reason == "above-target-depth")
                        ? NetherPauseReason.TargetReachedOutsideCheckpoint
                    : NetherPauseReason.NoSafeRoute;
            return Pause(reason, "no-safe-frontier", audit);
        }

        if (useVisibleBranchVector
            && context.StrategyMode == NetherStrategyMode.Research
            && safeCandidates.Any(candidate => candidate.EncounterVector != null)
            && context.ResearchIncomplete is null)
        {
            audit.Add(new NetherRouteCandidateAudit(
                currentNodeId,
                "research-completion-unknown"
            )
            {
                IsCandidate = false,
                Detail = "native-settlement-does-not-prove-pre-settlement-research-completion",
                FirstFailingHardGate = NetherRouteCandidateHardGate.ResearchCompletion,
                UnknownReasonCode = NetherStrategyUnknownReasonCode.ResearchCompletionUnknown,
            });
            return Pause(
                NetherPauseReason.UnknownMasterData,
                "research-completion-state-unknown-for-visible-route-vector",
                audit
            );
        }

        bool? modeResearchIncomplete = context.StrategyMode == NetherStrategyMode.Research
            ? context.ResearchIncomplete
            : false;
        Candidate selected = safeCandidates[0];
        for (int index = 1; index < safeCandidates.Count; index++)
        {
            Candidate contender = safeCandidates[index];
            int comparison = CompareCandidates(
                contender,
                selected,
                context,
                modeResearchIncomplete,
                out string comparisonRationale
            );
            if (comparison > 0)
            {
                AddComparisonRationale(
                    audit,
                    selected.Node.NodeId,
                    "lost-to:" + contender.Node.NodeId + ":" + comparisonRationale
                );
                AddComparisonRationale(
                    audit,
                    contender.Node.NodeId,
                    "won-over:" + selected.Node.NodeId + ":" + comparisonRationale
                );
                selected = contender;
            }
            else
            {
                AddComparisonRationale(
                    audit,
                    contender.Node.NodeId,
                    "lost-to:" + selected.Node.NodeId + ":" + comparisonRationale
                );
                AddComparisonRationale(
                    audit,
                    selected.Node.NodeId,
                    "retained-over:" + contender.Node.NodeId + ":" + comparisonRationale
                );
            }
        }

        foreach (Candidate candidate in safeCandidates)
        {
            NetherRouteSemanticTier tier = candidate.EncounterVector?.HighestSemanticTier(
                modeResearchIncomplete == true
            ) ?? NetherRouteSemanticTier.None;
            UpdateCandidateAudit(audit, new NetherRouteCandidateAudit(
                candidate.Node.NodeId,
                candidate.Node.NodeId == selected.Node.NodeId ? "selected" : "excluded"
            )
            {
                SemanticVector = candidate.EncounterVector,
                SemanticVectorKnown = candidate.EncounterVector?.IsKnown == true,
                SemanticVectorUnknownReason = candidate.EncounterVector?.IsKnown == true
                    ? string.Empty
                    : candidate.EncounterVector?.UnknownReason ?? "legacy-comparator-vector-not-captured",
                SemanticTier = tier,
                SafetyProjectionKnown = candidate.Horizon != null,
                HardSafe = candidate.HardSafe,
                HpSafe = context.IsHpSafe(candidate.Node.NodeId),
                TerminalWorstCaseErosion = context.MinimumWorstCaseErosion(candidate.Node.NodeId),
                ProjectedErosionDelta = candidate.ProjectedErosionDelta,
                ProjectedHpDelta = candidate.ProjectedHpDelta,
                ProcurementCommitmentCount = context.EventProcurementCommitments.Count,
                TieBreakOrder = TieBreakOrder(candidate.EncounterVector != null),
                IsSelected = candidate.Node.NodeId == selected.Node.NodeId,
                ComparisonRationale = candidate.Node.NodeId == selected.Node.NodeId
                    ? "selected-after-comparison"
                    : "eligible-but-not-selected",
            });
        }
        IReadOnlyList<long> selectedPathNodeIds = CreateSelectedPathNodeIds(
            currentNodeId,
            selected.Node,
            context,
            selected.Horizon
        );
        NetherRouteBranchIdentity? branchIdentity = selectedPathNodeIds.Count == 0
            ? null
            : new NetherRouteBranchIdentity(
                snapshot.Fingerprint,
                currentNodeId,
                selected.Node.NodeId,
                NetherRouteBranchIdentity.CreatePathFingerprint(selectedPathNodeIds)
            );
        return new NetherRoutePlan
        {
            SelectedNode = selected.Node,
            SourceSnapshotFingerprint = snapshot.Fingerprint,
            SelectedPathNodeIds = selectedPathNodeIds,
            BranchIdentity = branchIdentity,
            Audit = audit,
            SelectionEvidence = new NetherRouteSelectionEvidence
            {
                SemanticVector = selected.EncounterVector,
                SemanticVectorKnown = selected.EncounterVector?.IsKnown == true,
                SemanticVectorUnknownReason = selected.EncounterVector?.UnknownReason ?? string.Empty,
                SelectedSemanticTier = selected.EncounterVector?.HighestSemanticTier(
                    modeResearchIncomplete == true
                ) ?? NetherRouteSemanticTier.None,
                SafetyProjectionKnown = selected.Horizon != null,
                HardSafe = selected.HardSafe,
                HpSafe = context.IsHpSafe(selected.Node.NodeId),
                TerminalWorstCaseErosion = context.MinimumWorstCaseErosion(selected.Node.NodeId),
                ProjectedErosionDelta = selected.ProjectedErosionDelta,
                ProjectedHpDelta = selected.ProjectedHpDelta,
                ProcurementCommitmentCount = context.EventProcurementCommitments.Count,
                TieBreakOrder = useVisibleBranchVector
                    ? "semantic-vector>peak-erosion>active-hp>coordinates"
                    : "legacy-safety>objective>erosion>hp>coordinates",
                CandidateAudits = audit.Where(item => item.IsCandidate).ToArray(),
            },
        };
    }

    private static bool HasUsableVisibleBranchVector(
        NetherSnapshot snapshot,
        NetherStrategyVisibleMapEvidence? visibleMap
    )
    {
        if (snapshot == null
            || visibleMap?.Floors == null
            || visibleMap.ContentRows == null
            || visibleMap.Floors.Count == 0
            || visibleMap.ContentRows.Count == 0)
            return false;
        var nodeIds = new HashSet<long>();
        foreach (NetherFloorNode floor in visibleMap.Floors)
        {
            if (floor == null
                || floor.NodeId <= 0
                || floor.FloorId <= 0
                || !nodeIds.Add(floor.NodeId))
            {
                return false;
            }
        }
        if (nodeIds.Count == 0 || snapshot.CurrentNodeId <= 0 || !nodeIds.Contains(snapshot.CurrentNodeId))
            return false;
        foreach (NetherStrategyVisibleContentRow? row in visibleMap.ContentRows)
        {
            if (row == null
                || row.Kind == NetherStrategyVisibleContentKind.Unknown
                || row.NodeId <= 0
                || !nodeIds.Contains(row.NodeId)
                || !row.IsKnown && string.IsNullOrWhiteSpace(row.UnknownReason))
            {
                return false;
            }
        }
        return true;
    }

    private static bool HasCompleteVisibleHorizon(
        long candidateNodeId,
        NetherRouteHorizonSafetyEvaluation? horizon
    )
    {
        if (horizon == null
            || !horizon.IsEligible
            || horizon.Steps == null
            || horizon.HorizonSteps == null
            || horizon.Steps.Count == 0
            || horizon.Steps.Count != horizon.HorizonSteps.Count
            || horizon.HorizonSteps[0] == null
            || horizon.HorizonSteps[0].NodeId != candidateNodeId)
        {
            return false;
        }

        NetherRouteHorizonStep terminal = horizon.HorizonSteps[^1];
        if (terminal == null
            || terminal.NodeId <= 0
            || terminal.NodeType != NetherFloorNodeType.Boss
            || !terminal.IsTerminalBoss)
        {
            return false;
        }

        var nodeIds = new HashSet<long>();
        for (int index = 0; index < horizon.HorizonSteps.Count; index++)
        {
            NetherRouteHorizonStep step = horizon.HorizonSteps[index];
            if (step == null
                || step.NodeId <= 0
                || !nodeIds.Add(step.NodeId)
                || horizon.Steps[index].NodeId != step.NodeId)
            {
                return false;
            }
        }
        return true;
    }

    private static IReadOnlyList<long> CreateSelectedPathNodeIds(
        long currentNodeId,
        NetherFloorNode selected,
        NetherRouteSafetyContext context,
        NetherRouteHorizonSafetyEvaluation? selectedHorizon
    )
    {
        NetherRouteHorizonSafetyEvaluation? horizon = selectedHorizon ?? context.HorizonEvaluation(selected.NodeId);
        if (horizon == null
            || !horizon.IsEligible
            || horizon.Steps == null
            || horizon.Steps.Count == 0)
        {
            return Array.Empty<long>();
        }

        var path = new List<long>(horizon.Steps.Count + 1) { currentNodeId };
        foreach (NetherRouteHorizonStepAudit step in horizon.Steps)
        {
            if (step.NodeId <= 0 || path.Contains(step.NodeId))
                return Array.Empty<long>();
            path.Add(step.NodeId);
        }
        return path[1] == selected.NodeId
            ? Array.AsReadOnly(path.ToArray())
            : Array.Empty<long>();
    }

    private static bool TryCreateNodeIndex(
        IReadOnlyList<NetherFloorNode> floors,
        out Dictionary<long, NetherFloorNode> nodes,
        out string error
    )
    {
        nodes = new Dictionary<long, NetherFloorNode>();
        error = string.Empty;
        if (floors.Count == 0)
        {
            error = "empty-floor-graph";
            return false;
        }

        foreach (NetherFloorNode node in floors)
        {
            if (node.FloorId <= 0 || node.NodeId <= 0 || !nodes.TryAdd(node.NodeId, node))
            {
                error = "duplicate-or-invalid-floor-id";
                return false;
            }
        }
        return true;
    }

    private static bool HasOnlyKnownPredecessors(
        IReadOnlyDictionary<long, NetherFloorNode> nodes,
        out string error
    )
    {
        foreach (NetherFloorNode node in nodes.Values)
        {
            foreach (long previousId in node.PreviousFloorIds)
            {
                if (!nodes.ContainsKey(previousId))
                {
                    error = $"missing-prev-floor:{node.FloorId}:{previousId}";
                    return false;
                }
            }
        }
        error = string.Empty;
        return true;
    }

    private static HashSet<long> FindTerminalReachable(IReadOnlyDictionary<long, NetherFloorNode> nodes)
    {
        var reachable = new HashSet<long>();
        var pending = new Stack<long>();
        foreach (NetherFloorNode terminal in nodes.Values.Where(node => node.NodeType == NetherFloorNodeType.Boss))
        {
            if (reachable.Add(terminal.NodeId))
                pending.Push(terminal.NodeId);
        }

        while (pending.Count > 0)
        {
            long floorId = pending.Pop();
            foreach (long previousId in nodes[floorId].PreviousFloorIds)
            {
                if (reachable.Add(previousId))
                    pending.Push(previousId);
            }
        }
        return reachable;
    }

    private static bool IsBelowHardErosionLimit(int current, int worstCaseDelta)
    {
        try
        {
            return checked(current + worstCaseDelta) < 100;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static NetherRoutePlan Pause(
        NetherPauseReason reason,
        string detail,
        IReadOnlyList<NetherRouteCandidateAudit> audit
    ) => new()
    {
        PauseReason = reason,
        PauseDetail = detail,
        Audit = audit,
    };

    private static NetherRouteCandidateAudit Reject(
        long floorId,
        string reason,
        NetherRouteCandidateHardGate gate,
        string detail = "",
        NetherStrategyUnknownReasonCode unknownReasonCode = NetherStrategyUnknownReasonCode.None
    ) => new(floorId, reason)
    {
        IsCandidate = true,
        FirstFailingHardGate = gate,
        Detail = detail,
        UnknownReasonCode = unknownReasonCode,
        ComparisonRationale = "excluded:first-failing-gate=" + gate,
    };

    private static NetherRouteCandidateAudit CreateCandidateAudit(
        NetherFloorNode candidate,
        NetherRouteSafetyContext context,
        bool useVisibleBranchVector
    ) => new(candidate.NodeId, "candidate")
    {
        IsCandidate = true,
        SemanticVectorUnknownReason = useVisibleBranchVector
            ? "visible-route-vector-not-yet-evaluated"
            : "legacy-comparator-vector-not-captured",
        SafetyProjectionKnown = context.HorizonEvaluation(candidate.NodeId) != null,
        HardSafe = context.IsHardSafe(candidate.NodeId),
        HpSafe = context.IsHpSafe(candidate.NodeId),
        TerminalWorstCaseErosion = context.MinimumWorstCaseErosion(candidate.NodeId),
        ProjectedErosionDelta = context.ProjectedErosionDelta(candidate.NodeId),
        ProjectedHpDelta = context.ProjectedHpDelta(candidate.NodeId),
        ProcurementCommitmentCount = context.EventProcurementCommitments.Count,
        TieBreakOrder = TieBreakOrder(useVisibleBranchVector),
        ComparisonRationale = "awaiting-selection",
    };

    private static NetherRouteCandidateAudit CreateEligibleCandidateAudit(
        NetherFloorNode candidate,
        NetherRouteSafetyContext context,
        NetherRouteHorizonSafetyEvaluation? horizon,
        NetherRouteEncounterVector? vector,
        bool useVisibleBranchVector
    ) => new(candidate.NodeId, "eligible")
    {
        IsCandidate = true,
        SemanticVector = vector,
        SemanticVectorKnown = vector?.IsKnown == true,
        SemanticVectorUnknownReason = vector?.IsKnown == true
            ? string.Empty
            : vector?.UnknownReason ?? (useVisibleBranchVector
                ? "visible-route-vector-not-evaluated"
                : "legacy-comparator-vector-not-captured"),
        SafetyProjectionKnown = horizon != null,
        HardSafe = context.IsHardSafe(candidate.NodeId),
        HpSafe = context.IsHpSafe(candidate.NodeId),
        TerminalWorstCaseErosion = context.MinimumWorstCaseErosion(candidate.NodeId),
        ProjectedErosionDelta = context.ProjectedErosionDelta(candidate.NodeId),
        ProjectedHpDelta = context.ProjectedHpDelta(candidate.NodeId),
        ProcurementCommitmentCount = context.EventProcurementCommitments.Count,
        TieBreakOrder = TieBreakOrder(useVisibleBranchVector),
    };

    private static string TieBreakOrder(bool useVisibleBranchVector) => useVisibleBranchVector
        ? "semantic-vector>peak-erosion>active-hp>coordinates"
        : "legacy-safety>objective>erosion>hp>coordinates";

    private static void AddComparisonRationale(
        List<NetherRouteCandidateAudit> audit,
        long floorId,
        string rationale
    ) => UpdateCandidateAudit(audit, new NetherRouteCandidateAudit(floorId, string.Empty)
    {
        IsCandidate = true,
        ComparisonRationale = rationale,
    });

    private static void UpdateCandidateAudit(
        List<NetherRouteCandidateAudit> audit,
        NetherRouteCandidateAudit incoming
    )
    {
        int index = audit.FindIndex(item => item.IsCandidate && item.FloorId == incoming.FloorId);
        if (index < 0)
        {
            audit.Add(incoming);
            return;
        }

        NetherRouteCandidateAudit existing = audit[index];
        NetherRouteCandidateAudit merged = existing with
        {
            IsSelected = existing.IsSelected || incoming.IsSelected,
            Reason = string.IsNullOrEmpty(incoming.Reason) || incoming.Reason == "candidate"
                ? existing.Reason
                : incoming.Reason,
            Detail = string.IsNullOrEmpty(incoming.Detail) ? existing.Detail : incoming.Detail,
            FirstFailingHardGate = incoming.FirstFailingHardGate == NetherRouteCandidateHardGate.None
                ? existing.FirstFailingHardGate
                : incoming.FirstFailingHardGate,
            UnknownReasonCode = incoming.UnknownReasonCode == NetherStrategyUnknownReasonCode.None
                ? existing.UnknownReasonCode
                : incoming.UnknownReasonCode,
            SemanticVector = incoming.SemanticVector ?? existing.SemanticVector,
            SemanticVectorKnown = existing.SemanticVectorKnown || incoming.SemanticVectorKnown,
            SemanticVectorUnknownReason = string.IsNullOrEmpty(incoming.SemanticVectorUnknownReason)
                ? existing.SemanticVectorUnknownReason
                : incoming.SemanticVectorUnknownReason,
            ComparisonRationale = string.IsNullOrEmpty(incoming.ComparisonRationale)
                ? existing.ComparisonRationale
                : string.IsNullOrEmpty(existing.ComparisonRationale)
                    ? incoming.ComparisonRationale
                    : existing.ComparisonRationale + "|" + incoming.ComparisonRationale,
        };

        bool incomingCarriesSafetyFacts = incoming.SafetyProjectionKnown
            || incoming.HardSafe
            || incoming.HpSafe
            || incoming.TerminalWorstCaseErosion != 0
            || incoming.ProjectedErosionDelta != 0
            || incoming.ProjectedHpDelta != 0
            || incoming.ProcurementCommitmentCount != 0;
        if (!string.IsNullOrEmpty(incoming.TieBreakOrder) && incomingCarriesSafetyFacts)
        {
            merged = merged with
            {
                SafetyProjectionKnown = incoming.SafetyProjectionKnown,
                HardSafe = incoming.HardSafe,
                HpSafe = incoming.HpSafe,
                TerminalWorstCaseErosion = incoming.TerminalWorstCaseErosion,
                ProjectedErosionDelta = incoming.ProjectedErosionDelta,
                ProjectedHpDelta = incoming.ProjectedHpDelta,
                ProcurementCommitmentCount = incoming.ProcurementCommitmentCount,
                TieBreakOrder = incoming.TieBreakOrder,
            };
        }
        audit[index] = merged;
    }

    private readonly record struct Candidate(
        NetherFloorNode Node,
        bool HardSafe,
        bool TerminalReachable,
        int ProjectedErosionDelta,
        int ProjectedHpDelta,
        int SafeCodeOpportunity,
        NetherRouteHorizonSafetyEvaluation? Horizon,
        NetherRouteEncounterVector? EncounterVector
    );

    private static int CompareCandidates(
        Candidate left,
        Candidate right,
        NetherRouteSafetyContext context,
        bool? researchIncomplete,
        out string rationale
    )
    {
        rationale = "all-comparison-keys-equal";
        if (left.EncounterVector != null || right.EncounterVector != null)
        {
            if (researchIncomplete is not bool knownResearchIncomplete)
            {
                rationale = "research-completion-unknown";
                return 0;
            }
            if (left.EncounterVector is not { IsKnown: true } leftVector
                || right.EncounterVector is not { IsKnown: true } rightVector)
            {
                rationale = "semantic-vector-unknown";
                return 0;
            }
            int semantic = leftVector.CompareTo(rightVector, knownResearchIncomplete);
            if (semantic != 0)
            {
                rationale = "semantic-vector";
                return semantic;
            }

            // These are true route-vector tie breaks only. Safety was already filtered above.
            int peak = right.Horizon?.PeakErosion is int rightPeak
                && left.Horizon?.PeakErosion is int leftPeak
                    ? rightPeak.CompareTo(leftPeak)
                    : 0;
            if (peak != 0)
            {
                rationale = "peak-erosion";
                return peak;
            }
            int hp = left.Horizon?.MinimumActiveCharacterHpPermille is int leftHp
                && right.Horizon?.MinimumActiveCharacterHpPermille is int rightHp
                    ? leftHp.CompareTo(rightHp)
                    : 0;
            if (hp != 0)
            {
                rationale = "active-hp";
                return hp;
            }
            rationale = "coordinates";
            return CompareCoordinates(left.Node, right.Node);
        }

        // Compatibility path for route fixtures that intentionally carry no visible semantic
        // package. It retains the pre-13 deterministic policy while new production captures use
        // the complete visible vector above.
        int result = left.HardSafe.CompareTo(right.HardSafe);
        if (result != 0)
        {
            rationale = "legacy-safety";
            return result;
        }
        result = left.TerminalReachable.CompareTo(right.TerminalReachable);
        if (result != 0)
        {
            rationale = "terminal-reachability";
            return result;
        }
        result = (left.Node.NodeType == NetherFloorNodeType.Boss)
            .CompareTo(right.Node.NodeType == NetherFloorNodeType.Boss);
        if (result != 0)
        {
            rationale = "boss-objective";
            return result;
        }
        result = context.HasMandatoryRankFiveKeyObjective(left.Node.NodeId)
            .CompareTo(context.HasMandatoryRankFiveKeyObjective(right.Node.NodeId));
        if (result != 0)
        {
            rationale = "rank-five-objective";
            return result;
        }
        result = right.ProjectedErosionDelta.CompareTo(left.ProjectedErosionDelta);
        if (result != 0)
        {
            rationale = "projected-erosion";
            return result;
        }
        result = left.ProjectedHpDelta.CompareTo(right.ProjectedHpDelta);
        if (result != 0)
        {
            rationale = "projected-hp";
            return result;
        }
        result = left.SafeCodeOpportunity.CompareTo(right.SafeCodeOpportunity);
        if (result != 0)
        {
            rationale = "safe-code-opportunity";
            return result;
        }
        result = left.Node.RewardTier.CompareTo(right.Node.RewardTier);
        if (result != 0)
        {
            rationale = "reward-tier";
            return result;
        }
        result = right.Node.OptionalCombatCount.CompareTo(left.Node.OptionalCombatCount);
        if (result != 0)
        {
            rationale = "optional-combat-count";
            return result;
        }
        rationale = "coordinates";
        return CompareCoordinates(left.Node, right.Node);
    }

    private static int CompareCoordinates(NetherFloorNode left, NetherFloorNode right)
    {
        int result = right.FloorIndex.CompareTo(left.FloorIndex);
        if (result != 0)
            return result;
        result = right.FloorId.CompareTo(left.FloorId);
        return result != 0 ? result : right.NodeId.CompareTo(left.NodeId);
    }
}
