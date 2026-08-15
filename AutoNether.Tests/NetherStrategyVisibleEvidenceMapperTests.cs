#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherStrategyVisibleEvidenceMapperTests
{
    [Fact]
    public void Production_mapper_resolves_direct_treasure_and_event_battles_through_exact_master_relations()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300 and GameAssembly.dll SHA-256
        // 573fa8...e1fb: NetherFloorModel.CreateModel resolves MNetherFloorBattles by
        // m_nether_map_floor_id and then MNetherBattleStages by m_nether_battle_stage_id.
        // NetherTreasurePopupController.InitializeView resolves MNetherFloorEvents/Parts from the
        // live map-floor/ExtendId. IDs are deliberately all different so ID reuse cannot pass.
        NetherFloorNode battle = Floor(101, 10001, NetherFloorNodeType.Battle);
        NetherFloorNode treasure = Floor(102, 10002, NetherFloorNodeType.Treasure);
        NetherFloorNode eventFloor = Floor(103, 10003, NetherFloorNodeType.Event);
        NetherFloorNode boss = Floor(104, 10004, NetherFloorNodeType.Boss);
        NetherFloorNode futureShop = Floor(105, 10005, NetherFloorNodeType.Shop);
        var request = new NetherStrategyVisibleEvidenceCaptureRequest(
            [battle, treasure, eventFloor, boss, futureShop],
            [
                new NetherStrategyBattleMasterRow(901, 101, 2, 1901, 321),
                new NetherStrategyBattleMasterRow(902, 999, 3, 1902, 654),
                new NetherStrategyBattleMasterRow(904, 104, 4, 1904, 777),
            ],
            [new NetherStrategyTreasureMasterRow(801, 102)],
            [
                new NetherFloorEventMasterRow(501, 102, 10, 601, 0, 0, 0),
                new NetherFloorEventMasterRow(502, 103, 20, 602, 0, 0, 0),
            ],
            [
                new NetherFloorEventPartMasterRow(601, 0, 0, 0, 0, 0, 0, 30, 701, 2),
                new NetherFloorEventPartMasterRow(602, 8, 902, 2, 123, 3, 7, 165, 0, 40),
            ],
            [new NetherStrategyItemMasterRow(701, 77, 5, 888, 9)]
        )
        {
            ExtendIdByNodeId = new Dictionary<long, long>
            {
                [10002] = 501,
                [10003] = 502,
            },
        };

        NetherStrategyVisibleEvidenceCaptureResult result =
            NetherStrategyVisibleEvidenceMapper.Map(request);

        Assert.True(result.IsSuccess, result.Detail);
        NetherStrategyVisibleMapEvidence visible = result.Evidence!;
        NetherStrategyVisibleContentRow direct = Assert.Single(
            visible.ContentRows,
            row => row.NodeId == 10001 && row.Kind == NetherStrategyVisibleContentKind.Battle
        );
        Assert.Equal(901, direct.MasterRowId);
        Assert.Equal(101, direct.MapFloorMasterId);
        Assert.Equal(1901, direct.BattleStageId);
        Assert.Equal(2, direct.BattleType);
        Assert.Equal(321, direct.CodeDropRatio);

        NetherStrategyVisibleContentRow treasureRow = Assert.Single(
            visible.ContentRows,
            row => row.NodeId == 10002 && row.Kind == NetherStrategyVisibleContentKind.Treasure
        );
        Assert.Equal(801, treasureRow.MasterRowId);
        Assert.Equal(501, treasureRow.EventId);
        NetherStrategyVisibleContentRow reward = Assert.Single(
            visible.ContentRows,
            row => row.NodeId == 10002 && row.Kind == NetherStrategyVisibleContentKind.Item
        );
        Assert.Equal(601, reward.EventPartId);
        Assert.Equal(701, reward.ContentId);
        Assert.Equal(77, reward.ItemType);
        Assert.Equal(5, reward.ItemRarity);
        Assert.Equal(888, reward.ItemValue);
        Assert.Equal(9, reward.ItemPossessionLimit);

        NetherStrategyVisibleContentRow eventBattle = Assert.Single(
            visible.ContentRows,
            row => row.NodeId == 10003 && row.Kind == NetherStrategyVisibleContentKind.Battle
        );
        Assert.Equal(902, eventBattle.MasterRowId);
        Assert.Equal(1902, eventBattle.BattleStageId);
        Assert.Equal(654, eventBattle.CodeDropRatio);
        Assert.Equal(602, eventBattle.EventPartId);

        NetherStrategyVisibleContentRow eventOption = Assert.Single(
            visible.ContentRows,
            row => row.NodeId == 10003
                && row.Kind == NetherStrategyVisibleContentKind.Event
                && row.EventPartId == 602
        );
        NetherStrategyVisibleEventOptionEvidence option = Assert.Single(eventOption.EventOptions);
        Assert.Equal(1, option.OptionNumber);
        Assert.Collection(
            option.Effects,
            effect =>
            {
                Assert.Equal(NetherStrategyVisibleEventEffectSource.Target1, effect.Source);
                Assert.Equal(8, effect.RawType);
                Assert.Equal(902, effect.RawParameter);
                Assert.True(effect.IsKnown, effect.UnknownReason);
            },
            effect =>
            {
                Assert.Equal(NetherStrategyVisibleEventEffectSource.Target2, effect.Source);
                Assert.Equal(2, effect.RawType);
                Assert.Equal(123, effect.RawParameter);
                Assert.Equal(NetherEffectKind.Damage, effect.EffectKind);
                Assert.True(effect.IsKnown, effect.UnknownReason);
            },
            effect =>
            {
                Assert.Equal(NetherStrategyVisibleEventEffectSource.Target3, effect.Source);
                Assert.Equal(3, effect.RawType);
                Assert.Equal(7, effect.RawParameter);
                Assert.Equal(NetherEffectKind.Erosion, effect.EffectKind);
                Assert.True(effect.IsKnown, effect.UnknownReason);
            },
            effect =>
            {
                Assert.Equal(NetherStrategyVisibleEventEffectSource.Content, effect.Source);
                Assert.Equal(165, effect.RawType);
                Assert.Equal(0, effect.ContentId);
                Assert.Equal(40, effect.Amount);
                Assert.True(effect.IsKnown, effect.UnknownReason);
            }
        );

        NetherStrategyVisibleContentRow bossRow = Assert.Single(
            visible.ContentRows,
            row => row.NodeId == 10004 && row.Kind == NetherStrategyVisibleContentKind.Boss
        );
        Assert.Equal(904, bossRow.MasterRowId);
        Assert.Equal(1904, bossRow.BattleStageId);

        NetherStrategyVisibleContentRow shop = Assert.Single(
            visible.ContentRows,
            row => row.NodeId == 10005 && row.Kind == NetherStrategyVisibleContentKind.ShopInventory
        );
        Assert.False(shop.IsKnown);
        Assert.Equal("shop-inventory-not-materialized-before-entry", shop.UnknownReason);

        var snapshot = new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            NetherId = 1,
            MapId = 2,
            CurrentFloorId = battle.FloorId,
            CurrentNodeId = battle.NodeId,
            FloorLevel = 20,
            MasterMaxFloorLevel = 130,
            AuthoritativeBossFloorLevels = new[] { 10, 20, 30, 40, 50, 60, 70 },
            Floors = request.Floors,
            CharacterHpHash = "party",
            CodeHash = "codes",
            MapHash = "map",
        };
        NetherStrategyEvidenceMapResult package = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(
                new NetherStrategyEvidenceIdentity(3, 3, 3, snapshot.Fingerprint),
                snapshot
            )
            {
                VisibleMap = visible,
            }
        );
        Assert.True(package.IsMapped, package.Detail);
        Assert.True(package.Package!.VisibleMap.IsKnown, package.Package.VisibleMap.UnknownReason);
    }

    [Fact]
    public void Production_assembler_derives_exact_extend_identity_from_the_interactive_capture()
    {
        NetherFloorNode floor = Floor(103, 10003, NetherFloorNodeType.Event);
        var snapshot = new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            CurrentFloorId = floor.FloorId,
            CurrentNodeId = floor.NodeId,
            Floors = [floor],
        };
        var interactiveInput = new NetherInteractiveFloorPreEntrySafetyInput(
            FloorKind: NetherFloorNodeType.Event,
            FloorMasterId: floor.FloorId,
            MapFloorRows: [],
            EventRows: [],
            EventPartRows: [],
            CurrentErosion: 20,
            ActiveHpPermille: [500],
            CurrentNetherGold: 0,
            CurrentTreasureKeys: 0,
            Settings: new NetherAutoClimbSettings()
        )
        {
            FloorExtendId = 502,
        };
        NetherRuntimeInteractivePreEntryInputsResult interactive =
            NetherRuntimeInteractivePreEntryInputsResult.Success(
                new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
                {
                    [floor.NodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
                    {
                        IsCaptured = true,
                        Input = interactiveInput,
                    },
                }
            );
        var capturedMasters = new NetherStrategyVisibleEvidenceCaptureRequest(
            [floor],
            [new NetherStrategyBattleMasterRow(902, 999, 3, 1902, 654)],
            [],
            [new NetherFloorEventMasterRow(502, 103, 20, 602, 0, 0, 0)],
            [new NetherFloorEventPartMasterRow(602, 8, 902, 0, 0, 0, 0, 0, 0, 0)],
            []
        );

        NetherStrategyVisibleEvidenceCaptureResult result =
            NetherStrategyVisibleEvidenceAssembler.Assemble(
                new NetherStrategyVisibleEvidenceAssemblyRequest(
                    snapshot,
                    interactive,
                    NetherRuntimePopupResult.Failure("no-current-popup"),
                    capturedMasters
                )
            );

        Assert.True(result.IsSuccess, result.Detail);
        NetherStrategyVisibleContentRow eventBattle = Assert.Single(
            result.Evidence!.ContentRows,
            row => row.NodeId == floor.NodeId
                && row.Kind == NetherStrategyVisibleContentKind.Battle
        );
        Assert.Equal(602, eventBattle.EventPartId);
        Assert.Equal(902, eventBattle.MasterRowId);
        Assert.Equal(1902, eventBattle.BattleStageId);
    }

    private static NetherFloorNode Floor(long masterId, long nodeId, NetherFloorNodeType type) =>
        new(masterId, 20, 0, type)
        {
            NodeId = nodeId,
            ApiFloorIndex = checked((int)(nodeId - 10000)),
            IsUnlocked = true,
        };
}
