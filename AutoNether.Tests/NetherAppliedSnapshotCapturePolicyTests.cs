#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherAppliedSnapshotCapturePolicyTests
{
    [Fact]
    public void Postbattle_settlement_ignores_stale_live_floor_model_and_uses_authoritative_transition_snapshot()
    {
        int fullSnapshotReads = 0;
        int transitionSnapshotReads = 0;
        var staleFloorModel = new NetherSnapshot
        {
            Status = NetherSessionStatus.Battle,
            NetherId = 1,
            MapId = 1,
            CurrentFloorId = 219,
            FloorLevel = 48,
            FloorIndex = 1,
        };
        var authoritativeTransition = staleFloorModel with
        {
            Status = NetherSessionStatus.Play,
        };

        NetherRuntimeSnapshotResult result = NetherAppliedSnapshotCapturePolicy.Capture(
            requireFreshBattleResultCharacters: true,
            captureFullSnapshot: () =>
            {
                fullSnapshotReads++;
                return NetherRuntimeSnapshotResult.Success(staleFloorModel);
            },
            captureTransitionSnapshot: () =>
            {
                transitionSnapshotReads++;
                return NetherRuntimeSnapshotResult.Success(authoritativeTransition);
            }
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Same(authoritativeTransition, result.Snapshot);
        Assert.Equal(0, fullSnapshotReads);
        Assert.Equal(1, transitionSnapshotReads);
    }

    [Fact]
    public void Ordinary_reconciliation_keeps_a_valid_live_floor_snapshot()
    {
        int transitionSnapshotReads = 0;
        var liveFloorSnapshot = new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            NetherId = 1,
            MapId = 1,
            CurrentFloorId = 219,
        };

        NetherRuntimeSnapshotResult result = NetherAppliedSnapshotCapturePolicy.Capture(
            requireFreshBattleResultCharacters: false,
            captureFullSnapshot: () => NetherRuntimeSnapshotResult.Success(liveFloorSnapshot),
            captureTransitionSnapshot: () =>
            {
                transitionSnapshotReads++;
                return NetherRuntimeSnapshotResult.Failure("unexpected-transition-read");
            }
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Same(liveFloorSnapshot, result.Snapshot);
        Assert.Equal(0, transitionSnapshotReads);
    }

    [Fact]
    public void Ordinary_scene_teardown_retains_the_existing_missing_controller_fallback()
    {
        int transitionSnapshotReads = 0;
        var authoritativeTransition = new NetherSnapshot
        {
            Status = NetherSessionStatus.Battle,
            NetherId = 1,
            MapId = 1,
            CurrentFloorId = 219,
        };

        NetherRuntimeSnapshotResult result = NetherAppliedSnapshotCapturePolicy.Capture(
            requireFreshBattleResultCharacters: false,
            captureFullSnapshot: () =>
                NetherRuntimeSnapshotResult.Failure("missing-floor-selection-controller"),
            captureTransitionSnapshot: () =>
            {
                transitionSnapshotReads++;
                return NetherRuntimeSnapshotResult.Success(authoritativeTransition);
            }
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Same(authoritativeTransition, result.Snapshot);
        Assert.Equal(1, transitionSnapshotReads);
    }
}
