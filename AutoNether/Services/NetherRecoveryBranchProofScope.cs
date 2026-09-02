#nullable enable

using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// Keeps Recovery proof binding scoped to the immediately selected runtime node. Every confirmed
/// floor mutation creates a fresh planning boundary; stale state must neither certify a later
/// Recovery nor poison the current frontier.
/// </summary>
internal static class NetherRecoveryBranchProofScope
{
    public static IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> ForNode(
        IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence>? proofs,
        long nodeId
    )
    {
        var scoped = new Dictionary<long, NetherRecoveryBranchSafetyEvidence>();
        if (proofs == null || nodeId <= 0)
            return scoped;

        foreach ((long partId, NetherRecoveryBranchSafetyEvidence proof) in proofs)
        {
            if (proof.NodeId == nodeId)
                scoped[partId] = proof;
        }
        return scoped;
    }

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

        return HasBoundProof(eventRow.PartId1, input.FloorNodeId, proofs)
            || HasBoundProof(eventRow.PartId2, input.FloorNodeId, proofs)
            || HasBoundProof(eventRow.PartId3, input.FloorNodeId, proofs)
            || HasBoundProof(eventRow.PartId4, input.FloorNodeId, proofs);
    }

    private static bool HasBoundProof(
        long eventPartId,
        long nodeId,
        IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> proofs
    ) => eventPartId > 0
        && nodeId > 0
        && proofs.TryGetValue(eventPartId, out NetherRecoveryBranchSafetyEvidence? proof)
        && proof.NodeId == nodeId;

    private static bool IsCombat(NetherFloorNodeType nodeType) => nodeType is
        NetherFloorNodeType.Battle or NetherFloorNodeType.MiniBoss or NetherFloorNodeType.Boss;
}
