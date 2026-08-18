using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherEventPolicyTests
{
    [Fact]
    public void Event_option_combines_all_three_effect_targets()
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(erosion: 50, hp: 500),
            [Option(1, new NetherEffect(NetherEffectKind.ErosionHeal, 5), new NetherEffect(NetherEffectKind.Heal, 100), new NetherEffect(NetherEffectKind.Item, 1))],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(1, decision.OptionNumber);
        Assert.Equal(45, decision.ProjectedErosion);
        Assert.Equal(100, decision.HpDelta);
    }

    [Fact]
    public void Lethal_damage_option_is_rejected()
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(hp: 100),
            [Option(1, new NetherEffect(NetherEffectKind.Damage, 100))],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.UnsafeHp, decision.PauseReason);
    }

    [Fact]
    public void Erosion_option_reaching_hard_limit_is_rejected()
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(erosion: 90),
            [Option(1, new NetherEffect(NetherEffectKind.Erosion, 10))],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.UnsafeErosion, decision.PauseReason);
    }

    [Fact]
    public void Erosion_heal_beats_hp_heal_when_erosion_pressure_is_higher()
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(erosion: 85, hp: 700),
            [Option(1, new NetherEffect(NetherEffectKind.Heal, 200)), Option(2, new NetherEffect(NetherEffectKind.ErosionHeal, 5))],
            Settings()
        );

        Assert.Equal(2, decision.OptionNumber);
    }

    [Fact]
    public void Hp_heal_beats_code_offer_when_character_is_below_soft_hp()
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(hp: 100),
            [
                Option(1, new NetherEffect(NetherEffectKind.AbyssCodeOffer, 1)),
                Option(2, new NetherEffect(NetherEffectKind.Heal, 250)),
            ],
            Settings()
        );

        Assert.Equal(2, decision.OptionNumber);
    }

    [Fact]
    public void Unknown_target_or_content_pauses_instead_of_selecting()
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(),
            [Option(1, new NetherEffect(NetherEffectKind.Unknown, 0) { Known = false })],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.UnknownEffect, decision.PauseReason);
    }

    [Fact]
    public void Resource_projection_overflow_is_unknown_instead_of_throwing()
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(gold: 0),
            [Option(
                1,
                new NetherEffect(NetherEffectKind.NetherGoldGain, int.MaxValue),
                new NetherEffect(NetherEffectKind.NetherGoldGain, 1)
            )],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.UnknownMasterData, decision.PauseReason);
        Assert.Contains("event-resource-projection", decision.Detail);
    }

    [Fact]
    public void Event_triggered_battle_is_marked_battle_only_after_event_selection()
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(),
            [Option(1, new NetherEffect(NetherEffectKind.Battle, 0))],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.ActionKind);
        Assert.True(decision.StartsBattleAfterSelection);
    }

    [Fact]
    public void KeyOnly_selects_the_exact_key_cost_option_when_key_is_available()
    {
        NetherEventDecision decision = EventPolicy().DecideTreasure(
            Snapshot(keys: 1),
            [
                Option(1, new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1), new NetherEffect(NetherEffectKind.Item, 1)),
                Option(2, new NetherEffect(NetherEffectKind.TreasureKeyUsed, 2), new NetherEffect(NetherEffectKind.Item, 1)),
            ],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(1, decision.OptionNumber);
    }

    [Fact]
    public void KeyOnly_pauses_when_already_in_treasure_without_a_key()
    {
        NetherEventDecision decision = EventPolicy().DecideTreasure(
            Snapshot(keys: 0),
            [Option(1, new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1))],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.NoSafeRoute, decision.PauseReason);
    }

    [Fact]
    public void Treasure_without_a_key_rejects_exact_damage_shape_without_route_proof()
    {
        NetherEventDecision decision = EventPolicy().DecideTreasure(
            Snapshot(hp: 500, keys: 0),
            [
                Option(1, new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1)),
                Option(2, new NetherEffect(NetherEffectKind.Damage, 200)),
                Option(3, new NetherEffect(NetherEffectKind.Erosion, 20)),
            ],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.NoSafeRoute, decision.PauseReason);
    }

    [Fact]
    public void Treasure_without_a_key_accepts_exact_damage_only_with_prevalidated_objective()
    {
        NetherEventDecision decision = EventPolicy().DecideTreasure(
            Snapshot(hp: 500, keys: 0),
            [
                Option(1, new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1)),
                Option(2, new NetherEffect(NetherEffectKind.Damage, 80)) with
                {
                    EventId = 42,
                    EventPartId = 1002,
                    PartialDeathEligibility = TreasureEligibility(42, 1002),
                },
            ],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(2, decision.OptionNumber);
        Assert.True(decision.AllowsPartialActiveDeaths);
    }

    [Fact]
    public void Treasure_never_selects_hp_or_erosion_payment()
    {
        NetherEventDecision decision = EventPolicy().DecideTreasure(
            Snapshot(keys: 1),
            [
                Option(1, new NetherEffect(NetherEffectKind.Damage, 1)),
                Option(2, new NetherEffect(NetherEffectKind.Erosion, 1)),
                Option(3, new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1), new NetherEffect(NetherEffectKind.Item, 1)),
            ],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(3, decision.OptionNumber);
    }

    [Fact]
    public void ShopOff_never_creates_a_purchase_request()
    {
        NetherShopDecision decision = EventPolicy().DecideShop(
            Snapshot(gold: 100),
            [new NetherShopContent(1, 2, 91, NetherRewardRarity.Gold, 10, usesNetherGold: true)],
            Settings(shopMode: NetherShopMode.Off)
        );

        Assert.Equal(NetherShopDecisionKind.Leave, decision.Kind);
        Assert.Equal(0, decision.ContentId);
    }

    [Fact]
    public void EquipmentBags_requires_type_91_gold_or_better_and_nether_gold_cost()
    {
        NetherShopDecision decision = EventPolicy().DecideShop(
            Snapshot(gold: 300, floorLevel: 91),
            [
                new NetherShopContent(1, 1, 90, NetherRewardRarity.UniqueWeapon, 1, usesNetherGold: true),
                new NetherShopContent(2, 2, 91, NetherRewardRarity.Purple, 1, usesNetherGold: true),
                new NetherShopContent(3, 3, 91, NetherRewardRarity.Gold, 1, usesNetherGold: false),
                new NetherShopContent(4, 4, 91, NetherRewardRarity.Gold, 301, usesNetherGold: true),
                new NetherShopContent(5, 5, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true)
                {
                    CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
                },
            ],
            Settings(shopMode: NetherShopMode.EquipmentBags)
        );

        Assert.Equal(NetherShopDecisionKind.Buy, decision.Kind);
        Assert.Equal(5, decision.ContentId);
        Assert.Equal(1, decision.Amount);
    }

    [Fact]
    public void Late_shop_requires_floor_strictly_above_90_and_at_least_300_gold()
    {
        NetherShopContent bag = new(5, 5005, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true)
        {
            CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
        };

        Assert.Equal(
            NetherShopDecisionKind.Leave,
            EventPolicy().DecideShop(
                Snapshot(gold: 500, floorLevel: 90),
                [bag],
                Settings(shopMode: NetherShopMode.EquipmentBags)
            ).Kind
        );
        Assert.Equal(
            NetherShopDecisionKind.Leave,
            EventPolicy().DecideShop(
                Snapshot(gold: 299, floorLevel: 91),
                [bag],
                Settings(shopMode: NetherShopMode.EquipmentBags)
            ).Kind
        );
        Assert.Equal(
            NetherShopDecisionKind.Buy,
            EventPolicy().DecideShop(
                Snapshot(gold: 300, floorLevel: 91),
                [bag],
                Settings(shopMode: NetherShopMode.EquipmentBags)
            ).Kind
        );
    }

    [Fact]
    public void Late_shop_accepts_only_one_exact_gold_rank_bag_and_never_infers_red_or_unknown_value()
    {
        NetherShopContent redBag = new(6, 6006, 91, NetherRewardRarity.Red, 300, usesNetherGold: true);
        NetherShopContent unknown = new(7, 7007, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true, known: false);

        NetherShopDecision redDecision = EventPolicy().DecideShop(
            Snapshot(gold: 500, floorLevel: 91),
            [redBag],
            Settings(shopMode: NetherShopMode.EquipmentBags)
        );
        NetherShopDecision unknownDecision = EventPolicy().DecideShop(
            Snapshot(gold: 500, floorLevel: 91),
            [unknown],
            Settings(shopMode: NetherShopMode.EquipmentBags)
        );

        Assert.Equal(NetherShopDecisionKind.Leave, redDecision.Kind);
        Assert.NotEqual(NetherShopDecisionKind.Buy, unknownDecision.Kind);
    }

    [Fact]
    public void Raw_gold_shop_without_provider_is_transit_only_while_typed_gold_is_buyable()
    {
        NetherShopContent rawGold = new(70, 7001, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true);
        NetherShopContent typedGold = new NetherShopContent(71, 7001, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true)
        {
            CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
        };

        NetherSnapshot snapshot = Snapshot(gold: 300, floorLevel: 91);
        Assert.Equal(
            NetherShopDecisionKind.Leave,
            EventPolicy().DecideShop(snapshot, [rawGold], Settings(shopMode: NetherShopMode.EquipmentBags)).Kind
        );
        Assert.Equal(
            NetherShopDecisionKind.Buy,
            EventPolicy().DecideShop(snapshot, [typedGold], Settings(shopMode: NetherShopMode.EquipmentBags)).Kind
        );
    }

    [Fact]
    public void Committed_rank_five_shop_orders_key_then_skips_bag_until_500_gold()
    {
        NetherShopProcurementCommitment commitment = new()
        {
            IsKnown = true,
            RequiresRankFiveKey = true,
            Objective = new NetherRankFiveTreasureIdentity(4, 401, 4011),
            KeyContentId = 2001,
            KeyCost = 200,
            RequiresRankFiveBag = true,
            BagContentId = 3001,
            BagCost = 300,
        };
        NetherShopContent key = new(2001, 0, 0, NetherRewardRarity.NoEffect, 200, usesNetherGold: true)
        {
            IsTreasureKey = true,
        };
        NetherShopContent bag = new(3001, 9001, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true)
        {
            CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
        };

        NetherShopDecision keyDecision = EventPolicy().DecideShop(
            Snapshot(gold: 300, keys: 0, floorLevel: 91),
            [key, bag],
            Settings(shopMode: NetherShopMode.EquipmentBags),
            commitment
        );
        NetherShopDecision skipBagDecision = EventPolicy().DecideShop(
            Snapshot(gold: 299, keys: 1, floorLevel: 91),
            [key, bag],
            Settings(shopMode: NetherShopMode.EquipmentBags),
            commitment
        );
        NetherShopDecision bagDecision = EventPolicy().DecideShop(
            Snapshot(gold: 500, keys: 1, floorLevel: 91),
            [key, bag],
            Settings(shopMode: NetherShopMode.EquipmentBags),
            commitment
        );

        Assert.Equal(NetherShopDecisionKind.Buy, keyDecision.Kind);
        Assert.Equal(2001, keyDecision.ContentId);
        Assert.Equal(NetherShopDecisionKind.Leave, skipBagDecision.Kind);
        Assert.Equal(NetherShopDecisionKind.Buy, bagDecision.Kind);
        Assert.Equal(3001, bagDecision.ContentId);
    }

    [Fact]
    public void Committed_shop_key_is_allowed_before_late_bag_boundary_but_unknown_key_is_not()
    {
        NetherShopProcurementCommitment commitment = new()
        {
            IsKnown = true,
            RequiresRankFiveKey = true,
            Objective = new NetherRankFiveTreasureIdentity(4, 401, 4011),
            KeyContentId = 2001,
            KeyCost = 200,
            RequiresRankFiveBag = true,
            BagContentId = 3001,
            BagCost = 300,
        };
        NetherShopContent key = new(2001, 0, 0, NetherRewardRarity.NoEffect, 200, usesNetherGold: true)
        {
            IsTreasureKey = true,
        };
        NetherShopContent bag = new(3001, 9001, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true)
        {
            CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
        };

        NetherShopDecision floorZeroKey = EventPolicy().DecideShop(
            Snapshot(gold: 200, floorLevel: 0),
            [key, bag],
            Settings(shopMode: NetherShopMode.EquipmentBags),
            commitment
        );
        NetherShopDecision floorNinetyKey = EventPolicy().DecideShop(
            Snapshot(gold: 299, floorLevel: 90),
            [key, bag],
            Settings(shopMode: NetherShopMode.EquipmentBags),
            commitment
        );
        NetherShopDecision unaffordableKey = EventPolicy().DecideShop(
            Snapshot(gold: 199, floorLevel: 91),
            [key, bag],
            Settings(shopMode: NetherShopMode.EquipmentBags),
            commitment
        );
        NetherShopContent unknownKey = new(2001, 0, 0, NetherRewardRarity.NoEffect, 200, usesNetherGold: true, known: false)
        {
            IsTreasureKey = true,
        };
        NetherShopDecision malformedKey = EventPolicy().DecideShop(
            Snapshot(gold: 299, floorLevel: 90),
            [unknownKey, bag],
            Settings(shopMode: NetherShopMode.EquipmentBags),
            commitment
        );

        Assert.Equal(NetherShopDecisionKind.Buy, floorZeroKey.Kind);
        Assert.Equal(2001, floorZeroKey.ContentId);
        Assert.Equal(NetherShopDecisionKind.Buy, floorNinetyKey.Kind);
        Assert.Equal(2001, floorNinetyKey.ContentId);
        Assert.Equal(NetherShopDecisionKind.Leave, unaffordableKey.Kind);
        Assert.NotEqual(NetherShopDecisionKind.Buy, malformedKey.Kind);
    }

    [Fact]
    public void EquipmentBags_ignores_valid_non_item_shop_rows_with_no_content_id()
    {
        NetherShopDecision decision = EventPolicy().DecideShop(
            Snapshot(gold: 15),
            [
                new NetherShopContent(10, 0, 0, NetherRewardRarity.NoEffect, 30, usesNetherGold: true),
                new NetherShopContent(11, 0, 0, NetherRewardRarity.NoEffect, 100, usesNetherGold: true),
            ],
            Settings(shopMode: NetherShopMode.EquipmentBags)
        );

        Assert.Equal(NetherShopDecisionKind.Leave, decision.Kind);
        Assert.Equal(0, decision.ContentId);
    }

    [Fact]
    public void Recovery_prefers_erosion_heal_over_neutral_choice()
    {
        NetherEventDecision decision = EventPolicy().DecideRecoveryForRouteAnalysis(
            Snapshot(erosion: 70),
            [Option(1, new NetherEffect(NetherEffectKind.Item, 1)), Option(2, new NetherEffect(NetherEffectKind.ErosionHeal, 3))],
            Settings(),
            [],
            NetherCodeTransformHardExclusionEvidence.Unknown("test-route-analysis")
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(2, decision.OptionNumber);
    }

    [Fact]
    public void Recovery_allows_a_completely_neutral_safe_fallback_when_no_positive_choice_exists()
    {
        NetherEventDecision decision = EventPolicy().DecideRecoveryForRouteAnalysis(
            Snapshot(erosion: 70),
            [Option(1, new NetherEffect(NetherEffectKind.NetherGoldUsed, 0))],
            Settings(),
            [],
            NetherCodeTransformHardExclusionEvidence.Unknown("test-route-analysis")
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(1, decision.OptionNumber);
    }

    [Fact]
    public void Exact_event_selection_retains_event_part_effect_and_reward_commitment()
    {
        NetherEffect item = new(NetherEffectKind.Item, 1)
        {
            ContentId = 7001,
            RewardEvidence = new NetherEventRewardEvidence(
                ContentId: 7001,
                ItemId: 7001,
                ItemType: 91,
                Rarity: NetherRewardRarity.Gold,
                Amount: 1
            ),
        };
        NetherEventOption option = Option(1, item) with
        {
            EventId = 701,
            EventPartId = 702,
            RequiresExactBinding = true,
            RewardEvidence = item.RewardEvidence,
        };

        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(),
            [option],
            Settings(),
            [],
            new NetherEventStrategyEvidence
            {
                IsKnown = true,
                Mode = NetherStrategyMode.Equipment,
            }
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(701, decision.EventId);
        Assert.Equal(702, decision.EventPartId);
        Assert.NotNull(decision.Commitment);
        Assert.True(decision.Commitment!.IsValid);
        Assert.Equal(7001, decision.Commitment.Reward!.ItemId);
    }

    [Fact]
    public void Unknown_exact_event_option_is_local_and_does_not_block_another_bound_option()
    {
        NetherEventOption unknown = Option(1, new NetherEffect(NetherEffectKind.Item, 1)) with
        {
            EventId = 801,
            EventPartId = 811,
            RequiresExactBinding = true,
            UnknownReason = "future-item-row-unavailable",
        };
        NetherEventOption known = Option(2, new NetherEffect(NetherEffectKind.NetherGoldGain, 50)) with
        {
            EventId = 801,
            EventPartId = 812,
            RequiresExactBinding = true,
        };

        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(),
            [unknown, known],
            Settings(),
            []
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(2, decision.OptionNumber);
        Assert.Equal(812, decision.EventPartId);
    }

    [Fact]
    public void Mode_aware_event_rewards_prefer_equipment_rank_five_bag_over_direct_code_offer()
    {
        NetherEffect item = new(NetherEffectKind.Item, 1)
        {
            ContentId = 901,
            RewardEvidence = new NetherEventRewardEvidence(901, 901, 91, NetherRewardRarity.Red, 1),
        };
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(),
            [
                Option(1, new NetherEffect(NetherEffectKind.AbyssCodeOffer, 0)) with
                {
                    EventId = 9010,
                    EventPartId = 9011,
                    RequiresExactBinding = true,
                },
                Option(2, item) with
                {
                    EventId = 9010,
                    EventPartId = 9012,
                    RequiresExactBinding = true,
                    RewardEvidence = item.RewardEvidence,
                },
            ],
            Settings(),
            [],
            new NetherEventStrategyEvidence
            {
                IsKnown = true,
                Mode = NetherStrategyMode.Equipment,
            }
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(2, decision.OptionNumber);
    }

    [Fact]
    public void Research_incomplete_mode_prefers_direct_code_offer_over_uncommitted_gold()
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(),
            [
                Option(1, new NetherEffect(NetherEffectKind.NetherGoldGain, 100)),
                Option(2, new NetherEffect(NetherEffectKind.AbyssCodeOffer, 0)),
            ],
            Settings(),
            [],
            new NetherEventStrategyEvidence
            {
                IsKnown = true,
                Mode = NetherStrategyMode.Research,
                ResearchIncomplete = true,
            }
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(2, decision.OptionNumber);
    }

    [Fact]
    public void Exact_event_damage_checks_every_living_character_not_the_popup_presenter()
    {
        NetherSnapshot snapshot = Snapshot(hp: 500) with
        {
            Characters =
            [
                new NetherCharacterState(1, 500),
                new NetherCharacterState(2, 100),
            ],
        };
        NetherEventDecision decision = EventPolicy().DecideEvent(
            snapshot,
            [Option(1, new NetherEffect(NetherEffectKind.Damage, 150))],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Pause, decision.Kind);
        Assert.Equal(NetherPauseReason.UnsafeHp, decision.PauseReason);
    }

    [Theory]
    [InlineData(40, false)]
    [InlineData(80, true)]
    [InlineData(120, false)]
    public void Hp_paid_rank_five_event_key_requires_exactly_eighty_damage(
        int damage,
        bool expectedSelection
    )
    {
        NetherEventDecision decision = EventPolicy().DecideEvent(
            Snapshot(hp: 100) with
            {
                Characters =
                [
                    new NetherCharacterState(1, 100),
                    new NetherCharacterState(2, 500),
                ],
            },
            [
                Option(
                    1,
                    new NetherEffect(NetherEffectKind.Damage, damage),
                    new NetherEffect(NetherEffectKind.TreasureKeyGain, 1)
                ) with
                {
                    EventId = 7001,
                    EventPartId = 7002,
                    PartialDeathEligibility = new NetherInteractivePartialDeathEligibility(
                        NetherInteractivePartialDeathObjectiveKind.HpPaidEventKeyForRank5Treasure,
                        7001,
                        7002,
                        7003
                    )
                    {
                        IsKnown = true,
                        ObjectiveReachable = true,
                        ExactTreasureRank = 5,
                        NoBetterAffordableCurrencyKeySource = true,
                    },
                },
            ],
            Settings()
        );

        Assert.Equal(expectedSelection, decision.Kind == NetherEventDecisionKind.Select);
        if (!expectedSelection)
            Assert.Equal(NetherPauseReason.NoSafeRoute, decision.PauseReason);
    }

    [Fact]
    public void Exact_event_battle_row_is_required_and_normal_battle_tier_is_mode_aware()
    {
        NetherEventBattleEvidence normal = new(
            BattleId: 9301,
            BattleStageId: 9302,
            BattleType: 1,
            CodeDropRatio: 300,
            SemanticTier: NetherEventBattleTier.NormalBattle
        );
        NetherEventOption exactBattle = Option(1, new NetherEffect(NetherEffectKind.Battle, 9301)) with
        {
            EventId = 9300,
            EventPartId = 9303,
            RequiresExactBinding = true,
            BattleEvidence = normal,
        };
        NetherEventOption codeOffer = Option(2, new NetherEffect(NetherEffectKind.AbyssCodeOffer, 0)) with
        {
            EventId = 9300,
            EventPartId = 9304,
            RequiresExactBinding = true,
        };

        NetherEventDecision research = EventPolicy().DecideEvent(
            Snapshot(),
            [exactBattle, codeOffer],
            Settings(),
            [],
            new NetherEventStrategyEvidence
            {
                IsKnown = true,
                Mode = NetherStrategyMode.Research,
                ResearchIncomplete = true,
            }
        );
        NetherEventDecision equipment = EventPolicy().DecideEvent(
            Snapshot(),
            [exactBattle, codeOffer],
            Settings(),
            [],
            new NetherEventStrategyEvidence
            {
                IsKnown = true,
                Mode = NetherStrategyMode.Equipment,
            }
        );

        Assert.Equal(2, research.OptionNumber);
        Assert.Equal(1, equipment.OptionNumber);
        Assert.NotNull(equipment.Commitment);
        Assert.True(equipment.Commitment!.IsValid);
        Assert.Equal(normal, equipment.Commitment.Battle);
    }

    private static NetherEventPolicy EventPolicy() => new();

    private static NetherAutoClimbSettings Settings(
        NetherTreasureMode treasureMode = NetherTreasureMode.KeyOnly,
        NetherShopMode shopMode = NetherShopMode.Off
    ) => new()
    {
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = 300,
        TreasureMode = treasureMode,
        ShopMode = shopMode,
    };

    private static NetherSnapshot Snapshot(
        int erosion = 20,
        int hp = 500,
        int keys = 0,
        int gold = 0,
        int floorLevel = 0
    ) => new()
    {
        ErosionPoint = erosion,
        TreasureKeyCount = keys,
        NetherGold = gold,
        FloorLevel = floorLevel,
        Characters = [new NetherCharacterState(1, hp)],
    };

    private static NetherEventOption Option(int number, params NetherEffect[] effects) => new(number, effects)
    {
        FloorId = 10,
        NodeId = 1,
    };

    private static NetherInteractivePartialDeathEligibility TreasureEligibility(long eventId, long partId) => new(
        NetherInteractivePartialDeathObjectiveKind.TreasureHpPayment,
        eventId,
        partId,
        ObjectiveNodeId: 999
    )
    {
        IsKnown = true,
        ObjectiveReachable = true,
        ExactTreasureRank = 5,
    };
}
