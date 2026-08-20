#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Resolves the operational Research target without inventing a pre-settlement point total.
/// A full authoritative wallet proves completion by itself. When the wallet is below the native
/// threshold and the future normal-settlement result is unavailable, the configured family stays
/// active conservatively; it is never marked complete or skipped for Equipment ordering.
/// </summary>
internal static class NetherResearchObjectivePolicy
{
    public const int CompletionPoints = 20_000;

    public static NetherResearchObjectiveResolution Resolve(
        NetherCodeFamily primary,
        NetherCodeFamily secondary,
        IReadOnlyList<NetherStrategyResearchFamilyState>? families
    )
    {
        if (families == null)
            return NetherResearchObjectiveResolution.Invalid("research-family-evidence-unavailable");

        NetherCodeFamily[] configured = new[] { primary, secondary }
            .Where(family => family != NetherCodeFamily.Unknown)
            .Distinct()
            .ToArray();
        var rows = new Dictionary<NetherCodeFamily, NetherStrategyResearchFamilyState>();
        foreach (NetherCodeFamily family in configured)
        {
            NetherStrategyResearchFamilyState[] matches = families
                .Where(row => row.Family == family)
                .ToArray();
            if (matches.Length != 1)
            {
                return NetherResearchObjectiveResolution.Invalid(
                    "research-target-family-row-unavailable:" + family
                );
            }
            rows[family] = matches[0];
        }

        foreach (NetherCodeFamily family in new[] { primary, secondary })
        {
            if (family == NetherCodeFamily.Unknown)
                continue;

            NetherStrategyResearchFamilyState row = rows[family];
            if (IsComplete(row))
                continue;

            if (!row.IsProjectedNormalSettlementKnown)
            {
                return NetherResearchObjectiveResolution.Active(
                    family,
                    usesConservativePriority: true,
                    string.IsNullOrWhiteSpace(row.ProjectionUnknownReason)
                        ? "research-completion-projection-unknown"
                        : row.ProjectionUnknownReason
                );
            }

            return NetherResearchObjectiveResolution.Active(
                family,
                usesConservativePriority: false,
                string.Empty
            );
        }

        return NetherResearchObjectiveResolution.Complete();
    }

    public static bool? ToRouteIncomplete(NetherStrategyEvidenceAudit? audit)
    {
        if (audit == null)
            return null;
        if (audit.Mode == NetherStrategyMode.Equipment)
            return false;
        if (audit.Mode != NetherStrategyMode.Research)
            return null;
        return audit.ResearchTargetState switch
        {
            NetherResearchTargetState.Active => true,
            NetherResearchTargetState.Complete => false,
            _ => null,
        };
    }

    public static bool IsFamilyComplete(
        NetherCodeFamily family,
        IReadOnlyList<NetherStrategyResearchFamilyState>? families
    )
    {
        if (family == NetherCodeFamily.Unknown)
            return true;
        if (families == null)
            return false;
        NetherStrategyResearchFamilyState[] matches = families
            .Where(row => row.Family == family)
            .ToArray();
        return matches.Length == 1 && IsComplete(matches[0]);
    }

    private static bool IsComplete(NetherStrategyResearchFamilyState row) =>
        row.WalletPoints >= CompletionPoints
        || row.IsProjectedNormalSettlementKnown
            && (long)row.WalletPoints + row.ProjectedNormalSettlementPoints >= CompletionPoints;
}

internal sealed record NetherResearchObjectiveResolution
{
    public bool IsValid { get; init; }
    public NetherCodeFamily ActiveFamily { get; init; } = NetherCodeFamily.Unknown;
    public bool HasIncompleteTargets { get; init; }
    public bool UsesConservativePriority { get; init; }
    public string Detail { get; init; } = string.Empty;

    public static NetherResearchObjectiveResolution Invalid(string detail) => new()
    {
        Detail = string.IsNullOrWhiteSpace(detail)
            ? "research-objective-evidence-unavailable"
            : detail,
    };

    public static NetherResearchObjectiveResolution Active(
        NetherCodeFamily family,
        bool usesConservativePriority,
        string detail
    ) => new()
    {
        IsValid = family != NetherCodeFamily.Unknown,
        ActiveFamily = family,
        HasIncompleteTargets = family != NetherCodeFamily.Unknown,
        UsesConservativePriority = usesConservativePriority,
        Detail = detail ?? string.Empty,
    };

    public static NetherResearchObjectiveResolution Complete() => new()
    {
        IsValid = true,
    };
}
