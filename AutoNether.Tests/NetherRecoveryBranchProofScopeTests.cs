#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherRecoveryBranchProofScopeTests
{
    [Fact]
    public void Recovery_after_a_newly_selected_battle_is_deferred_but_the_current_battle_is_not()
    {
        NetherFloorNode currentBattle = Node(1, NetherFloorNodeType.Battle);
        NetherFloorNode selectedBattle = Node(2, NetherFloorNodeType.Battle, 1);
        NetherFloorNode downstreamRecovery = Node(3, NetherFloorNodeType.Recovery, 2);
        var downstreamSnapshot = new NetherSnapshot
        {
            CurrentNodeId = 1,
            CurrentFloorId = 1,
            Floors = [currentBattle, selectedBattle, downstreamRecovery],
        };
        var downstreamRoute = new NetherRoutePlan
        {
            SelectedPathNodeIds = [1, 2, 3],
        };

        Assert.True(NetherRecoveryBranchProofScope.IsDeferredUntilBattleReplan(
            downstreamSnapshot,
            downstreamRoute,
            downstreamRecovery.NodeId
        ));

        NetherFloorNode immediateRecovery = Node(2, NetherFloorNodeType.Recovery, 1);
        var immediateSnapshot = downstreamSnapshot with
        {
            Floors = [currentBattle, immediateRecovery],
        };
        var immediateRoute = new NetherRoutePlan
        {
            SelectedPathNodeIds = [1, 2],
        };

        Assert.False(NetherRecoveryBranchProofScope.IsDeferredUntilBattleReplan(
            immediateSnapshot,
            immediateRoute,
            immediateRecovery.NodeId
        ));
    }

    [Fact]
    public void Complete_recovery_proof_is_requested_only_for_the_native_row_that_owns_a_bound_part()
    {
        NetherInteractiveFloorPreEntrySafetyInput boundRecovery = Input(
            eventId: 354,
            part1: 102,
            part2: 402,
            part3: 700
        );
        var proofs = new Dictionary<long, NetherRecoveryBranchSafetyEvidence>
        {
            [102] = new NetherRecoveryBranchSafetyEvidence
            {
                BranchKind = NetherRecoveryBranchKind.Rest,
                IsKnown = true,
                IsCompleteVisibleBranch = true,
                IsNextVisibleBranchSafe = true,
            },
        };

        Assert.True(NetherRecoveryBranchProofScope.RequiresCompleteProofForCapturedFloor(
            boundRecovery,
            proofs
        ));
        Assert.False(NetherRecoveryBranchProofScope.RequiresCompleteProofForCapturedFloor(
            Input(eventId: 355, part1: 202, part2: 502, part3: 800),
            proofs
        ));
    }

    private static NetherInteractiveFloorPreEntrySafetyInput Input(
        long eventId,
        long part1,
        long part2,
        long part3
    ) => new(
        NetherFloorNodeType.Recovery,
        FloorMasterId: 3,
        MapFloorRows: [new NetherFloorMasterBoundsRow(3, 0, 0)],
        EventRows: [new NetherFloorEventMasterRow(eventId, 3, 1, part1, part2, part3, 0)],
        EventPartRows: Array.Empty<NetherFloorEventPartMasterRow>(),
        CurrentErosion: 55,
        ActiveHpPermille: [1000],
        CurrentNetherGold: 0,
        CurrentTreasureKeys: 0,
        Settings: new NetherAutoClimbSettings()
    )
    {
        FloorExtendId = eventId,
        FloorNodeId = 3,
    };

    private static NetherFloorNode Node(
        long nodeId,
        NetherFloorNodeType nodeType,
        params long[] previous
    ) => new(nodeId, (int)nodeId, (int)nodeId, nodeType)
    {
        IsUnlocked = true,
        PreviousFloorIds = previous,
    };
}
