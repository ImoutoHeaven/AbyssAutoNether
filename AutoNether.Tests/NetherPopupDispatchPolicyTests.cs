using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherPopupDispatchPolicyTests
{
    [Fact]
    public void Code_offer_is_dispatched_to_code_flow_not_the_owned_code_list()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext { Kind = NetherRuntimePopupKind.CodeOffer },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.Code, decision.Kind);
    }

    [Fact]
    public void Raw_floor_event_type_four_remains_event_and_selects_its_safe_option()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                Options = [new NetherEventOption(1, [new NetherEffect(NetherEffectKind.Item, 1)])],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.Action.Kind);
        Assert.Equal(1, decision.Action.OptionNumber);
        Assert.Equal(101, decision.Action.TargetCharacterId);
        Assert.Single(decision.Action.ExpectedEffects);
        Assert.Equal(NetherEffectKind.Item, decision.Action.ExpectedEffects[0].Kind);
    }

    [Fact]
    public void Exact_event_popup_propagates_event_part_and_commitment_to_the_native_action()
    {
        NetherEventOption option = new NetherEventOption(
            1,
            [new NetherEffect(NetherEffectKind.Item, 1)
            {
                ContentId = 9001,
                RewardEvidence = new NetherEventRewardEvidence(
                    9001,
                    9001,
                    91,
                    NetherRewardRarity.Gold,
                    1
                ),
            }]
        )
        {
            EventId = 9101,
            EventPartId = 9102,
            RequiresExactBinding = true,
            FloorId = 10,
            NodeId = 1,
        };
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                ExpectedEventCommitment = new NetherEventCommitment(
                    EventId: 9101,
                    EventPartId: 9102,
                    OptionNumber: 1,
                    Effects: option.Effects,
                    ProjectedErosion: 20,
                    HpDelta: 0
                )
                {
                    FloorId = 10,
                    NodeId = 1,
                    Reward = option.Effects[0].RewardEvidence,
                    ProjectedNetherGold = 100,
                    ProjectedTreasureKeys = 0,
                },
                Options = [option],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(9101, decision.Action.EventId);
        Assert.Equal(9102, decision.Action.EventPartId);
        Assert.NotNull(decision.Action.EventCommitment);
        Assert.True(decision.Action.EventCommitment!.IsValid);
    }

    [Fact]
    public void Rank_five_event_commitment_is_carried_through_dispatch_and_rejects_a_stale_objective()
    {
        NetherRankFiveTreasureIdentity objective = new(9401, 9402, 9403);
        NetherRankFiveKeyProcurementCommitment procurement = new()
        {
            Objective = objective,
            SourceKind = NetherKeyProcurementSourceKind.EventGold150,
            SourceNodeId = 1,
            SourceEventId = 9101,
            SourceEventPartId = 9102,
            SourceOptionNumber = 1,
            GoldCost = 150,
        };
        NetherEffect effect = new(NetherEffectKind.Item, 1)
        {
            ContentId = 9001,
            RewardEvidence = new NetherEventRewardEvidence(9001, 9001, 91, NetherRewardRarity.Gold, 1),
        };
        NetherEventOption option = new(1, [effect])
        {
            EventId = 9101,
            EventPartId = 9102,
            FloorId = 10,
            NodeId = 1,
            RequiresExactBinding = true,
            RankFiveKeyProcurementCommitment = procurement,
            RankFiveTreasureObjective = objective,
        };
        NetherEventCommitment expected = new(9101, 9102, 1, [effect], 20, 0)
        {
            FloorId = 10,
            NodeId = 1,
            Reward = effect.RewardEvidence,
            ProjectedNetherGold = 100,
            ProjectedTreasureKeys = 0,
            RankFiveKeyProcurementCommitment = procurement,
            RankFiveTreasureObjective = objective,
        };

        NetherPopupDispatchDecision dispatched = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                ExpectedEventCommitment = expected,
                Options = [option],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, dispatched.Kind);
        Assert.Equal(procurement, dispatched.Action.EventCommitment!.RankFiveKeyProcurementCommitment);
        Assert.Equal(objective, dispatched.Action.EventCommitment.RankFiveTreasureObjective);

        NetherEventCommitment stale = expected with
        {
            RankFiveTreasureObjective = new NetherRankFiveTreasureIdentity(9501, 9502, 9503),
        };
        NetherPopupDispatchDecision rejected = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                ExpectedEventCommitment = stale,
                Options = [option],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, rejected.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, rejected.PauseReason);
    }

    [Fact]
    public void Positive_committed_procurement_minimum_mismatch_pauses_before_native_payment()
    {
        NetherEffect effect = new(NetherEffectKind.NetherGoldGain, 10);
        NetherEventOption option = new(1, [effect])
        {
            EventId = 9121,
            EventPartId = 9122,
            FloorId = 10,
            NodeId = 1,
            RequiresExactBinding = true,
        };
        NetherEventCommitment commitment = new(
            EventId: 9121,
            EventPartId: 9122,
            OptionNumber: 1,
            Effects: [effect],
            ProjectedErosion: 20,
            HpDelta: 0
        )
        {
            FloorId = 10,
            NodeId = 1,
            ProjectedNetherGold = 110,
            ProjectedTreasureKeys = 0,
            CommittedGoldMinimum = 80,
            CommittedKeyMinimum = 0,
        };

        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                ExpectedEventCommitment = commitment,
                Options = [option],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, decision.PauseReason);
    }

    [Fact]
    public void Positive_committed_procurement_minimum_is_carried_into_the_native_action_when_exact()
    {
        NetherEffect effect = new(NetherEffectKind.NetherGoldGain, 10);
        NetherEventOption option = new(1, [effect])
        {
            EventId = 9123,
            EventPartId = 9124,
            FloorId = 10,
            NodeId = 1,
            RequiresExactBinding = true,
            CommittedGoldMinimum = 80,
        };
        NetherEventCommitment commitment = new(
            EventId: 9123,
            EventPartId: 9124,
            OptionNumber: 1,
            Effects: [effect],
            ProjectedErosion: 20,
            HpDelta: 0
        )
        {
            FloorId = 10,
            NodeId = 1,
            ProjectedNetherGold = 110,
            ProjectedTreasureKeys = 0,
            CommittedGoldMinimum = 80,
        };

        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                ExpectedEventCommitment = commitment,
                Options = [option],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(80, decision.Action.CommittedGoldMinimum);
        Assert.Equal(0, decision.Action.CommittedKeyMinimum);
    }

    [Fact]
    public void Exact_event_commitment_without_projected_resources_is_not_valid()
    {
        NetherEventCommitment commitment = new(
            EventId: 9111,
            EventPartId: 9112,
            OptionNumber: 1,
            Effects: [new NetherEffect(NetherEffectKind.NetherGoldGain, 10)],
            ProjectedErosion: 20,
            HpDelta: 0
        )
        {
            FloorId = 10,
            NodeId = 1,
        };

        Assert.False(commitment.IsValid);
    }

    [Fact]
    public void Exact_event_commitment_lookup_rejects_an_event_id_mismatch()
    {
        NetherPopupDispatchDecision decision = DispatchWithCommitmentIdentity(
            optionEventId: 9113,
            optionPartId: 9114,
            optionFloorId: 10,
            optionNodeId: 1,
            commitmentEventId: 9115,
            commitmentPartId: 9114,
            commitmentFloorId: 10,
            commitmentNodeId: 1
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, decision.PauseReason);
    }

    [Fact]
    public void Exact_event_commitment_lookup_rejects_an_event_part_mismatch()
    {
        NetherPopupDispatchDecision decision = DispatchWithCommitmentIdentity(
            optionEventId: 9113,
            optionPartId: 9114,
            optionFloorId: 10,
            optionNodeId: 1,
            commitmentEventId: 9113,
            commitmentPartId: 9115,
            commitmentFloorId: 10,
            commitmentNodeId: 1
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, decision.PauseReason);
    }

    [Fact]
    public void Exact_event_commitment_lookup_rejects_a_floor_mismatch()
    {
        NetherPopupDispatchDecision decision = DispatchWithCommitmentIdentity(
            optionEventId: 9113,
            optionPartId: 9114,
            optionFloorId: 10,
            optionNodeId: 1,
            commitmentEventId: 9113,
            commitmentPartId: 9114,
            commitmentFloorId: 11,
            commitmentNodeId: 1
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, decision.PauseReason);
    }

    [Fact]
    public void Exact_event_commitment_lookup_rejects_a_node_mismatch()
    {
        NetherPopupDispatchDecision decision = DispatchWithCommitmentIdentity(
            optionEventId: 9113,
            optionPartId: 9114,
            optionFloorId: 10,
            optionNodeId: 1,
            commitmentEventId: 9113,
            commitmentPartId: 9114,
            commitmentFloorId: 10,
            commitmentNodeId: 2
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, decision.PauseReason);
    }

    [Fact]
    public void Event_ties_use_the_full_option_identity_as_a_deterministic_tie_break()
    {
        NetherEffect effect = new(NetherEffectKind.NetherGoldGain, 10);
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                Options =
                [
                    new NetherEventOption(1, [effect])
                    {
                        EventId = 9922,
                        EventPartId = 9923,
                        FloorId = 10,
                        NodeId = 1,
                    },
                    new NetherEventOption(1, [effect])
                    {
                        EventId = 9920,
                        EventPartId = 9921,
                        FloorId = 10,
                        NodeId = 1,
                    },
                ],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(9920, decision.Action.EventId);
        Assert.Equal(9921, decision.Action.EventPartId);
    }

    private static NetherPopupDispatchDecision DispatchWithCommitmentIdentity(
        long optionEventId,
        long optionPartId,
        long optionFloorId,
        long optionNodeId,
        long commitmentEventId,
        long commitmentPartId,
        long commitmentFloorId,
        long commitmentNodeId
    )
    {
        NetherEffect effect = new(NetherEffectKind.NetherGoldGain, 10);
        NetherEventCommitment commitment = new(
            commitmentEventId,
            commitmentPartId,
            1,
            [effect],
            ProjectedErosion: 20,
            HpDelta: 0
        )
        {
            FloorId = commitmentFloorId,
            NodeId = commitmentNodeId,
            ProjectedNetherGold = 110,
            ProjectedTreasureKeys = 0,
        };
        return NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                ExpectedEventCommitment = commitment,
                ExpectedEventCommitments = new Dictionary<NetherEventCommitmentKey, NetherEventCommitment>
                {
                    [new NetherEventCommitmentKey(
                        commitmentEventId,
                        commitmentPartId,
                        commitmentFloorId,
                        commitmentNodeId,
                        1
                    )] = commitment,
                },
                Options =
                [
                    new NetherEventOption(1, [effect])
                    {
                        EventId = optionEventId,
                        EventPartId = optionPartId,
                        RequiresExactBinding = true,
                        FloorId = optionFloorId,
                        NodeId = optionNodeId,
                    },
                ],
            },
            Settings()
        );
    }

    [Fact]
    public void Stale_event_popup_commitment_pauses_before_payment()
    {
        NetherEffect committedEffect = new(NetherEffectKind.NetherGoldGain, 10);
        NetherEventCommitment commitment = new(
            EventId: 9201,
            EventPartId: 9202,
            OptionNumber: 1,
            Effects: [committedEffect],
            ProjectedErosion: 20,
            HpDelta: 0
        )
        {
            FloorId = 10,
            NodeId = 1,
            ProjectedNetherGold = 110,
            ProjectedTreasureKeys = 0,
        };
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                ExpectedEventCommitment = commitment,
                Options =
                [
                    new NetherEventOption(1, [committedEffect])
                    {
                        EventId = 9201,
                        EventPartId = 9203,
                        RequiresExactBinding = true,
                        FloorId = 10,
                        NodeId = 1,
                    },
                ],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, decision.PauseReason);
    }

    [Fact]
    public void Changed_event_projection_pauses_even_when_event_identity_and_effects_match()
    {
        NetherEffect effect = new(NetherEffectKind.NetherGoldGain, 10);
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                ExpectedEventCommitment = new NetherEventCommitment(
                    EventId: 9301,
                    EventPartId: 9302,
                    OptionNumber: 1,
                    Effects: [effect],
                    ProjectedErosion: 19,
                    HpDelta: 0
                )
                {
                    FloorId = 10,
                    NodeId = 1,
                    ProjectedNetherGold = 110,
                    ProjectedTreasureKeys = 0,
                },
                Options =
                [
                    new NetherEventOption(1, [effect])
                    {
                        EventId = 9301,
                        EventPartId = 9302,
                        RequiresExactBinding = true,
                        FloorId = 10,
                        NodeId = 1,
                    },
                ],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, decision.PauseReason);
    }

    [Fact]
    public void Event_popup_with_missing_research_route_resource_and_semantic_evidence_pauses_before_payment()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                Options =
                [
                    new NetherEventOption(1, [new NetherEffect(NetherEffectKind.AbyssCodeOffer, 0)])
                    {
                        EventId = 9401,
                        EventPartId = 9402,
                        RequiresExactBinding = true,
                        FloorId = 10,
                        NodeId = 1,
                    },
                ],
            },
            Settings(strategy: NetherStrategyMode.Research)
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.BindingUnavailable, decision.PauseReason);
    }

    [Fact]
    public void Event_popup_with_unknown_research_production_evidence_pauses_before_payment()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                EventStrategyEvidence = new NetherEventStrategyEvidence
                {
                    IsKnown = false,
                    Mode = NetherStrategyMode.Research,
                },
                Options =
                [
                    new NetherEventOption(1, [new NetherEffect(NetherEffectKind.AbyssCodeOffer, 0)])
                    {
                        EventId = 9411,
                        EventPartId = 9412,
                        RequiresExactBinding = true,
                        FloorId = 10,
                        NodeId = 1,
                    },
                ],
            },
            Settings(strategy: NetherStrategyMode.Research)
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.BindingUnavailable, decision.PauseReason);
    }

    [Fact]
    public void Exact_event_reward_commitment_mismatch_pauses_before_payment()
    {
        NetherEventRewardEvidence liveReward = new(9501, 9501, 91, NetherRewardRarity.Gold, 1);
        NetherEffect item = new(NetherEffectKind.Item, 1)
        {
            ContentId = 9501,
            RewardEvidence = liveReward,
        };
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                ExpectedEventCommitment = new NetherEventCommitment(
                    EventId: 9502,
                    EventPartId: 9503,
                    OptionNumber: 1,
                    Effects: [item],
                    ProjectedErosion: 20,
                    HpDelta: 0
                )
                {
                    FloorId = 10,
                    NodeId = 1,
                    Reward = new NetherEventRewardEvidence(
                        9501,
                        9501,
                        91,
                        NetherRewardRarity.Red,
                        1
                    ),
                    ProjectedNetherGold = 100,
                    ProjectedTreasureKeys = 0,
                },
                Options =
                [
                    new NetherEventOption(1, [item])
                    {
                        EventId = 9502,
                        EventPartId = 9503,
                        RequiresExactBinding = true,
                        FloorId = 10,
                        NodeId = 1,
                        RewardEvidence = liveReward,
                    },
                ],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, decision.PauseReason);
    }

    [Fact]
    public void Exact_event_projected_resource_commitment_mismatch_pauses_before_payment()
    {
        NetherEffect gold = new(NetherEffectKind.NetherGoldGain, 10);
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                ExpectedEventCommitment = new NetherEventCommitment(
                    EventId: 9601,
                    EventPartId: 9602,
                    OptionNumber: 1,
                    Effects: [gold],
                    ProjectedErosion: 20,
                    HpDelta: 0
                )
                {
                    FloorId = 10,
                    NodeId = 1,
                    ProjectedNetherGold = 999,
                    ProjectedTreasureKeys = 0,
                },
                Options =
                [
                    new NetherEventOption(1, [gold])
                    {
                        EventId = 9601,
                        EventPartId = 9602,
                        RequiresExactBinding = true,
                        FloorId = 10,
                        NodeId = 1,
                    },
                ],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.StaleEventCommitment, decision.PauseReason);
    }

    [Fact]
    public void Equipment_popup_uses_explicit_mode_for_exact_reward_order_without_extra_projection()
    {
        NetherEffect item = new(NetherEffectKind.Item, 1)
        {
            ContentId = 9401,
            RewardEvidence = new NetherEventRewardEvidence(
                9401,
                9401,
                91,
                NetherRewardRarity.Red,
                1
            ),
        };
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                Options =
                [
                    new NetherEventOption(1, [new NetherEffect(NetherEffectKind.AbyssCodeOffer, 0)]),
                    new NetherEventOption(2, [item])
                    {
                        RewardEvidence = item.RewardEvidence,
                    },
                ],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(2, decision.Action.OptionNumber);
    }

    [Fact]
    public void Recovery_and_treasure_use_their_distinct_policies()
    {
        NetherPopupDispatchDecision recovery = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options = [new NetherEventOption(1, [new NetherEffect(NetherEffectKind.NetherGoldUsed, 0)])],
            },
            Settings()
        );
        NetherPopupDispatchDecision treasure = NetherPopupDispatchPolicy.Decide(
            Snapshot(keys: 1),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Treasure,
                Options = [new NetherEventOption(1, [new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1), new NetherEffect(NetherEffectKind.Item, 1)])],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, recovery.Kind);
        Assert.Equal(NetherPopupDispatchKind.NativeAction, treasure.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, recovery.Action.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, treasure.Action.Kind);
    }

    [Fact]
    public void Recovery_projection_applies_active_category_erosion_relief()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options = [new NetherEventOption(2, [new NetherEffect(NetherEffectKind.Heal, 300)])],
            },
            Settings(),
            new NetherActiveCodeErosionProjection
            {
                ErosionProjectionKnown = true,
                CodeHash = "nether-codes:safe-category-threshold",
                ErosionEffects =
                [
                    new NetherCodeEffect(30000, NetherCodeEffectKind.ErosionAdditionDown, 5),
                ],
            }
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.True(decision.HasEffectProjection);
        Assert.Equal(15, decision.ProjectedErosion);
        Assert.True(decision.Action.HasExpectedErosionDelta);
        Assert.Equal(-5, decision.Action.ExpectedErosionDelta);
    }

    [Fact]
    public void Recovery_transform_real_popup_flow_requires_zero_value_rest_and_purification_then_commits_hard_excluded_code()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly.dll 573fa800...e1fb:
        // NetherRecoveryFloorEventFlow opens NetherRecoverPopupController from floor type 5;
        // InitializeView resolves exactly three MNetherFloorEvents option parts and target_type=7
        // opens the separate AbyssCodeListPopupType.Change flow. The transform removal therefore
        // has to be committed while the exact Recovery options are still visible.
        NetherSnapshot snapshot = Snapshot() with
        {
            ErosionPoint = 0,
            CodeCapacity = 25,
            Codes =
            [
                new NetherCodeState(9001, NetherCodeFamily.Risk, 1)
                {
                    Category = NetherCodeCategory.Risk,
                    Rarity = 3,
                    Power = 999_999,
                },
            ],
        };
        NetherAutoClimbSettings settings = Settings() with
        {
            StrategyMode = NetherStrategyMode.Equipment,
            EquipmentRecoveryCodeTransformEnabled = true,
        };
        var hardEvidence = new NetherCodeTransformHardExclusionEvidence
        {
            IsKnown = true,
            HardExcludedCodes =
            [
                new NetherCodeTransformHardExclusion(
                    9001,
                    NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment
                ),
            ],
        };

        NetherPopupDispatchDecision recovery = NetherPopupDispatchPolicy.Decide(
            snapshot,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options =
                [
                    new NetherEventOption(1, [new NetherEffect(NetherEffectKind.Heal, 300)]),
                    new NetherEventOption(2, [new NetherEffect(NetherEffectKind.ErosionHeal, 30)]),
                    new NetherEventOption(3, [new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0)]),
                ],
            },
            settings,
            NoActiveErosion(),
            hardEvidence
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, recovery.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, recovery.Action.Kind);
        Assert.Equal(3, recovery.Action.OptionNumber);
        Assert.Equal(9001, recovery.Action.CodeId);

        NetherPopupDispatchDecision transform = NetherPopupDispatchPolicy.Decide(
            snapshot,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeTransform,
                CodeTransformCommitment = new NetherCodeTransformCommitment(9001),
            },
            settings,
            NoActiveErosion(),
            hardEvidence
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, transform.Kind);
        Assert.Equal(NetherActionKind.TransformCode, transform.Action.Kind);
        Assert.Equal(9001, transform.Action.ReplaceCodeId);
    }

    [Theory]
    [InlineData((int)NetherStrategyMode.Research, true, 0, 1000)]
    [InlineData((int)NetherStrategyMode.Equipment, false, 0, 1000)]
    [InlineData((int)NetherStrategyMode.Equipment, true, 20, 1000)]
    [InlineData((int)NetherStrategyMode.Equipment, true, 0, 700)]
    public void Recovery_transform_real_popup_flow_rejects_mode_opt_in_and_nonzero_deterministic_value(
        int rawMode,
        bool optIn,
        int erosion,
        int hp
    )
    {
        NetherSnapshot snapshot = Snapshot() with
        {
            ErosionPoint = erosion,
            CodeCapacity = 25,
            Characters = [new NetherCharacterState(1, hp)],
            Codes = [new NetherCodeState(9001, NetherCodeFamily.Risk, 1)],
        };
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            snapshot,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options =
                [
                    new NetherEventOption(1, [new NetherEffect(NetherEffectKind.Heal, 300)]),
                    new NetherEventOption(2, [new NetherEffect(NetherEffectKind.ErosionHeal, 30)]),
                    new NetherEventOption(3, [new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0)]),
                ],
            },
            Settings() with
            {
                StrategyMode = (NetherStrategyMode)rawMode,
                EquipmentRecoveryCodeTransformEnabled = optIn,
            },
            NoActiveErosion(),
            new NetherCodeTransformHardExclusionEvidence
            {
                IsKnown = true,
                HardExcludedCodes =
                [
                    new NetherCodeTransformHardExclusion(
                        9001,
                        NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment
                    ),
                ],
            }
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.Action.Kind);
        Assert.NotEqual(3, decision.Action.OptionNumber);
        Assert.Equal(0, decision.Action.CodeId);
    }

    [Fact]
    public void Shop_off_leaves_through_native_close_callback()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext { Kind = NetherRuntimePopupKind.Shop, ShopContents = [] },
            Settings(shop: NetherShopMode.Off)
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.LeaveShop, decision.Action.Kind);
    }

    [Fact]
    public void Rank_five_shop_key_commitment_is_carried_into_the_buy_action()
    {
        NetherShopProcurementCommitment commitment = new()
        {
            IsKnown = true,
            RequiresRankFiveKey = true,
            Objective = new NetherRankFiveTreasureIdentity(4, 401, 4011),
            KeyContentId = 3001,
            KeyCost = 200,
        };
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot() with { NetherGold = 250 },
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Shop,
                ShopProcurementCommitment = commitment,
                ShopContents =
                [
                    new NetherShopContent(
                        3001,
                        0,
                        0,
                        NetherRewardRarity.NoEffect,
                        200,
                        usesNetherGold: true
                    )
                    {
                        IsTreasureKey = true,
                    },
                ],
            },
            Settings(shop: NetherShopMode.EquipmentBags)
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.BuyShopItem, decision.Action.Kind);
        Assert.Equal(commitment, decision.Action.ShopProcurementCommitment);
    }

    private static NetherSnapshot Snapshot(int keys = 0) => new()
    {
        ErosionPoint = 20,
        NetherGold = 100,
        TreasureKeyCount = keys,
        Characters = [new NetherCharacterState(1, 1000)],
    };

    private static NetherAutoClimbSettings Settings(
        NetherShopMode shop = NetherShopMode.Off,
        NetherStrategyMode strategy = NetherStrategyMode.Equipment
    ) => new()
    {
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = 300,
        TreasureMode = NetherTreasureMode.KeyOnly,
        ShopMode = shop,
        StrategyMode = strategy,
    };

    private static NetherActiveCodeErosionProjection NoActiveErosion() => new()
    {
        ErosionProjectionKnown = true,
        CodeHash = "nether-codes:none",
        ErosionEffects = [],
    };
}
