#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// The non-safety part of one complete visible branch. Each member is a count in a semantic tier;
/// comparison is lexicographic over those counts, never a weighted scalar score.
/// </summary>
internal sealed record NetherRouteEncounterVector
{
    public bool IsKnown { get; init; } = true;
    public string UnknownReason { get; init; } = string.Empty;
    public NetherStrategyUnknownReasonCode UnknownReasonCode { get; init; }
    public int ImmediateTerminalBossCount { get; init; }
    public int RedRankFiveTreasureCount { get; init; }
    public int GoldRankFiveTreasureCount { get; init; }
    public int UncolouredRankFiveTreasureCount { get; init; }
    public int EligibleLateShopCount { get; init; }
    public int EventBossCount { get; init; }
    public int EliteCount { get; init; }
    public int NormalBattleCount { get; init; }
    public int DirectCodeOfferCount { get; init; }
    public int OrdinaryEventRewardCount { get; init; }
    public int RecoveryCount { get; init; }
    public int IneligibleShopCount { get; init; }
    public int OtherTreasureCount { get; init; }

    /// <summary>
    /// Returns the first non-empty semantic tier in the same order used by CompareTo. This is an
    /// audit projection only; the vector counts remain the authoritative comparison input.
    /// </summary>
    public NetherRouteSemanticTier HighestSemanticTier(bool researchIncomplete)
    {
        if (ImmediateTerminalBossCount > 0)
            return NetherRouteSemanticTier.ImmediateTerminalBoss;
        if (RedRankFiveTreasureCount > 0)
            return NetherRouteSemanticTier.RedRankFiveTreasure;
        if (GoldRankFiveTreasureCount > 0 && EligibleLateShopCount > 0)
            return NetherRouteSemanticTier.GoldObjective;
        if (GoldRankFiveTreasureCount > 0)
            return NetherRouteSemanticTier.GoldRankFiveTreasure;
        if (EligibleLateShopCount > 0)
            return NetherRouteSemanticTier.GoldObjective;
        if (UncolouredRankFiveTreasureCount > 0)
            return NetherRouteSemanticTier.UncolouredRankFiveTreasure;
        if (EventBossCount > 0)
            return NetherRouteSemanticTier.EventBoss;
        if (EliteCount > 0)
            return NetherRouteSemanticTier.Elite;
        if (researchIncomplete && DirectCodeOfferCount > 0)
            return NetherRouteSemanticTier.DirectCodeOffer;
        if (NormalBattleCount > 0)
            return NetherRouteSemanticTier.NormalBattle;
        if (DirectCodeOfferCount > 0)
            return NetherRouteSemanticTier.DirectCodeOffer;
        if (OrdinaryEventRewardCount > 0)
            return NetherRouteSemanticTier.OrdinaryEventReward;
        if (IneligibleShopCount > 0)
            return NetherRouteSemanticTier.IneligibleShop;
        if (RecoveryCount > 0)
            return NetherRouteSemanticTier.Recovery;
        if (OtherTreasureCount > 0)
            return NetherRouteSemanticTier.OtherTreasure;
        return NetherRouteSemanticTier.None;
    }

    public int CompareTo(NetherRouteEncounterVector? other, bool researchIncomplete)
    {
        if (other == null)
            return 1;

        int comparison = CompareDescending(ImmediateTerminalBossCount, other.ImmediateTerminalBossCount);
        if (comparison != 0)
            return comparison;
        comparison = CompareDescending(RedRankFiveTreasureCount, other.RedRankFiveTreasureCount);
        if (comparison != 0)
            return comparison;
        // Gold Treasure and an eligible late Shop occupy one semantic tier. Count the complete
        // tier first; only an equal tier count lets a Treasure preference break the tie.
        int goldObjectiveCount = GoldRankFiveTreasureCount + EligibleLateShopCount;
        int otherGoldObjectiveCount = other.GoldRankFiveTreasureCount + other.EligibleLateShopCount;
        comparison = CompareDescending(goldObjectiveCount, otherGoldObjectiveCount);
        if (comparison != 0)
            return comparison;
        comparison = CompareDescending(GoldRankFiveTreasureCount, other.GoldRankFiveTreasureCount);
        if (comparison != 0)
            return comparison;
        comparison = CompareDescending(UncolouredRankFiveTreasureCount, other.UncolouredRankFiveTreasureCount);
        if (comparison != 0)
            return comparison;
        comparison = CompareDescending(EventBossCount, other.EventBossCount);
        if (comparison != 0)
            return comparison;
        comparison = CompareDescending(EliteCount, other.EliteCount);
        if (comparison != 0)
            return comparison;

        // The direct Code Offer/Normal Battle order is mode and research-state dependent.
        if (researchIncomplete)
        {
            comparison = CompareDescending(DirectCodeOfferCount, other.DirectCodeOfferCount);
            if (comparison != 0)
                return comparison;
            comparison = CompareDescending(NormalBattleCount, other.NormalBattleCount);
        }
        else
        {
            comparison = CompareDescending(NormalBattleCount, other.NormalBattleCount);
            if (comparison != 0)
                return comparison;
            comparison = CompareDescending(DirectCodeOfferCount, other.DirectCodeOfferCount);
        }
        if (comparison != 0)
            return comparison;

        comparison = CompareDescending(OrdinaryEventRewardCount, other.OrdinaryEventRewardCount);
        if (comparison != 0)
            return comparison;

        // Ineligible Shop is a safety-negative semantic tier and must be resolved before the
        // Recovery-count tie break. Both remain before erosion/HP/coordinates, which are
        // route-vector tie breaks only.
        comparison = CompareAscending(IneligibleShopCount, other.IneligibleShopCount);
        if (comparison != 0)
            return comparison;
        comparison = CompareAscending(RecoveryCount, other.RecoveryCount);
        if (comparison != 0)
            return comparison;
        return CompareAscending(OtherTreasureCount, other.OtherTreasureCount);
    }

    private static int CompareDescending(int left, int right) => left.CompareTo(right);

    private static int CompareAscending(int left, int right) => right.CompareTo(left);
}

/// <summary>
/// Converts only exact, current visible rows into a route vector. An absent or unresolved row
/// contributes no reward; it is never promoted from a relation, raw battle type, or display hint.
/// </summary>
internal static class NetherRouteEncounterVectorPolicy
{
    public static NetherRouteEncounterVector Build(
        NetherSnapshot snapshot,
        NetherRouteSafetyContext context,
        NetherFloorNode selected,
        NetherRouteHorizonSafetyEvaluation horizon
    )
    {
        if (snapshot == null || context == null || selected == null || horizon == null)
        {
            return new NetherRouteEncounterVector
            {
                IsKnown = false,
                UnknownReason = "route-vector-input-unavailable",
                UnknownReasonCode = NetherStrategyUnknownReasonCode.RouteVectorInputUnavailable,
            };
        }

        IReadOnlyList<NetherStrategyVisibleContentRow> rows = context.VisibleMap?.ContentRows
            ?? Array.Empty<NetherStrategyVisibleContentRow>();
        Dictionary<long, NetherFloorNode> floors = (context.VisibleMap?.Floors ?? snapshot.Floors)
            .Where(floor => floor != null && floor.NodeId > 0)
            .GroupBy(floor => floor.NodeId)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single());

        var vector = new NetherRouteEncounterVector();
        IReadOnlyList<NetherRouteHorizonStep> steps = horizon.HorizonSteps ?? Array.Empty<NetherRouteHorizonStep>();
        for (int index = 0; index < steps.Count; index++)
        {
            NetherRouteHorizonStep step = steps[index];
            if (step == null || step.NodeId <= 0)
                continue;
            if (step.NodeType == NetherFloorNodeType.Boss)
            {
                if (index == 0 && step.NodeId == selected.NodeId)
                    vector = vector with { ImmediateTerminalBossCount = vector.ImmediateTerminalBossCount + 1 };
                continue;
            }

            floors.TryGetValue(step.NodeId, out NetherFloorNode? floor);
            switch (ClassifyNode(snapshot, context, floor ?? selected with
                    {
                        NodeId = step.NodeId,
                    }, step.NodeType, rows))
            {
                case RouteEncounterKind.RedRankFiveTreasure:
                    vector = vector with { RedRankFiveTreasureCount = vector.RedRankFiveTreasureCount + 1 };
                    break;
                case RouteEncounterKind.GoldRankFiveTreasure:
                    vector = vector with { GoldRankFiveTreasureCount = vector.GoldRankFiveTreasureCount + 1 };
                    break;
                case RouteEncounterKind.UncolouredRankFiveTreasure:
                    vector = vector with { UncolouredRankFiveTreasureCount = vector.UncolouredRankFiveTreasureCount + 1 };
                    break;
                case RouteEncounterKind.EligibleLateShop:
                    vector = vector with { EligibleLateShopCount = vector.EligibleLateShopCount + 1 };
                    break;
                case RouteEncounterKind.EventBoss:
                    vector = vector with { EventBossCount = vector.EventBossCount + 1 };
                    break;
                case RouteEncounterKind.Elite:
                    vector = vector with { EliteCount = vector.EliteCount + 1 };
                    break;
                case RouteEncounterKind.NormalBattle:
                    vector = vector with { NormalBattleCount = vector.NormalBattleCount + 1 };
                    break;
                case RouteEncounterKind.DirectCodeOffer:
                    vector = vector with { DirectCodeOfferCount = vector.DirectCodeOfferCount + 1 };
                    break;
                case RouteEncounterKind.OrdinaryEventReward:
                    vector = vector with { OrdinaryEventRewardCount = vector.OrdinaryEventRewardCount + 1 };
                    break;
                case RouteEncounterKind.Recovery:
                    vector = vector with { RecoveryCount = vector.RecoveryCount + 1 };
                    break;
                case RouteEncounterKind.IneligibleShop:
                    vector = vector with { IneligibleShopCount = vector.IneligibleShopCount + 1 };
                    break;
                case RouteEncounterKind.OtherTreasure:
                    vector = vector with { OtherTreasureCount = vector.OtherTreasureCount + 1 };
                    break;
                case RouteEncounterKind.Unknown:
                    vector = vector with
                    {
                        IsKnown = false,
                        UnknownReason = "event-battle-route-safety-proof-unavailable",
                        UnknownReasonCode = NetherStrategyUnknownReasonCode.NativeBattleRouteSafetyUnknown,
                    };
                    break;
            }
        }
        return vector;
    }

    private static RouteEncounterKind ClassifyNode(
        NetherSnapshot snapshot,
        NetherRouteSafetyContext context,
        NetherFloorNode floor,
        NetherFloorNodeType stepType,
        IReadOnlyList<NetherStrategyVisibleContentRow> rows
    )
    {
        switch (stepType)
        {
            case NetherFloorNodeType.MiniBoss:
                return RouteEncounterKind.Elite;
            case NetherFloorNodeType.Battle:
                return RouteEncounterKind.NormalBattle;
            case NetherFloorNodeType.Recovery:
                return RouteEncounterKind.Recovery;
            case NetherFloorNodeType.Shop:
                return IsEligibleLateShop(snapshot, floor, rows)
                    ? RouteEncounterKind.EligibleLateShop
                    : RouteEncounterKind.IneligibleShop;
            case NetherFloorNodeType.Treasure:
                return ClassifyTreasure(floor.NodeId, rows);
            case NetherFloorNodeType.Event:
                return ClassifyEvent(snapshot, context, floor, rows);
            default:
                return RouteEncounterKind.None;
        }
    }

    private static RouteEncounterKind ClassifyTreasure(
        long nodeId,
        IReadOnlyList<NetherStrategyVisibleContentRow> rows
    )
    {
        NetherStrategyVisibleContentRow[] treasures = rows
            .Where(row => row != null
                && row.Kind == NetherStrategyVisibleContentKind.Treasure
                && row.NodeId == nodeId)
            .ToArray();
        if (treasures.Length != 1 || !treasures[0].IsKnown)
            return RouteEncounterKind.OtherTreasure;

        NetherStrategyVisibleContentRow[] rewards = rows
            .Where(row => row != null
                && row.Kind == NetherStrategyVisibleContentKind.Item
                && row.NodeId == nodeId
                && row.EventId == treasures[0].EventId
                && row.IsKnown
                && row.ItemType == 91
                && row.Amount > 0)
            .ToArray();
        if (rewards.Length != 1)
            return RouteEncounterKind.OtherTreasure;

        NetherStrategyVisibleContentRow reward = rewards[0];
        // MItems raw type/rarity and a display Rank do not prove route colour. Only an
        // authoritative typed provider may mark this exact row as canonical rank five.
        if (!NetherCanonicalRewardTierProvider.TryGetRankFiveTier(
                reward,
                nodeId,
                treasures[0].EventId,
                out NetherCanonicalRewardTier tier
            ))
        {
            return RouteEncounterKind.OtherTreasure;
        }
        return tier switch
        {
            NetherCanonicalRewardTier.RedRankFive => RouteEncounterKind.RedRankFiveTreasure,
            NetherCanonicalRewardTier.GoldRankFive => RouteEncounterKind.GoldRankFiveTreasure,
            _ => RouteEncounterKind.UncolouredRankFiveTreasure,
        };
    }

    private static bool IsEligibleLateShop(
        NetherSnapshot snapshot,
        NetherFloorNode floor,
        IReadOnlyList<NetherStrategyVisibleContentRow> rows
    )
    {
        if (floor == null || floor.FloorLevel <= 90 || snapshot.NetherGold < 300)
            return false;
        NetherStrategyVisibleContentRow[] shopRows = rows
            .Where(row => row != null
                && row.Kind == NetherStrategyVisibleContentKind.ShopInventory
                && row.NodeId == floor.NodeId)
            .ToArray();
        if (shopRows.Length == 0
            || shopRows.Any(row => !row.IsKnown
                || row.MasterRowId < 0
                || row.MasterRowId == 0
                    && !NetherCanonicalRewardTierProvider.IsAuthoritativeShopKeyRow(row)
                || row.ContentId < 0
                || row.Amount <= 0
                || row.Cost < 0
                || row.ItemType < 0
                || row.Rank < 0))
        {
            return false;
        }
        NetherStrategyVisibleContentRow[] exact = shopRows
            .Where(NetherCanonicalRewardTierProvider.IsCanonicalGoldRankFiveShopRow)
            .ToArray();
        return exact.Length == 1;
    }

    private static RouteEncounterKind ClassifyEvent(
        NetherSnapshot snapshot,
        NetherRouteSafetyContext context,
        NetherFloorNode floor,
        IReadOnlyList<NetherStrategyVisibleContentRow> rows
    )
    {
        NetherStrategyVisibleContentRow[] eventRows = rows
            .Where(row => row != null
                && row.Kind == NetherStrategyVisibleContentKind.Event
                && row.NodeId == floor.NodeId
                && row.EventId > 0
                && row.EventPartId > 0)
            .ToArray();
        if (eventRows.Length == 0)
            return RouteEncounterKind.None;

        long[] eventIds = eventRows.Select(row => row.EventId).Distinct().ToArray();
        if (eventIds.Length != 1)
            return RouteEncounterKind.None;

        NetherStrategyVisibleContentRow[] uniqueParts = eventRows
            .GroupBy(row => row.EventPartId)
            .Where(group => group.Count() == 1)
            .Select(group => group.Single())
            .ToArray();
        if (uniqueParts.Length == 0)
            return RouteEncounterKind.None;

        NetherAutoClimbSettings settings = (context.StrategySettings ?? new NetherAutoClimbSettings()) with
        {
            StrategyMode = context.StrategyMode,
        };
        if (settings.StrategyMode == NetherStrategyMode.Research
            && context.ResearchIncomplete is not bool researchIncomplete)
        {
            return RouteEncounterKind.None;
        }
        bool incomplete = settings.StrategyMode == NetherStrategyMode.Research
            && context.ResearchIncomplete == true;
        NetherEventStrategyEvidence strategyEvidence = new()
        {
            IsKnown = true,
            Mode = settings.StrategyMode,
            ResearchIncomplete = incomplete,
            HasRouteEvidence = true,
            HasResourceEvidence = true,
            HasSemanticEvidence = true,
        };
        NetherEventOption[] options = uniqueParts
            .Select(row => BuildEventOption(
                context,
                floor,
                row,
                rows,
                strategyEvidence
            ))
            .ToArray();
        NetherEventDecision decision = new NetherEventPolicy().DecideEvent(
            snapshot,
            options,
            settings,
            Array.Empty<NetherErosionModifier>(),
            strategyEvidence
        );
        if (decision.Kind != NetherEventDecisionKind.Select)
            return RouteEncounterKind.None;

        NetherEventOption? selected = options.FirstOrDefault(option =>
            option.EventId == decision.EventId
            && option.EventPartId == decision.EventPartId
            && option.OptionNumber == decision.OptionNumber);
        if (selected == null)
            return RouteEncounterKind.None;
        if (decision.Battle is { IsKnown: true } battle)
        {
            if (!HasExactBattleRouteSafetyEvidence(
                    snapshot,
                    context,
                    floor,
                    selected,
                    decision,
                    battle
                ))
            {
                return RouteEncounterKind.Unknown;
            }
            return battle.SemanticTier switch
            {
                NetherEventBattleTier.Boss => RouteEncounterKind.EventBoss,
                NetherEventBattleTier.MiniBoss => RouteEncounterKind.Elite,
                NetherEventBattleTier.NormalBattle => RouteEncounterKind.NormalBattle,
                _ => RouteEncounterKind.None,
            };
        }
        return selected.Effects.Any(effect => effect.Kind == NetherEffectKind.AbyssCodeOffer)
            ? RouteEncounterKind.DirectCodeOffer
            : RouteEncounterKind.OrdinaryEventReward;
    }

    private static bool HasExactBattleRouteSafetyEvidence(
        NetherSnapshot snapshot,
        NetherRouteSafetyContext context,
        NetherFloorNode floor,
        NetherEventOption selected,
        NetherEventDecision decision,
        NetherEventBattleEvidence battle
    )
    {
        NetherRuntimeInteractivePreEntryInputsResult? interactive = context.InteractivePreEntry;
        if (interactive == null
            || !interactive.IsSuccess
            || interactive.SnapshotFingerprint != snapshot.Fingerprint
            || interactive.TypedSemanticProvider == null
            || !interactive.ByFloorNodeId.TryGetValue(
                floor.NodeId,
                out NetherRuntimeInteractivePreEntryCaptureResult? capture
            )
            || capture == null
            || !capture.IsCaptured
            || capture.Input == null)
        {
            return false;
        }

        NetherInteractiveFloorPreEntrySafetyInput input = capture.Input;
        if (input.FloorNodeId != floor.NodeId
            || input.FloorMasterId != floor.FloorId
            || input.CurrentErosion != snapshot.ErosionPoint
            || input.ActiveHpPermille == null)
        {
            return false;
        }

        NetherCharacterState[] activeCharacters = snapshot.Characters
            .Where(character => character.IsActive)
            .OrderBy(character => character.CharacterId)
            .ToArray();
        if (activeCharacters.Length == 0
            || input.ActiveHpPermille.Count != activeCharacters.Length
            || !input.ActiveHpPermille.SequenceEqual(
                activeCharacters.Select(character => character.HpPermille)
            ))
        {
            return false;
        }

        NetherStrategyTypedSemanticProviderEvidence? inputProvider = input.TypedSemanticProvider;
        if (inputProvider == null
            || !ProvidersEquivalent(inputProvider, interactive.TypedSemanticProvider))
        {
            return false;
        }
        NetherStrategySemanticTierLookup semantic = NetherStrategySemanticTierLookup.Create(inputProvider);
        if (!semantic.TryGetEventBattleTier(battle.BattleId, out NetherEventBattleTier tier)
            || tier != battle.SemanticTier
            || !semantic.TryGetEventBattleRouteSafety(
                selected.EventId,
                selected.EventPartId,
                selected.OptionNumber,
                floor.FloorId,
                floor.NodeId,
                battle.BattleId,
                out NetherEventBattleRouteSafetyProviderEvidence proof
            ))
        {
            return false;
        }

        return proof.ProjectedErosion == decision.ProjectedErosion
            && proof.ProjectedHpDelta == decision.HpDelta
            && proof.CurrentCombatCharacterIds
                .OrderBy(characterId => characterId)
                .SequenceEqual(activeCharacters.Select(character => character.CharacterId));
    }

    private static bool ProvidersEquivalent(
        NetherStrategyTypedSemanticProviderEvidence left,
        NetherStrategyTypedSemanticProviderEvidence right
    ) => (left.CanonicalRewardTiers ?? Array.Empty<NetherCanonicalRewardTierProviderEvidence>())
            .SequenceEqual(right.CanonicalRewardTiers ?? Array.Empty<NetherCanonicalRewardTierProviderEvidence>())
        && (left.EventBattleTiers ?? Array.Empty<NetherEventBattleTierProviderEvidence>())
            .SequenceEqual(right.EventBattleTiers ?? Array.Empty<NetherEventBattleTierProviderEvidence>())
        && (left.EventBattleRouteSafety ?? Array.Empty<NetherEventBattleRouteSafetyProviderEvidence>())
            .SequenceEqual(right.EventBattleRouteSafety ?? Array.Empty<NetherEventBattleRouteSafetyProviderEvidence>())
        && (left.ShopKeyIdentities ?? Array.Empty<NetherShopKeyProviderEvidence>())
            .SequenceEqual(right.ShopKeyIdentities ?? Array.Empty<NetherShopKeyProviderEvidence>());

    private static NetherEventOption BuildEventOption(
        NetherRouteSafetyContext context,
        NetherFloorNode floor,
        NetherStrategyVisibleContentRow row,
        IReadOnlyList<NetherStrategyVisibleContentRow> visibleRows,
        NetherEventStrategyEvidence strategyEvidence
    )
    {
        NetherStrategyVisibleEventOptionEvidence[] visibleOptions = (row.EventOptions ?? Array.Empty<NetherStrategyVisibleEventOptionEvidence>())
            .Where(option => option != null && option.EventPartId == row.EventPartId && option.OptionNumber > 0)
            .ToArray();
        int optionNumber = visibleOptions.Length == 1 ? visibleOptions[0].OptionNumber : 1;
        string? unknown = !row.IsKnown
            ? string.IsNullOrWhiteSpace(row.UnknownReason) ? "unknown-event-part" : row.UnknownReason
            : visibleOptions.Length != 1
                ? "ambiguous-event-option"
                : null;
        var effects = new List<NetherEffect>();
        if (unknown == null)
        {
            foreach (NetherStrategyVisibleEventEffectEvidence? effect in visibleOptions[0].Effects ?? Array.Empty<NetherStrategyVisibleEventEffectEvidence>())
            {
                if (effect == null)
                {
                    unknown = "null-event-effect";
                    break;
                }
                if (!effect.IsPresent)
                    continue;
                effects.Add(MapEventEffect(effect, row, visibleRows));
            }
            if (effects.Count is < 1 or > 4)
                unknown = "invalid-event-effect-count";
        }
        if (unknown != null)
        {
            effects =
            [
                new NetherEffect(NetherEffectKind.Unknown, 0)
                {
                    Known = false,
                    ContentKnown = false,
                },
            ];
        }

        var option = new NetherEventOption(optionNumber, effects)
        {
            EventId = row.EventId,
            EventPartId = row.EventPartId,
            FloorId = floor.FloorId,
            NodeId = floor.NodeId,
            RequiresExactBinding = true,
            UnknownReason = unknown ?? string.Empty,
            StrategyEvidence = strategyEvidence,
        };
        NetherInteractiveEventOptionKey key = new(row.EventId, row.EventPartId, optionNumber);
        if (context.EventProcurementCommitments.TryGetValue(
                key,
                out NetherEventProcurementBudget budget
            ) && budget.IsValid)
        {
            option = option with
            {
                CommittedGoldMinimum = budget.GoldMinimum,
                CommittedKeyMinimum = budget.KeyMinimum,
            };
        }
        return option;
    }

    private static NetherEffect MapEventEffect(
        NetherStrategyVisibleEventEffectEvidence evidence,
        NetherStrategyVisibleContentRow eventRow,
        IReadOnlyList<NetherStrategyVisibleContentRow> rows
    )
    {
        if (!evidence.IsKnown
            || evidence.EffectKind == NetherEffectKind.Unknown
            || evidence.Amount < 0
            || evidence.Amount > int.MaxValue)
        {
            return UnknownEffect();
        }
        NetherEffect effect = new(evidence.EffectKind, (int)evidence.Amount)
        {
            ContentId = evidence.ContentId,
        };
        if (evidence.EffectKind == NetherEffectKind.Battle)
        {
            NetherStrategyVisibleContentRow[] battles = rows
                .Where(row => row != null
                    && (row.Kind is NetherStrategyVisibleContentKind.Battle or NetherStrategyVisibleContentKind.Boss)
                    && row.NodeId == eventRow.NodeId
                    && row.EventId == eventRow.EventId
                    && row.EventPartId == eventRow.EventPartId)
                .ToArray();
            if (battles.Length != 1
                || !battles[0].IsKnown
                || battles[0].MasterRowId <= 0
                || battles[0].BattleStageId <= 0
                || battles[0].CodeDropRatio < 0
                || battles[0].EventBattleTier == NetherEventBattleTier.Unknown)
            {
                return UnknownEffect();
            }
            NetherStrategyVisibleContentRow battle = battles[0];
            effect = effect with
            {
                IsOptionalBattle = true,
                BattleEvidence = new NetherEventBattleEvidence(
                    battle.MasterRowId,
                    battle.BattleStageId,
                    battle.BattleType,
                    battle.CodeDropRatio,
                    battle.EventBattleTier
                ),
            };
        }
        else if (evidence.EffectKind == NetherEffectKind.Item)
        {
            NetherStrategyVisibleContentRow[] rewards = rows
                .Where(row => row != null
                    && row.Kind == NetherStrategyVisibleContentKind.Item
                    && row.NodeId == eventRow.NodeId
                    && row.EventId == eventRow.EventId
                    && row.EventPartId == eventRow.EventPartId
                    && row.IsKnown)
                .ToArray();
            if (rewards.Length != 1
                || !NetherCanonicalRewardTierProvider.TryGetTypedRewardEvidence(
                    rewards[0],
                    out NetherEventRewardEvidence rewardEvidence
                ))
            {
                return UnknownEffect();
            }
            effect = effect with
            {
                RewardEvidence = rewardEvidence,
            };
        }
        return effect;
    }

    private static NetherEffect UnknownEffect() => new(NetherEffectKind.Unknown, 0)
    {
        Known = false,
        ContentKnown = false,
    };

    private enum RouteEncounterKind
    {
        None,
        Unknown,
        RedRankFiveTreasure,
        GoldRankFiveTreasure,
        UncolouredRankFiveTreasure,
        EligibleLateShop,
        EventBoss,
        Elite,
        NormalBattle,
        DirectCodeOffer,
        OrdinaryEventReward,
        Recovery,
        IneligibleShop,
        OtherTreasure,
    }
}
