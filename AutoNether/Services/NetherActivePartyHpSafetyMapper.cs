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

/// <summary>
/// The lowest authoritative party HP fraction, or an explicit unknown result.  A nullable
/// value prevents callers from silently treating an unavailable health contract as full health.
/// </summary>
internal readonly record struct NetherActivePartyHpSafety(
    bool IsKnown,
    int? MinimumHpPermille,
    string Detail
);

/// <summary>
/// Reverses the packaged client's authoritative HP conversion: the server supplies an integer
/// permille, then <c>NumericsUtility.PerMilleToFloat</c> stores it as a <see cref="float"/> ratio.
/// Only the small representation error introduced by that Single conversion is accepted.
/// </summary>
internal static class NetherNativeHpPermille
{
    private const double MaximumSingleEncodingErrorPermille = 0.001d;

    public static bool TryDecode(double hpRatio, out int hpPermille)
    {
        hpPermille = 0;
        if (double.IsNaN(hpRatio) || double.IsInfinity(hpRatio) || hpRatio is < 0d or > 1d)
            return false;

        double scaled = hpRatio * 1000d;
        double rounded = Math.Round(scaled, MidpointRounding.AwayFromZero);
        if (rounded is < 0d or > 1000d
            || Math.Abs(scaled - rounded) > MaximumSingleEncodingErrorPermille)
        {
            return false;
        }

        hpPermille = (int)rounded;
        return true;
    }
}

/// <summary>
/// Converts every living party character's authoritative HP ratio back to the server permille
/// used by the battle route gate. The native <c>IsAlive</c> getter is exactly
/// <c>HpRatio &gt; 0</c>; zero-HP roster slots remain in <c>CharacterModels</c> but are not active
/// combatants. Any incomplete, duplicate, contradictory, non-finite, or out-of-range observation
/// is unsafe.
/// </summary>
internal sealed class NetherActivePartyHpSafetyMapper
{
    public NetherActivePartyHpSafety Map(IReadOnlyList<NetherActiveBattleMemberHp>? members)
    {
        if (members == null || members.Count == 0)
            return Unknown("empty-active-party-character-models");

        var characterIds = new HashSet<long>();
        int minimumPermille = 1000;
        int livingCount = 0;

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
            if (member.IsAlive != (member.HpRatio > 0d))
                return Unknown("contradictory-nether-party-alive-state:" + member.CharacterId);
            if (!NetherNativeHpPermille.TryDecode(member.HpRatio, out int permille))
                return Unknown("invalid-nether-party-hp-permille:" + member.CharacterId);

            if (!member.IsAlive)
                continue;

            minimumPermille = Math.Min(minimumPermille, permille);
            livingCount++;
        }

        if (livingCount == 0)
            return Unknown("empty-living-nether-party-character-models");

        return new NetherActivePartyHpSafety(true, minimumPermille, string.Empty);
    }

    internal static NetherActivePartyHpSafety Unknown(string detail) => new(
        IsKnown: false,
        MinimumHpPermille: null,
        Detail: detail
    );
}
