using System.Collections;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherRuntimeInteractivePreEntryInputCaptureTests
{
    [Fact]
    public void Exact_floor_and_master_numeric_fields_resolve_the_native_extend_event()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 42, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[]
            {
                Event(42, 900, rowWeight: 1, eventType: 4, part1: 1001),
                Event(43, 900, rowWeight: 1, eventType: 4, part1: 1002),
            },
            parts: new object[]
            {
                Part(1001, target1: (int)NetherEffectKind.Heal, parameter1: 1),
                Part(1002, target1: (int)NetherEffectKind.NetherGoldUsed, parameter1: 0),
            }
        );

        Assert.True(result.IsCaptured);
        Assert.NotNull(result.Input);
        Assert.Equal(900, result.Input!.FloorMasterId);
        Assert.Equal(42, result.Input.FloorExtendId);
        Assert.Equal(NetherFloorNodeType.Event, result.Input.FloorKind);
        Assert.True(result.Safety.IsSafe);
        Assert.Single(result.Safety.SafeOptionNumberByEventId);
        Assert.Equal(1, result.Safety.SafeOptionNumberByEventId[42]);
    }

    [Fact]
    public void Missing_master_or_unknown_authoritative_resource_is_captured_as_fail_closed_input()
    {
        NetherRuntimeInteractivePreEntryCaptureResult missingMaster = Capture(
            mapRows: Array.Empty<object>(),
            events: new object[] { Event(42, 900, 1, 4, 1001) },
            parts: new object[] { Part(1001, (int)NetherEffectKind.Heal, 1) }
        );
        NetherRuntimeInteractivePreEntryCaptureResult missingGold = Capture(
            gold: null,
            events: new object[] { Event(42, 900, 1, 4, 1001) },
            parts: new object[] { Part(1001, (int)NetherEffectKind.Heal, 1) }
        );

        Assert.True(missingMaster.IsCaptured);
        Assert.False(missingMaster.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, missingMaster.Safety.PauseReason);
        Assert.True(missingGold.IsCaptured);
        Assert.False(missingGold.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, missingGold.Safety.PauseReason);
    }

    [Fact]
    public void Missing_referenced_part_cannot_be_promoted_to_safe()
    {
        NetherRuntimeInteractivePreEntryCaptureResult missingPart = Capture(
            events: new object[] { Event(42, 900, 1, 4, 9999) },
            parts: Array.Empty<object>()
        );

        Assert.True(missingPart.IsCaptured);
        Assert.False(missingPart.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, missingPart.Safety.PauseReason);
    }

    [Fact]
    public void Bad_part_numeric_shape_is_rejected_without_localized_text_or_default_effect()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            events: new object[] { Event(42, 900, 1, 4, 1001) },
            parts: new object[] { Part(1001, target1: 99, parameter1: 0) }
        );

        Assert.True(result.IsCaptured);
        Assert.False(result.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, result.Safety.PauseReason);
        Assert.Contains("unsupported-event-target", result.Safety.Detail);
    }

    [Fact]
    public void Production_preentry_capture_carries_exact_route_procurement_budget_into_option_projection()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 42, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(42, 900, 1, 4, 1001) },
            parts: new object[] { Part(1001, target1: (int)NetherEffectKind.NetherGoldUsed, parameter1: 20) },
            gold: 200,
            committedProcurement: new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
            {
                [new NetherInteractiveEventOptionKey(42, 1001, 1)] = new(150, 0),
            }
        );

        Assert.True(result.IsCaptured);
        Assert.True(result.Safety.IsSafe, result.Safety.PauseReason + ":" + result.Safety.Detail);
        NetherInteractiveOptionProjection projection = result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(42, 1001, 1)
        ];
        Assert.True(projection.IsKnown);
        Assert.True(projection.HasCommittedProcurementEvidence);
        Assert.Equal(150, projection.CommittedGoldMinimum);
        Assert.Equal(0, projection.CommittedKeyMinimum);
    }

    [Fact]
    public void Production_recovery_capture_fails_closed_when_selected_horizon_proofs_are_absent()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 42, FloorType = (int)NetherFloorNodeType.Recovery },
            events: new object[] { Event(42, 900, 1, 4, 1001, 1002, 1003) },
            parts: new object[]
            {
                Part(1001, (int)NetherEffectKind.Heal, 100),
                Part(1002, (int)NetherEffectKind.ErosionHeal, 10),
                Part(1003, 7, 0),
            },
            requireCompleteRecoveryBranchSafety: true
        );

        Assert.True(result.IsCaptured);
        Assert.False(result.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, result.Safety.PauseReason);
        Assert.Contains("recovery-complete-visible-branch-unavailable", result.Safety.Detail);
    }

    [Fact]
    public void Malformed_empty_target_parameter_is_option_local_and_does_not_become_known_gold_gain()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 41, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(41, 900, 1, 4, 20091, part2: 20092) },
            parts: new object[]
            {
                Part(20091, target1: 0, parameter1: 999, contentType: 165, amount: 1),
                Part(20092, target1: 0, parameter1: 0, contentType: 165, amount: 1),
            }
        );

        Assert.True(result.IsCaptured);
        Assert.True(result.Safety.IsSafe, result.Safety.PauseReason + ":" + result.Safety.Detail);
        Assert.Equal(2, result.Safety.SafeOptionNumberByEventId[41]);
        NetherInteractiveOptionProjection malformed = result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(41, 20091, 1)
        ];
        Assert.False(malformed.IsKnown);
        Assert.Contains("unsupported-event-target", malformed.UnknownReason);
        Assert.True(result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(41, 20092, 2)
        ].IsKnown);
    }

    [Theory]
    [InlineData(165)]
    [InlineData(166)]
    public void Negative_resource_content_id_is_captured_as_unknown_instead_of_a_known_reward(int contentType)
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 43, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(43, 900, 1, 4, 20093) },
            parts: new object[]
            {
                Part(20093, target1: 0, parameter1: 0, contentType: contentType, contentId: -1, amount: 1),
            }
        );

        Assert.True(result.IsCaptured);
        Assert.False(result.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, result.Safety.PauseReason);
        Assert.Contains("unsupported-event-content-type:" + contentType, result.Safety.Detail);
    }

    [Fact]
    public void Out_of_domain_item_rarity_is_captured_as_unknown_instead_of_a_known_reward()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 44, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(44, 900, 1, 4, 20094) },
            parts: new object[] { Part(20094, target1: 0, parameter1: 0, contentType: 30, contentId: 701, amount: 1) },
            itemRows: new object[] { new ItemFixture { id = 701, type = 91, rarity = 999, value = 1, possession_limit = 99 } }
        );

        Assert.True(result.IsCaptured);
        Assert.False(result.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, result.Safety.PauseReason);
        Assert.Contains("event-item-master-row-unavailable", result.Safety.Detail);
    }

    [Fact]
    public void Live_event_shape_maps_native_code_offer_without_renumbering_it()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 35, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(35, 900, 1, 1, 20042) },
            parts: new object[]
            {
                Part(20042, target1: 5, parameter1: 50, contentType: 160, contentId: 0, amount: 1),
            }
        );

        Assert.True(result.IsCaptured);
        Assert.True(result.Safety.IsSafe, result.Safety.PauseReason + ":" + result.Safety.Detail);
        Assert.Equal(1, result.Safety.SafeOptionNumberByEventId[35]);
        NetherEffect offer = Assert.Single(result.Safety.SafeOptionProjectionByEventId[35].ExpectedEffects,
            effect => effect.Kind == NetherEffectKind.AbyssCodeOffer);
        Assert.Equal(0, offer.ContentId);
    }

    [Fact]
    public void Mixed_production_event_options_isolate_duplicate_item_rows_to_the_dependent_option()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 37, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(37, 900, 1, 4, 20051, part2: 20052) },
            parts: new object[]
            {
                Part(20051, target1: 0, parameter1: 0, contentType: 30, contentId: 8101, amount: 1),
                Part(20052, target1: (int)NetherEffectKind.Heal, parameter1: 1),
            },
            itemRows: new object[]
            {
                Item(8101),
                Item(8101),
            }
        );

        Assert.True(result.IsCaptured);
        Assert.True(result.Safety.IsSafe, result.Safety.PauseReason + ":" + result.Safety.Detail);
        Assert.Equal(2, result.Safety.SafeOptionNumberByEventId[37]);
        Assert.False(result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(37, 20051, 1)
        ].IsKnown);
        Assert.True(result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(37, 20052, 2)
        ].IsKnown);
    }

    [Fact]
    public void Malformed_item_row_with_same_id_as_valid_row_invalidates_only_its_dependent_option()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 39, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(39, 900, 1, 4, 20071, part2: 20072) },
            parts: new object[]
            {
                Part(20071, target1: 0, parameter1: 0, contentType: 30, contentId: 8102, amount: 1),
                Part(20072, target1: (int)NetherEffectKind.Heal, parameter1: 1),
            },
            itemRows: new object[]
            {
                new MalformedItemFixture { id = 8102 },
                Item(8102),
            }
        );

        Assert.True(result.IsCaptured);
        Assert.True(result.Safety.IsSafe, result.Safety.PauseReason + ":" + result.Safety.Detail);
        Assert.Equal(2, result.Safety.SafeOptionNumberByEventId[39]);
        Assert.False(result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(39, 20071, 1)
        ].IsKnown);
        Assert.True(result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(39, 20072, 2)
        ].IsKnown);
    }

    [Fact]
    public void Mixed_production_event_options_isolate_duplicate_battle_rows_to_the_dependent_option()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 38, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(38, 900, 1, 4, 20061, part2: 20062) },
            parts: new object[]
            {
                Part(20061, target1: (int)NetherEffectKind.Battle, parameter1: 8201),
                Part(20062, target1: (int)NetherEffectKind.Heal, parameter1: 1),
            },
            battleRows: new object[]
            {
                Battle(8201),
                Battle(8201),
            }
        );

        Assert.True(result.IsCaptured);
        Assert.True(result.Safety.IsSafe, result.Safety.PauseReason + ":" + result.Safety.Detail);
        Assert.Equal(2, result.Safety.SafeOptionNumberByEventId[38]);
        Assert.False(result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(38, 20061, 1)
        ].IsKnown);
        Assert.True(result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(38, 20062, 2)
        ].IsKnown);
    }

    [Fact]
    public void Malformed_battle_row_with_same_id_as_valid_row_invalidates_only_its_dependent_option()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 40, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(40, 900, 1, 4, 20081, part2: 20082) },
            parts: new object[]
            {
                Part(20081, target1: (int)NetherEffectKind.Battle, parameter1: 8202),
                Part(20082, target1: (int)NetherEffectKind.Heal, parameter1: 1),
            },
            battleRows: new object[]
            {
                new MalformedBattleFixture { id = 8202 },
                Battle(8202),
            }
        );

        Assert.True(result.IsCaptured);
        Assert.True(result.Safety.IsSafe, result.Safety.PauseReason + ":" + result.Safety.Detail);
        Assert.Equal(2, result.Safety.SafeOptionNumberByEventId[40]);
        Assert.False(result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(40, 20081, 1)
        ].IsKnown);
        Assert.True(result.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(40, 20082, 2)
        ].IsKnown);
    }

    [Fact]
    public void Live_target_seven_captures_current_codes_but_fails_closed_without_transform_eligibility()
    {
        NetherRuntimeInteractivePreEntryCaptureResult result = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 36, FloorType = (int)NetherFloorNodeType.Event },
            events: new object[] { Event(36, 900, 1, 1, 20043) },
            parts: new object[] { Part(20043, target1: 7, parameter1: 0) },
            codes: [new NetherCodeState(40024, NetherCodeFamily.Risk, 1)]
        );

        Assert.True(result.IsCaptured);
        Assert.False(result.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.NoSafeRoute, result.Safety.PauseReason);
        Assert.Contains("equipment-code-transform-disabled", result.Safety.Detail);
        Assert.Single(result.Input!.CurrentCodes);
    }

    [Fact]
    public void Shop_close_capability_is_an_explicit_exact_binding_boolean_not_a_default_true()
    {
        NetherRuntimeInteractivePreEntryCaptureResult unavailable = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 0, FloorType = (int)NetherFloorNodeType.Shop },
            canCloseShop: false
        );
        NetherRuntimeInteractivePreEntryCaptureResult available = Capture(
            floor: new FloorFixture { MNetherMapFloorId = 900, ExtendId = 0, FloorType = (int)NetherFloorNodeType.Shop },
            canCloseShop: true
        );

        Assert.True(unavailable.IsCaptured);
        Assert.False(unavailable.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.BindingUnavailable, unavailable.Safety.PauseReason);
        Assert.True(available.IsCaptured);
        Assert.True(available.Safety.IsSafe);
    }

    private static NetherRuntimeInteractivePreEntryCaptureResult Capture(
        object? floor = null,
        IEnumerable? mapRows = null,
        IEnumerable? events = null,
        IEnumerable? parts = null,
        int? erosion = 20,
        IReadOnlyList<int>? hp = null,
        int? gold = 100,
        int? keys = 1,
        bool canCloseShop = false,
        IReadOnlyList<NetherCodeState>? codes = null,
        IEnumerable? itemRows = null,
        IEnumerable? battleRows = null,
        IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? committedProcurement = null,
        bool requireCompleteRecoveryBranchSafety = false
    ) => new NetherRuntimeInteractivePreEntryInputCapture().Capture(new NetherRuntimeInteractivePreEntryCaptureRequest(
        FloorModel: floor ?? new FloorFixture
        {
            MNetherMapFloorId = 900,
            ExtendId = 42,
            FloorType = (int)NetherFloorNodeType.Event,
        },
        MapFloorRows: mapRows ?? new ArrayList { new MapFloorFixture { id = 900, min_erosion_point = 0, max_erosion_point = 10 } },
        EventRows: events ?? new ArrayList { Event(42, 900, 1, 4, 1001) },
        EventPartRows: parts ?? new ArrayList { Part(1001, (int)NetherEffectKind.Heal, 1) },
        CurrentErosion: erosion,
        ActiveHpPermille: hp ?? new[] { 500 },
        CurrentNetherGold: gold,
        CurrentTreasureKeys: keys,
        Settings: new NetherAutoClimbSettings
        {
            SoftErosionLimit = 90,
            MinimumCharacterHpPermille = 300,
            ShopMode = NetherShopMode.Off,
            TreasureMode = NetherTreasureMode.KeyOnly,
        },
        CanCloseShop: canCloseShop
    )
    {
        CurrentCodes = codes ?? [new NetherCodeState(40024, NetherCodeFamily.Risk, 1)],
        CodeCapacity = 5,
        ItemRows = itemRows,
        BattleRows = battleRows,
        CommittedProcurementByOption = committedProcurement
            ?? new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>(),
        RequireCompleteRecoveryBranchSafety = requireCompleteRecoveryBranchSafety,
    });

    private static EventFixture Event(
        long eventId,
        long mapFloorId,
        int rowWeight,
        int eventType,
        long part1,
        long part2 = 0,
        long part3 = 0,
        long part4 = 0
    ) => new()
    {
        id = eventId,
        m_nether_map_floor_id = mapFloorId,
        weight = rowWeight,
        type = eventType,
        m_nether_floor_event_part_id_1 = part1,
        m_nether_floor_event_part_id_2 = part2,
        m_nether_floor_event_part_id_3 = part3,
        m_nether_floor_event_part_id_4 = part4,
    };

    private static PartFixture Part(
        long partId,
        int target1,
        long parameter1,
        int target2 = 0,
        long parameter2 = 0,
        int target3 = 0,
        long parameter3 = 0,
        int contentType = 0,
        long contentId = 0,
        int amount = 0
    ) => new()
    {
        id = partId,
        target_type_1 = target1,
        select_parameter_1 = parameter1,
        target_type_2 = target2,
        select_parameter_2 = parameter2,
        target_type_3 = target3,
        select_parameter_3 = parameter3,
        content_type = contentType,
        content_id = contentId,
        amount = amount,
    };

    private static ItemFixture Item(long id) => new()
    {
        id = id,
        type = 91,
        rarity = (int)NetherRewardRarity.Gold,
        value = 1,
        possession_limit = 99,
    };

    private static BattleFixture Battle(long id) => new()
    {
        id = id,
        m_nether_map_floor_id = 900,
        type = 1,
        m_nether_battle_stage_id = 8202,
        code_drop_ratio = 100,
    };

    private sealed class FloorFixture
    {
        public long MNetherMapFloorId { get; init; }
        public long ExtendId { get; init; }
        public int FloorType { get; init; }
    }

    private sealed class MapFloorFixture
    {
        public long id;
        public int min_erosion_point;
        public int max_erosion_point;
    }

    private sealed class EventFixture
    {
        public long id;
        public long m_nether_map_floor_id;
        public int weight;
        public int type;
        public long m_nether_floor_event_part_id_1;
        public long m_nether_floor_event_part_id_2;
        public long m_nether_floor_event_part_id_3;
        public long m_nether_floor_event_part_id_4;
    }

    private sealed class PartFixture
    {
        public long id;
        public int target_type_1;
        public long select_parameter_1;
        public int target_type_2;
        public long select_parameter_2;
        public int target_type_3;
        public long select_parameter_3;
        public int content_type;
        public long content_id;
        public int amount;
    }

    private sealed class ItemFixture
    {
        public long id;
        public long type;
        public int rarity;
        public int value;
        public int possession_limit;
    }

    private sealed class MalformedItemFixture
    {
        public long id;
    }

    private sealed class BattleFixture
    {
        public long id;
        public long m_nether_map_floor_id;
        public int type;
        public long m_nether_battle_stage_id;
        public int code_drop_ratio;
    }

    private sealed class MalformedBattleFixture
    {
        public long id;
    }
}
