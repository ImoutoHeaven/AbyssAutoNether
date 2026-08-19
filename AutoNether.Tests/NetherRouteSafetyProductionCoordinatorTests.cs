#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherRouteSafetyProductionCoordinatorTests
{
    [Fact]
    public void Production_route_plan_carries_only_exact_route_owned_procurement_proof()
    {
        var key = new NetherInteractiveEventOptionKey(8801, 8802, 1);
        var budget = new NetherEventProcurementBudget(190, 2);
        NetherSnapshot snapshot = SnapshotWithHp(
            erosion: 40,
            hpPermille: 500,
            Floor(1, 1, NetherFloorNodeType.Recovery),
            Floor(2, 2, NetherFloorNodeType.Battle, 1)
        );
        NetherRuntimeRouteSafetyData runtime = Runtime(snapshot) with
        {
            EventProcurementCommitments = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
            {
                [key] = budget,
            },
        };

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            runtime
        );

        Assert.Equal(budget, plan.EventProcurementCommitments[key]);
    }

    [Fact]
    public void Production_route_plan_generates_gold_and_key_minima_from_the_same_safe_branch()
    {
        NetherFloorNode[] floors =
        [
            Floor(1, 1, NetherFloorNodeType.Recovery, previous: Array.Empty<long>()),
            Floor(2, 2, NetherFloorNodeType.Event, previous: new[] { 1L }),
            Floor(3, 3, NetherFloorNodeType.Shop, previous: new[] { 2L }),
            Floor(4, 4, NetherFloorNodeType.Treasure, previous: new[] { 3L }),
            Floor(5, 5, NetherFloorNodeType.Boss, previous: new[] { 4L }),
        ];
        NetherSnapshot snapshot = SnapshotWithHp(
            erosion: 20,
            hpPermille: 500,
            floors
        ) with
        {
            CurrentNodeId = 1,
            NetherGold = 0,
            TreasureKeyCount = 0,
        };
        NetherAutoClimbSettings settings = Settings();
        NetherInteractiveEventOptionKey goldKey = new(100, 1001, 1);
        NetherInteractiveEventOptionKey keyKey = new(100, 1002, 2);
        NetherRuntimeRouteSafetyData runtime = Runtime(
            snapshot,
            hpPermille: 500,
            bounds: new Dictionary<long, NetherFloorMasterBounds>
            {
                [5] = Bounds(5, 0, 0),
            }
        ) with
        {
            VisibleMap = new NetherStrategyVisibleMapEvidence(
                floors,
                [
                    VisibleEvent(2, 100, 1001, 1, NetherEffectKind.NetherGoldGain, amount: 200),
                    VisibleEvent(2, 100, 1002, 2, NetherEffectKind.TreasureKeyGain, amount: 1),
                    new NetherStrategyVisibleContentRow(
                        NetherStrategyVisibleContentKind.ShopInventory,
                        3,
                        3001,
                        3001
                    )
                    {
                        IsKnown = true,
                        ContentType = 166,
                        Cost = 200,
                        Amount = 1,
                        UsesNetherGold = true,
                    },
                    new NetherStrategyVisibleContentRow(
                        NetherStrategyVisibleContentKind.Treasure,
                        4,
                        4001,
                        401
                    )
                    {
                        IsKnown = true,
                        EventId = 401,
                        EventPartId = 4011,
                    },
                    new NetherStrategyVisibleContentRow(
                        NetherStrategyVisibleContentKind.Item,
                        4,
                        4011,
                        4011
                    )
                    {
                        IsKnown = true,
                        EventId = 401,
                        EventPartId = 4011,
                        ItemType = 91,
                        ItemRarity = 5,
                        Amount = 1,
                    },
                ]
            ),
        };
        NetherStrategyTypedSemanticProviderEvidence provider = ProcurementProvider();
        runtime = runtime with
        {
            VisibleMap = BindProviderBackedCanonicalEvidence(runtime.VisibleMap, provider),
        };

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            settings,
            runtime,
            BranchInteractive(snapshot, settings, goldKey, keyKey, provider)
        );

        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).NodeId);
        Assert.Equal(new NetherEventProcurementBudget(200, 0), plan.EventProcurementCommitments[goldKey]);
        Assert.Equal(new NetherEventProcurementBudget(0, 1), plan.EventProcurementCommitments[keyKey]);
        Assert.True(
            plan.RankFiveKeyProcurement.HasMandatoryObjective,
            $"{plan.RankFiveKeyProcurement.Detail}|source={plan.RankFiveKeyProcurement.SourceKind}|path={string.Join(",", plan.Route.SelectedPathNodeIds ?? Array.Empty<long>())}|hardSafe={string.Join(",", plan.Context.HardSafeByFloorId.Where(pair => pair.Value).Select(pair => pair.Key))}"
        );
        Assert.Equal(4, plan.RankFiveKeyProcurement.Objective.ObjectiveNodeId);
    }

    [Fact]
    public void Production_route_plan_rejects_unsupported_shop_costs_option_locally()
    {
        NetherProductionRouteSafetyPlan plan = PlanSameBranchProcurement(shopCost: 150, itemType: 91);

        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.DoesNotContain(
            new NetherInteractiveEventOptionKey(100, 1001, 1),
            plan.EventProcurementCommitments.Keys
        );
        Assert.Contains(
            new NetherInteractiveEventOptionKey(100, 1002, 2),
            plan.EventProcurementCommitments.Keys
        );
    }

    [Fact]
    public void Production_route_plan_rejects_non_equipment_rank_five_treasure_for_key_budget()
    {
        NetherProductionRouteSafetyPlan plan = PlanSameBranchProcurement(shopCost: 200, itemType: 92);

        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.DoesNotContain(
            new NetherInteractiveEventOptionKey(100, 1002, 2),
            plan.EventProcurementCommitments.Keys
        );
        Assert.Contains(
            new NetherInteractiveEventOptionKey(100, 1001, 1),
            plan.EventProcurementCommitments.Keys
        );
    }

    [Fact]
    public void Production_route_plan_rejects_procurement_proof_from_a_safe_alternate_branch()
    {
        NetherFloorNode[] floors =
        [
            Floor(1, 1, NetherFloorNodeType.Recovery, previous: Array.Empty<long>()),
            Floor(2, 2, NetherFloorNodeType.Event, previous: new[] { 1L }),
            Floor(3, 3, NetherFloorNodeType.Shop, previous: new[] { 2L }),
            Floor(4, 4, NetherFloorNodeType.Treasure, previous: new[] { 3L }),
            Floor(5, 5, NetherFloorNodeType.Boss, previous: new[] { 4L }),
            Floor(6, 6, NetherFloorNodeType.Shop, previous: new[] { 2L }),
            Floor(7, 7, NetherFloorNodeType.Treasure, previous: new[] { 6L }),
            Floor(8, 8, NetherFloorNodeType.Boss, previous: new[] { 7L }),
        ];
        NetherSnapshot snapshot = SnapshotWithHp(20, 500, floors) with
        {
            CurrentNodeId = 1,
            NetherGold = 0,
            TreasureKeyCount = 0,
        };
        NetherAutoClimbSettings settings = Settings();
        NetherInteractiveEventOptionKey selectedKey = new(100, 1001, 1);
        NetherInteractiveEventOptionKey alternateKey = new(900, 9001, 1);
        NetherRuntimeInteractivePreEntryInputsResult baseCapture = BranchInteractive(
            snapshot,
            settings,
            selectedKey,
            selectedKey
        );
        NetherInteractiveOptionProjection alternateProjection = new(
            alternateKey.OptionNumber,
            ErosionDelta: 0,
            HpDelta: 0,
            ExpectedEffects: [new NetherEffect(NetherEffectKind.NetherGoldGain, 200)]
        )
        {
            EventId = alternateKey.EventId,
            EventPartId = alternateKey.EventPartId,
            FloorId = 6,
            NodeId = 6,
            IsKnown = true,
            HasRouteSafetyEvidence = true,
            RouteSafetyAllowed = true,
            HasCommittedProcurementEvidence = true,
            CommittedGoldMinimum = 200,
        };
        NetherRuntimeInteractivePreEntryCaptureResult alternateCapture = baseCapture.ByFloorNodeId[6] with
        {
            Safety = NetherInteractiveFloorPreEntrySafetyResult.Safe(
                optionProjections: new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                {
                    [alternateKey] = alternateProjection,
                }
            ),
        };
        var entries = baseCapture.ByFloorNodeId.ToDictionary(pair => pair.Key, pair => pair.Value);
        entries[6] = alternateCapture;
        NetherRuntimeInteractivePreEntryInputsResult alternateBranchCapture =
            NetherRuntimeInteractivePreEntryInputsResult.Success(entries, snapshot.Fingerprint);

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            settings,
            Runtime(
                snapshot,
                hpPermille: 500,
                bounds: new Dictionary<long, NetherFloorMasterBounds>
                {
                    [5] = Bounds(5, 0, 0),
                    [8] = Bounds(8, 0, 0),
                }
            ),
            alternateBranchCapture
        );

        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).NodeId);
        Assert.DoesNotContain(alternateKey, plan.EventProcurementCommitments.Keys);
    }

    [Fact]
    public void Production_route_plan_does_not_reuse_procurement_proof_after_authoritative_snapshot_changes()
    {
        NetherSnapshot before = SnapshotWithHp(
            erosion: 20,
            hpPermille: 500,
            Floor(1, 1, NetherFloorNodeType.Recovery),
            Floor(2, 2, NetherFloorNodeType.Battle, 1),
            Floor(3, 3, NetherFloorNodeType.Boss, 2, previous: new long[] { 2 })
        );
        NetherSnapshot after = before with
        {
            MapId = 2,
            MapHash = "authoritative-post-event-map",
            CurrentNodeId = 2,
            CurrentFloorId = 2,
        };
        NetherInteractiveEventOptionKey staleKey = new(700, 7001, 1);
        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            after,
            130,
            Settings(),
            Runtime(after) with
            {
                EventProcurementCommitments = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
                {
                    [staleKey] = new NetherEventProcurementBudget(200, 1),
                },
                RouteIdentity = new NetherRouteBranchIdentity(
                    before.Fingerprint,
                    before.CurrentNodeId,
                    2,
                    "1>2>3"
                ),
            }
        );

        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.DoesNotContain(staleKey, plan.EventProcurementCommitments.Keys);
    }

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
                snapshot,
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
                snapshot,
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
            Runtime(snapshot, hp: runtimeHp)
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
            Runtime(snapshot, hp: runtimeHp)
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
            Runtime(snapshot, hp: runtimeHp)
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
            Runtime(snapshot, hp: runtimeHp)
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
            Runtime(snapshot, hp: runtimeHp)
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
                snapshot,
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
                snapshot,
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
            Runtime(snapshot, bounds: new Dictionary<long, NetherFloorMasterBounds>
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

    [Fact]
    public void Production_route_plan_projects_each_recovery_option_through_the_selected_visible_suffix()
    {
        // Fresh native decomp d: MNetherFloorEventParts exposes the exact numeric option effects,
        // while the native Recovery popup only submits the selected option callback. The route
        // owner must therefore simulate each option through the same visible suffix before binding
        // a proof; a local RouteSafetyAllowed bit is not a branch proof.
        NetherFloorNode[] floors =
        [
            Floor(1, 1, NetherFloorNodeType.Event, previous: Array.Empty<long>()),
            Floor(2, 2, NetherFloorNodeType.Recovery, previous: new[] { 1L }),
            Floor(3, 3, NetherFloorNodeType.Boss, previous: new[] { 2L }),
        ];
        NetherSnapshot snapshot = SnapshotWithHp(68, 500, floors) with
        {
            CurrentNodeId = 1,
            TreasureKeyCount = 1,
        };
        NetherAutoClimbSettings settings = Settings();
        NetherInteractiveOptionProjection eventProjection = Projection(
            eventId: 100,
            partId: 1001,
            optionNumber: 1,
            floorId: 1,
            nodeId: 1,
            erosionDelta: 0,
            effects: [new NetherEffect(NetherEffectKind.Heal, 1)]
        );
        NetherInteractiveOptionProjection rest = Projection(
            eventId: 200,
            partId: 2001,
            optionNumber: 1,
            floorId: 2,
            nodeId: 2,
            erosionDelta: 0,
            effects: [new NetherEffect(NetherEffectKind.Heal, 200)]
        );
        NetherInteractiveOptionProjection purification = Projection(
            eventId: 200,
            partId: 2002,
            optionNumber: 2,
            floorId: 2,
            nodeId: 2,
            erosionDelta: -20,
            effects: [new NetherEffect(NetherEffectKind.ErosionHeal, 20)]
        );
        NetherInteractiveOptionProjection transform = Projection(
            eventId: 200,
            partId: 2003,
            optionNumber: 3,
            floorId: 2,
            nodeId: 2,
            erosionDelta: 0,
            effects: [new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0)]
        );
        NetherRuntimeInteractivePreEntryInputsResult capture =
            InteractiveCapture(
                snapshot,
                settings,
                new Dictionary<long, NetherInteractiveFloorPreEntryCaptureSpec>
                {
                    [1] = new(
                        NetherFloorNodeType.Event,
                        NetherInteractiveFloorPreEntrySafetyResult.Safe(
                            new Dictionary<long, int> { [100] = 1 },
                            new Dictionary<long, NetherInteractiveOptionProjection> { [100] = eventProjection },
                            new NetherInteractiveWorstCaseProjection(0, 0),
                            new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                            {
                                [new NetherInteractiveEventOptionKey(100, 1001, 1)] = eventProjection,
                            }
                        )
                    ),
                    [2] = new(
                        NetherFloorNodeType.Recovery,
                        NetherInteractiveFloorPreEntrySafetyResult.Safe(
                            new Dictionary<long, int> { [200] = 2 },
                            new Dictionary<long, NetherInteractiveOptionProjection> { [200] = purification },
                            new NetherInteractiveWorstCaseProjection(-20, 0),
                            new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                            {
                                [new NetherInteractiveEventOptionKey(200, 2001, 1)] = rest,
                                [new NetherInteractiveEventOptionKey(200, 2002, 2)] = purification,
                                [new NetherInteractiveEventOptionKey(200, 2003, 3)] = transform,
                            }
                        )
                    ),
                }
            );

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            settings,
            Runtime(
                snapshot,
                hpPermille: 500,
                bounds: new Dictionary<long, NetherFloorMasterBounds> { [3] = Bounds(3, 0, 0) }
            ),
            capture
        );

        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).NodeId);
        NetherRecoveryBranchSafetyEvidence restProof = plan.RecoveryBranchSafetyByPartId[2001];
        NetherRecoveryBranchSafetyEvidence purificationProof = plan.RecoveryBranchSafetyByPartId[2002];
        Assert.True(restProof.IsKnown);
        Assert.True(restProof.IsCompleteVisibleBranch);
        Assert.False(restProof.IsNextVisibleBranchSafe);
        Assert.Contains("70", restProof.UnknownReason);
        Assert.True(purificationProof.IsKnown);
        Assert.True(purificationProof.IsCompleteVisibleBranch);
        Assert.True(purificationProof.IsNextVisibleBranchSafe);
    }

    [Fact]
    public void Production_final_recapture_preserves_carried_sibling_recovery_proofs()
    {
        // Fresh native decomp e: the Recovery controller binds one selected callback, so its
        // final capture may expose the selected projection while sibling rows are locally unknown.
        // The route-owned branch proof must remain authoritative across that recapture; otherwise
        // the controller would reject its own unchanged selected-horizon evidence.
        NetherFloorNode[] floors =
        [
            Floor(1, 1, NetherFloorNodeType.Event, previous: Array.Empty<long>()),
            Floor(2, 2, NetherFloorNodeType.Recovery, previous: new[] { 1L }),
            Floor(3, 3, NetherFloorNodeType.Boss, previous: new[] { 2L }),
        ];
        NetherSnapshot snapshot = SnapshotWithHp(68, 500, floors) with
        {
            CurrentNodeId = 1,
            TreasureKeyCount = 1,
        };
        NetherAutoClimbSettings settings = Settings();
        NetherInteractiveOptionProjection eventProjection = Projection(
            100, 1001, 1, 1, 1, 0, [new NetherEffect(NetherEffectKind.Heal, 1)]
        );
        NetherInteractiveOptionProjection rest = Projection(
            200, 2001, 1, 2, 2, 0, [new NetherEffect(NetherEffectKind.Heal, 200)]
        );
        NetherInteractiveOptionProjection purification = Projection(
            200, 2002, 2, 2, 2, -20, [new NetherEffect(NetherEffectKind.ErosionHeal, 20)]
        );
        NetherInteractiveOptionProjection transform = Projection(
            200, 2003, 3, 2, 2, 0, [new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0)]
        );
        NetherRuntimeInteractivePreEntryInputsResult initialCapture = InteractiveCapture(
            snapshot,
            settings,
            new Dictionary<long, NetherInteractiveFloorPreEntryCaptureSpec>
            {
                [1] = new(
                    NetherFloorNodeType.Event,
                    NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [100] = 1 },
                        new Dictionary<long, NetherInteractiveOptionProjection> { [100] = eventProjection },
                        new NetherInteractiveWorstCaseProjection(0, 0),
                        new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                        {
                            [new NetherInteractiveEventOptionKey(100, 1001, 1)] = eventProjection,
                        }
                    )
                ),
                [2] = new(
                    NetherFloorNodeType.Recovery,
                    NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [200] = 2 },
                        new Dictionary<long, NetherInteractiveOptionProjection> { [200] = purification },
                        new NetherInteractiveWorstCaseProjection(-20, 0),
                        new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                        {
                            [new NetherInteractiveEventOptionKey(200, 2001, 1)] = rest,
                            [new NetherInteractiveEventOptionKey(200, 2002, 2)] = purification,
                            [new NetherInteractiveEventOptionKey(200, 2003, 3)] = transform,
                        }
                    )
                ),
            }
        );
        NetherRuntimeRouteSafetyData runtime = Runtime(
            snapshot,
            hpPermille: 500,
            bounds: new Dictionary<long, NetherFloorMasterBounds> { [3] = Bounds(3, 0, 0) }
        );
        NetherRouteSafetyProductionCoordinator coordinator = new();
        NetherProductionRouteSafetyPlan initialPlan = coordinator.Plan(
            snapshot,
            130,
            settings,
            runtime,
            initialCapture
        );

        NetherInteractiveOptionProjection UnknownSibling(NetherInteractiveOptionProjection option) => option with
        {
            IsKnown = false,
            HasRouteSafetyEvidence = false,
            RouteSafetyAllowed = false,
            UnknownReason = "recovery-option-not-selected-by-complete-branch-proof",
            RouteSafetyUnknownReason = "recovery-option-not-selected-by-complete-branch-proof",
        };
        NetherInteractiveFloorPreEntrySafetyResult finalRecoverySafety =
            NetherInteractiveFloorPreEntrySafetyResult.Safe(
                new Dictionary<long, int> { [200] = 2 },
                new Dictionary<long, NetherInteractiveOptionProjection> { [200] = purification },
                new NetherInteractiveWorstCaseProjection(-20, 0),
                new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                {
                    [new NetherInteractiveEventOptionKey(200, 2001, 1)] = UnknownSibling(rest),
                    [new NetherInteractiveEventOptionKey(200, 2002, 2)] = purification,
                    [new NetherInteractiveEventOptionKey(200, 2003, 3)] = UnknownSibling(transform),
                }
            );
        var finalEntries = initialCapture.ByFloorNodeId.ToDictionary(
            pair => pair.Key,
            pair => pair.Value with
            {
                Input = pair.Value.Input! with
                {
                    RecoveryBranchSafetyByPartId = new Dictionary<long, NetherRecoveryBranchSafetyEvidence>(
                        initialPlan.RecoveryBranchSafetyByPartId
                    ),
                    RequireCompleteRecoveryBranchSafety = true,
                },
                Safety = pair.Key == 2 ? finalRecoverySafety : pair.Value.Safety,
            }
        );
        NetherProductionRouteSafetyPlan finalPlan = coordinator.Plan(
            snapshot,
            130,
            settings,
            runtime,
            initialCapture with
            {
                ByFloorNodeId = finalEntries,
            }
        );

        Assert.True(finalPlan.Route.HasSelection, finalPlan.Route.PauseReason + ":" + finalPlan.Route.PauseDetail);
        Assert.True(finalPlan.RecoveryBranchSafetyByPartId[2001].IsKnown);
        Assert.False(finalPlan.RecoveryBranchSafetyByPartId[2001].IsNextVisibleBranchSafe);
        Assert.True(finalPlan.RecoveryBranchSafetyByPartId[2002].IsKnown);
        Assert.True(finalPlan.RecoveryBranchSafetyByPartId[2002].IsNextVisibleBranchSafe);
    }

    [Fact]
    public void Production_event_erosion_requires_complete_visible_recovery_before_the_terminal_boss()
    {
        // Fresh native decomp d: Event execution is bound to the exact visible option, while the
        // route gate must evaluate the first later battle entry, not a terminal-node snapshot.
        // This route reaches 80 at the erosion-paid Event, recovers to 60, then enters the Boss.
        NetherFloorNode[] floors =
        [
            Floor(1, 1, NetherFloorNodeType.Recovery, previous: Array.Empty<long>()),
            Floor(2, 2, NetherFloorNodeType.Event, previous: new[] { 1L }),
            Floor(3, 3, NetherFloorNodeType.Recovery, previous: new[] { 2L }),
            Floor(4, 4, NetherFloorNodeType.Shop, previous: new[] { 3L }),
            Floor(5, 5, NetherFloorNodeType.Treasure, previous: new[] { 4L }),
            Floor(6, 6, NetherFloorNodeType.Boss, previous: new[] { 5L }),
        ];
        NetherSnapshot snapshot = SnapshotWithHp(0, 500, floors) with
        {
            CurrentNodeId = 1,
            NetherGold = 0,
            TreasureKeyCount = 0,
        };
        NetherAutoClimbSettings settings = Settings();
        NetherInteractiveOptionProjection rootRecovery = Projection(
            101, 1011, 1, 1, 1, 0, [new NetherEffect(NetherEffectKind.Heal, 1)]
        );
        NetherInteractiveOptionProjection erosionKey = Projection(
            102, 1021, 1, 2, 2, 80,
            [
                new NetherEffect(NetherEffectKind.Erosion, 80),
                new NetherEffect(NetherEffectKind.TreasureKeyGain, 1),
            ]
        );
        NetherInteractiveOptionProjection recovery = Projection(
            103, 1031, 1, 3, 3, -20,
            [new NetherEffect(NetherEffectKind.ErosionHeal, 20)]
        );
        NetherInteractiveOptionProjection treasure = Projection(
            105, 1051, 1, 5, 5, 0,
            [new NetherEffect(NetherEffectKind.TreasureKeyGain, 1)]
        );
        NetherStrategyTypedSemanticProviderEvidence provider = CanonicalRewardProvider(5011);
        NetherRuntimeInteractivePreEntryInputsResult capture = InteractiveCapture(
            snapshot,
            settings,
            new Dictionary<long, NetherInteractiveFloorPreEntryCaptureSpec>
            {
                [1] = RecoveryCapture(rootRecovery),
                [2] = EventCapture(erosionKey),
                [3] = RecoveryCapture(recovery),
                [4] = new(
                    NetherFloorNodeType.Shop,
                    NetherInteractiveFloorPreEntrySafetyResult.SafeNeutral()
                ),
                [5] = new(
                    NetherFloorNodeType.Treasure,
                    NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [105] = 1 },
                        new Dictionary<long, NetherInteractiveOptionProjection> { [105] = treasure },
                        new NetherInteractiveWorstCaseProjection(0, 0),
                        new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                        {
                            [new NetherInteractiveEventOptionKey(105, 1051, 1)] = treasure,
                        }
                    )
                ),
            },
            typedSemanticProvider: provider
        );
        NetherStrategyVisibleMapEvidence visibleMap = new(
            floors,
            [
                VisibleErosionKeyEvent(2, 102, 1021, 1),
                new NetherStrategyVisibleContentRow(
                    NetherStrategyVisibleContentKind.Treasure,
                    5,
                    5001,
                    500
                )
                {
                    IsKnown = true,
                    EventId = 501,
                    EventPartId = 5011,
                },
                new NetherStrategyVisibleContentRow(
                    NetherStrategyVisibleContentKind.Item,
                    5,
                    5011,
                    5011
                )
                {
                    IsKnown = true,
                    EventId = 501,
                    EventPartId = 5011,
                    ItemType = 91,
                    ItemRarity = 5,
                    Amount = 1,
                },
            ]
        );
        visibleMap = BindProviderBackedCanonicalEvidence(visibleMap, provider);

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            settings,
            Runtime(
                snapshot,
                hpPermille: 500,
                bounds: new Dictionary<long, NetherFloorMasterBounds> { [6] = Bounds(6, 0, 0) }
            ) with
            {
                VisibleMap = visibleMap,
            },
            capture
        );

        Assert.True(plan.Route.HasSelection, plan.Route.PauseReason + ":" + plan.Route.PauseDetail);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(plan.Route.SelectedNode).NodeId);
        Assert.Equal(NetherKeyProcurementSourceKind.ErosionPaidEventKey, plan.RankFiveKeyProcurement.SourceKind);
        Assert.True(plan.RankFiveKeyProcurement.ErosionAmountIsExactEighty);
        Assert.Equal(5, plan.RankFiveKeyProcurement.Objective.ObjectiveNodeId);
        NetherRouteHorizonSafetyEvaluation eventHorizon = plan.Context.HorizonEvaluationByFloorId[2];
        Assert.True(eventHorizon.IsEligible, eventHorizon.RejectionDetail);
        Assert.Contains(
            eventHorizon.HorizonSteps,
            step => step.NodeId == 3 && step.NodeType == NetherFloorNodeType.Recovery
        );
        Assert.Contains(
            plan.Route.SelectedPathNodeIds,
            nodeId => nodeId == 2
        );
        Assert.Contains(
            plan.Route.SelectedPathNodeIds,
            nodeId => nodeId == 3
        );
    }

    [Fact]
    public void Production_route_rejects_treasure_damage_twenty_even_when_partial_death_is_locally_proved()
    {
        // Fresh native decomp d: Treasure/Event popup controllers resolve exact EventPart IDs and
        // execute the selected callback; the mapper may grant group survival only for native 40/80
        // damage payments. Damage 20 must remain ordinary all-living survival.
        NetherFloorNode[] floors =
        [
            Floor(1, 1, NetherFloorNodeType.Shop, previous: Array.Empty<long>()),
            Floor(2, 2, NetherFloorNodeType.Treasure, previous: new[] { 1L }),
            Floor(3, 3, NetherFloorNodeType.Boss, previous: new[] { 2L }),
        ];
        NetherSnapshot snapshot = SnapshotWithHp(20, 10, floors) with
        {
            CurrentNodeId = 1,
            Characters =
            [
                new NetherCharacterState(1, 10, IsActive: true),
                new NetherCharacterState(2, 500, IsActive: true),
            ],
        };
        NetherAutoClimbSettings settings = Settings(minimumCharacterHpPermille: 1);
        NetherInteractiveOptionProjection damageTwenty = Projection(
            202, 2021, 1, 2, 2, 0,
            [new NetherEffect(NetherEffectKind.Damage, 20)]
        ) with
        {
            AllowsPartialActiveDeaths = true,
        };
        NetherRuntimeInteractivePreEntryInputsResult capture = InteractiveCapture(
            snapshot,
            settings,
            new Dictionary<long, NetherInteractiveFloorPreEntryCaptureSpec>
            {
                [1] = new(
                    NetherFloorNodeType.Shop,
                    NetherInteractiveFloorPreEntrySafetyResult.SafeNeutral()
                ),
                [2] = new(
                    NetherFloorNodeType.Treasure,
                    NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [202] = 1 },
                        new Dictionary<long, NetherInteractiveOptionProjection> { [202] = damageTwenty },
                        new NetherInteractiveWorstCaseProjection(0, -20),
                        new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                        {
                            [new NetherInteractiveEventOptionKey(202, 2021, 1)] = damageTwenty,
                        }
                    )
                ),
            }
        );

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            settings,
            Runtime(
                snapshot,
                bounds: new Dictionary<long, NetherFloorMasterBounds> { [3] = Bounds(3, 0, 0) },
                hp: NetherRouteSafetyHpTestEvidence.FromStates(snapshot.Characters)
            ),
            capture
        );

        Assert.False(plan.Route.HasSelection);
        Assert.Contains("ordinary-hp-cost-lethal", plan.Context.HorizonRejection(2));
    }

    private sealed record NetherInteractiveFloorPreEntryCaptureSpec(
        NetherFloorNodeType FloorKind,
        NetherInteractiveFloorPreEntrySafetyResult Safety
    );

    private static NetherInteractiveOptionProjection Projection(
        long eventId,
        long partId,
        int optionNumber,
        long floorId,
        long nodeId,
        int erosionDelta,
        IReadOnlyList<NetherEffect> effects
    ) => new(optionNumber, erosionDelta, effects.Sum(effect => effect.Kind switch
    {
        NetherEffectKind.Heal => effect.Amount,
        NetherEffectKind.Damage => -effect.Amount,
        _ => 0,
    }), effects)
    {
        EventId = eventId,
        EventPartId = partId,
        FloorId = floorId,
        NodeId = nodeId,
        IsKnown = true,
        HasRouteSafetyEvidence = true,
        RouteSafetyAllowed = true,
    };

    private static NetherInteractiveFloorPreEntryCaptureSpec RecoveryCapture(
        NetherInteractiveOptionProjection projection
    ) => new(
        NetherFloorNodeType.Recovery,
        NetherInteractiveFloorPreEntrySafetyResult.Safe(
            new Dictionary<long, int> { [projection.EventId] = projection.OptionNumber },
            new Dictionary<long, NetherInteractiveOptionProjection> { [projection.EventId] = projection },
            new NetherInteractiveWorstCaseProjection(projection.ErosionDelta, projection.HpDelta),
            new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
            {
                [new NetherInteractiveEventOptionKey(
                    projection.EventId,
                    projection.EventPartId,
                    projection.OptionNumber
                )] = projection,
            }
        )
    );

    private static NetherInteractiveFloorPreEntryCaptureSpec EventCapture(
        NetherInteractiveOptionProjection projection
    ) => RecoveryCapture(projection) with { FloorKind = NetherFloorNodeType.Event };

    private static NetherRuntimeInteractivePreEntryInputsResult InteractiveCapture(
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings,
        IReadOnlyDictionary<long, NetherInteractiveFloorPreEntryCaptureSpec> specs,
        NetherStrategyTypedSemanticProviderEvidence? typedSemanticProvider = null
    )
    {
        var entries = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>();
        foreach ((long nodeId, NetherInteractiveFloorPreEntryCaptureSpec spec) in specs)
        {
            NetherFloorNode floor = snapshot.Floors.Single(node => node.NodeId == nodeId);
            entries[nodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
            {
                IsCaptured = true,
                Input = new NetherInteractiveFloorPreEntrySafetyInput(
                    spec.FloorKind,
                    floor.FloorId,
                    [new NetherFloorMasterBoundsRow(floor.FloorId, 0, 0)],
                    Array.Empty<NetherFloorEventMasterRow>(),
                    Array.Empty<NetherFloorEventPartMasterRow>(),
                    snapshot.ErosionPoint,
                    snapshot.Characters.Where(character => character.IsActive).Select(character => character.HpPermille).ToArray(),
                    snapshot.NetherGold,
                    snapshot.TreasureKeyCount,
                    settings
                )
                {
                    FloorNodeId = nodeId,
                    TypedSemanticProvider = typedSemanticProvider,
                },
                Safety = spec.Safety,
            };
        }
        return NetherRuntimeInteractivePreEntryInputsResult.Success(
            entries,
            snapshot.Fingerprint,
            typedSemanticProvider
        );
    }

    private static NetherStrategyVisibleContentRow VisibleErosionKeyEvent(
        long nodeId,
        long eventId,
        long partId,
        int optionNumber
    ) => new(NetherStrategyVisibleContentKind.Event, nodeId, eventId, partId)
    {
        IsKnown = true,
        EventId = eventId,
        EventPartId = partId,
        EventOptions =
        [
            new NetherStrategyVisibleEventOptionEvidence(
                optionNumber,
                partId,
                [
                    new NetherStrategyVisibleEventEffectEvidence(
                        NetherStrategyVisibleEventEffectSource.Target1,
                        2,
                        80
                    )
                    {
                        IsPresent = true,
                        IsKnown = true,
                        EffectKind = NetherEffectKind.Erosion,
                        Amount = 80,
                    },
                    new NetherStrategyVisibleEventEffectEvidence(
                        NetherStrategyVisibleEventEffectSource.Content,
                        166,
                        1
                    )
                    {
                        IsPresent = true,
                        IsKnown = true,
                        EffectKind = NetherEffectKind.TreasureKeyGain,
                        Amount = 1,
                    },
                ]
            ),
        ],
    };

    private static NetherStrategyVisibleContentRow VisibleEvent(
        long nodeId,
        long eventId,
        long partId,
        int optionNumber,
        NetherEffectKind kind,
        int amount
    ) => new(
        NetherStrategyVisibleContentKind.Event,
        nodeId,
        eventId,
        partId
    )
    {
        EventId = eventId,
        EventPartId = partId,
        IsKnown = true,
        EventOptions =
        [
            new NetherStrategyVisibleEventOptionEvidence(
                optionNumber,
                partId,
                [new NetherStrategyVisibleEventEffectEvidence(
                    NetherStrategyVisibleEventEffectSource.Content,
                    kind == NetherEffectKind.NetherGoldGain ? 165 : 166,
                    0
                )
                {
                    EffectKind = kind,
                    Amount = amount,
                    ContentId = 0,
                    IsPresent = true,
                    IsKnown = true,
                }]
            ),
        ],
    };

    private static NetherRuntimeInteractivePreEntryInputsResult BranchInteractive(
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings,
        NetherInteractiveEventOptionKey goldKey,
        NetherInteractiveEventOptionKey keyKey,
        NetherStrategyTypedSemanticProviderEvidence? typedSemanticProvider = null
    )
    {
        NetherInteractiveOptionProjection goldProjection = new(
            goldKey.OptionNumber,
            ErosionDelta: 0,
            HpDelta: 0,
            ExpectedEffects: [new NetherEffect(NetherEffectKind.NetherGoldGain, 200)]
        )
        {
            EventId = goldKey.EventId,
            EventPartId = goldKey.EventPartId,
            FloorId = 2,
            NodeId = 2,
            IsKnown = true,
            HasRouteSafetyEvidence = true,
            RouteSafetyAllowed = true,
        };
        NetherInteractiveOptionProjection keyProjection = new(
            keyKey.OptionNumber,
            ErosionDelta: 0,
            HpDelta: 0,
            ExpectedEffects: [new NetherEffect(NetherEffectKind.TreasureKeyGain, 1)]
        )
        {
            EventId = keyKey.EventId,
            EventPartId = keyKey.EventPartId,
            FloorId = 2,
            NodeId = 2,
            IsKnown = true,
            HasRouteSafetyEvidence = true,
            RouteSafetyAllowed = true,
        };
        var entries = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>();
        foreach (NetherFloorNode floor in snapshot.Floors.Where(node =>
            node.NodeType is NetherFloorNodeType.Event
                or NetherFloorNodeType.Recovery
                or NetherFloorNodeType.Shop
                or NetherFloorNodeType.Treasure))
        {
            bool optionFloor = floor.NodeType is NetherFloorNodeType.Event or NetherFloorNodeType.Treasure;
            NetherInteractiveOptionProjection selected = floor.NodeType == NetherFloorNodeType.Treasure
                ? new NetherInteractiveOptionProjection(
                    1,
                    ErosionDelta: 0,
                    HpDelta: 0,
                    ExpectedEffects: [new NetherEffect(NetherEffectKind.TreasureKeyGain, 1)]
                )
                {
                    EventId = 401,
                    EventPartId = 4011,
                    FloorId = floor.FloorId,
                    NodeId = floor.NodeId,
                    IsKnown = true,
                    HasRouteSafetyEvidence = true,
                    RouteSafetyAllowed = true,
                }
                : goldProjection;
            NetherInteractiveFloorPreEntrySafetyInput input = new(
                floor.NodeType,
                floor.FloorId,
                [new NetherFloorMasterBoundsRow(floor.FloorId, 0, 0)],
                [],
                [],
                snapshot.ErosionPoint,
                snapshot.Characters.Where(character => character.IsActive).Select(character => character.HpPermille).ToArray(),
                snapshot.NetherGold,
                snapshot.TreasureKeyCount,
                settings
            )
            {
                FloorNodeId = floor.NodeId,
                TypedSemanticProvider = typedSemanticProvider,
            };
            NetherInteractiveFloorPreEntrySafetyResult safety;
            if (!optionFloor)
            {
                safety = NetherInteractiveFloorPreEntrySafetyResult.SafeNeutral();
            }
            else
            {
                var optionProjections = floor.NodeType == NetherFloorNodeType.Event
                    ? new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                    {
                        [goldKey] = goldProjection,
                        [keyKey] = keyProjection,
                    }
                    : new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                    {
                        [new NetherInteractiveEventOptionKey(401, 4011, 1)] = selected,
                    };
                safety = NetherInteractiveFloorPreEntrySafetyResult.Safe(
                    new Dictionary<long, int> { [selected.EventId] = selected.OptionNumber },
                    new Dictionary<long, NetherInteractiveOptionProjection> { [selected.EventId] = selected },
                    new NetherInteractiveWorstCaseProjection(0, 0),
                    optionProjections
                );
            }
            entries[floor.NodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
            {
                IsCaptured = true,
                Input = input,
                Safety = safety,
            };
        }
        return NetherRuntimeInteractivePreEntryInputsResult.Success(
            entries,
            snapshot.Fingerprint,
            typedSemanticProvider
        );
    }

    private static NetherProductionRouteSafetyPlan Plan(
        int erosion = 40,
        int hpPermille = 500,
        NetherActivePartyHpSafety? hp = null,
        IReadOnlyDictionary<long, NetherFloorMasterBounds>? bounds = null,
        NetherActiveCodeErosionProjection? code = null
    )
    {
        NetherSnapshot snapshot = SnapshotWithHp(
            erosion,
            hpPermille,
            Floor(1, 1, NetherFloorNodeType.Recovery),
            Floor(2, 2, NetherFloorNodeType.Battle, 1),
            Floor(3, 3, NetherFloorNodeType.Boss, 2, previous: new long[] { 2 })
        );
        return new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            Settings(),
            Runtime(snapshot, hpPermille, hp, bounds, code)
        );
    }

    private static NetherProductionRouteSafetyPlan PlanSameBranchProcurement(int shopCost, long itemType)
    {
        NetherFloorNode[] floors =
        [
            Floor(1, 1, NetherFloorNodeType.Recovery, previous: Array.Empty<long>()),
            Floor(2, 2, NetherFloorNodeType.Event, previous: new[] { 1L }),
            Floor(3, 3, NetherFloorNodeType.Shop, previous: new[] { 2L }),
            Floor(4, 4, NetherFloorNodeType.Treasure, previous: new[] { 3L }),
            Floor(5, 5, NetherFloorNodeType.Boss, previous: new[] { 4L }),
        ];
        NetherSnapshot snapshot = SnapshotWithHp(20, 500, floors) with
        {
            CurrentNodeId = 1,
            NetherGold = 0,
            TreasureKeyCount = 0,
        };
        NetherAutoClimbSettings settings = Settings();
        NetherInteractiveEventOptionKey goldKey = new(100, 1001, 1);
        NetherInteractiveEventOptionKey keyKey = new(100, 1002, 2);
        NetherRuntimeRouteSafetyData runtime = Runtime(
            snapshot,
            hpPermille: 500,
            bounds: new Dictionary<long, NetherFloorMasterBounds>
            {
                [5] = Bounds(5, 0, 0),
            }
        ) with
        {
            VisibleMap = new NetherStrategyVisibleMapEvidence(
                floors,
                [
                    VisibleEvent(2, 100, 1001, 1, NetherEffectKind.NetherGoldGain, amount: 200),
                    VisibleEvent(2, 100, 1002, 2, NetherEffectKind.TreasureKeyGain, amount: 1),
                    new NetherStrategyVisibleContentRow(
                        NetherStrategyVisibleContentKind.ShopInventory,
                        3,
                        3001,
                        3001
                    )
                    {
                        IsKnown = true,
                        ContentType = 166,
                        Cost = shopCost,
                        Amount = 1,
                        UsesNetherGold = true,
                    },
                    new NetherStrategyVisibleContentRow(
                        NetherStrategyVisibleContentKind.Treasure,
                        4,
                        4001,
                        401
                    )
                    {
                        IsKnown = true,
                        EventId = 401,
                        EventPartId = 4011,
                    },
                    new NetherStrategyVisibleContentRow(
                        NetherStrategyVisibleContentKind.Item,
                        4,
                        4011,
                        4011
                    )
                    {
                        IsKnown = true,
                        EventId = 401,
                        EventPartId = 4011,
                        ItemType = itemType,
                        ItemRarity = 5,
                        Amount = 1,
                    },
                ]
            ),
        };

        NetherStrategyTypedSemanticProviderEvidence provider = ProcurementProvider();
        runtime = runtime with
        {
            VisibleMap = BindProviderBackedCanonicalEvidence(runtime.VisibleMap, provider),
        };
        return new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            settings,
            runtime,
            BranchInteractive(snapshot, settings, goldKey, keyKey, provider)
        );
    }

    private static NetherStrategyVisibleMapEvidence BindProviderBackedCanonicalEvidence(
        NetherStrategyVisibleMapEvidence visibleMap,
        NetherStrategyTypedSemanticProviderEvidence provider
    )
    {
        NetherStrategySemanticTierLookup semanticTiers = NetherStrategySemanticTierLookup.Create(provider);
        return visibleMap with
        {
            ContentRows = visibleMap.ContentRows
                .Select(row =>
                {
                    if (row.Kind is NetherStrategyVisibleContentKind.ShopInventory
                        && semanticTiers.TryGetShopKey(
                            row.ContentId,
                            row.ContentType,
                            row.MasterRowId,
                            row.Amount,
                            out long shopKeyIdentity
                        ))
                    {
                        return row with
                        {
                            IsTreasureKey = true,
                            ShopKeyIdentity = shopKeyIdentity,
                        };
                    }
                    if (row.Kind is not (
                            NetherStrategyVisibleContentKind.Item
                            or NetherStrategyVisibleContentKind.ShopInventory
                        )
                        || !semanticTiers.TryGetCanonicalRewardTier(
                            row.ContentId,
                            out NetherCanonicalRewardTier tier
                        ))
                    {
                        return row;
                    }
                    return row with { CanonicalRewardTier = tier };
                })
                .ToArray(),
        };
    }

    private static NetherStrategyTypedSemanticProviderEvidence ProcurementProvider() =>
        CanonicalRewardProvider(4011) with
        {
            ShopKeyIdentities =
            [new NetherShopKeyProviderEvidence(3001, 166, 3001, 1, 3011)],
        };

    private static NetherStrategyTypedSemanticProviderEvidence CanonicalRewardProvider(long itemId)
    {
        NetherSnapshot snapshot = SnapshotWithHp(
            erosion: 0,
            hpPermille: 1000,
            Floor(1, 1, NetherFloorNodeType.Recovery)
        );
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            CanonicalRewardTiers =
            [new NetherCanonicalRewardTierProviderEvidence(itemId, NetherCanonicalRewardTier.GoldRankFive, 91)],
        };
        NetherRuntimeBridge bridge = new(_ =>
            new NetherRuntimeTypedSemanticProviderScope(snapshot.Fingerprint, provider));
        NetherRuntimeInteractivePreEntryCaptureResult captured = bridge.CaptureInteractivePreEntryFloor(
            snapshot,
            new NetherAutoClimbSettings(),
            new RuntimeFloorFixture
            {
                MNetherMapFloorId = snapshot.CurrentFloorId,
                ExtendId = 0,
                FloorType = (int)NetherFloorNodeType.Recovery,
            },
            mapFloorRows: null,
            eventRows: null,
            eventPartRows: null,
            itemRows: null,
            battleRows: null,
            floorNodeId: snapshot.CurrentNodeId,
            canCloseShop: false
        );
        Assert.True(captured.IsCaptured, captured.Detail);
        Assert.Same(provider, captured.Input!.TypedSemanticProvider);
        return provider;
    }

    private static string UnknownCandidateDetail(NetherProductionRouteSafetyPlan plan) =>
        Assert.Single(plan.Route.Audit, audit => audit.Reason == "unknown-node").Detail;

    private static NetherRuntimeRouteSafetyData Runtime(
        NetherSnapshot snapshot,
        int hpPermille = 500,
        NetherActivePartyHpSafety? hp = null,
        IReadOnlyDictionary<long, NetherFloorMasterBounds>? bounds = null,
        NetherActiveCodeErosionProjection? code = null
    ) => Runtime(hpPermille, hp, bounds, code) with
    {
        VisibleMap = CaptureVisibleMap(snapshot),
    };

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
        // These Equipment production fixtures intentionally provide a complete strategy state;
        // a missing value is reserved for the explicit native-unknown pause tests.
        ResearchIncomplete = false,
    };

    private static NetherStrategyVisibleMapEvidence CaptureVisibleMap(NetherSnapshot snapshot)
    {
        Assert.NotNull(snapshot);
        NetherRuntimeBridge bridge = new(_ => null);
        var entries = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>();
        foreach (NetherFloorNode floor in snapshot.Floors ?? Array.Empty<NetherFloorNode>())
        {
            if (floor.NodeType is not (
                    NetherFloorNodeType.Event
                    or NetherFloorNodeType.Recovery
                    or NetherFloorNodeType.Shop
                    or NetherFloorNodeType.Treasure
                ))
            {
                continue;
            }
            NetherRuntimeInteractivePreEntryCaptureResult captured = bridge.CaptureInteractivePreEntryFloor(
                snapshot,
                Settings(),
                new RuntimeFloorFixture
                {
                    MNetherMapFloorId = floor.FloorId,
                    ExtendId = 0,
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
            Assert.True(captured.IsCaptured, captured.Detail);
            entries[floor.NodeId] = captured;
        }

        NetherRuntimeInteractivePreEntryInputsResult interactive =
            NetherRuntimeInteractivePreEntryInputsResult.Success(entries, snapshot.Fingerprint);
        NetherStrategyVisibleEvidenceCaptureResult mapped =
            NetherStrategyVisibleEvidenceAssembler.Assemble(
                new NetherStrategyVisibleEvidenceAssemblyRequest(
                    snapshot,
                    interactive,
                    NetherRuntimePopupResult.Failure("no-current-popup"),
                    new NetherStrategyVisibleEvidenceCaptureRequest(
                        snapshot.Floors ?? Array.Empty<NetherFloorNode>(),
                        Array.Empty<NetherStrategyBattleMasterRow>(),
                        Array.Empty<NetherStrategyTreasureMasterRow>(),
                        Array.Empty<NetherFloorEventMasterRow>(),
                        Array.Empty<NetherFloorEventPartMasterRow>(),
                        Array.Empty<NetherStrategyItemMasterRow>()
                    )
                )
            );
        Assert.True(mapped.IsSuccess, mapped.Detail);
        Assert.NotEmpty(mapped.Evidence!.ContentRows);
        return mapped.Evidence!;
    }

    private static NetherFloorMasterBounds Bounds(long floorId, int min, int max) =>
        new(floorId, min, max, IsKnown: true, Detail: string.Empty);

    private sealed class RuntimeFloorFixture
    {
        public long MNetherMapFloorId { get; init; }
        public long ExtendId { get; init; }
        public int FloorType { get; init; }
    }

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
        CurrentFloorId = floors.FirstOrDefault()?.FloorId ?? 0,
        CurrentNodeId = floors.FirstOrDefault()?.NodeId ?? 0,
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
