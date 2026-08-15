#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherRouteHorizonProductionWiringTests
{
    [Fact]
    public void Context_builder_projects_the_erosion_horizon_but_defers_hp_after_battle_settlement()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300: NetherData.ErosionPoint is one
        // authoritative run state, so later visible floors must start from the preceding
        // projection rather than independently reusing the original value. The same assembly's
        // NetherClearBattleResponseEntity.t_nether_characters is the first authoritative source
        // of post-battle HP, so the later Recovery HP cost is deferred to the mandatory replan.
        NetherRouteSafetyFloorInput[] floors =
        [
            Floor(1, NetherFloorNodeType.Recovery, 68),
            Floor(2, NetherFloorNodeType.Battle, 68, erosionDelta: 5, previous: 1),
            Floor(3, NetherFloorNodeType.Recovery, 68, erosionDelta: -10, hpDelta: -100, previous: 2),
            Floor(4, NetherFloorNodeType.Boss, 68, erosionDelta: 5, previous: 3),
        ];
        NetherRouteSafetyContext context = new NetherRouteSafetyContextBuilder().Build(
            new NetherRouteSafetyContextBuilderInput(
                Floors: floors,
                NecessaryTerminalFloorIds: new HashSet<long> { 4 },
                SafeExitKnownByFloorId: floors.ToDictionary(floor => floor.ServerNode.NodeId, _ => true),
                MaximumFloorLevel: 130
            )
        );

        Assert.True(context.IsHardSafe(2), context.HorizonRejection(2));
        Assert.Equal(73, context.PeakErosion(2));
        Assert.Equal(500, context.MinimumActiveCharacterHpPermille(2));
        Assert.Equal(string.Empty, context.HorizonRejection(2));
    }

    [Fact]
    public void Production_coordinator_rejects_visible_branch_that_finishes_above_seventy_without_recovery()
    {
        NetherFloorNode[] floors =
        [
            Node(1, NetherFloorNodeType.Recovery),
            Node(2, NetherFloorNodeType.Battle, 1),
            Node(3, NetherFloorNodeType.Boss, 2),
        ];
        var snapshot = new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            NetherId = 1,
            MapId = 1,
            CurrentFloorId = 1,
            CurrentNodeId = 1,
            ErosionPoint = 68,
            Characters = new[] { new NetherCharacterState(1, 800, IsActive: true) },
            Floors = floors,
        };
        var runtime = new NetherRuntimeRouteSafetyData
        {
            FloorBoundsByFloorId = new Dictionary<long, NetherFloorMasterBounds>
            {
                [2] = new(2, 0, 0, true, string.Empty),
                [3] = new(3, 0, 0, true, string.Empty),
            },
            ActivePartyHp = NetherRouteSafetyHpTestEvidence.Single(1, 800),
            ActiveCodeErosion = new NetherActiveCodeErosionProjection
            {
                ErosionProjectionKnown = true,
                CodeHash = "nether-codes:none",
                ErosionEffects = Array.Empty<NetherCodeEffect>(),
            },
        };
        var settings = new NetherAutoClimbSettings
        {
            MaxDepth = 130,
            SoftErosionLimit = 90,
            MinimumCharacterHpPermille = 300,
            StrategyMode = NetherStrategyMode.Equipment,
        };

        NetherProductionRouteSafetyPlan plan = new NetherRouteSafetyProductionCoordinator().Plan(
            snapshot,
            130,
            settings,
            runtime
        );

        Assert.False(plan.Route.HasSelection);
        Assert.Equal("route-finishes-above-70", plan.Context.HorizonRejection(2));
        Assert.True(plan.Context.RequiresUserPause(2));
    }

    private static NetherRouteSafetyFloorInput Floor(
        long id,
        NetherFloorNodeType type,
        int currentErosion,
        int erosionDelta = 0,
        int hpDelta = 0,
        long? previous = null
    ) => new(
        Node(id, type, previous),
        new NetherFloorSafetyInput(
            CurrentErosion: currentErosion,
            FloorMinimumErosion: erosionDelta,
            FloorMaximumErosion: erosionDelta,
            KnownModifierDelta: 0,
            Kind: type == NetherFloorNodeType.Boss
                ? NetherFloorSafetyKind.NecessaryTerminal
                : NetherFloorSafetyKind.Optional,
            NodeType: type,
            CurrentHpPermille: new[] { 700, 500 },
            MinimumHpPermille: 300,
            SoftErosionLimit: 90,
            HardErosionLimit: 100,
            AllInputsKnown: true
        )
        {
            ErosionModifiers = Array.Empty<NetherErosionModifier>(),
        },
        ProjectedHpDelta: hpDelta,
        SafeCodeOpportunity: 0
    );

    private static NetherFloorNode Node(
        long id,
        NetherFloorNodeType type,
        long? previous = null
    ) => new(id, (int)id, (int)id, type)
    {
        NodeId = id,
        IsUnlocked = true,
        PreviousFloorIds = previous.HasValue ? new[] { previous.Value } : Array.Empty<long>(),
    };
}
