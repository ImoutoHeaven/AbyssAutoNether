using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherActionReconcilePolicyTests
{
    [Theory]
    [InlineData(26, -100, 110, 0)]
    [InlineData(25, -99, 110, 0)]
    [InlineData(25, -100, 111, 0)]
    [InlineData(25, -100, 110, 1)]
    public void Event_reconcile_rejects_projected_commitment_state_mismatch_even_when_identity_and_effects_match(
        int committedErosion,
        int committedHpDelta,
        int committedGold,
        int committedKeys
    )
    {
        NetherEffect[] effects =
        [
            new(NetherEffectKind.Erosion, 5),
            new(NetherEffectKind.Damage, 100),
            new(NetherEffectKind.NetherGoldGain, 10),
            new(NetherEffectKind.TreasureKeyUsed, 1),
        ];
        NetherSnapshot before = Snapshot(gold: 100) with
        {
            Characters = [new NetherCharacterState(1, 1_000)],
            CharacterHpHash = "1:1000:1",
        };
        NetherSnapshot after = before with
        {
            ErosionPoint = 25,
            NetherGold = 110,
            TreasureKeyCount = 0,
            Characters = [new NetherCharacterState(1, 900)],
            CharacterHpHash = "1:900:1",
        };
        NetherEventCommitment commitment = new(
            EventId: 9701,
            EventPartId: 9702,
            OptionNumber: 1,
            Effects: effects,
            ProjectedErosion: committedErosion,
            HpDelta: committedHpDelta
        )
        {
            FloorId = 10,
            NodeId = 1,
            ProjectedNetherGold = committedGold,
            ProjectedTreasureKeys = committedKeys,
        };
        NetherPlannedAction action = new(NetherActionKind.SelectEventOption)
        {
            OptionNumber = 1,
            ExpectedEffects = effects,
            EventId = 9701,
            EventPartId = 9702,
            EventFloorId = 10,
            EventNodeId = 1,
            EventCommitment = commitment,
            ProjectedErosion = committedErosion,
            ProjectedHpDelta = committedHpDelta,
            ProjectedNetherGold = committedGold,
            ProjectedTreasureKeys = committedKeys,
        };

        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(action, before, after)
        );
    }

    [Fact]
    public void Event_reconcile_applies_a_positive_procurement_commitment_when_minima_survive_the_handoff()
    {
        NetherEffect effect = new(NetherEffectKind.NetherGoldGain, 10);
        NetherEventCommitment commitment = new(
            EventId: 9731,
            EventPartId: 9732,
            OptionNumber: 1,
            Effects: [effect],
            ProjectedErosion: 20,
            HpDelta: 0
        )
        {
            FloorId = 10,
            NodeId = 1,
            ProjectedNetherGold = 110,
            ProjectedTreasureKeys = 1,
            CommittedGoldMinimum = 80,
            CommittedKeyMinimum = 1,
        };
        NetherPlannedAction action = new(NetherActionKind.SelectEventOption)
        {
            OptionNumber = 1,
            ExpectedEffects = [effect],
            EventId = 9731,
            EventPartId = 9732,
            EventFloorId = 10,
            EventNodeId = 1,
            EventCommitment = commitment,
            ProjectedErosion = 20,
            ProjectedHpDelta = 0,
            ProjectedNetherGold = 110,
            ProjectedTreasureKeys = 1,
            CommittedGoldMinimum = 80,
            CommittedKeyMinimum = 1,
        };

        NetherSnapshot before = Snapshot(gold: 100);
        NetherSnapshot after = before with { NetherGold = 110 };

        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(action, before, after)
        );
    }

    [Fact]
    public void Select_floor_requires_an_authoritative_floor_or_status_postcondition()
    {
        NetherSnapshot before = Snapshot(floorId: 10, floorLevel: 10);
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11, status: NetherSessionStatus.Battle);

        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(
                new NetherPlannedAction(NetherActionKind.SelectFloor)
                {
                    FloorId = 11,
                    ExpectedBeforeStatus = NetherSessionStatus.Play,
                    ExpectedAfterStatus = NetherSessionStatus.Battle,
                },
                before,
                after
            )
        );
    }

    [Fact]
    public void Wrong_floor_or_status_is_never_treated_as_the_selected_floor()
    {
        NetherSnapshot before = Snapshot(floorId: 10, floorLevel: 10);
        NetherSnapshot wrongFloor = Snapshot(floorId: 12, floorLevel: 11, status: NetherSessionStatus.Battle);
        NetherSnapshot wrongStatus = Snapshot(floorId: 11, floorLevel: 11, status: NetherSessionStatus.Wait);
        NetherPlannedAction action = new(NetherActionKind.SelectFloor)
        {
            FloorId = 11,
            ExpectedBeforeStatus = NetherSessionStatus.Play,
            ExpectedAfterStatus = NetherSessionStatus.Battle,
        };

        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrongFloor));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrongStatus));
    }

    [Fact]
    public void Exact_code_add_and_replace_is_applied_but_a_wrong_code_is_not()
    {
        NetherSnapshot before = Snapshot(codes: new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) });
        NetherSnapshot exact = Snapshot(codes: new[] { new NetherCodeState(40024, NetherCodeFamily.Risk, 1) }, codeHash: "40024:1:1");
        NetherSnapshot wrong = Snapshot(codes: new[] { new NetherCodeState(50024, NetherCodeFamily.Rush, 1) }, codeHash: "50024:1:1");
        NetherPlannedAction action = new(NetherActionKind.SelectCode) { CodeId = 40024, ReplaceCodeId = 30024 };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrong));
    }

    [Fact]
    public void Duplicate_code_select_is_ambiguous_without_the_authoritative_fix_response()
    {
        NetherCodeState selected = new NetherCodeState(30024, NetherCodeFamily.Safe, 1)
        {
            Category = NetherCodeCategory.Safe,
            PossessionAmount = 2,
            Power = 100,
        };
        NetherCodeState survivor = new NetherCodeState(40024, NetherCodeFamily.Risk, 1)
        {
            Category = NetherCodeCategory.Risk,
            PossessionAmount = 1,
            Power = 200,
        };
        NetherSnapshot before = Snapshot(codeHash: "before") with { Codes = [selected, survivor] };
        NetherSnapshot exact = before with
        {
            Codes = [selected with { PossessionAmount = 3 }, survivor],
            CodeHash = "after",
        };
        NetherPlannedAction action = new(NetherActionKind.SelectCode) { CodeId = selected.CodeId };

        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(action, before, exact)
        );
    }

    [Fact]
    public void New_code_select_preserves_every_unrelated_authoritative_code_field()
    {
        NetherCodeState survivor = new NetherCodeState(30024, NetherCodeFamily.Safe, 1)
        {
            Category = NetherCodeCategory.Safe,
            PossessionAmount = 1,
            Power = 100,
        };
        NetherSnapshot before = Snapshot(codeHash: "before") with { Codes = [survivor] };
        NetherSnapshot exact = before with
        {
            Codes = [survivor, new NetherCodeState(40024, NetherCodeFamily.Risk, 1)],
            CodeHash = "after",
        };
        NetherSnapshot unrelatedMutation = exact with
        {
            Codes = [survivor with { Power = 101 }, new NetherCodeState(40024, NetherCodeFamily.Risk, 1)],
        };
        NetherPlannedAction action = new(NetherActionKind.SelectCode) { CodeId = 40024 };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, unrelatedMutation));
    }

    [Fact]
    public void Direct_code_select_requires_zero_reload_delta_when_no_reload_stage_was_retained()
    {
        NetherSnapshot before = Snapshot(codeReload: 2, codeHash: "codes:none") with
        {
            Codes = Array.Empty<NetherCodeState>(),
        };
        NetherSnapshot wrongReloadDelta = Snapshot(codeReload: 1, codeHash: "codes:30024") with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };

        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(
                new NetherPlannedAction(NetherActionKind.SelectCode) { CodeId = 30024 },
                before,
                wrongReloadDelta
            )
        );
    }

    [Fact]
    public void Exact_shop_content_and_cost_is_applied_but_a_wrong_content_is_not()
    {
        NetherSnapshot before = Snapshot(items: Array.Empty<NetherRewardItem>(), gold: 100);
        NetherSnapshot exact = Snapshot(items: new[] { new NetherRewardItem(42, 1) }, gold: 80);
        NetherSnapshot wrong = Snapshot(items: new[] { new NetherRewardItem(99, 1) }, gold: 80);
        NetherPlannedAction action = new(NetherActionKind.BuyShopItem) { ContentId = 42, GoldCost = 20, ContentAmount = 1 };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrong));
    }

    [Fact]
    public void Shop_reconcile_rejects_wrong_amount_and_an_unauthorised_balance_debit()
    {
        NetherPlannedAction action = new(NetherActionKind.BuyShopItem)
        {
            ContentId = 42,
            GoldCost = 300,
            ContentAmount = 1,
        };
        NetherSnapshot before = Snapshot(items: Array.Empty<NetherRewardItem>(), gold: 300);

        NetherSnapshot wrongAmount = Snapshot(
            items: new[] { new NetherRewardItem(42, 2) },
            gold: 0
        );
        NetherSnapshot overspent = Snapshot(
            items: new[] { new NetherRewardItem(42, 1) },
            gold: -1
        );

        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(action, before, wrongAmount)
        );
        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(action, before with { NetherGold = 299 }, overspent)
        );
    }

    [Fact]
    public void Rank_five_shop_commitment_must_match_the_reconciled_content_and_cost()
    {
        NetherShopProcurementCommitment commitment = new()
        {
            IsKnown = true,
            RequiresRankFiveKey = true,
            Objective = new NetherRankFiveTreasureIdentity(4, 401, 4011),
            KeyContentId = 3001,
            KeyCost = 200,
        };
        NetherSnapshot before = Snapshot(items: Array.Empty<NetherRewardItem>(), gold: 250);
        NetherSnapshot exact = Snapshot(items: new[] { new NetherRewardItem(3001, 1) }, gold: 50);
        NetherSnapshot wrongCost = Snapshot(items: new[] { new NetherRewardItem(3001, 1) }, gold: 49);
        NetherPlannedAction action = new(NetherActionKind.BuyShopItem)
        {
            ContentId = 3001,
            GoldCost = 200,
            ContentAmount = 1,
            ShopProcurementCommitment = commitment,
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrongCost));

        NetherPlannedAction wrongContent = action with { ContentId = 3002 };
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(wrongContent, before, exact));
    }

    [Fact]
    public void Rank_five_event_commitment_survives_dispatch_action_reconciliation()
    {
        NetherRankFiveTreasureIdentity objective = new(4, 401, 4011);
        NetherRankFiveKeyProcurementCommitment procurement = new()
        {
            Objective = objective,
            SourceKind = NetherKeyProcurementSourceKind.EventGold150,
            SourceNodeId = 1,
            SourceEventId = 9901,
            SourceEventPartId = 9902,
            SourceOptionNumber = 1,
            GoldCost = 150,
        };
        NetherEffect effect = new(NetherEffectKind.Item, 1)
        {
            ContentId = 42,
            RewardEvidence = new NetherEventRewardEvidence(42, 42, 91, NetherRewardRarity.Gold, 1),
        };
        NetherEventCommitment commitment = new(9901, 9902, 1, [effect], 20, 0)
        {
            FloorId = 10,
            NodeId = 1,
            Reward = effect.RewardEvidence,
            ProjectedNetherGold = 100,
            ProjectedTreasureKeys = 1,
            RankFiveKeyProcurementCommitment = procurement,
            RankFiveTreasureObjective = objective,
        };
        NetherPlannedAction action = new(NetherActionKind.SelectEventOption)
        {
            OptionNumber = 1,
            ExpectedEffects = [effect],
            EventId = 9901,
            EventPartId = 9902,
            EventFloorId = 10,
            EventNodeId = 1,
            ProjectedErosion = 20,
            ProjectedHpDelta = 0,
            ProjectedNetherGold = 100,
            ProjectedTreasureKeys = 1,
            EventCommitment = commitment,
        };
        NetherActionOutcome outcome = NetherActionReconcilePolicy.Evaluate(
            action,
            Snapshot(items: Array.Empty<NetherRewardItem>(), gold: 100),
            Snapshot(items: new[] { new NetherRewardItem(42, 1) }, gold: 100)
        );

        Assert.Equal(NetherActionOutcome.Applied, outcome);
    }

    [Fact]
    public void Exact_continue_ticket_map_floor_and_segment_is_applied_but_wrong_target_is_not()
    {
        NetherSnapshot before = Snapshot(ticketCount: 3, mapId: 2, floorLevel: 10);
        NetherSnapshot exact = Snapshot(floorId: 33, ticketCount: 2, mapId: 3, floorLevel: 10);
        NetherSnapshot wrongTicket = Snapshot(floorId: 33, ticketCount: 1, mapId: 3, floorLevel: 10);
        NetherSnapshot wrongMap = Snapshot(floorId: 33, ticketCount: 2, mapId: 4, floorLevel: 10);
        NetherSnapshot wrongFloor = Snapshot(floorId: 34, ticketCount: 2, mapId: 3, floorLevel: 10);
        NetherSnapshot wrongSegment = Snapshot(floorId: 33, ticketCount: 2, mapId: 3, floorLevel: 11);
        NetherPlannedAction action = new(NetherActionKind.Continue)
        {
            TicketCost = 1,
            ExpectedMapId = 3,
            ExpectedFloorId = 33,
            ExpectedSegmentFloorLevel = 10,
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrongTicket));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrongMap));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrongFloor));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrongSegment));
    }

    [Fact]
    public void Only_an_unchanged_exact_target_is_a_genuine_not_applied_outcome()
    {
        NetherSnapshot before = Snapshot(items: Array.Empty<NetherRewardItem>(), gold: 100);
        NetherSnapshot unchanged = Snapshot(items: Array.Empty<NetherRewardItem>(), gold: 100);
        NetherSnapshot unrelatedChange = Snapshot(items: Array.Empty<NetherRewardItem>(), gold: 90);
        NetherPlannedAction action = new(NetherActionKind.BuyShopItem) { ContentId = 42, GoldCost = 20, ContentAmount = 1 };

        Assert.Equal(NetherActionOutcome.NotApplied, NetherActionReconcilePolicy.Evaluate(action, before, unchanged));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, unrelatedChange));
    }

    [Fact]
    public void Reload_code_requires_code_or_reload_resource_change_not_an_unrelated_map_change()
    {
        NetherSnapshot before = Snapshot(codeReload: 2, mapHash: "map-a");
        NetherSnapshot unrelated = Snapshot(codeReload: 2, mapHash: "map-b");
        NetherSnapshot applied = Snapshot(codeReload: 1, mapHash: "map-a");

        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(new NetherPlannedAction(NetherActionKind.ReloadCode), before, unrelated)
        );
        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(new NetherPlannedAction(NetherActionKind.ReloadCode), before, applied)
        );
    }

    [Fact]
    public void Unknown_outcome_with_no_action_specific_postcondition_stays_ambiguous_and_is_never_replayed()
    {
        NetherSnapshot snapshot = Snapshot();

        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(new NetherPlannedAction(NetherActionKind.BuyShopItem) { ContentId = 7 }, snapshot, snapshot)
        );
    }

    [Fact]
    public void Composed_event_parent_requires_the_exact_floor_status_and_resource_effects()
    {
        NetherSnapshot before = Snapshot(floorId: 10, gold: 20);
        NetherSnapshot exact = Snapshot(floorId: 11, floorLevel: 11, gold: 23);
        NetherSnapshot wrongGold = Snapshot(floorId: 11, floorLevel: 11, gold: 22);
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Event,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 2,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.NetherGoldGain, 3) },
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrongGold));
    }

    [Fact]
    public void Same_master_floor_event_accepts_authoritative_server_selected_hp_update_and_combined_effects()
    {
        NetherSnapshot before = Snapshot(floorId: 364, floorLevel: 77, gold: 20) with
        {
            TreasureKeyCount = 0,
            ErosionPoint = 0,
            Characters =
            [
                new NetherCharacterState(1300026, 1000),
                new NetherCharacterState(1300027, 1000),
            ],
            CharacterHpHash = "1300026:1000:1;1300027:1000:1",
        };
        NetherSnapshot after = before with
        {
            FloorLevel = 78,
            FloorIndex = 1,
            NetherGold = 50,
            Characters =
            [
                new NetherCharacterState(1300026, 900),
                new NetherCharacterState(1300027, 1000),
            ],
            CharacterHpHash = "1300026:900:1;1300027:1000:1",
        };
        NetherEffect[] effects =
        [
            new(NetherEffectKind.Damage, 100),
            new(NetherEffectKind.NetherGoldGain, 30),
        ];
        NetherPlannedAction action = new(NetherActionKind.SelectFloor)
        {
            FloorId = 364,
            FloorLevel = 78,
            FloorIndex = 1,
            ExpectedBeforeStatus = NetherSessionStatus.Play,
            ExpectedAfterStatus = NetherSessionStatus.Play,
            OwnedPopupKind = NetherRuntimePopupKind.Event,
            OwnedPopupActionKind = NetherActionKind.SelectEventOption,
            OptionNumber = 1,
            TargetCharacterId = 1300026,
            ExpectedEffects = effects,
            HasExpectedErosionDelta = true,
            ExpectedErosionDelta = 0,
            OwnedPopupStages =
            [
                new NetherFloorPopupStage(
                    NetherRuntimePopupKind.Event,
                    NetherActionKind.SelectEventOption,
                    OwnerGeneration: 8,
                    Sequence: 13,
                    ExpectedAfterStatus: NetherSessionStatus.Play,
                    OptionNumber: 1,
                    ExpectedEffects: effects,
                    ContentId: 0,
                    ContentAmount: 0,
                    GoldCost: 0,
                    CodeId: 0,
                    ReplaceCodeId: 0,
                    TargetCharacterId: 1300026
                )
                {
                    HasExpectedErosionDelta = true,
                    ExpectedErosionDelta = 0,
                },
            ],
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, after));
    }

    [Fact]
    public void Floor_94_event_accepts_authoritative_server_selected_damage_with_item_reward()
    {
        NetherSnapshot before = Snapshot(floorId: 430, floorLevel: 93, gold: 25) with
        {
            ErosionPoint = 0,
            TreasureKeyCount = 0,
            Characters =
            [
                new NetherCharacterState(1300026, 1000),
                new NetherCharacterState(1300027, 1000),
            ],
            CharacterHpHash = "1300026:1000:1;1300027:1000:1",
            AcquiredItems = Array.Empty<NetherRewardItem>(),
        };
        NetherSnapshot after = before with
        {
            CurrentFloorId = 438,
            FloorLevel = 94,
            FloorIndex = 1,
            Characters =
            [
                new NetherCharacterState(1300026, 700),
                new NetherCharacterState(1300027, 1000),
            ],
            CharacterHpHash = "1300026:700:1;1300027:1000:1",
            AcquiredItems = [new NetherRewardItem(210107, 1)],
        };
        NetherEffect[] effects =
        [
            new(NetherEffectKind.Damage, 300),
            new(NetherEffectKind.Item, 1) { ContentId = 210107 },
        ];
        NetherPlannedAction action = new(NetherActionKind.SelectFloor)
        {
            FloorId = 438,
            FloorLevel = 94,
            FloorIndex = 1,
            ExpectedBeforeStatus = NetherSessionStatus.Play,
            ExpectedAfterStatus = NetherSessionStatus.Play,
            OwnedPopupKind = NetherRuntimePopupKind.Event,
            OwnedPopupActionKind = NetherActionKind.SelectEventOption,
            OptionNumber = 1,
            TargetCharacterId = 1300026,
            ExpectedEffects = effects,
            HasExpectedErosionDelta = true,
            ExpectedErosionDelta = 0,
            OwnedPopupStages =
            [
                new NetherFloorPopupStage(
                    NetherRuntimePopupKind.Event,
                    NetherActionKind.SelectEventOption,
                    OwnerGeneration: 6,
                    Sequence: 17,
                    ExpectedAfterStatus: NetherSessionStatus.Play,
                    OptionNumber: 1,
                    ExpectedEffects: effects,
                    ContentId: 210107,
                    ContentAmount: 1,
                    GoldCost: 0,
                    CodeId: 0,
                    ReplaceCodeId: 0,
                    TargetCharacterId: 1300026
                )
                {
                    HasExpectedErosionDelta = true,
                    ExpectedErosionDelta = 0,
                },
            ],
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, after));
    }

    [Fact]
    public void Composed_recovery_treasure_and_event_effects_do_not_accept_wrong_hp_or_item()
    {
        NetherSnapshot before = Snapshot(floorId: 10, gold: 20) with
        {
            Characters = new[] { new NetherCharacterState(1, 900) },
            CharacterHpHash = "1:900:1",
            AcquiredItems = Array.Empty<NetherRewardItem>(),
            Codes = Array.Empty<NetherCodeState>(),
            CodeHash = "codes:none",
        };
        NetherSnapshot exact = Snapshot(floorId: 11, floorLevel: 11, gold: 20, codeHash: "codes:30024") with
        {
            Characters = new[] { new NetherCharacterState(1, 920) },
            CharacterHpHash = "1:920:1",
            AcquiredItems = new[] { new NetherRewardItem(7001, 1) },
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherSnapshot wrongItem = exact with { AcquiredItems = new[] { new NetherRewardItem(7002, 1) } };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Recovery,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 1,
            ExpectedEffects = new[]
            {
                new NetherEffect(NetherEffectKind.Heal, 20),
                new NetherEffect(NetherEffectKind.Item, 1) { ContentId = 7001 },
            },
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, wrongItem));
    }

    [Fact]
    public void Server_assigned_continue_target_requires_exact_ticket_segment_and_a_new_positive_identity()
    {
        NetherSnapshot before = Snapshot(floorId: 23, ticketCount: 3, mapId: 2, floorLevel: 10);
        NetherSnapshot assigned = Snapshot(floorId: 33, ticketCount: 2, mapId: 3, floorLevel: 10);
        NetherSnapshot unchangedIdentity = Snapshot(floorId: 23, ticketCount: 2, mapId: 2, floorLevel: 10);
        NetherSnapshot invalidIdentity = Snapshot(floorId: 0, ticketCount: 2, mapId: 0, floorLevel: 10);
        NetherPlannedAction action = new(NetherActionKind.Continue)
        {
            TicketCost = 1,
            ExpectedMapId = 0,
            ExpectedFloorId = 0,
            ExpectedSegmentFloorLevel = 10,
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, assigned));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, unchangedIdentity));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, invalidIdentity));
    }

    [Fact]
    public void Composed_recovery_heal_accepts_the_authoritative_full_hp_cap()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with
        {
            Characters = new[] { new NetherCharacterState(1, 1000) },
            CharacterHpHash = "1:1000:1",
        };
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11) with
        {
            Characters = new[] { new NetherCharacterState(1, 1000) },
            CharacterHpHash = "1:1000:1",
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Recovery,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 2,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.Heal, 300) },
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, after));
    }

    [Fact]
    public void Composed_recovery_uses_the_projected_category_skill_erosion_delta()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with
        {
            ErosionPoint = 20,
            Characters = new[] { new NetherCharacterState(1, 1000) },
            CharacterHpHash = "1:1000:1",
        };
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11) with
        {
            ErosionPoint = 15,
            Characters = before.Characters,
            CharacterHpHash = before.CharacterHpHash,
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Recovery,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 2,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.Heal, 300) },
            HasExpectedErosionDelta = true,
            ExpectedErosionDelta = -5,
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, after));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            after with { ErosionPoint = 20 }
        ));
    }

    [Fact]
    public void Standalone_full_hp_heal_without_a_parent_postcondition_is_not_treated_as_applied()
    {
        NetherSnapshot snapshot = Snapshot() with
        {
            Characters = new[] { new NetherCharacterState(1, 1000) },
            CharacterHpHash = "1:1000:1",
        };
        NetherPlannedAction action = new(NetherActionKind.SelectEventOption)
        {
            OptionNumber = 2,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.Heal, 300) },
        };

        Assert.Equal(NetherActionOutcome.NotApplied, NetherActionReconcilePolicy.Evaluate(action, snapshot, snapshot));
    }

    [Fact]
    public void Composed_event_heal_no_op_requires_every_possible_server_target_to_be_saturated()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 700),
            },
            CharacterHpHash = "101:1000:1;102:700:1",
        };
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11) with
        {
            Characters = before.Characters,
            CharacterHpHash = before.CharacterHpHash,
        };
        NetherSnapshot allFullBefore = before with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 1000),
            },
            CharacterHpHash = "101:1000:1;102:1000:1",
        };
        NetherSnapshot allFullAfter = after with
        {
            Characters = allFullBefore.Characters,
            CharacterHpHash = allFullBefore.CharacterHpHash,
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Event,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 1,
            TargetCharacterId = 101,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.Heal, 100) },
        };

        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, after));
        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(
            action with { TargetCharacterId = 999 },
            allFullBefore,
            allFullAfter
        ));
    }

    [Fact]
    public void Composed_event_hp_effect_uses_authoritative_updates_without_a_presentation_target()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 1000),
            },
            CharacterHpHash = "101:1000:1;102:1000:1",
        };
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11) with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 900),
                new NetherCharacterState(102, 900),
            },
            CharacterHpHash = "101:900:1;102:900:1",
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Event,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 1,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.Damage, 100) },
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, after));
    }

    [Fact]
    public void Composed_event_damage_accepts_authoritative_subset_scope_and_rejects_out_of_bound_delta()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 1000),
                new NetherCharacterState(103, 0, IsActive: false),
            },
            CharacterHpHash = "101:1000:1;102:1000:1;103:0:0",
        };
        NetherSnapshot exact = Snapshot(floorId: 11, floorLevel: 11) with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 900),
                new NetherCharacterState(102, 1000),
                new NetherCharacterState(103, 0, IsActive: false),
            },
            CharacterHpHash = "101:900:1;102:1000:1;103:0:0",
        };
        NetherSnapshot otherServerSelectedSubset = exact with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 900),
                new NetherCharacterState(103, 0, IsActive: false),
            },
            CharacterHpHash = "101:1000:1;102:900:1;103:0:0",
        };
        NetherSnapshot partyWide = exact with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 900),
                new NetherCharacterState(102, 900),
                new NetherCharacterState(103, 0, IsActive: false),
            },
            CharacterHpHash = "101:900:1;102:900:1;103:0:0",
        };
        NetherSnapshot noDamageApplied = exact with
        {
            Characters =
            [
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 1000),
                new NetherCharacterState(103, 0, IsActive: false),
            ],
            CharacterHpHash = "101:1000:1;102:1000:1;103:0:0",
        };
        NetherSnapshot mixedDelta = exact with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 900),
                new NetherCharacterState(102, 850),
                new NetherCharacterState(103, 0, IsActive: false),
            },
            CharacterHpHash = "101:900:1;102:850:1;103:0:0",
        };
        NetherSnapshot inactiveChanged = exact with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 900),
                new NetherCharacterState(102, 1000),
                new NetherCharacterState(103, 100, IsActive: false),
            },
            CharacterHpHash = "101:900:1;102:1000:1;103:100:0",
        };
        NetherEffect[] effects = { new(NetherEffectKind.Damage, 100) };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Event,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 1,
            ExpectedEffects = effects,
            OwnedPopupStages = new[]
            {
                new NetherFloorPopupStage(
                    NetherRuntimePopupKind.Event,
                    NetherActionKind.SelectEventOption,
                    OwnerGeneration: 7,
                    Sequence: 1,
                    ExpectedAfterStatus: NetherSessionStatus.Play,
                    OptionNumber: 1,
                    ExpectedEffects: effects,
                    ContentId: 0,
                    ContentAmount: 0,
                    GoldCost: 0,
                    CodeId: 0,
                    ReplaceCodeId: 0,
                    TargetCharacterId: 101
                ),
            },
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, otherServerSelectedSubset));
        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, partyWide));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            noDamageApplied
        ));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, mixedDelta));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(action, before, inactiveChanged));
    }

    [Fact]
    public void Event_damage_with_native_server_selected_survivor_subset_reconciles_the_completed_parent()
    {
        NetherSnapshot before = Snapshot(
            floorId: 75,
            floorLevel: 18,
            codeReload: 0,
            mapId: 1,
            ticketCount: 33,
            gold: 70,
            codeHash: "code-v2:"
        ) with
        {
            TreasureKeyCount = 0,
            Characters =
            [
                new NetherCharacterState(1300003, 1000),
                new NetherCharacterState(1300007, 1000),
                new NetherCharacterState(1300008, 1000),
                new NetherCharacterState(1300011, 1000),
                new NetherCharacterState(1300018, 1000),
                new NetherCharacterState(1300019, 1000),
                new NetherCharacterState(1300024, 1000),
                new NetherCharacterState(1300026, 1000),
                new NetherCharacterState(1300041, 1000),
                new NetherCharacterState(1300052, 1000),
            ],
            CharacterHpHash = "1300003:1000:1;1300007:1000:1;1300008:1000:1;1300011:1000:1;1300018:1000:1;1300019:1000:1;1300024:1000:1;1300026:1000:1;1300041:1000:1;1300052:1000:1",
            Codes = Array.Empty<NetherCodeState>(),
        };
        NetherSnapshot after = Snapshot(
            floorId: 94,
            floorLevel: 19,
            codeReload: 0,
            mapId: 1,
            ticketCount: 33,
            gold: 70,
            codeHash: "code-v2:"
        ) with
        {
            TreasureKeyCount = 0,
            Characters =
            [
                new NetherCharacterState(1300003, 600),
                new NetherCharacterState(1300007, 600),
                new NetherCharacterState(1300008, 600),
                new NetherCharacterState(1300011, 1000),
                new NetherCharacterState(1300018, 600),
                new NetherCharacterState(1300019, 600),
                new NetherCharacterState(1300024, 600),
                new NetherCharacterState(1300026, 600),
                new NetherCharacterState(1300041, 1000),
                new NetherCharacterState(1300052, 1000),
            ],
            CharacterHpHash = "1300003:600:1;1300007:600:1;1300008:600:1;1300011:1000:1;1300018:600:1;1300019:600:1;1300024:600:1;1300026:600:1;1300041:1000:1;1300052:1000:1",
            Codes = Array.Empty<NetherCodeState>(),
        };
        NetherEffect[] effects =
        [
            new NetherEffect(NetherEffectKind.Damage, 400),
            new NetherEffect(NetherEffectKind.AbyssCodeOffer, 1),
        ];
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.CodeOffer,
            NetherActionKind.KeepCode
        ) with
        {
            FloorId = 94,
            FloorLevel = 19,
            FloorIndex = 0,
            OwnedPopupStages =
            [
                new NetherFloorPopupStage(
                    NetherRuntimePopupKind.Event,
                    NetherActionKind.SelectEventOption,
                    OwnerGeneration: 2,
                    Sequence: 1,
                    ExpectedAfterStatus: NetherSessionStatus.Play,
                    OptionNumber: 1,
                    ExpectedEffects: effects,
                    ContentId: 0,
                    ContentAmount: 0,
                    GoldCost: 0,
                    CodeId: 0,
                    ReplaceCodeId: 0,
                    TargetCharacterId: 1300026
                ),
                CodeStage(NetherActionKind.KeepCode, epoch: 0),
            ],
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, after));
    }

    [Fact]
    public void Authorized_partial_death_event_applies_when_one_authoritative_survivor_remains()
    {
        NetherSnapshot before = PartialDeathBefore();
        NetherSnapshot after = before with
        {
            Characters =
            [
                new NetherCharacterState(1, 0, IsActive: false),
                new NetherCharacterState(2, 300),
            ],
            CharacterHpHash = "1:0:0;2:300:1",
        };

        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(PartialDeathAction(), before, after)
        );
    }

    [Fact]
    public void Authorized_partial_death_event_rejects_full_party_death()
    {
        NetherSnapshot before = PartialDeathBefore();
        NetherSnapshot after = before with
        {
            Characters =
            [
                new NetherCharacterState(1, 0, IsActive: false),
                new NetherCharacterState(2, 0, IsActive: false),
            ],
            CharacterHpHash = "1:0:0;2:0:0",
        };

        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(PartialDeathAction(), before, after)
        );
    }

    [Fact]
    public void Unauthorized_partial_death_event_rejects_a_true_to_false_character_transition()
    {
        NetherSnapshot before = PartialDeathBefore();
        NetherSnapshot after = before with
        {
            Characters =
            [
                new NetherCharacterState(1, 0, IsActive: false),
                new NetherCharacterState(2, 300),
            ],
            CharacterHpHash = "1:0:0;2:300:1",
        };

        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(PartialDeathAction(allowPartialDeath: false), before, after)
        );
    }

    [Fact]
    public void Authorized_partial_death_event_rejects_exact_projected_state_mismatch()
    {
        NetherSnapshot before = PartialDeathBefore();
        NetherSnapshot after = before with
        {
            ErosionPoint = 21,
            Characters =
            [
                new NetherCharacterState(1, 0, IsActive: false),
                new NetherCharacterState(2, 300),
            ],
            CharacterHpHash = "1:0:0;2:300:1",
        };

        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(PartialDeathAction(), before, after)
        );
    }

    [Fact]
    public void Live_event_damage_with_saturated_erosion_heal_accepts_server_owned_party_scope()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with
        {
            ErosionPoint = 0,
            Characters = new[]
            {
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 1000),
                new NetherCharacterState(103, 0, IsActive: false),
            },
            CharacterHpHash = "101:1000:1;102:1000:1;103:0:0",
        };
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11) with
        {
            ErosionPoint = 0,
            Characters = new[]
            {
                new NetherCharacterState(101, 900),
                new NetherCharacterState(102, 900),
                new NetherCharacterState(103, 0, IsActive: false),
            },
            CharacterHpHash = "101:900:1;102:900:1;103:0:0",
        };
        NetherEffect[] effects =
        {
            new(NetherEffectKind.Damage, 100),
            new(NetherEffectKind.ErosionHeal, 10),
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Event,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 3,
            ExpectedEffects = effects,
            HasExpectedErosionDelta = true,
            ExpectedErosionDelta = 0,
            OwnedPopupStages = new[]
            {
                new NetherFloorPopupStage(
                    NetherRuntimePopupKind.Event,
                    NetherActionKind.SelectEventOption,
                    OwnerGeneration: 7,
                    Sequence: 1,
                    ExpectedAfterStatus: NetherSessionStatus.Play,
                    OptionNumber: 3,
                    ExpectedEffects: effects,
                    ContentId: 0,
                    ContentAmount: 0,
                    GoldCost: 0,
                    CodeId: 0,
                    ReplaceCodeId: 0,
                    TargetCharacterId: 101
                )
                {
                    HasExpectedErosionDelta = true,
                    ExpectedErosionDelta = 0,
                },
            },
        };

        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(action, before, after)
        );
    }

    [Fact]
    public void Live_floor_event_damage_and_erosion_heal_accepts_authoritative_subset_update()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with
        {
            ErosionPoint = 25,
            Characters = new[]
            {
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 1000),
            },
            CharacterHpHash = "101:1000:1;102:1000:1",
        };
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11) with
        {
            ErosionPoint = 15,
            Characters = new[]
            {
                new NetherCharacterState(101, 900),
                new NetherCharacterState(102, 1000),
            },
            CharacterHpHash = "101:900:1;102:1000:1",
        };
        NetherEffect[] effects =
        {
            new(NetherEffectKind.Damage, 100),
            new(NetherEffectKind.ErosionHeal, 10),
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Event,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 3,
            ExpectedEffects = effects,
            OwnedPopupStages = new[]
            {
                new NetherFloorPopupStage(
                    NetherRuntimePopupKind.Event,
                    NetherActionKind.SelectEventOption,
                    OwnerGeneration: 2,
                    Sequence: 2,
                    ExpectedAfterStatus: NetherSessionStatus.Play,
                    OptionNumber: 3,
                    ExpectedEffects: effects,
                    ContentId: 0,
                    ContentAmount: 0,
                    GoldCost: 0,
                    CodeId: 0,
                    ReplaceCodeId: 0,
                    TargetCharacterId: 101
                ),
            },
        };

        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(action, before, after)
        );
    }

    [Fact]
    public void Server_selected_subset_event_damage_reconciles_when_another_living_character_is_unchanged()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 1000),
            },
            CharacterHpHash = "101:1000:1;102:1000:1",
        };
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11) with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 900),
                new NetherCharacterState(102, 1000),
            },
            CharacterHpHash = "101:900:1;102:1000:1",
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Event,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 3,
            TargetCharacterId = 101,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.Damage, 100) },
        };

        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(action, before, after)
        );
    }

    [Fact]
    public void Ordinary_event_damage_accepts_authoritative_subset_when_every_living_character_stays_safe()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 1000),
                new NetherCharacterState(102, 1000),
            },
            CharacterHpHash = "101:1000:1;102:1000:1",
        };
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11) with
        {
            Characters = new[]
            {
                new NetherCharacterState(101, 900),
                new NetherCharacterState(102, 1000),
            },
            CharacterHpHash = "101:900:1;102:1000:1",
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Event,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 3,
            TargetCharacterId = 101,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.Damage, 100) },
        };

        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(action, before, after)
        );
    }

    [Fact]
    public void Erosion_heal_without_an_hp_effect_reconciles_independently()
    {
        NetherSnapshot before = Snapshot(floorId: 10) with { ErosionPoint = 25 };
        NetherSnapshot after = Snapshot(floorId: 11, floorLevel: 11) with { ErosionPoint = 15 };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.Event,
            NetherActionKind.SelectEventOption
        ) with
        {
            OptionNumber = 3,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.ErosionHeal, 10) },
        };

        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(action, before, after)
        );
    }

    [Fact]
    public void Multi_stage_event_then_code_parent_requires_both_effect_contracts_from_one_get()
    {
        NetherSnapshot before = Snapshot(floorId: 10, gold: 20, codeHash: "codes:none") with
        {
            Codes = Array.Empty<NetherCodeState>(),
        };
        NetherSnapshot exact = Snapshot(floorId: 11, floorLevel: 11, gold: 25, codeHash: "codes:30024") with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.CodeOffer,
            NetherActionKind.SelectCode
        ) with
        {
            CodeId = 30024,
            OwnedPopupStages = new NetherFloorPopupStage[]
            {
                new(
                    NetherRuntimePopupKind.Event,
                    NetherActionKind.SelectEventOption,
                    OwnerGeneration: 7,
                    Sequence: 1,
                    ExpectedAfterStatus: NetherSessionStatus.Play,
                    OptionNumber: 1,
                    ExpectedEffects: new NetherEffect[]
                    {
                        new NetherEffect(NetherEffectKind.NetherGoldGain, 5),
                        new NetherEffect(NetherEffectKind.AbyssCodeOffer, 1),
                    },
                    ContentId: 0,
                    ContentAmount: 0,
                    GoldCost: 0,
                    CodeId: 0,
                    ReplaceCodeId: 0
                ),
                new(
                    NetherRuntimePopupKind.CodeOffer,
                    NetherActionKind.SelectCode,
                    OwnerGeneration: 7,
                    Sequence: 2,
                    ExpectedAfterStatus: NetherSessionStatus.Play,
                    OptionNumber: 0,
                    ExpectedEffects: Array.Empty<NetherEffect>(),
                    ContentId: 0,
                    ContentAmount: 0,
                    GoldCost: 0,
                    CodeId: 30024,
                    ReplaceCodeId: 0
                ),
            },
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            exact with { NetherGold = 24 }
        ));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            exact with
            {
                Codes = new[] { new NetherCodeState(40024, NetherCodeFamily.Risk, 1) },
                CodeHash = "codes:40024",
            }
        ));
    }

    [Fact]
    public void Multi_reload_parent_aggregates_exact_reload_consumption_once_before_final_code_select()
    {
        NetherSnapshot before = Snapshot(floorId: 10, codeReload: 3, codeHash: "codes:none") with
        {
            Codes = Array.Empty<NetherCodeState>(),
        };
        NetherSnapshot exact = Snapshot(floorId: 11, floorLevel: 11, codeReload: 1, codeHash: "codes:30024") with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.CodeOffer,
            NetherActionKind.SelectCode
        ) with
        {
            CodeId = 30024,
            OwnedPopupStages = new NetherFloorPopupStage[]
            {
                CodeStage(NetherActionKind.ReloadCode, epoch: 0),
                CodeStage(NetherActionKind.ReloadCode, epoch: 1),
                CodeStage(NetherActionKind.SelectCode, epoch: 2, codeId: 30024),
            },
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            exact with { CodeReloadCount = 2 }
        ));
    }

    [Fact]
    public void Keep_code_requires_an_unchanged_portfolio_and_unchanged_reload_count()
    {
        NetherSnapshot before = Snapshot(codeReload: 2, codeHash: "codes:30024") with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherSnapshot alteredPortfolio = before with
        {
            Codes = new[] { new NetherCodeState(40024, NetherCodeFamily.Risk, 1) },
            CodeHash = "codes:40024",
        };

        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(new NetherPlannedAction(NetherActionKind.KeepCode), before, before)
        );
        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(new NetherPlannedAction(NetherActionKind.KeepCode), before, alteredPortfolio)
        );
        Assert.Equal(
            NetherActionOutcome.Ambiguous,
            NetherActionReconcilePolicy.Evaluate(
                new NetherPlannedAction(NetherActionKind.KeepCode),
                before,
                before with { CodeReloadCount = 1 }
            )
        );
    }

    [Fact]
    public void Reload_then_keep_aggregates_reload_consumption_while_preserving_the_original_portfolio()
    {
        NetherSnapshot before = Snapshot(floorId: 10, codeReload: 2, codeHash: "codes:30024") with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherSnapshot exact = Snapshot(floorId: 11, floorLevel: 11, codeReload: 1, codeHash: "codes:30024") with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.CodeOffer,
            NetherActionKind.KeepCode
        ) with
        {
            OwnedPopupStages = new NetherFloorPopupStage[]
            {
                CodeStage(NetherActionKind.ReloadCode, epoch: 0),
                CodeStage(NetherActionKind.KeepCode, epoch: 1),
            },
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            exact with { CodeReloadCount = 2 }
        ));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            exact with
            {
                Codes = new[] { new NetherCodeState(40024, NetherCodeFamily.Risk, 1) },
                CodeHash = "codes:40024",
            }
        ));
    }

    [Fact]
    public void Composed_code_terminals_require_an_exact_zero_reload_delta_when_there_are_no_reload_stages()
    {
        NetherSnapshot selectBefore = Snapshot(floorId: 10, codeReload: 2, codeHash: "codes:none") with
        {
            Codes = Array.Empty<NetherCodeState>(),
        };
        NetherSnapshot selectWrongReload = Snapshot(floorId: 11, floorLevel: 11, codeReload: 1, codeHash: "codes:30024") with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherPlannedAction select = ComposedFloor(NetherRuntimePopupKind.CodeOffer, NetherActionKind.SelectCode) with
        {
            CodeId = 30024,
            OwnedPopupStages = new[] { CodeStage(NetherActionKind.SelectCode, epoch: 0, codeId: 30024) },
        };

        NetherSnapshot keepBefore = Snapshot(floorId: 10, codeReload: 2, codeHash: "codes:30024") with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherSnapshot keepWrongReload = Snapshot(floorId: 11, floorLevel: 11, codeReload: 1, codeHash: "codes:30024") with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherPlannedAction keep = ComposedFloor(NetherRuntimePopupKind.CodeOffer, NetherActionKind.KeepCode) with
        {
            OwnedPopupStages = new[] { CodeStage(NetherActionKind.KeepCode, epoch: 0) },
        };

        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(select, selectBefore, selectWrongReload));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(keep, keepBefore, keepWrongReload));
    }

    [Fact]
    public void Event_code_change_battle_and_resource_stage_all_require_one_final_battle_snapshot()
    {
        NetherSnapshot before = Snapshot(floorId: 10, gold: 20, codeHash: "codes:none") with
        {
            Codes = Array.Empty<NetherCodeState>(),
        };
        NetherSnapshot exact = Snapshot(
            floorId: 11,
            floorLevel: 11,
            gold: 25,
            codeHash: "codes:30024",
            status: NetherSessionStatus.Battle
        ) with
        {
            Codes = new[] { new NetherCodeState(30024, NetherCodeFamily.Safe, 1) },
        };
        NetherPlannedAction action = ComposedFloor(
            NetherRuntimePopupKind.CodeOffer,
            NetherActionKind.SelectCode
        ) with
        {
            ExpectedAfterStatus = NetherSessionStatus.Battle,
            CodeId = 30024,
            OwnedPopupStages = new NetherFloorPopupStage[]
            {
                new(
                    NetherRuntimePopupKind.Event,
                    NetherActionKind.SelectEventOption,
                    OwnerGeneration: 7,
                    Sequence: 1,
                    ExpectedAfterStatus: NetherSessionStatus.Battle,
                    OptionNumber: 1,
                    ExpectedEffects: new NetherEffect[]
                    {
                        new(NetherEffectKind.NetherGoldGain, 5),
                        new(NetherEffectKind.AbyssCodeOffer, 1),
                        new(NetherEffectKind.Battle, 0),
                    },
                    ContentId: 0,
                    ContentAmount: 0,
                    GoldCost: 0,
                    CodeId: 0,
                    ReplaceCodeId: 0
                ),
                CodeStage(NetherActionKind.SelectCode, epoch: 0, codeId: 30024, terminal: NetherSessionStatus.Battle),
            },
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            exact with { NetherGold = 24 }
        ));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            exact with
            {
                Codes = new[] { new NetherCodeState(40024, NetherCodeFamily.Risk, 1) },
                CodeHash = "codes:40024",
            }
        ));
    }

    [Fact]
    public void Composed_shop_buy_and_battle_option_require_their_own_terminal_contract()
    {
        NetherSnapshot before = Snapshot(floorId: 10, gold: 100);
        NetherSnapshot bought = Snapshot(floorId: 11, floorLevel: 11, gold: 80) with
        {
            AcquiredItems = new[] { new NetherRewardItem(42, 1) },
        };
        NetherPlannedAction buy = ComposedFloor(NetherRuntimePopupKind.Shop, NetherActionKind.BuyShopItem) with
        {
            ContentId = 42,
            ContentAmount = 1,
            GoldCost = 20,
        };
        NetherPlannedAction battle = ComposedFloor(NetherRuntimePopupKind.Treasure, NetherActionKind.SelectEventOption) with
        {
            ExpectedAfterStatus = NetherSessionStatus.Battle,
            OptionNumber = 1,
            ExpectedEffects = new[] { new NetherEffect(NetherEffectKind.Battle, 0) },
        };
        NetherSnapshot battleAfter = Snapshot(floorId: 11, floorLevel: 11, status: NetherSessionStatus.Battle, gold: 100);

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(buy, before, bought));
        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(battle, before, battleAfter));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            battle,
            before,
            battleAfter with { Status = NetherSessionStatus.Play }
        ));
    }

    [Fact]
    public void Transform_code_requires_exact_one_removed_one_added_and_preserves_other_codes()
    {
        NetherSnapshot before = Snapshot(codeHash: "30024|40024") with
        {
            Codes =
            [
                new NetherCodeState(30024, NetherCodeFamily.Safe, 1),
                new NetherCodeState(40024, NetherCodeFamily.Risk, 1),
            ],
        };
        NetherSnapshot exact = before with
        {
            Codes =
            [
                new NetherCodeState(30024, NetherCodeFamily.Safe, 1),
                new NetherCodeState(51001, NetherCodeFamily.Rush, 1),
            ],
            CodeHash = "30024|51001",
        };
        NetherPlannedAction action = new(NetherActionKind.TransformCode) { ReplaceCodeId = 40024 };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            exact with { Codes = [new NetherCodeState(51001, NetherCodeFamily.Rush, 1)], CodeHash = "51001" }
        ));
        Assert.Equal(NetherActionOutcome.NotApplied, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            before
        ));
    }

    [Fact]
    public void Composed_event_transform_and_offer_reconcile_each_contract_once()
    {
        NetherSnapshot before = Snapshot(floorId: 10, gold: 20, codeHash: "30024|40024") with
        {
            Codes =
            [
                new NetherCodeState(30024, NetherCodeFamily.Safe, 1),
                new NetherCodeState(40024, NetherCodeFamily.Risk, 1),
            ],
        };
        NetherSnapshot exact = Snapshot(floorId: 11, floorLevel: 11, gold: 25, codeHash: "30024|51001") with
        {
            Codes =
            [
                new NetherCodeState(30024, NetherCodeFamily.Safe, 1),
                new NetherCodeState(51001, NetherCodeFamily.Rush, 1),
            ],
        };
        NetherPlannedAction action = ComposedFloor(NetherRuntimePopupKind.CodeOffer, NetherActionKind.KeepCode) with
        {
            OwnedPopupStages =
            [
                new NetherFloorPopupStage(
                    NetherRuntimePopupKind.Event,
                    NetherActionKind.SelectEventOption,
                    7, 1, NetherSessionStatus.Play, 1,
                    [
                        new NetherEffect(NetherEffectKind.NetherGoldGain, 5),
                        new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0),
                        new NetherEffect(NetherEffectKind.AbyssCodeOffer, 1),
                    ],
                    0, 0, 0, 0, 0
                ),
                new NetherFloorPopupStage(
                    NetherRuntimePopupKind.CodeTransform,
                    NetherActionKind.TransformCode,
                    7, 2, NetherSessionStatus.Play, 0,
                    Array.Empty<NetherEffect>(),
                    0, 0, 0, 0, 40024
                ),
                CodeStage(NetherActionKind.KeepCode, epoch: 0) with { Sequence = 3 },
            ],
        };

        Assert.Equal(NetherActionOutcome.Applied, NetherActionReconcilePolicy.Evaluate(action, before, exact));
        Assert.Equal(NetherActionOutcome.Ambiguous, NetherActionReconcilePolicy.Evaluate(
            action,
            before,
            exact with { NetherGold = 24 }
        ));
    }

    private static NetherPlannedAction ComposedFloor(
        NetherRuntimePopupKind popup,
        NetherActionKind child
    ) => new(NetherActionKind.SelectFloor)
    {
        FloorId = 11,
        FloorLevel = 11,
        FloorIndex = 0,
        ExpectedBeforeStatus = NetherSessionStatus.Play,
        ExpectedAfterStatus = NetherSessionStatus.Play,
        OwnedPopupKind = popup,
        OwnedPopupActionKind = child,
    };

    private static NetherFloorPopupStage CodeStage(
        NetherActionKind action,
        long epoch,
        long codeId = 0,
        NetherSessionStatus terminal = NetherSessionStatus.Play
    ) => new(
        NetherRuntimePopupKind.CodeOffer,
        action,
        OwnerGeneration: 7,
        Sequence: 2,
        ExpectedAfterStatus: terminal,
        OptionNumber: 0,
        ExpectedEffects: Array.Empty<NetherEffect>(),
        ContentId: 0,
        ContentAmount: 0,
        GoldCost: 0,
        CodeId: codeId,
        ReplaceCodeId: 0,
        DecisionEpoch: epoch
    );

    private static NetherSnapshot Snapshot(
        long floorId = 10,
        int floorLevel = 10,
        int codeReload = 2,
        string mapHash = "map-a",
        NetherSessionStatus status = NetherSessionStatus.Play,
        long mapId = 2,
        int ticketCount = 3,
        int gold = 100,
        string codeHash = "30024:5:1",
        IReadOnlyList<NetherCodeState>? codes = null,
        IReadOnlyList<NetherRewardItem>? items = null
    ) => new()
    {
        Status = status,
        NetherId = 1,
        MapId = mapId,
        CurrentFloorId = floorId,
        FloorLevel = floorLevel,
        FloorIndex = 0,
        ErosionPoint = 20,
        TicketCount = ticketCount,
        TreasureKeyCount = 1,
        NetherGold = gold,
        CodeReloadCount = codeReload,
        LockReward = 1,
        CharacterHpHash = "1:1000:1",
        CodeHash = codeHash,
        MapHash = mapHash,
        Codes = codes ?? Array.Empty<NetherCodeState>(),
        AcquiredItems = items ?? Array.Empty<NetherRewardItem>(),
    };

    private static NetherSnapshot PartialDeathBefore() => Snapshot() with
    {
        CurrentNodeId = 1,
        ErosionPoint = 20,
        NetherGold = 100,
        TreasureKeyCount = 1,
        Characters =
        [
            new NetherCharacterState(1, 100),
            new NetherCharacterState(2, 400),
        ],
        CharacterHpHash = "1:100:1;2:400:1",
    };

    private static NetherPlannedAction PartialDeathAction(bool allowPartialDeath = true)
    {
        NetherEffect[] effects = [new NetherEffect(NetherEffectKind.Damage, 100)];
        NetherInteractivePartialDeathEligibility proof = new(
            NetherInteractivePartialDeathObjectiveKind.TreasureHpPayment,
            EventId: 9901,
            EventPartId: 9902,
            ObjectiveNodeId: 1
        )
        {
            IsKnown = true,
            ObjectiveReachable = true,
            ExactTreasureRank = 5,
        };
        NetherEventCommitment commitment = new(
            EventId: 9901,
            EventPartId: 9902,
            OptionNumber: 1,
            Effects: effects,
            ProjectedErosion: 20,
            HpDelta: -100
        )
        {
            FloorId = 10,
            NodeId = 1,
            ProjectedNetherGold = 100,
            ProjectedTreasureKeys = 1,
            PartialDeathEligibility = allowPartialDeath ? proof : null,
            AllowsPartialActiveDeaths = allowPartialDeath,
        };
        return new NetherPlannedAction(NetherActionKind.SelectEventOption)
        {
            OptionNumber = 1,
            ExpectedEffects = effects,
            EventId = 9901,
            EventPartId = 9902,
            EventFloorId = 10,
            EventNodeId = 1,
            ProjectedErosion = 20,
            ProjectedHpDelta = -100,
            ProjectedNetherGold = 100,
            ProjectedTreasureKeys = 1,
            EventCommitment = commitment,
        };
    }
}
