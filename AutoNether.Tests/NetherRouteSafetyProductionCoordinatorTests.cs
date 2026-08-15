#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherRouteSafetyProductionCoordinatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void Map_generation_ranges_do_not_change_the_battle_base_cost(int maximumErosion)
    {
        NetherProductionRouteSafetyPlan plan = Plan(
            erosion: 40,
            bounds: new Dictionary<long, NetherFloorMasterBounds>
            {
                [2] = Bounds(2, 0, maximumErosion),
                [3] = Bounds(3, 0, 0),
            }
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).FloorId);
        Assert.Equal(45, plan.BattleProjectionByFloorId[2].ProjectedMaximumErosion);
    }

    [Fact]
    public void Map_generation_erosion_range_does_not_replace_the_battle_base_cost()
    {
        NetherProductionRouteSafetyPlan plan = Plan(
            erosion: 0,
            bounds: new Dictionary<long, NetherFloorMasterBounds>
            {
                [2] = Bounds(2, 0, 100),
                [3] = Bounds(3, 0, 100),
            }
        );

        Assert.True(
            plan.Route.HasSelection,
            plan.Route.PauseReason + ":" + plan.Route.PauseDetail + ":"
                + string.Join("|", plan.Route.Audit.Select(item =>
                    item.FloorId + ":" + item.Reason + ":" + item.Detail + ":"
                        + plan.Context.HorizonRejection(item.FloorId)))
        );
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).FloorId);
        NetherBattleProjectionPayload payload = plan.BattleProjectionByFloorId[2];
        Assert.Equal(5, payload.FloorMinimumErosion);
        Assert.Equal(5, payload.FloorMaximumErosion);
        Assert.Equal(5, payload.ProjectedMinimumErosion);
        Assert.Equal(5, payload.ProjectedMaximumErosion);
    }

    [Fact]
    public void OptionalBattle_ProjectingEightyNineToNinety_IsRejectedByProductionChain()
    {
        NetherProductionRouteSafetyPlan plan = Plan(
            erosion: 89,
            bounds: new Dictionary<long, NetherFloorMasterBounds>
            {
                [2] = Bounds(2, 1, 1),
                [3] = Bounds(3, 0, 0),
            }
        );

        Assert.False(plan.Route.HasSelection);
        Assert.DoesNotContain(plan.BattleProjectionByFloorId.Keys, id => id == 2);
    }

    [Fact]
    public void NecessaryBoss_ProjectingNinetyNineToOneHundred_IsRejectedByProductionChain()
    {
        NetherSnapshot snapshot = Snapshot(
            erosion: 99,
            Floor(1, 1, NetherFloorNodeType.Recovery),
            Floor(2, 2, NetherFloorNodeType.Boss, 1)
        );
        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            Runtime(
                hpPermille: 500,
                bounds: new Dictionary<long, NetherFloorMasterBounds> { [2] = Bounds(2, 1, 1) }
            )
        );

        Assert.False(plan.Route.HasSelection);
    }

    [Fact]
    public void ActivePartyMinimumOfTwoHundredNinetyNine_RejectsOptionalBattle()
    {
        NetherProductionRouteSafetyPlan plan = Plan(
            hpPermille: 299,
            bounds: new Dictionary<long, NetherFloorMasterBounds>
            {
                [2] = Bounds(2, 0, 0),
                [3] = Bounds(3, 0, 0),
            }
        );

        Assert.False(plan.Route.HasSelection);
    }

    [Theory]
    [InlineData(299, false)]
    [InlineData(300, true)]
    public void NecessaryBoss_UsesHpBoundaryThroughTheProductionCoordinator(int hpPermille, bool expectedSelection)
    {
        NetherSnapshot snapshot = SnapshotWithHp(
            erosion: 20,
            hpPermille: hpPermille,
            Floor(1, 1, NetherFloorNodeType.Recovery),
            Floor(2, 2, NetherFloorNodeType.Boss, 1)
        );
        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            Runtime(
                hpPermille: hpPermille,
                bounds: new Dictionary<long, NetherFloorMasterBounds> { [2] = Bounds(2, 0, 1) }
            )
        );

        Assert.Equal(expectedSelection, plan.Route.HasSelection);
        if (expectedSelection)
            Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).FloorId);
    }

    [Fact]
    public void UnknownMasterCodeOrHp_IsNeverPromotedToTheOldPermissiveSafetyMaps()
    {
        NetherProductionRouteSafetyPlan missingMaster = Plan(
            bounds: new Dictionary<long, NetherFloorMasterBounds> { [3] = Bounds(3, 0, 0) }
        );
        NetherProductionRouteSafetyPlan unknownCode = Plan(
            code: new NetherActiveCodeErosionProjection { ErosionProjectionKnown = false, Detail = "unknown" }
        );
        NetherProductionRouteSafetyPlan unknownHp = Plan(
            hp: new NetherActivePartyHpSafety(false, null, "unknown")
        );

        Assert.False(missingMaster.Route.HasSelection);
        Assert.False(unknownCode.Route.HasSelection);
        Assert.False(unknownHp.Route.HasSelection);
        Assert.Contains("bounds:missing-runtime-node", UnknownCandidateDetail(missingMaster));
        Assert.Contains("codes:unknown", UnknownCandidateDetail(unknownCode));
        Assert.Contains("hp:unknown", UnknownCandidateDetail(unknownHp));
    }

    [Fact]
    public void Production_does_not_certify_a_later_combat_from_the_preceding_battles_stale_hp()
    {
        // Fresh GameAssembly 573fa800...1fb / Project.dll 53806a5b...1300:
        // NetherClearBattleResponseEntity.t_nether_characters is the post-battle authority,
        // and each NetherCharacterEntity carries current_hp_ratio.  Until that response has
        // produced a fresh snapshot, the Boss after this Battle has no exact pre-entry HP.
        NetherProductionRouteSafetyPlan plan = Plan(hpPermille: 500);

        Assert.True(
            plan.Route.HasSelection,
            plan.Route.PauseReason + ":" + plan.Route.PauseDetail + ":"
                + string.Join("|", plan.Route.Audit.Select(item =>
                    item.FloorId + ":" + item.Reason + ":" + item.Detail + ":"
                        + plan.Context.HorizonRejection(item.FloorId)))
        );
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).NodeId);
        Assert.Equal(int.MinValue, plan.Context.ProjectedHpDelta(2));
        Assert.DoesNotContain(3, plan.BattleProjectionByFloorId.Keys);
        Assert.False(plan.Context.IsHardSafe(3));
        Assert.Equal("combat-preentry-hp-unavailable:3", plan.Context.HorizonRejection(3));
        Assert.True(plan.Context.RequiresUserPause(3));
    }

    [Fact]
    public void Fresh_post_treasure_partial_party_snapshot_replans_from_current_living_survivors()
    {
        // Fresh Project.dll 53806a5b...1300 exposes exact MCharacterId, HpRatio, and IsAlive on
        // NetherPartyCharacterModel.  A dead roster member remains present but IsAlive=false;
        // the authoritative current-party HP contract is therefore the surviving set only.
        NetherSnapshot snapshot = Snapshot(
            erosion: 40,
            Floor(1, 1, NetherFloorNodeType.Treasure),
            Floor(2, 2, NetherFloorNodeType.Battle, 1),
            Floor(3, 3, NetherFloorNodeType.Boss, 2, previous: new long[] { 2 })
        ) with
        {
            Characters = new[]
            {
                new NetherCharacterState(10, 0, IsActive: false),
                new NetherCharacterState(20, 500, IsActive: true),
            },
        };
        NetherActivePartyHpSafety runtimeHp = new NetherRuntimeActivePartyHpExtractor().Extract(
            new FakeNetherModel(
                new FakePartyModel(
                    new FakePartyCharacter(10, 0d, isAlive: false),
                    new FakePartyCharacter(20, 0.500d, isAlive: true)
                )
            )
        );

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            Runtime(hp: runtimeHp)
        );

        Assert.True(
            plan.Route.HasSelection,
            plan.Route.PauseReason + ":" + plan.Route.PauseDetail
        );
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).NodeId);
        Assert.True(plan.BattleProjectionByFloorId.ContainsKey(2));
    }

    [Fact]
    public void Fresh_post_treasure_snapshot_with_no_living_member_remains_fail_closed()
    {
        NetherSnapshot snapshot = Snapshot(
            erosion: 40,
            Floor(1, 1, NetherFloorNodeType.Treasure),
            Floor(2, 2, NetherFloorNodeType.Battle, 1),
            Floor(3, 3, NetherFloorNodeType.Boss, 2, previous: new long[] { 2 })
        ) with
        {
            Characters = new[] { new NetherCharacterState(10, 0, IsActive: false) },
        };
        NetherActivePartyHpSafety runtimeHp = RuntimeHp(
            new FakePartyCharacter(10, 0d, isAlive: false)
        );

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            Runtime(hp: runtimeHp)
        );

        Assert.False(plan.Route.HasSelection);
    }

    [Fact]
    public void Fresh_post_treasure_survivor_hp_mismatch_remains_fail_closed()
    {
        NetherSnapshot snapshot = Snapshot(
            erosion: 40,
            Floor(1, 1, NetherFloorNodeType.Treasure),
            Floor(2, 2, NetherFloorNodeType.Battle, 1),
            Floor(3, 3, NetherFloorNodeType.Boss, 2, previous: new long[] { 2 })
        ) with
        {
            Characters = new[]
            {
                new NetherCharacterState(10, 0, IsActive: false),
                new NetherCharacterState(20, 500, IsActive: true),
            },
        };
        NetherActivePartyHpSafety runtimeHp = RuntimeHp(
            new FakePartyCharacter(10, 0d, isAlive: false),
            new FakePartyCharacter(20, 0.499d, isAlive: true)
        );

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            Runtime(hp: runtimeHp)
        );

        Assert.False(plan.Route.HasSelection);
    }

    [Fact]
    public void Equal_minimum_hp_from_a_different_living_character_set_remains_fail_closed()
    {
        // Fresh Project.dll 53806a5b...1300 exposes MCharacterId beside HpRatio/IsAlive.
        // Equal aggregate HP cannot authorize a replan when the authoritative living identity
        // changed between the snapshot and runtime capture.
        NetherSnapshot snapshot = Snapshot(
            erosion: 40,
            Floor(1, 1, NetherFloorNodeType.Treasure),
            Floor(2, 2, NetherFloorNodeType.Battle, 1),
            Floor(3, 3, NetherFloorNodeType.Boss, 2, previous: new long[] { 2 })
        ) with
        {
            Characters = new[] { new NetherCharacterState(20, 500, IsActive: true) },
        };
        NetherActivePartyHpSafety runtimeHp = RuntimeHp(
            new FakePartyCharacter(10, 0.500d, isAlive: true)
        );

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            Runtime(hp: runtimeHp)
        );

        Assert.False(plan.Route.HasSelection);
    }

    [Fact]
    public void Native_single_ratio_quantizes_identically_for_snapshot_and_runtime_replan()
    {
        // Fresh Project.dll 53806a5b...1300 declares NetherPartyCharacterModel.HpRatio as
        // System.Single. The server-shaped 0.299f value must remain 299 permille on both capture
        // paths instead of becoming 298 on one path through binary-float flooring.
        NetherSnapshot snapshot = Snapshot(
            erosion: 40,
            Floor(1, 1, NetherFloorNodeType.Treasure),
            Floor(2, 2, NetherFloorNodeType.Battle, 1),
            Floor(3, 3, NetherFloorNodeType.Boss, 2, previous: new long[] { 2 })
        ) with
        {
            Characters = new[] { new NetherCharacterState(20, 299, IsActive: true) },
        };
        NetherActivePartyHpSafety runtimeHp = RuntimeHp(
            new FakeNativeFloatPartyCharacter(20, 0.299f, isAlive: true)
        );

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(minimumCharacterHpPermille: 1),
            Runtime(hp: runtimeHp)
        );

        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).NodeId);
    }

    [Fact]
    public void CurrentNativeOpaqueEffectTwelve_ReachesBattleProjectionInsteadOfMissingMetadata()
    {
        NetherActiveCodeErosionProjection code = new NetherActiveCodeErosionProjectionMapper().Map(
            new[] { new NetherPossessionCodeErosionInput(30026, 1) },
            new[] { new NetherCodeErosionMasterInput(30026, 12, 3, 100, 0) }
        );

        NetherProductionRouteSafetyPlan plan = Plan(code: code);

        Assert.True(code.ErosionProjectionKnown, code.Detail);
        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).FloorId);
        Assert.Equal(code.CodeHash, plan.BattleProjectionByFloorId[2].CodeHash);
        Assert.DoesNotContain(
            plan.Route.Audit,
            candidate => candidate.Detail.Contains("projection-metadata:missing", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void ActiveNativeCategorySkillTypeSeven_MapsReliefAndKeepsProductionRouteSelectable()
    {
        NetherPossessionCodeErosionInput[] possessions = Enumerable.Range(1, 5)
            .Select(id => new NetherPossessionCodeErosionInput(id, 1))
            .ToArray();
        NetherCodeErosionMasterInput[] masters = Enumerable.Range(1, 5)
            .Select(id => new NetherCodeErosionMasterInput(id, 1, 0, 0, 0)
            {
                NetherId = 1,
                Category = 3,
            })
            .ToArray();
        NetherActiveCodeErosionProjection code = new NetherActiveCodeErosionProjectionMapper().Map(
            possessions,
            masters,
            new[]
            {
                new NetherCodeCategoryErosionMasterInput(
                    SkillId: 30000,
                    NetherId: 1,
                    Counter: 5,
                    Category: 3,
                    EffectType: 7,
                    EffectParameter1: 5,
                    EffectParameter2: 0,
                    EffectParameter3: 0
                ),
            },
            activeNetherId: 1
        );

        NetherProductionRouteSafetyPlan plan = Plan(code: code);

        Assert.True(code.ErosionProjectionKnown, code.Detail);
        Assert.True(Assert.Single(code.CategorySkillEntries).IsActive);
        NetherCodeEffect effect = Assert.Single(code.ErosionEffects);
        Assert.Equal(30000, effect.CodeId);
        Assert.Equal(NetherCodeEffectKind.ErosionAdditionDown, effect.EffectKind);
        Assert.Equal(5, effect.Amount);
        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).FloorId);
        Assert.Equal(40, plan.BattleProjectionByFloorId[2].ProjectedMaximumErosion);
    }

    [Fact]
    public void NecessaryBoss_AboveSeventyWithoutConfirmedRecovery_PausesBeforeMutation()
    {
        NetherSnapshot snapshot = Snapshot(
            erosion: 94,
            Floor(1, 1, NetherFloorNodeType.Recovery),
            Floor(2, 2, NetherFloorNodeType.Boss, 1)
        );
        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            Runtime(
                hpPermille: 500,
                bounds: new Dictionary<long, NetherFloorMasterBounds> { [2] = Bounds(2, 1, 1) }
            )
        );

        Assert.False(plan.Route.HasSelection);
        Assert.Equal("erosion-70-without-confirmed-recovery", plan.Context.HorizonRejection(2));
        Assert.True(plan.Context.RequiresUserPause(2));
    }

    [Fact]
    public void MaximumDepthGate_RemainsInTheProductionPlanningChain()
    {
        NetherSnapshot snapshot = Snapshot(
            erosion: 40,
            Floor(1, 1, NetherFloorNodeType.Recovery),
            Floor(2, 3, NetherFloorNodeType.Battle, 1),
            Floor(3, 4, NetherFloorNodeType.Boss, 2, previous: new long[] { 2 })
        );
        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            2,
            Settings(),
            Runtime(
                hpPermille: 500,
                bounds: new Dictionary<long, NetherFloorMasterBounds>
                {
                    [2] = Bounds(2, 0, 0),
                    [3] = Bounds(3, 0, 0),
                }
            )
        );

        Assert.False(plan.Route.HasSelection);
        Assert.Equal(NetherPauseReason.TargetReachedOutsideCheckpoint, plan.Route.PauseReason);
    }

    [Fact]
    public void SelectedBattle_StoresBuilderDerivedProjectionPayloadBeforeNativeFloorAction()
    {
        NetherActiveCodeErosionProjection code = new()
        {
            ErosionProjectionKnown = true,
            CodeHash = "active:60001:6:2",
            ErosionEffects = new[]
            {
                new NetherCodeEffect(60001, NetherCodeEffectKind.ErosionAdditionUp, 2),
            },
        };
        NetherProductionRouteSafetyPlan plan = Plan(
            erosion: 40,
            bounds: new Dictionary<long, NetherFloorMasterBounds>
            {
                [2] = Bounds(2, 5, 10),
                [3] = Bounds(3, 0, 0),
            },
            code: code
        );

        NetherBattleProjectionPayload payload = plan.BattleProjectionByFloorId[2];
        Assert.Equal(2, payload.FloorId);
        Assert.Equal(40, payload.PreBattleErosion);
        Assert.Equal(5, payload.FloorMinimumErosion);
        Assert.Equal(5, payload.FloorMaximumErosion);
        Assert.Equal(47, payload.ProjectedMinimumErosion);
        Assert.Equal(47, payload.ProjectedMaximumErosion);
        Assert.Equal("active:60001:6:2", payload.CodeHash);
        Assert.Equal("route-battle:2:1:40:5:5:active:60001:6:2", payload.ProjectionIdentity);
    }

    [Fact]
    public void Production_safety_maps_are_keyed_by_runtime_node_when_master_id_is_reused()
    {
        NetherFloorNode current = Floor(3, 3, NetherFloorNodeType.Recovery, previous: Array.Empty<long>()) with { NodeId = 100 };
        NetherFloorNode next = Floor(3, 4, NetherFloorNodeType.Battle, previous: new long[] { 100 }) with { NodeId = 200 };
        NetherFloorNode terminal = Floor(9, 5, NetherFloorNodeType.Boss, previous: new long[] { 200 }) with { NodeId = 300 };
        NetherSnapshot snapshot = Snapshot(40, current, next, terminal) with
        {
            CurrentFloorId = 3,
            CurrentNodeId = 100,
        };

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            Runtime(bounds: new Dictionary<long, NetherFloorMasterBounds>
            {
                [200] = Bounds(3, 0, 0),
                [300] = Bounds(9, 0, 0),
            })
        );

        Assert.True(
            plan.Route.HasSelection,
            plan.Route.PauseReason + ":" + plan.Route.PauseDetail + ":"
                + string.Join("|", plan.Route.Audit.Select(item => item.FloorId + ":" + item.Reason))
        );
        NetherFloorNode selected = Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode);
        Assert.Equal(3, selected.FloorId);
        Assert.Equal(200, selected.NodeId);
        Assert.True(plan.BattleProjectionByFloorId.ContainsKey(200));
        Assert.False(plan.BattleProjectionByFloorId.ContainsKey(3));
    }

    private static NetherProductionRouteSafetyPlan Plan(
        int erosion = 40,
        int hpPermille = 500,
        NetherActivePartyHpSafety? hp = null,
        IReadOnlyDictionary<long, NetherFloorMasterBounds>? bounds = null,
        NetherActiveCodeErosionProjection? code = null
    ) => new NetherRouteSafetyProductionCoordinator().Plan(
        SnapshotWithHp(
            erosion,
            hpPermille,
            Floor(1, 1, NetherFloorNodeType.Recovery),
            Floor(2, 2, NetherFloorNodeType.Battle, 1),
            Floor(3, 3, NetherFloorNodeType.Boss, 2, previous: new long[] { 2 })
        ),
        130,
        Settings(),
        Runtime(hpPermille, hp, bounds, code)
    );

    private static string UnknownCandidateDetail(NetherProductionRouteSafetyPlan plan) =>
        Assert.Single(plan.Route.Audit, audit => audit.Reason == "unknown-node").Detail;

    private static NetherRuntimeRouteSafetyData Runtime(
        int hpPermille = 500,
        NetherActivePartyHpSafety? hp = null,
        IReadOnlyDictionary<long, NetherFloorMasterBounds>? bounds = null,
        NetherActiveCodeErosionProjection? code = null
    ) => new()
    {
        FloorBoundsByFloorId = bounds ?? new Dictionary<long, NetherFloorMasterBounds>
        {
            [2] = Bounds(2, 0, 0),
            [3] = Bounds(3, 0, 0),
        },
        ActivePartyHp = hp ?? NetherRouteSafetyHpTestEvidence.Single(1, hpPermille),
        ActiveCodeErosion = code ?? new NetherActiveCodeErosionProjection
        {
            ErosionProjectionKnown = true,
            CodeHash = "nether-codes:none",
            ErosionEffects = Array.Empty<NetherCodeEffect>(),
        },
    };

    private static NetherFloorMasterBounds Bounds(long floorId, int min, int max) =>
        new(floorId, min, max, IsKnown: true, Detail: string.Empty);

    private static NetherAutoClimbSettings Settings(
        int minimumCharacterHpPermille = 300
    ) => new()
    {
        MaxDepth = 130,
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = minimumCharacterHpPermille,
    };

    private static NetherSnapshot Snapshot(int erosion, params NetherFloorNode[] floors) =>
        SnapshotWithHp(erosion, hpPermille: 500, floors);

    private static NetherSnapshot SnapshotWithHp(
        int erosion,
        int hpPermille,
        params NetherFloorNode[] floors
    ) => new()
    {
        Status = NetherSessionStatus.Play,
        MapId = 1,
        CurrentFloorId = 1,
        ErosionPoint = erosion,
        Floors = floors,
        Characters = new[] { new NetherCharacterState(1, hpPermille, IsActive: true) },
    };

    private static NetherFloorNode Floor(
        long id,
        int level,
        NetherFloorNodeType type,
        int index = 0,
        long[]? previous = null
    ) => new(id, level, index, type)
    {
        IsUnlocked = true,
        PreviousFloorIds = previous ?? (id == 1 ? Array.Empty<long>() : new[] { 1L }),
    };

    private static NetherActivePartyHpSafety RuntimeHp(params object[] characters) =>
        new NetherRuntimeActivePartyHpExtractor().Extract(
            new FakeNetherModel(new FakePartyModel(characters))
        );

    private sealed class FakeNetherModel
    {
        public FakeNetherModel(FakePartyModel partyModel) => PartyModel = partyModel;

        public FakePartyModel PartyModel { get; }
    }

    private sealed class FakePartyModel
    {
        public FakePartyModel(params object[] characterModels) => CharacterModels = characterModels;

        public object[] CharacterModels { get; }
    }

    private sealed class FakePartyCharacter
    {
        public FakePartyCharacter(long characterId, double hpRatio, bool isAlive)
        {
            MCharacterId = characterId;
            HpRatio = hpRatio;
            IsAlive = isAlive;
        }

        public long MCharacterId { get; }
        public double HpRatio { get; }
        public bool IsAlive { get; }
    }

    private sealed class FakeNativeFloatPartyCharacter
    {
        public FakeNativeFloatPartyCharacter(long characterId, float hpRatio, bool isAlive)
        {
            MCharacterId = characterId;
            HpRatio = hpRatio;
            IsAlive = isAlive;
        }

        public long MCharacterId { get; }
        public float HpRatio { get; }
        public bool IsAlive { get; }
    }
}
