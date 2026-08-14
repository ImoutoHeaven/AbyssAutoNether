#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// One canonical identity for authoritative code state and offer candidates.  Every capture,
/// transition and reconciliation seam must compare the same proven fields so an unchanged
/// portfolio cannot appear to mutate merely because it was read through another adapter.
/// </summary>
internal static class NetherCodeIdentity
{
    private const string Version = "code-v2:";

    public static string CreatePortfolio(IEnumerable<NetherCodeState> codes) =>
        Version + string.Join(";", codes
            .Select(Create)
            .OrderBy(identity => identity, System.StringComparer.Ordinal));

    public static string CreateCandidates(IEnumerable<NetherCodeCandidate> candidates) =>
        Version + string.Join(";", candidates
            .Select(Create)
            .OrderBy(identity => identity, System.StringComparer.Ordinal));

    internal static string Create(NetherCodeState code) => string.Join(
        ":",
        Format(code.CodeId),
        Format(code.PossessionAmount),
        Bool(code.IsKnown),
        Bool(code.EffectSemanticsKnown),
        Format((int)code.Category),
        Format((int)code.Family),
        Format(code.Rarity),
        Format(code.Power),
        Format((int)code.MasterEffectType),
        Format(code.EffectParameter1),
        Format(code.EffectParameter2),
        Format(code.EffectParameter3),
        Format(code.AbilityAssetId),
        Format(code.AbilityLevel),
        Bool(code.PartyCoverageKnown),
        Format(code.PartyCoverage)
    );

    internal static string Create(NetherCodeCandidate code) => string.Join(
        ":",
        Format(code.CodeId),
        Bool(code.IsKnown),
        Bool(code.EffectSemanticsKnown),
        Format((int)code.Category),
        Format((int)code.Family),
        Format(code.Rarity),
        Format(code.Power),
        Format((int)code.MasterEffectType),
        Format(code.EffectParameter1),
        Format(code.EffectParameter2),
        Format(code.EffectParameter3),
        Format(code.AbilityAssetId),
        Format(code.AbilityLevel),
        Bool(code.PartyCoverageKnown),
        Format(code.PartyCoverage)
    );

    private static string Format(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "1" : "0";
}
