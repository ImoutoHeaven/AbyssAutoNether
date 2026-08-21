#nullable enable

using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// Keeps Recovery proof binding scoped to the portion of the selected route that can run before
/// the next native Battle settlement. A downstream Recovery is re-evaluated from the authoritative
/// post-Battle snapshot; stale HP must neither certify it nor poison the current frontier.
/// </summary>
internal static class NetherRecoveryBranchProofScope
{
    public static bool IsDeferredUntilBattleReplan(
        NetherSnapshot? snapshot,
        NetherRoutePlan? route,
        long nodeId
    )
    {
        if (snapshot?.Floors == null
            || route?.SelectedPathNodeIds == null
            || nodeId <= 0)
        {
            return false;
        }

        long currentNodeId = snapshot.CurrentNodeId > 0
            ? snapshot.CurrentNodeId
            : snapshot.CurrentFloorId;
        if (currentNodeId <= 0
            || route.SelectedPathNodeIds.Count < 2
            || route.SelectedPathNodeIds[0] != currentNodeId)
        {
            return false;
        }

        var floors = new Dictionary<long, NetherFloorNode>();
        foreach (NetherFloorNode? floor in snapshot.Floors)
        {
            if (floor == null || floor.NodeId <= 0 || !floors.TryAdd(floor.NodeId, floor))
                return false;
        }

        int targetIndex = -1;
        for (int index = 1; index < route.SelectedPathNodeIds.Count; index++)
        {
            long pathNodeId = route.SelectedPathNodeIds[index];
            if (!floors.TryGetValue(pathNodeId, out NetherFloorNode? floor))
                return false;
            if (pathNodeId == nodeId)
            {
                targetIndex = index;
                break;
            }
        }
        if (targetIndex < 0)
            return false;

        // The current node is already settled. A combat node after it becomes the boundary whose
        // native clear response must be observed before any later Recovery can be evaluated.
        for (int index = 1; index < targetIndex; index++)
        {
            NetherFloorNode floor = floors[route.SelectedPathNodeIds[index]];
            if (IsCombat(floor.NodeType))
                return true;
        }
        return false;
    }

    public static bool RequiresCompleteProofForCapturedFloor(
        NetherInteractiveFloorPreEntrySafetyInput? input,
        IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence>? proofs
    )
    {
        if (input?.FloorKind != NetherFloorNodeType.Recovery
            || proofs == null
            || proofs.Count == 0
            || input.EventRows == null)
        {
            return false;
        }

        NetherFloorEventMasterRow? resolved = null;
        foreach (NetherFloorEventMasterRow row in input.EventRows)
        {
            bool matches = input.FloorExtendId > 0
                ? row.EventId == input.FloorExtendId
                : row.MapFloorMasterId == input.FloorMasterId;
            if (matches)
            {
                resolved = row;
                break;
            }
        }
        if (resolved is not NetherFloorEventMasterRow eventRow)
            return false;

        return HasBoundProof(eventRow.PartId1, proofs)
            || HasBoundProof(eventRow.PartId2, proofs)
            || HasBoundProof(eventRow.PartId3, proofs)
            || HasBoundProof(eventRow.PartId4, proofs);
    }

    private static bool HasBoundProof(
        long eventPartId,
        IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> proofs
    ) => eventPartId > 0 && proofs.ContainsKey(eventPartId);

    private static bool IsCombat(NetherFloorNodeType nodeType) => nodeType is
        NetherFloorNodeType.Battle or NetherFloorNodeType.MiniBoss or NetherFloorNodeType.Boss;
}
