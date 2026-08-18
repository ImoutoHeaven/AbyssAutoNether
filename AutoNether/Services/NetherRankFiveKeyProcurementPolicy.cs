#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal enum NetherKeyProcurementSourceKind
{
    None = 0,
    EventGold150,
    ShopGold200,
    HpPaidEventKey,
    ErosionPaidEventKey,
}

/// <summary>
/// Exact EventPart effect predicates derived from the native two-effect payment/key shape.
/// Unknown, extra, or non-exact effects are not procurement commitments.
/// </summary>
internal static class NetherRankFiveKeyProcurementPredicates
{
    internal static bool IsExactEventGold150KeyOption(
        NetherStrategyVisibleEventOptionEvidence? option
    ) => IsExactPaymentAndKeyOption(option, NetherEffectKind.NetherGoldUsed, 150);

    internal static bool IsExactHpPaidEventKeyOption(
        NetherStrategyVisibleEventOptionEvidence? option
    ) => IsExactPaymentAndKeyOption(option, NetherEffectKind.Damage, 80);

    internal static bool IsExactErosionPaidEventKeyOption(
        NetherStrategyVisibleEventOptionEvidence? option
    ) => IsExactPaymentAndKeyOption(option, NetherEffectKind.Erosion, 80);

    internal static bool IsExactPaymentAndKeyOption(
        NetherStrategyVisibleEventOptionEvidence? option,
        NetherEffectKind paymentKind,
        int paymentAmount
    ) => option != null
        && option.Effects != null
        && option.Effects.Count(effect => effect.IsPresent) == 2
        && option.Effects.All(effect => !effect.IsPresent || effect.IsKnown)
        && option.Effects.Count(effect => effect.IsPresent
            && effect.EffectKind == paymentKind
            && effect.Amount == paymentAmount) == 1
        && option.Effects.Count(effect => effect.IsPresent
            && effect.EffectKind == NetherEffectKind.TreasureKeyGain
            && effect.Amount == 1) == 1;
}

/// <summary>
/// Route-value adapter for a typed canonical reward-tier provider. It validates the exact visible
/// row identity but never derives rank-five or colour from native MItems type/rarity fields.
/// </summary>
internal static class NetherCanonicalRewardTierProvider
{
    internal static bool TryGetTypedRewardEvidence(
        NetherStrategyVisibleContentRow? row,
        out NetherEventRewardEvidence evidence
    )
    {
        evidence = null!;
        if (row == null
            || row.Kind != NetherStrategyVisibleContentKind.Item
            || !row.IsKnown
            || row.ContentId <= 0
            || row.MasterRowId <= 0
            || row.ItemType != 91
            || row.Amount <= 0)
        {
            return false;
        }
        NetherRewardRarity expectedRarity = row.CanonicalRewardTier switch
        {
            NetherCanonicalRewardTier.GoldRankFive => NetherRewardRarity.Gold,
            NetherCanonicalRewardTier.RedRankFive => NetherRewardRarity.Red,
            NetherCanonicalRewardTier.UncolouredRankFive => NetherRewardRarity.UniqueWeapon,
            _ => NetherRewardRarity.NoEffect,
        };
        if (expectedRarity == NetherRewardRarity.NoEffect || row.ItemRarity != (int)expectedRarity)
            return false;
        evidence = new NetherEventRewardEvidence(
            row.ContentId,
            row.MasterRowId,
            (int)row.ItemType,
            expectedRarity,
            row.Amount
        );
        return evidence.IsKnown;
    }

    internal static bool IsCanonicalGoldRankFiveShopContent(NetherShopContent content) =>
        content.Known
        && content.ContentId > 0
        && content.ItemId > 0
        && content.ItemType == 91
        && content.Amount > 0
        && content.UsesNetherGold
        && content.Price == 300
        && content.CanonicalRewardTier == NetherCanonicalRewardTier.GoldRankFive;

    internal static bool IsCanonicalGoldRankFiveShopRow(NetherStrategyVisibleContentRow? row) =>
        row != null
        && row.Kind == NetherStrategyVisibleContentKind.ShopInventory
        && row.IsKnown
        && row.MasterRowId > 0
        && row.ContentId > 0
        && row.ItemType == 91
        && row.Amount > 0
        && row.Cost == 300
        && row.UsesNetherGold
        && row.CanonicalRewardTier == NetherCanonicalRewardTier.GoldRankFive;

    internal static bool IsAuthoritativeShopKeyRow(NetherStrategyVisibleContentRow? row) =>
        row != null
        && row.Kind == NetherStrategyVisibleContentKind.ShopInventory
        && row.IsKnown
        && row.IsTreasureKey
        && row.ShopKeyIdentity > 0
        && row.ContentId > 0
        && row.ContentType == 166
        && row.ItemType >= 0
        && row.Amount > 0
        && row.Cost >= 0
        && row.UsesNetherGold;

    internal static bool TryGetRankFiveTier(
        NetherStrategyVisibleContentRow? row,
        long treasureNodeId,
        long treasureEventId,
        out NetherCanonicalRewardTier tier
    )
    {
        tier = NetherCanonicalRewardTier.Unknown;
        if (row == null
            || row.Kind != NetherStrategyVisibleContentKind.Item
            || !row.IsKnown
            || row.NodeId != treasureNodeId
            || row.EventId != treasureEventId
            || row.EventPartId <= 0
            || row.ContentId <= 0
            || row.ItemType != 91
            || row.Amount <= 0)
        {
            return false;
        }
        if (row.CanonicalRewardTier is not (
                NetherCanonicalRewardTier.GoldRankFive
                or NetherCanonicalRewardTier.RedRankFive
                or NetherCanonicalRewardTier.UncolouredRankFive
            ))
        {
            return false;
        }
        tier = row.CanonicalRewardTier;
        return true;
    }
}

internal readonly record struct NetherRankFiveTreasureIdentity(
    long ObjectiveNodeId,
    long ObjectiveEventId,
    long ObjectiveEventPartId
)
{
    public bool IsValid => ObjectiveNodeId > 0 && ObjectiveEventId > 0 && ObjectiveEventPartId > 0;
}

internal sealed record NetherRankFiveKeyProcurementCommitment
{
    public NetherRankFiveTreasureIdentity Objective { get; init; }
    public NetherKeyProcurementSourceKind SourceKind { get; init; }
    public long SourceNodeId { get; init; }
    public long SourceEventId { get; init; }
    public long SourceEventPartId { get; init; }
    public int SourceOptionNumber { get; init; }
    public long SourceContentId { get; init; }
    public int GoldCost { get; init; }

    public bool IsValid => Objective.IsValid
        && SourceKind != NetherKeyProcurementSourceKind.None
        && SourceNodeId > 0
        && (SourceKind is NetherKeyProcurementSourceKind.EventGold150
            or NetherKeyProcurementSourceKind.HpPaidEventKey
            or NetherKeyProcurementSourceKind.ErosionPaidEventKey
                ? SourceEventId > 0 && SourceEventPartId > 0 && SourceOptionNumber > 0
                : SourceKind == NetherKeyProcurementSourceKind.ShopGold200
                    && SourceContentId > 0 && GoldCost == 200);
}

internal sealed record NetherRankFiveKeyProcurementInput(
    int CurrentNetherGold,
    int CurrentTreasureKeys,
    IReadOnlyList<int> ActiveHpPermille,
    IReadOnlyList<long> SelectedPathNodeIds,
    IReadOnlySet<long> HardSafeNodeIds,
    IReadOnlyList<NetherFloorNode> Floors,
    IReadOnlyList<NetherStrategyVisibleContentRow> ContentRows
)
{
    /// <summary>
    /// The selected route includes the already-entered current node so that source and objective
    /// identities retain their exact path positions. That node is context, not a future action;
    /// it may therefore be outside the selected future-node hard-safety proof.
    /// </summary>
    public long AlreadyEnteredNodeId { get; init; }

    /// <summary>
    /// Maximum exact battle-entry erosion on the selected visible branch. Unknown is represented
    /// by null and never treated as zero for an erosion-paid key.
    /// </summary>
    public int? MaximumBattleEntryErosionPoint { get; init; }

    /// <summary>Fresh route evidence that recovery to 70 or below is certain before the next battle.</summary>
    public bool RecoveryToSeventyOrBelowCertainBeforeNextBattle { get; init; }
}

internal sealed record NetherRankFiveKeyProcurementDecision
{
    public bool IsKnown { get; init; }
    public bool HasMandatoryObjective { get; init; }
    public NetherKeyProcurementSourceKind SourceKind { get; init; }
    public int GoldCost { get; init; }
    public bool AllowsHpFallback { get; init; }
    public bool AllowsPartialPartyDeath { get; init; }
    public bool ErosionAmountIsExactEighty { get; init; }
    public NetherRankFiveTreasureIdentity Objective { get; init; }
    public NetherRankFiveKeyProcurementCommitment? Commitment { get; init; }
    public string Detail { get; init; } = string.Empty;

    public static NetherRankFiveKeyProcurementDecision Unknown(string detail) => new()
    {
        Detail = string.IsNullOrWhiteSpace(detail) ? "rank-five-procurement-unknown" : detail,
    };
}

/// <summary>
/// Evaluates only exact, selected-branch evidence for the rank-five Treasure key objective. It
/// does not search hidden floors or invent a key semantic from a raw shop content type.
/// </summary>
internal sealed class NetherRankFiveKeyProcurementPolicy
{
    public NetherRankFiveKeyProcurementDecision Evaluate(NetherRankFiveKeyProcurementInput input)
    {
        if (input == null
            || input.CurrentNetherGold < 0
            || input.CurrentTreasureKeys < 0
            || input.ActiveHpPermille == null
            || input.ActiveHpPermille.Count == 0
            || input.ActiveHpPermille.Any(value => value is < 0 or > 1000)
            || input.SelectedPathNodeIds == null
            || input.HardSafeNodeIds == null
            || input.Floors == null
            || input.ContentRows == null)
        {
            return NetherRankFiveKeyProcurementDecision.Unknown("rank-five-procurement-input-invalid");
        }
        if (input.CurrentTreasureKeys > 0)
        {
            return new NetherRankFiveKeyProcurementDecision
            {
                IsKnown = true,
                Detail = "held-treasure-key-satisfies-objective",
            };
        }
        if (!HasCompleteSafeBossPath(input))
            return NetherRankFiveKeyProcurementDecision.Unknown("rank-five-objective-path-unavailable");

        Dictionary<long, NetherFloorNode> floors = input.Floors
            .Where(floor => floor != null && floor.NodeId > 0)
            .GroupBy(floor => floor.NodeId)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single());
        int objectiveIndex = -1;
        NetherStrategyVisibleContentRow? objectiveTreasure = null;
        NetherRankFiveTreasureIdentity objectiveIdentity = default;
        foreach ((NetherStrategyVisibleContentRow treasure, NetherRankFiveTreasureIdentity identity)
            in FindRankFiveTreasureObjectives(input.ContentRows))
        {
            int index = NetherPathIndexUtility.PathIndexOf(input.SelectedPathNodeIds, treasure.NodeId);
            if (index <= 0)
                continue;
            if (objectiveTreasure != null)
                return NetherRankFiveKeyProcurementDecision.Unknown("ambiguous-rank-five-treasure-objective");
            objectiveTreasure = treasure;
            objectiveIndex = index;
            objectiveIdentity = identity;
        }

        if (objectiveTreasure == null
            || objectiveIndex <= 0
            || !objectiveIdentity.IsValid
            || !floors.TryGetValue(objectiveTreasure.NodeId, out NetherFloorNode? objectiveFloor)
            || objectiveFloor == null)
        {
            return new NetherRankFiveKeyProcurementDecision
            {
                IsKnown = true,
                Detail = "no-known-rank-five-treasure-on-selected-branch",
            };
        }

        NetherStrategyVisibleContentRow[] branchRows = input.ContentRows
            .Where(row => row != null
                && row.IsKnown
                && NetherPathIndexUtility.PathIndexOf(input.SelectedPathNodeIds, row.NodeId) > 0
                && NetherPathIndexUtility.PathIndexOf(input.SelectedPathNodeIds, row.NodeId) < objectiveIndex)
            .ToArray();
        bool hasEvent150 = TryFindExactEventSource(
            branchRows,
            NetherRankFiveKeyProcurementPredicates.IsExactEventGold150KeyOption,
            out NetherStrategyVisibleContentRow? event150Row,
            out NetherStrategyVisibleEventOptionEvidence? event150Option
        );
        bool hasShop200 = TryFindExactShopSource(
            branchRows,
            out NetherStrategyVisibleContentRow? shop200Row
        );

        if (hasEvent150 && input.CurrentNetherGold >= 150)
        {
            return CurrencyDecision(
                objectiveIdentity,
                NetherKeyProcurementSourceKind.EventGold150,
                150,
                event150Row!,
                event150Option!
            );
        }
        if (hasShop200 && input.CurrentNetherGold >= 200)
        {
            return CurrencyDecision(
                objectiveIdentity,
                NetherKeyProcurementSourceKind.ShopGold200,
                200,
                shop200Row!,
                sourceOption: null
            );
        }

        bool hasHpKey = TryFindExactEventSource(
            branchRows,
            NetherRankFiveKeyProcurementPredicates.IsExactHpPaidEventKeyOption,
            out NetherStrategyVisibleContentRow? hpKeyRow,
            out NetherStrategyVisibleEventOptionEvidence? hpKeyOption
        );
        if (hasHpKey && input.ActiveHpPermille.Any(hp => hp > 80))
        {
            return new NetherRankFiveKeyProcurementDecision
            {
                IsKnown = true,
                HasMandatoryObjective = true,
                Objective = objectiveIdentity,
                SourceKind = NetherKeyProcurementSourceKind.HpPaidEventKey,
                AllowsPartialPartyDeath = true,
                AllowsHpFallback = false,
                Commitment = EventCommitment(
                    objectiveIdentity,
                    NetherKeyProcurementSourceKind.HpPaidEventKey,
                    hpKeyRow!,
                    hpKeyOption!,
                    goldCost: 0
                ),
                Detail = "exact-80-hp-event-key-group-survival",
            };
        }

        bool hasErosionKey = TryFindExactEventSource(
            branchRows,
            NetherRankFiveKeyProcurementPredicates.IsExactErosionPaidEventKeyOption,
            out NetherStrategyVisibleContentRow? erosionKeyRow,
            out NetherStrategyVisibleEventOptionEvidence? erosionKeyOption
        );
        bool erosionGate = input.MaximumBattleEntryErosionPoint.HasValue
            && input.MaximumBattleEntryErosionPoint.Value <= 70
            && input.RecoveryToSeventyOrBelowCertainBeforeNextBattle;
        if (hasErosionKey && erosionGate)
        {
            return new NetherRankFiveKeyProcurementDecision
            {
                IsKnown = true,
                HasMandatoryObjective = true,
                Objective = objectiveIdentity,
                SourceKind = NetherKeyProcurementSourceKind.ErosionPaidEventKey,
                ErosionAmountIsExactEighty = true,
                Commitment = EventCommitment(
                    objectiveIdentity,
                    NetherKeyProcurementSourceKind.ErosionPaidEventKey,
                    erosionKeyRow!,
                    erosionKeyOption!,
                    goldCost: 0
                ),
                Detail = "exact-80-erosion-event-key-with-recovery-gate",
            };
        }

        // The objective is still legal through the approved Treasure HP payment. The caller must
        // attach the exact rank-five partial-death proof to that Treasure option before mutation.
        return new NetherRankFiveKeyProcurementDecision
        {
            IsKnown = true,
            HasMandatoryObjective = true,
            Objective = objectiveIdentity,
            SourceKind = NetherKeyProcurementSourceKind.None,
            AllowsHpFallback = true,
            Detail = "no-affordable-exact-key-source-use-treasure-hp-fallback",
        };
    }

    public static IReadOnlySet<long> FindKnownObjectiveNodes(
        int currentTreasureKeys,
        NetherStrategyVisibleMapEvidence? visibleMap
    )
    {
        if (currentTreasureKeys > 0 || visibleMap?.ContentRows == null)
            return new HashSet<long>();
        var objectives = new HashSet<long>();
        foreach ((NetherStrategyVisibleContentRow treasure, NetherRankFiveTreasureIdentity _) in
            FindRankFiveTreasureObjectives(visibleMap.ContentRows))
        {
            objectives.Add(treasure.NodeId);
        }
        return objectives;
    }

    private static NetherRankFiveKeyProcurementDecision CurrencyDecision(
        NetherRankFiveTreasureIdentity objective,
        NetherKeyProcurementSourceKind sourceKind,
        int goldCost,
        NetherStrategyVisibleContentRow sourceRow,
        NetherStrategyVisibleEventOptionEvidence? sourceOption
    ) => new()
    {
        IsKnown = true,
        HasMandatoryObjective = true,
        Objective = objective,
        SourceKind = sourceKind,
        GoldCost = goldCost,
        Commitment = sourceKind == NetherKeyProcurementSourceKind.ShopGold200
            ? new NetherRankFiveKeyProcurementCommitment
            {
                Objective = objective,
                SourceKind = sourceKind,
                SourceNodeId = sourceRow.NodeId,
                SourceContentId = sourceRow.ContentId,
                GoldCost = goldCost,
            }
            : EventCommitment(objective, sourceKind, sourceRow, sourceOption!, goldCost),
        Detail = "exact-visible-branch-currency-key-source",
    };

    private static NetherRankFiveKeyProcurementCommitment EventCommitment(
        NetherRankFiveTreasureIdentity objective,
        NetherKeyProcurementSourceKind sourceKind,
        NetherStrategyVisibleContentRow row,
        NetherStrategyVisibleEventOptionEvidence option,
        int goldCost
    ) => new()
    {
        Objective = objective,
        SourceKind = sourceKind,
        SourceNodeId = row.NodeId,
        SourceEventId = row.EventId,
        SourceEventPartId = option.EventPartId > 0 ? option.EventPartId : row.EventPartId,
        SourceOptionNumber = option.OptionNumber,
        GoldCost = goldCost,
    };

    internal static bool TryFindRankFiveTreasureIdentity(
        IReadOnlyList<NetherStrategyVisibleContentRow>? rows,
        NetherStrategyVisibleContentRow treasure,
        out NetherRankFiveTreasureIdentity identity
    )
    {
        identity = default;
        if (rows == null || treasure == null || !treasure.IsKnown
            || treasure.Kind != NetherStrategyVisibleContentKind.Treasure
            || treasure.NodeId <= 0 || treasure.EventId <= 0)
            return false;
        NetherStrategyVisibleContentRow[] rewards = rows
            .Where(row => IsRankFiveTreasureReward(row, treasure.NodeId, treasure.EventId))
            .ToArray();
        if (rewards.Length != 1)
            return false;
        identity = new NetherRankFiveTreasureIdentity(
            treasure.NodeId,
            treasure.EventId,
            rewards[0].EventPartId
        );
        return identity.IsValid;
    }

    private static IEnumerable<(NetherStrategyVisibleContentRow Row, NetherRankFiveTreasureIdentity Identity)>
        FindRankFiveTreasureObjectives(IReadOnlyList<NetherStrategyVisibleContentRow>? rows)
    {
        foreach (NetherStrategyVisibleContentRow treasure in rows ?? Array.Empty<NetherStrategyVisibleContentRow>())
        {
            if (treasure == null
                || treasure.Kind != NetherStrategyVisibleContentKind.Treasure
                || !treasure.IsKnown
                || treasure.NodeId <= 0
                || !TryFindRankFiveTreasureIdentity(rows, treasure, out NetherRankFiveTreasureIdentity identity))
                continue;
            yield return (treasure, identity);
        }
    }

    /// <summary>
    /// One shared native-visible reward predicate used by both objective discovery and identity
    /// construction. Raw UniqueWeapon rarity is not semantic proof; only the canonical typed
    /// provider may mark this exact row as rank five.
    /// </summary>
    internal static bool IsRankFiveTreasureReward(
        NetherStrategyVisibleContentRow? row,
        long treasureNodeId,
        long treasureEventId
    ) => NetherCanonicalRewardTierProvider.TryGetRankFiveTier(
        row,
        treasureNodeId,
        treasureEventId,
        out _
    );

    private static bool TryFindExactEventSource(
        IReadOnlyList<NetherStrategyVisibleContentRow> rows,
        Func<NetherStrategyVisibleEventOptionEvidence, bool> optionPredicate,
        out NetherStrategyVisibleContentRow? sourceRow,
        out NetherStrategyVisibleEventOptionEvidence? sourceOption
    )
    {
        var matches = new List<(NetherStrategyVisibleContentRow Row, NetherStrategyVisibleEventOptionEvidence Option)>();
        foreach (NetherStrategyVisibleContentRow row in rows ?? Array.Empty<NetherStrategyVisibleContentRow>())
        {
            if (row == null || row.Kind != NetherStrategyVisibleContentKind.Event || !row.IsKnown)
                continue;
            foreach (NetherStrategyVisibleEventOptionEvidence option in row.EventOptions ?? Array.Empty<NetherStrategyVisibleEventOptionEvidence>())
            {
                if (option != null && optionPredicate(option))
                    matches.Add((row, option));
            }
        }
        if (matches.Count != 1)
        {
            sourceRow = null;
            sourceOption = null;
            return false;
        }
        sourceRow = matches[0].Row;
        sourceOption = matches[0].Option;
        return true;
    }

    private static bool TryFindExactShopSource(
        IReadOnlyList<NetherStrategyVisibleContentRow> rows,
        out NetherStrategyVisibleContentRow? sourceRow
    )
    {
        NetherStrategyVisibleContentRow[] matches = (rows ?? Array.Empty<NetherStrategyVisibleContentRow>())
            .Where(row => row != null
                && row.Kind == NetherStrategyVisibleContentKind.ShopInventory
                && row.IsKnown
                && NetherCanonicalRewardTierProvider.IsAuthoritativeShopKeyRow(row)
                && row.UsesNetherGold
                && row.Cost == 200
                && row.ContentId > 0
                && row.Amount > 0)
            .ToArray();
        sourceRow = matches.Length == 1 ? matches[0] : null;
        return sourceRow != null;
    }

    private static bool HasCompleteSafeBossPath(NetherRankFiveKeyProcurementInput input)
    {
        if (input.SelectedPathNodeIds.Count < 2
            || input.SelectedPathNodeIds.Distinct().Count() != input.SelectedPathNodeIds.Count)
        {
            return false;
        }
        for (int index = 0; index < input.SelectedPathNodeIds.Count; index++)
        {
            long nodeId = input.SelectedPathNodeIds[index];
            if (nodeId <= 0
                || (index != 0 || nodeId != input.AlreadyEnteredNodeId)
                    && !input.HardSafeNodeIds.Contains(nodeId))
            {
                return false;
            }
        }
        Dictionary<long, NetherFloorNode> floors = input.Floors
            .Where(floor => floor != null && floor.NodeId > 0)
            .GroupBy(floor => floor.NodeId)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single());
        return floors.TryGetValue(input.SelectedPathNodeIds[^1], out NetherFloorNode? terminal)
            && terminal.NodeType == NetherFloorNodeType.Boss;
    }

}
