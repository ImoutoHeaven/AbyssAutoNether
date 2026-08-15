#nullable enable

using System;
using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherAutoClimbRouteSafetyWiringTests
{
    [Fact]
    public void ControllerRouteWiring_ForwardsCoordinatorContextAuditAndPreClickBattlePayload()
    {
        NetherAutoClimbRouteSafetyDecision decision = Decide(
            erosion: 40,
            bounds: Bounds((2, 5, 10), (3, 0, 0)),
            code: KnownCode("active:60001:6:2", new NetherCodeEffect(
                60001,
                NetherCodeEffectKind.ErosionAdditionUp,
                2
            ))
        );

        Assert.Equal(2, Assert.IsType<NetherFloorNode>(decision.Route.SelectedNode).FloorId);
        Assert.NotNull(decision.SelectedBattleProjection);
        Assert.NotNull(decision.SelectFloorAction);
        NetherPlannedAction action = decision.SelectFloorAction!.Value;
        Assert.Equal(NetherSessionStatus.Play, action.ExpectedBeforeStatus);
        Assert.Equal(NetherSessionStatus.Battle, action.ExpectedAfterStatus);
        Assert.Same(decision.SelectedBattleProjection, action.BattleProjection);
        Assert.Equal("route-battle:2:1:40:5:5:active:60001:6:2", decision.SelectedBattleProjection!.ProjectionIdentity);
        Assert.True(decision.Context.KnownNodeByFloorId[2]);
        Assert.Contains(decision.Route.Audit, item => item.FloorId == 2 && item.Reason == "selected");
    }

    [Fact]
    public void SoftLimitHardLimitHpAndUnknownInputs_CannotBypassProductionCoordinator()
    {
        NetherAutoClimbRouteSafetyDecision soft90 = Decide(
            erosion: 89,
            bounds: Bounds((2, 1, 1), (3, 0, 0))
        );
        NetherAutoClimbRouteSafetyDecision hp299 = Decide(
            hp: NetherRouteSafetyHpTestEvidence.Single(1, 299)
        );
        NetherAutoClimbRouteSafetyDecision unknownMaster = Decide(
            bounds: Bounds((3, 0, 0))
        );
        NetherAutoClimbRouteSafetyDecision unknownCode = Decide(
            code: new NetherActiveCodeErosionProjection { ErosionProjectionKnown = false, Detail = "unknown" }
        );

        Assert.False(soft90.Route.HasSelection);
        Assert.False(hp299.Route.HasSelection);
        Assert.False(unknownMaster.Route.HasSelection);
        Assert.False(unknownCode.Route.HasSelection);
        Assert.Null(soft90.SelectedBattleProjection);
        Assert.Null(soft90.SelectFloorAction);
    }

    [Fact]
    public void Boss_at_or_above_seventy_without_confirmed_recovery_is_paused_before_mutation()
    {
        NetherAutoClimbRouteSafetyDecision allowedBoss = DecideBoss(94, Bounds((2, 0, 100)));
        NetherAutoClimbRouteSafetyDecision rejectedBoss = DecideBoss(95, Bounds((2, 0, 100)));

        Assert.False(allowedBoss.Route.HasSelection);
        Assert.Null(allowedBoss.SelectedBattleProjection);
        Assert.Equal("erosion-70-without-confirmed-recovery", allowedBoss.Context.HorizonRejection(2));
        Assert.True(allowedBoss.Context.RequiresUserPause(2));
        Assert.False(rejectedBoss.Route.HasSelection);
        Assert.Null(rejectedBoss.SelectedBattleProjection);
    }

    [Fact]
    public void Boss_route_carries_sleep_as_its_exact_postbattle_settlement_status()
    {
        NetherAutoClimbRouteSafetyDecision decision = DecideBoss(20, Bounds((2, 0, 0)));

        Assert.True(decision.Route.HasSelection);
        Assert.NotNull(decision.SelectedBattleProjection);
        Assert.Equal(
            NetherSessionStatus.Sleep,
            decision.SelectedBattleProjection!.ExpectedSettlementStatus
        );
    }

    private static NetherAutoClimbRouteSafetyDecision Decide(
        int erosion = 40,
        NetherActivePartyHpSafety? hp = null,
        IReadOnlyDictionary<long, NetherFloorMasterBounds>? bounds = null,
        NetherActiveCodeErosionProjection? code = null
    ) => new NetherAutoClimbRouteSafetyWiring().Plan(
        new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            MapId = 1,
            CurrentFloorId = 1,
            ErosionPoint = erosion,
            Characters = new[]
            {
                new NetherCharacterState(1, hp?.MinimumHpPermille ?? 500, IsActive: true),
            },
            Floors = new[]
            {
                Floor(1, 1, NetherFloorNodeType.Recovery),
                Floor(2, 2, NetherFloorNodeType.Battle, previous: new[] { 1L }),
                Floor(3, 3, NetherFloorNodeType.Boss, previous: new[] { 2L }),
            },
        },
        settings: Settings(),
        effectiveMaximumDepth: 130,
        runtime: Runtime(hp, bounds, code)
    );

    private static NetherAutoClimbRouteSafetyDecision DecideBoss(
        int erosion,
        IReadOnlyDictionary<long, NetherFloorMasterBounds> bounds
    ) => new NetherAutoClimbRouteSafetyWiring().Plan(
        new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            MapId = 1,
            CurrentFloorId = 1,
            ErosionPoint = erosion,
            Characters = new[] { new NetherCharacterState(1, 500, IsActive: true) },
            Floors = new[]
            {
                Floor(1, 1, NetherFloorNodeType.Recovery),
                Floor(2, 2, NetherFloorNodeType.Boss, previous: new[] { 1L }),
            },
        },
        settings: Settings(),
        effectiveMaximumDepth: 130,
        runtime: Runtime(bounds: bounds)
    );

    private static NetherRuntimeRouteSafetyData Runtime(
        NetherActivePartyHpSafety? hp = null,
        IReadOnlyDictionary<long, NetherFloorMasterBounds>? bounds = null,
        NetherActiveCodeErosionProjection? code = null
    ) => new()
    {
        FloorBoundsByFloorId = bounds ?? Bounds((2, 0, 0), (3, 0, 0)),
        ActivePartyHp = hp ?? NetherRouteSafetyHpTestEvidence.Single(1, 500),
        ActiveCodeErosion = code ?? KnownCode("nether-codes:none"),
    };

    private static NetherActiveCodeErosionProjection KnownCode(
        string hash,
        params NetherCodeEffect[] effects
    ) => new()
    {
        ErosionProjectionKnown = true,
        CodeHash = hash,
        ErosionEffects = effects,
    };

    private static Dictionary<long, NetherFloorMasterBounds> Bounds(params (long Id, int Min, int Max)[] rows)
    {
        var bounds = new Dictionary<long, NetherFloorMasterBounds>();
        foreach ((long id, int min, int max) in rows)
            bounds.Add(id, new NetherFloorMasterBounds(id, min, max, IsKnown: true, Detail: string.Empty));
        return bounds;
    }

    private static NetherAutoClimbSettings Settings() => new()
    {
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = 300,
    };

    private static NetherFloorNode Floor(
        long id,
        int level,
        NetherFloorNodeType type,
        long[]? previous = null
    ) => new(id, level, (int)id, type)
    {
        IsUnlocked = true,
        PreviousFloorIds = previous ?? Array.Empty<long>(),
    };
}
