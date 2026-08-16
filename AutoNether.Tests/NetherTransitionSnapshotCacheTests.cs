#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherTransitionSnapshotCacheTests
{
    [Fact]
    public void Scene_teardown_uses_fresh_datastore_identity_with_cached_floor_graph()
    {
        var cache = new NetherTransitionSnapshotCache();
        NetherSnapshot before = Snapshot(NetherSessionStatus.Play, floorId: 30, floorLevel: 7, apiFloorIndex: 1);
        cache.ObserveFullSnapshot(before);
        cache.BeginBattle();

        NetherRuntimeSnapshotResult result = NetherTransitionSnapshotCompositionPolicy.Compose(
            cache,
            new NetherAuthoritativeTransitionState
            {
                Status = NetherSessionStatus.Battle,
                NetherId = 1,
                MapId = 1,
                CurrentFloorId = 27,
                FloorLevel = 8,
                FloorIndex = 1,
                MaxFloorLevel = 130,
                ContinuanceFloorLevel = 10,
                ErosionPoint = 5,
                TicketCount = 13,
                SignalCount = 0,
                TreasureKeyCount = 0,
                NetherGold = 45,
                CodeReloadCount = 1,
                CodeCapacity = 28,
                LockReward = 0,
                Codes = Array.Empty<NetherCodeState>(),
                AcquiredItems = Array.Empty<NetherRewardItem>(),
            },
            requireFreshBattleCharacters: false,
            purpose: NetherTransitionSnapshotPurpose.BattleSettlement
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(NetherSessionStatus.Battle, result.Snapshot!.Status);
        Assert.Equal(27, result.Snapshot.CurrentFloorId);
        Assert.Equal(38654705666, result.Snapshot.CurrentNodeId);
        Assert.Equal(before.Floors, result.Snapshot.Floors);
        Assert.Equal(before.Characters, result.Snapshot.Characters);
        Assert.Equal(45, result.Snapshot.NetherGold);
    }

    [Fact]
    public void Battle_status_zero_master_floor_id_uses_unique_authoritative_coordinate()
    {
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(Snapshot(NetherSessionStatus.Play, floorId: 30, floorLevel: 7, apiFloorIndex: 1));
        cache.BeginBattle();

        NetherRuntimeSnapshotResult result = cache.TryCompose(
            TransitionState(
                NetherSessionStatus.Battle,
                floorId: 0,
                floorLevel: 8,
                apiFloorIndex: 1
            ),
            requireFreshBattleCharacters: false
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(27, result.Snapshot!.CurrentFloorId);
        Assert.Equal(38654705666, result.Snapshot.CurrentNodeId);
    }

    [Fact]
    public void Battle_status_zero_master_floor_id_fails_when_coordinate_is_not_unique()
    {
        var cache = new NetherTransitionSnapshotCache();
        NetherSnapshot snapshot = Snapshot(
            NetherSessionStatus.Play,
            floorId: 30,
            floorLevel: 7,
            apiFloorIndex: 1
        );
        cache.ObserveFullSnapshot(snapshot with
        {
            Floors = snapshot.Floors.Concat(new[]
            {
                new NetherFloorNode(99, 8, 1, NetherFloorNodeType.Boss)
                {
                    NodeId = 38654705667,
                    ApiFloorIndex = 1,
                    IsUnlocked = true,
                },
            }).ToArray(),
        });

        NetherRuntimeSnapshotResult result = cache.TryCompose(
            TransitionState(
                NetherSessionStatus.Battle,
                floorId: 0,
                floorLevel: 8,
                apiFloorIndex: 1
            ),
            requireFreshBattleCharacters: false
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "authoritative-battle-coordinate-not-unique:level=8:api-index=1:matches=2",
            result.Detail
        );
    }

    [Fact]
    public void Play_status_zero_master_floor_id_remains_invalid()
    {
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(Snapshot(NetherSessionStatus.Play, floorId: 30, floorLevel: 7, apiFloorIndex: 1));

        NetherRuntimeSnapshotResult result = cache.TryCompose(
            TransitionState(
                NetherSessionStatus.Play,
                floorId: 0,
                floorLevel: 8,
                apiFloorIndex: 1
            ),
            requireFreshBattleCharacters: false
        );

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-authoritative-current-floor", result.Detail);
    }

    [Fact]
    public void Postbattle_play_status_zero_master_floor_id_uses_unique_authoritative_coordinate()
    {
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(Snapshot(NetherSessionStatus.Play, floorId: 30, floorLevel: 7, apiFloorIndex: 1));
        cache.BeginBattle();
        Assert.True(cache.ObserveBattleResultCharacters(new[]
        {
            new NetherCharacterState(1001, 720, true),
            new NetherCharacterState(1002, 0, false),
        }));

        NetherRuntimeSnapshotResult result = cache.TryCompose(
            TransitionState(
                NetherSessionStatus.Play,
                floorId: 0,
                floorLevel: 8,
                apiFloorIndex: 1
            ),
            requireFreshBattleCharacters: true
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(NetherSessionStatus.Play, result.Snapshot!.Status);
        Assert.Equal(27, result.Snapshot.CurrentFloorId);
        Assert.Equal(38654705666, result.Snapshot.CurrentNodeId);
        Assert.Equal(720, result.Snapshot.Characters[0].HpPermille);
    }

    [Fact]
    public void Postbattle_sleep_status_zero_master_floor_id_uses_unique_authoritative_coordinate()
    {
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(Snapshot(NetherSessionStatus.Play, floorId: 30, floorLevel: 7, apiFloorIndex: 1));
        cache.BeginBattle();
        Assert.True(cache.ObserveBattleResultCharacters(new[]
        {
            new NetherCharacterState(1001, 720, true),
            new NetherCharacterState(1002, 0, false),
        }));

        NetherRuntimeSnapshotResult result = cache.TryCompose(
            TransitionState(
                NetherSessionStatus.Sleep,
                floorId: 0,
                floorLevel: 8,
                apiFloorIndex: 1
            ),
            requireFreshBattleCharacters: true
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(NetherSessionStatus.Sleep, result.Snapshot!.Status);
        Assert.Equal(27, result.Snapshot.CurrentFloorId);
        Assert.Equal(38654705666, result.Snapshot.CurrentNodeId);
        Assert.Equal(720, result.Snapshot.Characters[0].HpPermille);
    }

    [Fact]
    public void Clear_result_characters_replace_prebattle_hp_in_postbattle_snapshot()
    {
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(Snapshot(NetherSessionStatus.Play, floorId: 30, floorLevel: 7, apiFloorIndex: 1));
        cache.BeginBattle();
        Assert.True(cache.ObserveBattleResultCharacters(new[]
        {
            new NetherCharacterState(1001, 720, true),
            new NetherCharacterState(1002, 0, false),
        }));

        NetherRuntimeSnapshotResult result = cache.TryCompose(
            new NetherAuthoritativeTransitionState
            {
                Status = NetherSessionStatus.Play,
                NetherId = 1,
                MapId = 1,
                CurrentFloorId = 27,
                FloorLevel = 8,
                FloorIndex = 1,
                MaxFloorLevel = 130,
                ContinuanceFloorLevel = 10,
                ErosionPoint = 10,
                TicketCount = 13,
                SignalCount = 0,
                TreasureKeyCount = 0,
                NetherGold = 50,
                CodeReloadCount = 1,
                CodeCapacity = 28,
                LockReward = 0,
                Codes = new[]
                {
                    new NetherCodeState(30024, NetherCodeFamily.Safe, 1),
                },
                AcquiredItems = Array.Empty<NetherRewardItem>(),
            },
            requireFreshBattleCharacters: true
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(720, result.Snapshot!.Characters[0].HpPermille);
        Assert.Equal(0, result.Snapshot.Characters[1].HpPermille);
        Assert.Contains("1001:720", result.Snapshot.CharacterHpHash);
        Assert.Contains("30024", result.Snapshot.CodeHash);
    }

    [Fact]
    public void Postbattle_reused_master_floor_id_resolves_the_exact_authoritative_coordinate()
    {
        var cache = new NetherTransitionSnapshotCache();
        NetherSnapshot cached = Snapshot(
            NetherSessionStatus.Play,
            floorId: 219,
            floorLevel: 47,
            apiFloorIndex: 1
        ) with
        {
            CurrentNodeId = 206158430211,
            Floors = new[]
            {
                new NetherFloorNode(219, 47, 1, NetherFloorNodeType.Battle)
                {
                    NodeId = 206158430211,
                    ApiFloorIndex = 1,
                    IsUnlocked = true,
                },
                new NetherFloorNode(219, 48, 1, NetherFloorNodeType.MiniBoss)
                {
                    NodeId = 210453397506,
                    ApiFloorIndex = 1,
                    IsUnlocked = true,
                    PreviousFloorIds = new[] { 206158430211L },
                },
            },
        };
        cache.ObserveFullSnapshot(cached);
        cache.BeginBattle();
        Assert.True(cache.ObserveBattleResultCharacters(new[]
        {
            new NetherCharacterState(1001, 720, true),
            new NetherCharacterState(1002, 0, false),
        }));

        NetherRuntimeSnapshotResult result = cache.TryCompose(
            TransitionState(
                NetherSessionStatus.Play,
                floorId: 219,
                floorLevel: 48,
                apiFloorIndex: 1
            ),
            requireFreshBattleCharacters: true
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(219, result.Snapshot!.CurrentFloorId);
        Assert.Equal(210453397506, result.Snapshot.CurrentNodeId);
        Assert.Equal(48, result.Snapshot.FloorLevel);
    }

    [Fact]
    public void Postbattle_snapshot_fails_closed_without_authoritative_result_characters()
    {
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(Snapshot(NetherSessionStatus.Play, floorId: 30, floorLevel: 7, apiFloorIndex: 1));
        cache.BeginBattle();

        NetherRuntimeSnapshotResult result = cache.TryCompose(
            new NetherAuthoritativeTransitionState
            {
                Status = NetherSessionStatus.Play,
                NetherId = 1,
                MapId = 1,
                CurrentFloorId = 27,
                FloorLevel = 8,
                FloorIndex = 1,
                MaxFloorLevel = 130,
                ContinuanceFloorLevel = 10,
                ErosionPoint = 10,
                TicketCount = 13,
                CodeCapacity = 28,
                Codes = Array.Empty<NetherCodeState>(),
                AcquiredItems = Array.Empty<NetherRewardItem>(),
            },
            requireFreshBattleCharacters: true
        );

        Assert.False(result.IsSuccess);
        Assert.Equal("missing-authoritative-battle-result-characters", result.Detail);
    }

    [Fact]
    public void Transition_cannot_reuse_a_graph_from_another_map()
    {
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(Snapshot(NetherSessionStatus.Play, floorId: 30, floorLevel: 7, apiFloorIndex: 1));

        NetherRuntimeSnapshotResult result = cache.TryCompose(
            new NetherAuthoritativeTransitionState
            {
                Status = NetherSessionStatus.Battle,
                NetherId = 1,
                MapId = 2,
                CurrentFloorId = 27,
                FloorLevel = 8,
                FloorIndex = 1,
                Codes = Array.Empty<NetherCodeState>(),
                AcquiredItems = Array.Empty<NetherRewardItem>(),
            },
            requireFreshBattleCharacters: false
        );

        Assert.False(result.IsSuccess);
        Assert.Contains("cached-transition-owner-mismatch", result.Detail);
    }

    [Fact]
    public void Continue_composition_accepts_fresh_cross_map_datastore_without_reusing_stale_graph()
    {
        var cache = new NetherTransitionSnapshotCache();
        NetherSnapshot stalePresentation = Snapshot(
            NetherSessionStatus.Sleep,
            floorId: 23,
            floorLevel: 23,
            apiFloorIndex: 0
        ) with
        {
            MapId = 2,
            CurrentNodeId = 987654321,
            Floors = new[]
            {
                new NetherFloorNode(23, 23, 0, NetherFloorNodeType.Boss)
                {
                    NodeId = 987654321,
                    ApiFloorIndex = 0,
                    IsUnlocked = true,
                },
            },
            MapHash = "stale-map-2",
        };
        cache.ObserveFullSnapshot(stalePresentation);

        NetherRuntimeSnapshotResult result = NetherTransitionSnapshotCompositionPolicy.Compose(
            cache,
            new NetherAuthoritativeTransitionState
            {
                Status = NetherSessionStatus.Play,
                NetherId = 1,
                MapId = 3,
                CurrentFloorId = 33,
                FloorLevel = 23,
                FloorIndex = 0,
                MaxFloorLevel = 130,
                ContinuanceFloorLevel = 20,
                MasterMaxFloorLevel = 130,
                TicketCount = 0,
                CodeCapacity = 28,
                Codes = Array.Empty<NetherCodeState>(),
                AcquiredItems = Array.Empty<NetherRewardItem>(),
            },
            requireFreshBattleCharacters: false,
            purpose: NetherTransitionSnapshotPurpose.ContinueSettlement
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(3, result.Snapshot!.MapId);
        Assert.Equal(33, result.Snapshot.CurrentFloorId);
        Assert.Equal(23, result.Snapshot.FloorLevel);
        Assert.Equal(130, result.Snapshot.MasterMaxFloorLevel);
        Assert.Equal(0, result.Snapshot.CurrentNodeId);
        Assert.Empty(result.Snapshot.Floors);
        Assert.Equal(string.Empty, result.Snapshot.MapHash);
    }

    [Fact]
    public void Continue_composition_accepts_coordinate_only_play_entry_with_zero_master_floor()
    {
        var cache = new NetherTransitionSnapshotCache();
        NetherSnapshot stalePresentation = Snapshot(
            NetherSessionStatus.Sleep,
            floorId: 96,
            floorLevel: 20,
            apiFloorIndex: 1
        ) with
        {
            MapId = 1,
            CurrentNodeId = 90194313218,
            Floors = new[]
            {
                new NetherFloorNode(96, 20, 1, NetherFloorNodeType.Boss)
                {
                    NodeId = 90194313218,
                    ApiFloorIndex = 1,
                    IsUnlocked = true,
                },
            },
            TicketCount = 62,
            MapHash = "completed-segment-20",
        };
        cache.ObserveFullSnapshot(stalePresentation);

        NetherRuntimeSnapshotResult result = NetherTransitionSnapshotCompositionPolicy.Compose(
            cache,
            new NetherAuthoritativeTransitionState
            {
                Status = NetherSessionStatus.Play,
                NetherId = 1,
                MapId = 1,
                CurrentFloorId = 0,
                FloorLevel = 20,
                FloorIndex = 1,
                MaxFloorLevel = 130,
                ContinuanceFloorLevel = 20,
                MasterMaxFloorLevel = 130,
                TicketCount = 61,
                CodeCapacity = 28,
                Codes = Array.Empty<NetherCodeState>(),
                AcquiredItems = Array.Empty<NetherRewardItem>(),
            },
            requireFreshBattleCharacters: false,
            purpose: NetherTransitionSnapshotPurpose.ContinueSettlement
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(NetherSessionStatus.Play, result.Snapshot!.Status);
        Assert.Equal(1, result.Snapshot.MapId);
        Assert.Equal(0, result.Snapshot.CurrentFloorId);
        Assert.Equal(0, result.Snapshot.CurrentNodeId);
        Assert.Equal(20, result.Snapshot.FloorLevel);
        Assert.Equal(1, result.Snapshot.FloorIndex);
        Assert.Equal(61, result.Snapshot.TicketCount);
        Assert.Empty(result.Snapshot.Floors);
        Assert.Equal(string.Empty, result.Snapshot.MapHash);
    }

    [Theory]
    [InlineData((int)NetherSessionStatus.Sleep)]
    [InlineData((int)NetherSessionStatus.Battle)]
    public void Continue_composition_rejects_zero_master_floor_outside_play_entry(
        int rawStatus
    )
    {
        var status = (NetherSessionStatus)rawStatus;
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(
            Snapshot(NetherSessionStatus.Sleep, floorId: 96, floorLevel: 20, apiFloorIndex: 1)
        );

        NetherRuntimeSnapshotResult result = NetherTransitionSnapshotCompositionPolicy.Compose(
            cache,
            TransitionState(status, floorId: 0, floorLevel: 20, apiFloorIndex: 1),
            requireFreshBattleCharacters: false,
            purpose: NetherTransitionSnapshotPurpose.ContinueSettlement
        );

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-authoritative-continue-state", result.Detail);
    }

    [Fact]
    public void Continue_composition_rejects_negative_master_floor_in_play()
    {
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(
            Snapshot(NetherSessionStatus.Sleep, floorId: 96, floorLevel: 20, apiFloorIndex: 1)
        );

        NetherRuntimeSnapshotResult result = NetherTransitionSnapshotCompositionPolicy.Compose(
            cache,
            TransitionState(
                NetherSessionStatus.Play,
                floorId: -1,
                floorLevel: 20,
                apiFloorIndex: 1
            ),
            requireFreshBattleCharacters: false,
            purpose: NetherTransitionSnapshotPurpose.ContinueSettlement
        );

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-authoritative-continue-state", result.Detail);
    }

    private static NetherAuthoritativeTransitionState TransitionState(
        NetherSessionStatus status,
        long floorId,
        int floorLevel,
        int apiFloorIndex
    ) => new()
    {
        Status = status,
        NetherId = 1,
        MapId = 1,
        CurrentFloorId = floorId,
        FloorLevel = floorLevel,
        FloorIndex = apiFloorIndex,
        MaxFloorLevel = 130,
        ContinuanceFloorLevel = 10,
        ErosionPoint = 5,
        TicketCount = 13,
        SignalCount = 0,
        TreasureKeyCount = 0,
        NetherGold = 45,
        CodeReloadCount = 1,
        CodeCapacity = 28,
        LockReward = 0,
        Codes = Array.Empty<NetherCodeState>(),
        AcquiredItems = Array.Empty<NetherRewardItem>(),
    };

    private static NetherSnapshot Snapshot(
        NetherSessionStatus status,
        long floorId,
        int floorLevel,
        int apiFloorIndex
    ) => new()
    {
        Status = status,
        NetherId = 1,
        MapId = 1,
        CurrentFloorId = floorId,
        CurrentNodeId = floorId == 30 ? 34359738370 : 38654705666,
        FloorLevel = floorLevel,
        FloorIndex = apiFloorIndex,
        MaxFloorLevel = 130,
        ContinuanceFloorLevel = 10,
        MasterMaxFloorLevel = 130,
        ErosionPoint = 5,
        TicketCount = 13,
        NetherGold = 45,
        CodeReloadCount = 1,
        CodeCapacity = 28,
        Characters = new[]
        {
            new NetherCharacterState(1001, 900, true),
            new NetherCharacterState(1002, 1000, true),
        },
        Codes = Array.Empty<NetherCodeState>(),
        Floors = new[]
        {
            new NetherFloorNode(30, 7, 1, NetherFloorNodeType.Event)
            {
                NodeId = 34359738370,
                ApiFloorIndex = 1,
                IsUnlocked = true,
            },
            new NetherFloorNode(27, 8, 1, NetherFloorNodeType.MiniBoss)
            {
                NodeId = 38654705666,
                ApiFloorIndex = 1,
                IsUnlocked = true,
                PreviousFloorIds = new[] { 34359738370L },
            },
        },
        CharacterHpHash = "1001:900:1|1002:1000:1",
        CodeHash = string.Empty,
        MapHash = "cached-map-1",
    };
}
