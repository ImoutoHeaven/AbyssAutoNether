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

internal enum NetherCodeTransformHardExclusionReason
{
    None = 0,
    UniformCrestMismatch,
    ResearchRateOverwrite,
    MinimumErosionSeventy,
    AdverseErosionAdjustment,
}

internal readonly record struct NetherCodeTransformHardExclusion(
    long CodeId,
    NetherCodeTransformHardExclusionReason Reason
);

/// <summary>
/// Complete classification of currently held hard-excluded Codes, captured from the accepted
/// strategy package. Recovery policy must not infer this list from display power or Code IDs.
/// </summary>
internal sealed record NetherCodeTransformHardExclusionEvidence
{
    public bool IsKnown { get; init; }
    public string UnknownReason { get; init; } = string.Empty;
    public IReadOnlyList<NetherCodeTransformHardExclusion> HardExcludedCodes { get; init; } =
        Array.Empty<NetherCodeTransformHardExclusion>();

    public static NetherCodeTransformHardExclusionEvidence Unknown(string reason) => new()
    {
        UnknownReason = string.IsNullOrWhiteSpace(reason)
            ? "code-transform-hard-exclusions-unavailable"
            : reason,
    };
}

/// <summary>
/// Immutable child-flow commitment created only by an accepted Recovery transform option. The
/// later native Change popup has lost the original three option rows, so it consumes this exact
/// committed removal instead of re-planning from the new presentation surface.
/// </summary>
internal readonly record struct NetherCodeTransformCommitment(long RemoveCodeId)
{
    public bool IsValid => RemoveCodeId > 0;
}

/// <summary>
/// Maps the same typed hard relationships consumed by Code selection onto the complete held
/// portfolio. A transform is recovery, not a candidate offer, so any missing held row makes the
/// complete removable set unknown rather than silently skipping that Code.
/// </summary>
internal static class NetherCodeTransformHardExclusionMapper
{
    public static NetherCodeTransformHardExclusionEvidence Map(
        NetherSnapshot snapshot,
        NetherCodePolicyEvidence evidence
    )
    {
        if (snapshot == null || evidence == null || snapshot.Codes == null)
        {
            return NetherCodeTransformHardExclusionEvidence.Unknown(
                "code-transform-held-policy-evidence-unavailable"
            );
        }

        var exclusions = new List<NetherCodeTransformHardExclusion>();
        foreach (NetherCodeState code in snapshot.Codes)
        {
            if (code == null || !code.IsKnown || code.CodeId <= 0
                || !evidence.MechanicsByCodeId.TryGetValue(
                    code.CodeId,
                    out NetherCodeHardEligibilityEvidence? mechanic
                )
                || mechanic == null || !mechanic.IsKnown)
            {
                return NetherCodeTransformHardExclusionEvidence.Unknown(
                    "code-transform-held-hard-row-unavailable:" + (code?.CodeId ?? 0)
                );
            }

            NetherCodeTransformHardExclusionReason reason = mechanic.RiskRule switch
            {
                NetherCodeRiskRule.AdverseErosionAdjustment =>
                    NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment,
                NetherCodeRiskRule.MinimumErosionSeventy =>
                    NetherCodeTransformHardExclusionReason.MinimumErosionSeventy,
                _ => NetherCodeTransformHardExclusionReason.None,
            };
            if (reason == NetherCodeTransformHardExclusionReason.None
                && mechanic.ResearchRateOverwrite > 0)
            {
                reason = NetherCodeTransformHardExclusionReason.ResearchRateOverwrite;
            }
            if (reason == NetherCodeTransformHardExclusionReason.None
                && mechanic.UniformCrestTargetRow != NetherCodeTargetRow.None)
            {
                if (!TryIsUniformCrestCompatible(mechanic, evidence.ActiveParty, out bool compatible))
                {
                    return NetherCodeTransformHardExclusionEvidence.Unknown(
                        "code-transform-held-crest-compatibility-unavailable:" + code.CodeId
                    );
                }
                if (!compatible)
                    reason = NetherCodeTransformHardExclusionReason.UniformCrestMismatch;
            }

            if (reason != NetherCodeTransformHardExclusionReason.None)
                exclusions.Add(new NetherCodeTransformHardExclusion(code.CodeId, reason));
        }

        return new NetherCodeTransformHardExclusionEvidence
        {
            IsKnown = true,
            HardExcludedCodes = exclusions,
        };
    }

    private static bool TryIsUniformCrestCompatible(
        NetherCodeHardEligibilityEvidence mechanic,
        IReadOnlyList<NetherStrategyPartyMember>? party,
        out bool compatible
    )
    {
        compatible = false;
        NetherCrestIdentity required = mechanic.UniformCrestFamily switch
        {
            NetherCodeFamily.Rush => NetherCrestIdentity.Passion,
            NetherCodeFamily.Impact => NetherCrestIdentity.Impact,
            _ => NetherCrestIdentity.Unknown,
        };
        if (required == NetherCrestIdentity.Unknown || party == null
            || mechanic.UniformCrestTargetRow is not (
                NetherCodeTargetRow.Forward or NetherCodeTargetRow.Back or NetherCodeTargetRow.All
            ))
        {
            return false;
        }

        NetherStrategyPartyMember[] recipients = party
            .Where(member => member != null && member.IsAlive)
            .Where(member => mechanic.UniformCrestTargetRow switch
            {
                NetherCodeTargetRow.Forward => member.PartyPosition == NetherPartyPosition.Forward,
                NetherCodeTargetRow.Back => member.PartyPosition == NetherPartyPosition.Back,
                NetherCodeTargetRow.All => member.PartyPosition is NetherPartyPosition.Forward
                    or NetherPartyPosition.Back or NetherPartyPosition.Assist,
                _ => false,
            })
            .ToArray();
        compatible = recipients.Length > 0 && recipients.All(member => member.Crest == required);
        return true;
    }
}

/// <summary>
/// Prevalidated Recovery-only authority. The deterministic Recovery branch owns proof that rest
/// and purification have zero actual value; Code policy owns hard-exclusion classification. The
/// server-random transform policy consumes those facts but never reconstructs them from display
/// power or category counts.
/// </summary>
internal sealed record NetherCodeTransformEligibilityEvidence
{
    public bool IsKnown { get; init; } = true;
    public string UnknownReason { get; init; } = string.Empty;
    public NetherStrategyMode StrategyMode { get; init; } = NetherStrategyMode.Equipment;
    public bool EquipmentOptInEnabled { get; init; }
    public bool IsRecovery { get; init; }
    public bool DeterministicRecoveryChoicesHaveZeroValue { get; init; }
    public IReadOnlyList<NetherCodeTransformHardExclusion> HardExcludedCodes { get; init; } =
        Array.Empty<NetherCodeTransformHardExclusion>();
}

/// <summary>
/// Chooses the existing code passed to the native target_type=7 conversion flow. The server selects
/// the new Code, so transformation is never an ordinary portfolio upgrade. It is an explicitly
/// enabled Equipment-only last resort which sacrifices only a prevalidated hard-excluded Code.
/// </summary>
internal sealed class NetherCodeTransformPolicy
{
    public NetherCodeTransformDecision Decide(
        IReadOnlyList<NetherCodeState>? codes,
        int capacity,
        NetherCodeTransformEligibilityEvidence? evidence
    )
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

        if (evidence == null || !evidence.IsKnown)
        {
            return Pause(
                NetherPauseReason.UnknownMasterData,
                evidence?.UnknownReason ?? "code-transform-eligibility-evidence-unavailable"
            );
        }
        if (!Enum.IsDefined(typeof(NetherStrategyMode), evidence.StrategyMode))
        {
            return Pause(
                NetherPauseReason.UnknownMasterData,
                "invalid-code-transform-strategy-mode"
            );
        }
        if (evidence.StrategyMode != NetherStrategyMode.Equipment)
            return Pause(NetherPauseReason.NoSafeRoute, "research-code-transform-rejected");
        if (!evidence.EquipmentOptInEnabled)
            return Pause(NetherPauseReason.NoSafeRoute, "equipment-code-transform-disabled");
        if (!evidence.IsRecovery)
            return Pause(NetherPauseReason.NoSafeRoute, "code-transform-outside-recovery-rejected");
        if (!evidence.DeterministicRecoveryChoicesHaveZeroValue)
        {
            return Pause(
                NetherPauseReason.NoSafeRoute,
                "deterministic-recovery-choice-has-value"
            );
        }

        HashSet<long> ownedIds = codes.Select(code => code.CodeId).ToHashSet();
        NetherCodeTransformHardExclusion[] exclusions = evidence.HardExcludedCodes?
            .ToArray() ?? Array.Empty<NetherCodeTransformHardExclusion>();
        if (exclusions.Any(row => row.CodeId <= 0
                || !ownedIds.Contains(row.CodeId)
                || row.Reason == NetherCodeTransformHardExclusionReason.None
                || !Enum.IsDefined(typeof(NetherCodeTransformHardExclusionReason), row.Reason))
            || exclusions.Select(row => row.CodeId).Distinct().Count() != exclusions.Length)
        {
            return Pause(
                NetherPauseReason.UnknownMasterData,
                "invalid-code-transform-hard-exclusion-evidence"
            );
        }
        NetherCodeTransformHardExclusion? selected = exclusions
            .OrderByDescending(row => row.Reason)
            .ThenBy(row => row.CodeId)
            .Cast<NetherCodeTransformHardExclusion?>()
            .FirstOrDefault();

        return selected == null
            ? new NetherCodeTransformDecision
            {
                PauseReason = NetherPauseReason.NoSafeRoute,
                Detail = "no-removable-code-for-native-transform",
            }
            : new NetherCodeTransformDecision
            {
                CanTransform = true,
                RemoveCodeId = selected.Value.CodeId,
                Detail = "remove-hard-excluded:" + selected.Value.Reason
                    + ":" + selected.Value.CodeId,
            };
    }

    private static NetherCodeTransformDecision Pause(NetherPauseReason reason, string detail) => new()
    {
        PauseReason = reason,
        Detail = detail,
    };
}
