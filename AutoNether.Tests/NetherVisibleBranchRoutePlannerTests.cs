#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherVisibleBranchRoutePlannerTests
{
    [Fact]
    public void Complete_visible_branch_counts_beat_recovery_count_only_after_semantic_tiers_match()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode normal = Node(2, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode eliteA = Node(4, 82, NetherFloorNodeType.MiniBoss, 2);
        NetherFloorNode eliteB = Node(5, 83, NetherFloorNodeType.MiniBoss, 4);
        NetherFloorNode elite = Node(3, 81, NetherFloorNodeType.MiniBoss, 1);
        NetherFloorNode recoveryA = Node(6, 82, NetherFloorNodeType.Recovery, 3);
        NetherFloorNode recoveryB = Node(7, 83, NetherFloorNodeType.Recovery, 6);
        NetherFloorNode boss = Node(8, 84, NetherFloorNodeType.Boss, 5, 7);
        NetherFloorNode[] floors = [current, normal, eliteA, eliteB, elite, recoveryA, recoveryB, boss];

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4, 5, 8),
                    [3] = Horizon(floors, 3, 6, 7, 8),
                }
            )
        );

        Assert.Null(plan.SelectedNode);
        Assert.Equal(NetherPauseReason.UnknownMasterData, plan.PauseReason);
        Assert.Equal("visible-route-vector-unavailable-for-production", plan.PauseDetail);
    }

    [Fact]
    public void Ineligible_shop_is_compared_before_recovery_count()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode ineligibleShop = Node(2, 81, NetherFloorNodeType.Shop, 1);
        NetherFloorNode recovery = Node(3, 81, NetherFloorNodeType.Recovery, 1);
        NetherFloorNode shopBoss = Node(4, 82, NetherFloorNodeType.Boss, 2);
        NetherFloorNode recoveryBoss = Node(5, 82, NetherFloorNodeType.Boss, 3);
        NetherFloorNode[] floors = [current, ineligibleShop, recovery, shopBoss, recoveryBoss];

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 5),
                },
                rows: [ShopRow(2, 2202)]
            )
        );

        // An ineligible Shop is a safety-negative semantic tier. It must lose to the branch with
        // no ineligible Shop even though that branch contains one Recovery node.
        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Research_incomplete_puts_exact_direct_code_offer_before_normal_battle_but_equipment_reverses_it()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode directOffer = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, directOffer, normal, boss];
        NetherStrategyVisibleContentRow directOfferRow = DirectCodeOffer(2);

        NetherSnapshot snapshot = Snapshot(current, floors);
        NetherRouteSafetyContext research = Context(
            floors,
            new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
            {
                [2] = Horizon(floors, 2, 4),
                [3] = Horizon(floors, 3, 4),
            },
            rows: [directOfferRow],
            mode: NetherStrategyMode.Research,
            researchIncomplete: true
        );
        NetherRouteSafetyContext equipment = research with
        {
            StrategyMode = NetherStrategyMode.Equipment,
            ResearchIncomplete = false,
        };

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(new NetherRoutePlanner().Plan(snapshot, research).SelectedNode).NodeId);
        Assert.Equal(3, Assert.IsType<NetherFloorNode>(new NetherRoutePlanner().Plan(snapshot, equipment).SelectedNode).NodeId);
    }

    [Fact]
    public void Unknown_research_completion_pauses_instead_of_using_completed_equipment_order()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode directOffer = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, directOffer, normal, boss];

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: [DirectCodeOffer(2)],
                mode: NetherStrategyMode.Research,
                researchIncomplete: null
            )
        );

        Assert.False(plan.HasSelection);
        Assert.Equal(NetherPauseReason.UnknownMasterData, plan.PauseReason);
        Assert.Equal("research-completion-state-unknown-for-visible-route-vector", plan.PauseDetail);
    }

    [Fact]
    public void Production_route_pauses_when_visible_branch_vector_is_missing()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode directOffer = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, directOffer, normal, boss];

        NetherRouteSafetyContext context = Context(
            floors,
            new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
            {
                [2] = Horizon(floors, 2, 4),
                [3] = Horizon(floors, 3, 4),
            },
            rows: [DirectCodeOffer(2)],
            mode: NetherStrategyMode.Research,
            researchIncomplete: true
        ) with
        {
            VisibleMap = null,
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            context
        );

        Assert.Null(plan.SelectedNode);
        Assert.Equal(NetherPauseReason.UnknownMasterData, plan.PauseReason);
        Assert.Equal("visible-route-vector-unavailable-for-production", plan.PauseDetail);
    }

    [Fact]
    public void Production_route_excludes_candidate_when_visible_horizon_is_missing()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode directOffer = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, directOffer, normal, boss];

        NetherRouteSafetyContext context = Context(
            floors,
            new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
            {
                [3] = Horizon(floors, 3, 4),
            },
            rows: [DirectCodeOffer(2)],
            mode: NetherStrategyMode.Research,
            researchIncomplete: true
        );

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            context
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
        Assert.Contains(plan.Audit, audit =>
            audit.FloorId == 2
            && audit.Reason == "unknown-node"
            && audit.Detail == "visible-route-horizon-unavailable"
        );
    }

    [Fact]
    public void Production_route_excludes_candidate_when_visible_horizon_vector_is_empty()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode directOffer = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, directOffer, normal, boss];

        NetherRouteSafetyContext context = Context(
            floors,
            new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
            {
                [2] = new NetherRouteHorizonSafetyEvaluation
                {
                    IsEligible = true,
                    HorizonSteps = Array.Empty<NetherRouteHorizonStep>(),
                    Steps = Array.Empty<NetherRouteHorizonStepAudit>(),
                },
                [3] = Horizon(floors, 3, 4),
            },
            rows: [DirectCodeOffer(2)],
            mode: NetherStrategyMode.Research,
            researchIncomplete: true
        );

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            context
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
        Assert.Contains(plan.Audit, audit =>
            audit.FloorId == 2
            && audit.Reason == "unknown-node"
            && audit.Detail == "visible-route-vector-unavailable"
        );
    }

    [Fact]
    public void Four_native_event_parts_keep_exact_direct_offer_when_another_part_has_unknown_battle_tier()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode eventNode = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, eventNode, normal, boss];
        const long eventId = 2002;

        NetherStrategyVisibleContentRow unknownPart = EventPart(2, eventId, 2101) with
        {
            IsKnown = false,
            UnknownReason = "event-part-not-selected-by-authoritative-policy",
        };
        NetherStrategyVisibleContentRow ordinaryPart = EventPart(2, eventId, 2102);
        NetherStrategyVisibleContentRow directPart = EventPart(
            2,
            eventId,
            2103,
            new NetherStrategyVisibleEventEffectEvidence(
                NetherStrategyVisibleEventEffectSource.Content,
                160,
                1
            )
            {
                IsPresent = true,
                IsKnown = true,
                EffectKind = NetherEffectKind.AbyssCodeOffer,
                Amount = 1,
            }
        );
        NetherStrategyVisibleContentRow unknownBattlePart = EventPart(2, eventId, 2104);
        NetherStrategyVisibleContentRow unknownBattle = new(
            NetherStrategyVisibleContentKind.Battle,
            2,
            4104,
            4104
        )
        {
            EventId = eventId,
            EventPartId = 2104,
            IsKnown = false,
            EventBattleTier = NetherEventBattleTier.Unknown,
            UnknownReason = "event-battle-semantic-tier-unavailable-for-raw-type",
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors, netherGold: 300),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: [unknownPart, ordinaryPart, directPart, unknownBattlePart, unknownBattle],
                mode: NetherStrategyMode.Research,
                researchIncomplete: true
            )
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Event_vector_uses_the_event_policy_selected_part_instead_of_highest_battle_tier()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode eventNode = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, eventNode, normal, boss];
        const long eventId = 2202;
        const long blockedBossPartId = 2203;
        const long ordinaryPartId = 2204;

        NetherStrategyVisibleContentRow blockedBossPart = EventPart(
            2,
            eventId,
            blockedBossPartId,
            new NetherStrategyVisibleEventEffectEvidence(
                NetherStrategyVisibleEventEffectSource.Target1,
                (int)NetherEffectKind.Battle,
                9201
            )
            {
                IsPresent = true,
                IsKnown = true,
                EffectKind = NetherEffectKind.Battle,
                Amount = 1,
            },
            new NetherStrategyVisibleEventEffectEvidence(
                NetherStrategyVisibleEventEffectSource.Target2,
                (int)NetherEffectKind.NetherGoldUsed,
                400
            )
            {
                IsPresent = true,
                IsKnown = true,
                EffectKind = NetherEffectKind.NetherGoldUsed,
                Amount = 400,
            }
        );
        NetherStrategyVisibleContentRow ordinaryPart = EventPart(
            2,
            eventId,
            ordinaryPartId,
            new NetherStrategyVisibleEventEffectEvidence(
                NetherStrategyVisibleEventEffectSource.Target1,
                (int)NetherEffectKind.Heal,
                0
            )
            {
                IsPresent = true,
                IsKnown = true,
                EffectKind = NetherEffectKind.Heal,
                Amount = 0,
            }
        );
        NetherStrategyVisibleContentRow typedBoss = new(
            NetherStrategyVisibleContentKind.Battle,
            2,
            9201,
            9201
        )
        {
            EventId = eventId,
            EventPartId = blockedBossPartId,
            IsKnown = true,
            EventBattleTier = NetherEventBattleTier.Boss,
            BattleStageId = 1,
            BattleType = 3,
            CodeDropRatio = 1000,
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors, netherGold: 0),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: [blockedBossPart, ordinaryPart, typedBoss],
                mode: NetherStrategyMode.Research,
                researchIncomplete: true
            )
        );

        // The Boss-looking option is rejected by the actual Event policy because it spends more
        // Gold than the authoritative snapshot holds. The surviving ordinary part has no vector
        // reward, so the competing Normal Battle wins. A fixed highest-tier mapper would choose 2.
        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Event_vector_resolves_typed_battle_and_item_siblings_from_the_complete_visible_rows()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode eventNode = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, eventNode, normal, boss];
        const long eventId = 2402;
        const long partId = 2403;

        NetherStrategyVisibleContentRow eventPart = EventPart(
            2,
            eventId,
            partId,
            new NetherStrategyVisibleEventEffectEvidence(
                NetherStrategyVisibleEventEffectSource.Target1,
                (int)NetherEffectKind.Battle,
                9501
            )
            {
                IsPresent = true,
                IsKnown = true,
                EffectKind = NetherEffectKind.Battle,
                Amount = 1,
            },
            new NetherStrategyVisibleEventEffectEvidence(
                NetherStrategyVisibleEventEffectSource.Content,
                (int)NetherEffectKind.Item,
                9502
            )
            {
                IsPresent = true,
                IsKnown = true,
                EffectKind = NetherEffectKind.Item,
                ContentId = 9502,
                Amount = 1,
            }
        );
        NetherStrategyVisibleContentRow typedBattle = new(
            NetherStrategyVisibleContentKind.Battle,
            2,
            9501,
            9501
        )
        {
            EventId = eventId,
            EventPartId = partId,
            IsKnown = true,
            BattleStageId = 9511,
            BattleType = 7,
            CodeDropRatio = 1000,
            EventBattleTier = NetherEventBattleTier.Boss,
        };
        NetherStrategyVisibleContentRow typedItem = new(
            NetherStrategyVisibleContentKind.Item,
            2,
            9502,
            9502
        )
        {
            EventId = eventId,
            EventPartId = partId,
            IsKnown = true,
            ItemType = 91,
            ItemRarity = (int)NetherRewardRarity.Gold,
            Amount = 1,
            CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: [eventPart, typedBattle, typedItem],
                interactive: TypedBattleRouteProof(
                    Snapshot(current, floors),
                    eventNode,
                    eventId,
                    partId,
                    battleId: 9501,
                    projectedErosion: 20,
                    projectedHpDelta: 0,
                    semanticTier: NetherEventBattleTier.Boss
                )
            )
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Event_battle_without_typed_route_safety_projection_is_rejected()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode eventNode = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, eventNode, normal, boss];
        const long eventId = 2602;
        const long partId = 2603;
        const long battleId = 9601;

        NetherStrategyVisibleContentRow eventPart = EventPart(
            2,
            eventId,
            partId,
            new NetherStrategyVisibleEventEffectEvidence(
                NetherStrategyVisibleEventEffectSource.Target1,
                (int)NetherEffectKind.Battle,
                battleId
            )
            {
                IsPresent = true,
                IsKnown = true,
                EffectKind = NetherEffectKind.Battle,
                Amount = battleId,
            }
        );
        // EventBattleTier is authoritative typed provider output. Raw BattleType is retained only
        // as diagnostics; the route still lacks the separate projected HP/erosion/ownership proof.
        NetherStrategyVisibleContentRow typedBattle = new(
            NetherStrategyVisibleContentKind.Battle,
            2,
            battleId,
            battleId
        )
        {
            EventId = eventId,
            EventPartId = partId,
            IsKnown = true,
            BattleStageId = 9602,
            BattleType = 7,
            CodeDropRatio = 1000,
            EventBattleTier = NetherEventBattleTier.Boss,
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: [eventPart, typedBattle]
            )
        );

        // A typed semantic tier alone cannot authorize the event battle route candidate. The
        // complete snapshot-scoped route proof is absent, so the known normal branch wins.
        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Production_mapper_shop_rarity_flows_into_late_shop_route_value()
    {
        NetherFloorNode current = Node(1, 95, NetherFloorNodeType.Recovery);
        NetherFloorNode shop = Node(2, 96, NetherFloorNodeType.Shop, 1);
        NetherFloorNode normal = Node(3, 96, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 97, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, shop, normal, boss];
        NetherStrategyVisibleEvidenceCaptureResult mapped = NetherStrategyVisibleEvidenceMapper.Map(
            new NetherStrategyVisibleEvidenceCaptureRequest(
                floors,
                [],
                [],
                [],
                [],
                []
            )
            {
                ShopInventoryByNodeId = new Dictionary<long, NetherStrategyShopInventoryCapture>
                {
                    [2] = new NetherStrategyShopInventoryCapture(
                        true,
                        [new NetherShopContent(9601, 9602, 91, NetherRewardRarity.Gold, 300, true)],
                        string.Empty
                    ),
                },
                TypedSemanticProvider = new NetherStrategyTypedSemanticProviderEvidence
                {
                    CanonicalRewardTiers =
                    [new NetherCanonicalRewardTierProviderEvidence(9602, NetherCanonicalRewardTier.GoldRankFive, 91)],
                },
            }
        );

        Assert.True(mapped.IsSuccess, mapped.Detail);
        NetherStrategyVisibleContentRow shopRow = Assert.Single(
            mapped.Evidence!.ContentRows,
            row => row.Kind == NetherStrategyVisibleContentKind.ShopInventory
        );
        Assert.Equal(NetherRewardRarity.Gold, (NetherRewardRarity)shopRow.ItemRarity);
        Assert.Equal(NetherCanonicalRewardTier.GoldRankFive, shopRow.CanonicalRewardTier);

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors, netherGold: 300),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: mapped.Evidence.ContentRows,
                researchIncomplete: false
            )
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Production_raw_capture_mapper_provider_counts_two_shops_against_one_canonical_treasure()
    {
        (NetherFloorNode[] floors, NetherStrategyVisibleMapEvidence visible) =
            MapProviderBackedCanonicalRouteEvidence(twoShops: true);

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(floors[0], floors, netherGold: 300),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 5),
                    [3] = Horizon(floors, 3, 4, 5),
                },
                rows: visible.ContentRows,
                researchIncomplete: false
            )
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Production_mapper_canonical_reward_reaches_key_objective_without_raw_rarity_shortcut()
    {
        (_, NetherStrategyVisibleMapEvidence visible) = MapProviderBackedCanonicalRouteEvidence(twoShops: false);

        IReadOnlySet<long> objectiveNodes = NetherRankFiveKeyProcurementPolicy.FindKnownObjectiveNodes(
            currentTreasureKeys: 0,
            visible
        );

        Assert.Contains(2, objectiveNodes);
    }

    [Fact]
    public void Production_raw_capture_mapper_provider_treasure_wins_one_to_one_shop_tie()
    {
        (NetherFloorNode[] floors, NetherStrategyVisibleMapEvidence visible) =
            MapProviderBackedCanonicalRouteEvidence(twoShops: false);

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(floors[0], floors, netherGold: 300),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 5),
                    [3] = Horizon(floors, 3, 5),
                    [4] = Horizon(floors, 4, 5),
                },
                rows: visible.ContentRows,
                researchIncomplete: false
            )
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Production_raw_capture_mapper_provider_typed_battle_reaches_event_route_vector()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode eventFloor = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, eventFloor, normal, boss];
        NetherSnapshot snapshot = Snapshot(current, floors);
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            EventBattleTiers =
            [new NetherEventBattleTierProviderEvidence(8101, NetherEventBattleTier.Boss)],
            EventBattleRouteSafety =
            [new NetherEventBattleRouteSafetyProviderEvidence(
                8001,
                8002,
                1,
                eventFloor.FloorId,
                eventFloor.NodeId,
                8101,
                20,
                0,
                [1]
            )],
        };
        NetherRuntimeBridge bridge = new(_ =>
            new NetherRuntimeTypedSemanticProviderScope(snapshot.Fingerprint, provider));
        NetherRuntimeInteractivePreEntryCaptureResult captured = bridge.CaptureInteractivePreEntryFloor(
            snapshot,
            new NetherAutoClimbSettings(),
            new RuntimeFloorFixture
            {
                MNetherMapFloorId = eventFloor.FloorId,
                ExtendId = 8001,
                FloorType = (int)NetherFloorNodeType.Event,
            },
            mapFloorRows: null,
            eventRows: new object[]
            {
                new RuntimeEventFixture
                {
                    id = 8001,
                    m_nether_map_floor_id = eventFloor.FloorId,
                    weight = 1,
                    type = 4,
                    m_nether_floor_event_part_id_1 = 8002,
                },
            },
            eventPartRows: new object[]
            {
                new RuntimePartFixture
                {
                    id = 8002,
                    target_type_1 = (int)NetherEffectKind.Battle,
                    select_parameter_1 = 8101,
                },
            },
            itemRows: null,
            battleRows: new object[]
            {
                new RuntimeBattleFixture
                {
                    id = 8101,
                    m_nether_map_floor_id = eventFloor.FloorId,
                    type = 7,
                    m_nether_battle_stage_id = 8102,
                    code_drop_ratio = 1000,
                },
            },
            floorNodeId: eventFloor.NodeId,
            canCloseShop: false
        );
        NetherRuntimeInteractivePreEntryInputsResult interactive =
            NetherRuntimeInteractivePreEntryInputsResult.Success(
                new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
                {
                    [eventFloor.NodeId] = captured,
                },
                snapshot.Fingerprint,
                provider
            );
        NetherStrategyVisibleEvidenceCaptureResult mapped = NetherStrategyVisibleEvidenceAssembler.Assemble(
            new NetherStrategyVisibleEvidenceAssemblyRequest(
                snapshot,
                interactive,
                NetherRuntimePopupResult.Failure("no-current-popup"),
                new NetherStrategyVisibleEvidenceCaptureRequest(
                    floors,
                    [new NetherStrategyBattleMasterRow(8101, eventFloor.FloorId, 7, 8102, 1000)],
                    [],
                    [new NetherFloorEventMasterRow(8001, eventFloor.FloorId, 1, 8002, 0, 0, 0)],
                    [new NetherFloorEventPartMasterRow(8002, 8, 8101, 0, 0, 0, 0, 0, 0, 0)],
                    []
                )
            )
        );

        Assert.True(mapped.IsSuccess, mapped.Detail);
        NetherStrategyVisibleContentRow battle = Assert.Single(
            mapped.Evidence!.ContentRows,
            row => row.Kind == NetherStrategyVisibleContentKind.Battle
                && row.EventPartId == 8002
        );
        Assert.True(battle.IsKnown, battle.UnknownReason);
        Assert.Equal(NetherEventBattleTier.Boss, battle.EventBattleTier);

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: mapped.Evidence.ContentRows,
                researchIncomplete: false,
                interactive: interactive
            )
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Canonical_rank_five_treasure_beats_nonterminal_combat_when_shop_is_not_affordable()
    {
        NetherFloorNode current = Node(1, 95, NetherFloorNodeType.Recovery);
        NetherFloorNode treasure = Node(2, 96, NetherFloorNodeType.Treasure, 1);
        NetherFloorNode shop = Node(3, 96, NetherFloorNodeType.Shop, 1);
        NetherFloorNode normal = Node(4, 96, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(5, 97, NetherFloorNodeType.Boss, 2, 3, 4);
        NetherFloorNode[] floors = [current, treasure, shop, normal, boss];
        NetherStrategyVisibleContentRow treasureRow = new(
            NetherStrategyVisibleContentKind.Treasure,
            2,
            2002,
            2003
        )
        {
            IsKnown = true,
            EventId = 2003,
        };
        NetherStrategyVisibleContentRow rankFiveGold = new(
            NetherStrategyVisibleContentKind.Item,
            2,
            2004,
            2004
        )
        {
            IsKnown = true,
            EventId = 2003,
            EventPartId = 2004,
            ItemType = 91,
            ItemRarity = (int)NetherRewardRarity.UniqueWeapon,
            Rank = 5,
            Amount = 1,
            CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
        };
        NetherStrategyVisibleContentRow shopRow = new(
            NetherStrategyVisibleContentKind.ShopInventory,
            3,
            3003,
            3004
        )
        {
            IsKnown = true,
            Cost = 300,
            Amount = 1,
            ItemType = 91,
            ItemRarity = (int)NetherRewardRarity.Gold,
            UsesNetherGold = true,
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors, netherGold: 0),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 5),
                    [3] = Horizon(floors, 3, 5),
                    [4] = Horizon(floors, 4, 5),
                },
                rows: [treasureRow, rankFiveGold, shopRow]
            )
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Two_eligible_late_shops_beat_one_gold_treasure_before_treasure_tie_break()
    {
        NetherFloorNode current = Node(1, 95, NetherFloorNodeType.Recovery);
        NetherFloorNode treasure = Node(2, 96, NetherFloorNodeType.Treasure, 1);
        NetherFloorNode firstShop = Node(3, 96, NetherFloorNodeType.Shop, 1);
        NetherFloorNode secondShop = Node(4, 97, NetherFloorNodeType.Shop, 3);
        NetherFloorNode boss = Node(5, 98, NetherFloorNodeType.Boss, 2, 4);
        NetherFloorNode[] floors = [current, treasure, firstShop, secondShop, boss];
        NetherStrategyVisibleContentRow treasureRow = TreasureRow(2, 2003);
        NetherStrategyVisibleContentRow goldReward = RankFiveReward(
            2,
            2003,
            2004,
            NetherRewardRarity.Gold,
            NetherCanonicalRewardTier.GoldRankFive
        );
        NetherStrategyVisibleContentRow firstShopRow = ShopRow(3, 3003);
        NetherStrategyVisibleContentRow secondShopRow = ShopRow(4, 3004);

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors, netherGold: 300),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 5),
                    [3] = Horizon(floors, 3, 4, 5),
                },
                rows: [treasureRow, goldReward, firstShopRow, secondShopRow],
                researchIncomplete: false
            )
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void One_gold_treasure_and_one_eligible_shop_tie_prefers_the_treasure()
    {
        NetherFloorNode current = Node(1, 95, NetherFloorNodeType.Recovery);
        NetherFloorNode treasure = Node(2, 96, NetherFloorNodeType.Treasure, 1);
        NetherFloorNode shop = Node(3, 96, NetherFloorNodeType.Shop, 1);
        NetherFloorNode normal = Node(4, 96, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(5, 97, NetherFloorNodeType.Boss, 2, 3, 4);
        NetherFloorNode[] floors = [current, treasure, shop, normal, boss];

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors, netherGold: 300),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 5),
                    [3] = Horizon(floors, 3, 5),
                    [4] = Horizon(floors, 4, 5),
                },
                rows:
                [
                    TreasureRow(2, 2703),
                    RankFiveReward(
                        2,
                        2703,
                        2704,
                        NetherRewardRarity.UniqueWeapon,
                        NetherCanonicalRewardTier.GoldRankFive
                    ),
                    ShopRow(3, 3703),
                ],
                researchIncomplete: false
            )
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Treasure_rank_shortcut_without_native_rarity_does_not_create_rank_five_value()
    {
        NetherFloorNode current = Node(1, 95, NetherFloorNodeType.Recovery);
        NetherFloorNode treasure = Node(2, 96, NetherFloorNodeType.Treasure, 1);
        NetherFloorNode normal = Node(3, 96, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 97, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, treasure, normal, boss];
        NetherStrategyVisibleContentRow treasureRow = TreasureRow(2, 2003);
        NetherStrategyVisibleContentRow unsupportedRankShortcut = RankFiveReward(
            2,
            2003,
            2004,
            NetherRewardRarity.Purple
        );

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: [treasureRow, unsupportedRankShortcut],
                researchIncomplete: false
            )
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Theory]
    [InlineData((int)NetherRewardRarity.Gold)]
    [InlineData((int)NetherRewardRarity.Red)]
    public void Noncanonical_gold_or_red_rank_shortcut_does_not_create_rank_five_value(int rarity)
    {
        NetherFloorNode current = Node(1, 95, NetherFloorNodeType.Recovery);
        NetherFloorNode treasure = Node(2, 96, NetherFloorNodeType.Treasure, 1);
        NetherFloorNode normal = Node(3, 96, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 97, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, treasure, normal, boss];

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows:
                [
                    TreasureRow(2, 2303),
                    RankFiveReward(2, 2303, 2304, (NetherRewardRarity)rarity),
                ],
                researchIncomplete: false
            )
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Equipment_mode_does_not_use_research_order_when_completion_is_true()
    {
        NetherRoutePlan plan = PlanDirectOfferVsNormal(
            NetherStrategyMode.Equipment,
            researchIncomplete: true
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Equipment_mode_does_not_pause_when_completion_is_unknown()
    {
        NetherRoutePlan plan = PlanDirectOfferVsNormal(
            NetherStrategyMode.Equipment,
            researchIncomplete: null
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Production_empty_visible_vector_pauses_instead_of_using_legacy_route_comparator()
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode normal = Node(2, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(3, 82, NetherFloorNodeType.Boss, 2);
        NetherFloorNode[] floors = [current, normal, boss];

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            new NetherRouteSafetyContext
            {
                StrategyMode = NetherStrategyMode.Equipment,
                ResearchIncomplete = false,
                VisibleMap = new NetherStrategyVisibleMapEvidence(floors, []),
                HorizonEvaluationByFloorId = new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 3),
                },
            }
        );

        Assert.Null(plan.SelectedNode);
        Assert.Equal(NetherPauseReason.UnknownMasterData, plan.PauseReason);
        Assert.Equal("visible-route-vector-unavailable-for-production", plan.PauseDetail);
    }

    [Fact]
    public void Research_mode_uses_equipment_order_only_after_completion_is_false()
    {
        NetherRoutePlan plan = PlanDirectOfferVsNormal(
            NetherStrategyMode.Research,
            researchIncomplete: false
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Unknown_shop_sibling_makes_late_shop_value_unknown()
    {
        NetherFloorNode current = Node(1, 95, NetherFloorNodeType.Recovery);
        NetherFloorNode shop = Node(2, 96, NetherFloorNodeType.Shop, 1);
        NetherFloorNode normal = Node(3, 96, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 97, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, shop, normal, boss];
        NetherStrategyVisibleContentRow exactShop = ShopRow(2, 3002);
        NetherStrategyVisibleContentRow unknownSibling = new(
            NetherStrategyVisibleContentKind.ShopInventory,
            2,
            3003,
            0
        )
        {
            IsKnown = false,
            UnknownReason = "invalid-shop-inventory-row:3003",
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors, netherGold: 300),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: [exactShop, unknownSibling],
                researchIncomplete: false
            )
        );

        Assert.Equal(3, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    [Fact]
    public void Typed_idless_key_sibling_does_not_invalidate_late_shop_value()
    {
        NetherFloorNode current = Node(1, 95, NetherFloorNodeType.Recovery);
        NetherFloorNode shop = Node(2, 96, NetherFloorNodeType.Shop, 1);
        NetherFloorNode normal = Node(3, 96, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 97, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, shop, normal, boss];
        NetherStrategyVisibleContentRow exactShop = ShopRow(2, 3002);
        NetherStrategyVisibleContentRow typedIdlessKey = new(
            NetherStrategyVisibleContentKind.ShopInventory,
            2,
            0,
            3003
        )
        {
            IsKnown = true,
            ContentType = 166,
            Amount = 1,
            Cost = 200,
            UsesNetherGold = true,
            IsTreasureKey = true,
            ShopKeyIdentity = 7001,
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            Snapshot(current, floors, netherGold: 300),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: [exactShop, typedIdlessKey],
                researchIncomplete: false
            )
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.SelectedNode).NodeId);
    }

    private static (NetherFloorNode[] Floors, NetherStrategyVisibleMapEvidence Visible)
        MapProviderBackedCanonicalRouteEvidence(bool twoShops)
    {
        NetherFloorNode current = Node(1, 95, NetherFloorNodeType.Recovery);
        NetherFloorNode treasure = Node(2, 96, NetherFloorNodeType.Treasure, 1);
        NetherFloorNode firstShop = Node(3, 96, NetherFloorNodeType.Shop, 1);
        NetherFloorNode secondShop = Node(4, 97, NetherFloorNodeType.Shop, 3);
        NetherFloorNode normal = Node(4, 96, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = twoShops
            ? Node(5, 98, NetherFloorNodeType.Boss, 2, 4)
            : Node(5, 97, NetherFloorNodeType.Boss, 2, 3, 4);
        NetherFloorNode[] floors = twoShops
            ? [current, treasure, firstShop, secondShop, boss]
            : [current, treasure, firstShop, normal, boss];
        NetherSnapshot routeSnapshot = Snapshot(current, floors, netherGold: 300);
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            CanonicalRewardTiers =
            [
                new NetherCanonicalRewardTierProviderEvidence(9001, NetherCanonicalRewardTier.GoldRankFive, 91),
                new NetherCanonicalRewardTierProviderEvidence(3004, NetherCanonicalRewardTier.GoldRankFive, 91),
                new NetherCanonicalRewardTierProviderEvidence(4004, NetherCanonicalRewardTier.GoldRankFive, 91),
            ],
        };
        NetherAutoClimbController.RegisterTypedSemanticProviderFactory(_ =>
            new NetherRuntimeTypedSemanticProviderScope(routeSnapshot.Fingerprint, provider));
        try
        {
            NetherRuntimeInteractivePreEntryCaptureResult currentCapture = CaptureFloor(current, 0);
            NetherRuntimeInteractivePreEntryCaptureResult treasureCapture = CaptureFloor(treasure, 2003);
        NetherRuntimeInteractivePreEntryInputsResult interactive =
            NetherRuntimeInteractivePreEntryInputsResult.Success(
                new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
                {
                    [current.NodeId] = currentCapture,
                    [treasure.NodeId] = treasureCapture,
                },
                routeSnapshot.Fingerprint
            );
            NetherStrategyVisibleEvidenceCaptureResult mapped = NetherStrategyVisibleEvidenceAssembler.Assemble(
            new NetherStrategyVisibleEvidenceAssemblyRequest(
                routeSnapshot,
                interactive,
                NetherRuntimePopupResult.Failure("no-current-popup"),
                new NetherStrategyVisibleEvidenceCaptureRequest(
                floors,
                [],
                [new NetherStrategyTreasureMasterRow(2002, treasure.FloorId)],
                [new NetherFloorEventMasterRow(2003, treasure.FloorId, 1, 2004, 0, 0, 0)],
                [new NetherFloorEventPartMasterRow(2004, 0, 0, 0, 0, 0, 0, 30, 9001, 1)],
                [new NetherStrategyItemMasterRow(
                    9001,
                    91,
                    (int)NetherRewardRarity.UniqueWeapon,
                    1,
                    99
                )]
            )
            {
                ShopInventoryByNodeId = new Dictionary<long, NetherStrategyShopInventoryCapture>
                {
                    [firstShop.NodeId] = new NetherStrategyShopInventoryCapture(
                        true,
                        [new NetherShopContent(3003, 3004, 91, NetherRewardRarity.Gold, 300, true)],
                        string.Empty
                    ),
                    [secondShop.NodeId] = new NetherStrategyShopInventoryCapture(
                        true,
                        [new NetherShopContent(4003, 4004, 91, NetherRewardRarity.Gold, 300, true)],
                        string.Empty
                    ),
                },
            }
            )
        );
            Assert.True(mapped.IsSuccess, mapped.Detail);
            return (floors, mapped.Evidence!);
        }
        finally
        {
            NetherAutoClimbController.RegisterTypedSemanticProviderFactory(null);
        }

        NetherRuntimeInteractivePreEntryCaptureResult CaptureFloor(
            NetherFloorNode floor,
            long extendId
        ) => NetherRuntimeBridge.Instance.CaptureInteractivePreEntryFloor(
            routeSnapshot,
            new NetherAutoClimbSettings(),
            new RuntimeFloorFixture
            {
                MNetherMapFloorId = floor.FloorId,
                ExtendId = extendId,
                FloorType = (int)floor.NodeType,
            },
            mapFloorRows: null,
            eventRows: null,
            eventPartRows: null,
            itemRows: null,
            battleRows: null,
            floorNodeId: floor.NodeId,
            canCloseShop: false
        );
    }

    private static NetherRouteSafetyContext Context(
        IReadOnlyList<NetherFloorNode> floors,
        IReadOnlyDictionary<long, NetherRouteHorizonSafetyEvaluation> horizons,
        IReadOnlyList<NetherStrategyVisibleContentRow>? rows = null,
        NetherStrategyMode mode = NetherStrategyMode.Equipment,
        bool? researchIncomplete = false,
        NetherRuntimeInteractivePreEntryInputsResult? interactive = null
    ) => new()
    {
        StrategyMode = mode,
        ResearchIncomplete = researchIncomplete,
        VisibleMap = new NetherStrategyVisibleMapEvidence(
            floors,
            rows ?? Array.Empty<NetherStrategyVisibleContentRow>()
        ),
        InteractivePreEntry = interactive,
        HorizonEvaluationByFloorId = horizons,
    };

    private static NetherRuntimeInteractivePreEntryInputsResult TypedBattleRouteProof(
        NetherSnapshot snapshot,
        NetherFloorNode eventNode,
        long eventId,
        long eventPartId,
        long battleId,
        int projectedErosion,
        int projectedHpDelta,
        NetherEventBattleTier semanticTier
    )
    {
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            EventBattleTiers =
            [new NetherEventBattleTierProviderEvidence(battleId, semanticTier)],
            EventBattleRouteSafety =
            [new NetherEventBattleRouteSafetyProviderEvidence(
                eventId,
                eventPartId,
                1,
                eventNode.FloorId,
                eventNode.NodeId,
                battleId,
                projectedErosion,
                projectedHpDelta,
                snapshot.Characters
                    .Where(character => character.IsActive)
                    .Select(character => character.CharacterId)
                    .ToArray()
            )],
        };
        NetherInteractiveFloorPreEntrySafetyInput input = new(
            NetherFloorNodeType.Event,
            eventNode.FloorId,
            [],
            [],
            [],
            snapshot.ErosionPoint,
            snapshot.Characters
                .Where(character => character.IsActive)
                .Select(character => character.HpPermille)
                .ToArray(),
            snapshot.NetherGold,
            snapshot.TreasureKeyCount,
            new NetherAutoClimbSettings()
        )
        {
            FloorNodeId = eventNode.NodeId,
            TypedSemanticProvider = provider,
        };
        return NetherRuntimeInteractivePreEntryInputsResult.Success(
            new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
            {
                [eventNode.NodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
                {
                    IsCaptured = true,
                    Input = input,
                },
            },
            snapshot.Fingerprint,
            provider
        );
    }

    private static NetherRoutePlan PlanDirectOfferVsNormal(
        NetherStrategyMode mode,
        bool? researchIncomplete
    )
    {
        NetherFloorNode current = Node(1, 80, NetherFloorNodeType.Recovery);
        NetherFloorNode directOffer = Node(2, 81, NetherFloorNodeType.Event, 1);
        NetherFloorNode normal = Node(3, 81, NetherFloorNodeType.Battle, 1);
        NetherFloorNode boss = Node(4, 82, NetherFloorNodeType.Boss, 2, 3);
        NetherFloorNode[] floors = [current, directOffer, normal, boss];
        return new NetherRoutePlanner().Plan(
            Snapshot(current, floors),
            Context(
                floors,
                new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [2] = Horizon(floors, 2, 4),
                    [3] = Horizon(floors, 3, 4),
                },
                rows: [DirectCodeOffer(2)],
                mode: mode,
                researchIncomplete: researchIncomplete
            )
        );
    }

    private static NetherRouteHorizonSafetyEvaluation Horizon(
        IReadOnlyList<NetherFloorNode> floors,
        params long[] nodeIds
    )
    {
        Dictionary<long, NetherFloorNode> floorByNodeId = floors.ToDictionary(floor => floor.NodeId);
        NetherRouteHorizonStep[] steps = nodeIds
            .Select((nodeId, index) =>
            {
                NetherFloorNodeType nodeType = floorByNodeId[nodeId].NodeType;
                return new NetherRouteHorizonStep(
                    nodeId,
                    nodeType,
                    0,
                    0,
                    Array.Empty<NetherErosionModifier>()
                )
                {
                    IsTerminalBoss = index == nodeIds.Length - 1,
                    MinimumCombatEntryHpPermille = 0,
                };
            })
            .ToArray();
        return new NetherRouteHorizonSafetyEvaluation
        {
            IsEligible = true,
            PeakErosion = 20,
            MinimumActiveCharacterHpPermille = 900,
            FinalErosion = 20,
            HorizonSteps = steps,
            Steps = steps
                .Select(step => new NetherRouteHorizonStepAudit(step.NodeId, 20, 20, 900))
                .ToArray(),
        };
    }

    private static NetherStrategyVisibleContentRow DirectCodeOffer(long nodeId) => new(
        NetherStrategyVisibleContentKind.Event,
        nodeId,
        1000 + nodeId,
        2000 + nodeId
    )
    {
        IsKnown = true,
        EventId = 2000 + nodeId,
        EventPartId = 3000 + nodeId,
        EventOptions =
        [
            new NetherStrategyVisibleEventOptionEvidence(
                1,
                3000 + nodeId,
                [
                    new NetherStrategyVisibleEventEffectEvidence(
                        NetherStrategyVisibleEventEffectSource.Content,
                        160,
                        1
                    )
                    {
                        IsPresent = true,
                        IsKnown = true,
                        EffectKind = NetherEffectKind.AbyssCodeOffer,
                        Amount = 1,
                    },
                ]
            ),
        ],
    };

    private static NetherStrategyVisibleContentRow EventPart(
        long nodeId,
        long eventId,
        long eventPartId,
        params NetherStrategyVisibleEventEffectEvidence[] effects
    ) => new(
        NetherStrategyVisibleContentKind.Event,
        nodeId,
        eventId,
        eventPartId
    )
    {
        IsKnown = true,
        EventId = eventId,
        EventPartId = eventPartId,
        EventOptions =
        [
            new NetherStrategyVisibleEventOptionEvidence(1, eventPartId, effects),
        ],
    };

    private static NetherStrategyVisibleContentRow TreasureRow(long nodeId, long eventId) => new(
        NetherStrategyVisibleContentKind.Treasure,
        nodeId,
        eventId,
        eventId
    )
    {
        IsKnown = true,
        EventId = eventId,
    };

    private static NetherStrategyVisibleContentRow RankFiveReward(
        long nodeId,
        long eventId,
        long eventPartId,
        NetherRewardRarity rarity,
        NetherCanonicalRewardTier canonicalTier = NetherCanonicalRewardTier.Unknown
    ) => new(
        NetherStrategyVisibleContentKind.Item,
        nodeId,
        eventPartId,
        eventPartId
    )
    {
        IsKnown = true,
        EventId = eventId,
        EventPartId = eventPartId,
        ItemType = 91,
        ItemRarity = (int)rarity,
        Rank = 5,
        Amount = 1,
        CanonicalRewardTier = canonicalTier,
    };

    private static NetherStrategyVisibleContentRow ShopRow(long nodeId, long contentId) => new(
        NetherStrategyVisibleContentKind.ShopInventory,
        nodeId,
        contentId,
        contentId + 1
    )
    {
        IsKnown = true,
        Cost = 300,
        Amount = 1,
        ItemType = 91,
        ItemRarity = (int)NetherRewardRarity.Gold,
        UsesNetherGold = true,
        CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
    };

    private static NetherSnapshot Snapshot(
        NetherFloorNode current,
        IReadOnlyList<NetherFloorNode> floors,
        int netherGold = 0
    ) => new()
    {
        Status = NetherSessionStatus.Play,
        CurrentFloorId = current.FloorId,
        CurrentNodeId = current.NodeId,
        FloorLevel = current.FloorLevel,
        FloorIndex = current.FloorIndex,
        ErosionPoint = 20,
        Characters = [new NetherCharacterState(1, 900)],
        NetherGold = netherGold,
        Floors = floors,
    };

    private sealed class RuntimeFloorFixture
    {
        public long MNetherMapFloorId { get; init; }
        public long ExtendId { get; init; }
        public int FloorType { get; init; }
    }

    private sealed class RuntimeEventFixture
    {
        public long id { get; init; }
        public long m_nether_map_floor_id { get; init; }
        public int weight { get; init; }
        public int type { get; init; }
        public long m_nether_floor_event_part_id_1 { get; init; }
        public long m_nether_floor_event_part_id_2 { get; init; }
        public long m_nether_floor_event_part_id_3 { get; init; }
        public long m_nether_floor_event_part_id_4 { get; init; }
    }

    private sealed class RuntimePartFixture
    {
        public long id { get; init; }
        public int target_type_1 { get; init; }
        public long select_parameter_1 { get; init; }
        public int target_type_2 { get; init; }
        public long select_parameter_2 { get; init; }
        public int target_type_3 { get; init; }
        public long select_parameter_3 { get; init; }
        public int content_type { get; init; }
        public long content_id { get; init; }
        public int amount { get; init; }
    }

    private sealed class RuntimeBattleFixture
    {
        public long id { get; init; }
        public long m_nether_map_floor_id { get; init; }
        public int type { get; init; }
        public long m_nether_battle_stage_id { get; init; }
        public int code_drop_ratio { get; init; }
    }

    private static NetherFloorNode Node(
        long nodeId,
        int floorLevel,
        NetherFloorNodeType type,
        params long[] previous
    ) => new(nodeId, floorLevel, (int)nodeId, type)
    {
        IsUnlocked = true,
        PreviousFloorIds = previous,
    };
}
