#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Exact, current route-horizon relationships needed by Code policy. This is produced only from
/// the same T03 production safety plan used for route selection; an unavailable/unsafe route stays
/// component-local unknown instead of being replaced by the snapshot's current erosion.
/// </summary>
internal sealed record NetherCodePolicyRouteEvidence
{
    public const string BattleResultBeforeFloorRebindReason =
        "battle-result-code-route-horizon-unavailable-before-floor-scene-rebind";

    public bool IsKnown { get; init; }
    public int MinimumBattleStartErosion { get; init; }
    public int MaximumBattleStartErosion { get; init; }
    public bool RecoverableToFiftySeventyBand { get; init; }
    public bool SurvivalBaselineKnown { get; init; }
    public bool HasSurvivalDeficit { get; init; }
    public bool BossDurationKnown { get; init; }
    public int BossDurationSeconds { get; init; }
    public string BossDurationUnknownReason { get; init; } = string.Empty;
    public IReadOnlyList<NetherConfirmedCombatErosion> ConfirmedCombats { get; init; } =
        Array.Empty<NetherConfirmedCombatErosion>();
    public string UnknownReason { get; init; } = string.Empty;

    public bool IsBattleResultBeforeFloorRebind =>
        !IsKnown
        && string.Equals(
            UnknownReason,
            BattleResultBeforeFloorRebindReason,
            StringComparison.Ordinal
        );

    public static NetherCodePolicyRouteEvidence Unknown(string reason) => new()
    {
        UnknownReason = string.IsNullOrWhiteSpace(reason)
            ? "code-policy-route-horizon-unavailable"
            : reason,
    };

    public static NetherCodePolicyRouteEvidence BattleResultBeforeFloorRebind() =>
        Unknown(BattleResultBeforeFloorRebindReason);
}

internal readonly record struct NetherCodePolicyBattleStageRow(long Id, int TimeLimitSeconds);

internal static class NetherCodePolicyRouteEvidenceMapper
{
    public static NetherCodePolicyRouteEvidence Map(
        NetherSnapshot snapshot,
        NetherProductionRouteSafetyPlan plan
    )
    {
        if (snapshot == null || plan == null || plan.Route?.SelectedNode == null
            || plan.Context == null)
        {
            return NetherCodePolicyRouteEvidence.Unknown("route-plan-selection-unavailable");
        }

        long selectedNodeId = plan.Route.SelectedNode.NodeId;
        NetherRouteHorizonSafetyEvaluation? horizon = plan.Context.HorizonEvaluation(selectedNodeId);
        if (horizon == null || !horizon.IsEligible || horizon.Steps == null
            || horizon.Steps.Count == 0 || !horizon.PeakErosion.HasValue)
        {
            return NetherCodePolicyRouteEvidence.Unknown(
                "selected-route-horizon-unavailable:"
                    + (horizon?.RejectionDetail ?? plan.Route.PauseDetail)
            );
        }

        var nodeTypeById = (snapshot.Floors ?? Array.Empty<NetherFloorNode>())
            .Where(floor => floor != null && floor.NodeId > 0)
            .GroupBy(floor => floor.NodeId)
            .ToDictionary(group => group.Key, group => group.First().NodeType);
        if (horizon.Steps.Any(step => !nodeTypeById.ContainsKey(step.NodeId)))
            return NetherCodePolicyRouteEvidence.Unknown("route-horizon-node-identity-unavailable");

        NetherRouteHorizonStepAudit[] combatSteps = horizon.Steps
            .Where(step => IsCombat(nodeTypeById[step.NodeId]))
            .ToArray();
        int minimum = combatSteps
            .Select(step => step.StartErosion)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        int maximum = combatSteps
            .Select(step => step.StartErosion)
            .DefaultIfEmpty(int.MinValue)
            .Max();
        NetherConfirmedCombatErosion[] combats = combatSteps
            .Select(step => new NetherConfirmedCombatErosion(
                step.NodeId,
                checked(step.StartErosion * 10),
                IsExact: true
            ))
            .ToArray();
        if (combats.Length == 0 || minimum == int.MaxValue || maximum == int.MinValue)
            return NetherCodePolicyRouteEvidence.Unknown("route-horizon-combat-unavailable");

        bool survivalKnown = horizon.MinimumActiveCharacterHpPermille is >= 0 and <= 1000;
        return new NetherCodePolicyRouteEvidence
        {
            IsKnown = true,
            MinimumBattleStartErosion = minimum,
            MaximumBattleStartErosion = maximum,
            RecoverableToFiftySeventyBand = horizon.HasConfirmedRecoveryToOperatingBand,
            SurvivalBaselineKnown = survivalKnown,
            HasSurvivalDeficit = survivalKnown
                && horizon.MinimumActiveCharacterHpPermille!.Value <= 0,
            ConfirmedCombats = combats,
            BossDurationUnknownReason = "boss-stage-duration-not-captured",
        };
    }

    public static NetherCodePolicyRouteEvidence Map(
        NetherSnapshot snapshot,
        NetherProductionRouteSafetyPlan plan,
        IReadOnlyList<NetherStrategyBattleMasterRow> battleRows,
        IReadOnlyList<NetherCodePolicyBattleStageRow> battleStageRows
    )
    {
        NetherCodePolicyRouteEvidence mapped = Map(snapshot, plan);
        if (!mapped.IsKnown)
            return mapped;
        if (battleRows == null || battleStageRows == null)
            return mapped with { BossDurationUnknownReason = "boss-stage-master-cache-unavailable" };

        long selectedNodeId = plan.Route!.SelectedNode!.NodeId;
        NetherRouteHorizonSafetyEvaluation horizon = plan.Context!.HorizonEvaluation(selectedNodeId)!;
        var floorByNodeId = (snapshot.Floors ?? Array.Empty<NetherFloorNode>())
            .Where(floor => floor != null && floor.NodeId > 0)
            .GroupBy(floor => floor.NodeId)
            .ToDictionary(group => group.Key, group => group.First());
        NetherRouteHorizonStepAudit[] bossSteps = horizon.Steps
            .Where(step => floorByNodeId.TryGetValue(step.NodeId, out NetherFloorNode? floor)
                && floor.NodeType == NetherFloorNodeType.Boss)
            .ToArray();
        if (bossSteps.Length != 1)
            return mapped with { BossDurationUnknownReason = "selected-horizon-boss-identity-unavailable" };

        NetherFloorNode bossFloor = floorByNodeId[bossSteps[0].NodeId];
        NetherStrategyBattleMasterRow[] battles = battleRows
            .Where(row => row.MapFloorMasterId == bossFloor.FloorId)
            .ToArray();
        if (battles.Length != 1)
            return mapped with { BossDurationUnknownReason = "boss-battle-master-relation-unavailable" };
        NetherCodePolicyBattleStageRow[] stages = battleStageRows
            .Where(row => row.Id == battles[0].BattleStageId)
            .ToArray();
        if (stages.Length != 1 || stages[0].TimeLimitSeconds <= 0)
            return mapped with { BossDurationUnknownReason = "boss-stage-duration-unavailable" };
        return mapped with
        {
            BossDurationKnown = true,
            BossDurationSeconds = stages[0].TimeLimitSeconds,
            BossDurationUnknownReason = string.Empty,
        };
    }

    private static bool IsCombat(NetherFloorNodeType type) => type is
        NetherFloorNodeType.Battle or NetherFloorNodeType.MiniBoss or NetherFloorNodeType.Boss;
}
