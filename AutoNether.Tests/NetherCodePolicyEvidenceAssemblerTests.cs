using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodePolicyEvidenceAssemblerTests
{
    [Fact]
    public void Production_assembler_carries_future_typed_research_rate_overwrite_into_hard_eligibility()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate candidate = Candidate(88001, NetherCodeFamily.Rush, power: 1);
        NetherStrategyNativeMechanic mechanic = OrdinaryMechanic(candidate.CodeId) with
        {
            ResearchRateOverwrite = NetherStrategyResearchRateOverwriteEvidence.Known(
                NetherCodeFamily.Rush,
                15
            ),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, primaryWallet: 0, primaryProjection: 0),
            snapshot,
            [candidate],
            [mechanic],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Rush,
            },
            SafeRouteEvidence()
        );

        Assert.Equal(
            15,
            captured.Evidence!.MechanicsByCodeId[candidate.CodeId].ResearchRateOverwrite
        );
    }

    [Fact]
    public void Production_assembler_keeps_unproven_research_rate_candidate_local_and_fail_closed()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate candidate = Candidate(88002, NetherCodeFamily.Impact, power: 1);
        NetherStrategyNativeMechanic mechanic = OrdinaryMechanic(candidate.CodeId) with
        {
            ResearchRateOverwrite = NetherStrategyResearchRateOverwriteEvidence.Unknown(
                NetherCodeFamily.Impact,
                "future-selectable-research-rate-not-proven"
            ),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, primaryWallet: 0, primaryProjection: 0),
            snapshot,
            [candidate],
            [mechanic],
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Research },
            SafeRouteEvidence()
        );

        NetherCodeHardEligibilityEvidence evidence =
            captured.Evidence!.MechanicsByCodeId[candidate.CodeId];
        Assert.False(evidence.IsKnown);
        Assert.Equal("future-selectable-research-rate-not-proven", evidence.UnknownReason);
    }

    [Fact]
    public void Production_route_evidence_uses_only_confirmed_combat_start_erosion()
    {
        // Fresh Project.dll 53806a5b...1300: MNetherMapFloors exposes the typed floor kind;
        // battle-start erosion is consumed by BattleSituation erosion triggers. A preceding
        // Recovery audit is not a battle start and therefore cannot lower the conditional range.
        NetherFloorNode recovery = new(99001, 11, 0, NetherFloorNodeType.Recovery)
        {
            NodeId = 99101,
            IsUnlocked = true,
        };
        NetherFloorNode battle = new(99002, 12, 0, NetherFloorNodeType.Battle)
        {
            NodeId = 99102,
            IsUnlocked = true,
            PreviousFloorIds = [recovery.NodeId],
        };
        NetherSnapshot snapshot = Snapshot() with { Floors = [recovery, battle] };
        NetherRouteHorizonSafetyEvaluation horizon = new()
        {
            IsEligible = true,
            PeakErosion = 55,
            HasConfirmedRecoveryToOperatingBand = true,
            Steps =
            [
                new NetherRouteHorizonStepAudit(recovery.NodeId, 40, 40, 900),
                new NetherRouteHorizonStepAudit(battle.NodeId, 50, 55, 900),
            ],
        };
        NetherProductionRouteSafetyPlan plan = new()
        {
            Route = new NetherRoutePlan { SelectedNode = recovery },
            Context = new NetherRouteSafetyContext
            {
                HorizonEvaluationByFloorId = new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [recovery.NodeId] = horizon,
                },
            },
        };

        NetherCodePolicyRouteEvidence evidence = NetherCodePolicyRouteEvidenceMapper.Map(
            snapshot,
            plan
        );

        Assert.True(evidence.IsKnown, evidence.UnknownReason);
        Assert.Equal(50, evidence.MinimumBattleStartErosion);
        Assert.Equal(50, evidence.MaximumBattleStartErosion);
        Assert.True(evidence.RecoverableToFiftySeventyBand);
        NetherConfirmedCombatErosion combat = Assert.Single(evidence.ConfirmedCombats);
        Assert.Equal(battle.NodeId, combat.FloorId);
        Assert.Equal(500, combat.ProjectedErosionPermille);
    }

    [Fact]
    public void Production_route_evidence_does_not_invent_recovery_from_in_band_combats()
    {
        // Fresh Project.dll 53806a5b...1300: BattleSituationAboveErosion evaluates only the
        // current erosion threshold. The strategic <=70 ceiling additionally requires the T03
        // horizon's explicit recovery proof; merely observing an in-band combat is not recovery.
        NetherFloorNode battle = new(99003, 12, 0, NetherFloorNodeType.Battle)
        {
            NodeId = 99103,
            IsUnlocked = true,
        };
        NetherSnapshot snapshot = Snapshot() with { Floors = [battle] };
        NetherRouteHorizonSafetyEvaluation horizon = new()
        {
            IsEligible = true,
            PeakErosion = 65,
            HasConfirmedRecoveryToOperatingBand = false,
            Steps = [new NetherRouteHorizonStepAudit(battle.NodeId, 55, 65, 900)],
        };
        NetherProductionRouteSafetyPlan plan = new()
        {
            Route = new NetherRoutePlan { SelectedNode = battle },
            Context = new NetherRouteSafetyContext
            {
                HorizonEvaluationByFloorId = new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [battle.NodeId] = horizon,
                },
            },
        };

        NetherCodePolicyRouteEvidence evidence = NetherCodePolicyRouteEvidenceMapper.Map(snapshot, plan);

        Assert.True(evidence.IsKnown, evidence.UnknownReason);
        Assert.Equal(55, evidence.MinimumBattleStartErosion);
        Assert.Equal(55, evidence.MaximumBattleStartErosion);
        Assert.False(evidence.RecoverableToFiftySeventyBand);
    }

    [Fact]
    public void Production_route_evidence_resolves_boss_duration_through_exact_master_relations()
    {
        // Fresh Project.dll 53806a5b...1300: NetherFloorModel.CreateModel resolves a Boss's
        // MNetherFloorBattles row by m_nether_map_floor_id, then its MNetherBattleStages row by
        // m_nether_battle_stage_id. The deliberately different node/floor/battle/stage IDs make
        // accidental ID reuse fail this behavior test.
        NetherFloorNode boss = new(71_001, 70, 0, NetherFloorNodeType.Boss)
        {
            NodeId = 81_001,
            IsUnlocked = true,
        };
        NetherSnapshot snapshot = Snapshot() with { Floors = [boss] };
        NetherRouteHorizonSafetyEvaluation horizon = new()
        {
            IsEligible = true,
            PeakErosion = 60,
            HasConfirmedRecoveryToOperatingBand = true,
            MinimumActiveCharacterHpPermille = 900,
            Steps = [new NetherRouteHorizonStepAudit(boss.NodeId, 55, 60, 900)],
        };
        NetherProductionRouteSafetyPlan plan = new()
        {
            Route = new NetherRoutePlan { SelectedNode = boss },
            Context = new NetherRouteSafetyContext
            {
                HorizonEvaluationByFloorId = new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [boss.NodeId] = horizon,
                },
            },
        };

        NetherCodePolicyRouteEvidence evidence = NetherCodePolicyRouteEvidenceMapper.Map(
            snapshot,
            plan,
            [
                new NetherStrategyBattleMasterRow(91_001, boss.FloorId, 3, 101_001, 500),
                new NetherStrategyBattleMasterRow(91_002, boss.NodeId, 3, 101_002, 500),
            ],
            [
                new NetherCodePolicyBattleStageRow(101_001, 37),
                new NetherCodePolicyBattleStageRow(101_002, 99),
            ]
        );

        Assert.True(evidence.IsKnown, evidence.UnknownReason);
        Assert.True(evidence.BossDurationKnown, evidence.BossDurationUnknownReason);
        Assert.Equal(37, evidence.BossDurationSeconds);
    }

    [Fact]
    public void Production_assembler_fails_closed_when_selected_route_survival_baseline_is_not_exact()
    {
        // T03's authoritative horizon owns the post-cost active-party HP minimum. Code policy may
        // reuse that exact aggregate, but cannot replace a missing value with a fabricated
        // "no deficit" result.
        NetherFloorNode battle = new(99011, 12, 0, NetherFloorNodeType.Battle)
        {
            NodeId = 99111,
            IsUnlocked = true,
        };
        NetherSnapshot snapshot = Snapshot() with { Floors = [battle] };
        NetherRouteHorizonSafetyEvaluation horizon = new()
        {
            IsEligible = true,
            PeakErosion = 55,
            MinimumActiveCharacterHpPermille = null,
            Steps = [new NetherRouteHorizonStepAudit(battle.NodeId, 50, 55, 900)],
        };
        NetherProductionRouteSafetyPlan plan = new()
        {
            Route = new NetherRoutePlan { SelectedNode = battle },
            Context = new NetherRouteSafetyContext
            {
                HorizonEvaluationByFloorId = new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [battle.NodeId] = horizon,
                },
            },
        };
        NetherCodePolicyRouteEvidence route = NetherCodePolicyRouteEvidenceMapper.Map(
            snapshot,
            plan
        );
        NetherCodeCandidate candidate = Candidate(88111, NetherCodeFamily.Safe, power: 99_999);
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [candidate],
            [AttackMechanic(candidate.CodeId, 500)],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                CodeReloadReserve = 1,
            },
            route
        );

        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                CodeReloadReserve = 1,
            },
            captured.Evidence!
        );

        Assert.True(route.IsKnown, route.UnknownReason);
        Assert.False(route.SurvivalBaselineKnown);
        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
    }

    [Fact]
    public void Production_assembler_preserves_proven_survival_deficit_when_offer_cannot_prove_repair()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // NetherUpdateEventResponseEntity.t_nether_characters and
        // NetherClearBattleResponseEntity.t_nether_characters are the authoritative post-action
        // HP rows. NetherPartyModel.UpdateCharacterStatuses applies those server ratios by
        // character id. Future combat damage runs through UnitDamageCalculator (including its
        // live RandomModifier), so an offer-side MaxHP/Defence buff cannot prove that it repairs a
        // deficit already established by the selected route horizon at this lifecycle.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate maxHp = Candidate(88112, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate defense = Candidate(88113, NetherCodeFamily.Safe, power: 99_999);
        NetherCodePolicyRouteEvidence deficit = SafeRouteEvidence() with
        {
            HasSurvivalDeficit = true,
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithDefense(snapshot),
            snapshot,
            [maxHp, defense],
            [
                OrdinaryBuffMechanic(
                    maxHp.CodeId,
                    80,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    500
                ),
                DefenseMechanic(defense.CodeId, additionPermille: 500),
            ],
            settings,
            deficit
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        foreach (NetherCodeCandidate candidate in new[] { maxHp, defense })
        {
            NetherSurvivalRepairEvidence survival = captured.Evidence!
                .EquipmentMutationValuesByKey[new NetherCodeMutationKey(candidate.CodeId, 0)]
                .Survival;
            Assert.False(survival.IsKnown);
            Assert.True(survival.HasDeficit);
            Assert.Equal(
                "survival-repair-proof-unavailable:server-authoritative-event-or-battle-result",
                survival.UnknownReason
            );
            NetherCodeDecision decision = new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [candidate],
                settings,
                captured.Evidence
            );
            Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
        }
    }

    [Fact]
    public void Production_conditional_risk_consumes_exact_selected_route_horizon_candidate_locally()
    {
        // Fresh Project.dll 53806a5b...1300 and current m_nether_codes zh-Hant assets:
        // current Risk cards 40022/40023 are StartBattle + AboveErosion(50) + ChargeMana(5)
        // effects. There is no native BelowErosion(70) situation; 70 is the strategic route ceiling.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate candidate = Candidate(40022, NetherCodeFamily.Risk, power: 99_999);
        NetherStrategyNativeMechanic mechanic = ConditionalRiskManaMechanic(candidate.CodeId);
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Risk,
            CodeReloadReserve = 1,
        };
        NetherRuntimeCodePolicyEvidenceResult safe = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [candidate],
            [mechanic],
            settings,
            SafeRouteEvidence()
        );
        NetherRuntimeCodePolicyEvidenceResult noRecovery = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [candidate],
            [mechanic],
            settings,
            SafeRouteEvidence() with { RecoverableToFiftySeventyBand = false }
        );
        NetherRuntimeCodePolicyEvidenceResult unsafeRoute = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [candidate],
            [mechanic],
            settings,
            SafeRouteEvidence() with { MaximumBattleStartErosion = 71 }
        );
        NetherRuntimeCodePolicyEvidenceResult unknownRoute = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [candidate],
            [mechanic],
            settings,
            NetherCodePolicyRouteEvidence.Unknown("selected-route-horizon-unavailable")
        );

        Assert.Equal(
            NetherCodeDecisionKind.Select,
            new NetherCodePolicy().Decide(Portfolio(snapshot), [candidate], settings, safe.Evidence!).Kind
        );
        Assert.Equal(
            NetherCodeRiskRule.ConditionalFiftyToSeventy,
            safe.Evidence!.MechanicsByCodeId[candidate.CodeId].RiskRule
        );
        Assert.Equal(
            NetherCodeDecisionKind.Reload,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [candidate],
                settings,
                noRecovery.Evidence!
            ).Kind
        );
        Assert.Equal(
            NetherCodeDecisionKind.Reload,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [candidate],
                settings,
                unsafeRoute.Evidence!
            ).Kind
        );
        Assert.Equal(
            NetherCodeDecisionKind.Reload,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [candidate],
                settings,
                unknownRoute.Evidence!
            ).Kind
        );
    }

    [Fact]
    public void Production_assembler_maps_each_supported_ordinary_native_metric_exactly()
    {
        // Fresh Project.dll 53806a5b...1300 BuffType literals and
        // BuffParameterByTypeExtension.ToBuffParam branches: Attack/MaxHp use RatePermille;
        // Damage/TakenDamage/DebuffResist/element-target damage use FixedPermille. These typed
        // parameters, recipients and HigherValue coexistence—not displayed Power—form the window.
        var rows = new[]
        {
            (Candidate(88301, NetherCodeFamily.Safe, power: 99_999),
                OrdinaryBuffMechanic(88301, 10, NetherStrategyBuffParameterReferenceKind.RatePermille, 111),
                NetherCombatMetricKind.Attack, 111),
            (Candidate(88302, NetherCodeFamily.Safe, power: 1),
                OrdinaryBuffMechanic(88302, 80, NetherStrategyBuffParameterReferenceKind.RatePermille, 222),
                NetherCombatMetricKind.MaxHp, 222),
            (Candidate(88303, NetherCodeFamily.Safe, power: 1),
                OrdinaryBuffMechanic(88303, 52, NetherStrategyBuffParameterReferenceKind.FixedPermille, 333),
                NetherCombatMetricKind.TakenDamage, 333),
            (Candidate(88304, NetherCodeFamily.Safe, power: 1),
                OrdinaryBuffMechanic(88304, 41, NetherStrategyBuffParameterReferenceKind.FixedPermille, 444),
                NetherCombatMetricKind.DamageModifier, 444),
            (Candidate(88305, NetherCodeFamily.Safe, power: 1),
                OrdinaryBuffMechanic(88305, 200, NetherStrategyBuffParameterReferenceKind.FixedPermille, 555),
                NetherCombatMetricKind.Resistance, 555),
            (Candidate(88306, NetherCodeFamily.Safe, power: 1),
                OrdinaryBuffMechanic(88306, 3002, NetherStrategyBuffParameterReferenceKind.FixedPermille, 666),
                NetherCombatMetricKind.ElementDamage, 666),
        };
        NetherSnapshot snapshot = Snapshot();
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithDefense(snapshot),
            snapshot,
            rows.Select(row => row.Item1).ToArray(),
            rows.Select(row => row.Item2).ToArray(),
            settings,
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        foreach (var (candidate, _, metric, value) in rows)
        {
            NetherCodeEquipmentMutationEvidence mutation =
                captured.Evidence!.EquipmentMutationValuesByKey[
                    new NetherCodeMutationKey(candidate.CodeId, 0)
                ];
            NetherNativeBuffWindow window = Assert.Single(mutation.NativePortfolio.AfterWindows);
            Assert.Equal(metric, window.Metric);
            Assert.Equal(value, window.ValuePermille);
            Assert.Equal(
                NetherCodeDecisionKind.Select,
                new NetherCodePolicy().Decide(
                    Portfolio(snapshot),
                    [candidate],
                    settings,
                    captured.Evidence
                ).Kind
            );
        }
    }

    [Fact]
    public void Production_assembler_uses_native_delayed_repeat_windows_over_the_boss_reference()
    {
        // Fresh GameAssembly 573fa800...c1fb / Project.dll 53806a5b...1300:
        // BattleTriggerSituationService converts BattleSituationDuration.MilliSec with 0.001f,
        // BattleTriggerTimer completes at the interval and ResetInternal subtracts that interval,
        // making it repeatable. AbilityEffectParameterBuff.EndSituation(situation=Duration,value)
        // supplies the applied buff lifetime. MNetherBattleStages.time_limit is the exact Boss
        // comparison horizon. These independent literals make the recurring 300x5x2 exposure
        // exceed the one-shot 400x5 exposure; the former (0,1) production hardcode cannot do so.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate oneShot = Candidate(88311, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate recurring = Candidate(88312, NetherCodeFamily.Safe, power: 1);
        NetherCodePolicyRouteEvidence route = SafeRouteEvidence() with
        {
            BossDurationKnown = true,
            BossDurationSeconds = 30,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [oneShot, recurring],
            [
                TimedAttackMechanic(
                    oneShot.CodeId,
                    NetherStrategyTriggerKind.StartBattle,
                    triggerMilliSeconds: 0,
                    buffDurationMilliSeconds: 5_000,
                    valuePermille: 400
                ),
                TimedAttackMechanic(
                    recurring.CodeId,
                    NetherStrategyTriggerKind.Duration,
                    triggerMilliSeconds: 10_000,
                    buffDurationMilliSeconds: 5_000,
                    valuePermille: 300
                ),
            ],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                CodeReloadReserve = 1,
            },
            route
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [oneShot, recurring],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                CodeReloadReserve = 1,
            },
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherNativePortfolioComparisonInput oneShotTimeline = captured.Evidence!
            .EquipmentMutationValuesByKey[new NetherCodeMutationKey(oneShot.CodeId, 0)]
            .NativePortfolio;
        NetherNativePortfolioComparisonInput recurringTimeline = captured.Evidence
            .EquipmentMutationValuesByKey[new NetherCodeMutationKey(recurring.CodeId, 0)]
            .NativePortfolio;
        Assert.Equal(30, oneShotTimeline.BossDurationSeconds);
        NetherNativeBuffWindow oneShotWindow = Assert.Single(oneShotTimeline.AfterWindows);
        Assert.Equal((0, 5), (oneShotWindow.StartSecond, oneShotWindow.DurationSeconds));
        Assert.Equal([10, 20], recurringTimeline.AfterWindows.Select(row => row.StartSecond));
        Assert.All(recurringTimeline.AfterWindows, row => Assert.Equal(5, row.DurationSeconds));
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(recurring.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_keeps_unsupported_live_target_filter_candidate_local_unknown()
    {
        // Fresh Project.dll 53806a5b...1300: BuffTargetFilter.IsMatchTarget evaluates live
        // RequiredBuffTypes/weakness/union/job/species/size relationships. The immutable Code-offer
        // party evidence has none of those live relationships, so treating the filter as false
        // would fabricate "no recipient". Only the dependent candidate must fail closed.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate filtered = Candidate(88321, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate exact = Candidate(88322, NetherCodeFamily.Safe, power: 1);
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [filtered, exact],
            [AttackMechanicWithUnsupportedTarget(filtered.CodeId, 900), AttackMechanic(exact.CodeId, 100)],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                CodeReloadReserve = 1,
            },
            SafeRouteEvidence()
        );
        NetherCodeEquipmentMutationEvidence filteredMutation = captured.Evidence!
            .EquipmentMutationValuesByKey[new NetherCodeMutationKey(filtered.CodeId, 0)];
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [filtered, exact],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                CodeReloadReserve = 1,
            },
            captured.Evidence
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCombatValueEvidenceKind.ReachableUnquantified, filteredMutation.MechanismValue.Kind);
        Assert.Contains("native-target-filter-live-relationship-unavailable", filteredMutation.MechanismValue.Detail);
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(exact.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_rejects_unknown_target_parameters_and_flag_bits_candidate_locally()
    {
        // Fresh Project.dll 53806a5b...1300: AbilityTargetFriend.Param and BuffTargetFilter
        // preserve native typed flag domains. Parameters unavailable at capture time, a flag bit
        // outside the current enum, or a filter bit outside ElementTypeFlag cannot become a proven
        // no-recipient result. Each dependent row is unknown while an exact sibling remains usable.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate parametersUnknown = Candidate(883210, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate targetFlagsUnknown = Candidate(883211, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate filterFlagsUnknown = Candidate(883212, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate exact = Candidate(883213, NetherCodeFamily.Safe, 1);
        NetherStrategyNativeMechanic first = AttackMechanic(parametersUnknown.CodeId, 900);
        first = first with
        {
            Target = first.Target with
            {
                ParametersKnown = false,
                UnknownReason = "native-target-parameters-unavailable",
            },
        };
        NetherStrategyNativeMechanic second = AttackMechanic(targetFlagsUnknown.CodeId, 900);
        second = second with
        {
            Target = second.Target with
            {
                PartyPositionFlags = NetherPartyPositionFlags.Back
                    | (NetherPartyPositionFlags)16,
            },
        };
        NetherStrategyNativeMechanic third = AttackMechanic(filterFlagsUnknown.CodeId, 900);
        NetherStrategyBuffParameterEvidence thirdParameter = Assert.Single(
            third.AbilityEffect.BuffParameters
        );
        third = third with
        {
            AbilityEffect = third.AbilityEffect with
            {
                BuffParameters =
                [
                    thirdParameter with
                    {
                        TargetFilter = new NetherStrategyBuffTargetFilterEvidence(
                            IgnoreDeadUnit: true,
                            ElementTypeFlags: 128,
                            ElementWeakTypeFlags: 0,
                            PartyPositionFlags: NetherPartyPositionFlags.None,
                            UnionTypeFlags: 0,
                            JobGroupFlags: 0,
                            JobSpeciesFlags: 0,
                            CharacterSizeFlags: 0,
                            RequiredBuffTypes: []
                        ),
                    },
                ],
            },
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [parametersUnknown, targetFlagsUnknown, filterFlagsUnknown, exact],
            [first, second, third, AttackMechanic(exact.CodeId, 100)],
            settings,
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        foreach (NetherCodeCandidate candidate in new[]
                 { parametersUnknown, targetFlagsUnknown, filterFlagsUnknown })
        {
            NetherMechanismValue value = captured.Evidence!.MechanismValuesByCodeId[
                candidate.CodeId
            ];
            Assert.Equal(NetherCombatValueEvidenceKind.Missing, value.Kind);
            Assert.Contains("target", value.Detail);
        }
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [parametersUnknown, targetFlagsUnknown, filterFlagsUnknown, exact],
            settings,
            captured.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(exact.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_matches_the_selected_buff_parameter_filter_not_the_first_parameter()
    {
        // Fresh Project.dll 53806a5b...1300: AbilityPassiveBuff.TryGetTargetFilterInternal
        // enumerates BuffParameterByType, matches queryBuffType, then returns that exact entry's
        // BuffTargetFilter. The unrelated first entry must not poison the selected Attack entry.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate selectedParameter = Candidate(88323, NetherCodeFamily.Safe, power: 1);
        NetherCodeCandidate exactSibling = Candidate(88324, NetherCodeFamily.Safe, power: 1);
        NetherStrategyNativeMechanic mechanic = AttackMechanic(selectedParameter.CodeId, 200);
        NetherStrategyBuffParameterEvidence attack = Assert.Single(
            mechanic.AbilityEffect.BuffParameters
        );
        NetherStrategyBuffParameterEvidence unrelated = attack with
        {
            BuffType = new NetherStrategyBuffType(777),
            TargetFilter = UnsupportedLiveFilter(),
        };
        mechanic = mechanic with
        {
            AbilityEffect = mechanic.AbilityEffect with
            {
                BuffParameters = [unrelated, attack],
            },
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [selectedParameter, exactSibling],
            [mechanic, AttackMechanic(exactSibling.CodeId, 100)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeEquipmentMutationEvidence mutation = captured.Evidence!
            .EquipmentMutationValuesByKey[
                new NetherCodeMutationKey(selectedParameter.CodeId, 0)
            ];

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, mutation.MechanismValue.Kind);
        Assert.Equal(
            selectedParameter.CodeId,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [selectedParameter, exactSibling],
                settings,
                captured.Evidence
            ).SelectedCodeId
        );
    }

    [Fact]
    public void Production_assembler_requires_exact_guaranteed_builtin_controls_candidate_locally()
    {
        // Fresh Project.dll 53806a5b...1300: BattleSituationBuiltIn inherits
        // BattleSituationBase. GetProbabilityPerMille, CreateSituationLimits/ExecuteCountLimit and
        // SituationCost are consumed before the subtype condition. BuiltIn is not unconditional
        // unless all inherited controls prove guaranteed, unlimited and cost-free.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate probabilistic = Candidate(88325, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate limited = Candidate(88326, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate costly = Candidate(88327, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate unknownControl = Candidate(88328, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate guaranteed = Candidate(88329, NetherCodeFamily.Safe, power: 1);
        NetherStrategyExecuteCountLimitEvidence oneExecution = new(
            NetherStrategyExecuteCountLimitKind.Battle,
            "Project.BattleSituations.SituationLimits.ExecuteCountLimitBattle",
            RawValueType: 0,
            FixedCountLimit: 1,
            LevelCountLimits: []
        );
        NetherStrategySituationCostEvidence cost = new(
            NetherStrategySituationCostKind.BuffStack,
            "Project.BattleSituations.SituationCosts.SituationCostBuffStack",
            BuffType: 123,
            FixedStack: 1,
            LevelStacks: []
        );
        NetherStrategyNativeMechanic WithControl(
            long id,
            NetherStrategyTriggerControlEvidence control
        )
        {
            NetherStrategyNativeMechanic row = AttackMechanic(id, 900);
            return row with
            {
                Triggers = [KnownTrigger(NetherStrategyTriggerKind.BuiltIn) with
                {
                    ControlRelationships = control,
                }],
            };
        }
        NetherStrategyTriggerControlEvidence unlimited =
            NetherStrategyTriggerControlEvidence.KnownNotApplicable();
        NetherStrategyTriggerControlEvidence limitedControl = unlimited with
        {
            ExecuteCountLimit = oneExecution,
        };
        NetherStrategyTriggerControlEvidence costlyControl = unlimited with
        {
            SituationCosts = [cost],
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [probabilistic, limited, costly, unknownControl, guaranteed],
            [
                WithControl(
                    probabilistic.CodeId,
                    NetherStrategyTriggerControlEvidence.KnownFixed(500)
                ),
                WithControl(limited.CodeId, limitedControl),
                WithControl(costly.CodeId, costlyControl),
                WithControl(
                    unknownControl.CodeId,
                    NetherStrategyTriggerControlEvidence.Unknown("builtin-controls-missing")
                ),
                AttackMechanic(guaranteed.CodeId, 100),
            ],
            settings,
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        foreach (NetherCodeCandidate rejected in new[]
                 {
                     probabilistic, limited, costly, unknownControl,
                 })
        {
            NetherCodeEquipmentMutationEvidence mutation = captured.Evidence!
                .EquipmentMutationValuesByKey[new NetherCodeMutationKey(rejected.CodeId, 0)];
            Assert.Equal(
                NetherCombatValueEvidenceKind.ReachableUnquantified,
                mutation.MechanismValue.Kind
            );
            Assert.Contains("native-buff-trigger-control-unavailable", mutation.MechanismValue.Detail);
        }
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [probabilistic, limited, costly, unknownControl, guaranteed],
            settings,
            captured.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(guaranteed.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_maps_active_research_family_and_keeps_unknown_candidate_local()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate unknownHighPower = Candidate(
            88001,
            NetherCodeFamily.Rush,
            power: 99_999
        );
        NetherCodeCandidate knownLowPower = Candidate(88002, NetherCodeFamily.Rush, power: 1);
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, primaryWallet: 19_000, primaryProjection: 500),
            snapshot,
            [unknownHighPower, knownLowPower],
            [UnknownMechanic(unknownHighPower.CodeId), ForceChainMechanic(knownLowPower.CodeId)],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Rush,
                ResearchSecondaryFamily = NetherCodeFamily.Safe,
                CodeReloadReserve = 1,
            },
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess);
        Assert.Equal(NetherCodeFamily.Rush, captured.Evidence!.ActiveResearchFamily);
        Assert.False(captured.Evidence.MechanicsByCodeId[unknownHighPower.CodeId].IsKnown);
        Assert.True(captured.Evidence.MechanicsByCodeId[knownLowPower.CodeId].IsKnown);

        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [unknownHighPower, knownLowPower],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Rush,
                ResearchSecondaryFamily = NetherCodeFamily.Safe,
                CodeReloadReserve = 1,
            },
            captured.Evidence
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(knownLowPower.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_advances_to_secondary_only_after_exact_projected_completion()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, primaryWallet: 19_500, primaryProjection: 500),
            snapshot,
            [Candidate(88003, NetherCodeFamily.Safe, power: 1)],
            [ForceChainMechanic(88003)],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Rush,
                ResearchSecondaryFamily = NetherCodeFamily.Safe,
            },
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess);
        Assert.Equal(NetherCodeFamily.Safe, captured.Evidence!.ActiveResearchFamily);
    }

    [Fact]
    public void Production_assembler_allows_only_proven_back_force_chain_when_survival_is_adequate()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate ordinaryDisplayedHigh = Candidate(
            88004,
            NetherCodeFamily.Safe,
            power: 99_999
        );
        NetherCodeCandidate forceChain = Candidate(88005, NetherCodeFamily.Safe, power: 1);
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, primaryWallet: 0, primaryProjection: 0),
            snapshot,
            [ordinaryDisplayedHigh, forceChain],
            [OrdinaryMechanic(ordinaryDisplayedHigh.CodeId), ForceChainMechanic(forceChain.CodeId)],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                MinimumCharacterHpPermille = 300,
                CodeReloadReserve = 1,
            },
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [ordinaryDisplayedHigh, forceChain],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                MinimumCharacterHpPermille = 300,
                CodeReloadReserve = 1,
            },
            captured.Evidence!
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(forceChain.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_uses_effective_critical_threshold_not_reversed_displayed_power()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // NetherPartyCharacterParametersCalculator.CalculateUnitParametersMap supplies the native
        // CriticalProb input. CriticalRate.CalculateCritical samples 0..999 inclusive, making 999
        // the guaranteed threshold. These literals deliberately make master Power disagree.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate weakDisplayedHigh = Candidate(88101, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate strongDisplayedLow = Candidate(88102, NetherCodeFamily.Safe, 1);
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            MinimumCharacterHpPermille = 300,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, primaryWallet: 0, primaryProjection: 0),
            snapshot,
            [weakDisplayedHigh, strongDisplayedLow],
            [
                CriticalMechanic(weakDisplayedHigh.CodeId, additionPermille: 20),
                CriticalMechanic(strongDisplayedLow.CodeId, additionPermille: 100),
            ],
            settings,
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [weakDisplayedHigh, strongDisplayedLow],
            settings,
            captured.Evidence!
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(strongDisplayedLow.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_requires_live_continuous_count_ladder_before_quantifying()
    {
        // Fresh GameAssembly 573fa800...c1fb: UnitAttackContinuous loads the live
        // ICharacterStatus.AttackContinuousCntMax, decrements probability by 100 after each
        // success, and stops at that count. Probability alone cannot certify the Boss marginal.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate candidate = Candidate(88103, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyNativeMechanic mechanic = ContinuousMechanic(candidate.CodeId, 100);
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            MinimumCharacterHpPermille = 300,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult unknown = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithProbability(snapshot, liveMaximumKnown: false),
            snapshot,
            [candidate],
            [mechanic],
            settings,
            SafeRouteEvidence()
        );
        NetherRuntimeCodePolicyEvidenceResult known = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithProbability(snapshot, liveMaximumKnown: true),
            snapshot,
            [candidate],
            [mechanic],
            settings,
            SafeRouteEvidence()
        );

        NetherCodeEquipmentMutationEvidence unknownMutation =
            unknown.Evidence!.EquipmentMutationValuesByKey[
                new NetherCodeMutationKey(candidate.CodeId, 0)
            ];
        Assert.Equal(
            NetherCombatValueEvidenceKind.ReachableUnquantified,
            unknownMutation.MechanismValue.Kind
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            settings,
            known.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
    }

    [Fact]
    public void Production_assembler_compares_defense_from_exact_native_parameter_inputs_not_displayed_power()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // CalculateUnitParameter delegates the exact typed inputs below to
        // ParameterCalculator.Calculate_Unit. UnitDamageCalculator then applies
        // clamp(1000 - TotalDefence, 0, 1000) as the damage factor. Displayed MNetherCodes.power
        // deliberately disagrees with the native defense relationship in this fixture.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate weakDisplayedHigh = Candidate(88104, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate strongDisplayedLow = Candidate(88105, NetherCodeFamily.Safe, 1);
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            MinimumCharacterHpPermille = 300,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithDefense(snapshot),
            snapshot,
            [weakDisplayedHigh, strongDisplayedLow],
            [
                DefenseMechanic(weakDisplayedHigh.CodeId, additionPermille: 100),
                DefenseMechanic(strongDisplayedLow.CodeId, additionPermille: 200),
            ],
            settings,
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [weakDisplayedHigh, strongDisplayedLow],
            settings,
            captured.Evidence!
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(strongDisplayedLow.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_prioritizes_rear_defense_coverage_before_weakest_and_aggregate_gain()
    {
        // Fresh Project.dll 53806a5b...1300: ElementType Artifact=1/Fire=2 and the exact
        // ElementTypeFlag values are Artifact=2/Fire=4. These deliberately different recipient
        // sets prove the native different-recipient order: rear coverage precedes magnitude.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate oneRearDisplayedHigh = Candidate(88106, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate twoRearDisplayedLow = Candidate(88107, NetherCodeFamily.Safe, 1);
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            MinimumCharacterHpPermille = 300,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithDefenseRecipients(snapshot),
            snapshot,
            [oneRearDisplayedHigh, twoRearDisplayedLow],
            [
                DefenseMechanic(oneRearDisplayedHigh.CodeId, 500, elementTypeFlags: 2),
                DefenseMechanic(twoRearDisplayedLow.CodeId, 10, elementTypeFlags: 2 | 4),
            ],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [oneRearDisplayedHigh, twoRearDisplayedLow],
            settings,
            captured.Evidence!
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(twoRearDisplayedLow.CodeId, decision.SelectedCodeId);
    }

    [Theory]
    [InlineData((int)NetherKnownBuffType.MaxHpRateUp)]
    [InlineData((int)NetherKnownBuffType.TakenDamageDown)]
    [InlineData((int)NetherKnownBuffType.DebuffResistProbabilityUp)]
    public void Production_defense_candidate_cannot_hide_a_lost_held_defensive_domain(
        int rawBuffType
    )
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // ParameterCalculator.Calculate_Unit supplies live MaxHP/Defence, while
        // UnitDamageCalculator applies TakenDamageDown (52) in its incoming-damage modifier.
        // Debuff resistance is a separate native quantity and therefore cannot be suppressed by
        // an effective-HP comparison. A small DefenceUp must not erase any of these held domains.
        NetherKnownBuffType buffType = (NetherKnownBuffType)rawBuffType;
        NetherCodeState held = HeldCode(88170 + rawBuffType, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate candidate = Candidate(88290 + rawBuffType, NetherCodeFamily.Safe, 99_999);
        NetherStrategyBuffParameterReferenceKind referenceKind = buffType
            == NetherKnownBuffType.MaxHpRateUp
                ? NetherStrategyBuffParameterReferenceKind.RatePermille
                : NetherStrategyBuffParameterReferenceKind.FixedPermille;
        NetherStrategyNativeMechanic heldMechanic = OrdinaryBuffMechanic(
            held.CodeId,
            rawBuffType,
            referenceKind,
            500
        );
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithHeldDefensiveBuff(snapshot, heldMechanic, buffType, 500),
            snapshot,
            [candidate],
            [DefenseMechanic(candidate.CodeId, 10)],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            EquipmentSettings(),
            captured.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
    }

    [Fact]
    public void Production_defense_comparison_projects_complete_hp_and_defense_portfolio_delta()
    {
        NetherCodeState held = HeldCode(88480, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate candidate = Candidate(88481, NetherCodeFamily.Safe, 1);
        NetherStrategyNativeMechanic heldMaxHp = OrdinaryBuffMechanic(
            held.CodeId,
            (int)NetherKnownBuffType.MaxHpRateUp,
            NetherStrategyBuffParameterReferenceKind.RatePermille,
            100
        );
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithHeldDefensiveBuff(
                snapshot,
                heldMaxHp,
                NetherKnownBuffType.MaxHpRateUp,
                100
            ),
            snapshot,
            [candidate],
            [DefenseMechanic(candidate.CodeId, 1000)],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeEquipmentMutationEvidence mutation = captured.Evidence!
            .EquipmentMutationValuesByKey[new NetherCodeMutationKey(candidate.CodeId, held.CodeId)];
        NetherCharacterEffectiveHpEvidence row = Assert.Single(
            Assert.Single(mutation.NativeComparisons).DefenseRows
        );
        Assert.Equal(12_500m, row.AfterEffectiveHp);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            EquipmentSettings(),
            captured.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
    }

    [Fact]
    public void Production_assembler_compares_complete_retained_portfolio_at_capacity()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // AbilityPassiveBuff.Initialize materializes the exact BuffType -> Buff.Param and target
        // maps. BuffGroup.GetSumValue/GetMaxLimit and BuffController's HigherValue coexistence then
        // consume the complete active set. A replacement therefore cannot be valued in isolation.
        NetherCodeState held = new(88201, NetherCodeFamily.Safe, 1)
        {
            Category = NetherCodeCategory.Safe,
            PossessionAmount = 1,
        };
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 1,
            Codes = [held],
        };
        NetherCodeCandidate weaker = Candidate(88202, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate stronger = Candidate(88203, NetherCodeFamily.Safe, power: 1);
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([AttackMechanic(held.CodeId, 200)])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            MinimumCharacterHpPermille = 300,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [weaker, stronger],
            [AttackMechanic(weaker.CodeId, 100), AttackMechanic(stronger.CodeId, 300)],
            settings,
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeDecision weakerDecision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [weaker],
            settings,
            captured.Evidence!
        );
        NetherCodeDecision strongerDecision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [stronger],
            settings,
            captured.Evidence!
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, weakerDecision.Kind);
        Assert.Equal(NetherCodeDecisionKind.Select, strongerDecision.Kind);
        Assert.Equal(held.CodeId, strongerDecision.RemoveCodeId);
    }

    [Fact]
    public void Production_offer_ranking_uses_only_unsuppressed_actual_recipient_tiers()
    {
        // Fresh Project.dll 53806a5b...1300: AbilityTargetGroupBase._partyPositionFlag is
        // evaluated by IsMatch/IsMatchFilter for each party member. BuffController's
        // CheckCoexistenceHigherValue and BuffGroup.GetHighestValueBuff then resolve the active
        // BuffType independently per matched recipient. A suppressed Back row cannot lend its
        // rear-offense tier to the surviving Forward row of the same All-target ability.
        NetherCodeState heldBackAttack = HeldCode(89901, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 3,
            Codes = [heldBackAttack],
        };
        NetherCodeCandidate allAttack = Candidate(89902, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate backDefense = Candidate(89903, NetherCodeFamily.Safe, power: 1);
        NetherStrategyEvidencePackage package = PackageWithFrontBackProbabilityAndDefense(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    AttackMechanic(
                        heldBackAttack.CodeId,
                        additionPermille: 500,
                        targetFlags: NetherPartyPositionFlags.Back
                    ),
                ])),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [allAttack, backDefense],
            [
                AttackMechanic(
                    allAttack.CodeId,
                    additionPermille: 300,
                    targetFlags: NetherPartyPositionFlags.Forward
                        | NetherPartyPositionFlags.Back
                        | NetherPartyPositionFlags.Assist
                ),
                DefenseMechanic(backDefense.CodeId, additionPermille: 100),
            ],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        var valuePolicy = new NetherEquipmentCodeValuePolicy();
        NetherEquipmentMutationValue allAttackValue = valuePolicy.Evaluate(
            captured.Evidence!.EquipmentMutationValuesByKey[
                new NetherCodeMutationKey(allAttack.CodeId, 0)
            ]
        );
        NetherEquipmentMutationValue backDefenseValue = valuePolicy.Evaluate(
            captured.Evidence.EquipmentMutationValuesByKey[
                new NetherCodeMutationKey(backDefense.CodeId, 0)
            ]
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [allAttack, backDefense],
            EquipmentSettings(),
            captured.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(backDefense.CodeId, decision.SelectedCodeId);
        Assert.Equal(NetherEquipmentCombatTier.FrontFallback, allAttackValue.CombatTier);
        Assert.Equal(
            NetherEquipmentCombatTier.RearOrFullNonessentialDefense,
            backDefenseValue.CombatTier
        );
    }

    [Theory]
    [InlineData(
        (int)(NetherPartyPositionFlags.Forward
            | NetherPartyPositionFlags.Back
            | NetherPartyPositionFlags.Assist),
        600,
        89912L
    )]
    [InlineData((int)NetherPartyPositionFlags.Forward, 900, 89913L)]
    public void Production_offer_ranking_preserves_only_the_actual_surviving_recipient_tier(
        int rawAttackTarget,
        int attackValue,
        long expectedCodeId
    )
    {
        NetherCodeState heldBackAttack = HeldCode(89911, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 3,
            Codes = [heldBackAttack],
        };
        NetherCodeCandidate attack = Candidate(89912, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate backDefense = Candidate(89913, NetherCodeFamily.Safe, power: 1);
        NetherStrategyEvidencePackage package = PackageWithFrontBackProbabilityAndDefense(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    AttackMechanic(heldBackAttack.CodeId, 500, NetherPartyPositionFlags.Back),
                ])),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [attack, backDefense],
            [
                AttackMechanic(
                    attack.CodeId,
                    attackValue,
                    (NetherPartyPositionFlags)rawAttackTarget
                ),
                DefenseMechanic(backDefense.CodeId, 100),
            ],
            EquipmentSettings(),
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [attack, backDefense],
            EquipmentSettings(),
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(expectedCodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_offer_ranking_keeps_unknown_recipient_candidate_local()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate unknownTarget = Candidate(89921, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate backDefense = Candidate(89922, NetherCodeFamily.Safe, power: 1);
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithFrontBackProbabilityAndDefense(snapshot),
            snapshot,
            [unknownTarget, backDefense],
            [
                AttackMechanic(
                    unknownTarget.CodeId,
                    900,
                    (NetherPartyPositionFlags)16
                ),
                DefenseMechanic(backDefense.CodeId, 100),
            ],
            EquipmentSettings(),
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [unknownTarget, backDefense],
            EquipmentSettings(),
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(backDefense.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_declines_positive_candidate_that_removes_held_force_chain()
    {
        // Fresh GameAssembly 573fa800...c1fb: BattleTriggerActivateForceChain is completed by
        // ForceChainFinishedMessage, while AbilityPassiveBuff is an independent native effect.
        // A full replacement comparison must retain both typed channels; a positive attack card
        // cannot make the removed Force Chain disappear from the before portfolio.
        NetherCodeState heldForceChain = new(88211, NetherCodeFamily.Safe, 1)
        {
            Category = NetherCodeCategory.Safe,
            PossessionAmount = 1,
        };
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 1,
            Codes = [heldForceChain],
        };
        NetherCodeCandidate attack = Candidate(88212, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence(
                    [ForceChainMechanic(heldForceChain.CodeId)]
                )),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            MinimumCharacterHpPermille = 300,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [attack],
            [AttackMechanic(attack.CodeId, 500)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [attack],
            settings,
            captured.Evidence!
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
        Assert.Equal(0, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_allows_higher_tier_back_force_chain_to_replace_front_fallback()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // AbilityTargetGroupBase.IsMatch consumes the exact party-position flags, while
        // BattleTriggerActivateForceChain is completed by ForceChainFinishedMessage. The approved
        // strategy order is therefore typed and lexicographic: proven Back Force Chain may trade
        // away a lower-tier front-only Attack window without exchanging their native magnitudes.
        NetherCodeState heldFront = HeldCode(88213, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [heldFront] };
        NetherCodeCandidate candidate = Candidate(88214, NetherCodeFamily.Safe, power: 1);
        NetherStrategyEvidencePackage package = PackageWithFrontAndBack(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    AttackMechanic(
                        heldFront.CodeId,
                        additionPermille: 900,
                        targetFlags: NetherPartyPositionFlags.Forward
                    ),
                ])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [ForceChainMechanic(candidate.CodeId, NetherPartyPositionFlags.Back)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            settings,
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(heldFront.CodeId, decision.RemoveCodeId);
    }

    [Fact]
    public void Production_assembler_keeps_higher_tier_back_force_chain_over_front_fallback()
    {
        NetherCodeState heldBackForceChain = HeldCode(88215, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 1,
            Codes = [heldBackForceChain],
        };
        NetherCodeCandidate frontAttack = Candidate(88216, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyEvidencePackage package = PackageWithFrontAndBack(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    ForceChainMechanic(
                        heldBackForceChain.CodeId,
                        NetherPartyPositionFlags.Back
                    ),
                ])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [frontAttack],
            [
                AttackMechanic(
                    frontAttack.CodeId,
                    additionPermille: 900,
                    targetFlags: NetherPartyPositionFlags.Forward
                ),
            ],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [frontAttack],
            settings,
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
    }

    [Fact]
    public void Production_assembler_allows_back_force_chain_to_trade_lower_tier_front_erosion_quantity()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // AbilityErosionLinkedBuff retains its exact typed BuffType/Min/Max relationship, while
        // BattleTriggerActivateForceChain completes from ForceChainFinishedMessage. The approved
        // strategic tier is lexicographic: a proven Back Force Chain may trade away this lower-tier
        // front-only native quantity without numerically exchanging their unlike domains.
        NetherCodeState heldFrontErosion = HeldCode(88601, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 1,
            Codes = [heldFrontErosion],
        };
        NetherCodeCandidate backForceChain = Candidate(88602, NetherCodeFamily.Safe, power: 1);
        NetherStrategyNativeMechanic heldMechanic = ErosionMechanic(
            heldFrontErosion.CodeId,
            NetherKnownBuffType.AttackUp1,
            NetherStrategyBuffParameterReferenceKind.RatePermille,
            maximumValue: 500,
            targetFlags: NetherPartyPositionFlags.Forward
        );
        NetherStrategyEvidencePackage package = PackageWithFrontAndBack(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([heldMechanic])),
        };
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [backForceChain],
            [ForceChainMechanic(backForceChain.CodeId, NetherPartyPositionFlags.Back)],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [backForceChain],
            EquipmentSettings(),
            captured.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(heldFrontErosion.CodeId, decision.RemoveCodeId);
    }

    [Fact]
    public void Production_assembler_keeps_back_force_chain_over_lower_tier_front_erosion_quantity()
    {
        NetherCodeState heldBackForceChain = HeldCode(88603, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 1,
            Codes = [heldBackForceChain],
        };
        NetherCodeCandidate frontErosion = Candidate(88604, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyEvidencePackage package = PackageWithFrontAndBack(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    ForceChainMechanic(
                        heldBackForceChain.CodeId,
                        NetherPartyPositionFlags.Back
                    ),
                ])),
        };
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [frontErosion],
            [
                ErosionMechanic(
                    frontErosion.CodeId,
                    NetherKnownBuffType.AttackUp1,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    maximumValue: 500,
                    targetFlags: NetherPartyPositionFlags.Forward
                ),
            ],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(
            NetherCodeDecisionKind.Keep,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [frontErosion],
                EquipmentSettings(),
                captured.Evidence!
            ).Kind
        );
    }

    [Fact]
    public void Production_erosion_linked_attack_uses_exact_back_recipient_combat_tier()
    {
        // Fresh Project.dll 53806a5b...1300: AbilityErosionLinkedBuff.Param preserves the exact
        // BuffType, Min/Max parameter-reference domain and MinParameter.TargetFilter consumed by
        // IAbilityPassiveBuff. AttackUp therefore remains a typed offensive outcome for its exact
        // recipients; a Back recipient outranks an unrelated Forward-only ordinary fallback.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate frontFallback = Candidate(88605, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate backErosionAttack = Candidate(88606, NetherCodeFamily.Safe, power: 1);
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithFrontAndBack(snapshot),
            snapshot,
            [frontFallback, backErosionAttack],
            [
                AttackMechanic(
                    frontFallback.CodeId,
                    additionPermille: 900,
                    targetFlags: NetherPartyPositionFlags.Forward
                ),
                ErosionMechanic(
                    backErosionAttack.CodeId,
                    NetherKnownBuffType.AttackUp1,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    maximumValue: 100,
                    targetFlags: NetherPartyPositionFlags.Back
                ),
            ],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeEquipmentMutationEvidence erosionMutation = captured.Evidence!
            .EquipmentMutationValuesByKey[new NetherCodeMutationKey(backErosionAttack.CodeId, 0)];
        Assert.Equal(NetherEquipmentCombatTier.RearOrFullOffense, erosionMutation.CombatTier);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [frontFallback, backErosionAttack],
            EquipmentSettings(),
            captured.Evidence
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(backErosionAttack.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_erosion_linked_unknown_buff_domain_is_candidate_local()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate unknownDomain = Candidate(88607, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate exactSibling = Candidate(88608, NetherCodeFamily.Safe, power: 1);
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithFrontAndBack(snapshot),
            snapshot,
            [unknownDomain, exactSibling],
            [
                ErosionMechanic(
                    unknownDomain.CodeId,
                    (NetherKnownBuffType)777_777,
                    NetherStrategyBuffParameterReferenceKind.FixedPermille,
                    maximumValue: 900,
                    targetFlags: NetherPartyPositionFlags.Back
                ),
                AttackMechanic(
                    exactSibling.CodeId,
                    additionPermille: 100,
                    targetFlags: NetherPartyPositionFlags.Forward
                ),
            ],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherMechanismValue unknown = captured.Evidence!.MechanismValuesByCodeId[
            unknownDomain.CodeId
        ];
        Assert.Equal(NetherCombatValueEvidenceKind.Missing, unknown.Kind);
        Assert.Contains("erosion-linked-native-buff-domain-unavailable", unknown.Detail);
        Assert.Equal(
            exactSibling.CodeId,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [unknownDomain, exactSibling],
                EquipmentSettings(),
                captured.Evidence
            ).SelectedCodeId
        );
    }

    [Theory]
    [InlineData(
        (int)NetherKnownBuffType.AttackUp1,
        (int)NetherStrategyBuffParameterReferenceKind.RatePermille,
        (int)NetherEquipmentCombatTier.RearOrFullOffense
    )]
    [InlineData(
        (int)NetherKnownBuffType.DamageUp,
        (int)NetherStrategyBuffParameterReferenceKind.FixedPermille,
        (int)NetherEquipmentCombatTier.RearOrFullOffense
    )]
    [InlineData(
        (int)NetherKnownBuffType.CriticalUp,
        (int)NetherStrategyBuffParameterReferenceKind.FixedPermille,
        (int)NetherEquipmentCombatTier.RearOrFullOffense
    )]
    [InlineData(
        (int)NetherKnownBuffType.DefenceUp,
        (int)NetherStrategyBuffParameterReferenceKind.RatePermille,
        (int)NetherEquipmentCombatTier.RearOrFullNonessentialDefense
    )]
    [InlineData(
        (int)NetherKnownBuffType.MaxHpRateUp,
        (int)NetherStrategyBuffParameterReferenceKind.RatePermille,
        (int)NetherEquipmentCombatTier.RearOrFullNonessentialDefense
    )]
    [InlineData(
        (int)NetherKnownBuffType.TakenDamageDown,
        (int)NetherStrategyBuffParameterReferenceKind.FixedPermille,
        (int)NetherEquipmentCombatTier.RearOrFullNonessentialDefense
    )]
    public void Production_erosion_linked_supported_native_domains_keep_typed_rear_tier(
        int rawBuffType,
        int rawReferenceKind,
        int rawExpectedTier
    )
    {
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate candidate = Candidate(88609 + rawBuffType, NetherCodeFamily.Safe, 1);
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithFrontAndBack(snapshot),
            snapshot,
            [candidate],
            [
                ErosionMechanic(
                    candidate.CodeId,
                    (NetherKnownBuffType)rawBuffType,
                    (NetherStrategyBuffParameterReferenceKind)rawReferenceKind,
                    maximumValue: 100,
                    targetFlags: NetherPartyPositionFlags.Back
                ),
            ],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeEquipmentMutationEvidence mutation = captured.Evidence!
            .EquipmentMutationValuesByKey[new NetherCodeMutationKey(candidate.CodeId, 0)];
        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, mutation.MechanismValue.Kind);
        Assert.Equal((NetherEquipmentCombatTier)rawExpectedTier, mutation.CombatTier);
    }

    [Theory]
    [InlineData(
        (int)NetherPartyPositionFlags.Back,
        10,
        (int)NetherPartyPositionFlags.Forward,
        100,
        (int)NetherCodeDecisionKind.Keep
    )]
    [InlineData(
        (int)NetherPartyPositionFlags.Forward,
        10,
        (int)NetherPartyPositionFlags.Back,
        100,
        (int)NetherCodeDecisionKind.Select
    )]
    [InlineData(
        (int)NetherPartyPositionFlags.Back,
        10,
        (int)NetherPartyPositionFlags.Back,
        100,
        (int)NetherCodeDecisionKind.Select
    )]
    public void Production_erosion_replacement_preserves_exact_recipient_tier_before_magnitude(
        int rawHeldTarget,
        int heldMaximum,
        int rawCandidateTarget,
        int candidateMaximum,
        int rawExpectedDecision
    )
    {
        // Fresh Project.dll 53806a5b...1300: AbilityErosionLinkedBuff.Param retains one exact
        // TargetFilter together with BuffType and Min/Max parameter references; IAbilityPassiveBuff
        // evaluates that typed value for each matching target. Equal BuffType/reference identity is
        // therefore insufficient to aggregate a Back loss with a Forward gain.
        NetherCodeState held = HeldCode(88671, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate candidate = Candidate(88672, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyEvidencePackage package = PackageWithFrontAndBack(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    ErosionMechanic(
                        held.CodeId,
                        NetherKnownBuffType.AttackUp1,
                        NetherStrategyBuffParameterReferenceKind.RatePermille,
                        heldMaximum,
                        (NetherPartyPositionFlags)rawHeldTarget
                    ),
                ])),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [
                ErosionMechanic(
                    candidate.CodeId,
                    NetherKnownBuffType.AttackUp1,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    candidateMaximum,
                    (NetherPartyPositionFlags)rawCandidateTarget
                ),
            ],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            EquipmentSettings(),
            captured.Evidence!
        );
        Assert.Equal((NetherCodeDecisionKind)rawExpectedDecision, decision.Kind);
    }

    [Fact]
    public void Production_contamination_uses_only_actual_nonzero_recipient_metric_tiers()
    {
        // Fresh BuffController/BuffGroup evidence under GameAssembly 573fa800...c1fb applies
        // HigherValue within the complete active recipient/BuffType group. The common Back Attack
        // suppresses the Rush Back Attack, leaving only Forward Attack versus Forward Damage.
        // Those distinct same-tier native domains are incomparable; the suppressed rear row cannot
        // lend its tier to the surviving front marginal.
        NetherCodeState rushRearSuppressed = HeldCode(88673, NetherCodeFamily.Rush);
        NetherCodeState rushFrontAttack = HeldCode(88674, NetherCodeFamily.Rush);
        NetherCodeState impactFrontDamage = HeldCode(88675, NetherCodeFamily.Impact);
        NetherCodeState commonRearAttack = HeldCode(88676, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 4,
            Codes = [rushRearSuppressed, rushFrontAttack, impactFrontDamage, commonRearAttack],
        };
        NetherStrategyEvidencePackage package = PackageWithFrontAndBack(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    AttackMechanic(rushRearSuppressed.CodeId, 300, NetherPartyPositionFlags.Back),
                    AttackMechanic(rushFrontAttack.CodeId, 10, NetherPartyPositionFlags.Forward),
                    OrdinaryBuffMechanic(
                        impactFrontDamage.CodeId,
                        (int)NetherKnownBuffType.DamageUp,
                        NetherStrategyBuffParameterReferenceKind.FixedPermille,
                        20,
                        NetherPartyPositionFlags.Forward
                    ),
                    AttackMechanic(commonRearAttack.CodeId, 350, NetherPartyPositionFlags.Back),
                ])),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [],
            [],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherFamilyRetentionEvidence retention = captured.Evidence!.FamilyRetentionByPair[
            NetherOpposedFamilyPair.RushImpact
        ];
        Assert.False(retention.IsKnown);
        Assert.Equal(NetherCodeFamily.Unknown, retention.PreferredFamily);
        Assert.Contains("incomparable", retention.Detail);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Production_contamination_does_not_promote_front_gain_with_saturated_rear_special_tier(
        bool defenseIsRush
    )
    {
        // CriticalRate caps the complete per-character probability below 1000. The common +1000
        // Critical row therefore makes the side-specific rear Critical row an exact zero outcome.
        // Only the side's Forward Attack remains; the opposing exact rear-defense gain must win.
        NetherCodeFamily defenseFamily = defenseIsRush
            ? NetherCodeFamily.Rush
            : NetherCodeFamily.Impact;
        NetherCodeFamily frontFamily = defenseIsRush
            ? NetherCodeFamily.Impact
            : NetherCodeFamily.Rush;
        NetherCodeState frontAttack = HeldCode(88677, frontFamily);
        NetherCodeState saturatedCritical = HeldCode(88678, frontFamily);
        NetherCodeState rearDefense = HeldCode(88679, defenseFamily);
        NetherCodeState commonCritical = HeldCode(88680, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 4,
            Codes = [frontAttack, saturatedCritical, rearDefense, commonCritical],
        };
        NetherStrategyEvidencePackage package = PackageWithFrontBackProbabilityAndDefense(snapshot)
            with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    AttackMechanic(frontAttack.CodeId, 100, NetherPartyPositionFlags.Forward),
                    CriticalMechanic(
                        saturatedCritical.CodeId,
                        100,
                        NetherPartyPositionFlags.Back
                    ),
                    DefenseMechanic(rearDefense.CodeId, 100),
                    CriticalMechanic(
                        commonCritical.CodeId,
                        1_000,
                        NetherPartyPositionFlags.Back
                    ),
                ])),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [],
            [],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherFamilyRetentionEvidence retention = captured.Evidence!.FamilyRetentionByPair[
            NetherOpposedFamilyPair.RushImpact
        ];
        Assert.True(retention.IsKnown, retention.Detail);
        Assert.Equal(defenseFamily, retention.PreferredFamily);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Production_opposed_family_retention_uses_cross_tier_complete_portfolios(
        bool rushHasBackForceChain
    )
    {
        // Fresh native control flow keeps ForceChainFinishedMessage completion and Forward Attack
        // parameter windows in distinct typed channels. Family contamination repair must apply the
        // same lexicographic recipient tier as normal replacement while retaining the common Code.
        NetherCodeFamily preferred = rushHasBackForceChain
            ? NetherCodeFamily.Rush
            : NetherCodeFamily.Impact;
        NetherCodeFamily losing = rushHasBackForceChain
            ? NetherCodeFamily.Impact
            : NetherCodeFamily.Rush;
        NetherCodeState preferredHeld = HeldCode(88631, preferred);
        NetherCodeState losingOne = HeldCode(88632, losing);
        NetherCodeState losingTwo = HeldCode(88633, losing);
        NetherCodeState common = HeldCode(88634, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 4,
            Codes = [preferredHeld, losingOne, losingTwo, common],
        };
        NetherCodeCandidate preferredCandidate = Candidate(88635, preferred, power: 1);
        NetherStrategyEvidencePackage package = PackageWithFrontAndBack(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    ForceChainMechanic(
                        preferredHeld.CodeId,
                        NetherPartyPositionFlags.Back
                    ),
                    AttackMechanic(
                        losingOne.CodeId,
                        50,
                        NetherPartyPositionFlags.Forward
                    ),
                    AttackMechanic(
                        losingTwo.CodeId,
                        40,
                        NetherPartyPositionFlags.Forward
                    ),
                    AttackMechanic(common.CodeId, 10, NetherPartyPositionFlags.Back),
                ])),
        };
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [preferredCandidate],
            [AttackMechanic(preferredCandidate.CodeId, 100, NetherPartyPositionFlags.Back)],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherFamilyRetentionEvidence retention = captured.Evidence!.FamilyRetentionByPair[
            NetherOpposedFamilyPair.RushImpact
        ];
        Assert.True(retention.IsKnown, retention.Detail);
        Assert.Equal(preferred, retention.PreferredFamily);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [preferredCandidate],
            EquipmentSettings(),
            captured.Evidence
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Contains(
            decision.RemoveCodeId,
            new[] { losingOne.CodeId, losingTwo.CodeId }
        );
    }

    [Theory]
    [InlineData((int)NetherNativeSpecialComparisonKind.CriticalProbability, true)]
    [InlineData((int)NetherNativeSpecialComparisonKind.CriticalProbability, false)]
    [InlineData((int)NetherNativeSpecialComparisonKind.ContinuousAttackProbability, true)]
    [InlineData((int)NetherNativeSpecialComparisonKind.ContinuousAttackProbability, false)]
    public void Production_special_replacement_uses_per_recipient_combat_tier(
        int rawSpecialKind,
        bool candidateTargetsBack
    )
    {
        // CriticalRate and UnitAttackContinuous consume each character's independent live status.
        // Their marginal must retain CharacterId + PartyPosition: a Back gain may trade a Forward
        // loss, while a Forward gain cannot numerically compensate a Back loss.
        NetherNativeSpecialComparisonKind specialKind =
            (NetherNativeSpecialComparisonKind)rawSpecialKind;
        NetherCodeState held = HeldCode(88641, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate candidate = Candidate(88642, NetherCodeFamily.Safe, power: 1);
        NetherPartyPositionFlags heldTarget = candidateTargetsBack
            ? NetherPartyPositionFlags.Forward
            : NetherPartyPositionFlags.Back;
        NetherPartyPositionFlags candidateTarget = candidateTargetsBack
            ? NetherPartyPositionFlags.Back
            : NetherPartyPositionFlags.Forward;
        NetherStrategyNativeMechanic heldMechanic = SpecialMechanic(
            specialKind,
            held.CodeId,
            candidateTargetsBack ? 800 : 100,
            heldTarget
        );
        NetherStrategyNativeMechanic candidateMechanic = SpecialMechanic(
            specialKind,
            candidate.CodeId,
            candidateTargetsBack ? 100 : 800,
            candidateTarget
        );
        NetherStrategyEvidencePackage package = PackageWithFrontAndBackProbability(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([heldMechanic])),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [candidateMechanic],
            EquipmentSettings(),
            SafeRouteEvidence()
        );
        NetherCodeEquipmentMutationEvidence mutation = captured.Evidence!
            .EquipmentMutationValuesByKey[new NetherCodeMutationKey(candidate.CodeId, held.CodeId)];
        NetherNativeSpecialComparisonEvidence comparison = Assert.Single(
            mutation.NativeComparisons
        );
        Assert.Contains(
            comparison.ProbabilityRows,
            row => row.PartyPosition == NetherPartyPosition.Forward
                && row.AfterProbabilityPermille != row.BeforeProbabilityPermille
        );
        Assert.Contains(
            comparison.ProbabilityRows,
            row => row.PartyPosition == NetherPartyPosition.Back
                && row.AfterProbabilityPermille != row.BeforeProbabilityPermille
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            EquipmentSettings(),
            captured.Evidence
        );
        Assert.Equal(
            candidateTargetsBack ? NetherCodeDecisionKind.Select : NetherCodeDecisionKind.Keep,
            decision.Kind
        );
    }

    [Theory]
    [InlineData((int)NetherNativeSpecialComparisonKind.CriticalProbability, true)]
    [InlineData((int)NetherNativeSpecialComparisonKind.CriticalProbability, false)]
    [InlineData((int)NetherNativeSpecialComparisonKind.ContinuousAttackProbability, true)]
    [InlineData((int)NetherNativeSpecialComparisonKind.ContinuousAttackProbability, false)]
    public void Production_special_replacement_keeps_same_recipient_magnitude_comparable(
        int rawSpecialKind,
        bool candidateIsStronger
    )
    {
        NetherNativeSpecialComparisonKind specialKind =
            (NetherNativeSpecialComparisonKind)rawSpecialKind;
        NetherCodeState held = HeldCode(88651, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate candidate = Candidate(88652, NetherCodeFamily.Safe, power: 1);
        NetherStrategyNativeMechanic heldMechanic = SpecialMechanic(
            specialKind,
            held.CodeId,
            candidateIsStronger ? 100 : 200,
            NetherPartyPositionFlags.Back
        );
        NetherStrategyNativeMechanic candidateMechanic = SpecialMechanic(
            specialKind,
            candidate.CodeId,
            candidateIsStronger ? 200 : 100,
            NetherPartyPositionFlags.Back
        );
        NetherStrategyEvidencePackage package = PackageWithFrontAndBackProbability(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([heldMechanic])),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [candidateMechanic],
            EquipmentSettings(),
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            EquipmentSettings(),
            captured.Evidence!
        );
        Assert.Equal(
            candidateIsStronger ? NetherCodeDecisionKind.Select : NetherCodeDecisionKind.Keep,
            decision.Kind
        );
    }

    [Theory]
    [InlineData((int)NetherNativeSpecialComparisonKind.CriticalProbability)]
    [InlineData((int)NetherNativeSpecialComparisonKind.ContinuousAttackProbability)]
    public void Production_special_replacement_keeps_same_tier_mixed_recipients_unquantified(
        int rawSpecialKind
    )
    {
        NetherNativeSpecialComparisonKind specialKind =
            (NetherNativeSpecialComparisonKind)rawSpecialKind;
        NetherCodeState held = HeldCode(88661, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate candidate = Candidate(88662, NetherCodeFamily.Safe, power: 1);
        NetherStrategyNativeMechanic heldMechanic = SpecialMechanicForElement(
            specialKind,
            held.CodeId,
            additionPermille: 300,
            elementTypeFlags: 2
        );
        NetherStrategyNativeMechanic candidateMechanic = SpecialMechanicForElement(
            specialKind,
            candidate.CodeId,
            additionPermille: 300,
            elementTypeFlags: 4
        );
        NetherStrategyEvidencePackage package = PackageWithTwoBackProbability(snapshot) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([heldMechanic])),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [candidateMechanic],
            EquipmentSettings(),
            SafeRouteEvidence()
        );
        NetherCodeEquipmentMutationEvidence mutation = captured.Evidence!
            .EquipmentMutationValuesByKey[new NetherCodeMutationKey(candidate.CodeId, held.CodeId)];
        NetherEquipmentMutationValue value = new NetherEquipmentCodeValuePolicy().Evaluate(mutation);

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherEquipmentMutationValueKind.ReachableUnquantified, value.Kind);
        Assert.Equal(
            NetherCodeDecisionKind.Keep,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [candidate],
                EquipmentSettings(),
                captured.Evidence
            ).Kind
        );
    }

    [Theory]
    [InlineData((int)NetherNativeSpecialComparisonKind.CriticalProbability, true)]
    [InlineData((int)NetherNativeSpecialComparisonKind.CriticalProbability, false)]
    [InlineData((int)NetherNativeSpecialComparisonKind.ContinuousAttackProbability, true)]
    [InlineData((int)NetherNativeSpecialComparisonKind.ContinuousAttackProbability, false)]
    public void Production_offer_comparison_treats_ordinary_and_native_special_as_incomparable(
        int rawSpecialKind,
        bool specialHasLowerCodeId
    )
    {
        // CriticalRate.CalculateCritical and UnitAttackContinuous consume distinct bounded native
        // probability relationships. Ordinary Attack instead enters ParameterCalculator and later
        // UnitDamageCalculator. Without a complete outcome bridge, a missing special dimension is
        // not zero; deterministic CodeId alone breaks the same-tier offer tie.
        NetherNativeSpecialComparisonKind specialKind =
            (NetherNativeSpecialComparisonKind)rawSpecialKind;
        long specialId = specialHasLowerCodeId ? 88611 : 88612;
        long attackId = specialHasLowerCodeId ? 88612 : 88611;
        NetherCodeCandidate special = Candidate(specialId, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate attack = Candidate(attackId, NetherCodeFamily.Safe, power: 1);
        NetherStrategyNativeMechanic specialMechanic = specialKind switch
        {
            NetherNativeSpecialComparisonKind.CriticalProbability =>
                CriticalMechanic(special.CodeId, 100),
            NetherNativeSpecialComparisonKind.ContinuousAttackProbability =>
                ContinuousMechanic(special.CodeId, 100),
            _ => throw new InvalidOperationException(),
        };
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithProbability(snapshot, liveMaximumKnown: true),
            snapshot,
            [attack, special],
            [AttackMechanic(attack.CodeId, 500), specialMechanic],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [attack, special],
            EquipmentSettings(),
            captured.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(Math.Min(attackId, specialId), decision.SelectedCodeId);
    }

    [Theory]
    [InlineData((int)NetherNativeSpecialComparisonKind.CriticalProbability)]
    [InlineData((int)NetherNativeSpecialComparisonKind.ContinuousAttackProbability)]
    public void Production_offer_comparison_keeps_same_special_magnitude_comparable(
        int rawSpecialKind
    )
    {
        NetherNativeSpecialComparisonKind specialKind =
            (NetherNativeSpecialComparisonKind)rawSpecialKind;
        NetherCodeCandidate weakLowId = Candidate(88621, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate strongHighId = Candidate(88622, NetherCodeFamily.Safe, power: 1);
        NetherStrategyNativeMechanic Mechanic(long id, int value) => specialKind switch
        {
            NetherNativeSpecialComparisonKind.CriticalProbability => CriticalMechanic(id, value),
            NetherNativeSpecialComparisonKind.ContinuousAttackProbability =>
                ContinuousMechanic(id, value),
            _ => throw new InvalidOperationException(),
        };
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithProbability(snapshot, liveMaximumKnown: true),
            snapshot,
            [weakLowId, strongHighId],
            [Mechanic(weakLowId.CodeId, 10), Mechanic(strongHighId.CodeId, 100)],
            EquipmentSettings(),
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(
            strongHighId.CodeId,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [weakLowId, strongHighId],
                EquipmentSettings(),
                captured.Evidence!
            ).SelectedCodeId
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Production_assembler_keeps_cross_domain_replacement_unquantified(
        bool heldIsCritical
    )
    {
        // Fresh Project.dll 53806a5b...1300: CriticalProbability is a bounded probability
        // parameter, while Attack is a RatePermille unit parameter. UnitDamageCalculator also needs
        // live enemy/skill inputs before those domains can share a damage outcome. Equal strategic
        // tier alone cannot turn either native quantity into the other's replacement magnitude.
        NetherCodeState held = HeldCode(88217, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate candidate = Candidate(88218, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyNativeMechanic heldMechanic = heldIsCritical
            ? CriticalMechanic(held.CodeId, 200)
            : AttackMechanic(held.CodeId, 200);
        NetherStrategyNativeMechanic candidateMechanic = heldIsCritical
            ? AttackMechanic(candidate.CodeId, 500)
            : CriticalMechanic(candidate.CodeId, 500);
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([heldMechanic])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [candidateMechanic],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeEquipmentMutationEvidence mutation = captured.Evidence!
            .EquipmentMutationValuesByKey[
                new NetherCodeMutationKey(candidate.CodeId, held.CodeId)
            ];
        NetherEquipmentMutationValue value = new NetherEquipmentCodeValuePolicy().Evaluate(mutation);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            settings,
            captured.Evidence
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherEquipmentMutationValueKind.ReachableUnquantified, value.Kind);
        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
    }

    [Fact]
    public void Production_assembler_declines_ordinary_candidate_that_removes_held_critical_probability()
    {
        // Fresh Project.dll 53806a5b...1300: CriticalProbability is a live per-character
        // parameter and CriticalUp is combined by native BuffController coexistence. The complete
        // replacement portfolio must retain the held recipient/value relationship even when the
        // offered mechanic is an unrelated ordinary Attack buff.
        NetherCodeState held = HeldCode(88241, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate attack = Candidate(88242, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    CriticalMechanic(held.CodeId, additionPermille: 200),
                ])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [attack],
            [AttackMechanic(attack.CodeId, 500)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [attack],
            settings,
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
    }

    [Fact]
    public void Production_assembler_declines_ordinary_candidate_that_removes_held_continuous_attack()
    {
        // Fresh Project.dll 53806a5b...1300: continuous-attack probability consumes the live
        // finite maximum ladder. Removal must preserve that exact per-recipient relationship; an
        // unrelated positive window cannot erase the held special from the before portfolio.
        NetherCodeState held = HeldCode(88243, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate attack = Candidate(88244, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyEvidencePackage package = PackageWithProbability(snapshot, liveMaximumKnown: true) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    ContinuousMechanic(held.CodeId, additionPermille: 200),
                ])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [attack],
            [AttackMechanic(attack.CodeId, 500)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [attack],
            settings,
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
    }

    [Fact]
    public void Production_assembler_allows_rear_offense_to_replace_held_nonessential_defense()
    {
        // Fresh Project.dll 53806a5b...1300: UnitDamageCalculator consumes each recipient's live
        // Defence through the native ParameterCalculator path. The held exact effective-HP
        // relationship remains part of the complete portfolio, but the approved Equipment combat
        // order places rear offense above rear nonessential defense once survival already passes.
        NetherCodeState held = HeldCode(88245, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate attack = Candidate(88246, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyEvidencePackage package = PackageWithHeldDefense(snapshot, held.CodeId, 500);
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [attack],
            [AttackMechanic(attack.CodeId, 500)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [attack],
            settings,
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
    }

    [Theory]
    [InlineData((int)NetherNativeSpecialComparisonKind.CriticalProbability)]
    [InlineData((int)NetherNativeSpecialComparisonKind.ContinuousAttackProbability)]
    [InlineData((int)NetherNativeSpecialComparisonKind.DefenseEffectiveHp)]
    public void Production_assembler_keeps_failed_changed_special_comparison_candidate_local(
        int rawSpecialKind
    )
    {
        NetherNativeSpecialComparisonKind specialKind =
            (NetherNativeSpecialComparisonKind)rawSpecialKind;
        // Fresh Project.dll 53806a5b...1300: CriticalProbability, the finite continuous ladder,
        // and Defence/HP remain recipient-specific live relationships. If one changed special
        // cannot be reconstructed, only that removal mutation is unknown; an exact sibling removal
        // which retains the same special is still eligible.
        NetherCodeState heldSpecial = HeldCode(88261 + (int)specialKind, NetherCodeFamily.Safe);
        NetherCodeState heldOrdinary = HeldCode(88270 + (int)specialKind, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 2,
            Codes = [heldSpecial, heldOrdinary],
        };
        NetherCodeCandidate candidate = Candidate(
            88280 + (int)specialKind,
            NetherCodeFamily.Safe,
            power: 99_999
        );
        NetherStrategyNativeMechanic special = specialKind switch
        {
            NetherNativeSpecialComparisonKind.CriticalProbability =>
                CriticalMechanic(heldSpecial.CodeId, 200),
            NetherNativeSpecialComparisonKind.ContinuousAttackProbability =>
                ContinuousMechanic(heldSpecial.CodeId, 200),
            NetherNativeSpecialComparisonKind.DefenseEffectiveHp =>
                DefenseMechanic(heldSpecial.CodeId, 200),
            _ => throw new InvalidOperationException(),
        };
        // Base Package intentionally lacks ContinuousAttackCountMaximum and the exact Defence
        // ParameterCalculation rows. Critical is made unavailable with a missing live parameter.
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0);
        if (specialKind == NetherNativeSpecialComparisonKind.CriticalProbability)
        {
            package = package with
            {
                Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
                    new NetherStrategyPartyProfile([
                        package.Party.Value!.Members[0] with
                        {
                            EffectiveParameters = package.Party.Value.Members[0].EffectiveParameters
                                .Where(row => row.Kind != NetherCharacterParameterKind.CriticalProbability)
                                .ToArray(),
                        },
                    ])
                ),
            };
        }
        package = package with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    special,
                    AttackMechanic(heldOrdinary.CodeId, 10),
                ])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [AttackMechanic(candidate.CodeId, 500)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeEquipmentMutationEvidence removesSpecial = captured.Evidence!
            .EquipmentMutationValuesByKey[
                new NetherCodeMutationKey(candidate.CodeId, heldSpecial.CodeId)
            ];
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            settings,
            captured.Evidence
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(
            NetherCombatValueEvidenceKind.ReachableUnquantified,
            removesSpecial.MechanismValue.Kind
        );
        Assert.Contains("native-special-comparison", removesSpecial.MechanismValue.Detail);
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(heldOrdinary.CodeId, decision.RemoveCodeId);
    }

    [Fact]
    public void Production_opposed_family_retention_compares_common_and_side_specific_specials()
    {
        // Fresh Project.dll 53806a5b...1300: CriticalUp contributions use the same native
        // recipient/coexistence relationship regardless of Code family. A common held Code remains
        // in both hypothetical portfolios; the stronger exact side-specific Critical contribution
        // is still determinable and must not be blanket-unknown.
        NetherCodeState rush = HeldCode(88291, NetherCodeFamily.Rush);
        NetherCodeState impact = HeldCode(88292, NetherCodeFamily.Impact);
        NetherCodeState common = HeldCode(88293, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 3,
            Codes = [rush, impact, common],
        };
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    CriticalMechanic(rush.CodeId, 100),
                    CriticalMechanic(impact.CodeId, 200),
                    CriticalMechanic(common.CodeId, 50),
                ])),
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [],
            [],
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment },
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherFamilyRetentionEvidence retention = captured.Evidence!.FamilyRetentionByPair[
            NetherOpposedFamilyPair.RushImpact
        ];
        Assert.True(retention.IsKnown, retention.Detail);
        Assert.Equal(NetherCodeFamily.Unknown, retention.PreferredFamily);
        Assert.Contains("equal", retention.Detail);
    }

    [Fact]
    public void Production_complete_special_portfolio_keeps_retained_saturation_when_replacing_ordinary_code()
    {
        // Fresh Project.dll 53806a5b...1300: CriticalUp Allow contributions coexist in the
        // complete active Buff group, and native critical probability is capped below 1000. With
        // base 950, a retained +300 already saturates; adding +100 while removing an unrelated held
        // Code is an exact zero marginal, not a fabricated +49 improvement.
        NetherCodeState retainedCritical = HeldCode(88294, NetherCodeFamily.Safe);
        NetherCodeState removedOrdinary = HeldCode(88295, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 2,
            Codes = [retainedCritical, removedOrdinary],
        };
        NetherCodeCandidate candidate = Candidate(88296, NetherCodeFamily.Safe, 99_999);
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([
                    CriticalMechanic(retainedCritical.CodeId, 300),
                    AttackMechanic(removedOrdinary.CodeId, 10),
                ])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [CriticalMechanic(candidate.CodeId, 100)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeEquipmentMutationEvidence mutation = captured.Evidence!
            .EquipmentMutationValuesByKey[
                new NetherCodeMutationKey(candidate.CodeId, removedOrdinary.CodeId)
            ];
        NetherNativeSpecialComparisonEvidence comparison = Assert.Single(
            mutation.NativeComparisons
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.All(comparison.ProbabilityRows, row =>
            Assert.Equal(
                Math.Min(999, row.BeforeProbabilityPermille),
                Math.Min(999, row.AfterProbabilityPermille)
            ));
        Assert.Equal(
            NetherCodeDecisionKind.Keep,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [candidate],
                settings,
                captured.Evidence
            ).Kind
        );
    }

    [Fact]
    public void Production_policy_repairs_opposed_families_incrementally_from_complete_typed_portfolios()
    {
        // Fresh Project.dll 53806a5b...1300: NetherCodeCategoryModel.GetCount delegates to
        // GetCategoryCount over the held Code models, so Rush/Impact opposition remains until every
        // losing-side row is removed. Fresh GameAssembly 573fa800...c1fb BuffController HigherValue
        // consumes the complete retained BuffType set; family retention cannot use a summed scalar.
        NetherCodeState rushOne = HeldCode(88231, NetherCodeFamily.Rush);
        NetherCodeState rushTwo = HeldCode(88232, NetherCodeFamily.Rush);
        NetherCodeState impactOne = HeldCode(88233, NetherCodeFamily.Impact);
        NetherCodeState impactTwo = HeldCode(88234, NetherCodeFamily.Impact);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 4,
            Codes = [rushOne, rushTwo, impactOne, impactTwo],
        };
        NetherCodeCandidate candidate = Candidate(88235, NetherCodeFamily.Rush, power: 1);
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence(
                [
                    AttackMechanic(rushOne.CodeId, 300),
                    AttackMechanic(rushTwo.CodeId, 200),
                    AttackMechanic(impactOne.CodeId, 25),
                    AttackMechanic(impactTwo.CodeId, 25),
                ]
                )),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            MinimumCharacterHpPermille = 300,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [AttackMechanic(candidate.CodeId, 350)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            settings,
            captured.Evidence!
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(candidate.CodeId, decision.SelectedCodeId);
        Assert.Equal(impactOne.CodeId, decision.RemoveCodeId);
    }

    [Fact]
    public void Production_opposed_family_retention_includes_common_higher_value_code()
    {
        // Fresh GameAssembly 573fa800...c1fb: BuffController HigherValue compares the incoming
        // value against the complete matched active group. A common held Safe Attack(350) therefore
        // suppresses both the Rush(300) and Impact(25) alternatives; comparing only opposed-side
        // windows would fabricate a Rush preference that the complete native portfolio does not have.
        NetherCodeState rush = HeldCode(88251, NetherCodeFamily.Rush);
        NetherCodeState impact = HeldCode(88252, NetherCodeFamily.Impact);
        NetherCodeState commonSafe = HeldCode(88253, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 3,
            Codes = [rush, impact, commonSafe],
        };
        NetherCodeCandidate candidate = Candidate(88254, NetherCodeFamily.Rush, power: 99_999);
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence(
                [
                    AttackMechanic(rush.CodeId, 300),
                    AttackMechanic(impact.CodeId, 25),
                    AttackMechanic(commonSafe.CodeId, 350),
                ])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [candidate],
            [AttackMechanic(candidate.CodeId, 500)],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [candidate],
            settings,
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherFamilyRetentionEvidence retention = captured.Evidence!.FamilyRetentionByPair[
            NetherOpposedFamilyPair.RushImpact
        ];
        Assert.True(retention.IsKnown, retention.Detail);
        Assert.Equal(NetherCodeFamily.Unknown, retention.PreferredFamily);
        Assert.Contains("equal", retention.Detail);
        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
    }

    [Fact]
    public void Production_assembler_keeps_unknown_crossed_category_effect_candidate_local()
    {
        // Fresh Project.dll 53806a5b...1300: MNetherCodeCategorySkills supplies the exact
        // category/counter/effect_type/effect_parameter_1..3 row, and
        // NetherCodeCategoryTypeExtensions.GetCategoryCount applies opposed-family subtraction.
        // The accepted package intentionally has no decoded native ability mechanic for skill
        // 77501. Crossing it therefore cannot borrow value from the candidate's ordinary Attack
        // buff, while a non-crossing candidate remains independently selectable.
        NetherCodeState[] held =
        [
            HeldCode(88401, NetherCodeFamily.Safe),
            HeldCode(88402, NetherCodeFamily.Safe),
            HeldCode(88403, NetherCodeFamily.Safe),
            HeldCode(88404, NetherCodeFamily.Safe),
        ];
        NetherSnapshot snapshot = Snapshot() with
        {
            CodeCapacity = 10,
            Codes = held,
        };
        NetherCodeCandidate crossing = Candidate(88405, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate nonCrossing = Candidate(88406, NetherCodeFamily.Impact, power: 1);
        NetherStrategyNativeMechanic[] heldMechanics = held
            .Select((code, index) => AttackMechanic(code.CodeId, 10 + index))
            .ToArray();
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            OwnedCodes = NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence>.Known(
                new NetherStrategyOwnedCodeEvidence(held, Capacity: 10, Rerolls: 1)
                {
                    FamilyCounts =
                    [
                        new NetherStrategyFamilyCount(NetherCodeFamily.Safe, 4, 0, 4),
                        new NetherStrategyFamilyCount(NetherCodeFamily.Risk, 0, 4, 0),
                        new NetherStrategyFamilyCount(NetherCodeFamily.Rush, 0, 0, 0),
                        new NetherStrategyFamilyCount(NetherCodeFamily.Impact, 0, 0, 0),
                    ],
                    CategorySkills =
                    [
                        new NetherStrategyCategorySkill(
                            SkillId: 77501,
                            Counter: 5,
                            Family: NetherCodeFamily.Safe,
                            RawEffectType: (int)NetherCodeMasterEffectType.NetherAbility,
                            EffectParameter1: 66501,
                            EffectParameter2: 0,
                            EffectParameter3: 0
                        ),
                    ],
                }
            ),
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence(heldMechanics)),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [crossing, nonCrossing],
            [
                AttackMechanic(crossing.CodeId, 500),
                AttackMechanic(nonCrossing.CodeId, 100),
            ],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [crossing, nonCrossing],
            settings,
            captured.Evidence!
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(
            NetherCombatValueEvidenceKind.ReachableUnquantified,
            captured.Evidence!.EquipmentMutationValuesByKey[
                new NetherCodeMutationKey(crossing.CodeId, 0)
            ].MechanismValue.Kind
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(nonCrossing.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_assembler_keeps_offer_lifecycle_mechanism_unknowns_candidate_local()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // AbilityChargeMana needs the live shared 0..10 pool and enabled modifier chain;
        // AbilitySkillCharge needs per-recipient live gauge/max/modifiers; GetStackCount is only an
        // instantaneous battle value; ReceiveBuff/SpendBuff crest payoff needs a decoded
        // provider/consumer graph; BuffType 120 applies to recurring AddChargeCount operations.
        // None of those live relationships exists on the current Code Offer party model, so each
        // candidate is RU without preventing an independently exact ordinary buff from selection.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate mana = Candidate(88501, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate initialCharge = Candidate(88502, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate stack = Candidate(88503, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate crestPayoff = Candidate(88504, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate recurringCharge = Candidate(88505, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate ordinary = Candidate(88506, NetherCodeFamily.Safe, 1);
        NetherStrategyNativeMechanic manaMechanic = OrdinaryMechanic(mana.CodeId) with
        {
            AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                NetherStrategyAbilityEffectKind.ChargeMana
            )
            {
                ManaEnergy = 5f,
                ParametersKnown = true,
            },
        };
        NetherStrategyNativeMechanic initialChargeMechanic = OrdinaryMechanic(initialCharge.CodeId) with
        {
            AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                NetherStrategyAbilityEffectKind.SkillCharge
            )
            {
                SkillChargePermille = 500,
                ParametersKnown = true,
            },
        };
        NetherStrategyNativeMechanic stackMechanic = OrdinaryMechanic(stack.CodeId) with
        {
            AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                NetherStrategyAbilityEffectKind.StackLinkedBuff
            )
            {
                ParametersKnown = true,
            },
        };
        NetherStrategyNativeMechanic crestMechanic = AttackMechanic(crestPayoff.CodeId, 250) with
        {
            Triggers =
            [
                KnownTrigger(NetherStrategyTriggerKind.ReceiveBuff) with
                {
                    Parameter1 = (int)NetherKnownBuffType.CrestImpact,
                },
            ],
        };
        NetherStrategyNativeMechanic recurringMechanic = OrdinaryBuffMechanic(
            recurringCharge.CodeId,
            120,
            NetherStrategyBuffParameterReferenceKind.FixedPermille,
            200
        );
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [mana, initialCharge, stack, crestPayoff, recurringCharge, ordinary],
            [
                manaMechanic,
                initialChargeMechanic,
                stackMechanic,
                crestMechanic,
                recurringMechanic,
                AttackMechanic(ordinary.CodeId, 100),
            ],
            settings,
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        var expected = new Dictionary<long, string>
        {
            [mana.CodeId] = "code-offer-lifecycle-shared-mana-pool-and-modifier-chain-unavailable;"
                + "exact-trigger-recipient-count=1",
            [initialCharge.CodeId] = "code-offer-lifecycle-live-skill-charge-recipient-state-unavailable",
            [stack.CodeId] = "stack-timeline-or-guaranteed-lower-bound-unavailable",
            [crestPayoff.CodeId] = "crest-provider-consumer-ability-paths-unavailable",
            [recurringCharge.CodeId] = "code-offer-lifecycle-recurring-skill-charge-timeline-unavailable",
        };
        foreach ((long codeId, string reason) in expected)
        {
            NetherMechanismValue value = captured.Evidence!.MechanismValuesByCodeId[codeId];
            Assert.Equal(NetherCombatValueEvidenceKind.ReachableUnquantified, value.Kind);
            Assert.Equal(reason, value.Detail);
        }
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [mana, initialCharge, stack, crestPayoff, recurringCharge, ordinary],
            settings,
            captured.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(ordinary.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_shared_mana_requires_exact_matching_trigger_recipient_candidate_locally()
    {
        // Fresh Project.dll 53806a5b...1300: AbilityChargeMana.Initialize receives the exact
        // AbilityTargetFriend resolver. AbilityTargetGroupBase.IsMatch applies its element and
        // party-position flags before ExecuteInternal can mutate the shared ManaEnergyOrbsController.
        // A party-global resource does not bypass an empty or unknown trigger-recipient set.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate wrongElement = Candidate(88507, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate wrongPosition = Candidate(88508, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate unsupportedFilter = Candidate(88509, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate unknownProvider = Candidate(88510, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate exactSibling = Candidate(88516, NetherCodeFamily.Safe, 1);
        NetherStrategyNativeMechanic providerUnknown = SharedManaMechanic(
            unknownProvider.CodeId,
            NetherPartyPositionFlags.Back
        );
        providerUnknown = providerUnknown with
        {
            Target = providerUnknown.Target with
            {
                ParametersKnown = false,
                UnknownReason = "native-mana-target-provider-unavailable",
            },
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [wrongElement, wrongPosition, unsupportedFilter, unknownProvider, exactSibling],
            [
                SharedManaMechanic(
                    wrongElement.CodeId,
                    NetherPartyPositionFlags.Back,
                    elementTypeFlags: 4
                ),
                SharedManaMechanic(wrongPosition.CodeId, NetherPartyPositionFlags.Forward),
                SharedManaMechanic(
                    unsupportedFilter.CodeId,
                    NetherPartyPositionFlags.Back,
                    unionTypeFlags: 2
                ),
                providerUnknown,
                AttackMechanic(exactSibling.CodeId, 100),
            ],
            settings,
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        foreach (NetherCodeCandidate noRecipient in new[] { wrongElement, wrongPosition })
        {
            NetherMechanismValue value = captured.Evidence!.MechanismValuesByCodeId[
                noRecipient.CodeId
            ];
            Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
            Assert.Equal(0m, value.Quantity.Value);
            Assert.Contains("no-authoritative-trigger-recipient", value.Detail);
        }
        foreach (NetherCodeCandidate unknown in new[] { unsupportedFilter, unknownProvider })
        {
            NetherMechanismValue value = captured.Evidence!.MechanismValuesByCodeId[unknown.CodeId];
            Assert.Equal(NetherCombatValueEvidenceKind.Missing, value.Kind);
            Assert.Contains("target", value.Detail);
        }
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [wrongElement, wrongPosition, unsupportedFilter, unknownProvider, exactSibling],
            settings,
            captured.Evidence!
        );
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(exactSibling.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_shared_mana_front_trigger_match_reaches_global_pool_evidence_gate()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate candidate = Candidate(88517, NetherCodeFamily.Safe, 1);
        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            PackageWithFrontAndBack(snapshot),
            snapshot,
            [candidate],
            [SharedManaMechanic(candidate.CodeId, NetherPartyPositionFlags.Forward)],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Equipment,
                CodeReloadReserve = 1,
            },
            SafeRouteEvidence()
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        NetherMechanismValue value = captured.Evidence!.MechanismValuesByCodeId[candidate.CodeId];
        Assert.Equal(NetherCombatValueEvidenceKind.ReachableUnquantified, value.Kind);
        Assert.Equal(
            "code-offer-lifecycle-shared-mana-pool-and-modifier-chain-unavailable;"
                + "exact-trigger-recipient-count=1",
            value.Detail
        );
    }

    [Fact]
    public void Production_assembler_uses_native_to_even_erosion_value_at_each_confirmed_combat()
    {
        // Fresh Cpp2IL for AbilityErosionLinkedBuff.OnChangedHpLinkedIncrease/Decrease converts
        // the interpolated Single to Double and calls one-argument Math.Round(double). The exact
        // midpoint between 0 and 5 is therefore 2 (ToEven), not 3, at the confirmed 500-permille
        // battle start supplied by the T03 route horizon.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate candidate = Candidate(88511, NetherCodeFamily.Safe, power: 99_999);
        NetherStrategyBuffParameterEvidence minimum = LinkedParameter(value: 0);
        NetherStrategyBuffParameterEvidence maximum = LinkedParameter(value: 5);
        NetherStrategyNativeMechanic mechanic = OrdinaryMechanic(candidate.CodeId) with
        {
            AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                NetherStrategyAbilityEffectKind.ErosionLinkedBuff
            )
            {
                ParametersKnown = true,
                MinLinkedBuff = new NetherStrategyLinkedBuffThresholdEvidence(0, minimum),
                MaxLinkedBuff = new NetherStrategyLinkedBuffThresholdEvidence(1000, maximum),
            },
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [candidate],
            [mechanic],
            settings,
            SafeRouteEvidence()
        );

        NetherMechanismValue value = captured.Evidence!.MechanismValuesByCodeId[candidate.CodeId];
        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        Assert.Equal(NetherMechanismQuantityKind.ErosionLinkedPayoff, value.Quantity.Kind);
        Assert.Equal(2m, value.Quantity.Value);
        Assert.Equal(
            NetherCodeDecisionKind.Select,
            new NetherCodePolicy().Decide(
                Portfolio(snapshot),
                [candidate],
                settings,
                captured.Evidence
            ).Kind
        );
    }

    [Fact]
    public void Production_erosion_linked_target_filter_comes_from_the_native_min_parameter()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // AbilityErosionLinkedBuff.Param.Create converts Min and Max parameters independently but
        // stores MinParameter.TargetFilter as the runtime TargetFilter. IAbilityPassiveBuff then
        // returns that filter for the matching BuffType. An unrelated first ability parameter and
        // Max's non-runtime filter are not targeting authority.
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate linked = Candidate(88512, NetherCodeFamily.Safe, power: 1);
        NetherCodeCandidate exactSibling = Candidate(88513, NetherCodeFamily.Safe, power: 1);
        NetherStrategyBuffParameterEvidence unrelated = LinkedParameter(999) with
        {
            BuffType = new NetherStrategyBuffType(777),
            TargetFilter = UnsupportedLiveFilter(),
        };
        NetherStrategyBuffParameterEvidence minimum = LinkedParameter(0);
        NetherStrategyBuffParameterEvidence maximum = LinkedParameter(10) with
        {
            TargetFilter = UnsupportedLiveFilter(),
        };
        NetherStrategyNativeMechanic mechanic = OrdinaryMechanic(linked.CodeId) with
        {
            AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                NetherStrategyAbilityEffectKind.ErosionLinkedBuff
            )
            {
                ParametersKnown = true,
                BuffParameters = [unrelated],
                MinLinkedBuff = new NetherStrategyLinkedBuffThresholdEvidence(0, minimum),
                MaxLinkedBuff = new NetherStrategyLinkedBuffThresholdEvidence(1000, maximum),
            },
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [linked, exactSibling],
            [mechanic, AttackMechanic(exactSibling.CodeId, 1)],
            settings,
            SafeRouteEvidence()
        );
        NetherMechanismValue linkedValue = captured.Evidence!
            .MechanismValuesByCodeId[linked.CodeId];

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, linkedValue.Kind);
        Assert.Equal(NetherMechanismQuantityKind.ErosionLinkedPayoff, linkedValue.Quantity.Kind);
    }

    [Fact]
    public void Production_erosion_linked_unsupported_min_filter_is_candidate_local_unknown()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherCodeCandidate filtered = Candidate(88514, NetherCodeFamily.Safe, power: 99_999);
        NetherCodeCandidate exact = Candidate(88515, NetherCodeFamily.Safe, power: 1);
        NetherStrategyBuffParameterEvidence minimum = LinkedParameter(0) with
        {
            TargetFilter = UnsupportedLiveFilter(),
        };
        NetherStrategyNativeMechanic mechanic = OrdinaryMechanic(filtered.CodeId) with
        {
            AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                NetherStrategyAbilityEffectKind.ErosionLinkedBuff
            )
            {
                ParametersKnown = true,
                MinLinkedBuff = new NetherStrategyLinkedBuffThresholdEvidence(0, minimum),
                MaxLinkedBuff = new NetherStrategyLinkedBuffThresholdEvidence(
                    1000,
                    LinkedParameter(10)
                ),
            },
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            Package(snapshot, 0, 0),
            snapshot,
            [filtered, exact],
            [mechanic, AttackMechanic(exact.CodeId, 100)],
            settings,
            SafeRouteEvidence()
        );
        NetherMechanismValue filteredValue = captured.Evidence!
            .MechanismValuesByCodeId[filtered.CodeId];
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [filtered, exact],
            settings,
            captured.Evidence
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        Assert.Equal(NetherCombatValueEvidenceKind.Missing, filteredValue.Kind);
        Assert.Contains("native-target-filter-live-relationship-unavailable", filteredValue.Detail);
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(exact.CodeId, decision.SelectedCodeId);
    }

    [Fact]
    public void Production_erosion_linked_portfolio_compares_only_the_same_native_buff_and_reference_domain()
    {
        // Fresh Project.dll 53806a5b...1300: AbilityErosionLinkedBuff.Param keeps the exact
        // Min/Max BuffParameterByType. Its BuffType and concrete parameterReference determine which
        // native status quantity receives the interpolated value. Equal-looking numbers in Attack
        // RatePermille and Defence RatePermille still are not exchangeable BuffType domains.
        NetherCodeState held = HeldCode(88521, NetherCodeFamily.Safe);
        NetherSnapshot snapshot = Snapshot() with { CodeCapacity = 1, Codes = [held] };
        NetherCodeCandidate incompatible = Candidate(88522, NetherCodeFamily.Safe, 99_999);
        NetherCodeCandidate compatible = Candidate(88523, NetherCodeFamily.Safe, 1);
        NetherStrategyNativeMechanic heldMechanic = ErosionMechanic(
            held.CodeId,
            NetherKnownBuffType.AttackUp1,
            NetherStrategyBuffParameterReferenceKind.RatePermille,
            maximumValue: 5
        );
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0) with
        {
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([heldMechanic])),
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            CodeReloadReserve = 1,
        };

        NetherRuntimeCodePolicyEvidenceResult captured = NetherCodePolicyEvidenceAssembler.Assemble(
            package,
            snapshot,
            [incompatible, compatible],
            [
                ErosionMechanic(
                    incompatible.CodeId,
                    NetherKnownBuffType.DefenceUp,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    maximumValue: 10
                ),
                ErosionMechanic(
                    compatible.CodeId,
                    NetherKnownBuffType.AttackUp1,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    maximumValue: 10
                ),
            ],
            settings,
            SafeRouteEvidence()
        );
        NetherCodeEquipmentMutationEvidence incompatibleMutation = captured.Evidence!
            .EquipmentMutationValuesByKey[
                new NetherCodeMutationKey(incompatible.CodeId, held.CodeId)
            ];
        NetherEquipmentMutationValue incompatibleValue = new NetherEquipmentCodeValuePolicy()
            .Evaluate(incompatibleMutation);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(snapshot),
            [incompatible, compatible],
            settings,
            captured.Evidence
        );

        Assert.True(captured.IsSuccess, captured.Detail);
        // The unlike native quantities are never subtracted. Once their exact common Back recipient
        // is retained, the approved typed tier order can still prove that losing rear offense for
        // rear nonessential defense is non-positive.
        Assert.Equal(NetherEquipmentMutationValueKind.NonPositive, incompatibleValue.Kind);
        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(compatible.CodeId, decision.SelectedCodeId);
        Assert.Equal(held.CodeId, decision.RemoveCodeId);
    }

    private static NetherCodePolicyRouteEvidence SafeRouteEvidence() => new()
    {
        IsKnown = true,
        MinimumBattleStartErosion = 50,
        MaximumBattleStartErosion = 65,
        RecoverableToFiftySeventyBand = true,
        SurvivalBaselineKnown = true,
        HasSurvivalDeficit = false,
        BossDurationKnown = true,
        BossDurationSeconds = 30,
        ConfirmedCombats = [new NetherConfirmedCombatErosion(7001, 500, IsExact: true)],
    };

    private static NetherStrategyEvidencePackage Package(
        NetherSnapshot snapshot,
        int primaryWallet,
        int primaryProjection
    ) => new()
    {
        Identity = new NetherStrategyEvidenceIdentity(1, 1, 1, snapshot.Fingerprint),
        Server = new NetherStrategyServerEvidence
        {
            Status = snapshot.Status,
            NetherId = snapshot.NetherId,
            MapId = snapshot.MapId,
        },
        Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
            new NetherStrategyPartyProfile(
            [
                new NetherStrategyPartyMember(
                    100,
                    0,
                    NetherPartyPosition.Back,
                    1,
                    NetherCrestIdentity.Impact,
                    900,
                    true,
                    1,
                    0
                )
                {
                    EffectiveParametersKnown = true,
                    EffectiveParameters =
                    [
                        new NetherStrategyEffectiveParameter(
                            NetherCharacterParameterKind.CriticalProbability,
                            950
                        ),
                        new NetherStrategyEffectiveParameter(NetherCharacterParameterKind.Hp, 10_000),
                        new NetherStrategyEffectiveParameter(NetherCharacterParameterKind.Defence, 100),
                    ],
                },
            ])
        ),
        Research = NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence>.Known(
            new NetherStrategyResearchEvidence(
            [
                new NetherStrategyResearchFamilyState(
                    NetherCodeFamily.Rush,
                    primaryWallet,
                    primaryProjection,
                    10
                ),
                new NetherStrategyResearchFamilyState(NetherCodeFamily.Impact, 0, 0, 10),
                new NetherStrategyResearchFamilyState(NetherCodeFamily.Safe, 0, 0, 10),
                new NetherStrategyResearchFamilyState(NetherCodeFamily.Risk, 0, 0, 10),
            ])
        ),
    };

    private static NetherStrategyEvidencePackage PackageWithProbability(
        NetherSnapshot snapshot,
        bool liveMaximumKnown
    ) => Package(snapshot, primaryWallet: 0, primaryProjection: 0) with
    {
        Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
            new NetherStrategyPartyProfile(
            [
                new NetherStrategyPartyMember(
                    100,
                    0,
                    NetherPartyPosition.Back,
                    1,
                    NetherCrestIdentity.Impact,
                    900,
                    true,
                    1,
                    0
                )
                {
                    EffectiveParametersKnown = true,
                    EffectiveParameters =
                    [
                        new NetherStrategyEffectiveParameter(
                            NetherCharacterParameterKind.CriticalProbability,
                            950
                        ),
                        new NetherStrategyEffectiveParameter(
                            NetherCharacterParameterKind.ContinuousAttackProbability,
                            950
                        ),
                    ],
                    ContinuousAttackCountMaximumKnown = liveMaximumKnown,
                    ContinuousAttackCountMaximum = liveMaximumKnown ? 3 : 0,
                    ContinuousAttackCountMaximumUnknownReason = liveMaximumKnown
                        ? string.Empty
                        : "code-offer-party-model-has-no-live-i-character-status",
                },
            ])
        ),
    };

    private static NetherStrategyEvidencePackage PackageWithFrontAndBack(
        NetherSnapshot snapshot
    )
    {
        NetherStrategyEvidencePackage package = Package(snapshot, 0, 0);
        NetherStrategyPartyMember back = package.Party.Value!.Members.Single();
        return package with
        {
            Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
                new NetherStrategyPartyProfile([
                    back with
                    {
                        CharacterId = 101,
                        PartyIndex = 0,
                        PartyPosition = NetherPartyPosition.Forward,
                    },
                    back with
                    {
                        CharacterId = 102,
                        PartyIndex = 1,
                    },
                ])
            ),
        };
    }

    private static NetherStrategyEvidencePackage PackageWithFrontAndBackProbability(
        NetherSnapshot snapshot
    )
    {
        NetherStrategyEvidencePackage package = PackageWithProbability(
            snapshot,
            liveMaximumKnown: true
        );
        NetherStrategyPartyMember back = package.Party.Value!.Members.Single() with
        {
            EffectiveParameters =
            [
                new NetherStrategyEffectiveParameter(
                    NetherCharacterParameterKind.CriticalProbability,
                    100
                ),
                new NetherStrategyEffectiveParameter(
                    NetherCharacterParameterKind.ContinuousAttackProbability,
                    100
                ),
            ],
        };
        return package with
        {
            Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
                new NetherStrategyPartyProfile([
                    back with
                    {
                        CharacterId = 101,
                        PartyIndex = 0,
                        PartyPosition = NetherPartyPosition.Forward,
                    },
                    back with
                    {
                        CharacterId = 102,
                        PartyIndex = 1,
                        PartyPosition = NetherPartyPosition.Back,
                    },
                ])
            ),
        };
    }

    private static NetherStrategyEvidencePackage PackageWithTwoBackProbability(
        NetherSnapshot snapshot
    )
    {
        NetherStrategyEvidencePackage package = PackageWithFrontAndBackProbability(snapshot);
        NetherStrategyPartyMember first = package.Party.Value!.Members[0];
        NetherStrategyPartyMember second = package.Party.Value.Members[1];
        return package with
        {
            Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
                new NetherStrategyPartyProfile([
                    first with
                    {
                        PartyPosition = NetherPartyPosition.Back,
                        ElementType = 1,
                    },
                    second with
                    {
                        PartyPosition = NetherPartyPosition.Back,
                        ElementType = 2,
                    },
                ])
            ),
        };
    }

    private static NetherStrategyEvidencePackage PackageWithFrontBackProbabilityAndDefense(
        NetherSnapshot snapshot
    )
    {
        NetherStrategyEvidencePackage package = PackageWithFrontAndBackProbability(snapshot);
        return package with
        {
            Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
                new NetherStrategyPartyProfile(package.Party.Value!.Members.Select(member =>
                    member with
                    {
                        EffectiveParameters = member.EffectiveParameters.Concat(
                        [
                            new NetherStrategyEffectiveParameter(
                                NetherCharacterParameterKind.Hp,
                                10_000
                            ),
                            new NetherStrategyEffectiveParameter(
                                NetherCharacterParameterKind.Defence,
                                100
                            ),
                        ]
                        ).ToArray(),
                        ParameterCalculationsKnown = true,
                        ParameterCalculations =
                        [
                            NativeParameterInput(NetherCharacterParameterKind.Hp, 10_000),
                            NativeParameterInput(NetherCharacterParameterKind.Defence, 100),
                        ],
                        ParameterCalculationsUnknownReason = string.Empty,
                    }
                ).ToArray())
            ),
        };
    }

    private static NetherStrategyEvidencePackage PackageWithDefense(NetherSnapshot snapshot) =>
        Package(snapshot, primaryWallet: 0, primaryProjection: 0) with
        {
            Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
                new NetherStrategyPartyProfile(
                [
                    new NetherStrategyPartyMember(
                        100,
                        0,
                        NetherPartyPosition.Back,
                        1,
                        NetherCrestIdentity.Impact,
                        900,
                        true,
                        1,
                        0
                    )
                    {
                        EffectiveParametersKnown = true,
                        EffectiveParameters =
                        [
                            new NetherStrategyEffectiveParameter(NetherCharacterParameterKind.Hp, 10_000),
                            new NetherStrategyEffectiveParameter(NetherCharacterParameterKind.Defence, 100),
                        ],
                        ParameterCalculationsKnown = true,
                        ParameterCalculations =
                        [
                            NativeParameterInput(NetherCharacterParameterKind.Hp, 10_000),
                            NativeParameterInput(NetherCharacterParameterKind.Defence, 100),
                        ],
                        ParameterCalculationsUnknownReason = string.Empty,
                    },
                ])
            ),
        };

    private static NetherStrategyEvidencePackage PackageWithHeldDefense(
        NetherSnapshot snapshot,
        long heldCodeId,
        int heldModifierPermille
    ) => Package(snapshot, primaryWallet: 0, primaryProjection: 0) with
    {
        Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
            new NetherStrategyPartyProfile(
            [
                new NetherStrategyPartyMember(
                    100,
                    0,
                    NetherPartyPosition.Back,
                    1,
                    NetherCrestIdentity.Impact,
                    900,
                    true,
                    1,
                    0
                )
                {
                    EffectiveParametersKnown = true,
                    EffectiveParameters =
                    [
                        new NetherStrategyEffectiveParameter(NetherCharacterParameterKind.Hp, 10_000),
                        new NetherStrategyEffectiveParameter(NetherCharacterParameterKind.Defence, 150),
                    ],
                    ParameterCalculationsKnown = true,
                    ParameterCalculations =
                    [
                        NativeParameterInput(NetherCharacterParameterKind.Hp, 10_000),
                        NativeParameterInput(
                            NetherCharacterParameterKind.Defence,
                            100,
                            allTargetAbilityModifier: heldModifierPermille
                        ),
                    ],
                },
            ])
        ),
        NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
            .Known(new NetherStrategyNativeMechanicsEvidence([
                DefenseMechanic(heldCodeId, heldModifierPermille),
            ])),
    };

    private static NetherStrategyEvidencePackage PackageWithHeldDefensiveBuff(
        NetherSnapshot snapshot,
        NetherStrategyNativeMechanic heldMechanic,
        NetherKnownBuffType buffType,
        int heldValue
    )
    {
        NetherStrategyEvidencePackage package = PackageWithDefense(snapshot);
        NetherStrategyPartyMember member = Assert.Single(package.Party.Value!.Members);
        int currentHp = buffType == NetherKnownBuffType.MaxHpRateUp
            ? 10_000 * (1000 + heldValue) / 1000
            : 10_000;
        NetherStrategyParameterCalculationEvidence[] calculations =
        [
            NativeParameterInput(
                NetherCharacterParameterKind.Hp,
                10_000,
                buffType == NetherKnownBuffType.MaxHpRateUp ? heldValue : 0
            ),
            NativeParameterInput(NetherCharacterParameterKind.Defence, 100),
        ];
        return package with
        {
            Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
                new NetherStrategyPartyProfile([
                    member with
                    {
                        EffectiveParameters =
                        [
                            new NetherStrategyEffectiveParameter(
                                NetherCharacterParameterKind.Hp,
                                currentHp
                            ),
                            new NetherStrategyEffectiveParameter(
                                NetherCharacterParameterKind.Defence,
                                100
                            ),
                        ],
                        ParameterCalculations = calculations,
                    },
                ])
            ),
            NativeMechanics = NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>
                .Known(new NetherStrategyNativeMechanicsEvidence([heldMechanic])),
        };
    }

    private static NetherStrategyEvidencePackage PackageWithDefenseRecipients(
        NetherSnapshot snapshot
    ) => Package(snapshot, primaryWallet: 0, primaryProjection: 0) with
    {
        Party = NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
            new NetherStrategyPartyProfile(
            [
                DefenseMember(characterId: 100, partyIndex: 0, elementType: 1),
                DefenseMember(characterId: 101, partyIndex: 1, elementType: 2),
            ])
        ),
    };

    private static NetherStrategyPartyMember DefenseMember(
        long characterId,
        int partyIndex,
        int elementType
    ) => new(
        characterId,
        partyIndex,
        NetherPartyPosition.Back,
        elementType,
        NetherCrestIdentity.Impact,
        900,
        true,
        1,
        0
    )
    {
        EffectiveParametersKnown = true,
        EffectiveParameters =
        [
            new NetherStrategyEffectiveParameter(NetherCharacterParameterKind.Hp, 10_000),
            new NetherStrategyEffectiveParameter(NetherCharacterParameterKind.Defence, 100),
        ],
        ParameterCalculationsKnown = true,
        ParameterCalculations =
        [
            NativeParameterInput(NetherCharacterParameterKind.Hp, 10_000),
            NativeParameterInput(NetherCharacterParameterKind.Defence, 100),
        ],
        ParameterCalculationsUnknownReason = string.Empty,
    };

    private static NetherStrategyParameterCalculationEvidence NativeParameterInput(
        NetherCharacterParameterKind kind,
        int characterValue,
        int allTargetAbilityModifier = 0
    ) => new(
        kind,
        characterValue,
        SelfAbilityFixedValue: 0,
        EquipmentValue: 0,
        AllTargetAbilityFixedValue: 0,
        SelfAbilityModifier: 0,
        AllTargetAbilityModifier: allTargetAbilityModifier,
        EquipmentEnchantModifier: 0,
        TotalBuildingModifier: 0,
        SupportBuff: 0
    );

    private static NetherAutoClimbSettings EquipmentSettings() => new()
    {
        StrategyMode = NetherStrategyMode.Equipment,
        CodeReloadReserve = 1,
    };

    private static NetherStrategyNativeMechanic ForceChainMechanic(
        long codeId,
        NetherPartyPositionFlags targetFlags = NetherPartyPositionFlags.Back
    ) => new(
        codeId,
        NetherCodeMasterEffectType.NetherAbility,
        [KnownTrigger(NetherStrategyTriggerKind.ActivateForceChain)],
        new NetherStrategyTargetEvidence(NetherStrategyTargetKind.Friend)
        {
            ParametersKnown = true,
            PartyPositionFlags = targetFlags,
        }
    )
    {
        AbilityEffect = new NetherStrategyAbilityEffectEvidence(
            NetherStrategyAbilityEffectKind.NativeCodeEffect
        ),
        IsKnown = true,
    };

    private static NetherStrategyNativeMechanic OrdinaryMechanic(long codeId) => new(
        codeId,
        NetherCodeMasterEffectType.NetherAbility,
        [KnownTrigger(NetherStrategyTriggerKind.GameStart)],
        new NetherStrategyTargetEvidence(NetherStrategyTargetKind.Friend)
        {
            ParametersKnown = true,
            PartyPositionFlags = NetherPartyPositionFlags.Back,
        }
    )
    {
        AbilityEffect = new NetherStrategyAbilityEffectEvidence(
            NetherStrategyAbilityEffectKind.ParameterBuff
        ),
        IsKnown = true,
    };

    private static NetherStrategyNativeMechanic CriticalMechanic(
        long codeId,
        int additionPermille,
        NetherPartyPositionFlags targetFlags = NetherPartyPositionFlags.Back
    ) => OrdinaryMechanic(codeId) with
    {
        Target = OrdinaryMechanic(codeId).Target with
        {
            PartyPositionFlags = targetFlags,
        },
        Triggers = [KnownTrigger(NetherStrategyTriggerKind.BuiltIn)],
        AbilityEffect = new NetherStrategyAbilityEffectEvidence(
            NetherStrategyAbilityEffectKind.PassiveBuff
        )
        {
            BuffParameters =
            [
                new NetherStrategyBuffParameterEvidence(
                    new NetherStrategyBuffType(30),
                    null,
                    new NetherStrategyBuffParameterReferenceEvidence(
                        NetherStrategyBuffParameterReferenceKind.FixedPermille,
                        "Project.Ingame.CriticalUpPermilleBuffParameterReference"
                    )
                    {
                        ValueType = 0,
                        Value = additionPermille,
                        Limit = 0,
                        ValuesKnown = true,
                    }
                ),
            ],
        },
        BuffStrategies =
        [
            new NetherStrategyBuffEvidence(
                new NetherStrategyBuffType(30),
                NetherStrategyBuffEffectKind.Buff,
                NetherStrategyStatusPriorityKind.Buff,
                NetherStrategyBuffCoexistenceKind.Allow
            ),
        ],
    };

    private static NetherStrategyNativeMechanic ContinuousMechanic(
        long codeId,
        int additionPermille,
        NetherPartyPositionFlags targetFlags = NetherPartyPositionFlags.Back
    ) => CriticalMechanic(codeId, additionPermille, targetFlags) with
    {
        AbilityEffect = new NetherStrategyAbilityEffectEvidence(
            NetherStrategyAbilityEffectKind.PassiveBuff
        )
        {
            BuffParameters =
            [
                new NetherStrategyBuffParameterEvidence(
                    new NetherStrategyBuffType(
                        (int)NetherKnownBuffType.ContinuousAttackProbabilityUp
                    ),
                    null,
                    new NetherStrategyBuffParameterReferenceEvidence(
                        NetherStrategyBuffParameterReferenceKind.FixedPermille,
                        "Project.Ingame.AttackContinuousProbabilityUpPermilleBuffParameterReference"
                    )
                    {
                        ValueType = 0,
                        Value = additionPermille,
                        Limit = 0,
                        ValuesKnown = true,
                    }
                ),
            ],
        },
        BuffStrategies =
        [
            new NetherStrategyBuffEvidence(
                new NetherStrategyBuffType(
                    (int)NetherKnownBuffType.ContinuousAttackProbabilityUp
                ),
                NetherStrategyBuffEffectKind.Buff,
                NetherStrategyStatusPriorityKind.Buff,
                NetherStrategyBuffCoexistenceKind.Allow
            ),
        ],
    };

    private static NetherStrategyNativeMechanic SpecialMechanic(
        NetherNativeSpecialComparisonKind kind,
        long codeId,
        int additionPermille,
        NetherPartyPositionFlags targetFlags
    ) => kind switch
    {
        NetherNativeSpecialComparisonKind.CriticalProbability =>
            CriticalMechanic(codeId, additionPermille, targetFlags),
        NetherNativeSpecialComparisonKind.ContinuousAttackProbability =>
            ContinuousMechanic(codeId, additionPermille, targetFlags),
        _ => throw new InvalidOperationException(),
    };

    private static NetherStrategyNativeMechanic SpecialMechanicForElement(
        NetherNativeSpecialComparisonKind kind,
        long codeId,
        int additionPermille,
        int elementTypeFlags
    )
    {
        NetherStrategyNativeMechanic mechanic = SpecialMechanic(
            kind,
            codeId,
            additionPermille,
            NetherPartyPositionFlags.Back
        );
        NetherStrategyBuffParameterEvidence parameter = Assert.Single(
            mechanic.AbilityEffect.BuffParameters
        );
        return mechanic with
        {
            AbilityEffect = mechanic.AbilityEffect with
            {
                BuffParameters =
                [
                    parameter with
                    {
                        TargetFilter = new NetherStrategyBuffTargetFilterEvidence(
                            IgnoreDeadUnit: true,
                            ElementTypeFlags: elementTypeFlags,
                            ElementWeakTypeFlags: 0,
                            PartyPositionFlags: NetherPartyPositionFlags.None,
                            UnionTypeFlags: 0,
                            JobGroupFlags: 0,
                            JobSpeciesFlags: 0,
                            CharacterSizeFlags: 0,
                            RequiredBuffTypes: []
                        ),
                    },
                ],
            },
        };
    }

    private static NetherStrategyNativeMechanic DefenseMechanic(
        long codeId,
        int additionPermille,
        int elementTypeFlags = 0
    ) => OrdinaryMechanic(codeId) with
    {
        Triggers = [KnownTrigger(NetherStrategyTriggerKind.BuiltIn)],
        AbilityEffect = new NetherStrategyAbilityEffectEvidence(
            NetherStrategyAbilityEffectKind.PassiveBuff
        )
        {
            BuffParameters =
            [
                new NetherStrategyBuffParameterEvidence(
                    new NetherStrategyBuffType((int)NetherKnownBuffType.DefenceUp),
                    elementTypeFlags == 0
                        ? null
                        : new NetherStrategyBuffTargetFilterEvidence(
                            IgnoreDeadUnit: true,
                            ElementTypeFlags: elementTypeFlags,
                            ElementWeakTypeFlags: 0,
                            PartyPositionFlags: NetherPartyPositionFlags.None,
                            UnionTypeFlags: 0,
                            JobGroupFlags: 0,
                            JobSpeciesFlags: 0,
                            CharacterSizeFlags: 0,
                            RequiredBuffTypes: []
                        ),
                    new NetherStrategyBuffParameterReferenceEvidence(
                        NetherStrategyBuffParameterReferenceKind.RatePermille,
                        "Project.Ingame.DefenceUpPermilleBuffParameterReference"
                    )
                    {
                        ValueType = 0,
                        Value = additionPermille,
                        Limit = 0,
                        ValuesKnown = true,
                    }
                ),
            ],
        },
        BuffStrategies =
        [
            new NetherStrategyBuffEvidence(
                new NetherStrategyBuffType((int)NetherKnownBuffType.DefenceUp),
                NetherStrategyBuffEffectKind.Buff,
                NetherStrategyStatusPriorityKind.Buff,
                NetherStrategyBuffCoexistenceKind.Allow
            ),
        ],
    };

    private static NetherStrategyNativeMechanic AttackMechanic(
        long codeId,
        int additionPermille,
        NetherPartyPositionFlags targetFlags = NetherPartyPositionFlags.Back
    ) =>
        OrdinaryMechanic(codeId) with
        {
            Target = OrdinaryMechanic(codeId).Target with
            {
                PartyPositionFlags = targetFlags,
            },
            Triggers = [KnownTrigger(NetherStrategyTriggerKind.BuiltIn)],
            AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                NetherStrategyAbilityEffectKind.PassiveBuff
            )
            {
                BuffParameters =
                [
                    new NetherStrategyBuffParameterEvidence(
                        new NetherStrategyBuffType(10),
                        null,
                        new NetherStrategyBuffParameterReferenceEvidence(
                            NetherStrategyBuffParameterReferenceKind.RatePermille,
                            "Project.Ingame.AttackUpPermilleBuffParameterReference"
                        )
                        {
                            ValueType = 0,
                            Value = additionPermille,
                            Limit = 0,
                            ValuesKnown = true,
                        }
                    ),
                ],
            },
            BuffStrategies =
            [
                new NetherStrategyBuffEvidence(
                    new NetherStrategyBuffType(10),
                    NetherStrategyBuffEffectKind.Buff,
                    NetherStrategyStatusPriorityKind.Buff,
                    NetherStrategyBuffCoexistenceKind.HigherValue
                ),
            ],
        };

    private static NetherStrategyNativeMechanic AttackMechanicWithUnsupportedTarget(
        long codeId,
        int additionPermille
    )
    {
        NetherStrategyNativeMechanic mechanic = AttackMechanic(codeId, additionPermille);
        NetherStrategyBuffParameterEvidence parameter = Assert.Single(
            mechanic.AbilityEffect.BuffParameters
        );
        return mechanic with
        {
            AbilityEffect = mechanic.AbilityEffect with
            {
                BuffParameters =
                [
                    parameter with
                    {
                        TargetFilter = UnsupportedLiveFilter(),
                    },
                ],
            },
        };
    }

    private static NetherStrategyBuffTargetFilterEvidence UnsupportedLiveFilter() => new(
        IgnoreDeadUnit: true,
        ElementTypeFlags: 0,
        ElementWeakTypeFlags: 0,
        PartyPositionFlags: NetherPartyPositionFlags.None,
        UnionTypeFlags: 0,
        JobGroupFlags: 0,
        JobSpeciesFlags: 0,
        CharacterSizeFlags: 0,
        RequiredBuffTypes: [new NetherStrategyBuffType(777)]
    );

    private static NetherStrategyNativeMechanic TimedAttackMechanic(
        long codeId,
        NetherStrategyTriggerKind triggerKind,
        int triggerMilliSeconds,
        int buffDurationMilliSeconds,
        int valuePermille
    )
    {
        NetherStrategyNativeMechanic passive = AttackMechanic(codeId, valuePermille);
        return passive with
        {
            Triggers =
            [
                KnownTrigger(triggerKind) with
                {
                    Parameter1 = triggerMilliSeconds,
                },
            ],
            Duration = triggerMilliSeconds,
            DurationKnown = triggerKind == NetherStrategyTriggerKind.Duration,
            AbilityEffect = passive.AbilityEffect with
            {
                Kind = NetherStrategyAbilityEffectKind.ParameterBuff,
                EndSituationCondition = 7,
                EndSituationValue = buffDurationMilliSeconds,
                EndSituationKnown = true,
            },
        };
    }

    private static NetherStrategyNativeMechanic ConditionalRiskManaMechanic(long codeId) =>
        OrdinaryMechanic(codeId) with
    {
        Triggers =
        [
            KnownTrigger(NetherStrategyTriggerKind.StartBattle),
            KnownTrigger(NetherStrategyTriggerKind.AboveErosion) with { Parameter1 = 50 },
        ],
        AbilityEffect = new NetherStrategyAbilityEffectEvidence(
            NetherStrategyAbilityEffectKind.ChargeMana
        )
        {
            ManaEnergy = 5f,
            ParametersKnown = true,
        },
    };

    private static NetherStrategyNativeMechanic SharedManaMechanic(
        long codeId,
        NetherPartyPositionFlags targetFlags,
        int elementTypeFlags = 0,
        int unionTypeFlags = 0
    ) => OrdinaryMechanic(codeId) with
    {
        Target = new NetherStrategyTargetEvidence(NetherStrategyTargetKind.Friend)
        {
            ParametersKnown = true,
            PartyPositionFlags = targetFlags,
            ElementTypeFlags = elementTypeFlags,
            UnionTypeFlags = unionTypeFlags,
        },
        AbilityEffect = new NetherStrategyAbilityEffectEvidence(
            NetherStrategyAbilityEffectKind.ChargeMana
        )
        {
            ManaEnergy = 5f,
            ParametersKnown = true,
        },
    };

    private static NetherStrategyNativeMechanic OrdinaryBuffMechanic(
        long codeId,
        int buffType,
        NetherStrategyBuffParameterReferenceKind referenceKind,
        int value,
        NetherPartyPositionFlags targetFlags = NetherPartyPositionFlags.Back
    ) => OrdinaryMechanic(codeId) with
    {
        Target = OrdinaryMechanic(codeId).Target with
        {
            PartyPositionFlags = targetFlags,
        },
        Triggers = [KnownTrigger(NetherStrategyTriggerKind.BuiltIn)],
        AbilityEffect = new NetherStrategyAbilityEffectEvidence(
            NetherStrategyAbilityEffectKind.PassiveBuff
        )
        {
            BuffParameters =
            [
                new NetherStrategyBuffParameterEvidence(
                    new NetherStrategyBuffType(buffType),
                    null,
                    new NetherStrategyBuffParameterReferenceEvidence(
                        referenceKind,
                        "fresh-current-native-parameter-reference"
                    )
                    {
                        ValueType = 0,
                        Value = value,
                        Limit = 0,
                        ValuesKnown = true,
                    }
                ),
            ],
        },
        BuffStrategies =
        [
            new NetherStrategyBuffEvidence(
                new NetherStrategyBuffType(buffType),
                NetherStrategyBuffEffectKind.Buff,
                NetherStrategyStatusPriorityKind.Buff,
                NetherStrategyBuffCoexistenceKind.HigherValue
            ),
        ],
    };

    private static NetherStrategyBuffParameterEvidence LinkedParameter(int value) => new(
        new NetherStrategyBuffType((int)NetherKnownBuffType.AttackUp1),
        null,
        new NetherStrategyBuffParameterReferenceEvidence(
            NetherStrategyBuffParameterReferenceKind.RatePermille,
            "Project.Ingame.AttackUpPermilleBuffParameterReference"
        )
        {
            ValueType = 0,
            Value = value,
            Limit = 0,
            ValuesKnown = true,
        }
    );

    private static NetherStrategyNativeMechanic ErosionMechanic(
        long codeId,
        NetherKnownBuffType buffType,
        NetherStrategyBuffParameterReferenceKind referenceKind,
        int maximumValue,
        NetherPartyPositionFlags targetFlags = NetherPartyPositionFlags.Back
    )
    {
        NetherStrategyBuffParameterEvidence Parameter(int value) => new(
            new NetherStrategyBuffType((int)buffType),
            null,
            new NetherStrategyBuffParameterReferenceEvidence(
                referenceKind,
                "fresh-current-native-erosion-parameter-reference"
            )
            {
                ValueType = 0,
                Value = value,
                Limit = 0,
                ValuesKnown = true,
            }
        );
        return OrdinaryMechanic(codeId) with
        {
            Target = OrdinaryMechanic(codeId).Target with
            {
                PartyPositionFlags = targetFlags,
            },
            AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                NetherStrategyAbilityEffectKind.ErosionLinkedBuff
            )
            {
                ParametersKnown = true,
                MinLinkedBuff = new NetherStrategyLinkedBuffThresholdEvidence(0, Parameter(0)),
                MaxLinkedBuff = new NetherStrategyLinkedBuffThresholdEvidence(
                    1000,
                    Parameter(maximumValue)
                ),
            },
        };
    }

    private static NetherStrategyNativeMechanic UnknownMechanic(long codeId) =>
        OrdinaryMechanic(codeId) with
        {
            IsKnown = false,
            UnknownReason = "ability-effect-asset-unavailable:" + codeId,
        };

    private static NetherStrategyTriggerEvidence KnownTrigger(NetherStrategyTriggerKind kind) =>
        new(kind)
        {
            ParametersKnown = true,
            ControlRelationships = NetherStrategyTriggerControlEvidence.KnownNotApplicable(),
        };

    private static NetherCodePortfolio Portfolio(NetherSnapshot snapshot) => new()
    {
        CurrentCodes = snapshot.Codes,
        Capacity = snapshot.CodeCapacity,
        ReloadCount = snapshot.CodeReloadCount,
        IsMasterComplete = true,
    };

    private static NetherCodeState HeldCode(long codeId, NetherCodeFamily family) => new(
        codeId,
        family,
        1
    )
    {
        Category = family switch
        {
            NetherCodeFamily.Rush => NetherCodeCategory.Rush,
            NetherCodeFamily.Impact => NetherCodeCategory.Impact,
            NetherCodeFamily.Safe => NetherCodeCategory.Safe,
            NetherCodeFamily.Risk => NetherCodeCategory.Risk,
            _ => NetherCodeCategory.Unknown,
        },
        PossessionAmount = 1,
    };

    private static NetherSnapshot Snapshot() => new()
    {
        Status = NetherSessionStatus.Play,
        NetherId = 1,
        MapId = 2,
        CurrentFloorId = 3,
        CurrentNodeId = 4,
        FloorLevel = 5,
        FloorIndex = 0,
        ErosionPoint = 10,
        CodeCapacity = 5,
        CodeReloadCount = 1,
        Characters = [new NetherCharacterState(100, 900)],
        Codes = [],
        Floors = [],
        CharacterHpHash = "100:900:1",
        CodeHash = string.Empty,
        MapHash = "map",
    };

    private static NetherCodeCandidate Candidate(
        long codeId,
        NetherCodeFamily family,
        int power
    ) => new(codeId, family, 1)
    {
        Category = family switch
        {
            NetherCodeFamily.Rush => NetherCodeCategory.Rush,
            NetherCodeFamily.Impact => NetherCodeCategory.Impact,
            NetherCodeFamily.Safe => NetherCodeCategory.Safe,
            NetherCodeFamily.Risk => NetherCodeCategory.Risk,
            _ => NetherCodeCategory.Unknown,
        },
        Power = power,
        PartyCoverageKnown = true,
        PartyCoverage = 1,
    };
}
