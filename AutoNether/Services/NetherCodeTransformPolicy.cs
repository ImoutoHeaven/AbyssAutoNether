#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal sealed record NetherCodeTransformDecision
{
    public bool CanTransform { get; init; }
    public long RemoveCodeId { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// Chooses the existing code passed to the native target_type=7 conversion flow.  The server
/// selects the new code, so this policy ranks only the authoritative current portfolio.  There
/// are no magic Safe/Risk IDs: it removes the card whose absence preserves the strongest native
/// paired-category counts, then uses code identity only as a deterministic tie-breaker.
/// </summary>
internal sealed class NetherCodeTransformPolicy
{
    public NetherCodeTransformDecision Decide(IReadOnlyList<NetherCodeState>? codes, int capacity)
    {
        if (codes == null
            || capacity < 1
            || codes.Count is < 1
            || codes.Count > capacity
            || codes.Any(code => code == null
                || !code.IsKnown
                || code.CodeId <= 0
                || code.Family == NetherCodeFamily.Unknown
                || code.AbilityLevel < 0
                || code.Rarity < 0
                || code.Power < 0)
            || codes.Select(code => code.CodeId).Distinct().Count() != codes.Count)
        {
            return Pause(NetherPauseReason.UnknownMasterData, "invalid-code-transform-portfolio");
        }

        NetherCodeState? selected = NetherCodePolicy.RankRemovals(codes).FirstOrDefault();

        return selected == null
            ? new NetherCodeTransformDecision
            {
                PauseReason = NetherPauseReason.NoSafeRoute,
                Detail = "no-removable-code-for-native-transform",
            }
            : new NetherCodeTransformDecision
            {
                CanTransform = true,
                RemoveCodeId = selected.CodeId,
                Detail = "remove:" + selected.CodeId,
            };
    }

    private static NetherCodeTransformDecision Pause(NetherPauseReason reason, string detail) => new()
    {
        PauseReason = reason,
        Detail = detail,
    };
}
