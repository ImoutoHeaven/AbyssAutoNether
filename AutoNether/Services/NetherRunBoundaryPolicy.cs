#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal sealed record NetherRunBoundaryDecision
{
    public bool IsReady { get; init; }
    public int TargetFloorLevel { get; init; }
    public int StartFloorLevel { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// Resolves only reward-preserving run boundaries. The current packaged client exposes the map
/// cap through MNetherMaps.max_floor_num, Boss templates through MNetherMapFloors.type, and the
/// live elevator authority through NetherPointData.RecoveryFloorLevel.  The native floor-level
/// selector materializes checkpoint starts in ten-floor increments up to that live authority
/// (both modes, strictly below the target);
/// Boss rows normalize only the reward-preserving target and never define elevator starts.
/// </summary>
internal sealed class NetherRunBoundaryPolicy
{
    public NetherRunBoundaryDecision Resolve(
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings
    )
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        if (!Enum.IsDefined(typeof(NetherStrategyMode), settings.StrategyMode)
            || settings.MaxDepth < 1
            || settings.ResearchMaxDepth < 1)
        {
            return Pause(NetherPauseReason.InvalidConfiguration, "invalid-run-boundary-settings");
        }

        IReadOnlyList<int>? rawBosses = snapshot.AuthoritativeBossFloorLevels;
        if (rawBosses == null || rawBosses.Count == 0)
        {
            return Pause(
                NetherPauseReason.UnknownMasterData,
                "authoritative-boss-floor-unavailable"
            );
        }
        if (snapshot.MasterMaxFloorLevel < 1
            || rawBosses.Any(floor => floor < 1 || floor > snapshot.MasterMaxFloorLevel)
            || rawBosses.Distinct().Count() != rawBosses.Count)
        {
            return Pause(NetherPauseReason.UnknownMasterData, "invalid-authoritative-boss-floors");
        }

        int[] bosses = rawBosses.OrderBy(floor => floor).ToArray();
        int requested = settings.StrategyMode == NetherStrategyMode.Research
            ? settings.ResearchMaxDepth
            : settings.MaxDepth;
        int target = bosses.FirstOrDefault(floor => floor >= requested);
        if (target == 0)
            target = bosses[^1];

        // Both modes start from the deepest unlocked elevator step (tech-tree direct elevator).
        if (snapshot.RecoveryFloorLevel < 0)
        {
            return Pause(
                NetherPauseReason.UnknownMasterData,
                "invalid-live-recovery-floor-level"
            );
        }

        // A start at the target itself would be a Play snapshot already at the ceiling.
        int unlockedLimit = Math.Min(snapshot.RecoveryFloorLevel, target - 1);
        int start = unlockedLimit < 10 ? 0 : unlockedLimit / 10 * 10;

        return new NetherRunBoundaryDecision
        {
            IsReady = true,
            TargetFloorLevel = target,
            StartFloorLevel = start,
            PauseReason = NetherPauseReason.None,
            Detail = "boss-aligned-run-boundary",
        };
    }

    private static NetherRunBoundaryDecision Pause(NetherPauseReason reason, string detail) => new()
    {
        IsReady = false,
        PauseReason = reason,
        Detail = detail,
    };
}
