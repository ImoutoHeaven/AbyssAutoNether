using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherStrategyModes1012Tests
{
    [Fact]
    public void Recovery_uses_the_only_complete_visible_branch_proven_safe()
    {
        NetherEventDecision decision = new NetherEventPolicy().DecideRecovery(
            Snapshot(erosion: 50, hp: 500),
            [
                RecoveryOption(1, NetherRecoveryBranchKind.Rest, new NetherEffect(NetherEffectKind.Heal, 100), safe: false),
                RecoveryOption(2, NetherRecoveryBranchKind.Purification, new NetherEffect(NetherEffectKind.ErosionHeal, 10), safe: true),
                RecoveryOption(3, NetherRecoveryBranchKind.Transform, new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0), safe: false),
            ],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(2, decision.OptionNumber);
    }

    [Fact]
    public void Recovery_pauses_when_complete_visible_branch_proves_neither_repair_safe()
    {
        NetherEventDecision decision = new NetherEventPolicy().DecideRecovery(
            Snapshot(erosion: 50, hp: 500),
            [
                RecoveryOption(1, NetherRecoveryBranchKind.Rest, new NetherEffect(NetherEffectKind.Heal, 100), safe: false),
                RecoveryOption(2, NetherRecoveryBranchKind.Purification, new NetherEffect(NetherEffectKind.ErosionHeal, 10), safe: false),
                RecoveryOption(3, NetherRecoveryBranchKind.Transform, new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0), safe: false),
            ],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.NoSafeRoute, decision.PauseReason);
    }

    [Fact]
    public void Recovery_all_safe_tie_keeps_safe_known_loser_in_audit()
    {
        NetherEventDecision decision = new NetherEventPolicy().DecideRecovery(
            Snapshot(erosion: 0, hp: 1000) with
            {
                Codes = [new NetherCodeState(40024, NetherCodeFamily.Risk, 1) { IsKnown = true }],
                CodeCapacity = 5,
            },
            [
                RecoveryOption(1, NetherRecoveryBranchKind.Rest, new NetherEffect(NetherEffectKind.Heal, 100), safe: true),
                RecoveryOption(2, NetherRecoveryBranchKind.Purification, new NetherEffect(NetherEffectKind.ErosionHeal, 10), safe: true),
                RecoveryOption(
                    3,
                    NetherRecoveryBranchKind.Transform,
                    new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0),
                    safe: true,
                    transformEligibility: new NetherCodeTransformEligibilityEvidence
                    {
                        StrategyMode = NetherStrategyMode.Equipment,
                        EquipmentOptInEnabled = true,
                        IsRecovery = true,
                        DeterministicRecoveryChoicesHaveZeroValue = false,
                        HardExcludedCodes = [],
                    }
                ),
            ],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(1, decision.OptionNumber);
        Assert.Equal(3, decision.OptionAudits.Count);

        NetherEventOptionAudit loser = Assert.Single(
            decision.OptionAudits,
            audit => audit.OptionNumber == 2
        );
        Assert.True(loser.IsKnown);
        Assert.False(loser.IsSelected);
        Assert.Equal(NetherEventOptionHardGate.None, loser.FirstFailingHardGate);
        Assert.Equal(NetherEventOptionSelectionTier.Recovery, loser.SelectionTier);
        Assert.Equal(NetherStrategyUnknownReasonCode.None, loser.UnknownReasonCode);
        Assert.Equal(
            "eligible-safe-but-not-selected-by-deterministic-recovery-tie-break",
            loser.ComparisonRationale
        );

        NetherEventOptionAudit selected = Assert.Single(
            decision.OptionAudits,
            audit => audit.OptionNumber == 1
        );
        Assert.True(selected.IsKnown);
        Assert.True(selected.IsSelected);
        Assert.Equal(NetherEventOptionHardGate.None, selected.FirstFailingHardGate);
        Assert.Equal(NetherEventOptionSelectionTier.Recovery, selected.SelectionTier);
        Assert.Equal(NetherStrategyUnknownReasonCode.None, selected.UnknownReasonCode);
        Assert.Equal("selected-by-complete-branch-proof", selected.ComparisonRationale);

        NetherEventOptionAudit transform = Assert.Single(
            decision.OptionAudits,
            audit => audit.OptionNumber == 3
        );
        Assert.True(transform.IsKnown);
        Assert.False(transform.IsSelected);
        Assert.Equal(
            NetherEventOptionHardGate.RecoveryTransformPolicy,
            transform.FirstFailingHardGate
        );
        Assert.Equal(NetherEventOptionSelectionTier.None, transform.SelectionTier);
        Assert.Equal(NetherStrategyUnknownReasonCode.None, transform.UnknownReasonCode);
        Assert.Equal("deterministic-recovery-choice-has-value", transform.Detail);
        Assert.Equal(
            "excluded:recovery-transform-policy=deterministic-recovery-choice-has-value",
            transform.ComparisonRationale
        );
    }

    [Fact]
    public void Recovery_transform_selection_retains_the_transform_semantic_tier_in_the_policy_audit()
    {
        NetherSnapshot snapshot = Snapshot(erosion: 0, hp: 1000) with
        {
            Codes = [new NetherCodeState(40024, NetherCodeFamily.Risk, 1) { IsKnown = true }],
            CodeCapacity = 5,
        };
        NetherEventOption[] options =
        [
            RecoveryOption(1, NetherRecoveryBranchKind.Rest, new NetherEffect(NetherEffectKind.Heal, 100), safe: false),
            RecoveryOption(2, NetherRecoveryBranchKind.Purification, new NetherEffect(NetherEffectKind.ErosionHeal, 10), safe: false),
            RecoveryOption(3, NetherRecoveryBranchKind.Transform, new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0), safe: true),
        ];

        NetherEventDecision decision = new NetherEventPolicy().DecideRecovery(
            snapshot,
            options,
            Settings() with { EquipmentRecoveryCodeTransformEnabled = true },
            Array.Empty<NetherErosionModifier>(),
            new NetherCodeTransformHardExclusionEvidence
            {
                IsKnown = true,
                HardExcludedCodes =
                [
                    new NetherCodeTransformHardExclusion(
                        40024,
                        NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment
                    ),
                ],
            }
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        NetherEventOptionAudit selected = Assert.Single(
            decision.OptionAudits,
            audit => audit.OptionNumber == 3
        );
        Assert.True(selected.IsSelected);
        Assert.Equal(NetherEventOptionSelectionTier.RecoveryTransform, selected.SelectionTier);
    }

    [Fact]
    public void Treasure_rejects_a_non_40_or_80_hp_payment_even_with_route_proof()
    {
        NetherEventDecision decision = new NetherEventPolicy().DecideTreasure(
            Snapshot(hp: 500),
            [
                new NetherEventOption(1, [new NetherEffect(NetherEffectKind.Damage, 20)])
                {
                    EventId = 10,
                    EventPartId = 11,
                    PartialDeathEligibility = TreasureProof(10, 11),
                },
            ],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.NoSafeRoute, decision.PauseReason);
    }

    [Fact]
    public void Rank_five_procurement_prefers_exact_150_gold_event_before_200_gold_shop()
    {
        NetherRankFiveKeyProcurementDecision decision = new NetherRankFiveKeyProcurementPolicy().Evaluate(
            new NetherRankFiveKeyProcurementInput(
                CurrentNetherGold: 250,
                CurrentTreasureKeys: 0,
                ActiveHpPermille: [500],
                SelectedPathNodeIds: [1, 2, 3, 4, 5],
                HardSafeNodeIds: new HashSet<long> { 1, 2, 3, 4, 5 },
                Floors: Floors(),
                ContentRows:
                [
                    EventSource(2, 2001, 2002),
                    ShopSource(3, 3001),
                    Treasure(4, 4001, 4002),
                    RankFiveItem(4, 4002),
                ]
            )
        );

        Assert.True(decision.HasMandatoryObjective);
        Assert.Equal(NetherKeyProcurementSourceKind.EventGold150, decision.SourceKind);
        Assert.Equal(150, decision.GoldCost);
        Assert.False(decision.AllowsHpFallback);
        Assert.Equal(4, decision.Objective.ObjectiveNodeId);
        Assert.True(decision.Commitment!.IsValid);
    }

    [Fact]
    public void Rank_five_procurement_falls_back_to_group_survival_hp_key_when_currency_is_unavailable()
    {
        NetherRankFiveKeyProcurementDecision decision = new NetherRankFiveKeyProcurementPolicy().Evaluate(
            new NetherRankFiveKeyProcurementInput(
                CurrentNetherGold: 0,
                CurrentTreasureKeys: 0,
                ActiveHpPermille: [100, 20],
                SelectedPathNodeIds: [1, 2, 4, 5],
                HardSafeNodeIds: new HashSet<long> { 1, 2, 4, 5 },
                Floors: Floors(),
                ContentRows:
                [
                    HpKeyEventSource(2, 2001, 2003),
                    Treasure(4, 4001, 4002),
                    RankFiveItem(4, 4002),
                ]
            )
        );

        Assert.True(decision.HasMandatoryObjective);
        Assert.Equal(NetherKeyProcurementSourceKind.HpPaidEventKey, decision.SourceKind);
        Assert.True(decision.AllowsPartialPartyDeath);
        Assert.Equal(4, decision.Objective.ObjectiveNodeId);
    }

    [Fact]
    public void Rank_five_procurement_rejects_raw_unique_weapon_without_typed_canonical_provider()
    {
        NetherRankFiveKeyProcurementDecision decision = new NetherRankFiveKeyProcurementPolicy().Evaluate(
            new NetherRankFiveKeyProcurementInput(
                CurrentNetherGold: 250,
                CurrentTreasureKeys: 0,
                ActiveHpPermille: [500],
                SelectedPathNodeIds: [1, 2, 3, 4, 5],
                HardSafeNodeIds: new HashSet<long> { 1, 2, 3, 4, 5 },
                Floors: Floors(),
                ContentRows:
                [
                    EventSource(2, 2001, 2002),
                    ShopSource(3, 3001),
                    Treasure(4, 4001, 4002),
                    RankFiveItem(4, 4002, NetherCanonicalRewardTier.Unknown),
                ]
            )
        );

        Assert.True(decision.IsKnown, decision.Detail);
        Assert.False(decision.HasMandatoryObjective);
        Assert.Equal("no-known-rank-five-treasure-on-selected-branch", decision.Detail);
    }

    [Fact]
    public void Route_planner_prefers_a_safe_path_with_rank_five_objective_over_ordinary_safe_path()
    {
        NetherFloorNode current = Floor(1, NetherFloorNodeType.Recovery);
        NetherFloorNode ordinary = Floor(2, NetherFloorNodeType.Recovery, 1);
        NetherFloorNode objective = Floor(3, NetherFloorNodeType.Recovery, 1);
        NetherFloorNode boss = Floor(4, NetherFloorNodeType.Boss, 2, 3);
        NetherSnapshot snapshot = new()
        {
            CurrentNodeId = 1,
            CurrentFloorId = 1,
            ErosionPoint = 20,
            Characters = [new NetherCharacterState(1, 800)],
            Floors = [current, ordinary, objective, boss],
        };
        NetherRouteSafetyContext context = SafeContext() with
        {
            HorizonEvaluationByFloorId = new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
            {
                [2] = Horizon(2),
                [3] = Horizon(3),
            },
            MandatoryRankFiveKeyObjectiveNodeIds = new HashSet<long> { 3 },
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(snapshot, context);

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    private static NetherEventOption RecoveryOption(
        int optionNumber,
        NetherRecoveryBranchKind branchKind,
        NetherEffect effect,
        bool safe,
        NetherCodeTransformEligibilityEvidence? transformEligibility = null
    ) => new(optionNumber, [effect])
    {
        FloorId = 10,
        NodeId = 1,
        RecoveryBranchSafety = new NetherRecoveryBranchSafetyEvidence
        {
            BranchKind = branchKind,
            IsKnown = true,
            IsCompleteVisibleBranch = true,
            IsNextVisibleBranchSafe = safe,
            TransformEligibility = transformEligibility,
        },
    };

    private static NetherInteractivePartialDeathEligibility TreasureProof(long eventId, long partId) => new(
        NetherInteractivePartialDeathObjectiveKind.TreasureHpPayment,
        eventId,
        partId,
        99
    )
    {
        IsKnown = true,
        ObjectiveReachable = true,
        ExactTreasureRank = 5,
    };

    private static NetherAutoClimbSettings Settings() => new()
    {
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = 300,
        TreasureMode = NetherTreasureMode.KeyOnly,
    };

    private static NetherSnapshot Snapshot(int erosion = 20, int hp = 500) => new()
    {
        ErosionPoint = erosion,
        NetherGold = 0,
        TreasureKeyCount = 0,
        Characters = [new NetherCharacterState(1, hp)],
    };

    private static NetherStrategyVisibleContentRow EventSource(long nodeId, long eventId, long partId) =>
        new(NetherStrategyVisibleContentKind.Event, nodeId, eventId, partId)
        {
            IsKnown = true,
            EventId = eventId,
            EventPartId = partId,
            EventOptions =
            [
                new NetherStrategyVisibleEventOptionEvidence(
                    1,
                    partId,
                    [
                        new NetherStrategyVisibleEventEffectEvidence(NetherStrategyVisibleEventEffectSource.Target1, 5, 150)
                        {
                            IsPresent = true,
                            IsKnown = true,
                            EffectKind = NetherEffectKind.NetherGoldUsed,
                            Amount = 150,
                        },
                        new NetherStrategyVisibleEventEffectEvidence(NetherStrategyVisibleEventEffectSource.Content, 166, 1)
                        {
                            IsPresent = true,
                            IsKnown = true,
                            EffectKind = NetherEffectKind.TreasureKeyGain,
                            Amount = 1,
                        },
                    ]
                ),
            ],
        };

    private static NetherStrategyVisibleContentRow HpKeyEventSource(long nodeId, long eventId, long partId) =>
        new(NetherStrategyVisibleContentKind.Event, nodeId, eventId, partId)
        {
            IsKnown = true,
            EventId = eventId,
            EventPartId = partId,
            EventOptions =
            [
                new NetherStrategyVisibleEventOptionEvidence(
                    1,
                    partId,
                    [
                        new NetherStrategyVisibleEventEffectEvidence(NetherStrategyVisibleEventEffectSource.Target1, 2, 80)
                        {
                            IsPresent = true,
                            IsKnown = true,
                            EffectKind = NetherEffectKind.Damage,
                            Amount = 80,
                        },
                        new NetherStrategyVisibleEventEffectEvidence(NetherStrategyVisibleEventEffectSource.Content, 166, 1)
                        {
                            IsPresent = true,
                            IsKnown = true,
                            EffectKind = NetherEffectKind.TreasureKeyGain,
                            Amount = 1,
                        },
                    ]
                ),
            ],
        };

    private static NetherStrategyVisibleContentRow ShopSource(long nodeId, long contentId) =>
        new(NetherStrategyVisibleContentKind.ShopInventory, nodeId, contentId, 0)
        {
            IsKnown = true,
            Cost = 200,
            Amount = 1,
            UsesNetherGold = true,
            IsTreasureKey = true,
        };

    private static NetherStrategyVisibleContentRow Treasure(long nodeId, long masterId, long eventId) =>
        new(NetherStrategyVisibleContentKind.Treasure, nodeId, masterId, eventId)
        {
            IsKnown = true,
            EventId = eventId,
        };

    private static NetherStrategyVisibleContentRow RankFiveItem(
        long nodeId,
        long partId,
        NetherCanonicalRewardTier canonicalTier = NetherCanonicalRewardTier.GoldRankFive
    ) =>
        new(NetherStrategyVisibleContentKind.Item, nodeId, partId, partId)
        {
            IsKnown = true,
            EventId = 4002,
            EventPartId = partId,
            ItemType = 91,
            ItemRarity = 5,
            Amount = 1,
            CanonicalRewardTier = canonicalTier,
        };

    private static NetherFloorNode[] Floors() =>
    [
        Floor(1, NetherFloorNodeType.Recovery),
        Floor(2, NetherFloorNodeType.Event, 1),
        Floor(3, NetherFloorNodeType.Shop, 2),
        Floor(4, NetherFloorNodeType.Treasure, 3),
        Floor(5, NetherFloorNodeType.Boss, 4),
    ];

    private static NetherFloorNode Floor(long nodeId, NetherFloorNodeType type, params long[] previous) =>
        new(nodeId, (int)nodeId, (int)nodeId, type)
        {
            IsUnlocked = true,
            PreviousFloorIds = previous,
        };

    private static NetherRouteSafetyContext SafeContext() => new()
    {
        AllowLegacyComparatorCompatibility = true,
        MinimumWorstCaseErosionToTerminal = new Dictionary<long, int> { [2] = 1, [3] = 1 },
        HpSafeByFloorId = new Dictionary<long, bool> { [2] = true, [3] = true },
        KnownNodeByFloorId = new Dictionary<long, bool> { [2] = true, [3] = true, [4] = true },
        HardSafeByFloorId = new Dictionary<long, bool> { [2] = true, [3] = true, [4] = true },
    };

    private static NetherRouteHorizonSafetyEvaluation Horizon(long nodeId) => new()
    {
        IsEligible = true,
        PeakErosion = 30,
        MinimumActiveCharacterHpPermille = 800,
        FinalErosion = 21,
        Steps = [new NetherRouteHorizonStepAudit(nodeId, 0, 0, 800)],
    };
}
