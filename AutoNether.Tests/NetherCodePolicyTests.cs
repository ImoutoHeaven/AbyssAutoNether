using System.Collections.Generic;
using System.Linq;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherCodePolicyTests
{
    [Fact]
    public void Mixed_back_row_rejects_uniform_crest_grant_by_mechanics_not_identifier()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly.dll 573fa800...c1fb:
        // PartyPositionType.Back=2 and ManaType.Passion/Impact=2/3. The candidate identifier is
        // deliberately unrelated to current IDs; the typed native grant relationship is authority.
        NetherCodeCandidate candidate = Candidate(990001, NetherCodeFamily.Impact);
        NetherCodePolicyEvidence uniformGrant = Evidence(
            candidate.CodeId,
            new NetherCodeHardEligibilityEvidence
            {
                IsKnown = true,
                UniformCrestFamily = NetherCodeFamily.Impact,
                UniformCrestTargetRow = NetherCodeTargetRow.Back,
            },
            Party(
                Member(1, partyIndex: 0, position: 2, manaType: 2),
                Member(2, partyIndex: 1, position: 2, manaType: 3)
            )
        );
        NetherCodePolicyEvidence ordinary = Evidence(
            candidate.CodeId,
            new NetherCodeHardEligibilityEvidence { IsKnown = true },
            uniformGrant.ActiveParty!
        );

        NetherCodeDecision rejected = Decide(Portfolio(), uniformGrant, candidate);
        NetherCodeDecision accepted = Decide(Portfolio(), ordinary, candidate);

        Assert.Equal(NetherCodeDecisionKind.Keep, rejected.Kind);
        Assert.Equal(NetherCodeDecisionKind.Select, accepted.Kind);
    }

    [Fact]
    public void Effective_counts_use_distinct_positive_owned_codes_before_opposition()
    {
        NetherCodeEffectiveLevels levels = NetherCodePolicy.CalculateEffectiveLevels(
            [
                Code(10, NetherCodeFamily.Rush, possessionAmount: 1),
                Code(10, NetherCodeFamily.Rush, possessionAmount: 8),
                Code(11, NetherCodeFamily.Rush, possessionAmount: 0),
                Code(20, NetherCodeFamily.Impact, possessionAmount: 1),
            ]
        );

        Assert.Equal(0, levels.Rush);
        Assert.Equal(0, levels.Impact);
    }

    [Fact]
    public void Opposed_family_is_not_added_and_existing_contamination_removes_weaker_side_first()
    {
        NetherCodeState rush = Code(10, NetherCodeFamily.Rush);
        NetherCodeState impact = Code(20, NetherCodeFamily.Impact);
        NetherCodeCandidate impactOffer = Candidate(21, NetherCodeFamily.Impact);
        NetherCodeCandidate rushOffer = Candidate(11, NetherCodeFamily.Rush);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 2)),
            rush,
            impact,
            impactOffer,
            rushOffer
        ) with
        {
            FamilyRetentionByPair = new Dictionary<
                NetherOpposedFamilyPair,
                NetherFamilyRetentionEvidence
            >
            {
                [NetherOpposedFamilyPair.RushImpact] =
                    NetherFamilyRetentionEvidence.Known(NetherCodeFamily.Rush),
            },
        };

        NetherCodeDecision opposed = Decide(
            Portfolio(current: [rush]),
            evidence,
            impactOffer
        );
        NetherCodeDecision repaired = Decide(
            Portfolio(capacity: 2, current: [rush, impact]),
            evidence,
            rushOffer
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, opposed.Kind);
        Assert.Equal(NetherCodeDecisionKind.Select, repaired.Kind);
        Assert.Equal(20, repaired.RemoveCodeId);
    }

    [Fact]
    public void Crossing_effective_count_five_requires_every_active_character_matching_family_crest()
    {
        NetherCodeState[] fourRush =
        [
            Code(101, NetherCodeFamily.Rush),
            Code(102, NetherCodeFamily.Rush),
            Code(103, NetherCodeFamily.Rush),
            Code(104, NetherCodeFamily.Rush),
        ];
        NetherCodeCandidate fifth = Candidate(105, NetherCodeFamily.Rush);
        NetherCodePolicyEvidence mixed = KnownEvidence(
            Party(Member(1, 0, 1, 2), Member(2, 1, 2, 3)),
            fourRush.Cast<object>().Append(fifth).ToArray()
        );
        NetherCodePolicyEvidence matching = mixed with
        {
            ActiveParty = Party(Member(1, 0, 1, 2), Member(2, 1, 2, 2)),
        };

        NetherCodeDecision rejected = Decide(
            Portfolio(current: fourRush),
            mixed,
            fifth
        );
        NetherCodeDecision accepted = Decide(
            Portfolio(current: fourRush),
            matching,
            fifth
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, rejected.Kind);
        Assert.Equal(NetherCodeDecisionKind.Select, accepted.Kind);
    }

    [Fact]
    public void Risk_rules_and_research_overwrite_are_hard_eligibility_not_display_power()
    {
        NetherCodeCandidate highGate = Candidate(990010, NetherCodeFamily.Risk, power: 99999);
        NetherCodeCandidate adverse = Candidate(990024, NetherCodeFamily.Risk, power: 99999);
        NetherCodeCandidate conditional = Candidate(990022, NetherCodeFamily.Risk);
        NetherCodeCandidate research = Candidate(990030, NetherCodeFamily.Risk);
        var mechanics = new Dictionary<long, NetherCodeHardEligibilityEvidence>
        {
            [highGate.CodeId] = new() { IsKnown = true, RiskRule = NetherCodeRiskRule.MinimumErosionSeventy },
            [adverse.CodeId] = new() { IsKnown = true, RiskRule = NetherCodeRiskRule.AdverseErosionAdjustment },
            [conditional.CodeId] = new() { IsKnown = true, RiskRule = NetherCodeRiskRule.ConditionalFiftyToSeventy },
            [research.CodeId] = new() { IsKnown = true, ResearchRateOverwrite = 15 },
        };
        NetherCodePolicyEvidence baseEvidence = new()
        {
            MechanicsByCodeId = mechanics,
            MechanismValuesByCodeId = mechanics.Keys.ToDictionary(
                codeId => codeId,
                _ => KnownZeroMechanism()
            ),
            EquipmentMutationValuesByKey = DefaultEquipmentMutations(
                highGate,
                adverse,
                conditional,
                research
            ),
            ActiveParty = Party(Member(1, 0, 2, 3)),
            ErosionHorizonKnown = true,
            ProjectedMinimumErosion = 50,
            ProjectedMaximumErosion = 70,
            RecoverableToFiftySeventyBand = true,
            Research = ResearchState(NetherCodeFamily.Risk, technologyRate: 10),
            ActiveResearchFamily = NetherCodeFamily.Risk,
        };
        NetherAutoClimbSettings researchSettings = new()
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Risk,
            CodeReloadReserve = 1,
        };

        Assert.Equal(NetherCodeDecisionKind.Keep, Decide(Portfolio(), baseEvidence, highGate).Kind);
        Assert.Equal(NetherCodeDecisionKind.Keep, Decide(Portfolio(), baseEvidence, adverse).Kind);
        Assert.Equal(NetherCodeDecisionKind.Select, Decide(Portfolio(), baseEvidence, conditional).Kind);
        Assert.Equal(NetherCodeDecisionKind.Keep, Decide(Portfolio(), baseEvidence, research).Kind);
        Assert.Equal(
            NetherCodeDecisionKind.Select,
            new NetherCodePolicy().Decide(Portfolio(), [research], researchSettings, baseEvidence).Kind
        );
        Assert.Equal(
            NetherCodeDecisionKind.Keep,
            Decide(
                Portfolio(),
                baseEvidence with { ProjectedMaximumErosion = 71 },
                conditional
            ).Kind
        );
    }

    [Fact]
    public void Unknown_candidate_is_rejected_locally_while_known_candidate_remains_selectable()
    {
        NetherCodeCandidate unknown = Candidate(991001, NetherCodeFamily.Safe, power: 99999);
        NetherCodeCandidate known = Candidate(991002, NetherCodeFamily.Safe, power: 1);
        NetherCodePolicyEvidence evidence = new()
        {
            MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
            {
                [unknown.CodeId] = new() { IsKnown = false, UnknownReason = "target-filter-unavailable" },
                [known.CodeId] = new() { IsKnown = true },
            },
            MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
            {
                [unknown.CodeId] = KnownZeroMechanism(),
                [known.CodeId] = KnownZeroMechanism(),
            },
            EquipmentMutationValuesByKey = DefaultEquipmentMutations(unknown, known),
            ActiveParty = Party(Member(1, 0, 2, 3)),
        };

        NetherCodeDecision decision = Decide(Portfolio(), evidence, unknown, known);

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(known.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Missing_and_reachable_unquantified_values_reject_only_their_candidates()
    {
        NetherCodeCandidate missing = Candidate(991101, NetherCodeFamily.Safe);
        NetherCodeCandidate reachable = Candidate(991102, NetherCodeFamily.Safe);
        NetherCodeCandidate proven = Candidate(991103, NetherCodeFamily.Safe);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3)),
            missing,
            reachable,
            proven
        ) with
        {
            MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
            {
                [missing.CodeId] = NetherMechanismValue.Missing("trigger-evidence-unavailable"),
                [reachable.CodeId] = NetherMechanismValue.ReachableUnquantified(
                    "trigger-reachable;cadence-unavailable"
                ),
                [proven.CodeId] = KnownZeroMechanism(),
            },
        };

        NetherCodeDecision decision = Decide(Portfolio(), evidence, missing, reachable, proven);

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(proven.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void High_back_force_chain_priority_does_not_bypass_mixed_row_crest_compatibility()
    {
        NetherCodeCandidate candidate = Candidate(991110, NetherCodeFamily.Impact);
        NetherCodePolicyEvidence evidence = Evidence(
            candidate.CodeId,
            new NetherCodeHardEligibilityEvidence
            {
                IsKnown = true,
                UniformCrestFamily = NetherCodeFamily.Impact,
                UniformCrestTargetRow = NetherCodeTargetRow.Back,
            },
            Party(
                Member(1, partyIndex: 0, position: 2, manaType: 2),
                Member(2, partyIndex: 1, position: 2, manaType: 3)
            )
        ) with
        {
            MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
            {
                [candidate.CodeId] = NetherMechanismValue.Qualitative(
                    NetherMechanismQualitativePriority.BackForceChainHigh,
                    "force-chain-completion-message"
                ),
            },
        };

        NetherCodeDecision decision = Decide(Portfolio(), evidence, candidate);

        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
    }

    [Fact]
    public void Equipment_orders_by_complete_native_value_when_displayed_power_is_reversed()
    {
        NetherCodeCandidate weaker = Candidate(
            991201,
            NetherCodeFamily.Safe,
            power: 99_999,
            coverage: 99
        );
        NetherCodeCandidate stronger = Candidate(
            991202,
            NetherCodeFamily.Safe,
            power: 1,
            coverage: 1
        );
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3)),
            weaker,
            stronger
        ) with
        {
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(weaker.CodeId, 0)] = Mutation(
                    weaker.CodeId,
                    removeCodeId: 0,
                    before: [],
                    after: [CombatWindow(weaker.CodeId, value: 100)]
                ),
                [new NetherCodeMutationKey(stronger.CodeId, 0)] = Mutation(
                    stronger.CodeId,
                    removeCodeId: 0,
                    before: [],
                    after: [CombatWindow(stronger.CodeId, value: 200)]
                ),
            },
        };

        NetherCodeDecision decision = Decide(Portfolio(), evidence, weaker, stronger);

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(stronger.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Hard_gate_precedes_retained_portfolio_value_ordering()
    {
        NetherCodeCandidate forbiddenRisk = Candidate(991205, NetherCodeFamily.Risk);
        NetherCodeCandidate eligibleSafe = Candidate(991206, NetherCodeFamily.Safe);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3)),
            forbiddenRisk,
            eligibleSafe
        ) with
        {
            MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
            {
                [forbiddenRisk.CodeId] = new()
                {
                    IsKnown = true,
                    RiskRule = NetherCodeRiskRule.MinimumErosionSeventy,
                },
                [eligibleSafe.CodeId] = new() { IsKnown = true },
            },
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(forbiddenRisk.CodeId, 0)] = Mutation(
                    forbiddenRisk.CodeId,
                    0,
                    before: [],
                    after: [CombatWindow(forbiddenRisk.CodeId, value: 9_999)]
                ),
                [new NetherCodeMutationKey(eligibleSafe.CodeId, 0)] = Mutation(
                    eligibleSafe.CodeId,
                    0,
                    before: [],
                    after: [CombatWindow(eligibleSafe.CodeId, value: 1)]
                ),
            },
        };

        NetherCodeDecision decision = Decide(
            Portfolio(),
            evidence,
            forbiddenRisk,
            eligibleSafe
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(eligibleSafe.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Equipment_zero_negative_and_reachable_unquantified_value_cannot_prove_selection()
    {
        NetherCodeCandidate zero = Candidate(991210, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate negative = Candidate(991211, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate unquantified = Candidate(991212, NetherCodeFamily.Safe, power: 99_999);
        NetherNativeBuffWindow held = CombatWindow(900001, value: 200);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3)),
            zero,
            negative,
            unquantified
        ) with
        {
            MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
            {
                [zero.CodeId] = KnownZeroMechanism(),
                [negative.CodeId] = KnownZeroMechanism(),
                [unquantified.CodeId] = NetherMechanismValue.ReachableUnquantified(
                    "trigger-reachable;cadence-unavailable"
                ),
            },
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(zero.CodeId, 0)] = Mutation(
                    zero.CodeId,
                    0,
                    before: [],
                    after: []
                ),
                [new NetherCodeMutationKey(negative.CodeId, 0)] = Mutation(
                    negative.CodeId,
                    0,
                    before: [held],
                    after: [held with { ValuePermille = 100 }]
                ),
                [new NetherCodeMutationKey(unquantified.CodeId, 0)] = Mutation(
                    unquantified.CodeId,
                    0,
                    before: [],
                    after: [CombatWindow(unquantified.CodeId, 9_999) with { TriggerKnown = false }],
                    mechanism: NetherMechanismValue.ReachableUnquantified(
                        "trigger-reachable;cadence-unavailable"
                    )
                ),
            },
        };

        Assert.Equal(NetherCodeDecisionKind.Keep, Decide(Portfolio(), evidence, zero).Kind);
        Assert.Equal(NetherCodeDecisionKind.Keep, Decide(Portfolio(), evidence, negative).Kind);
        Assert.Equal(NetherCodeDecisionKind.Keep, Decide(Portfolio(), evidence, unquantified).Kind);
        Assert.Equal(
            NetherCodeDecisionKind.Reload,
            Decide(Portfolio(reloadCount: 2), evidence, zero).Kind
        );
    }

    [Fact]
    public void Approved_back_force_chain_qualitative_tier_outranks_ordinary_quantified_value()
    {
        NetherCodeCandidate ordinary = Candidate(
            991220,
            NetherCodeFamily.Safe,
            power: 99_999
        );
        NetherCodeCandidate backForceChain = Candidate(
            991221,
            NetherCodeFamily.Safe,
            power: 1
        );
        NetherMechanismValue forceChain = NetherMechanismValue.Qualitative(
            NetherMechanismQualitativePriority.BackForceChainHigh,
            "force-chain-completion-message"
        );
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3)),
            ordinary,
            backForceChain
        ) with
        {
            MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
            {
                [ordinary.CodeId] = KnownZeroMechanism(),
                [backForceChain.CodeId] = forceChain,
            },
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(ordinary.CodeId, 0)] = Mutation(
                    ordinary.CodeId,
                    0,
                    before: [],
                    after: [CombatWindow(ordinary.CodeId, value: 500)]
                ),
                [new NetherCodeMutationKey(backForceChain.CodeId, 0)] = Mutation(
                    backForceChain.CodeId,
                    0,
                    before: [],
                    after: [],
                    mechanism: forceChain
                ),
            },
        };

        NetherCodeDecision decision = Decide(Portfolio(), evidence, ordinary, backForceChain);

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(backForceChain.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Equipment_does_not_exchange_incommensurate_native_mechanism_units()
    {
        // Shared Mana energy (bounded 0..10) and skill-charge count are separate native units.
        // Their literal magnitudes cannot be exchanged; equal combat tiers therefore use the
        // deterministic Code-id tie break rather than whichever raw decimal happens to be larger.
        NetherCodeCandidate smallerIdSkillCharge = Candidate(991222, NetherCodeFamily.Safe);
        NetherCodeCandidate largerIdMana = Candidate(991223, NetherCodeFamily.Safe);
        NetherMechanismValue skillCharge = NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.InitialSkillCharge,
            1,
            "native-skill-charge-count"
        );
        NetherMechanismValue mana = NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.SharedManaEnergy,
            9,
            "native-shared-mana-energy"
        );
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3)),
            smallerIdSkillCharge,
            largerIdMana
        ) with
        {
            MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
            {
                [smallerIdSkillCharge.CodeId] = skillCharge,
                [largerIdMana.CodeId] = mana,
            },
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(smallerIdSkillCharge.CodeId, 0)] = Mutation(
                    smallerIdSkillCharge.CodeId,
                    0,
                    before: [],
                    after: [],
                    mechanism: skillCharge
                ),
                [new NetherCodeMutationKey(largerIdMana.CodeId, 0)] = Mutation(
                    largerIdMana.CodeId,
                    0,
                    before: [],
                    after: [],
                    mechanism: mana
                ),
            },
        };

        NetherCodeDecision decision = Decide(
            Portfolio(),
            evidence,
            largerIdMana,
            smallerIdSkillCharge
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(smallerIdSkillCharge.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Survival_repair_precedes_back_force_chain_only_while_deficit_is_authoritative()
    {
        NetherCodeCandidate forceChain = Candidate(991224, NetherCodeFamily.Safe);
        NetherCodeCandidate survivalRepair = Candidate(991225, NetherCodeFamily.Safe);
        NetherMechanismValue forceValue = NetherMechanismValue.Qualitative(
            NetherMechanismQualitativePriority.BackForceChainHigh,
            "force-chain-completion-message"
        );
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3)),
            forceChain,
            survivalRepair
        ) with
        {
            MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
            {
                [forceChain.CodeId] = forceValue,
                [survivalRepair.CodeId] = KnownZeroMechanism(),
            },
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(forceChain.CodeId, 0)] = Mutation(
                    forceChain.CodeId,
                    0,
                    before: [],
                    after: [],
                    mechanism: forceValue
                ) with
                {
                    Survival = NetherSurvivalRepairEvidence.Known(
                        hasDeficit: true,
                        repairsDeficit: false
                    ),
                },
                [new NetherCodeMutationKey(survivalRepair.CodeId, 0)] = Mutation(
                    survivalRepair.CodeId,
                    0,
                    before: [],
                    after: [CombatWindow(survivalRepair.CodeId, value: 1)]
                ) with
                {
                    CombatTier = NetherEquipmentCombatTier.RearOrFullNonessentialDefense,
                    Survival = NetherSurvivalRepairEvidence.Known(
                        hasDeficit: true,
                        repairsDeficit: true
                    ),
                },
            },
        };

        NetherCodeDecision decision = Decide(Portfolio(), evidence, forceChain, survivalRepair);

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(survivalRepair.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Equipment_policy_consumes_native_critical_threshold_and_finite_continuous_ladder()
    {
        // Fresh native literals: CriticalRate.CalculateCritical is guaranteed at 999; continuous
        // attacks are instead bounded by the live maximum and decrement probability by 100.
        NetherCodeCandidate saturatedCritical = Candidate(991226, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate usefulCritical = Candidate(991227, NetherCodeFamily.Safe, power: 1);
        NetherCodeCandidate exhaustedContinuous = Candidate(991228, NetherCodeFamily.Safe);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3)),
            saturatedCritical,
            usefulCritical,
            exhaustedContinuous
        ) with
        {
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(saturatedCritical.CodeId, 0)] = Mutation(
                    saturatedCritical.CodeId,
                    0,
                    before: [],
                    after: []
                ) with
                {
                    NativeComparison = NetherNativeSpecialComparisonEvidence.Critical(
                        beforePermille: 999,
                        afterPermille: 1_099
                    ),
                },
                [new NetherCodeMutationKey(usefulCritical.CodeId, 0)] = Mutation(
                    usefulCritical.CodeId,
                    0,
                    before: [],
                    after: []
                ) with
                {
                    NativeComparison = NetherNativeSpecialComparisonEvidence.Critical(
                        beforePermille: 950,
                        afterPermille: 999
                    ),
                },
                [new NetherCodeMutationKey(exhaustedContinuous.CodeId, 0)] = Mutation(
                    exhaustedContinuous.CodeId,
                    0,
                    before: [],
                    after: []
                ) with
                {
                    NativeComparison = NetherNativeSpecialComparisonEvidence.Continuous(
                        beforePermille: 100,
                        afterPermille: 1_000,
                        liveMaximumCount: 0
                    ),
                },
            },
        };

        NetherCodeDecision critical = Decide(
            Portfolio(),
            evidence,
            saturatedCritical,
            usefulCritical
        );
        NetherCodeDecision continuous = Decide(Portfolio(), evidence, exhaustedContinuous);

        Assert.Equal(usefulCritical.CodeId, critical.SelectedCodeId);
        Assert.Equal(NetherCodeDecisionKind.Keep, continuous.Kind);
    }

    [Fact]
    public void Equipment_defense_order_uses_rear_coverage_before_weakest_and_aggregate_gain()
    {
        NetherCodeCandidate oneRear = Candidate(991229, NetherCodeFamily.Safe);
        NetherCodeCandidate twoRear = Candidate(991230, NetherCodeFamily.Safe);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3), Member(2, 1, 2, 3)),
            oneRear,
            twoRear
        ) with
        {
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(oneRear.CodeId, 0)] = Mutation(
                    oneRear.CodeId,
                    0,
                    before: [],
                    after: []
                ) with
                {
                    CombatTier = NetherEquipmentCombatTier.RearOrFullNonessentialDefense,
                    NativeComparison = NetherNativeSpecialComparisonEvidence.Defense(
                    [
                        new NetherCharacterEffectiveHpEvidence(
                            1,
                            NetherPartyPosition.Back,
                            100,
                            200,
                            IsKnown: true
                        ),
                    ]),
                },
                [new NetherCodeMutationKey(twoRear.CodeId, 0)] = Mutation(
                    twoRear.CodeId,
                    0,
                    before: [],
                    after: []
                ) with
                {
                    CombatTier = NetherEquipmentCombatTier.RearOrFullNonessentialDefense,
                    NativeComparison = NetherNativeSpecialComparisonEvidence.Defense(
                    [
                        new NetherCharacterEffectiveHpEvidence(
                            1,
                            NetherPartyPosition.Back,
                            100,
                            110,
                            IsKnown: true
                        ),
                        new NetherCharacterEffectiveHpEvidence(
                            2,
                            NetherPartyPosition.Back,
                            100,
                            110,
                            IsKnown: true
                        ),
                    ]),
                },
            },
        };

        NetherCodeDecision decision = Decide(Portfolio(), evidence, oneRear, twoRear);

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(twoRear.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Equipment_full_portfolio_requires_strict_complete_retained_improvement()
    {
        NetherCodeState held = Code(991230, NetherCodeFamily.Safe, power: 1);
        NetherCodeCandidate weaker = Candidate(991231, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate stronger = Candidate(991232, NetherCodeFamily.Safe, power: 1);
        NetherNativeBuffWindow heldWindow = CombatWindow(held.CodeId, value: 200);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 3)),
            held,
            weaker,
            stronger
        ) with
        {
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(weaker.CodeId, held.CodeId)] = Mutation(
                    weaker.CodeId,
                    held.CodeId,
                    before: [heldWindow],
                    after: [CombatWindow(weaker.CodeId, value: 100)]
                ),
                [new NetherCodeMutationKey(stronger.CodeId, held.CodeId)] = Mutation(
                    stronger.CodeId,
                    held.CodeId,
                    before: [heldWindow],
                    after: [CombatWindow(stronger.CodeId, value: 300)]
                ),
            },
        };
        NetherCodePortfolio portfolio = Portfolio(capacity: 1, current: [held]);

        NetherCodeDecision rejected = Decide(portfolio, evidence, weaker);
        NetherCodeDecision accepted = Decide(portfolio, evidence, stronger);

        Assert.Equal(NetherCodeDecisionKind.Keep, rejected.Kind);
        Assert.Equal(NetherCodeDecisionKind.Select, accepted.Kind);
        Assert.Equal(held.CodeId, accepted.RemoveCodeId);
    }

    [Fact]
    public void Research_rate_requires_exact_current_active_family_not_either_configured_family()
    {
        NetherCodeCandidate primaryRate = Candidate(991240, NetherCodeFamily.Rush);
        NetherCodeCandidate secondaryRate = Candidate(991241, NetherCodeFamily.Safe);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 2)),
            primaryRate,
            secondaryRate
        ) with
        {
            MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
            {
                [primaryRate.CodeId] = new() { IsKnown = true, ResearchRateOverwrite = 15 },
                [secondaryRate.CodeId] = new() { IsKnown = true, ResearchRateOverwrite = 15 },
            },
            Research = ResearchState(NetherCodeFamily.Rush, technologyRate: 10),
            ActiveResearchFamily = NetherCodeFamily.Rush,
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Rush,
            ResearchSecondaryFamily = NetherCodeFamily.Safe,
            CodeReloadReserve = 1,
        };

        NetherCodeDecision primaryActive = new NetherCodePolicy().Decide(
            Portfolio(),
            [secondaryRate],
            settings,
            evidence
        );
        NetherCodeDecision secondaryActive = new NetherCodePolicy().Decide(
            Portfolio(),
            [secondaryRate],
            settings,
            evidence with { ActiveResearchFamily = NetherCodeFamily.Safe }
        );
        NetherCodeDecision activeUnknown = new NetherCodePolicy().Decide(
            Portfolio(),
            [primaryRate],
            settings,
            evidence with { ActiveResearchFamily = NetherCodeFamily.Unknown }
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, primaryActive.Kind);
        Assert.Equal(NetherCodeDecisionKind.Select, secondaryActive.Kind);
        Assert.Equal(NetherCodeDecisionKind.Keep, activeUnknown.Kind);
    }

    [Fact]
    public void Current_risk_identifiers_are_characterization_fixtures_while_mechanics_remain_authority()
    {
        // Fresh Project.dll 53806a5b...1300 exposes BattleSituationAboveErosion.Percent;
        // current assets characterize 40010..40019 with a 70-percent gate. The policy consumes
        // the decoded relationship: an unrelated ID with the same mechanic is rejected, while a
        // current ID whose supplied current mechanic has no gate is not rejected by its ID alone.
        NetherCodeCandidate currentId = Candidate(40010, NetherCodeFamily.Risk);
        NetherCodeCandidate unrelatedId = Candidate(994010, NetherCodeFamily.Risk);
        NetherCodePolicyEvidence characterized = new()
        {
            MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
            {
                [currentId.CodeId] = new()
                {
                    IsKnown = true,
                    RiskRule = NetherCodeRiskRule.MinimumErosionSeventy,
                },
                [unrelatedId.CodeId] = new()
                {
                    IsKnown = true,
                    RiskRule = NetherCodeRiskRule.MinimumErosionSeventy,
                },
            },
            MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
            {
                [currentId.CodeId] = KnownZeroMechanism(),
                [unrelatedId.CodeId] = KnownZeroMechanism(),
            },
            EquipmentMutationValuesByKey = DefaultEquipmentMutations(currentId, unrelatedId),
            ActiveParty = Party(Member(1, 0, 2, 3)),
        };
        NetherCodePolicyEvidence changedMechanic = Evidence(
            currentId.CodeId,
            new NetherCodeHardEligibilityEvidence { IsKnown = true },
            characterized.ActiveParty!
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, Decide(Portfolio(), characterized, currentId).Kind);
        Assert.Equal(NetherCodeDecisionKind.Keep, Decide(Portfolio(), characterized, unrelatedId).Kind);
        Assert.Equal(NetherCodeDecisionKind.Select, Decide(Portfolio(), changedMechanic, currentId).Kind);
    }

    [Fact]
    public void Incompatible_count_five_is_repaired_below_threshold_or_pauses_before_combat()
    {
        NetherCodeState[] fiveRush =
        [
            Code(101, NetherCodeFamily.Rush),
            Code(102, NetherCodeFamily.Rush),
            Code(103, NetherCodeFamily.Rush),
            Code(104, NetherCodeFamily.Rush),
            Code(105, NetherCodeFamily.Rush),
        ];
        NetherCodeCandidate repair = Candidate(201, NetherCodeFamily.Safe);
        NetherCodeCandidate cannotRepair = Candidate(202, NetherCodeFamily.Impact);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 1, 2), Member(2, 1, 2, 3)),
            fiveRush.Cast<object>().Append(repair).Append(cannotRepair).ToArray()
        );

        NetherCodeDecision repaired = Decide(
            Portfolio(capacity: 5, current: fiveRush),
            evidence,
            repair
        );
        NetherCodeDecision paused = Decide(
            Portfolio(capacity: 5, current: fiveRush),
            evidence,
            cannotRepair
        );

        Assert.Equal(NetherCodeDecisionKind.Select, repaired.Kind);
        Assert.Equal(101, repaired.RemoveCodeId);
        Assert.Equal(NetherCodeDecisionKind.Pause, paused.Kind);
        Assert.Equal(NetherPauseReason.UnknownMasterData, paused.PauseReason);
    }

    [Fact]
    public void Research_contamination_retains_configured_side_even_when_equipment_value_prefers_opponent()
    {
        NetherCodeState rush = Code(10, NetherCodeFamily.Rush);
        NetherCodeState impact = Code(20, NetherCodeFamily.Impact);
        NetherCodeCandidate rushOffer = Candidate(11, NetherCodeFamily.Rush);
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(1, 0, 2, 2)),
            rush,
            impact,
            rushOffer
        ) with
        {
            ActiveResearchFamily = NetherCodeFamily.Rush,
            Research = ResearchState(NetherCodeFamily.Rush, technologyRate: 0),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Rush,
            CodeReloadReserve = 1,
        };

        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(capacity: 2, current: [rush, impact]),
            [rushOffer],
            settings,
            evidence
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(impact.CodeId, decision.RemoveCodeId);
    }

    [Fact]
    public void Effective_category_counts_count_cards_not_ability_level_power_or_possession_amount()
    {
        NetherCodeEffectiveLevels levels = NetherCodePolicy.CalculateEffectiveLevels(
            [
                Code(1, NetherCodeFamily.Safe, abilityLevel: 500, power: 900, possessionAmount: 999),
                Code(2, NetherCodeFamily.Risk, abilityLevel: 1, power: 1, possessionAmount: 1),
                Code(3, NetherCodeFamily.Rush, abilityLevel: 20),
                Code(4, NetherCodeFamily.Rush, abilityLevel: 1),
                Code(5, NetherCodeFamily.Impact, abilityLevel: 1),
            ]
        );

        Assert.Equal(0, levels.Safe);
        Assert.Equal(0, levels.Risk);
        Assert.Equal(1, levels.Rush);
        Assert.Equal(0, levels.Impact);
    }

    [Fact]
    public void Risk_family_is_not_globally_rejected()
    {
        NetherCodeDecision decision = Decide(
            Portfolio(),
            Candidate(40024, NetherCodeFamily.Risk, power: 500)
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(40024, decision.SelectedCodeId);
    }

    [Fact]
    public void Safe_and_risk_are_ranked_as_peer_families_when_evidence_is_equal()
    {
        NetherCodeDecision decision = Decide(
            Portfolio(),
            Candidate(20, NetherCodeFamily.Safe, power: 5),
            Candidate(10, NetherCodeFamily.Risk, power: 5)
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(10, decision.SelectedCodeId);
    }

    [Fact]
    public void Reload_reserve_is_honored_when_no_new_candidate_exists()
    {
        NetherCodeState existing = Code(1, NetherCodeFamily.Safe);
        NetherCodeDecision reload = Decide(
            Portfolio(reloadCount: 2, current: [existing]),
            Candidate(1, NetherCodeFamily.Safe)
        );
        NetherCodeDecision keep = Decide(
            Portfolio(reloadCount: 1, current: [existing]),
            Candidate(1, NetherCodeFamily.Safe)
        );

        Assert.Equal(NetherCodeDecisionKind.Reload, reload.Kind);
        Assert.Equal(NetherCodeDecisionKind.Keep, keep.Kind);
    }

    [Fact]
    public void Duplicate_offer_is_not_assigned_an_unproven_stack_value_even_when_inventory_has_space()
    {
        NetherCodeState existing = Code(1, NetherCodeFamily.Safe, possessionAmount: 2);

        NetherCodeDecision decision = Decide(
            Portfolio(capacity: 5, reloadCount: 1, current: [existing]),
            Candidate(1, NetherCodeFamily.Safe, power: 999, coverage: 99)
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
        Assert.Equal(0, decision.SelectedCodeId);
    }

    [Fact]
    public void Unknown_effect_semantics_do_not_erase_a_proven_category_card()
    {
        NetherCodeCandidate candidate = Candidate(12, NetherCodeFamily.Impact) with
        {
            EffectSemanticsKnown = false,
            MasterEffectType = (NetherCodeMasterEffectType)12,
        };

        NetherCodeDecision decision = Decide(Portfolio(), candidate);

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(12, decision.SelectedCodeId);
    }

    [Fact]
    public void Missing_master_over_capacity_or_unknown_family_pauses()
    {
        NetherCodeDecision missingMaster = Decide(
            Portfolio(masterComplete: false),
            Candidate(1, NetherCodeFamily.Safe)
        );
        NetherCodeDecision overCapacity = Decide(
            Portfolio(capacity: 1, current: [Code(1, NetherCodeFamily.Rush), Code(2, NetherCodeFamily.Impact)]),
            Candidate(3, NetherCodeFamily.Safe)
        );
        NetherCodeDecision unknown = Decide(
            Portfolio(),
            Candidate(4, NetherCodeFamily.Unknown)
        );

        Assert.Equal(NetherPauseReason.UnknownMasterData, missingMaster.PauseReason);
        Assert.Equal(NetherPauseReason.UnknownMasterData, overCapacity.PauseReason);
        Assert.Equal(NetherPauseReason.UnknownMasterData, unknown.PauseReason);
    }

    private static NetherCodeDecision Decide(
        NetherCodePortfolio portfolio,
        params NetherCodeCandidate[] candidates
    ) => Decide(portfolio, NetherCombatLane.Auto, candidates);

    private static NetherCodeDecision Decide(
        NetherCodePortfolio portfolio,
        NetherCombatLane lane,
        params NetherCodeCandidate[] candidates
    )
    {
        object[] codeRows = portfolio.CurrentCodes.Cast<object>()
            .Concat(candidates.Cast<object>())
            .ToArray();
        NetherCodePolicyEvidence evidence = KnownEvidence(
            Party(Member(99, 0, 2, 2)),
            codeRows
        );
        return new NetherCodePolicy().Decide(
            portfolio,
            candidates,
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                CombatLane = lane,
                CodeReloadReserve = 1,
            },
            evidence
        );
    }

    private static NetherCodeDecision Decide(
        NetherCodePortfolio portfolio,
        NetherCodePolicyEvidence evidence,
        params NetherCodeCandidate[] candidates
    ) => new NetherCodePolicy().Decide(
        portfolio,
        candidates,
        new NetherAutoClimbSettings { CodeReloadReserve = 1 },
        evidence
    );

    private static NetherCodePolicyEvidence Evidence(
        long codeId,
        NetherCodeHardEligibilityEvidence mechanic,
        IReadOnlyList<NetherStrategyPartyMember> party
    ) => new()
    {
        MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
        {
            [codeId] = mechanic,
        },
        MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
        {
            [codeId] = KnownZeroMechanism(),
        },
        EquipmentMutationValuesByKey = DefaultEquipmentMutations(
            Candidate(codeId, NetherCodeFamily.Safe)
        ),
        ActiveParty = party,
    };

    private static NetherCodePolicyEvidence KnownEvidence(
        IReadOnlyList<NetherStrategyPartyMember> party,
        params object[] codes
    )
    {
        long[] codeIds = codes.Select(CodeIdOf).Distinct().ToArray();
        return new NetherCodePolicyEvidence
        {
            MechanicsByCodeId = codeIds.ToDictionary(
                codeId => codeId,
                _ => new NetherCodeHardEligibilityEvidence { IsKnown = true }
            ),
            MechanismValuesByCodeId = codeIds.ToDictionary(
                codeId => codeId,
                _ => KnownZeroMechanism()
            ),
            EquipmentMutationValuesByKey = DefaultEquipmentMutations(codes),
            ActiveParty = party,
        };
    }

    private static IReadOnlyDictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
        DefaultEquipmentMutations(params object[] codes)
    {
        NetherCodeState[] states = codes
            .OfType<NetherCodeState>()
            .GroupBy(code => code.CodeId)
            .Select(group => group.First())
            .ToArray();
        NetherCodeCandidate[] candidates = codes
            .OfType<NetherCodeCandidate>()
            .GroupBy(code => code.CodeId)
            .Select(group => group.First())
            .ToArray();
        NetherNativeBuffWindow[] before = states
            .Select(state => CombatWindow(state.CodeId, value: 50))
            .ToArray();
        var values = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>();
        foreach (NetherCodeCandidate candidate in candidates)
        {
            values[new NetherCodeMutationKey(candidate.CodeId, 0)] = Mutation(
                candidate.CodeId,
                0,
                before,
                before.Append(CombatWindow(candidate.CodeId, value: 100)).ToArray()
            );
            foreach (NetherCodeState removal in states)
            {
                values[new NetherCodeMutationKey(candidate.CodeId, removal.CodeId)] = Mutation(
                    candidate.CodeId,
                    removal.CodeId,
                    before,
                    states
                        .Where(state => state.CodeId != removal.CodeId)
                        .Select(state => CombatWindow(state.CodeId, value: 50))
                        .Append(CombatWindow(candidate.CodeId, value: 100))
                        .ToArray()
                );
            }
        }
        return values;
    }

    private static long CodeIdOf(object code) => code switch
    {
        NetherCodeState state => state.CodeId,
        NetherCodeCandidate candidate => candidate.CodeId,
        _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
    };

    private static NetherMechanismValue KnownZeroMechanism() =>
        NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.None,
            0,
            "fixture-known-zero-marginal-value"
        );

    private static NetherCodeEquipmentMutationEvidence Mutation(
        long candidateCodeId,
        long removeCodeId,
        IReadOnlyList<NetherNativeBuffWindow> before,
        IReadOnlyList<NetherNativeBuffWindow> after,
        NetherMechanismValue? mechanism = null
    ) => new(
        candidateCodeId,
        removeCodeId,
        new NetherNativePortfolioComparisonInput(before, after, BossDurationSeconds: 10),
        mechanism ?? KnownZeroMechanism()
    )
    {
        CombatTier = NetherEquipmentCombatTier.RearOrFullOffense,
        Survival = NetherSurvivalRepairEvidence.Known(
            hasDeficit: false,
            repairsDeficit: false
        ),
        MechanismPortfolio = NetherMechanismPortfolioComparisonEvidence.Known(
            [],
            [new NetherMechanismPortfolioEntry(
                candidateCodeId,
                mechanism ?? KnownZeroMechanism()
            )]
        ),
        RecipientPositions = new Dictionary<long, NetherPartyPosition>
        {
            [100] = NetherPartyPosition.Back,
        },
    };

    private static NetherNativeBuffWindow CombatWindow(long codeId, int value) => new(
        codeId,
        RecipientCharacterId: 100,
        new NetherStrategyBuffType(10),
        NetherStrategyBuffEffectKind.Buff,
        NetherStrategyBuffCoexistenceKind.Allow,
        NetherCombatMetricKind.Attack,
        value,
        StartSecond: 0,
        DurationSeconds: 10
    );

    private static IReadOnlyList<NetherStrategyResearchFamilyState> ResearchState(
        NetherCodeFamily activeFamily,
        int technologyRate
    ) => new[]
    {
        ResearchFamily(NetherCodeFamily.Rush, activeFamily, technologyRate),
        ResearchFamily(NetherCodeFamily.Impact, activeFamily, technologyRate),
        ResearchFamily(NetherCodeFamily.Safe, activeFamily, technologyRate),
        ResearchFamily(NetherCodeFamily.Risk, activeFamily, technologyRate),
    };

    private static NetherStrategyResearchFamilyState ResearchFamily(
        NetherCodeFamily family,
        NetherCodeFamily activeFamily,
        int technologyRate
    ) => new(family, 0, 0, family == activeFamily ? technologyRate : 0);

    private static IReadOnlyList<NetherStrategyPartyMember> Party(
        params NetherStrategyPartyMember[] members
    ) => members;

    private static NetherStrategyPartyMember Member(
        long id,
        int partyIndex,
        int position,
        int manaType
    ) => new(
        id,
        partyIndex,
        position switch
        {
            1 => NetherPartyPosition.Forward,
            2 => NetherPartyPosition.Back,
            3 => NetherPartyPosition.Assist,
            _ => NetherPartyPosition.Unknown,
        },
        1,
        manaType switch
        {
            1 => NetherCrestIdentity.General,
            2 => NetherCrestIdentity.Passion,
            3 => NetherCrestIdentity.Impact,
            _ => NetherCrestIdentity.Unknown,
        },
        1000,
        true,
        1,
        0
    );

    private static NetherCodePortfolio Portfolio(
        int capacity = 5,
        int reloadCount = 1,
        bool masterComplete = true,
        NetherCombatLane? lockedLane = null,
        IReadOnlyList<NetherCodeState>? current = null
    ) => new()
    {
        Capacity = capacity,
        ReloadCount = reloadCount,
        IsMasterComplete = masterComplete,
        LockedLane = lockedLane,
        CurrentCodes = current ?? [],
    };

    private static NetherCodeState Code(
        long id,
        NetherCodeFamily family,
        int abilityLevel = 1,
        int rarity = 0,
        int power = 0,
        int coverage = 0,
        int possessionAmount = 1,
        bool coverageKnown = true
    ) => new(id, family, abilityLevel)
    {
        Category = Category(family),
        Rarity = rarity,
        Power = power,
        PossessionAmount = possessionAmount,
        PartyCoverageKnown = coverageKnown,
        PartyCoverage = coverage,
    };

    private static NetherCodeCandidate Candidate(
        long id,
        NetherCodeFamily family,
        int abilityLevel = 1,
        int rarity = 0,
        int power = 0,
        int coverage = 0,
        bool coverageKnown = true
    ) => new(id, family, abilityLevel)
    {
        Category = Category(family),
        Rarity = rarity,
        Power = power,
        PartyCoverageKnown = coverageKnown,
        PartyCoverage = coverage,
    };

    private static NetherCodeCategory Category(NetherCodeFamily family) => family switch
    {
        NetherCodeFamily.Rush => NetherCodeCategory.Rush,
        NetherCodeFamily.Impact => NetherCodeCategory.Impact,
        NetherCodeFamily.Safe => NetherCodeCategory.Safe,
        NetherCodeFamily.Risk => NetherCodeCategory.Risk,
        _ => NetherCodeCategory.Unknown,
    };
}
