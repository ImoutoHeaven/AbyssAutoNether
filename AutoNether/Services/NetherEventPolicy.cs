#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal enum NetherEventBattleTier
{
    Unknown = 0,
    NormalBattle = 1,
    MiniBoss = 2,
    Boss = 3,
}

internal sealed record NetherEventBattleEvidence(
    long BattleId,
    long BattleStageId,
    int BattleType,
    int CodeDropRatio,
    NetherEventBattleTier SemanticTier
)
{
    public bool IsKnown => BattleId > 0
        && BattleStageId > 0
        && BattleType >= 0
        && CodeDropRatio >= 0
        && SemanticTier != NetherEventBattleTier.Unknown;

    public string UnknownReason { get; init; } = string.Empty;

    public static NetherEventBattleEvidence Unknown(
        long battleId,
        string reason
    ) => new(battleId, 0, 0, 0, NetherEventBattleTier.Unknown)
    {
        UnknownReason = string.IsNullOrWhiteSpace(reason)
            ? "event-battle-evidence-unavailable"
            : reason,
    };
}

internal sealed record NetherEventRewardEvidence(
    long ContentId,
    long ItemId,
    int ItemType,
    NetherRewardRarity Rarity,
    int Amount
)
{
    public bool IsKnown => ContentId > 0
        && ItemId > 0
        && ItemType >= 0
        && NetherEventNativeMapping.IsKnownRewardRarity(Rarity)
        && Amount >= 0;

    public string UnknownReason { get; init; } = string.Empty;
}

internal readonly record struct NetherEventCommitmentKey(
    long EventId,
    long EventPartId,
    long FloorId,
    long NodeId,
    int OptionNumber
);

/// <summary>
/// Exact branch-local procurement state supplied by route/pre-entry evidence. A zero value means
/// that no budget is committed for the option; it is never derived from a displayed reward or a
/// speculative future node.
/// </summary>
internal readonly record struct NetherEventProcurementBudget(
    int GoldMinimum,
    int KeyMinimum
)
{
    public bool IsValid => GoldMinimum >= 0 && KeyMinimum >= 0;
}

/// <summary>
/// Immutable route-facing identity.  The native request still submits floor/option coordinates;
/// this record proves which server master rows were used before that request was allowed.
/// </summary>
internal sealed record NetherEventCommitment(
    long EventId,
    long EventPartId,
    int OptionNumber,
    IReadOnlyList<NetherEffect> Effects,
    int ProjectedErosion,
    int HpDelta
)
{
    /// <summary>Exact rendered floor master identity captured before the native update.</summary>
    public long FloorId { get; init; }
    /// <summary>Exact rendered floor/node coordinate captured before the native update.</summary>
    public long NodeId { get; init; }
    public NetherEventBattleEvidence? Battle { get; init; }
    public NetherEventRewardEvidence? Reward { get; init; }
    public int? ProjectedNetherGold { get; init; }
    public int? ProjectedTreasureKeys { get; init; }
    public int CommittedGoldMinimum { get; init; }
    public int CommittedKeyMinimum { get; init; }
    public NetherInteractivePartialDeathEligibility? PartialDeathEligibility { get; init; }
    public bool AllowsPartialActiveDeaths { get; init; }
    public NetherRankFiveKeyProcurementCommitment? RankFiveKeyProcurementCommitment { get; init; }
    public NetherRankFiveTreasureIdentity? RankFiveTreasureObjective { get; init; }
    public bool IsValid => EventId > 0
        && EventPartId > 0
        && FloorId > 0
        && NodeId > 0
        && OptionNumber > 0
        && Effects != null
        && Effects.Count is >= 1 and <= 4
        && Effects.All(effect => effect != null
            && effect.Known
            && effect.ContentKnown
            && effect.Kind != NetherEffectKind.Unknown)
        && Effects
            .Where(effect => effect.Kind == NetherEffectKind.Battle)
            .All(effect => Battle != null && Battle.IsKnown)
        && Effects
            .Where(effect => effect.Kind == NetherEffectKind.Item)
            .All(effect => Reward != null && Reward.IsKnown)
        && ProjectedNetherGold.HasValue
        && ProjectedTreasureKeys.HasValue
        && CommittedGoldMinimum >= 0
        && CommittedKeyMinimum >= 0
        && (!AllowsPartialActiveDeaths
            || PartialDeathEligibility?.IsKnown == true)
        && (RankFiveKeyProcurementCommitment == null || RankFiveKeyProcurementCommitment.IsValid)
        && (RankFiveTreasureObjective == null || RankFiveTreasureObjective.Value.IsValid)
        && (PartialDeathEligibility == null
            || PartialDeathEligibility.IsKnown
                && PartialDeathEligibility.EventId == EventId
                && PartialDeathEligibility.EventPartId == EventPartId
                && PartialDeathEligibility.ObjectiveNodeId == NodeId);

    public bool Matches(NetherEventOption option) => option != null
        && option.OptionNumber == OptionNumber
        && option.EventId == EventId
        && option.EventPartId == EventPartId
        && option.FloorId == FloorId
        && option.NodeId == NodeId
        && option.ProjectedErosion == ProjectedErosion
        && option.ProjectedHpDelta == HpDelta
        && option.ProjectedNetherGold == ProjectedNetherGold
        && option.ProjectedTreasureKeys == ProjectedTreasureKeys
        && option.CommittedGoldMinimum == CommittedGoldMinimum
        && option.CommittedKeyMinimum == CommittedKeyMinimum
        && option.AllowsPartialActiveDeaths == AllowsPartialActiveDeaths
        && Equals(RankFiveKeyProcurementCommitment, option.RankFiveKeyProcurementCommitment)
        && RankFiveTreasureObjective == option.RankFiveTreasureObjective
        && PartialDeathMatches(PartialDeathEligibility, option.PartialDeathEligibility)
        && NetherEventPolicy.EffectFingerprintsEqual(Effects, option.Effects)
        && EvidenceMatches(
            Battle,
            option.BattleEvidence ?? option.Effects
                .Select(effect => effect.BattleEvidence)
                .FirstOrDefault(value => value != null)
        )
        && EvidenceMatches(
            Reward,
            option.RewardEvidence ?? option.Effects
                .Select(effect => effect.RewardEvidence)
                .FirstOrDefault(value => value != null)
        );

    public bool Matches(
        NetherEventOption option,
        int projectedErosion,
        int hpDelta,
        NetherEventBattleEvidence? battle,
        NetherEventRewardEvidence? reward,
        int? projectedNetherGold = null,
        int? projectedTreasureKeys = null
    ) => Matches(option with
        {
            ProjectedErosion = projectedErosion,
            ProjectedHpDelta = hpDelta,
            ProjectedNetherGold = projectedNetherGold,
            ProjectedTreasureKeys = projectedTreasureKeys,
        })
        && EvidenceMatches(Battle, battle)
        && EvidenceMatches(Reward, reward);

    public bool Matches(NetherEventCommitment other) => other != null
        && EventId == other.EventId
        && EventPartId == other.EventPartId
        && FloorId == other.FloorId
        && NodeId == other.NodeId
        && OptionNumber == other.OptionNumber
        && ProjectedErosion == other.ProjectedErosion
        && HpDelta == other.HpDelta
        && ProjectedNetherGold == other.ProjectedNetherGold
        && ProjectedTreasureKeys == other.ProjectedTreasureKeys
        && CommittedGoldMinimum == other.CommittedGoldMinimum
        && CommittedKeyMinimum == other.CommittedKeyMinimum
        && AllowsPartialActiveDeaths == other.AllowsPartialActiveDeaths
        && Equals(RankFiveKeyProcurementCommitment, other.RankFiveKeyProcurementCommitment)
        && RankFiveTreasureObjective == other.RankFiveTreasureObjective
        && NetherEventPolicy.EffectFingerprintsEqual(Effects, other.Effects)
        && EvidenceMatches(Battle, other.Battle)
        && EvidenceMatches(Reward, other.Reward)
        && PartialDeathMatches(PartialDeathEligibility, other.PartialDeathEligibility);

    private static bool EvidenceMatches(
        NetherEventBattleEvidence? expected,
        NetherEventBattleEvidence? actual
    ) => expected == null && actual == null
        || expected != null
            && actual != null
            && expected.BattleId == actual.BattleId
            && expected.BattleStageId == actual.BattleStageId
            && expected.BattleType == actual.BattleType
            && expected.CodeDropRatio == actual.CodeDropRatio
            && expected.SemanticTier == actual.SemanticTier
            && expected.UnknownReason == actual.UnknownReason;

    private static bool EvidenceMatches(
        NetherEventRewardEvidence? expected,
        NetherEventRewardEvidence? actual
    ) => expected == null && actual == null
        || expected != null
            && actual != null
            && expected.ContentId == actual.ContentId
            && expected.ItemId == actual.ItemId
            && expected.ItemType == actual.ItemType
            && expected.Rarity == actual.Rarity
            && expected.Amount == actual.Amount
            && expected.UnknownReason == actual.UnknownReason;

    private static bool PartialDeathMatches(
        NetherInteractivePartialDeathEligibility? expected,
        NetherInteractivePartialDeathEligibility? actual
    ) => expected == null && actual == null
        || expected != null
            && actual != null
            && expected.Kind == actual.Kind
            && expected.EventId == actual.EventId
            && expected.EventPartId == actual.EventPartId
            && expected.ObjectiveNodeId == actual.ObjectiveNodeId
            && expected.IsKnown == actual.IsKnown
            && expected.ObjectiveReachable == actual.ObjectiveReachable
            && expected.ExactTreasureRank == actual.ExactTreasureRank
            && expected.IsOnlyTerminalReachingRoute == actual.IsOnlyTerminalReachingRoute
            && expected.NoBetterAffordableCurrencyKeySource == actual.NoBetterAffordableCurrencyKeySource
            && expected.UnknownReason == actual.UnknownReason;
}

/// <summary>
/// Mode facts already justified by route/strategy evidence.  Unknown fields never create a
/// reward preference; callers may still use the legacy local safety ordering when no mode package
/// is available.
/// </summary>
internal sealed record NetherEventStrategyEvidence
{
    public bool IsKnown { get; init; }
    public NetherStrategyMode Mode { get; init; }
    public bool ResearchIncomplete { get; init; }
    public bool HasRankFiveTreasureObjective { get; init; }
    public bool HasRouteEvidence { get; init; }
    public bool HasResourceEvidence { get; init; }
    public bool HasSemanticEvidence { get; init; }
    public bool HasPartialDeathEvidence { get; init; }
    public bool AllowsPartialActiveDeaths { get; init; }
    public string UnknownReason { get; init; } = string.Empty;

    public bool IsUsableFor(NetherStrategyMode requestedMode) =>
        IsKnown
        && Mode == requestedMode
        && (requestedMode != NetherStrategyMode.Research
            || HasRouteEvidence && HasResourceEvidence && HasSemanticEvidence);
}

internal enum NetherRecoveryBranchKind
{
    Unknown = 0,
    Rest,
    Purification,
    Transform,
}

/// <summary>
/// Exact proof for one Recovery option's complete visible continuation. The native Recovery
/// popup supplies only the local action; route policy may use this record only after the complete
/// next visible branch has been captured. An absent proof preserves compatibility with legacy
/// callers, while a supplied but invalid proof is fail-closed.
/// </summary>
internal sealed record NetherRecoveryBranchSafetyEvidence
{
    public NetherRecoveryBranchKind BranchKind { get; init; }
    public bool IsKnown { get; init; }
    public bool IsCompleteVisibleBranch { get; init; }
    public bool IsNextVisibleBranchSafe { get; init; }
    public string UnknownReason { get; init; } = string.Empty;
    /// <summary>
    /// Exact held-Code transform eligibility captured with the same Recovery branch. It is
    /// carried through route binding so the complete popup cannot reconstruct transform safety
    /// from a later child popup or from a display-only Code list.
    /// </summary>
    public NetherCodeTransformEligibilityEvidence? TransformEligibility { get; init; }

    public bool IsAuthoritative => IsKnown
        && IsCompleteVisibleBranch
        && BranchKind != NetherRecoveryBranchKind.Unknown;
}

internal sealed record NetherEventOption(int OptionNumber, IReadOnlyList<NetherEffect> Effects)
{
    public long EventId { get; init; }
    public long EventPartId { get; init; }
    public long FloorId { get; init; }
    public long NodeId { get; init; }
    public int? ProjectedErosion { get; init; }
    public int? ProjectedHpDelta { get; init; }
    public int? ProjectedNetherGold { get; init; }
    public int? ProjectedTreasureKeys { get; init; }
    public NetherInteractivePartialDeathEligibility? PartialDeathEligibility { get; init; }
    public bool RequiresExactBinding { get; init; }
    public string UnknownReason { get; init; } = string.Empty;
    public bool HasRouteSafetyEvidence { get; init; }
    public bool RouteSafetyAllowed { get; init; } = true;
    public string RouteSafetyUnknownReason { get; init; } = string.Empty;
    public int CommittedGoldMinimum { get; init; }
    public int CommittedKeyMinimum { get; init; }
    public bool IsMandatoryRankFiveKeyObjective { get; init; }
    public NetherEventBattleEvidence? BattleEvidence { get; init; }
    public NetherEventRewardEvidence? RewardEvidence { get; init; }
    public bool AllowsPartialActiveDeaths { get; init; }
    /// <summary>
    /// Exact per-option route/resource/semantic facts. An unknown sibling must not poison another
    /// option from the same native popup.
    /// </summary>
    public NetherEventStrategyEvidence? StrategyEvidence { get; init; }
    public NetherRecoveryBranchSafetyEvidence? RecoveryBranchSafety { get; init; }
    public NetherRankFiveKeyProcurementCommitment? RankFiveKeyProcurementCommitment { get; init; }
    public NetherRankFiveTreasureIdentity? RankFiveTreasureObjective { get; init; }
}

internal enum NetherEventDecisionKind
{
    Select,
    Pause,
}

internal sealed record NetherEventDecision
{
    public NetherEventDecisionKind Kind { get; init; }
    public NetherActionKind ActionKind { get; init; }
    public int OptionNumber { get; init; }
    public long ReplacementCodeId { get; init; }
    public int ProjectedErosion { get; init; }
    public int ExpectedErosionDelta { get; init; }
    public int HpDelta { get; init; }
    public int ProjectedNetherGold { get; init; }
    public int ProjectedTreasureKeys { get; init; }
    public int CommittedGoldMinimum { get; init; }
    public int CommittedKeyMinimum { get; init; }
    /// <summary>
    /// Immutable authoritative effect payload for the selected native option.  Reconcile must
    /// compare this exact server-visible resource delta rather than treating an option click as
    /// a generic visual close.
    /// </summary>
    public IReadOnlyList<NetherEffect> ExpectedEffects { get; init; } = Array.Empty<NetherEffect>();
    public bool StartsBattleAfterSelection { get; init; }
    public bool AllowsPartialActiveDeaths { get; init; }
    public long EventId { get; init; }
    public long EventPartId { get; init; }
    public long FloorId { get; init; }
    public long NodeId { get; init; }
    public NetherEventCommitment? Commitment { get; init; }
    public NetherRankFiveKeyProcurementCommitment? RankFiveKeyProcurementCommitment { get; init; }
    public NetherRankFiveTreasureIdentity? RankFiveTreasureObjective { get; init; }
    public NetherEventBattleEvidence? Battle { get; init; }
    public NetherEventRewardEvidence? Reward { get; init; }
    public NetherInteractivePartialDeathEligibility? PartialDeathEligibility { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
}

internal readonly record struct NetherShopContent(
    long contentId,
    long itemId,
    int itemType,
    NetherRewardRarity rarity,
    int price,
    bool usesNetherGold,
    int amount = 1,
    bool known = true
)
{
    public long ContentId => contentId;
    public long ItemId => itemId;
    public int ItemType => itemType;
    public NetherRewardRarity Rarity => rarity;
    public int Price => price;
    public bool UsesNetherGold => usesNetherGold;
    public int Amount => amount;
    public bool Known => known;
    /// <summary>Exact MNetherFloorShopContents.content_type; retained for strategy evidence.</summary>
    public int RawContentType { get; init; }
    /// <summary>
    /// Explicit key-product evidence. The current native shop transport exposes only raw content
    /// fields; until a mapper proves this semantic, the value remains false and key procurement
    /// fails closed.
    /// </summary>
    public bool IsTreasureKey { get; init; }
}

internal enum NetherShopDecisionKind
{
    Leave,
    Buy,
    Pause,
}

internal sealed record NetherShopDecision
{
    public NetherShopDecisionKind Kind { get; init; }
    public long ContentId { get; init; }
    public int Amount { get; init; }
    /// <summary>Exact server-visible NetherGold debit for a buy transaction.</summary>
    public int GoldCost { get; init; }
    /// <summary>Exact route-owned procurement commitment carried into the native child action.</summary>
    public NetherShopProcurementCommitment? ProcurementCommitment { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
}

/// <summary>Exact branch-local Shop child order for a proven rank-five Treasure objective.</summary>
internal sealed record NetherShopProcurementCommitment
{
    public bool IsKnown { get; init; }
    public bool RequiresRankFiveKey { get; init; }
    public NetherRankFiveTreasureIdentity? Objective { get; init; }
    public long KeyContentId { get; init; }
    public int KeyCost { get; init; } = 200;
    public bool RequiresRankFiveBag { get; init; }
    public long BagContentId { get; init; }
    public int BagCost { get; init; } = 300;
    public string UnknownReason { get; init; } = string.Empty;

    public bool IsValid => IsKnown
        && (!RequiresRankFiveKey || KeyCost == 200)
        && (!RequiresRankFiveKey || Objective is { IsValid: true })
        && (!RequiresRankFiveBag || BagCost == 300);
}

internal sealed class NetherEventPolicy
{
    private readonly NetherErosionPolicy _erosionPolicy = new();
    private readonly NetherCodeTransformPolicy _transformPolicy = new();

    public NetherEventDecision DecideEvent(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings
    ) => DecideEvent(snapshot, options, settings, Array.Empty<NetherErosionModifier>());

    public NetherEventDecision DecideEvent(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers
    ) => Decide(
        snapshot,
        options,
        settings,
        modifiers,
        isRecovery: false,
        NetherCodeTransformHardExclusionEvidence.Unknown(
            "code-transform-outside-recovery"
        ),
        strategyEvidence: null
    );

    public NetherEventDecision DecideEvent(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        NetherEventStrategyEvidence? strategyEvidence
    ) => Decide(
        snapshot,
        options,
        settings,
        modifiers,
        isRecovery: false,
        NetherCodeTransformHardExclusionEvidence.Unknown(
            "code-transform-outside-recovery"
        ),
        strategyEvidence
    );

    public NetherEventDecision DecideRecovery(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings
    ) => DecideRecovery(snapshot, options, settings, Array.Empty<NetherErosionModifier>());

    public NetherEventDecision DecideRecovery(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers
    ) => DecideRecovery(
        snapshot,
        options,
        settings,
        modifiers,
        NetherCodeTransformHardExclusionEvidence.Unknown(
            "code-transform-hard-exclusions-not-captured"
        )
    );

    public NetherEventDecision DecideRecovery(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        NetherCodeTransformHardExclusionEvidence hardExclusions
    ) => DecideRecovery(
        snapshot,
        options,
        settings,
        modifiers,
        hardExclusions,
        requireCompleteBranchEvidence: true
    );

    /// <summary>
    /// Provisional, read-only route discovery before the selected-horizon Recovery proof exists.
    /// This seam may never dispatch a native payment; popup production uses the fail-closed
    /// overload above and must provide complete visible branch evidence.
    /// </summary>
    internal NetherEventDecision DecideRecoveryForRouteAnalysis(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        NetherCodeTransformHardExclusionEvidence hardExclusions
    ) => DecideRecovery(
        snapshot,
        options,
        settings,
        modifiers,
        hardExclusions,
        requireCompleteBranchEvidence: false
    );

    public NetherEventDecision DecideRecovery(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        NetherCodeTransformHardExclusionEvidence hardExclusions,
        bool requireCompleteBranchEvidence
    )
    {
        if (TryDecideRecoveryFromCompleteBranchEvidence(
                snapshot,
                options,
                settings,
                modifiers,
                hardExclusions,
                out NetherEventDecision? branchDecision
            ))
        {
            return branchDecision!;
        }

        if (requireCompleteBranchEvidence)
        {
            return Pause(
                NetherPauseReason.UnknownMasterData,
                "recovery-complete-visible-branch-unavailable"
            );
        }

        return Decide(
            snapshot,
            options,
            settings,
            modifiers,
            isRecovery: true,
            hardExclusions,
            strategyEvidence: null
        );
    }

    public NetherEventDecision DecideTreasure(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings
    ) => DecideTreasure(snapshot, options, settings, Array.Empty<NetherErosionModifier>());

    public NetherEventDecision DecideTreasure(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers
    )
    {
        ValidateInputs(snapshot, options, settings);
        if (settings.TreasureMode != NetherTreasureMode.KeyOnly)
            return Pause(NetherPauseReason.NoSafeRoute, "treasure-mode-off");

        var keyCandidates = new List<EventCandidate>();
        var hpCandidates = new List<EventCandidate>();
        foreach (NetherEventOption option in options)
        {
            bool isExactHpPayment = IsExactTreasureHpPayment(option);
            bool isStrategicallyEligibleHpPayment = isExactHpPayment
                && option.PartialDeathEligibility?.AllowsTreasureHpPayment == true;
            if (!TryValidateOption(
                    option,
                    snapshot,
                    settings,
                    modifiers,
                    allowPartialActiveDeaths: isStrategicallyEligibleHpPayment,
                    new NetherCodeTransformEligibilityEvidence
                    {
                        StrategyMode = settings.StrategyMode,
                        EquipmentOptInEnabled = settings.EquipmentRecoveryCodeTransformEnabled,
                        IsRecovery = false,
                    },
                    strategyEvidence: null,
                    out EventCandidate candidate,
                    out _
                ))
            {
                continue;
            }
            int exactKeyCosts = option.Effects.Count(effect => effect.Kind == NetherEffectKind.TreasureKeyUsed && effect.Amount == 1);
            bool hasOnlySafePayments = option.Effects.All(effect => effect.Kind is not NetherEffectKind.Damage and not NetherEffectKind.Erosion);
            bool hasNoOtherKeyCost = option.Effects.All(effect => effect.Kind != NetherEffectKind.TreasureKeyUsed || effect.Amount == 1);
            if (exactKeyCosts == 1 && hasNoOtherKeyCost && hasOnlySafePayments && snapshot.TreasureKeyCount >= 1)
                keyCandidates.Add(candidate);
            else if (snapshot.TreasureKeyCount < 1 && isStrategicallyEligibleHpPayment)
                hpCandidates.Add(candidate);
        }

        // The live popup exposes distinct Key/Hp/Abyss panels.  A verified one-key option is
        // always preferred.  The exact Damage-only Hp panel is a fallback only when no key is
        // held; the Erosion/Abyss panel is never promoted to a substitute.
        List<EventCandidate> candidates = keyCandidates.Count > 0 ? keyCandidates : hpCandidates;
        if (candidates.Count == 0)
            return Pause(NetherPauseReason.NoSafeRoute, "no-key-only-treasure-option");

        EventCandidate selected = candidates
            .OrderByDescending(candidate => candidate.Benefit)
            .ThenBy(candidate => candidate.Option.OptionNumber)
            .ThenBy(candidate => candidate.Option.EventId)
            .ThenBy(candidate => candidate.Option.EventPartId)
            .ThenBy(candidate => candidate.Option.FloorId)
            .ThenBy(candidate => candidate.Option.NodeId)
            .First();
        return Select(selected);
    }

    public NetherShopDecision DecideShop(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherShopContent> contents,
        NetherAutoClimbSettings settings
    ) => DecideShop(snapshot, contents, settings, commitment: null);

    public NetherShopDecision DecideShop(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherShopContent> contents,
        NetherAutoClimbSettings settings,
        NetherShopProcurementCommitment? commitment
    )
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        if (contents == null)
            throw new ArgumentNullException(nameof(contents));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (settings.ShopMode == NetherShopMode.Off)
            return new NetherShopDecision { Kind = NetherShopDecisionKind.Leave };
        // The native shop mixes MItems with valid ID-less products (keys, code effects, etc.).
        // EquipmentBags ignores those known non-item rows; ItemId is required only for an
        // actual equipment candidate, never as a blanket validity condition for the popup.
        if (contents.Any(content => !content.Known || content.ContentId <= 0 || content.Amount <= 0 || content.Price < 0))
            return new NetherShopDecision { Kind = NetherShopDecisionKind.Pause, PauseReason = NetherPauseReason.UnknownMasterData, Detail = "invalid-shop-content" };

        if (commitment is { IsKnown: true })
        {
            if (!commitment.IsValid)
            {
                return new NetherShopDecision
                {
                    Kind = NetherShopDecisionKind.Pause,
                    PauseReason = NetherPauseReason.UnknownMasterData,
                    Detail = string.IsNullOrWhiteSpace(commitment.UnknownReason)
                        ? "invalid-shop-procurement-commitment"
                        : commitment.UnknownReason,
                };
            }

            // A committed key is always the first child. Once the authoritative snapshot shows
            // that it was acquired, the same commitment may advance to the exact bag child.
            if (commitment.RequiresRankFiveKey && snapshot.TreasureKeyCount == 0)
            {
                NetherShopContent[] exactKeys = contents
                    .Where(content => content.IsTreasureKey
                        && content.UsesNetherGold
                        && content.Price == commitment.KeyCost
                        && (commitment.KeyContentId <= 0 || content.ContentId == commitment.KeyContentId))
                    .ToArray();
                if (exactKeys.Length == 1 && exactKeys[0].Price <= snapshot.NetherGold)
                {
                    NetherShopContent key = exactKeys[0];
                    return new NetherShopDecision
                    {
                        Kind = NetherShopDecisionKind.Buy,
                        ContentId = key.ContentId,
                        Amount = key.Amount,
                        GoldCost = key.Price,
                        ProcurementCommitment = commitment,
                    };
                }
                // Missing/ambiguous/unaffordable key evidence is not permission to buy a bag or
                // reinterpret another raw content type as a key.
                return new NetherShopDecision { Kind = NetherShopDecisionKind.Leave };
            }

            if (commitment.RequiresRankFiveBag)
            {
                NetherShopContent[] exactBags = contents
                    .Where(content => content.ItemId > 0
                        && content.ItemType == 91
                        && content.Rarity >= NetherRewardRarity.Gold
                        && content.UsesNetherGold
                        && content.Price == commitment.BagCost
                        && (commitment.BagContentId <= 0 || content.ContentId == commitment.BagContentId))
                    .ToArray();
                if (exactBags.Length == 1 && exactBags[0].Price <= snapshot.NetherGold)
                {
                    NetherShopContent bag = exactBags[0];
                    return new NetherShopDecision
                    {
                        Kind = NetherShopDecisionKind.Buy,
                        ContentId = bag.ContentId,
                        Amount = bag.Amount,
                        GoldCost = bag.Price,
                        ProcurementCommitment = commitment,
                    };
                }
                return new NetherShopDecision { Kind = NetherShopDecisionKind.Leave };
            }
        }

        NetherShopContent? selected = contents
            .Where(content => content.ItemId > 0)
            .Where(content => content.ItemType == 91)
            .Where(content => content.Rarity >= NetherRewardRarity.Gold)
            .Where(content => content.UsesNetherGold)
            .Where(content => content.Price <= snapshot.NetherGold)
            .OrderByDescending(content => content.Rarity)
            .ThenBy(content => content.Price)
            .ThenBy(content => content.ContentId)
            .Cast<NetherShopContent?>()
            .FirstOrDefault();
        if (selected == null)
            return new NetherShopDecision { Kind = NetherShopDecisionKind.Leave };

        return new NetherShopDecision
        {
            Kind = NetherShopDecisionKind.Buy,
            ContentId = selected.Value.ContentId,
            Amount = selected.Value.Amount,
            GoldCost = selected.Value.Price,
        };
    }

    private bool TryDecideRecoveryFromCompleteBranchEvidence(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        NetherCodeTransformHardExclusionEvidence hardExclusions,
        out NetherEventDecision? decision
    )
    {
        decision = null;
        if (options == null || options.Count != 3)
            return false;

        NetherEventOption[] rests = options.Where(option =>
            option.Effects?.Count == 1
            && option.Effects[0].Kind == NetherEffectKind.Heal).ToArray();
        NetherEventOption[] purifications = options.Where(option =>
            option.Effects?.Count == 1
            && option.Effects[0].Kind == NetherEffectKind.ErosionHeal).ToArray();
        NetherEventOption[] transforms = options.Where(option =>
            option.Effects?.Count == 1
            && option.Effects[0].Kind == NetherEffectKind.AbyssCodeTransform).ToArray();
        if (rests.Length != 1 || purifications.Length != 1 || transforms.Length != 1)
            return false;

        NetherEventOption rest = rests[0];
        NetherEventOption purification = purifications[0];
        NetherEventOption transform = transforms[0];
        NetherRecoveryBranchSafetyEvidence?[] proofs =
        [
            rest.RecoveryBranchSafety,
            purification.RecoveryBranchSafety,
            transform.RecoveryBranchSafety,
        ];
        bool anyProof = proofs.Any(proof => proof != null);
        if (!anyProof)
            return false;
        if (proofs.Any(proof => proof == null || !proof.IsAuthoritative))
        {
            decision = Pause(
                NetherPauseReason.UnknownMasterData,
                proofs.FirstOrDefault(proof => proof != null && !proof.IsAuthoritative)?.UnknownReason
                    ?? "recovery-complete-visible-branch-unavailable"
            );
            return true;
        }
        if (rest.RecoveryBranchSafety!.BranchKind != NetherRecoveryBranchKind.Rest
            || purification.RecoveryBranchSafety!.BranchKind != NetherRecoveryBranchKind.Purification
            || transform.RecoveryBranchSafety!.BranchKind != NetherRecoveryBranchKind.Transform)
        {
            decision = Pause(NetherPauseReason.UnknownMasterData, "recovery-branch-kind-mismatch");
            return true;
        }

        bool restSafe = rest.RecoveryBranchSafety.IsNextVisibleBranchSafe;
        bool purificationSafe = purification.RecoveryBranchSafety.IsNextVisibleBranchSafe;
        NetherEventOption? selected = restSafe == purificationSafe
            ? restSafe
                ? SelectRecoveryTieBreak(snapshot, settings, rest, purification)
                : null
            : restSafe ? rest : purification;
        if (selected == null)
        {
            decision = Pause(NetherPauseReason.NoSafeRoute, "no-complete-safe-recovery-branch");
            return true;
        }

        // Re-run the ordinary public-effect validator on the selected option only. This keeps the
        // branch proof separate from exact HP/erosion/resource/commitment validation and means a
        // stale local option can never be selected merely because its continuation was safe.
        decision = Decide(
            snapshot,
            [selected],
            settings,
            modifiers,
            isRecovery: true,
            hardExclusions,
            strategyEvidence: null
        );
        return true;
    }

    private static NetherEventOption SelectRecoveryTieBreak(
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings,
        NetherEventOption rest,
        NetherEventOption purification
    )
    {
        bool belowHpSoftLimit = snapshot.Characters.Any(character =>
            character.IsActive
            && character.HpPermille < settings.MinimumCharacterHpPermille);
        if (belowHpSoftLimit)
            return rest;
        if (snapshot.ErosionPoint > 0)
            return purification;
        return new[] { rest, purification }
            .OrderBy(option => option.OptionNumber)
            .ThenBy(option => option.EventId)
            .ThenBy(option => option.EventPartId)
            .First();
    }

    private NetherEventDecision Decide(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        bool isRecovery,
        NetherCodeTransformHardExclusionEvidence hardExclusions,
        NetherEventStrategyEvidence? strategyEvidence
    )
    {
        ValidateInputs(snapshot, options, settings);
        NetherCodeTransformEligibilityEvidence transformEligibility =
            BuildTransformEligibility(
                snapshot,
                options,
                settings,
                modifiers,
                isRecovery,
                hardExclusions
            );
        var candidates = new List<EventCandidate>();
        NetherPauseReason firstRejection = NetherPauseReason.NoSafeRoute;
        string firstDetail = "no-safe-event-option";
        foreach (NetherEventOption option in options)
        {
            bool isHpPaidKeyEvent = !isRecovery && IsExactHpPaidKeyEvent(option);
            if (!isRecovery
                && option.Effects.Any(effect => effect.Kind == NetherEffectKind.Damage && effect.Amount > 0)
                && option.Effects.Any(effect => effect.Kind == NetherEffectKind.TreasureKeyGain)
                && !isHpPaidKeyEvent)
            {
                firstRejection = NetherPauseReason.NoSafeRoute;
                firstDetail = "hp-paid-key-damage-must-equal-eighty";
                continue;
            }
            bool hasAuthorizedHpPaidKeyProof = option.PartialDeathEligibility?.AllowsHpPaidEventKey == true;
            bool allowPartialActiveDeaths = isHpPaidKeyEvent
                && snapshot.TreasureKeyCount == 0
                && hasAuthorizedHpPaidKeyProof;
            if (isHpPaidKeyEvent
                && snapshot.TreasureKeyCount == 0
                && !hasAuthorizedHpPaidKeyProof)
            {
                firstRejection = NetherPauseReason.NoSafeRoute;
                firstDetail = "hp-paid-key-objective-proof-unavailable";
                continue;
            }
            if (!TryValidateOption(
                    option,
                    snapshot,
                    settings,
                    modifiers,
                    allowPartialActiveDeaths,
                    transformEligibility,
                    strategyEvidence,
                    out EventCandidate candidate,
                    out NetherEventDecision rejection
                ))
            {
                if (firstRejection == NetherPauseReason.NoSafeRoute)
                {
                    firstRejection = rejection.PauseReason;
                    firstDetail = rejection.Detail;
                }
                continue;
            }

            if (isRecovery && !candidate.HasPositiveOrNeutralRecoveryEffect)
            {
                firstRejection = NetherPauseReason.NoSafeRoute;
                firstDetail = "no-positive-recovery-effect";
                continue;
            }
            candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            return Pause(firstRejection, firstDetail);

        bool belowHpSoftLimit = snapshot.Characters.Any(character => character.IsActive && character.HpPermille < settings.MinimumCharacterHpPermille);
        EventCandidate selected = candidates
            // A transform candidate can exist only after the exact Recovery option set proved both
            // deterministic alternatives have zero clipped value.  Rank that committed recovery
            // action before their raw (but already saturated) effect amounts.
            .OrderByDescending(candidate => candidate.ReplacementCodeId > 0)
            .ThenByDescending(candidate => strategyEvidence?.IsKnown == true
                ? candidate.SemanticPriority
                : belowHpSoftLimit && candidate.HpDelta > 0 ? 1 : 0)
            .ThenBy(candidate => strategyEvidence?.IsKnown == true ? 0 : candidate.ErosionDelta)
            .ThenByDescending(candidate => strategyEvidence?.IsKnown == true ? 0 : candidate.HpDelta)
            .ThenByDescending(candidate => candidate.SafeCodeBenefit)
            .ThenByDescending(candidate => candidate.Benefit)
            .ThenBy(candidate => candidate.OptionalBattle)
            .ThenBy(candidate => candidate.Option.OptionNumber)
            .ThenBy(candidate => candidate.Option.EventId)
            .ThenBy(candidate => candidate.Option.EventPartId)
            .ThenBy(candidate => candidate.Option.FloorId)
            .ThenBy(candidate => candidate.Option.NodeId)
            .First();
        return Select(selected);
    }

    private bool TryValidateOption(
        NetherEventOption option,
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        bool allowPartialActiveDeaths,
        NetherCodeTransformEligibilityEvidence transformEligibility,
        NetherEventStrategyEvidence? strategyEvidence,
        out EventCandidate candidate,
        out NetherEventDecision rejection
    )
    {
        candidate = default;
        rejection = default!;
        long replacementCodeId = 0;
        if (option == null || option.OptionNumber < 1 || option.Effects == null || option.Effects.Count is < 1 or > 4)
        {
            rejection = Pause(NetherPauseReason.UnknownEffect, "invalid-event-option");
            return false;
        }
        if (!string.IsNullOrWhiteSpace(option.UnknownReason))
        {
            rejection = Pause(NetherPauseReason.UnknownMasterData, option.UnknownReason);
            return false;
        }
        if (option.RequiresExactBinding
            && (option.EventId <= 0
                || option.EventPartId <= 0
                || option.FloorId <= 0
                || option.NodeId <= 0))
        {
            rejection = Pause(
                NetherPauseReason.BindingUnavailable,
                "event-binding-unavailable:" + option.OptionNumber
            );
            return false;
        }
        NetherEventStrategyEvidence? optionStrategyEvidence = option.StrategyEvidence ?? strategyEvidence;
        if (option.RequiresExactBinding
            && settings.StrategyMode == NetherStrategyMode.Research
            && (optionStrategyEvidence == null
                || !optionStrategyEvidence.IsUsableFor(NetherStrategyMode.Research)))
        {
            rejection = Pause(
                NetherPauseReason.BindingUnavailable,
                "event-option-strategy-evidence-unavailable:" + option.OptionNumber
            );
            return false;
        }
        if (option.HasRouteSafetyEvidence && !option.RouteSafetyAllowed)
        {
            rejection = Pause(
                NetherPauseReason.UnsafeErosion,
                string.IsNullOrWhiteSpace(option.RouteSafetyUnknownReason)
                    ? "event-route-safety-rejected"
                    : option.RouteSafetyUnknownReason
            );
            return false;
        }
        if (option.Effects.Any(effect => !effect.Known || !effect.ContentKnown || effect.Kind == NetherEffectKind.Unknown || effect.Amount < 0))
        {
            rejection = Pause(NetherPauseReason.UnknownEffect, "unknown-event-effect");
            return false;
        }
        if (option.RequiresExactBinding
            && option.Effects.Any(effect => effect.Kind == NetherEffectKind.Item && effect.ContentId <= 0))
        {
            rejection = Pause(NetherPauseReason.UnknownMasterData, "event-item-content-binding-unavailable");
            return false;
        }
        NetherEffect? battleEffect = option.Effects.FirstOrDefault(effect => effect.Kind == NetherEffectKind.Battle);
        NetherEventBattleEvidence? battleEvidence = option.BattleEvidence ?? battleEffect?.BattleEvidence;
        if (battleEffect != null && option.RequiresExactBinding
            && (battleEvidence == null || !battleEvidence.IsKnown))
        {
            rejection = Pause(
                NetherPauseReason.UnknownMasterData,
                battleEvidence?.UnknownReason ?? "event-battle-row-unavailable"
            );
            return false;
        }
        if (battleEvidence != null && !battleEvidence.IsKnown)
        {
            rejection = Pause(
                NetherPauseReason.UnknownMasterData,
                string.IsNullOrWhiteSpace(battleEvidence.UnknownReason)
                    ? "event-battle-row-unavailable"
                    : battleEvidence.UnknownReason
            );
            return false;
        }
        NetherEventRewardEvidence? rewardEvidence = option.RewardEvidence
            ?? option.Effects.Select(effect => effect.RewardEvidence).FirstOrDefault(reward => reward != null);
        if (option.RequiresExactBinding
            && option.Effects.Any(effect => effect.Kind == NetherEffectKind.Item)
            && (rewardEvidence == null || !rewardEvidence.IsKnown))
        {
            rejection = Pause(
                NetherPauseReason.UnknownMasterData,
                rewardEvidence == null || rewardEvidence.UnknownReason.Length == 0
                    ? "event-reward-row-unavailable"
                    : rewardEvidence.UnknownReason
            );
            return false;
        }
        if (option.Effects.Count(effect => effect.Kind == NetherEffectKind.AbyssCodeTransform) > 1
            || option.Effects.Count(effect => effect.Kind == NetherEffectKind.AbyssCodeOffer) > 1)
        {
            rejection = Pause(NetherPauseReason.UnknownEffect, "ambiguous-code-event-trigger");
            return false;
        }
        if (option.Effects.Any(effect => effect.Kind == NetherEffectKind.AbyssCodeTransform))
        {
            NetherCodeTransformDecision transform = _transformPolicy.Decide(
                snapshot.Codes,
                snapshot.CodeCapacity,
                transformEligibility
            );
            if (!transform.CanTransform)
            {
                rejection = Pause(transform.PauseReason, transform.Detail);
                return false;
            }
            replacementCodeId = transform.RemoveCodeId;
        }
        if (option.Effects.Any(effect => effect.Kind == NetherEffectKind.NetherGoldUsed && effect.Amount > snapshot.NetherGold)
            || option.Effects.Any(effect => effect.Kind == NetherEffectKind.TreasureKeyUsed && effect.Amount > snapshot.TreasureKeyCount))
        {
            rejection = Pause(NetherPauseReason.NoSafeRoute, "insufficient-event-resource");
            return false;
        }

        int hpDelta;
        try
        {
            hpDelta = option.Effects.Aggregate(0, (total, effect) => effect.Kind switch
            {
                NetherEffectKind.Heal => checked(total + effect.Amount),
                NetherEffectKind.Damage => checked(total - effect.Amount),
                _ => total,
            });
        }
        catch (OverflowException)
        {
            rejection = Pause(NetherPauseReason.UnknownEffect, "event-hp-overflow");
            return false;
        }
        NetherCharacterState[] activeCharacters = snapshot.Characters
            .Where(character => character.IsActive)
            .ToArray();
        bool hpIsLethal = allowPartialActiveDeaths
            ? activeCharacters.Length == 0
                || activeCharacters.All(character => character.HpPermille + hpDelta <= 0)
            : activeCharacters.Any(character => character.HpPermille + hpDelta <= 0);
        if (hpIsLethal)
        {
            rejection = Pause(
                NetherPauseReason.UnsafeHp,
                allowPartialActiveDeaths ? "party-lethal-event-damage" : "lethal-event-damage"
            );
            return false;
        }
        if (!NetherEventResourceProjection.TryProject(
                snapshot.NetherGold,
                snapshot.TreasureKeyCount,
                option.Effects,
                out int projectedGold,
                out int projectedKeys
            ))
        {
            rejection = Pause(NetherPauseReason.UnknownMasterData, "event-resource-projection-unavailable");
            return false;
        }
        int goldDelta = projectedGold - snapshot.NetherGold;
        int keyDelta = projectedKeys - snapshot.TreasureKeyCount;
        if (projectedGold < option.CommittedGoldMinimum
            || projectedKeys < option.CommittedKeyMinimum)
        {
            rejection = Pause(NetherPauseReason.NoSafeRoute, "event-committed-budget-would-break");
            return false;
        }
        bool crossesCommittedProcurementThreshold = CrossesCommittedProcurementThreshold(
            option,
            snapshot.NetherGold,
            snapshot.TreasureKeyCount,
            projectedGold,
            projectedKeys
        );

        NetherErosionProjection erosion = _erosionPolicy.ProjectEffects(
            snapshot.ErosionPoint,
            option.Effects,
            modifiers,
            settings.SoftErosionLimit,
            isMandatoryBoss: false
        );
        if (!erosion.IsAllowed)
        {
            rejection = Pause(erosion.PauseReason, erosion.Detail);
            return false;
        }

        int erosionDelta = erosion.ProjectedErosion - snapshot.ErosionPoint;
        bool startsBattle = option.Effects.Any(effect => effect.Kind == NetherEffectKind.Battle);
        bool optionalBattle = option.Effects.Any(effect => effect.Kind == NetherEffectKind.Battle && effect.IsOptionalBattle);
        int benefit = option.Effects.Count(effect => effect.Kind is NetherEffectKind.Item
            or NetherEffectKind.NetherGoldGain
            or NetherEffectKind.TreasureKeyGain
            or NetherEffectKind.AbyssCodeOffer);
        int semanticPriority = optionStrategyEvidence?.IsKnown == true
            ? ComputeSemanticPriority(option, optionStrategyEvidence, battleEvidence, rewardEvidence)
            : option.Effects.Any(effect => effect.Kind is NetherEffectKind.AbyssCodeOffer
                or NetherEffectKind.AbyssCodeTransform) ? 1 : 0;
        if (crossesCommittedProcurementThreshold)
        {
            // A committed resource objective is a proven route obligation. It outranks ordinary
            // Code/Gold semantics (600/400) while remaining below rank-five rewards and battle
            // tiers already assigned by ComputeSemanticPriority.
            semanticPriority = Math.Max(700, semanticPriority);
        }
        candidate = new EventCandidate(
            option,
            erosion.ProjectedErosion,
            erosionDelta,
            hpDelta,
            replacementCodeId,
            semanticPriority,
            benefit,
            projectedGold,
            projectedKeys,
            startsBattle,
            optionalBattle,
            allowPartialActiveDeaths,
            semanticPriority,
            battleEvidence,
            rewardEvidence
        );
        return true;
    }

    private static bool CrossesCommittedProcurementThreshold(
        NetherEventOption option,
        int currentGold,
        int currentKeys,
        int projectedGold,
        int projectedKeys
    ) => option.CommittedGoldMinimum > currentGold
        && projectedGold >= option.CommittedGoldMinimum
        || option.CommittedKeyMinimum > currentKeys
        && projectedKeys >= option.CommittedKeyMinimum;

    private NetherCodeTransformEligibilityEvidence BuildTransformEligibility(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        bool isRecovery,
        NetherCodeTransformHardExclusionEvidence hardExclusions
    )
    {
        if (!isRecovery)
        {
            return new NetherCodeTransformEligibilityEvidence
            {
                StrategyMode = settings.StrategyMode,
                EquipmentOptInEnabled = settings.EquipmentRecoveryCodeTransformEnabled,
                IsRecovery = false,
            };
        }
        if (hardExclusions == null || !hardExclusions.IsKnown)
        {
            return new NetherCodeTransformEligibilityEvidence
            {
                IsKnown = false,
                UnknownReason = hardExclusions?.UnknownReason
                    ?? "code-transform-hard-exclusions-unavailable",
            };
        }

        NetherEventOption[] transforms = options.Where(IsExactTransformOption).ToArray();
        NetherEventOption[] rests = options.Where(IsExactRestOption).ToArray();
        NetherEventOption[] purifications = options.Where(IsExactPurificationOption).ToArray();
        if (options.Count != 3 || transforms.Length != 1 || rests.Length != 1
            || purifications.Length != 1)
        {
            return new NetherCodeTransformEligibilityEvidence
            {
                IsKnown = false,
                UnknownReason = "recovery-transform-three-option-shape-unavailable",
            };
        }

        bool restHasValue = HasActualRecoveryValue(
            rests[0],
            snapshot,
            settings,
            modifiers
        );
        bool purificationHasValue = HasActualRecoveryValue(
            purifications[0],
            snapshot,
            settings,
            modifiers
        );
        return new NetherCodeTransformEligibilityEvidence
        {
            StrategyMode = settings.StrategyMode,
            EquipmentOptInEnabled = settings.EquipmentRecoveryCodeTransformEnabled,
            IsRecovery = true,
            DeterministicRecoveryChoicesHaveZeroValue = !restHasValue
                && !purificationHasValue,
            HardExcludedCodes = hardExclusions.HardExcludedCodes,
        };
    }

    private bool HasActualRecoveryValue(
        NetherEventOption option,
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers
    )
    {
        NetherEffect effect = option.Effects[0];
        bool hpValue = effect.Kind == NetherEffectKind.Heal
            && snapshot.Characters.Any(character => character.IsActive
                && character.HpPermille < 1000
                && effect.Amount > 0);
        NetherErosionProjection projection = _erosionPolicy.ProjectEffects(
            snapshot.ErosionPoint,
            option.Effects,
            modifiers,
            settings.SoftErosionLimit,
            isMandatoryBoss: false
        );
        // An ineligible or non-neutral deterministic branch is not proof of zero value.
        return hpValue || !projection.IsAllowed
            || projection.ProjectedErosion != snapshot.ErosionPoint;
    }

    private static bool IsExactTransformOption(NetherEventOption option) =>
        IsExactSingleEffect(option, NetherEffectKind.AbyssCodeTransform);

    private static bool IsExactRestOption(NetherEventOption option) =>
        IsExactSingleEffect(option, NetherEffectKind.Heal);

    private static bool IsExactPurificationOption(NetherEventOption option) =>
        IsExactSingleEffect(option, NetherEffectKind.ErosionHeal);

    private static bool IsExactSingleEffect(NetherEventOption option, NetherEffectKind kind) =>
        option != null
        && option.Effects != null
        && option.Effects.Count == 1
        && option.Effects[0] != null
        && option.Effects[0].Known
        && option.Effects[0].ContentKnown
        && option.Effects[0].Kind == kind
        && option.Effects[0].Amount >= 0;

    private static bool IsExactTreasureHpPayment(NetherEventOption option) =>
        option?.Effects != null
        && option.Effects.Count == 1
        && option.Effects[0].Kind == NetherEffectKind.Damage
        && option.Effects[0].Amount is 40 or 80;

    private static bool IsExactHpPaidKeyEvent(NetherEventOption option) =>
        option?.Effects != null
        && option.Effects.Count == 2
        && option.Effects.Count(effect => effect.Kind == NetherEffectKind.Damage && effect.Amount == 80) == 1
        && option.Effects.Count(effect => effect.Kind == NetherEffectKind.TreasureKeyGain && effect.Amount == 1) == 1;

    private static int ComputeSemanticPriority(
        NetherEventOption option,
        NetherEventStrategyEvidence? strategyEvidence,
        NetherEventBattleEvidence? battleEvidence,
        NetherEventRewardEvidence? rewardEvidence
    )
    {
        if (strategyEvidence?.IsKnown != true)
            return 0;
        if (strategyEvidence.ResearchIncomplete
            && (strategyEvidence.HasRankFiveTreasureObjective
                && option.IsMandatoryRankFiveKeyObjective))
        {
            return 1_000;
        }

        if (battleEvidence?.IsKnown == true)
        {
            int battlePriority = battleEvidence.SemanticTier switch
            {
                NetherEventBattleTier.Boss => 800,
                NetherEventBattleTier.MiniBoss => 750,
                NetherEventBattleTier.NormalBattle => strategyEvidence.ResearchIncomplete
                    ? battleEvidence.CodeDropRatio >= 1_000 ? 700 : 350
                    : 650,
                _ => 0,
            };
            if (battlePriority > 0)
                return battlePriority;
        }

        bool hasCodeOffer = option.Effects.Any(effect => effect.Kind == NetherEffectKind.AbyssCodeOffer);
        bool hasGold = option.Effects.Any(effect => effect.Kind == NetherEffectKind.NetherGoldGain);
        if (strategyEvidence.ResearchIncomplete && hasCodeOffer)
            return 600;

        if (rewardEvidence?.IsKnown == true && rewardEvidence.ItemId > 0)
        {
            if (rewardEvidence.Rarity == NetherRewardRarity.Red && rewardEvidence.ItemType == 91)
                return 950;
            if (rewardEvidence.Rarity == NetherRewardRarity.Gold && rewardEvidence.ItemType == 91)
                return 900;
            return 500 + (int)rewardEvidence.Rarity * 10;
        }
        if (hasCodeOffer)
            return 600;
        if (hasGold)
            return 400;
        return 0;
    }

    internal static bool EffectFingerprintsEqual(
        IReadOnlyList<NetherEffect>? left,
        IReadOnlyList<NetherEffect>? right
    )
    {
        if (left == null || right == null || left.Count != right.Count)
            return false;
        for (int index = 0; index < left.Count; index++)
        {
            NetherEffect a = left[index];
            NetherEffect b = right[index];
            if (a == null || b == null
                || a.Kind != b.Kind
                || a.Amount != b.Amount
                || a.Known != b.Known
                || a.ContentKnown != b.ContentKnown
                || a.RatePermille != b.RatePermille
                || a.ContentId != b.ContentId
                || a.ReplacementCodeId != b.ReplacementCodeId
                || a.IsOptionalBattle != b.IsOptionalBattle
                || a.BattleEvidence?.BattleId != b.BattleEvidence?.BattleId
                || a.BattleEvidence?.BattleStageId != b.BattleEvidence?.BattleStageId
                || a.BattleEvidence?.BattleType != b.BattleEvidence?.BattleType
                || a.BattleEvidence?.CodeDropRatio != b.BattleEvidence?.CodeDropRatio
                || a.BattleEvidence?.SemanticTier != b.BattleEvidence?.SemanticTier
                || a.BattleEvidence?.UnknownReason != b.BattleEvidence?.UnknownReason
                || a.RewardEvidence?.ContentId != b.RewardEvidence?.ContentId
                || a.RewardEvidence?.ItemId != b.RewardEvidence?.ItemId
                || a.RewardEvidence?.ItemType != b.RewardEvidence?.ItemType
                || a.RewardEvidence?.Rarity != b.RewardEvidence?.Rarity
                || a.RewardEvidence?.Amount != b.RewardEvidence?.Amount
                || a.RewardEvidence?.UnknownReason != b.RewardEvidence?.UnknownReason)
            {
                return false;
            }
        }
        return true;
    }

    private static NetherEventDecision Select(EventCandidate candidate) => new()
    {
        Kind = NetherEventDecisionKind.Select,
        ActionKind = NetherActionKind.SelectEventOption,
        OptionNumber = candidate.Option.OptionNumber,
        ReplacementCodeId = candidate.ReplacementCodeId,
        ProjectedErosion = candidate.ProjectedErosion,
        ExpectedErosionDelta = candidate.ErosionDelta,
        HpDelta = candidate.HpDelta,
        ProjectedNetherGold = candidate.ProjectedNetherGold,
        ProjectedTreasureKeys = candidate.ProjectedTreasureKeys,
        CommittedGoldMinimum = candidate.Option.CommittedGoldMinimum,
        CommittedKeyMinimum = candidate.Option.CommittedKeyMinimum,
        ExpectedEffects = candidate.Option.Effects.ToArray(),
        StartsBattleAfterSelection = candidate.StartsBattle,
        AllowsPartialActiveDeaths = candidate.AllowsPartialActiveDeaths,
        EventId = candidate.Option.EventId,
        EventPartId = candidate.Option.EventPartId,
        FloorId = candidate.Option.FloorId,
        NodeId = candidate.Option.NodeId,
        PartialDeathEligibility = candidate.Option.PartialDeathEligibility,
        RankFiveKeyProcurementCommitment = candidate.Option.RankFiveKeyProcurementCommitment,
        RankFiveTreasureObjective = candidate.Option.RankFiveTreasureObjective,
        Commitment = candidate.Option.RequiresExactBinding
            ? new NetherEventCommitment(
                candidate.Option.EventId,
                candidate.Option.EventPartId,
                candidate.Option.OptionNumber,
                candidate.Option.Effects.ToArray(),
                candidate.ProjectedErosion,
                candidate.HpDelta
            )
            {
                FloorId = candidate.Option.FloorId,
                NodeId = candidate.Option.NodeId,
                Battle = candidate.Battle,
                Reward = candidate.Reward,
                ProjectedNetherGold = candidate.ProjectedNetherGold,
                ProjectedTreasureKeys = candidate.ProjectedTreasureKeys,
                CommittedGoldMinimum = candidate.Option.CommittedGoldMinimum,
                CommittedKeyMinimum = candidate.Option.CommittedKeyMinimum,
                PartialDeathEligibility = candidate.Option.PartialDeathEligibility,
                AllowsPartialActiveDeaths = candidate.AllowsPartialActiveDeaths,
                RankFiveKeyProcurementCommitment = candidate.Option.RankFiveKeyProcurementCommitment,
                RankFiveTreasureObjective = candidate.Option.RankFiveTreasureObjective,
            }
            : null,
        Battle = candidate.Battle,
        Reward = candidate.Reward,
    };

    private static NetherEventDecision Pause(NetherPauseReason reason, string detail) => new()
    {
        Kind = NetherEventDecisionKind.Pause,
        PauseReason = reason,
        Detail = detail,
    };

    private static void ValidateInputs(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings
    )
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
    }

    private readonly record struct EventCandidate(
        NetherEventOption Option,
        int ProjectedErosion,
        int ErosionDelta,
        int HpDelta,
        long ReplacementCodeId,
        int SafeCodeBenefit,
        int Benefit,
        int ProjectedNetherGold,
        int ProjectedTreasureKeys,
        bool StartsBattle,
        bool OptionalBattle,
        bool AllowsPartialActiveDeaths,
        int SemanticPriority,
        NetherEventBattleEvidence? Battle = null,
        NetherEventRewardEvidence? Reward = null
    )
    {
        // Recovery must never select damage/erosion, but an otherwise neutral native option
        // is a valid safe fallback.  Requiring a positive reward here can deadlock the only
        // harmless recovery popup even though it has no projected downside.
        public bool HasPositiveOrNeutralRecoveryEffect => ErosionDelta <= 0 && HpDelta >= 0;
    }
}
