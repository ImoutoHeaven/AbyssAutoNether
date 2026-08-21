#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Owns exact procurement commitments produced by route/pre-entry evidence. This durable state is
/// separate from the one-shot pending handoff because route-safety capture can be repeated before
/// the native Event update. No native Event row or displayed resource creates a commitment.
/// </summary>
internal sealed class NetherRouteOwnedEventProcurementProducer
{
    private IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> _committed =
        new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
    private NetherRouteBranchIdentity? _identity;

    public NetherRouteBranchIdentity? CurrentIdentity => _identity;

    /// <summary>
    /// Replaces the current route-owned proof only when a new exact route proof exists. An empty
    /// capture is not evidence that a previously captured branch disappeared; this distinction is
    /// required because native FloorSelection capture can legitimately be repeated between popup
    /// stages without exposing the same rows again.
    /// </summary>
    public void Commit(
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? commitments
    )
    {
        if (commitments == null || commitments.Count == 0)
            return;

        Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> exact = Normalize(commitments);
        if (exact.Count > 0)
            _committed = exact;
    }

    /// <summary>
    /// Replaces the durable proof with the exact selected route identity.  An empty map is a
    /// meaningful recomputation for that branch and therefore clears the prior option map.
    /// </summary>
    public void Commit(
        NetherRouteBranchIdentity identity,
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? commitments
    )
    {
        if (!identity.IsValid)
            return;
        _identity = identity;
        _committed = Normalize(commitments);
    }

    public IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> Capture() =>
        new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>(_committed);

    public IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
        CaptureForSnapshot(NetherSnapshotFingerprint fingerprint) =>
        _identity is { } identity && identity.SnapshotFingerprint == fingerprint
            ? Capture()
            : new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();

    public NetherRouteBranchIdentity? IdentityForSnapshot(NetherSnapshotFingerprint fingerprint) =>
        _identity is { } identity && identity.SnapshotFingerprint == fingerprint
            ? identity
            : null;

    public void InvalidateForSnapshot(NetherSnapshotFingerprint fingerprint)
    {
        if (_identity is { } identity && identity.SnapshotFingerprint != fingerprint)
            Clear();
    }

    public void Clear()
    {
        _committed = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
        _identity = null;
    }

    public static IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
        FromInteractivePreEntry(
            NetherSnapshot snapshot,
            NetherRoutePlan route,
            NetherRuntimeInteractivePreEntryInputsResult? interactivePreEntry
        )
    {
        var commitments = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
        if (snapshot == null
            || route?.BranchIdentity is not NetherRouteBranchIdentity identity
            || !identity.Matches(snapshot, route.SelectedPathNodeIds)
            || interactivePreEntry == null
            || !interactivePreEntry.IsSuccess
            || interactivePreEntry.SnapshotFingerprint != snapshot.Fingerprint)
            return commitments;

        foreach (NetherRuntimeInteractivePreEntryCaptureResult capture in interactivePreEntry.ByFloorNodeId.Values)
        {
            if (capture?.Input == null
                || !route.SelectedPathNodeIds.Contains(capture.Input.FloorNodeId))
            {
                continue;
            }
            foreach ((NetherInteractiveEventOptionKey key, NetherInteractiveOptionProjection projection)
                in capture.Safety.OptionProjectionByKey)
            {
                if (key.EventId <= 0
                    || key.EventPartId <= 0
                    || key.OptionNumber <= 0
                    || !projection.HasCommittedProcurementEvidence)
                {
                    continue;
                }

                NetherEventProcurementBudget budget = new(
                    projection.CommittedGoldMinimum,
                    projection.CommittedKeyMinimum
                );
                if (budget.IsValid)
                    commitments[key] = budget;
            }
        }
        return commitments;
    }

    /// <summary>
    /// Creates procurement minima only from a selected route's exact visible branch.  The Event
    /// option must be present in both the pre-entry projection and the visible native row, and the
    /// future Shop/Treasure source must be known, reachable through hard-safe nodes, and affordable
    /// after the exact reward projection.  A missing or non-materialized source produces no budget.
    /// </summary>
    public static IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
        FromSelectedVisibleBranch(
            NetherSnapshot snapshot,
            NetherRoutePlan route,
            NetherRouteSafetyContext context,
            NetherRuntimeInteractivePreEntryInputsResult? interactivePreEntry,
            NetherStrategyVisibleMapEvidence? visibleMap
        )
    {
        var commitments = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
        if (snapshot == null
            || route?.SelectedNode == null
            || context == null
            || interactivePreEntry == null
            || !interactivePreEntry.IsSuccess
            || route.BranchIdentity is not NetherRouteBranchIdentity identity
            || !identity.Matches(snapshot, route.SelectedPathNodeIds)
            || interactivePreEntry.SnapshotFingerprint != snapshot.Fingerprint
            || visibleMap == null
            || visibleMap.ContentRows == null)
        {
            return commitments;
        }

        IReadOnlyList<NetherFloorNode> floors = snapshot.Floors ?? Array.Empty<NetherFloorNode>();
        foreach ((long nodeId, NetherRuntimeInteractivePreEntryCaptureResult capture) in interactivePreEntry.ByFloorNodeId)
        {
            if (capture?.Input == null
                || !capture.IsCaptured
                || !capture.Safety.IsSafe
                || capture.Safety.OptionProjectionByKey == null
                || !IsSafeNode(context, nodeId)
                || !IsSelectedPathNode(route, nodeId)
                || !HasSelectedTerminalPath(nodeId, route, floors, context))
            {
                continue;
            }

            foreach ((NetherInteractiveEventOptionKey key, NetherInteractiveOptionProjection projection)
                in capture.Safety.OptionProjectionByKey)
            {
                if (key.EventId <= 0
                    || key.EventPartId <= 0
                    || key.OptionNumber <= 0
                    || projection == null
                    || projection.EventId != key.EventId
                    || projection.EventPartId != key.EventPartId
                    || projection.NodeId != nodeId
                    || !projection.IsKnown
                    || !projection.HasRouteSafetyEvidence
                    || !projection.RouteSafetyAllowed
                    || !HasExactVisibleOption(visibleMap, nodeId, key, projection)
                    || !NetherEventResourceProjection.TryProject(
                        snapshot.NetherGold,
                        snapshot.TreasureKeyCount,
                        projection.ExpectedEffects,
                        out int projectedGold,
                        out int projectedKeys
                    ))
                {
                    continue;
                }

            int goldMinimum = FindAffordableShopMinimum(
                    visibleMap.ContentRows,
                    nodeId,
                    route,
                    floors,
                    context,
                    projectedGold,
                    projection.ExpectedEffects.Any(effect => effect.Kind == NetherEffectKind.NetherGoldGain)
                );
            int keyMinimum = HasAffordableRankFiveTreasure(
                    visibleMap.ContentRows,
                    nodeId,
                    route,
                    floors,
                    context,
                    projectedKeys,
                    projection.ExpectedEffects.Any(effect => effect.Kind == NetherEffectKind.TreasureKeyGain)
                ) ? 1 : 0;
                NetherEventProcurementBudget budget = new(goldMinimum, keyMinimum);
                if (budget.GoldMinimum > 0 || budget.KeyMinimum > 0)
                    commitments[key] = budget;
            }
        }
        return commitments;
    }

    private static bool HasExactVisibleOption(
        NetherStrategyVisibleMapEvidence visibleMap,
        long nodeId,
        NetherInteractiveEventOptionKey key,
        NetherInteractiveOptionProjection projection
    )
    {
        NetherStrategyVisibleContentRow[] rows = visibleMap.ContentRows
            .Where(row => row != null
                && row.Kind == NetherStrategyVisibleContentKind.Event
                && row.NodeId == nodeId
                && row.EventId == key.EventId
                && row.EventPartId == key.EventPartId
                && row.IsKnown)
            .ToArray();
        if (rows.Length != 1)
            return false;
        NetherStrategyVisibleEventOptionEvidence[] options = rows[0].EventOptions
            .Where(option => option != null
                && option.OptionNumber == key.OptionNumber
                && option.EventPartId == key.EventPartId)
            .ToArray();
        if (options.Length != 1)
            return false;
        if (options[0].Effects.Any(effect => effect.IsPresent && !effect.IsKnown))
            return false;
        foreach (NetherEffect expected in projection.ExpectedEffects)
        {
            if (expected.Kind is not NetherEffectKind.NetherGoldGain and not NetherEffectKind.TreasureKeyGain)
                continue;
            if (!options[0].Effects.Any(effect => effect.IsPresent
                    && effect.IsKnown
                    && effect.EffectKind == expected.Kind
                    && effect.Amount == expected.Amount
                    && effect.ContentId == expected.ContentId))
            {
                return false;
            }
        }
        return true;
    }

    private static int FindAffordableShopMinimum(
        IReadOnlyList<NetherStrategyVisibleContentRow> rows,
        long eventNodeId,
        NetherRoutePlan route,
        IReadOnlyList<NetherFloorNode> floors,
        NetherRouteSafetyContext context,
        int projectedGold,
        bool requiresGoldReward
    )
    {
        if (!requiresGoldReward)
            return 0;
        int[] costs = rows
            .Where(row => row != null
                && row.Kind == NetherStrategyVisibleContentKind.ShopInventory
                && row.IsKnown
                && row.UsesNetherGold
                && IsPermittedGoldProcurement(row)
                && (NetherCanonicalRewardTierProvider.IsCanonicalGoldRankFiveShopRow(row)
                    || NetherCanonicalRewardTierProvider.IsAuthoritativeShopKeyRow(row))
                && row.Cost > 0
                && projectedGold >= row.Cost
                && row.NodeId != eventNodeId
                && IsLaterOnSelectedPath(route, eventNodeId, row.NodeId)
                && HasSelectedTerminalPath(row.NodeId, route, floors, context))
            .Select(row => row.Cost)
            .Distinct()
            .OrderBy(cost => cost)
            .ToArray();
        return costs.Length == 0 ? 0 : costs[0];
    }

    private static bool IsPermittedGoldProcurement(NetherStrategyVisibleContentRow row) =>
        row.Cost is 200 or 300 or 500
        && row.Amount > 0
        && row.UsesNetherGold;

    private static bool HasAffordableRankFiveTreasure(
        IReadOnlyList<NetherStrategyVisibleContentRow> rows,
        long eventNodeId,
        NetherRoutePlan route,
        IReadOnlyList<NetherFloorNode> floors,
        NetherRouteSafetyContext context,
        int projectedKeys,
        bool requiresKeyReward
    )
    {
        if (!requiresKeyReward || projectedKeys < 1)
            return false;
        foreach (NetherStrategyVisibleContentRow treasure in rows.Where(row =>
            row != null
            && row.Kind == NetherStrategyVisibleContentKind.Treasure
            && row.IsKnown
            && row.NodeId != eventNodeId
            && IsLaterOnSelectedPath(route, eventNodeId, row.NodeId)
            && HasSelectedTerminalPath(row.NodeId, route, floors, context)))
        {
            if (NetherRankFiveKeyProcurementPolicy.TryFindRankFiveTreasureIdentity(
                    rows,
                    treasure,
                    out NetherRankFiveTreasureIdentity _
                ))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsSelectedPathNode(NetherRoutePlan route, long nodeId) =>
        route.SelectedPathNodeIds != null && route.SelectedPathNodeIds.Contains(nodeId);

    private static bool IsLaterOnSelectedPath(NetherRoutePlan route, long startNodeId, long targetNodeId)
    {
        if (route.SelectedPathNodeIds == null)
            return false;
        int startIndex = NetherPathIndexUtility.PathIndexOf(route.SelectedPathNodeIds, startNodeId);
        int targetIndex = NetherPathIndexUtility.PathIndexOf(route.SelectedPathNodeIds, targetNodeId);
        return startIndex >= 0 && targetIndex > startIndex;
    }

    private static bool HasSelectedTerminalPath(
        long startNodeId,
        NetherRoutePlan route,
        IReadOnlyList<NetherFloorNode> floors,
        NetherRouteSafetyContext context
    )
    {
        if (route.SelectedPathNodeIds == null)
            return false;
        int startIndex = NetherPathIndexUtility.PathIndexOf(route.SelectedPathNodeIds, startNodeId);
        if (startIndex < 0)
            return false;
        Dictionary<long, NetherFloorNode> nodes = floors
            .Where(node => node != null && node.NodeId > 0)
            .GroupBy(node => node.NodeId)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single());
        for (int index = startIndex; index < route.SelectedPathNodeIds.Count; index++)
        {
            long nodeId = route.SelectedPathNodeIds[index];
            if (!IsSafeNode(context, nodeId) || !nodes.TryGetValue(nodeId, out NetherFloorNode? node))
                return false;
            if (index == route.SelectedPathNodeIds.Count - 1)
                return node.NodeType == NetherFloorNodeType.Boss;
        }
        return false;
    }

    private static bool IsSafeNode(NetherRouteSafetyContext context, long nodeId) =>
        context.KnownNodeByFloorId.TryGetValue(nodeId, out bool known)
        && known
        && context.HardSafeByFloorId.TryGetValue(nodeId, out bool hardSafe)
        && hardSafe;

    private static Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> Normalize(
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? commitments
    )
    {
        var exact = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
        if (commitments == null)
            return exact;
        foreach ((NetherInteractiveEventOptionKey key, NetherEventProcurementBudget budget) in commitments)
        {
            if (IsUsableKey(key) && budget.IsValid)
                exact[key] = budget;
        }
        return exact;
    }

    public static IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
        FromCommitments(IReadOnlyDictionary<NetherEventCommitmentKey, NetherEventCommitment>? commitments)
    {
        var budgets = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
        if (commitments == null)
            return budgets;

        foreach ((NetherEventCommitmentKey key, NetherEventCommitment commitment) in commitments)
        {
            if (commitment == null || !commitment.IsValid)
                continue;
            NetherEventProcurementBudget budget = new(
                commitment.CommittedGoldMinimum,
                commitment.CommittedKeyMinimum
            );
            if (budget.IsValid)
            {
                budgets[new NetherInteractiveEventOptionKey(key.EventId, key.EventPartId, key.OptionNumber)] = budget;
            }
        }
        return budgets;
    }

    public static IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> Merge(
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? first,
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? second
    )
    {
        var merged = first == null
            ? new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>()
            : first
                .Where(pair => IsUsableKey(pair.Key) && pair.Value.IsValid)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        if (second != null)
        {
            foreach ((NetherInteractiveEventOptionKey key, NetherEventProcurementBudget budget) in second)
            {
                if (IsUsableKey(key) && budget.IsValid)
                    merged[key] = budget;
            }
        }
        return merged;
    }

    private static bool IsUsableKey(NetherInteractiveEventOptionKey key) =>
        key.EventId > 0 && key.EventPartId > 0 && key.OptionNumber > 0;
}

/// <summary>
/// The complete read-only runtime inputs consumed by production route planning.  Individual
/// entries remain nullable/unknown so a missing master or runtime observation cannot be
/// converted to the old permissive <c>current &lt; 100</c> / <c>HP &gt; 0</c> maps.
/// </summary>
internal sealed record NetherRuntimeRouteSafetyData
{
    public IReadOnlyDictionary<long, NetherFloorMasterBounds> FloorBoundsByFloorId { get; init; } =
        new Dictionary<long, NetherFloorMasterBounds>();
    public NetherActivePartyHpSafety ActivePartyHp { get; init; } = new(
        IsKnown: false,
        MinimumHpPermille: null,
        Detail: "missing-active-party-hp"
    );
    public NetherActiveCodeErosionProjection ActiveCodeErosion { get; init; } = new()
    {
        ErosionProjectionKnown = false,
        CodeHash = "nether-codes:unknown",
        Detail = "missing-active-code-erosion-projection",
    };
    /// <summary>
    /// Exact route-owned procurement commitments carried from the prior authoritative capture.
    /// Native Event rows do not expose these hidden branch budgets; an absent entry means no
    /// repository-owned commitment, never a guessed minimum.
    /// </summary>
    public IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> EventProcurementCommitments { get; init; } =
        new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
    /// <summary>Identity of the route branch that produced <see cref="EventProcurementCommitments"/>.</summary>
    public NetherRouteBranchIdentity? RouteIdentity { get; init; }
    /// <summary>
    /// Exact visible branch rows captured by the same FloorSelection owner.  Procurement
    /// generation may use only known Shop/Treasure rows from this package; absent or
    /// non-materialized inventory remains an unknown source.
    /// </summary>
    public NetherStrategyVisibleMapEvidence? VisibleMap { get; init; }
    /// <summary>
    /// Exact operational Research priority from the accepted strategy package. Null is retained
    /// when no package can be correlated; when only the future native settlement result is missing,
    /// the objective resolver supplies conservative incomplete-family priority instead.
    /// </summary>
    public bool? ResearchIncomplete { get; init; }
    /// <summary>
    /// Complete Recovery transform eligibility captured by Controller from the held-code policy
    /// seam. It is copied into each branch proof before that proof is rebound to native capture.
    /// </summary>
    public NetherCodeTransformEligibilityEvidence? RecoveryTransformEligibility { get; init; }
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// One production route decision and the exact pre-mutation battle evidence created for each
/// safe combat candidate.  The Controller stores the selected payload in the pending floor
/// action before invoking native selection.
/// </summary>
internal sealed record NetherProductionRouteSafetyPlan
{
    public NetherRoutePlan Route { get; init; } = new();
    public NetherRouteSafetyContext Context { get; init; } = new();
    public IReadOnlyDictionary<long, NetherBattleProjectionPayload> BattleProjectionByFloorId { get; init; } =
        new Dictionary<long, NetherBattleProjectionPayload>();
    public IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
        EventProcurementCommitments { get; init; } =
        new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
    public NetherRouteBranchIdentity? RouteIdentity { get; init; }
    public NetherRankFiveKeyProcurementDecision RankFiveKeyProcurement { get; init; } =
        NetherRankFiveKeyProcurementDecision.Unknown("rank-five-procurement-not-evaluated");
    /// <summary>
    /// Exact Recovery option proofs executable before the next unsettled Battle on the selected
    /// route. The bridge binds this map only to native rows owning these parts; later Recovery
    /// rows are re-evaluated from that Battle's authoritative snapshot.
    /// </summary>
    public IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> RecoveryBranchSafetyByPartId { get; init; } =
        new Dictionary<long, NetherRecoveryBranchSafetyEvidence>();
    public NetherCodeTransformEligibilityEvidence? RecoveryTransformEligibility { get; init; }
}

/// <summary>
/// Production wiring for the safety pipeline:
/// runtime master/HP/code observations → battle projection → full route-safety context → route
/// planner.  It intentionally owns no Unity/reflection/API operation, which makes the same
/// Controller decision chain executable in characterization tests.
/// </summary>
internal sealed class NetherRouteSafetyProductionCoordinator
{
    private const int HardErosionLimit = 100;
    // Server battle settlement currently starts from +5 erosion. Active Nether-code
    // addition/rate effects are applied by NetherErosionPolicy, so the effective result can
    // still be 0, 10, or another value. MNetherMapFloors.min/max_erosion_point are map-row
    // generation eligibility bounds and must never replace this battle cost.
    private const int BattleBaseErosionIncrease = 5;
    private readonly NetherBattleRouteProjectionBuilder _battleProjectionBuilder = new();
    private readonly NetherFloorMasterBoundsMapper _floorBoundsMapper = new();
    private readonly NetherRouteSafetyContextBuilder _contextBuilder = new();
    private readonly NetherRoutePlanner _routePlanner = new();
    private readonly NetherRankFiveKeyProcurementPolicy _rankFiveKeyProcurementPolicy = new();
    private readonly NetherErosionPolicy _erosionPolicy = new();
    private readonly NetherRouteHorizonSafetyPolicy _horizonSafetyPolicy = new();

    private readonly record struct RankFiveBattleEntryProjection(
        int? MaximumBattleEntryErosionPoint,
        bool RecoveryToSeventyOrBelowCertainBeforeNextBattle
    );

    private readonly record struct RecoveryBranchSimulation(
        bool IsKnown,
        bool IsSafe,
        string Detail
    );

    public NetherProductionRouteSafetyPlan Plan(
        NetherSnapshot snapshot,
        int effectiveMaximumDepth,
        NetherAutoClimbSettings settings,
        NetherRuntimeRouteSafetyData runtime,
        NetherRuntimeInteractivePreEntryInputsResult? interactivePreEntry = null
    )
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (runtime == null)
            throw new ArgumentNullException(nameof(runtime));

        IReadOnlyList<NetherFloorNode> serverFloors = snapshot.Floors ?? Array.Empty<NetherFloorNode>();
        HashSet<long> necessaryTerminalIds = ResolveNecessaryTerminalFloorIds(serverFloors);
        HashSet<long> exactCombatPreEntryIds = ResolveCombatNodesBeforeFirstUnsettledBattle(
            snapshot,
            serverFloors
        );
        var floorInputs = new List<NetherRouteSafetyFloorInput>(serverFloors.Count);
        var safeExitKnown = new Dictionary<long, bool>();
        var payloads = new Dictionary<long, NetherBattleProjectionPayload>();

        foreach (NetherFloorNode floor in serverFloors)
        {
            if (floor == null || floor.FloorId <= 0 || floor.NodeId <= 0)
                continue;

            bool necessaryTerminal = necessaryTerminalIds.Contains(floor.NodeId);
            if (!IsCombat(floor.NodeType))
            {
                if (TryBuildInteractiveSafetyInput(
                        snapshot,
                        floor,
                        settings,
                        interactivePreEntry,
                        runtime.ActiveCodeErosion,
                        out NetherFloorSafetyInput interactiveInput,
                        out NetherInteractiveWorstCaseProjection interactiveProjection,
                        out NetherRouteHpRule hpRule
                    ))
                {
                    floorInputs.Add(new NetherRouteSafetyFloorInput(
                        floor,
                        interactiveInput,
                        // Every server-possible row contributes to the pre-click worst case.
                        // The owned popup flow still validates the exact row/option before it
                        // mutates state, so this aggregate never replaces identity evidence.
                        ProjectedHpDelta: interactiveProjection.HpDelta,
                        SafeCodeOpportunity: 0
                    )
                    {
                        Detail = "interactive:known",
                        HpRule = hpRule,
                    });
                    safeExitKnown[floor.NodeId] = true;
                }
                else
                {
                    floorInputs.Add(new NetherRouteSafetyFloorInput(
                        floor,
                        UnknownInput(snapshot, floor, necessaryTerminal),
                        ProjectedHpDelta: null,
                        SafeCodeOpportunity: null
                    )
                    {
                        Detail = DescribeInteractiveFailure(floor, interactivePreEntry),
                    });
                    safeExitKnown[floor.NodeId] = false;
                }
                continue;
            }

            NetherBattleRouteProjection projection = BuildCombatProjection(
                snapshot,
                floor,
                necessaryTerminal,
                settings,
                runtime
            );
            NetherFloorSafetyInput evaluationInput = projection.EvaluatorInput
                ?? UnknownInput(snapshot, floor, necessaryTerminal);
            bool hasExactPreEntryHp = exactCombatPreEntryIds.Contains(floor.NodeId);
            floorInputs.Add(new NetherRouteSafetyFloorInput(
                floor,
                evaluationInput,
                // Fresh native evidence exposes post-battle HP only on
                // NetherClearBattleResponseEntity.t_nether_characters. Do not turn the absence
                // of that future response into a fabricated zero-damage result.
                ProjectedHpDelta: null,
                SafeCodeOpportunity: projection.EvaluatorInput == null ? null : 0
            )
            {
                Detail = DescribeCombatInputs(floor, runtime, projection),
                HasExactPreEntryHpEvidence = hasExactPreEntryHp,
            });
            safeExitKnown[floor.NodeId] = projection.EvaluatorInput != null;

            if (projection.IsSafe && hasExactPreEntryHp)
            {
                payloads[floor.NodeId] = CreatePayload(
                    snapshot,
                    floor,
                    projection,
                    runtime.ActiveCodeErosion.CodeHash
                );
            }
        }

        NetherRouteSafetyContext context = _contextBuilder.Build(new NetherRouteSafetyContextBuilderInput(
            Floors: floorInputs,
            NecessaryTerminalFloorIds: necessaryTerminalIds,
            SafeExitKnownByFloorId: safeExitKnown,
            MaximumFloorLevel: effectiveMaximumDepth
        )
        {
            StrategyMode = settings.StrategyMode,
            PrimaryResearchFamily = settings.ResearchPrimaryFamily,
        });
        NetherRankFiveKeyProcurementDecision[] horizonProcurementDecisions =
            EvaluateRankFiveHorizonDecisions(snapshot, context, serverFloors, runtime.VisibleMap);
        context = context with
        {
            MandatoryRankFiveKeyObjectiveNodeIds = horizonProcurementDecisions
                .Where(decision => decision.IsKnown && decision.HasMandatoryObjective)
                .Select(decision => decision.Objective.ObjectiveNodeId)
                .Where(nodeId => nodeId > 0)
                .ToHashSet(),
            StrategyMode = settings.StrategyMode,
            PrimaryResearchFamily = settings.ResearchPrimaryFamily,
            ResearchIncomplete = runtime.ResearchIncomplete,
            StrategySettings = settings,
            EventProcurementCommitments = runtime.EventProcurementCommitments,
            VisibleMap = runtime.VisibleMap,
            InteractivePreEntry = interactivePreEntry,
        };
        NetherRoutePlan route = _routePlanner.Plan(snapshot, context);
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> routeCommitments =
            BuildRouteOwnedCommitments(
                snapshot,
                route,
                context,
                runtime,
                interactivePreEntry
            );
        bool procurementFixedPoint = false;
        // Branch-local commitments are discovered from the selected route's exact pre-entry
        // capture. They must become planner input before the authoritative route audit is built;
        // otherwise the audit records the provisional count even though the returned plan carries
        // the newly generated budgets. Replan until the route and its owned commitment map agree,
        // with a bounded fail-closed guard for an impossible oscillating capture.
        for (int iteration = 0; iteration < 4; iteration++)
        {
            if (ProcurementCommitmentsEqual(context.EventProcurementCommitments, routeCommitments))
            {
                procurementFixedPoint = true;
                break;
            }

            context = context with
            {
                EventProcurementCommitments = routeCommitments,
            };
            route = _routePlanner.Plan(snapshot, context);
            routeCommitments = BuildRouteOwnedCommitments(
                snapshot,
                route,
                context,
                runtime,
                interactivePreEntry
            );
        }
        if (!procurementFixedPoint
            && ProcurementCommitmentsEqual(context.EventProcurementCommitments, routeCommitments))
        {
            procurementFixedPoint = true;
        }
        if (!procurementFixedPoint)
        {
            route = route with
            {
                SelectedNode = null,
                SelectedPathNodeIds = Array.Empty<long>(),
                BranchIdentity = null,
                PauseReason = NetherPauseReason.UnknownMasterData,
                PauseDetail = "route-owned-procurement-fixed-point-unavailable",
            };
            routeCommitments = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
            context = context with
            {
                EventProcurementCommitments = routeCommitments,
            };
        }
        NetherRankFiveKeyProcurementDecision rankFiveProcurement = EvaluateRankFiveSelectedRoute(
            snapshot,
            context,
            serverFloors,
            runtime.VisibleMap,
            route.SelectedPathNodeIds
        );
        IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> recoveryBranchSafety =
            BuildRecoveryBranchSafetyEvidence(
                snapshot,
                context,
                route,
                interactivePreEntry,
                runtime.RecoveryTransformEligibility
            );
        return new NetherProductionRouteSafetyPlan
        {
            Route = route,
            Context = context,
            BattleProjectionByFloorId = payloads,
            EventProcurementCommitments = routeCommitments,
            RouteIdentity = route.BranchIdentity,
            RankFiveKeyProcurement = rankFiveProcurement,
            RecoveryBranchSafetyByPartId = recoveryBranchSafety,
            RecoveryTransformEligibility = runtime.RecoveryTransformEligibility,
        };
    }

    private static IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
        BuildRouteOwnedCommitments(
            NetherSnapshot snapshot,
            NetherRoutePlan route,
            NetherRouteSafetyContext context,
            NetherRuntimeRouteSafetyData runtime,
            NetherRuntimeInteractivePreEntryInputsResult? interactivePreEntry
        )
    {
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> runtimeCommitments =
            runtime.RouteIdentity is { } runtimeIdentity
                && route.BranchIdentity is { } routeIdentity
                && runtimeIdentity == routeIdentity
                ? runtime.EventProcurementCommitments
                : runtime.RouteIdentity == null
                    ? runtime.EventProcurementCommitments
                    : new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> routeCommitments =
            NetherRouteOwnedEventProcurementProducer.Merge(
                runtimeCommitments,
                NetherRouteOwnedEventProcurementProducer.FromInteractivePreEntry(
                    snapshot,
                    route,
                    interactivePreEntry
                )
            );
        return NetherRouteOwnedEventProcurementProducer.Merge(
            routeCommitments,
            NetherRouteOwnedEventProcurementProducer.FromSelectedVisibleBranch(
                snapshot,
                route,
                context,
                interactivePreEntry,
                runtime.VisibleMap
            )
        );
    }

    private static bool ProcurementCommitmentsEqual(
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? first,
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? second
    )
    {
        if (first == null || second == null || first.Count != second.Count)
            return first == null && second == null;
        foreach ((NetherInteractiveEventOptionKey key, NetherEventProcurementBudget budget) in first)
        {
            if (!second.TryGetValue(key, out NetherEventProcurementBudget other)
                || other != budget)
                return false;
        }
        return true;
    }

    private NetherRankFiveKeyProcurementDecision[] EvaluateRankFiveHorizonDecisions(
        NetherSnapshot snapshot,
        NetherRouteSafetyContext context,
        IReadOnlyList<NetherFloorNode> floors,
        NetherStrategyVisibleMapEvidence? visibleMap
    )
    {
        if (visibleMap == null || visibleMap.ContentRows == null)
            return Array.Empty<NetherRankFiveKeyProcurementDecision>();
        return context.HorizonEvaluationByFloorId
            .Where(pair => pair.Value != null && pair.Value.IsEligible && pair.Value.Steps.Count > 0)
            .Select(pair => EvaluateRankFivePath(
                snapshot,
                context,
                floors,
                visibleMap,
                pair.Value.Steps.Select(step => step.NodeId).ToArray()
            ))
            .ToArray();
    }

    private NetherRankFiveKeyProcurementDecision EvaluateRankFiveSelectedRoute(
        NetherSnapshot snapshot,
        NetherRouteSafetyContext context,
        IReadOnlyList<NetherFloorNode> floors,
        NetherStrategyVisibleMapEvidence? visibleMap,
        IReadOnlyList<long> selectedPathNodeIds
    ) => selectedPathNodeIds == null || selectedPathNodeIds.Count < 2
        ? NetherRankFiveKeyProcurementDecision.Unknown("rank-five-selected-safe-branch-unavailable")
        : EvaluateRankFivePath(snapshot, context, floors, visibleMap, selectedPathNodeIds);

    private NetherRankFiveKeyProcurementDecision EvaluateRankFivePath(
        NetherSnapshot snapshot,
        NetherRouteSafetyContext context,
        IReadOnlyList<NetherFloorNode> floors,
        NetherStrategyVisibleMapEvidence? visibleMap,
        IReadOnlyList<long> path
    )
    {
        if (visibleMap == null || path == null || path.Count < 2)
            return NetherRankFiveKeyProcurementDecision.Unknown("rank-five-selected-safe-branch-unavailable");
        RankFiveBattleEntryProjection battleEntry = ProjectRankFiveBattleEntry(
            context,
            floors,
            visibleMap,
            path
        );
        return _rankFiveKeyProcurementPolicy.Evaluate(new NetherRankFiveKeyProcurementInput(
            snapshot.NetherGold,
            snapshot.TreasureKeyCount,
            snapshot.Characters
                .Where(character => character.IsActive)
                .Select(character => character.HpPermille)
                .ToArray(),
            path,
            context.HardSafeByFloorId
                .Where(pair => pair.Value)
                .Select(pair => pair.Key)
                .ToHashSet(),
            floors,
            visibleMap.ContentRows
        )
        {
            AlreadyEnteredNodeId = snapshot.CurrentNodeId > 0
                ? snapshot.CurrentNodeId
                : snapshot.CurrentFloorId,
            MaximumBattleEntryErosionPoint = battleEntry.MaximumBattleEntryErosionPoint,
            RecoveryToSeventyOrBelowCertainBeforeNextBattle =
                battleEntry.RecoveryToSeventyOrBelowCertainBeforeNextBattle,
        });
    }

    private static RankFiveBattleEntryProjection ProjectRankFiveBattleEntry(
        NetherRouteSafetyContext context,
        IReadOnlyList<NetherFloorNode> floors,
        NetherStrategyVisibleMapEvidence visibleMap,
        IReadOnlyList<long> path
    )
    {
        if (context == null || floors == null || visibleMap?.ContentRows == null || path == null)
            return new RankFiveBattleEntryProjection(null, false);

        NetherRouteHorizonSafetyEvaluation? horizon = context.HorizonEvaluationByFloorId.Values
            .Where(candidate => candidate != null
                && candidate.IsEligible
                && candidate.HorizonSteps != null
                && candidate.HorizonSteps.Count == path.Count
                && candidate.HorizonSteps.Select(step => step.NodeId).SequenceEqual(path))
            .FirstOrDefault();
        if (horizon == null
            || horizon.HorizonSteps.Count != horizon.Steps.Count
            || horizon.HorizonSteps.Count != path.Count)
        {
            return new RankFiveBattleEntryProjection(null, false);
        }

        Dictionary<long, NetherFloorNode> floorByNodeId = floors
            .Where(floor => floor != null && floor.NodeId > 0)
            .GroupBy(floor => floor.NodeId)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single());
        if (floorByNodeId.Count != path.Distinct().Count())
            return new RankFiveBattleEntryProjection(null, false);

        int objectiveIndex = -1;
        foreach (NetherStrategyVisibleContentRow treasure in visibleMap.ContentRows.Where(row =>
            row != null
            && row.Kind == NetherStrategyVisibleContentKind.Treasure
            && row.IsKnown
            && row.NodeId > 0))
        {
            if (!NetherRankFiveKeyProcurementPolicy.TryFindRankFiveTreasureIdentity(
                    visibleMap.ContentRows,
                    treasure,
                    out _
                ))
                continue;
            int index = NetherPathIndexUtility.PathIndexOf(path, treasure.NodeId);
            if (index <= 0)
                continue;
            if (objectiveIndex >= 0)
                return new RankFiveBattleEntryProjection(null, false);
            objectiveIndex = index;
        }
        if (objectiveIndex <= 0)
            return new RankFiveBattleEntryProjection(null, false);

        var sourceMatches = new List<(int Index, NetherStrategyVisibleEventOptionEvidence Option)>();
        foreach (NetherStrategyVisibleContentRow row in visibleMap.ContentRows.Where(row =>
            row != null
            && row.Kind == NetherStrategyVisibleContentKind.Event
            && row.IsKnown))
        {
            int index = NetherPathIndexUtility.PathIndexOf(path, row.NodeId);
            if (index <= 0 || index >= objectiveIndex)
                continue;
            foreach (NetherStrategyVisibleEventOptionEvidence option in
                row.EventOptions ?? Array.Empty<NetherStrategyVisibleEventOptionEvidence>())
            {
                if (NetherRankFiveKeyProcurementPredicates.IsExactErosionPaidEventKeyOption(option))
                    sourceMatches.Add((index, option));
            }
        }
        if (sourceMatches.Count != 1)
            return new RankFiveBattleEntryProjection(null, false);

        int sourceIndex = sourceMatches[0].Index;
        int nextBattleIndex = -1;
        for (int index = sourceIndex + 1; index < path.Count; index++)
        {
            if (floorByNodeId[path[index]].NodeType is NetherFloorNodeType.Battle
                or NetherFloorNodeType.MiniBoss
                or NetherFloorNodeType.Boss)
            {
                nextBattleIndex = index;
                break;
            }
        }
        if (nextBattleIndex < 0)
            return new RankFiveBattleEntryProjection(null, false);

        int maximumBattleEntryErosion = int.MinValue;
        for (int index = nextBattleIndex; index < horizon.Steps.Count; index++)
        {
            NetherFloorNodeType nodeType = floorByNodeId[horizon.Steps[index].NodeId].NodeType;
            if (nodeType is NetherFloorNodeType.Battle
                or NetherFloorNodeType.MiniBoss
                or NetherFloorNodeType.Boss)
            {
                maximumBattleEntryErosion = Math.Max(
                    maximumBattleEntryErosion,
                    horizon.Steps[index].StartErosion
                );
            }
        }
        if (maximumBattleEntryErosion == int.MinValue)
            return new RankFiveBattleEntryProjection(null, false);

        bool recoveredBeforeNextBattle = false;
        for (int index = sourceIndex + 1; index < nextBattleIndex; index++)
        {
            NetherRouteHorizonStep step = horizon.HorizonSteps[index];
            NetherRouteHorizonStepAudit audit = horizon.Steps[index];
            if (step.NodeType == NetherFloorNodeType.Recovery
                && step.IsConfirmedRecovery
                && audit.ProjectedErosion < audit.StartErosion
                && audit.ProjectedErosion <= 70)
            {
                recoveredBeforeNextBattle = true;
                break;
            }
        }
        return new RankFiveBattleEntryProjection(
            maximumBattleEntryErosion,
            maximumBattleEntryErosion <= 70 && recoveredBeforeNextBattle
        );
    }

    private IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence>
        BuildRecoveryBranchSafetyEvidence(
            NetherSnapshot snapshot,
            NetherRouteSafetyContext context,
            NetherRoutePlan route,
            NetherRuntimeInteractivePreEntryInputsResult? interactivePreEntry,
            NetherCodeTransformEligibilityEvidence? transformEligibility
        )
    {
        var proofs = new Dictionary<long, NetherRecoveryBranchSafetyEvidence>();
        if (context == null
            || route == null
            || route.SelectedPathNodeIds == null
            || route.SelectedPathNodeIds.Count < 2
            || interactivePreEntry == null
            || !interactivePreEntry.IsSuccess)
            return proofs;

        foreach ((long nodeId, NetherRuntimeInteractivePreEntryCaptureResult capture) in interactivePreEntry.ByFloorNodeId)
        {
            if (capture?.Input == null
                || !capture.IsCaptured
                || capture.Input.FloorKind != NetherFloorNodeType.Recovery)
                continue;
            // The dictionary key is runtime node identity, while the copied input also carries
            // that identity. A mismatch means this capture cannot authorize another branch.
            if (nodeId <= 0
                || capture.Input.FloorNodeId != nodeId
                || !route.SelectedPathNodeIds.Contains(nodeId)
                // The native Battle clear response owns the next party HP. A Recovery beyond that
                // Battle must be planned from the fresh response, not bound to this stale snapshot.
                || NetherRecoveryBranchProofScope.IsDeferredUntilBattleReplan(snapshot, route, nodeId))
                continue;
            NetherRouteHorizonSafetyEvaluation? horizon = context.HorizonEvaluation(nodeId);
            bool complete = IsSelectedCompleteHorizon(route.SelectedPathNodeIds, nodeId, horizon);
            NetherCodeTransformEligibilityEvidence? branchTransformEligibility =
                BuildRecoveryTransformEligibility(
                    transformEligibility,
                    capture.Input,
                    capture.Safety.OptionProjectionByKey
                );
            foreach ((NetherInteractiveEventOptionKey key, NetherInteractiveOptionProjection projection)
                in capture.Safety.OptionProjectionByKey)
            {
                if (projection == null
                    || projection.NodeId != nodeId
                    || projection.FloorId <= 0
                    || key.EventId <= 0
                    || key.EventPartId <= 0
                    || key.OptionNumber <= 0)
                    continue;
                NetherRecoveryBranchKind branchKind = MapRecoveryBranchKind(projection.ExpectedEffects);
                bool authoritative = complete && branchKind != NetherRecoveryBranchKind.Unknown;
                bool carriedProofMatches = capture.Input.RecoveryBranchSafetyByPartId.TryGetValue(
                    key.EventPartId,
                    out NetherRecoveryBranchSafetyEvidence? carriedProof
                )
                    && carriedProof.IsAuthoritative
                    && carriedProof.BranchKind == branchKind;
                RecoveryBranchSimulation simulation = authoritative
                    ? SimulateRecoveryBranch(capture.Input, horizon, projection)
                    : new RecoveryBranchSimulation(false, false, context.HorizonRejection(nodeId));
                bool optionEvidenceKnown = projection.IsKnown && projection.HasRouteSafetyEvidence
                    || carriedProofMatches
                        && projection.ExpectedEffects != null
                        && projection.ExpectedEffects.Count > 0
                        && projection.ExpectedEffects.All(effect => effect != null
                            && effect.Known
                            && effect.ContentKnown);
                bool routeSafetyAllowed = projection.RouteSafetyAllowed
                    || carriedProofMatches && carriedProof!.IsNextVisibleBranchSafe;
                bool isKnown = authoritative && optionEvidenceKnown && simulation.IsKnown;
                var evidence = new NetherRecoveryBranchSafetyEvidence
                {
                    BranchKind = branchKind,
                    IsKnown = isKnown,
                    IsCompleteVisibleBranch = complete,
                    IsNextVisibleBranchSafe = isKnown
                        && routeSafetyAllowed
                        && simulation.IsSafe,
                    UnknownReason = !isKnown
                        ? !authoritative
                            ? complete
                                ? "recovery-branch-kind-or-option-proof-unavailable"
                                : context.HorizonRejection(nodeId)
                            : !optionEvidenceKnown
                                ? projection.UnknownReason.Length == 0
                                    ? "recovery-option-proof-unavailable"
                                    : projection.UnknownReason
                                : simulation.Detail
                        : simulation.IsSafe && routeSafetyAllowed
                            ? string.Empty
                            : string.IsNullOrWhiteSpace(simulation.Detail)
                                ? projection.RouteSafetyUnknownReason
                                : simulation.Detail,
                    TransformEligibility = branchTransformEligibility,
                };
                if (key.EventPartId <= 0)
                    continue;
                if (proofs.TryGetValue(key.EventPartId, out NetherRecoveryBranchSafetyEvidence? existing)
                    && existing != evidence)
                {
                    proofs[key.EventPartId] = new NetherRecoveryBranchSafetyEvidence
                    {
                        UnknownReason = "ambiguous-recovery-branch-proof-identity",
                    };
                }
                else
                {
                    proofs[key.EventPartId] = evidence;
                }
            }
        }
        return proofs;
    }

    private static NetherCodeTransformEligibilityEvidence? BuildRecoveryTransformEligibility(
        NetherCodeTransformEligibilityEvidence? heldCodeEligibility,
        NetherInteractiveFloorPreEntrySafetyInput input,
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>? projections
    )
    {
        if (heldCodeEligibility == null)
            return null;
        NetherInteractiveOptionProjection[] options = (projections?.Values
                ?? Array.Empty<NetherInteractiveOptionProjection>())
            .Where(option => option != null)
            .GroupBy(option => new NetherInteractiveEventOptionKey(
                option.EventId,
                option.EventPartId,
                option.OptionNumber
            ))
            .Where(group => group.Count() == 1)
            .Select(group => group.Single())
            .ToArray();
        NetherInteractiveOptionProjection? rest = options.FirstOrDefault(option =>
            MapRecoveryBranchKind(option.ExpectedEffects) == NetherRecoveryBranchKind.Rest);
        NetherInteractiveOptionProjection? purification = options.FirstOrDefault(option =>
            MapRecoveryBranchKind(option.ExpectedEffects) == NetherRecoveryBranchKind.Purification);
        if (options.Length != 3
            || rest == null
            || purification == null
            || input.CurrentErosion is not int currentErosion
            || input.ActiveHpPermille == null
            || input.Settings == null
            || rest.ExpectedEffects == null
            || purification.ExpectedEffects == null
            || rest.ExpectedEffects.Any(effect => effect == null || !effect.Known || !effect.ContentKnown)
            || purification.ExpectedEffects.Any(effect => effect == null || !effect.Known || !effect.ContentKnown))
        {
            return heldCodeEligibility with
            {
                IsKnown = false,
                UnknownReason = "recovery-transform-choice-value-unavailable",
                DeterministicRecoveryChoicesHaveZeroValue = false,
            };
        }

        bool restHasValue = rest.ExpectedEffects.Count == 1
            && rest.ExpectedEffects[0].Kind == NetherEffectKind.Heal
            && input.ActiveHpPermille.Any(hp => hp < 1000)
            && rest.ExpectedEffects[0].Amount > 0;
        NetherErosionProjection purificationProjection = new NetherErosionPolicy().ProjectEffects(
            currentErosion,
            purification.ExpectedEffects,
            input.Settings.SoftErosionLimit,
            isMandatoryBoss: false
        );
        bool purificationHasValue = !purificationProjection.IsAllowed
            || purificationProjection.ProjectedErosion != currentErosion;
        return heldCodeEligibility with
        {
            DeterministicRecoveryChoicesHaveZeroValue = !restHasValue && !purificationHasValue,
        };
    }

    private RecoveryBranchSimulation SimulateRecoveryBranch(
        NetherInteractiveFloorPreEntrySafetyInput input,
        NetherRouteHorizonSafetyEvaluation? horizon,
        NetherInteractiveOptionProjection projection
    )
    {
        if (input == null
            || horizon == null
            || !horizon.IsEligible
            || horizon.HorizonSteps == null
            || horizon.HorizonSteps.Count < 2
            || horizon.HorizonSteps.Count != horizon.Steps.Count
            || input.FloorNodeId <= 0
            || horizon.HorizonSteps[0].NodeId != input.FloorNodeId
            || !input.CurrentErosion.HasValue
            || input.ActiveHpPermille == null
            || projection.ExpectedEffects == null
            || projection.ExpectedEffects.Count == 0
            || projection.ExpectedEffects.Any(effect => effect == null
                || !effect.Known
                || !effect.ContentKnown))
        {
            return new RecoveryBranchSimulation(
                false,
                false,
                "recovery-branch-suffix-projection-unavailable"
            );
        }

        NetherRouteHorizonStep[] steps = horizon.HorizonSteps.ToArray();
        NetherRouteHorizonStep first = steps[0];
        NetherErosionProjection projected = _erosionPolicy.ProjectEffects(
            input.CurrentErosion.Value,
            projection.ExpectedEffects,
            first.ErosionModifiers,
            70,
            isMandatoryBoss: true
        );
        if (!projected.IsAllowed)
        {
            return new RecoveryBranchSimulation(
                true,
                false,
                "recovery-branch-erosion-projection:" + projected.Detail
            );
        }

        steps[0] = first with
        {
            // The branch effect has already consumed the active code modifiers above. Collapse
            // that exact result into the option step so the suffix is not double-applied.
            BaseErosionDelta = projected.ProjectedErosion - input.CurrentErosion.Value,
            HpDeltaPermille = projection.HpDelta,
            ErosionModifiers = Array.Empty<NetherErosionModifier>(),
        };
        NetherRouteHorizonSafetyEvaluation result = _horizonSafetyPolicy.Evaluate(
            new NetherRouteHorizonSafetyInput(
                input.CurrentErosion.Value,
                input.ActiveHpPermille,
                steps,
                70,
                HardErosionLimit
            )
            {
                IsVisibleHorizonComplete = true,
                StrategyMode = input.Settings?.StrategyMode ?? NetherStrategyMode.Equipment,
                PrimaryResearchFamily = input.Settings?.ResearchPrimaryFamily ?? NetherCodeFamily.Unknown,
            }
        );
        return new RecoveryBranchSimulation(
            true,
            result.IsEligible,
            result.IsEligible
                ? string.Empty
                : string.IsNullOrWhiteSpace(result.RejectionDetail)
                    ? "recovery-branch-suffix-unsafe"
                    : result.RejectionDetail
        );
    }

    private static bool IsSelectedCompleteHorizon(
        IReadOnlyList<long> selectedPathNodeIds,
        long startNodeId,
        NetherRouteHorizonSafetyEvaluation? horizon
    )
    {
        if (horizon == null
            || !horizon.IsEligible
            || horizon.Steps == null
            || horizon.Steps.Count < 2
            || horizon.HorizonSteps == null
            || horizon.HorizonSteps.Count != horizon.Steps.Count
            || horizon.Steps[^1].NodeId <= 0
            || !selectedPathNodeIds.Contains(startNodeId))
            return false;

        int startIndex = -1;
        for (int index = 0; index < selectedPathNodeIds.Count; index++)
        {
            if (selectedPathNodeIds[index] == startNodeId)
            {
                startIndex = index;
                break;
            }
        }
        if (startIndex < 0
            || horizon.Steps.Count != selectedPathNodeIds.Count - startIndex
            || horizon.Steps[^1].NodeId != selectedPathNodeIds[^1])
            return false;
        for (int index = 0; index < horizon.Steps.Count; index++)
        {
            if (horizon.Steps[index].NodeId != selectedPathNodeIds[startIndex + index]
                || horizon.HorizonSteps[index].NodeId != selectedPathNodeIds[startIndex + index])
                return false;
        }
        return true;
    }

    private static NetherRecoveryBranchKind MapRecoveryBranchKind(IReadOnlyList<NetherEffect>? effects) =>
        effects is { Count: 1 }
            ? effects[0].Kind switch
            {
                NetherEffectKind.Heal => NetherRecoveryBranchKind.Rest,
                NetherEffectKind.ErosionHeal => NetherRecoveryBranchKind.Purification,
                NetherEffectKind.AbyssCodeTransform => NetherRecoveryBranchKind.Transform,
                _ => NetherRecoveryBranchKind.Unknown,
            }
            : NetherRecoveryBranchKind.Unknown;

    private static string DescribeCombatInputs(
        NetherFloorNode floor,
        NetherRuntimeRouteSafetyData runtime,
        NetherBattleRouteProjection projection
    )
    {
        string bounds = runtime.FloorBoundsByFloorId == null
            || !runtime.FloorBoundsByFloorId.TryGetValue(floor.NodeId, out NetherFloorMasterBounds mapped)
                ? "bounds:missing-runtime-node"
                : mapped.IsKnown
                    ? "bounds:known"
                    : "bounds:" + (string.IsNullOrEmpty(mapped.Detail) ? "unknown" : mapped.Detail);
        string hp = runtime.ActivePartyHp.IsKnown && runtime.ActivePartyHp.MinimumHpPermille.HasValue
            ? "hp:known"
            : "hp:" + (string.IsNullOrEmpty(runtime.ActivePartyHp.Detail) ? "unknown" : runtime.ActivePartyHp.Detail);
        NetherActiveCodeErosionProjection? code = runtime.ActiveCodeErosion;
        string codes = code != null && code.ErosionProjectionKnown
            ? "codes:known"
            : "codes:" + (string.IsNullOrEmpty(code?.Detail) ? "unknown" : code.Detail);
        string projectionDetail = projection.EvaluatorInput.HasValue
            ? "projection:known"
            : "projection:unknown";
        return string.Join("|", bounds, hp, codes, projectionDetail);
    }

    private static string DescribeInteractiveFailure(
        NetherFloorNode floor,
        NetherRuntimeInteractivePreEntryInputsResult? interactivePreEntry
    )
    {
        if (interactivePreEntry == null)
            return "interactive:missing-preentry-capture";
        if (!interactivePreEntry.IsSuccess)
            return "interactive:capture:" + (
                string.IsNullOrEmpty(interactivePreEntry.Detail)
                    ? "failed-without-detail"
                    : interactivePreEntry.Detail
            );
        if (interactivePreEntry.ByFloorNodeId == null
            || !interactivePreEntry.ByFloorNodeId.TryGetValue(
                floor.NodeId,
                out NetherRuntimeInteractivePreEntryCaptureResult? capture
            ))
        {
            return "interactive:missing-node-capture";
        }
        if (!capture.IsCaptured || capture.Input == null)
        {
            return "interactive:node-capture:" + (
                string.IsNullOrEmpty(capture.Detail) ? "invalid" : capture.Detail
            );
        }
        if (!capture.Safety.IsSafe)
        {
            return "interactive:safety:"
                + capture.Safety.PauseReason
                + ":"
                + (string.IsNullOrEmpty(capture.Safety.Detail)
                    ? "rejected-without-detail"
                    : capture.Safety.Detail);
        }
        return "interactive:captured-but-runtime-mismatch";
    }

    private NetherBattleRouteProjection BuildCombatProjection(
        NetherSnapshot snapshot,
        NetherFloorNode floor,
        bool necessaryTerminal,
        NetherAutoClimbSettings settings,
        NetherRuntimeRouteSafetyData runtime
    )
    {
        NetherFloorMasterBounds bounds = default;
        bool hasBounds = runtime.FloorBoundsByFloorId != null
            && runtime.FloorBoundsByFloorId.TryGetValue(floor.NodeId, out bounds)
            && bounds.IsKnown
            && bounds.MinimumErosionPoint.HasValue
            && bounds.MaximumErosionPoint.HasValue;
        NetherActiveCodeErosionProjection codeProjection = runtime.ActiveCodeErosion
            ?? NetherActiveCodeErosionProjectionMapper.Unknown("missing-active-code-erosion-projection");
        IReadOnlyList<NetherCodeEffect> codeEffects = codeProjection.ErosionEffects
            ?? Array.Empty<NetherCodeEffect>();
        IReadOnlyList<NetherCharacterState> activeCharacters = snapshot.Characters == null
            ? Array.Empty<NetherCharacterState>()
            : snapshot.Characters
                .Where(character => character.IsActive)
                .OrderBy(character => character.CharacterId)
                .ToArray();
        IReadOnlyList<int> activeHp = activeCharacters
            .Select(character => character.HpPermille)
            .ToArray();
        IReadOnlyList<NetherActiveLivingMemberHp>? observedRuntimeLiving =
            runtime.ActivePartyHp.LivingMembers;
        bool hasRuntimeHp = runtime.ActivePartyHp.IsKnown
            && runtime.ActivePartyHp.MinimumHpPermille.HasValue
            && observedRuntimeLiving != null;
        IReadOnlyList<NetherActiveLivingMemberHp> runtimeLiving = observedRuntimeLiving
            ?? Array.Empty<NetherActiveLivingMemberHp>();
        bool hasExactLivingRows = activeCharacters.Count == runtimeLiving.Count
            && activeCharacters.Count > 0
            && activeCharacters.Select(character => character.CharacterId).Distinct().Count()
                == activeCharacters.Count
            && runtimeLiving.Select(member => member.CharacterId).Distinct().Count()
                == runtimeLiving.Count
            && activeCharacters.Zip(
                runtimeLiving,
                static (snapshotMember, runtimeMember) =>
                    snapshotMember.CharacterId == runtimeMember.CharacterId
                    && snapshotMember.HpPermille == runtimeMember.HpPermille
            ).All(matches => matches);
        bool hasHp = hasRuntimeHp
            && hasExactLivingRows
            && activeHp.All(value => value is >= 0 and <= 1000)
            && activeHp.Min() == runtime.ActivePartyHp.MinimumHpPermille!.Value;
        bool hasCode = codeProjection.ErosionProjectionKnown
            && !string.IsNullOrEmpty(codeProjection.CodeHash)
            && codeProjection.ErosionEffects != null;
        bool hasValidCurrentErosion = snapshot.ErosionPoint is >= 0 and < HardErosionLimit;
        bool allInputsKnown = hasBounds && hasHp && hasCode && hasValidCurrentErosion;

        return _battleProjectionBuilder.Build(new NetherBattleRouteProjectionInput(
            FloorId: floor.FloorId,
            FloorKind: floor.NodeType,
            MinimumErosionPoint: hasBounds ? BattleBaseErosionIncrease : null,
            MaximumErosionPoint: hasBounds ? BattleBaseErosionIncrease : null,
            CurrentErosion: snapshot.ErosionPoint,
            ActiveHpPermille: hasHp ? activeHp : Array.Empty<int>(),
            ActiveCodeEffects: hasCode
                ? codeEffects
                : Array.Empty<NetherCodeEffect>(),
            CodeHash: hasCode ? codeProjection.CodeHash : string.Empty,
            Settings: settings,
            HardErosionLimit: HardErosionLimit
        )
        {
            HasMasterData = hasBounds,
            IsCodeHashKnown = hasCode,
            AllInputsKnown = allInputsKnown,
        });
    }

    /// <summary>
    /// Admits an interactive node only when the exact live capture has already proved all
    /// server-possible popup rows have a safe exit.  The context builder still receives a
    /// complete evaluator input so reverse terminal reachability cannot turn a missing map
    /// range, resource, or stale capture into a permissive dictionary default.
    /// </summary>
    private bool TryBuildInteractiveSafetyInput(
        NetherSnapshot snapshot,
        NetherFloorNode floor,
        NetherAutoClimbSettings settings,
        NetherRuntimeInteractivePreEntryInputsResult? interactivePreEntry,
        NetherActiveCodeErosionProjection? activeCodeErosion,
        out NetherFloorSafetyInput safetyInput,
        out NetherInteractiveWorstCaseProjection worstCaseProjection,
        out NetherRouteHpRule hpRule
    )
    {
        safetyInput = default;
        worstCaseProjection = default;
        hpRule = NetherRouteHpRule.OrdinaryAllLivingSurvive;
        if (!IsInteractive(floor.NodeType)
            || interactivePreEntry == null
            || !interactivePreEntry.IsSuccess
            || interactivePreEntry.ByFloorNodeId == null
            || !interactivePreEntry.ByFloorNodeId.TryGetValue(floor.NodeId, out NetherRuntimeInteractivePreEntryCaptureResult? capture)
            || !capture.IsCaptured
            || capture.Input == null
            || !capture.Safety.IsSafe
            || !TryGetInteractiveWorstCaseProjection(capture.Input.FloorKind, capture.Safety, out worstCaseProjection))
        {
            return false;
        }

        NetherInteractiveFloorPreEntrySafetyInput captured = capture.Input;
        if (captured.FloorMasterId != floor.FloorId
            || captured.FloorKind != floor.NodeType
            || captured.Settings == null
            || captured.Settings != settings
            || !captured.CurrentErosion.HasValue
            || captured.CurrentErosion.Value != snapshot.ErosionPoint
            || !captured.CurrentNetherGold.HasValue
            || captured.CurrentNetherGold.Value != snapshot.NetherGold
            || !captured.CurrentTreasureKeys.HasValue
            || captured.CurrentTreasureKeys.Value != snapshot.TreasureKeyCount
            || captured.ActiveHpPermille == null)
        {
            return false;
        }

        IReadOnlyList<int> expectedActiveHp = snapshot.Characters == null
            ? Array.Empty<int>()
            : snapshot.Characters
                .Where(character => character.IsActive)
                .Select(character => character.HpPermille)
                .ToArray();
        if (expectedActiveHp.Count == 0
            || !captured.ActiveHpPermille.SequenceEqual(expectedActiveHp))
        {
            return false;
        }

        NetherFloorMasterBounds bounds = _floorBoundsMapper.Map(
            captured.FloorMasterId,
            captured.MapFloorRows
        );
        if (!bounds.IsKnown
            || !bounds.MinimumErosionPoint.HasValue
            || !bounds.MaximumErosionPoint.HasValue)
        {
            return false;
        }

        hpRule = NetherRouteHpRuleMapper.Map(
            floor.NodeType,
            capture.Safety.SafeOptionProjectionByEventId?.Values,
            worstCaseProjection
        );

        IReadOnlyList<NetherErosionModifier> erosionModifiers = Array.Empty<NetherErosionModifier>();
        if (floor.NodeType is NetherFloorNodeType.Event
            or NetherFloorNodeType.Recovery
            or NetherFloorNodeType.Treasure)
        {
            if (activeCodeErosion == null
                || !activeCodeErosion.ErosionProjectionKnown
                || !NetherBattleRouteProjectionBuilder.TryMapModifiers(
                    activeCodeErosion.ErosionEffects,
                    out IReadOnlyList<NetherErosionModifier>? mappedModifiers,
                    out _
                ))
            {
                return false;
            }
            erosionModifiers = mappedModifiers!;
        }

        safetyInput = new NetherFloorSafetyInput(
            CurrentErosion: snapshot.ErosionPoint,
            // Selecting an interactive floor has no generic erosion cost. Exact event-option
            // effects are represented by KnownModifierDelta below. The map master min/max
            // values only describe when a row may be generated.
            FloorMinimumErosion: 0,
            FloorMaximumErosion: 0,
            KnownModifierDelta: worstCaseProjection.ErosionDelta,
            Kind: NetherFloorSafetyKind.Optional,
            NodeType: floor.NodeType,
            CurrentHpPermille: expectedActiveHp,
            // Ordinary deterministic Event costs are hard-eligible while every currently living
            // character remains above zero. The configurable HP floor is a later preference and
            // remains the combat-entry gate; it must not turn a surviving Event result into death.
            MinimumHpPermille: 1,
            SoftErosionLimit: settings.SoftErosionLimit,
            HardErosionLimit: HardErosionLimit,
            AllInputsKnown: true
        )
        {
            ErosionModifiers = erosionModifiers,
        };
        return true;
    }

    private static bool TryGetInteractiveWorstCaseProjection(
        NetherFloorNodeType floorKind,
        NetherInteractiveFloorPreEntrySafetyResult safety,
        out NetherInteractiveWorstCaseProjection projection
    )
    {
        projection = default;
        if (safety.WorstCaseProjection is not NetherInteractiveWorstCaseProjection captured)
            return false;

        if (floorKind is NetherFloorNodeType.Event
            or NetherFloorNodeType.Recovery
            or NetherFloorNodeType.Treasure)
        {
            if (safety.SafeOptionNumberByEventId == null
                || safety.SafeOptionProjectionByEventId == null
                || safety.SafeOptionNumberByEventId.Count == 0
                || safety.SafeOptionNumberByEventId.Count != safety.SafeOptionProjectionByEventId.Count)
            {
                return false;
            }
            foreach ((long eventId, int optionNumber) in safety.SafeOptionNumberByEventId)
            {
                if (!safety.SafeOptionProjectionByEventId.TryGetValue(eventId, out NetherInteractiveOptionProjection? option)
                    || option.OptionNumber != optionNumber
                    || option.ExpectedEffects == null
                    || option.ExpectedEffects.Count == 0
                    || option.ExpectedEffects.Any(effect => !effect.Known || !effect.ContentKnown))
                {
                    return false;
                }
            }
        }
        else if (floorKind is not NetherFloorNodeType.Shop)
        {
            return false;
        }

        projection = captured;
        return true;
    }

    private static NetherFloorSafetyInput UnknownInput(
        NetherSnapshot snapshot,
        NetherFloorNode floor,
        bool necessaryTerminal
    ) => new(
        CurrentErosion: snapshot.ErosionPoint,
        FloorMinimumErosion: 0,
        FloorMaximumErosion: 0,
        KnownModifierDelta: 0,
        Kind: necessaryTerminal ? NetherFloorSafetyKind.NecessaryTerminal : NetherFloorSafetyKind.Optional,
        NodeType: floor.NodeType,
        CurrentHpPermille: Array.Empty<int>(),
        MinimumHpPermille: 0,
        SoftErosionLimit: 90,
        HardErosionLimit: HardErosionLimit,
        AllInputsKnown: false
    )
    {
        ErosionModifiers = null,
    };

    private static NetherBattleProjectionPayload CreatePayload(
        NetherSnapshot snapshot,
        NetherFloorNode floor,
        NetherBattleRouteProjection projection,
        string codeHash
    )
    {
        NetherFloorSafetyInput input = projection.EvaluatorInput!.Value;
        return new NetherBattleProjectionPayload(
            MapId: snapshot.MapId,
            FloorId: floor.FloorId,
            PreBattleErosion: snapshot.ErosionPoint,
            FloorMinimumErosion: input.FloorMinimumErosion,
            FloorMaximumErosion: input.FloorMaximumErosion,
            ProjectedMinimumErosion: projection.ProjectedMinimumErosion!.Value,
            ProjectedMaximumErosion: projection.ProjectedMaximumErosion!.Value,
            CodeHash: codeHash,
            ProjectionIdentity: projection.ProjectionIdentity
        )
        {
            ExpectedSettlementStatus = floor.NodeType == NetherFloorNodeType.Boss
                ? NetherSessionStatus.Sleep
                : NetherSessionStatus.Play,
        };
    }

    private static bool IsCombat(NetherFloorNodeType type) => type is
        NetherFloorNodeType.Battle or NetherFloorNodeType.MiniBoss or NetherFloorNodeType.Boss;

    private static bool IsInteractive(NetherFloorNodeType type) => type is
        NetherFloorNodeType.Event or NetherFloorNodeType.Recovery or NetherFloorNodeType.Shop or NetherFloorNodeType.Treasure;

    private static HashSet<long> ResolveNecessaryTerminalFloorIds(IReadOnlyList<NetherFloorNode> floors)
    {
        var predecessorIds = new HashSet<long>();
        foreach (NetherFloorNode floor in floors)
        {
            if (floor?.PreviousFloorIds == null)
                continue;
            foreach (long previousId in floor.PreviousFloorIds)
                predecessorIds.Add(previousId);
        }
        return floors
            .Where(floor => floor != null
                && floor.NodeId > 0
                && floor.NodeType == NetherFloorNodeType.Boss
                && !predecessorIds.Contains(floor.NodeId))
            .Select(floor => floor.NodeId)
            .ToHashSet();
    }

    private static HashSet<long> ResolveCombatNodesBeforeFirstUnsettledBattle(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherFloorNode> floors
    )
    {
        long currentNodeId = snapshot.CurrentNodeId > 0
            ? snapshot.CurrentNodeId
            : snapshot.CurrentFloorId;
        if (currentNodeId <= 0 || floors.All(floor => floor?.NodeId != currentNodeId))
            return new HashSet<long>();

        var successors = floors
            .Where(floor => floor != null && floor.NodeId > 0)
            .ToDictionary(floor => floor.NodeId, _ => new List<NetherFloorNode>());
        foreach (NetherFloorNode floor in floors)
        {
            if (floor?.PreviousFloorIds == null)
                continue;
            foreach (long predecessorId in floor.PreviousFloorIds)
            {
                if (successors.TryGetValue(predecessorId, out List<NetherFloorNode>? next))
                    next.Add(floor);
            }
        }

        var exactCombatIds = new HashSet<long>();
        var visitedNonCombatIds = new HashSet<long> { currentNodeId };
        var pending = new Queue<long>();
        pending.Enqueue(currentNodeId);
        while (pending.Count > 0)
        {
            long predecessorId = pending.Dequeue();
            if (!successors.TryGetValue(predecessorId, out List<NetherFloorNode>? next))
                continue;
            foreach (NetherFloorNode floor in next)
            {
                if (IsCombat(floor.NodeType))
                {
                    exactCombatIds.Add(floor.NodeId);
                    continue;
                }
                if (visitedNonCombatIds.Add(floor.NodeId))
                    pending.Enqueue(floor.NodeId);
            }
        }
        return exactCombatIds;
    }
}
