#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Exact non-localized fields copied from one <c>MNetherFloorEvents</c> row.  The four part
/// fields are option references, not effect targets. The selected row is resolved with the same
/// ExtendId/id-or-first-floor rule as the game's NetherFloorMasterResolver.
/// </summary>
internal readonly record struct NetherFloorEventMasterRow(
    long EventId,
    long MapFloorMasterId,
    int Weight,
    long PartId1,
    long PartId2,
    long PartId3,
    long PartId4
)
{
    public bool HasRequiredFields { get; init; } = true;
    /// <summary>Raw <c>MNetherFloorEvents.type</c>, retained without guessing a localized semantic.</summary>
    public int Type { get; init; }
}

/// <summary>
/// Exact non-localized fields copied from one <c>MNetherFloorEventParts</c> row.  It deliberately
/// excludes select/effect text: locale text is not authoritative safety data.
/// </summary>
internal readonly record struct NetherFloorEventPartMasterRow(
    long PartId,
    int TargetType1,
    long SelectParameter1,
    int TargetType2,
    long SelectParameter2,
    int TargetType3,
    long SelectParameter3,
    int ContentType,
    long ContentId,
    long Amount
)
{
    public bool HasRequiredFields { get; init; } = true;
}

internal enum NetherInteractivePartialDeathObjectiveKind
{
    Unknown = 0,
    HpPaidEventKeyForRank5Treasure,
    TreasureHpPayment,
}

/// <summary>
/// Prevalidated route/objective proof supplied by the route-strategy production capture. An option
/// shape alone can never authorize a character death. Exact Event/part IDs prevent proof from being
/// reused for another popup option.
/// </summary>
internal sealed record NetherInteractivePartialDeathEligibility(
    NetherInteractivePartialDeathObjectiveKind Kind,
    NetherRankFiveTreasureIdentity Identity
)
{
    public NetherInteractivePartialDeathEligibility(
        NetherInteractivePartialDeathObjectiveKind kind,
        long EventId,
        long EventPartId,
        long ObjectiveNodeId
    ) : this(
        kind,
        new NetherRankFiveTreasureIdentity(ObjectiveNodeId, EventId, EventPartId)
    )
    {
    }

    public long EventId => Identity.ObjectiveEventId;
    public long EventPartId => Identity.ObjectiveEventPartId;
    public long ObjectiveNodeId => Identity.ObjectiveNodeId;
    public bool IsKnown { get; init; }
    public bool ObjectiveReachable { get; init; }
    public int ExactTreasureRank { get; init; }
    public bool IsOnlyTerminalReachingRoute { get; init; }
    public bool NoBetterAffordableCurrencyKeySource { get; init; }
    public string UnknownReason { get; init; } = string.Empty;

    public bool AllowsHpPaidEventKey => IsKnown
        && Kind == NetherInteractivePartialDeathObjectiveKind.HpPaidEventKeyForRank5Treasure
        && EventId > 0 && EventPartId > 0 && ObjectiveNodeId > 0
        && ObjectiveReachable && ExactTreasureRank == 5
        && NoBetterAffordableCurrencyKeySource;

    public bool AllowsTreasureHpPayment => IsKnown
        && Kind == NetherInteractivePartialDeathObjectiveKind.TreasureHpPayment
        && EventId > 0 && EventPartId > 0 && ObjectiveNodeId > 0
        && ObjectiveReachable
        && (ExactTreasureRank == 5 || IsOnlyTerminalReachingRoute);
}

/// <summary>
/// Complete authoritative input for pre-entry proof of an interactive floor.  Nullable resource
/// values mean the runtime failed to read them; they are never substituted with zero.
/// </summary>
internal sealed record NetherInteractiveFloorPreEntrySafetyInput(
    NetherFloorNodeType FloorKind,
    long FloorMasterId,
    IReadOnlyList<NetherFloorMasterBoundsRow>? MapFloorRows,
    IReadOnlyList<NetherFloorEventMasterRow>? EventRows,
    IReadOnlyList<NetherFloorEventPartMasterRow>? EventPartRows,
    int? CurrentErosion,
    IReadOnlyList<int>? ActiveHpPermille,
    int? CurrentNetherGold,
    int? CurrentTreasureKeys,
    NetherAutoClimbSettings? Settings
)
{
    /// <summary>True only after a real native shop close callback has been bound.</summary>
    public bool CanCloseShop { get; init; }
    /// <summary>
    /// Exact live <c>NetherFloorModel.ExtendId</c>.  A positive value must identify the native
    /// resolver's event row; zero means the resolver's floor-master fallback is in effect.
    /// </summary>
    public long FloorExtendId { get; init; }
    /// <summary>Stable runtime node coordinate paired with <see cref="FloorMasterId"/>.</summary>
    public long FloorNodeId { get; init; }
    /// <summary>Current authoritative portfolio used to prove that target_type=7 has a removable code.</summary>
    public IReadOnlyList<NetherCodeState> CurrentCodes { get; init; } = Array.Empty<NetherCodeState>();
    public int CodeCapacity { get; init; }
    public IReadOnlyList<NetherInteractivePartialDeathEligibility> PartialDeathEligibility { get; init; } =
        Array.Empty<NetherInteractivePartialDeathEligibility>();
    /// <summary>
    /// Exact selected-horizon Recovery continuation proofs keyed by EventPartId. Production sets
    /// <see cref="RequireCompleteRecoveryBranchSafety"/> after binding the selected route; missing
    /// proofs then pause the complete Recovery popup instead of using local scoring.
    /// </summary>
    public IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> RecoveryBranchSafetyByPartId { get; init; } =
        new Dictionary<long, NetherRecoveryBranchSafetyEvidence>();
    /// <summary>
    /// Production capture sets this after the selected route horizon has been bound. When true,
    /// the complete three-option Recovery policy is authoritative and missing proofs pause rather
    /// than falling back to local option scoring.
    /// </summary>
    public bool RequireCompleteRecoveryBranchSafety { get; init; }
    /// <summary>Exact selected-branch rank-five procurement decision bound before capture.</summary>
    public NetherRankFiveKeyProcurementDecision? RankFiveKeyProcurement { get; init; }
    /// <summary>
    /// Optional exact MItems/MNetherFloorBattles copies. Production supplies these rows; pure
    /// legacy callers may omit them only for non-exact options.
    /// </summary>
    public IReadOnlyList<NetherStrategyItemMasterRow>? ItemRows { get; init; }
    public IReadOnlyList<NetherStrategyBattleMasterRow>? BattleRows { get; init; }
    /// <summary>
    /// Optional authoritative semantic provider. The native capture currently supplies no
    /// provider because its item/battle rows expose only raw fields; absent or ambiguous evidence
    /// leaves the dependent option Unknown.
    /// </summary>
    public NetherStrategyTypedSemanticProviderEvidence? TypedSemanticProvider { get; init; }
    /// <summary>
    /// Exact route commitments keyed by native Event/part/option identity. Missing keys mean no
    /// committed procurement for that option; invalid values make only that option unknown.
    /// </summary>
    public IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> CommittedProcurementByOption { get; init; } =
        new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
}

/// <summary>
/// The safe option proof is retained under the exact native-resolved event ID so the later popup
/// dispatcher cannot mistake an option from another master row for the selected floor.
/// </summary>
internal sealed record NetherInteractiveOptionProjection(
    int OptionNumber,
    int ErosionDelta,
    int HpDelta,
    IReadOnlyList<NetherEffect> ExpectedEffects
)
{
    public long EventId { get; init; }
    public long EventPartId { get; init; }
    public long FloorId { get; init; }
    public long NodeId { get; init; }
    public bool IsKnown { get; init; } = true;
    public string UnknownReason { get; init; } = string.Empty;
    public bool HasRouteSafetyEvidence { get; init; }
    public bool RouteSafetyAllowed { get; init; } = true;
    public string RouteSafetyUnknownReason { get; init; } = string.Empty;
    public NetherEventBattleEvidence? Battle { get; init; }
    public NetherEventRewardEvidence? Reward { get; init; }
    public bool AllowsPartialActiveDeaths { get; init; }
    public NetherInteractivePartialDeathEligibility? PartialDeathEligibility { get; init; }
    public bool IsMandatoryRankFiveKeyObjective { get; init; }
    public NetherRankFiveKeyProcurementCommitment? RankFiveKeyProcurementCommitment { get; init; }
    public NetherRankFiveTreasureIdentity? RankFiveTreasureObjective { get; init; }
    public bool HasCommittedProcurementEvidence { get; init; }
    public int CommittedGoldMinimum { get; init; }
    public int CommittedKeyMinimum { get; init; }
    public bool ParticipatesInSelection { get; init; } = true;
    public bool IsSelected { get; init; }
    public NetherInteractiveOptionHardGate FirstFailingHardGate { get; init; }
    public NetherInteractiveOptionSelectionTier SelectionTier { get; init; }
    public NetherStrategyUnknownReasonCode UnknownReasonCode { get; init; }
    public string ComparisonRationale { get; init; } = string.Empty;
}

internal enum NetherInteractiveOptionHardGate
{
    None = 0,
    CandidateIdentity,
    NativeMasterData,
    NativeEffect,
    Binding,
    RecoveryBranchSafety,
    RouteSafety,
    BattleRouteSafety,
    HpSafety,
    ErosionSafety,
    Resource,
    Procurement,
}

internal enum NetherInteractiveOptionSelectionTier
{
    None = 0,
    Recovery,
    TreasureKey,
    TreasureHpPayment,
    Battle,
    Reward,
    DirectCodeOffer,
    NeutralSafeOption,
}

/// <summary>
/// Typed public audit seam for each exact Event/Recovery/Treasure option projection, including
/// options rejected while a sibling remains selectable.
/// </summary>
internal sealed record NetherInteractiveOptionAudit
{
    public NetherInteractiveEventOptionKey Key { get; init; }
    public int OptionNumber => Key.OptionNumber;
    public long FloorId { get; init; }
    public long NodeId { get; init; }
    public bool ParticipatesInSelection { get; init; } = true;
    public bool IsKnown { get; init; }
    public bool IsSelected { get; init; }
    public NetherInteractiveOptionHardGate FirstFailingHardGate { get; init; }
    public NetherInteractiveOptionSelectionTier SelectionTier { get; init; }
    public NetherStrategyUnknownReasonCode UnknownReasonCode { get; init; }
    public int ErosionDelta { get; init; }
    public int HpDelta { get; init; }
    public int CommittedGoldMinimum { get; init; }
    public int CommittedKeyMinimum { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string ComparisonRationale { get; init; } = string.Empty;
}

internal readonly record struct NetherInteractiveEventOptionKey(
    long EventId,
    long EventPartId,
    int OptionNumber
);

internal sealed record NetherInteractiveEventPartIndex(
    IReadOnlyDictionary<long, NetherFloorEventPartMasterRow> Rows,
    IReadOnlySet<long> AmbiguousIds
);

/// <summary>
/// Projection of the exact event row already represented by the server floor model. The route
/// planner consumes its erosion and HP outcome before clicking the floor.
/// </summary>
internal readonly record struct NetherInteractiveWorstCaseProjection(int ErosionDelta, int HpDelta);

internal sealed record NetherInteractiveFloorPreEntrySafetyResult
{
    public bool IsSafe { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
    public IReadOnlyDictionary<long, int> SafeOptionNumberByEventId { get; init; } =
        new Dictionary<long, int>();
    public IReadOnlyDictionary<long, NetherInteractiveOptionProjection> SafeOptionProjectionByEventId { get; init; } =
        new Dictionary<long, NetherInteractiveOptionProjection>();
    public IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection> OptionProjectionByKey { get; init; } =
        new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>();
    public IReadOnlyList<NetherInteractiveOptionAudit> OptionAudits { get; init; } =
        Array.Empty<NetherInteractiveOptionAudit>();
    public NetherInteractiveWorstCaseProjection? WorstCaseProjection { get; init; }

    public static NetherInteractiveFloorPreEntrySafetyResult Safe(
        IReadOnlyDictionary<long, int>? safeOptions = null,
        IReadOnlyDictionary<long, NetherInteractiveOptionProjection>? projections = null,
        NetherInteractiveWorstCaseProjection? worstCase = null,
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>? optionProjections = null,
        IReadOnlyList<NetherInteractiveOptionAudit>? optionAudits = null
    ) => new()
    {
        IsSafe = true,
        PauseReason = NetherPauseReason.None,
        SafeOptionNumberByEventId = safeOptions ?? new Dictionary<long, int>(),
        SafeOptionProjectionByEventId = projections ?? new Dictionary<long, NetherInteractiveOptionProjection>(),
        OptionProjectionByKey = optionProjections
            ?? new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>(),
        OptionAudits = optionAudits
            ?? CreateOptionAudits(optionProjections),
        WorstCaseProjection = worstCase,
    };

    public static NetherInteractiveFloorPreEntrySafetyResult SafeNeutral() => new()
    {
        IsSafe = true,
        PauseReason = NetherPauseReason.None,
        WorstCaseProjection = new NetherInteractiveWorstCaseProjection(ErosionDelta: 0, HpDelta: 0),
    };

    public static NetherInteractiveFloorPreEntrySafetyResult Pause(
        NetherPauseReason reason,
        string detail,
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>? optionProjections = null,
        IReadOnlyList<NetherInteractiveOptionAudit>? optionAudits = null
    ) => new()
    {
        IsSafe = false,
        PauseReason = reason,
        Detail = detail,
        OptionProjectionByKey = optionProjections
            ?? new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>(),
        OptionAudits = optionAudits ?? CreateOptionAudits(optionProjections),
    };

    private static IReadOnlyList<NetherInteractiveOptionAudit> CreateOptionAudits(
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>? projections
    ) => (projections ?? new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>())
        .OrderBy(entry => entry.Key.EventId)
        .ThenBy(entry => entry.Key.EventPartId)
        .ThenBy(entry => entry.Key.OptionNumber)
        .Select(entry => new NetherInteractiveOptionAudit
        {
            Key = entry.Key,
            FloorId = entry.Value.FloorId,
            NodeId = entry.Value.NodeId,
            ParticipatesInSelection = entry.Value.ParticipatesInSelection,
            IsKnown = entry.Value.IsKnown,
            IsSelected = entry.Value.IsSelected,
            FirstFailingHardGate = entry.Value.FirstFailingHardGate,
            SelectionTier = entry.Value.SelectionTier,
            UnknownReasonCode = entry.Value.UnknownReasonCode,
            ErosionDelta = entry.Value.ErosionDelta,
            HpDelta = entry.Value.HpDelta,
            CommittedGoldMinimum = entry.Value.CommittedGoldMinimum,
            CommittedKeyMinimum = entry.Value.CommittedKeyMinimum,
            Detail = entry.Value.UnknownReason,
            ComparisonRationale = entry.Value.ComparisonRationale,
        })
        .ToArray();
}

/// <summary>
/// Fail-closed proof that an interactive floor's native-resolved event row has a safe exit. It is
/// intentionally a pure production component: the
/// bridge can later copy exact master fields into these rows without exposing a reflection or UI
/// object to route policy.
/// </summary>
internal sealed class NetherInteractiveFloorPreEntrySafety
{
    private readonly NetherFloorMasterBoundsMapper _boundsMapper = new();
    private readonly NetherEventPolicy _eventPolicy = new();

    public NetherInteractiveFloorPreEntrySafetyResult Evaluate(NetherInteractiveFloorPreEntrySafetyInput? input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (!TryCreateSnapshot(input, out NetherSnapshot? snapshot, out NetherInteractiveFloorPreEntrySafetyResult? invalid))
            return invalid!;
        if (!TryValidateFloorMaster(input, out NetherInteractiveFloorPreEntrySafetyResult? boundsFailure))
            return boundsFailure!;

        return input.FloorKind switch
        {
            NetherFloorNodeType.Event => EvaluatePossibleEventRows(input, snapshot!),
            NetherFloorNodeType.Recovery => EvaluatePossibleEventRows(input, snapshot!),
            NetherFloorNodeType.Shop => EvaluateShopOff(input),
            NetherFloorNodeType.Treasure => EvaluatePossibleEventRows(input, snapshot!),
            _ => NetherInteractiveFloorPreEntrySafetyResult.Pause(
                NetherPauseReason.UnknownFloor,
                "unsupported-interactive-floor-kind:" + ((int)input.FloorKind).ToString(CultureInfo.InvariantCulture)
            ),
        };
    }

    private static bool TryCreateSnapshot(
        NetherInteractiveFloorPreEntrySafetyInput input,
        out NetherSnapshot? snapshot,
        out NetherInteractiveFloorPreEntrySafetyResult? failure
    )
    {
        snapshot = null;
        failure = null;
        if (input.Settings == null)
        {
            failure = Unknown("missing-interactive-safety-settings");
            return false;
        }
        if (!input.CurrentErosion.HasValue
            || !input.CurrentNetherGold.HasValue
            || !input.CurrentTreasureKeys.HasValue
            || input.ActiveHpPermille == null
            || input.PartialDeathEligibility == null)
        {
            failure = Unknown("missing-interactive-authoritative-resource");
            return false;
        }
        if (input.CurrentErosion.Value < 0
            || input.CurrentNetherGold.Value < 0
            || input.CurrentTreasureKeys.Value < 0
            || input.ActiveHpPermille.Count == 0)
        {
            failure = Unknown("invalid-interactive-authoritative-resource");
            return false;
        }

        var characters = new List<NetherCharacterState>(input.ActiveHpPermille.Count);
        for (int index = 0; index < input.ActiveHpPermille.Count; index++)
        {
            int hp = input.ActiveHpPermille[index];
            if (hp is < 0 or > 1000)
            {
                failure = Unknown("invalid-interactive-active-hp");
                return false;
            }
            characters.Add(new NetherCharacterState(index + 1L, hp));
        }

        snapshot = new NetherSnapshot
        {
            ErosionPoint = input.CurrentErosion.Value,
            NetherGold = input.CurrentNetherGold.Value,
            TreasureKeyCount = input.CurrentTreasureKeys.Value,
            Characters = characters,
            Codes = input.CurrentCodes ?? Array.Empty<NetherCodeState>(),
            CodeCapacity = input.CodeCapacity,
        };
        return true;
    }

    private bool TryValidateFloorMaster(
        NetherInteractiveFloorPreEntrySafetyInput input,
        out NetherInteractiveFloorPreEntrySafetyResult? failure
    )
    {
        failure = null;
        NetherFloorMasterBounds bounds = _boundsMapper.Map(input.FloorMasterId, input.MapFloorRows);
        if (!bounds.IsKnown || !bounds.MinimumErosionPoint.HasValue || !bounds.MaximumErosionPoint.HasValue)
        {
            failure = Unknown("interactive-floor-bounds:" + bounds.Detail);
            return false;
        }

        // min/max_erosion_point belongs to MNetherMapFloors row generation eligibility.
        // The server has already materialized this exact floor; treating the range as an
        // action delta double-counts up to 100 erosion. Exact event effects are evaluated
        // below, while neutral Shop/Treasure exits remain zero-cost.
        return true;
    }

    private NetherInteractiveFloorPreEntrySafetyResult EvaluatePossibleEventRows(
        NetherInteractiveFloorPreEntrySafetyInput input,
        NetherSnapshot snapshot
    )
    {
        if (!TryIndexEventMasters(
                input.EventRows,
                input.FloorMasterId,
                input.FloorExtendId,
                out IReadOnlyList<NetherFloorEventMasterRow>? resolvedRows,
                out string eventError
            ))
        {
            return Unknown(eventError);
        }
        if (!TryIndexEventParts(
                input.EventPartRows,
                out NetherInteractiveEventPartIndex? parts,
                out string partError
            ))
            return Unknown(partError);

        var safeOptions = new Dictionary<long, int>();
        var projections = new Dictionary<long, NetherInteractiveOptionProjection>();
        var optionProjections = new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>();
        int worstErosion = int.MinValue;
        int worstHp = int.MaxValue;
        foreach (NetherFloorEventMasterRow row in resolvedRows!)
        {
            if (!TryBuildOptions(
                    row,
                    parts!,
                    input.ItemRows,
                    input.BattleRows,
                    NetherStrategySemanticTierLookup.Create(input.TypedSemanticProvider),
                    input.PartialDeathEligibility,
                    input.RecoveryBranchSafetyByPartId,
                    input.FloorNodeId,
                    input.RankFiveKeyProcurement,
                    out IReadOnlyList<NetherEventOption>? options,
                    out string optionError
                ))
                return Unknown("event-row-" + row.EventId.ToString(CultureInfo.InvariantCulture) + ":" + optionError);
            IReadOnlyList<NetherEventOption> optionsWithBudget = ApplyProcurementBudgets(
                options!,
                input.CommittedProcurementByOption
            );
            if (!TrySelectSafeOption(
                    snapshot,
                    optionsWithBudget,
                    input.Settings!,
                    input.FloorKind,
                    input.FloorMasterId,
                    input.FloorNodeId > 0 ? input.FloorNodeId : input.FloorMasterId,
                    input.RequireCompleteRecoveryBranchSafety,
                    out int optionNumber,
                    out NetherInteractiveOptionProjection projection,
                    out IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>? rowProjections,
                    out NetherPauseReason rejection,
                    out string rejectionDetail
                ))
            {
                foreach (var optionProjection in rowProjections!
                    ?? new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>())
                {
                    optionProjections[optionProjection.Key] = optionProjection.Value;
                }
                return NetherInteractiveFloorPreEntrySafetyResult.Pause(
                    rejection,
                    "event-row-" + row.EventId.ToString(CultureInfo.InvariantCulture) + ":" + rejectionDetail,
                    optionProjections
                );
            }
            safeOptions.Add(row.EventId, optionNumber);
            projections.Add(row.EventId, projection);
            foreach (var optionProjection in rowProjections!)
                optionProjections[optionProjection.Key] = optionProjection.Value;
            worstErosion = Math.Max(worstErosion, projection.ErosionDelta);
            worstHp = Math.Min(worstHp, projection.HpDelta);
        }

        if (projections.Count != safeOptions.Count || worstErosion == int.MinValue || worstHp == int.MaxValue)
            return Unknown("missing-event-option-projection");

        return NetherInteractiveFloorPreEntrySafetyResult.Safe(
            safeOptions,
            projections,
            new NetherInteractiveWorstCaseProjection(worstErosion, worstHp),
            optionProjections
        );
    }

    private static bool TryIndexEventMasters(
        IReadOnlyList<NetherFloorEventMasterRow>? rows,
        long floorMasterId,
        long floorExtendId,
        out IReadOnlyList<NetherFloorEventMasterRow>? possibleRows,
        out string error
    )
    {
        possibleRows = null;
        if (floorMasterId <= 0 || floorExtendId < 0)
        {
            error = "invalid-interactive-floor-master-id";
            return false;
        }
        if (rows == null)
        {
            error = "missing-m-nether-floor-events";
            return false;
        }

        NetherFloorEventMasterRow? resolved = null;
        foreach (NetherFloorEventMasterRow row in rows)
        {
            bool matches = floorExtendId > 0
                ? row.EventId == floorExtendId
                : row.MapFloorMasterId == floorMasterId;
            if (matches)
            {
                resolved = row;
                break;
            }
        }
        if (!resolved.HasValue)
        {
            error = floorExtendId > 0
                ? "missing-extend-m-nether-floor-event:" + floorExtendId.ToString(CultureInfo.InvariantCulture)
                : "missing-floor-m-nether-floor-event:" + floorMasterId.ToString(CultureInfo.InvariantCulture);
            return false;
        }
        NetherFloorEventMasterRow selected = resolved.Value;
        if (!selected.HasRequiredFields
            || selected.EventId <= 0
            || selected.MapFloorMasterId <= 0
            || selected.Weight < 0
            || selected.Type < 0)
        {
            error = "invalid-resolved-m-nether-floor-event:" + selected.EventId.ToString(CultureInfo.InvariantCulture);
            return false;
        }

        // Native NetherFloorMasterResolver uses First(id == ExtendId) when ExtendId is
        // positive, otherwise First(m_nether_map_floor_id == floorMasterId). Weight and the
        // selected row's map-floor field are generation metadata, not extra resolver gates.
        possibleRows = new[] { selected };
        error = string.Empty;
        return true;
    }

    private static bool TryIndexEventParts(
        IReadOnlyList<NetherFloorEventPartMasterRow>? rows,
        out NetherInteractiveEventPartIndex? indexed,
        out string error
    )
    {
        indexed = null;
        if (rows == null)
        {
            error = "missing-m-nether-floor-event-parts";
            return false;
        }

        var parts = new Dictionary<long, NetherFloorEventPartMasterRow>();
        var ambiguous = new HashSet<long>();
        foreach (NetherFloorEventPartMasterRow row in rows)
        {
            if (!row.HasRequiredFields)
            {
                // A malformed row has no dependable identity. It cannot invalidate a sibling
                // option whose exact part ID still resolves.
                if (row.PartId > 0)
                {
                    parts.Remove(row.PartId);
                    ambiguous.Add(row.PartId);
                }
                continue;
            }
            if (row.PartId <= 0)
            {
                continue;
            }
            if (ambiguous.Contains(row.PartId))
                continue;
            if (!parts.TryAdd(row.PartId, row))
            {
                error = "duplicate-m-nether-floor-event-part:" + row.PartId.ToString(CultureInfo.InvariantCulture);
                parts.Remove(row.PartId);
                ambiguous.Add(row.PartId);
            }
        }

        indexed = new NetherInteractiveEventPartIndex(parts, ambiguous);
        error = string.Empty;
        return true;
    }

    private static bool TryBuildOptions(
        NetherFloorEventMasterRow row,
        NetherInteractiveEventPartIndex parts,
        IReadOnlyList<NetherStrategyItemMasterRow>? itemRows,
        IReadOnlyList<NetherStrategyBattleMasterRow>? battleRows,
        NetherStrategySemanticTierLookup semanticTiers,
        IReadOnlyList<NetherInteractivePartialDeathEligibility> partialDeathEligibility,
        IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> recoveryBranchSafetyByPartId,
        long floorNodeId,
        NetherRankFiveKeyProcurementDecision? rankFiveKeyProcurement,
        out IReadOnlyList<NetherEventOption>? options,
        out string error
    )
    {
        options = null;
        long[] ids = [row.PartId1, row.PartId2, row.PartId3, row.PartId4];
        bool foundEmptyPart = false;
        var seen = new HashSet<long>();
        var mapped = new List<NetherEventOption>();
        HashSet<long> ambiguousItemIds = new();
        Dictionary<long, NetherStrategyItemMasterRow>? itemById = null;
        if (itemRows != null)
            itemById = BuildOptionRows(itemRows, value => value.Id, out ambiguousItemIds);
        HashSet<long> ambiguousBattleIds = new();
        Dictionary<long, NetherStrategyBattleMasterRow>? battleById = null;
        if (battleRows != null)
            battleById = BuildOptionRows(battleRows, value => value.Id, out ambiguousBattleIds);
        for (int index = 0; index < ids.Length; index++)
        {
            long id = ids[index];
            if (id < 0)
            {
                mapped.Add(UnknownOption(row.EventId, id, index + 1, "invalid-event-part-reference"));
                continue;
            }
            if (id == 0)
            {
                foundEmptyPart = true;
                continue;
            }
            if (foundEmptyPart)
            {
                mapped.Add(UnknownOption(row.EventId, id, index + 1, "noncontiguous-event-part-reference"));
                continue;
            }
            if (!seen.Add(id))
            {
                mapped.Add(UnknownOption(
                    row.EventId,
                    id,
                    index + 1,
                    "duplicate-event-part-reference:" + id.ToString(CultureInfo.InvariantCulture)
                ));
                continue;
            }
            if (parts.AmbiguousIds.Contains(id))
            {
                mapped.Add(UnknownOption(
                    row.EventId,
                    id,
                    index + 1,
                    "duplicate-m-nether-floor-event-part:" + id.ToString(CultureInfo.InvariantCulture)
                ));
                continue;
            }
            if (!parts.Rows.TryGetValue(id, out NetherFloorEventPartMasterRow part))
            {
                mapped.Add(UnknownOption(
                    row.EventId,
                    id,
                    index + 1,
                    "missing-m-nether-floor-event-part:" + id.ToString(CultureInfo.InvariantCulture)
                ));
                continue;
            }
            if (!TryMapPart(
                    part,
                    itemById,
                    ambiguousItemIds,
                    battleById,
                    ambiguousBattleIds,
                    semanticTiers,
                    out IReadOnlyList<NetherEffect>? effects,
                    out string partError
                ))
            {
                mapped.Add(UnknownOption(
                    row.EventId,
                    id,
                    index + 1,
                    "event-part-" + id.ToString(CultureInfo.InvariantCulture) + ":" + partError
                ));
                continue;
            }
            NetherInteractivePartialDeathEligibility[] matchingEligibility = partialDeathEligibility
                .Where(proof => proof != null
                    && proof.EventId == row.EventId
                    && proof.EventPartId == part.PartId)
                .ToArray();
            if (matchingEligibility.Length > 1)
            {
                error = "ambiguous-partial-death-eligibility:" + part.PartId;
                return false;
            }
            NetherInteractivePartialDeathEligibility? eligibility = matchingEligibility.FirstOrDefault();
            NetherRankFiveKeyProcurementCommitment? rankFiveCommitment =
                ResolveEventRankFiveCommitment(
                    rankFiveKeyProcurement,
                    floorNodeId,
                    row.EventId,
                    part.PartId,
                    index + 1
                );
            NetherRankFiveTreasureIdentity? rankFiveObjective =
                ResolveRankFiveObjective(
                    rankFiveKeyProcurement,
                    floorNodeId,
                    row.EventId,
                    part.PartId
                );
            NetherInteractivePartialDeathEligibility? rankFivePartialProof =
                ResolveRankFivePartialDeathProof(
                    rankFiveKeyProcurement,
                    rankFiveCommitment,
                    floorNodeId,
                    row.EventId,
                    part.PartId,
                    effects!
                );
            eligibility ??= rankFivePartialProof;
            mapped.Add(new NetherEventOption(index + 1, effects!)
            {
                EventId = row.EventId,
                EventPartId = part.PartId,
                PartialDeathEligibility = eligibility,
                RecoveryBranchSafety = recoveryBranchSafetyByPartId.TryGetValue(
                    part.PartId,
                    out NetherRecoveryBranchSafetyEvidence? branchSafety
                ) ? branchSafety : null,
                IsMandatoryRankFiveKeyObjective = rankFiveCommitment != null
                    || rankFiveObjective.HasValue
                    || eligibility?.AllowsHpPaidEventKey == true
                        && eligibility.ExactTreasureRank == 5,
                RankFiveKeyProcurementCommitment = rankFiveCommitment,
                RankFiveTreasureObjective = rankFiveObjective,
            });
        }
        if (mapped.Count == 0)
        {
            error = "empty-event-part-references";
            return false;
        }

        options = mapped;
        error = string.Empty;
        return true;
    }

    private static NetherRankFiveKeyProcurementCommitment? ResolveEventRankFiveCommitment(
        NetherRankFiveKeyProcurementDecision? decision,
        long floorNodeId,
        long eventId,
        long eventPartId,
        int optionNumber
    )
    {
        NetherRankFiveKeyProcurementCommitment? commitment = decision?.Commitment;
        if (commitment == null
            || commitment.SourceKind == NetherKeyProcurementSourceKind.ShopGold200
            || commitment.SourceNodeId != floorNodeId
            || commitment.SourceEventId != eventId
            || commitment.SourceEventPartId != eventPartId
            || commitment.SourceOptionNumber != optionNumber)
            return null;
        return commitment;
    }

    private static NetherRankFiveTreasureIdentity? ResolveRankFiveObjective(
        NetherRankFiveKeyProcurementDecision? decision,
        long floorNodeId,
        long eventId,
        long eventPartId
    )
    {
        if (decision?.HasMandatoryObjective != true
            || !decision.Objective.IsValid
            || decision.Objective.ObjectiveNodeId != floorNodeId
            || decision.Objective.ObjectiveEventId != eventId
            || decision.Objective.ObjectiveEventPartId != eventPartId)
            return null;
        return decision.Objective;
    }

    private static NetherInteractivePartialDeathEligibility? ResolveRankFivePartialDeathProof(
        NetherRankFiveKeyProcurementDecision? decision,
        NetherRankFiveKeyProcurementCommitment? commitment,
        long floorNodeId,
        long eventId,
        long eventPartId,
        IReadOnlyList<NetherEffect> effects
    )
    {
        if (decision?.HasMandatoryObjective != true || !decision.Objective.IsValid)
            return null;

        if (commitment?.SourceKind == NetherKeyProcurementSourceKind.HpPaidEventKey
            && commitment.SourceNodeId == floorNodeId
            && commitment.SourceEventId == eventId
            && commitment.SourceEventPartId == eventPartId
            && IsExactHpPaidEventKey(effects))
        {
            return new NetherInteractivePartialDeathEligibility(
                NetherInteractivePartialDeathObjectiveKind.HpPaidEventKeyForRank5Treasure,
                eventId,
                eventPartId,
                floorNodeId
            )
            {
                IsKnown = true,
                ObjectiveReachable = true,
                ExactTreasureRank = 5,
                NoBetterAffordableCurrencyKeySource = true,
            };
        }

        if (decision.SourceKind == NetherKeyProcurementSourceKind.None
            && decision.AllowsHpFallback
            && decision.Objective.ObjectiveNodeId == floorNodeId
            && decision.Objective.ObjectiveEventId == eventId
            && decision.Objective.ObjectiveEventPartId == eventPartId
            && IsExactTreasureHpPayment(effects))
        {
            return new NetherInteractivePartialDeathEligibility(
                NetherInteractivePartialDeathObjectiveKind.TreasureHpPayment,
                eventId,
                eventPartId,
                floorNodeId
            )
            {
                IsKnown = true,
                ObjectiveReachable = true,
                ExactTreasureRank = 5,
            };
        }
        return null;
    }

    private static bool IsExactHpPaidEventKey(IReadOnlyList<NetherEffect> effects) =>
        effects != null
        && effects.Count == 2
        && effects.Count(effect => effect.Kind == NetherEffectKind.Damage && effect.Amount == 80) == 1
        && effects.Count(effect => effect.Kind == NetherEffectKind.TreasureKeyGain && effect.Amount == 1) == 1;

    private static bool IsExactTreasureHpPayment(IReadOnlyList<NetherEffect> effects) =>
        effects != null
        && effects.Count == 1
        && effects[0].Kind == NetherEffectKind.Damage
        && effects[0].Amount is 40 or 80;

    private static NetherEventOption UnknownOption(
        long eventId,
        long partId,
        int optionNumber,
        string reason
    ) => new(optionNumber, [new NetherEffect(NetherEffectKind.Unknown, 0)
    {
        Known = false,
        ContentKnown = false,
    }])
    {
        EventId = eventId,
        EventPartId = Math.Max(0, partId),
        UnknownReason = reason,
    };

    private static Dictionary<long, T> BuildOptionRows<T>(
        IEnumerable<T> rows,
        Func<T, long> keySelector,
        out HashSet<long> ambiguous
    )
    {
        var mapped = new Dictionary<long, T>();
        ambiguous = new HashSet<long>();
        foreach (T row in rows)
        {
            long key = keySelector(row);
            if (key <= 0 || ambiguous.Contains(key))
                continue;
            if (!mapped.TryAdd(key, row))
            {
                mapped.Remove(key);
                ambiguous.Add(key);
            }
        }
        return mapped;
    }

    private static bool TryMapPart(
        NetherFloorEventPartMasterRow part,
        IReadOnlyDictionary<long, NetherStrategyItemMasterRow>? itemById,
        IReadOnlySet<long> ambiguousItemIds,
        IReadOnlyDictionary<long, NetherStrategyBattleMasterRow>? battleById,
        IReadOnlySet<long> ambiguousBattleIds,
        NetherStrategySemanticTierLookup semanticTiers,
        out IReadOnlyList<NetherEffect>? effects,
        out string error
    )
    {
        effects = null;
        var mapped = new List<NetherEffect>();
        if (!TryMapTarget(
                part.TargetType1,
                part.SelectParameter1,
                battleById,
                ambiguousBattleIds,
                semanticTiers,
                mapped,
                out error
            )
            || !TryMapTarget(
                part.TargetType2,
                part.SelectParameter2,
                battleById,
                ambiguousBattleIds,
                semanticTiers,
                mapped,
                out error
            )
            || !TryMapTarget(
                part.TargetType3,
                part.SelectParameter3,
                battleById,
                ambiguousBattleIds,
                semanticTiers,
                mapped,
                out error
            ))
        {
            return false;
        }

        if (part.ContentType != 0)
        {
            if (part.Amount is < 0 or > int.MaxValue)
            {
                error = "invalid-event-content";
                return false;
            }
            NetherEffect? content = part.ContentType switch
            {
                30 or 31 when part.ContentId > 0
                    && TryResolveItemEvidence(
                        part,
                        itemById,
                        ambiguousItemIds,
                        semanticTiers,
                        out NetherEventRewardEvidence? rewardEvidence
                    ) => new NetherEffect(NetherEffectKind.Item, checked((int)part.Amount))
                {
                    ContentId = part.ContentId,
                    RewardEvidence = rewardEvidence,
                },
                160 when NetherEventNativeMapping.IsCodeOfferContentId(part.ContentId) => new NetherEffect(NetherEffectKind.AbyssCodeOffer, checked((int)part.Amount)),
                165 when NetherEventNativeMapping.IsValidResourceContentId(part.ContentId) => new NetherEffect(NetherEffectKind.NetherGoldGain, checked((int)part.Amount))
                {
                    ContentId = part.ContentId,
                },
                166 when NetherEventNativeMapping.IsValidResourceContentId(part.ContentId) => new NetherEffect(NetherEffectKind.TreasureKeyGain, checked((int)part.Amount))
                {
                    ContentId = part.ContentId,
                },
                _ => null,
            };
            if (content == null)
            {
                error = part.ContentType is 30 or 31
                    ? "event-item-master-row-unavailable:" + part.ContentId.ToString(CultureInfo.InvariantCulture)
                    : "unsupported-event-content-type:" + part.ContentType.ToString(CultureInfo.InvariantCulture);
                return false;
            }
            mapped.Add(content);
        }

        if (mapped.Count is < 1 or > 4)
        {
            error = "invalid-event-effect-count:" + mapped.Count.ToString(CultureInfo.InvariantCulture);
            return false;
        }
        effects = mapped;
        error = string.Empty;
        return true;
    }

    private static bool TryMapTarget(
        int rawType,
        long parameter,
        IReadOnlyDictionary<long, NetherStrategyBattleMasterRow>? battleById,
        IReadOnlySet<long> ambiguousBattleIds,
        NetherStrategySemanticTierLookup semanticTiers,
        ICollection<NetherEffect> effects,
        out string error
    )
    {
        if (!NetherEventNativeMapping.TryMapTargetType(
                rawType,
                parameter,
                out NetherEffectKind kind,
                out int mappedAmount,
                out error
            ))
        {
            return false;
        }
        if (rawType == 0)
            return true;

        if (kind == NetherEffectKind.AbyssCodeTransform)
        {
            effects.Add(new NetherEffect(kind, mappedAmount));
            return true;
        }
        if (kind == NetherEffectKind.Battle)
        {
            if (parameter == 0)
            {
                effects.Add(new NetherEffect(kind, 0) { IsOptionalBattle = true });
                return true;
            }
            if (battleById == null
                || ambiguousBattleIds.Contains(parameter)
                || !battleById.TryGetValue(parameter, out NetherStrategyBattleMasterRow battle)
                || !battle.HasRequiredFields
                || battle.Id <= 0
                || battle.BattleStageId <= 0
                || battle.CodeDropRatio < 0)
            {
                error = "event-battle-master-row-unavailable:" + parameter.ToString(CultureInfo.InvariantCulture);
                return false;
            }
            NetherEventBattleEvidence battleEvidence;
            if (semanticTiers.TryGetEventBattleTier(
                    battle.Id,
                    out NetherEventBattleTier semanticTier
                ))
            {
                battleEvidence = new NetherEventBattleEvidence(
                    battle.Id,
                    battle.BattleStageId,
                    battle.BattleType,
                    battle.CodeDropRatio,
                    semanticTier
                );
            }
            else
            {
                battleEvidence = NetherEventBattleEvidence.Unknown(
                    battle.Id,
                    "event-battle-semantic-tier-unavailable-for-raw-type:" + battle.BattleType
                ) with
                {
                    BattleStageId = battle.BattleStageId,
                    BattleType = battle.BattleType,
                    CodeDropRatio = battle.CodeDropRatio,
                };
            }
            effects.Add(new NetherEffect(kind, mappedAmount)
            {
                IsOptionalBattle = true,
                BattleEvidence = battleEvidence,
            });
            return true;
        }

        effects.Add(new NetherEffect(kind, mappedAmount));
        return true;
    }

    private static bool TryResolveItemEvidence(
        NetherFloorEventPartMasterRow part,
        IReadOnlyDictionary<long, NetherStrategyItemMasterRow>? itemById,
        IReadOnlySet<long> ambiguousItemIds,
        NetherStrategySemanticTierLookup semanticTiers,
        out NetherEventRewardEvidence? rewardEvidence
    )
    {
        rewardEvidence = null;
        if (itemById == null)
            return false;
        if (ambiguousItemIds.Contains(part.ContentId)
            || !itemById.TryGetValue(part.ContentId, out NetherStrategyItemMasterRow item)
            || !item.HasRequiredFields
            || item.Id <= 0
            || !semanticTiers.TryGetCanonicalRewardEvidence(
                item.Id,
                out NetherCanonicalRewardTier _,
                out int itemType,
                out NetherRewardRarity rarity
            )
            || part.Amount is < 0 or > int.MaxValue)
        {
            return false;
        }
        rewardEvidence = new NetherEventRewardEvidence(
            part.ContentId,
            item.Id,
            itemType,
            rarity,
            checked((int)part.Amount)
        );
        return true;
    }

    private bool TrySelectSafeOption(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        NetherFloorNodeType floorKind,
        long floorId,
        long nodeId,
        bool requireCompleteRecoveryBranchSafety,
        out int selectedOptionNumber,
        out NetherInteractiveOptionProjection selectedProjection,
        out IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>? allProjections,
        out NetherPauseReason rejection,
        out string detail
    )
    {
        selectedOptionNumber = 0;
        selectedProjection = default!;
        var optionProjections = new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>();
        allProjections = optionProjections;
        rejection = NetherPauseReason.NoSafeRoute;
        detail = "no-safe-event-option";
        var safeOptions = new List<NetherEventOption>();

        if (floorKind == NetherFloorNodeType.Recovery && requireCompleteRecoveryBranchSafety)
        {
            NetherCodeTransformHardExclusionEvidence transformHardExclusions =
                ResolveRecoveryTransformHardExclusions(options);
            NetherEventDecision completeBranchDecision = _eventPolicy.DecideRecovery(
                snapshot,
                options,
                settings,
                Array.Empty<NetherErosionModifier>(),
                transformHardExclusions,
                requireCompleteBranchEvidence: true
            );
            if (completeBranchDecision.Kind != NetherEventDecisionKind.Select
                || completeBranchDecision.StartsBattleAfterSelection)
            {
                rejection = completeBranchDecision.Kind == NetherEventDecisionKind.Pause
                    ? completeBranchDecision.PauseReason
                    : NetherPauseReason.NoSafeRoute;
                detail = string.IsNullOrWhiteSpace(completeBranchDecision.Detail)
                    ? "recovery-complete-visible-branch-unavailable"
                    : completeBranchDecision.Detail;
                foreach (NetherEventOption option in options)
                {
                    optionProjections[OptionKey(option)] = UnknownProjection(
                        option,
                        floorId,
                        nodeId,
                        detail,
                        NetherInteractiveOptionHardGate.RecoveryBranchSafety,
                        NetherStrategyUnknownReasonCodes.FromDetail(detail)
                    );
                }
                return false;
            }

            NetherEventOption? selectedOption = options.FirstOrDefault(option =>
                option.EventId == completeBranchDecision.EventId
                && option.EventPartId == completeBranchDecision.EventPartId
                && option.OptionNumber == completeBranchDecision.OptionNumber);
            if (selectedOption == null)
            {
                rejection = NetherPauseReason.UnknownMasterData;
                detail = "recovery-selected-branch-option-identity-unavailable";
                foreach (NetherEventOption option in options)
                {
                    optionProjections[OptionKey(option)] = UnknownProjection(
                        option,
                        floorId,
                        nodeId,
                        detail,
                        NetherInteractiveOptionHardGate.RecoveryBranchSafety,
                        NetherStrategyUnknownReasonCode.AmbiguousCandidateIdentity
                    );
                }
                return false;
            }

            NetherRecoveryBranchSafetyEvidence? selectedProof = selectedOption.RecoveryBranchSafety;
            if (selectedProof == null || !selectedProof.IsAuthoritative)
            {
                rejection = NetherPauseReason.UnknownMasterData;
                detail = selectedProof?.UnknownReason.Length > 0
                    ? selectedProof.UnknownReason
                    : "recovery-selected-branch-proof-unavailable";
                foreach (NetherEventOption option in options)
                {
                    optionProjections[OptionKey(option)] = UnknownProjection(
                        option,
                        floorId,
                        nodeId,
                        detail,
                        NetherInteractiveOptionHardGate.RecoveryBranchSafety,
                        NetherStrategyUnknownReasonCodes.FromDetail(detail)
                    );
                }
                return false;
            }
            if (!selectedProof.IsNextVisibleBranchSafe)
            {
                rejection = NetherPauseReason.NoSafeRoute;
                detail = string.IsNullOrWhiteSpace(selectedProof.UnknownReason)
                    ? "recovery-selected-branch-unsafe-through-visible-suffix"
                    : selectedProof.UnknownReason;
                foreach (NetherEventOption option in options)
                {
                    optionProjections[OptionKey(option)] = UnknownProjection(
                        option,
                        floorId,
                        nodeId,
                        detail,
                        NetherInteractiveOptionHardGate.RecoveryBranchSafety,
                        NetherStrategyUnknownReasonCode.None
                    );
                }
                return false;
            }

            NetherInteractiveOptionProjection projection = CreateProjection(
                completeBranchDecision,
                floorId,
                nodeId,
                isKnown: true,
                hasRouteSafetyEvidence: true,
                routeSafetyAllowed: selectedProof.IsNextVisibleBranchSafe,
                routeSafetyUnknownReason: selectedProof.UnknownReason,
                selectionTier: NetherInteractiveOptionSelectionTier.Recovery,
                isSelected: true,
                comparisonRationale: "selected-by-complete-branch-proof"
            );
            optionProjections[OptionKey(selectedOption)] = projection;
            foreach (NetherEventOption option in options)
            {
                if (ReferenceEquals(option, selectedOption))
                    continue;
                optionProjections[OptionKey(option)] = UnknownProjection(
                    option,
                    floorId,
                    nodeId,
                    "recovery-option-not-selected-by-complete-branch-proof",
                    NetherInteractiveOptionHardGate.RecoveryBranchSafety,
                    NetherStrategyUnknownReasonCode.None,
                    "excluded:first-failing-gate=RecoveryBranchSafety"
                );
            }
            selectedOptionNumber = completeBranchDecision.OptionNumber;
            selectedProjection = projection;
            return true;
        }

        foreach (NetherEventOption option in options)
        {
            NetherEventDecision decision = DecideInteractiveOption(
                snapshot,
                [option],
                settings,
                floorKind,
                requireCompleteRecoveryBranchSafety: false
            );
            if (decision.Kind != NetherEventDecisionKind.Select)
            {
                optionProjections[OptionKey(option)] = UnknownProjection(
                    option,
                    floorId,
                    nodeId,
                    decision.Detail.Length == 0 ? "event-option-rejected" : decision.Detail,
                    isKnown: IsKnownDecision(decision)
                );
                CaptureMoreSpecificRejection(decision, ref rejection, ref detail);
                continue;
            }
            if (decision.StartsBattleAfterSelection)
            {
                // A floor selected as interactive cannot prove a later battle's route/lease
                // safety.  It is not an exit unless a non-battle option from this same row exists.
                optionProjections[OptionKey(option)] = CreateProjection(
                    decision,
                    floorId,
                    nodeId,
                    isKnown: true,
                    hasRouteSafetyEvidence: true,
                    routeSafetyAllowed: false,
                    routeSafetyUnknownReason: "event-battle-route-requires-post-selection-proof",
                    firstFailingHardGate: NetherInteractiveOptionHardGate.BattleRouteSafety,
                    unknownReasonCode: NetherStrategyUnknownReasonCode.NativeBattleRouteSafetyUnknown,
                    comparisonRationale: "excluded:first-failing-gate=BattleRouteSafety"
                ) with
                {
                    IsSelected = false,
                };
                continue;
            }
            NetherRouteHpRule hpRule = MapOptionHpRule(floorKind, decision);
            if (!HasSafeHpFloor(
                    snapshot,
                    decision.HpDelta,
                    settings.MinimumCharacterHpPermille,
                    hpRule
                ))
            {
                rejection = NetherPauseReason.UnsafeHp;
                detail = "event-option-hp-below-minimum";
                optionProjections[OptionKey(option)] = CreateProjection(
                    decision,
                    floorId,
                    nodeId,
                    isKnown: true,
                    hasRouteSafetyEvidence: true,
                    routeSafetyAllowed: false,
                    routeSafetyUnknownReason: detail,
                    firstFailingHardGate: NetherInteractiveOptionHardGate.HpSafety,
                    comparisonRationale: "excluded:first-failing-gate=HpSafety"
                ) with
                {
                    IsSelected = false,
                };
                continue;
            }
            optionProjections[OptionKey(option)] = CreateProjection(
                decision,
                floorId,
                nodeId,
                isKnown: true,
                hasRouteSafetyEvidence: true,
                routeSafetyAllowed: true,
                routeSafetyUnknownReason: string.Empty
            ) with
            {
                IsSelected = false,
                ComparisonRationale = "eligible-for-comparison",
            };
            safeOptions.Add(option);
        }

        if (safeOptions.Count == 0)
            return false;

        NetherEventDecision selected = DecideInteractiveOption(
            snapshot,
            safeOptions,
            settings,
            floorKind,
            requireCompleteRecoveryBranchSafety: false
        );
        if (selected.Kind != NetherEventDecisionKind.Select || selected.StartsBattleAfterSelection)
        {
            rejection = selected.Kind == NetherEventDecisionKind.Pause ? selected.PauseReason : NetherPauseReason.NoSafeRoute;
            detail = selected.Detail.Length == 0 ? "safe-option-selection-unavailable" : selected.Detail;
            return false;
        }
        selectedOptionNumber = selected.OptionNumber;
        NetherInteractiveEventOptionKey selectedKey = new(
            selected.EventId,
            selected.EventPartId,
            selected.OptionNumber
        );
        if (!optionProjections.TryGetValue(selectedKey, out selectedProjection!))
        {
            rejection = NetherPauseReason.UnknownEffect;
            detail = "event-option-projection-overflow";
            return false;
        }
        foreach (NetherInteractiveEventOptionKey key in optionProjections.Keys.ToArray())
        {
            NetherInteractiveOptionProjection projection = optionProjections[key];
            bool selectedOption = key == selectedKey;
            optionProjections[key] = projection with
            {
                IsSelected = selectedOption,
                ComparisonRationale = selectedOption
                    ? "selected-by-deterministic-comparison"
                    : projection.IsKnown && projection.RouteSafetyAllowed
                        ? "eligible-but-not-selected-by-deterministic-comparison"
                        : projection.ComparisonRationale,
            };
        }
        selectedProjection = optionProjections[selectedKey];
        return true;
    }

    private static NetherCodeTransformHardExclusionEvidence ResolveRecoveryTransformHardExclusions(
        IReadOnlyList<NetherEventOption> options
    )
    {
        NetherCodeTransformEligibilityEvidence?[] eligibility = (options ?? Array.Empty<NetherEventOption>())
            .Select(option => option?.RecoveryBranchSafety?.TransformEligibility)
            .ToArray();
        if (eligibility.Length != 3
            || eligibility.Any(item => item == null || !item.IsKnown || !item.IsRecovery)
            || eligibility.Select(item => item!.HardExcludedCodes ?? Array.Empty<NetherCodeTransformHardExclusion>())
                .Distinct()
                .Count() != 1)
        {
            return NetherCodeTransformHardExclusionEvidence.Unknown(
                "code-transform-hard-exclusions-not-captured"
            );
        }
        return new NetherCodeTransformHardExclusionEvidence
        {
            IsKnown = true,
            HardExcludedCodes = eligibility[0]!.HardExcludedCodes
                ?? Array.Empty<NetherCodeTransformHardExclusion>(),
        };
    }

    private static NetherInteractiveEventOptionKey OptionKey(NetherEventOption option) => new(
        option.EventId,
        option.EventPartId,
        option.OptionNumber
    );

    private static NetherInteractiveOptionProjection CreateProjection(
        NetherEventDecision decision,
        long floorId,
        long nodeId,
        bool isKnown,
        bool hasRouteSafetyEvidence,
        bool routeSafetyAllowed,
        string routeSafetyUnknownReason,
        NetherInteractiveOptionHardGate firstFailingHardGate = NetherInteractiveOptionHardGate.None,
        NetherInteractiveOptionSelectionTier selectionTier = NetherInteractiveOptionSelectionTier.None,
        NetherStrategyUnknownReasonCode unknownReasonCode = NetherStrategyUnknownReasonCode.None,
        bool isSelected = false,
        string comparisonRationale = ""
    )
    {
        NetherEventOptionAudit? policyAudit = decision.OptionAudits.FirstOrDefault(audit =>
            audit.OptionNumber == decision.OptionNumber
            && (audit.EventPartId <= 0 || audit.EventPartId == decision.EventPartId)
        );
        NetherInteractiveOptionHardGate gate = firstFailingHardGate != NetherInteractiveOptionHardGate.None
            ? firstFailingHardGate
            : policyAudit == null
                ? NetherInteractiveOptionHardGate.None
                : MapInteractiveHardGate(policyAudit.FirstFailingHardGate);
        NetherInteractiveOptionSelectionTier tier = selectionTier != NetherInteractiveOptionSelectionTier.None
            ? selectionTier
            : MapInteractiveSelectionTier(policyAudit?.SelectionTier ?? NetherEventOptionSelectionTier.None);
        NetherStrategyUnknownReasonCode code = unknownReasonCode != NetherStrategyUnknownReasonCode.None
            ? unknownReasonCode
            : policyAudit?.UnknownReasonCode ?? NetherStrategyUnknownReasonCode.None;
        bool selected = isSelected || policyAudit?.IsSelected == true;
        string rationale = string.IsNullOrWhiteSpace(comparisonRationale)
            ? policyAudit?.ComparisonRationale ?? (isKnown ? "eligible-for-comparison" : "excluded")
            : comparisonRationale;
        return new(
        decision.OptionNumber,
        decision.ExpectedErosionDelta,
        decision.HpDelta,
        decision.ExpectedEffects.ToArray()
    )
    {
        EventId = decision.EventId,
        EventPartId = decision.EventPartId,
        FloorId = floorId,
        NodeId = nodeId,
        IsKnown = isKnown,
        UnknownReason = isKnown ? string.Empty : routeSafetyUnknownReason,
        HasRouteSafetyEvidence = hasRouteSafetyEvidence,
        RouteSafetyAllowed = routeSafetyAllowed,
        RouteSafetyUnknownReason = routeSafetyUnknownReason,
        Battle = decision.Battle,
        Reward = decision.Reward,
        AllowsPartialActiveDeaths = decision.AllowsPartialActiveDeaths,
        PartialDeathEligibility = decision.PartialDeathEligibility,
        HasCommittedProcurementEvidence = decision.CommittedGoldMinimum > 0
            || decision.CommittedKeyMinimum > 0,
        CommittedGoldMinimum = decision.CommittedGoldMinimum,
        CommittedKeyMinimum = decision.CommittedKeyMinimum,
        IsMandatoryRankFiveKeyObjective = decision.PartialDeathEligibility?.AllowsHpPaidEventKey == true
            && decision.PartialDeathEligibility.ExactTreasureRank == 5
            || decision.RankFiveKeyProcurementCommitment != null
            || decision.RankFiveTreasureObjective.HasValue,
        RankFiveKeyProcurementCommitment = decision.RankFiveKeyProcurementCommitment,
        RankFiveTreasureObjective = decision.RankFiveTreasureObjective,
        ParticipatesInSelection = true,
        IsSelected = selected,
        FirstFailingHardGate = gate,
        SelectionTier = tier,
        UnknownReasonCode = code,
        ComparisonRationale = rationale,
    };
    }

    private static NetherInteractiveOptionProjection UnknownProjection(
        NetherEventOption option,
        long floorId,
        long nodeId,
        string rejectionDetail,
        NetherInteractiveOptionHardGate? firstFailingHardGate = null,
        NetherStrategyUnknownReasonCode unknownReasonCode = NetherStrategyUnknownReasonCode.None,
        string comparisonRationale = "",
        bool isKnown = false
    )
    {
        NetherInteractiveOptionHardGate gate = firstFailingHardGate
            ?? MapInteractiveHardGate(
                InferEventOptionHardGate(option, rejectionDetail)
            );
        bool isUnknown = !isKnown && !string.IsNullOrWhiteSpace(option.UnknownReason);
        NetherStrategyUnknownReasonCode code = unknownReasonCode != NetherStrategyUnknownReasonCode.None
            ? unknownReasonCode
            : isUnknown
                ? NetherStrategyUnknownReasonCodes.FromDetail(option.UnknownReason)
                : NetherStrategyUnknownReasonCode.None;
        return new(
        option.OptionNumber,
        0,
        0,
        option.Effects ?? Array.Empty<NetherEffect>()
    )
    {
        EventId = option.EventId,
        EventPartId = option.EventPartId,
        FloorId = floorId,
        NodeId = nodeId,
        IsKnown = isKnown && !isUnknown,
        UnknownReason = isKnown
            ? string.Empty
            : string.IsNullOrWhiteSpace(option.UnknownReason)
                ? rejectionDetail
                : option.UnknownReason,
        HasRouteSafetyEvidence = false,
        RouteSafetyAllowed = false,
        RouteSafetyUnknownReason = rejectionDetail,
        ParticipatesInSelection = true,
        IsSelected = false,
        FirstFailingHardGate = gate,
        SelectionTier = NetherInteractiveOptionSelectionTier.None,
        UnknownReasonCode = code,
        ComparisonRationale = string.IsNullOrWhiteSpace(comparisonRationale)
            ? "excluded:first-failing-gate=" + gate
            : comparisonRationale,
    };
    }

    private static NetherEventOptionHardGate InferEventOptionHardGate(
        NetherEventOption option,
        string detail
    )
    {
        string combined = string.Join("|", option.UnknownReason, detail);
        if (combined.Contains("battle-route", StringComparison.OrdinalIgnoreCase))
            return NetherEventOptionHardGate.BattleRouteSafety;
        if (combined.Contains("recovery", StringComparison.OrdinalIgnoreCase))
            return NetherEventOptionHardGate.RecoveryBranchSafety;
        if (combined.Contains("procurement", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("committed", StringComparison.OrdinalIgnoreCase))
            return NetherEventOptionHardGate.Procurement;
        if (combined.Contains("resource", StringComparison.OrdinalIgnoreCase))
            return NetherEventOptionHardGate.Resource;
        if (combined.Contains("hp", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("lethal", StringComparison.OrdinalIgnoreCase))
            return NetherEventOptionHardGate.HpSafety;
        if (combined.Contains("erosion", StringComparison.OrdinalIgnoreCase))
            return NetherEventOptionHardGate.ErosionSafety;
        if (combined.Contains("binding", StringComparison.OrdinalIgnoreCase))
            return NetherEventOptionHardGate.Binding;
        if (combined.Contains("effect", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("target", StringComparison.OrdinalIgnoreCase))
            return NetherEventOptionHardGate.NativeEffect;
        return NetherEventOptionHardGate.NativeMasterData;
    }

    private static bool IsKnownDecision(NetherEventDecision decision) =>
        decision.PauseReason is not NetherPauseReason.UnknownMasterData
            and not NetherPauseReason.UnknownEffect
            and not NetherPauseReason.BindingUnavailable
            and not NetherPauseReason.InvalidConfiguration;

    private static NetherInteractiveOptionHardGate MapInteractiveHardGate(
        NetherEventOptionHardGate gate
    ) => gate switch
    {
        NetherEventOptionHardGate.Binding => NetherInteractiveOptionHardGate.Binding,
        NetherEventOptionHardGate.NativeMasterData => NetherInteractiveOptionHardGate.NativeMasterData,
        NetherEventOptionHardGate.NativeEffect => NetherInteractiveOptionHardGate.NativeEffect,
        NetherEventOptionHardGate.RecoveryBranchSafety => NetherInteractiveOptionHardGate.RecoveryBranchSafety,
        NetherEventOptionHardGate.BattleRouteSafety => NetherInteractiveOptionHardGate.BattleRouteSafety,
        NetherEventOptionHardGate.HpSafety => NetherInteractiveOptionHardGate.HpSafety,
        NetherEventOptionHardGate.ErosionSafety => NetherInteractiveOptionHardGate.ErosionSafety,
        NetherEventOptionHardGate.Resource => NetherInteractiveOptionHardGate.Resource,
        NetherEventOptionHardGate.Procurement => NetherInteractiveOptionHardGate.Procurement,
        NetherEventOptionHardGate.RouteSafety => NetherInteractiveOptionHardGate.RouteSafety,
        NetherEventOptionHardGate.CandidateIdentity => NetherInteractiveOptionHardGate.CandidateIdentity,
        _ => NetherInteractiveOptionHardGate.None,
    };

    private static NetherInteractiveOptionSelectionTier MapInteractiveSelectionTier(
        NetherEventOptionSelectionTier tier
    ) => tier switch
    {
        NetherEventOptionSelectionTier.Recovery => NetherInteractiveOptionSelectionTier.Recovery,
        NetherEventOptionSelectionTier.TreasureKey => NetherInteractiveOptionSelectionTier.TreasureKey,
        NetherEventOptionSelectionTier.TreasureHpPayment => NetherInteractiveOptionSelectionTier.TreasureHpPayment,
        NetherEventOptionSelectionTier.BossBattle
            or NetherEventOptionSelectionTier.MiniBossBattle
            or NetherEventOptionSelectionTier.NormalBattle => NetherInteractiveOptionSelectionTier.Battle,
        NetherEventOptionSelectionTier.RedRankFiveReward
            or NetherEventOptionSelectionTier.GoldRankFiveReward
            or NetherEventOptionSelectionTier.Reward => NetherInteractiveOptionSelectionTier.Reward,
        NetherEventOptionSelectionTier.DirectCodeOffer => NetherInteractiveOptionSelectionTier.DirectCodeOffer,
        _ => NetherInteractiveOptionSelectionTier.NeutralSafeOption,
    };

    private NetherEventDecision DecideInteractiveOption(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        NetherFloorNodeType floorKind,
        bool requireCompleteRecoveryBranchSafety
    ) => floorKind switch
    {
        NetherFloorNodeType.Event => _eventPolicy.DecideEvent(snapshot, options, settings),
        NetherFloorNodeType.Recovery => DecideRecoveryOption(
            snapshot,
            options,
            settings,
            requireCompleteRecoveryBranchSafety
        ),
        NetherFloorNodeType.Treasure => _eventPolicy.DecideTreasure(snapshot, options, settings),
        _ => new NetherEventDecision
        {
            Kind = NetherEventDecisionKind.Pause,
            PauseReason = NetherPauseReason.UnknownFloor,
            Detail = "unsupported-interactive-option-floor-kind",
        },
    };

    private NetherEventDecision DecideRecoveryOption(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        bool requireCompleteRecoveryBranchSafety
    )
    {
        NetherCodeTransformHardExclusionEvidence hardExclusions =
            NetherCodeTransformHardExclusionEvidence.Unknown(
                "code-transform-hard-exclusions-not-captured"
            );
        return requireCompleteRecoveryBranchSafety
            ? _eventPolicy.DecideRecovery(
                snapshot,
                options,
                settings,
                Array.Empty<NetherErosionModifier>(),
                hardExclusions,
                requireCompleteBranchEvidence: true
            )
            : _eventPolicy.DecideRecoveryForRouteAnalysis(
                snapshot,
                options,
                settings,
                Array.Empty<NetherErosionModifier>(),
                hardExclusions
            );
    }

    private static NetherRouteHpRule MapOptionHpRule(
        NetherFloorNodeType floorKind,
        NetherEventDecision decision
    )
    {
        var projection = new NetherInteractiveOptionProjection(
            decision.OptionNumber,
            decision.ExpectedErosionDelta,
            decision.HpDelta,
            decision.ExpectedEffects
        )
        {
            AllowsPartialActiveDeaths = decision.AllowsPartialActiveDeaths,
        };
        return NetherRouteHpRuleMapper.Map(
            floorKind,
            [projection],
            new NetherInteractiveWorstCaseProjection(decision.ExpectedErosionDelta, decision.HpDelta)
        );
    }

    private static IReadOnlyList<NetherEventOption> ApplyProcurementBudgets(
        IReadOnlyList<NetherEventOption> options,
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? budgets
    )
    {
        if (options == null || budgets == null || budgets.Count == 0)
            return options ?? Array.Empty<NetherEventOption>();

        return options.Select(option =>
        {
            NetherInteractiveEventOptionKey key = OptionKey(option);
            if (!budgets.TryGetValue(key, out NetherEventProcurementBudget budget))
            {
                return option;
            }
            if (!budget.IsValid)
            {
                return option with
                {
                    UnknownReason = string.IsNullOrWhiteSpace(option.UnknownReason)
                        ? "event-option-invalid-committed-procurement-budget"
                        : option.UnknownReason + ":invalid-committed-procurement-budget",
                };
            }
            return option with
            {
                CommittedGoldMinimum = budget.GoldMinimum,
                CommittedKeyMinimum = budget.KeyMinimum,
            };
        }).ToArray();
    }

    private static bool HasSafeHpFloor(
        NetherSnapshot snapshot,
        int hpDelta,
        int minimumHpPermille,
        NetherRouteHpRule hpRule
    )
    {
        if (hpDelta >= 0)
            return true;
        try
        {
            if (hpRule is NetherRouteHpRule.TreasureGroupSurvival
                or NetherRouteHpRule.HpPaidKeyGroupSurvival)
            {
                return snapshot.Characters.Any(character =>
                    character.IsActive && checked(character.HpPermille + hpDelta) > 0
                );
            }
            foreach (NetherCharacterState character in snapshot.Characters)
            {
                if (character.IsActive && checked(character.HpPermille + hpDelta) <= 0)
                    return false;
            }
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static void CaptureMoreSpecificRejection(
        NetherEventDecision decision,
        ref NetherPauseReason rejection,
        ref string detail
    )
    {
        if (rejection != NetherPauseReason.NoSafeRoute)
            return;
        if (decision.PauseReason == NetherPauseReason.NoSafeRoute)
        {
            if (!string.IsNullOrEmpty(decision.Detail))
                detail = decision.Detail;
            return;
        }
        rejection = decision.PauseReason;
        detail = decision.Detail;
    }

    private static NetherInteractiveFloorPreEntrySafetyResult EvaluateShopOff(
        NetherInteractiveFloorPreEntrySafetyInput input
    )
    {
        if (!input.CanCloseShop)
        {
            return NetherInteractiveFloorPreEntrySafetyResult.Pause(
                NetherPauseReason.BindingUnavailable,
                "interactive-shop-close-binding-unavailable"
            );
        }
        // ShopOff is the default and uses this proved close exit.  EquipmentBags may also
        // enter only because the same exact close exists; the later popup policy still has to
        // prove a particular purchase's content, amount and Gold cost before it mutates.  Do
        // not reject an otherwise safe route solely because the user enabled an optional buy.
        if (input.Settings!.ShopMode is not (NetherShopMode.Off or NetherShopMode.EquipmentBags))
        {
            return NetherInteractiveFloorPreEntrySafetyResult.Pause(
                NetherPauseReason.InvalidConfiguration,
                "interactive-shop-mode-invalid"
            );
        }
        return NetherInteractiveFloorPreEntrySafetyResult.SafeNeutral();
    }

    private static NetherInteractiveFloorPreEntrySafetyResult Unknown(string detail) =>
        NetherInteractiveFloorPreEntrySafetyResult.Pause(NetherPauseReason.UnknownMasterData, detail);
}

internal static class NetherInteractiveFloorPreEntrySafetyCharacterExtensions
{
    public static IReadOnlyList<int> SelectHpPermille(this IReadOnlyList<NetherCharacterState> characters)
    {
        var values = new int[characters.Count];
        for (int index = 0; index < characters.Count; index++)
            values[index] = characters[index].HpPermille;
        return values;
    }
}
