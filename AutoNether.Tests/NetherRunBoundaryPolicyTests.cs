using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherRunBoundaryPolicyTests
{
    [Fact]
    public void Equipment_normalizes_a_positive_non_boss_target_up_to_the_next_authoritative_boss()
    {
        NetherRunBoundaryDecision decision = Resolve(
            Snapshot(recoveryFloor: 40, bosses: new[] { 10, 20, 30, 40, 50, 60, 70, 80 }),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment, MaxDepth = 53 }
        );

        Assert.True(decision.IsReady);
        Assert.Equal(60, decision.TargetFloorLevel);
        Assert.Equal(40, decision.StartFloorLevel);
    }

    [Fact]
    public void Equipment_caps_an_out_of_range_target_at_the_deepest_authoritative_boss()
    {
        NetherRunBoundaryDecision decision = Resolve(
            Snapshot(recoveryFloor: 80, bosses: new[] { 10, 20, 30, 40, 50, 60, 70, 80 }),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment, MaxDepth = 999 }
        );

        Assert.True(decision.IsReady);
        Assert.Equal(80, decision.TargetFloorLevel);
        Assert.Equal(80, decision.StartFloorLevel);
    }

    [Fact]
    public void Missing_authoritative_boss_rows_fail_closed()
    {
        NetherRunBoundaryDecision decision = Resolve(
            Snapshot(recoveryFloor: 80, bosses: Array.Empty<int>()),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment, MaxDepth = 50 }
        );

        Assert.False(decision.IsReady);
        Assert.Equal(NetherPauseReason.UnknownMasterData, decision.PauseReason);
        Assert.Equal("authoritative-boss-floor-unavailable", decision.Detail);
    }

    [Fact]
    public void Research_uses_the_authoritative_floor_70_boss_and_always_starts_at_zero()
    {
        // Fresh Project.dll: MNetherMapFloors.type uses Project.Master.NetherFloorType.BattleBoss;
        // NetherPointData.RecoveryFloorLevel is the live equipment checkpoint authority only.
        NetherRunBoundaryDecision decision = Resolve(
            Snapshot(recoveryFloor: 80, bosses: new[] { 10, 20, 30, 40, 50, 60, 70, 80 }),
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Rush,
                MaxDepth = 130,
            }
        );

        Assert.True(decision.IsReady);
        Assert.Equal(70, decision.TargetFloorLevel);
        Assert.Equal(0, decision.StartFloorLevel);
    }

    [Fact]
    public void Research_fails_closed_when_floor_70_is_not_an_authoritative_boss()
    {
        NetherRunBoundaryDecision decision = Resolve(
            Snapshot(recoveryFloor: 60, bosses: new[] { 10, 20, 30, 40, 50, 60 }),
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Safe,
            }
        );

        Assert.False(decision.IsReady);
        Assert.Equal(NetherPauseReason.UnknownMasterData, decision.PauseReason);
        Assert.Equal("research-floor-70-boss-unavailable", decision.Detail);
    }

    [Fact]
    public void Equipment_uses_the_highest_unlocked_authoritative_checkpoint_not_above_target()
    {
        NetherRunBoundaryDecision decision = Resolve(
            Snapshot(recoveryFloor: 75, bosses: new[] { 10, 20, 30, 40, 50, 60, 70, 80 }),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment, MaxDepth = 63 }
        );

        Assert.True(decision.IsReady);
        Assert.Equal(70, decision.TargetFloorLevel);
        Assert.Equal(70, decision.StartFloorLevel);
    }

    [Fact]
    public void Equipment_checkpoint_levels_come_from_live_recovery_floor_not_boss_rows()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...e1fb:
        // NetherPointData.RecoveryFloorLevel is the live checkpoint authority and
        // Nether.Party.FloorLevelSelect.SubViewController owns `_checkPointFloorLevels`.
        // Boss rows normalize the target only; they are not the set of elevator starts.
        NetherRunBoundaryDecision decision = Resolve(
            Snapshot(recoveryFloor: 70, bosses: new[] { 15, 30, 45, 60, 75 }),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment, MaxDepth = 75 }
        );

        Assert.True(decision.IsReady);
        Assert.Equal(75, decision.TargetFloorLevel);
        Assert.Equal(70, decision.StartFloorLevel);
    }

    private static NetherRunBoundaryDecision Resolve(
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings
    ) => new NetherRunBoundaryPolicy().Resolve(snapshot, settings);

    private static NetherSnapshot Snapshot(int recoveryFloor, IReadOnlyList<int> bosses) => new()
    {
        Status = NetherSessionStatus.Play,
        FloorLevel = 0,
        MasterMaxFloorLevel = bosses.Count == 0 ? 130 : bosses.Max(),
        RecoveryFloorLevel = recoveryFloor,
        AuthoritativeBossFloorLevels = bosses,
    };
}
