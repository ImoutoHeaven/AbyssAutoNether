#nullable enable

using System;
using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// The authoritative HP surface carried by one live <c>NetherPartyCharacterModel</c>.  The
/// native model updates <see cref="HpRatio"/> from the server's <c>current_hp_ratio</c>; it is
/// intentionally not reconstructed from a guessed current/max pair.
/// </summary>
internal readonly record struct NetherActiveBattleMemberHp(
    long CharacterId,
    double HpRatio,
    bool IsAlive
);

/// <summary>One exact current-living row after checked native ratio conversion.</summary>
internal readonly record struct NetherActiveLivingMemberHp(
    long CharacterId,
    int HpPermille
);

/// <summary>
/// The lowest authoritative party HP fraction, or an explicit unknown result.  A nullable
/// value prevents callers from silently treating an unavailable health contract as full health.
/// </summary>
internal readonly record struct NetherActivePartyHpSafety(
    bool IsKnown,
    int? MinimumHpPermille,
    string Detail
)
{
    /// <summary>
    /// Deterministically CharacterId-sorted immutable current-living rows.  Aggregate minimum HP
    /// is insufficient authority because a different surviving roster can have the same value.
    /// </summary>
    public IReadOnlyList<NetherActiveLivingMemberHp>? LivingMembers { get; init; } =
        Array.Empty<NetherActiveLivingMemberHp>();
}

/// <summary>
/// Canonical conversion for native <c>System.Single HpRatio</c>. The game/server surface is
/// permille-shaped but binary Single values such as <c>0.299f</c> are not exact in Double. Round
/// midpoint away from zero, matching the authoritative snapshot convention, so every capture
/// path reconstructs the same integer without flooring a representation artifact.
/// </summary>
internal static class NetherHpRatioPermilleQuantizer
{
    public static bool TryQuantize(double hpRatio, out int hpPermille)
    {
        hpPermille = 0;
        if (double.IsNaN(hpRatio)
            || double.IsInfinity(hpRatio)
            || hpRatio is < 0d or > 1d)
        {
            return false;
        }

        try
        {
            hpPermille = checked((int)Math.Round(
                checked(hpRatio * 1000d),
                MidpointRounding.AwayFromZero
            ));
            return hpPermille is >= 0 and <= 1000;
        }
        catch (OverflowException)
        {
            hpPermille = 0;
            return false;
        }
    }
}

/// <summary>
/// Converts the current living party characters' authoritative HP ratios to the strict lowest
/// permille used by the battle route gate.  The native roster retains dead members with
/// <c>IsAlive=false</c>, so their zero HP is validated but excluded from the current-living
/// minimum. Any incomplete, duplicate, non-finite, out-of-range, or all-dead observation is
/// unsafe.
/// </summary>
internal sealed class NetherActivePartyHpSafetyMapper
{
    public NetherActivePartyHpSafety Map(IReadOnlyList<NetherActiveBattleMemberHp>? members)
    {
        if (members == null || members.Count == 0)
            return Unknown("empty-active-party-character-models");

        var characterIds = new HashSet<long>();
        var livingMembers = new List<NetherActiveLivingMemberHp>();
        int minimumPermille = 1000;

        foreach (NetherActiveBattleMemberHp member in members)
        {
            if (member.CharacterId <= 0)
                return Unknown("invalid-nether-party-character-id");
            if (!characterIds.Add(member.CharacterId))
                return Unknown("duplicate-nether-party-character-id:" + member.CharacterId);
            if (double.IsNaN(member.HpRatio) || double.IsInfinity(member.HpRatio))
                return Unknown("non-finite-nether-party-hp-ratio:" + member.CharacterId);
            if (member.HpRatio is < 0d or > 1d)
                return Unknown("out-of-range-nether-party-hp-ratio:" + member.CharacterId);

            if (!NetherHpRatioPermilleQuantizer.TryQuantize(
                    member.HpRatio,
                    out int permille
                ))
            {
                return Unknown("invalid-nether-party-hp-permille:" + member.CharacterId);
            }
            if (member.IsAlive)
            {
                livingMembers.Add(new NetherActiveLivingMemberHp(member.CharacterId, permille));
                minimumPermille = Math.Min(minimumPermille, permille);
            }
        }

        if (livingMembers.Count == 0)
            return Unknown("empty-living-nether-party");

        livingMembers.Sort(static (left, right) => left.CharacterId.CompareTo(right.CharacterId));
        return new NetherActivePartyHpSafety(true, minimumPermille, string.Empty)
        {
            LivingMembers = Array.AsReadOnly(livingMembers.ToArray()),
        };
    }

    internal static NetherActivePartyHpSafety Unknown(string detail) => new(
        IsKnown: false,
        MinimumHpPermille: null,
        Detail: detail
    );
}
