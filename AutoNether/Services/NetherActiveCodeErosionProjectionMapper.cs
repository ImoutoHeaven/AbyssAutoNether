#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Raw ownership fields copied from a live <c>NetherCodeData</c>.  Amount is retained in the
/// fingerprint even though the current erosion master mapping consumes the master parameter,
/// so a server-side ownership change can never reuse a stale projection identity.
/// </summary>
internal readonly record struct NetherPossessionCodeErosionInput(long CodeId, long Amount)
{
    public bool HasRequiredFields { get; init; } = true;
}

/// <summary>
/// All raw effect fields from one <c>MNetherCodes</c> master row.  Parameter two and three are
/// deliberately retained rather than discarded: a non-zero value on a 6–9 erosion effect is
/// not currently representable by the one-amount erosion policy and therefore fails closed.
/// </summary>
internal readonly record struct NetherCodeErosionMasterInput(
    long CodeId,
    int EffectType,
    long EffectParameter1,
    long EffectParameter2,
    long EffectParameter3
)
{
    public bool HasRequiredFields { get; init; } = true;
    public long NetherId { get; init; }
    public int Category { get; init; }
}

/// <summary>
/// Exact erosion-relevant fields from one <c>MNetherCodeCategorySkills</c> row.  A category
/// effect is active only when the current portfolio contains at least <see cref="Counter"/>
/// distinct codes in that category for the current Nether.
/// </summary>
internal readonly record struct NetherCodeCategoryErosionMasterInput(
    long SkillId,
    long NetherId,
    int Counter,
    int Category,
    int EffectType,
    long EffectParameter1,
    long EffectParameter2,
    long EffectParameter3
)
{
    public bool HasRequiredFields { get; init; } = true;
}

/// <summary>
/// A sorted, authoritative code/master join retained with the projection so diagnostics and
/// future policy versions can distinguish a code-ID-only match from a complete parameter match.
/// </summary>
internal readonly record struct NetherActiveCodeErosionEntry(
    long CodeId,
    long PossessionAmount,
    int EffectType,
    long EffectParameter1,
    long EffectParameter2,
    long EffectParameter3
)
{
    public long NetherId { get; init; }
    public int Category { get; init; }
}

internal readonly record struct NetherActiveCodeCategoryErosionEntry(
    long SkillId,
    long NetherId,
    int Counter,
    int Category,
    int EffectType,
    long EffectParameter1,
    long EffectParameter2,
    long EffectParameter3,
    bool IsActive
);

/// <summary>
/// Result of mapping the current possession portfolio to erosion modifiers.  The projection is
/// usable only when every active code/master relationship is unambiguous and understood.
/// </summary>
internal sealed record NetherActiveCodeErosionProjection
{
    public bool ErosionProjectionKnown { get; init; }
    public IReadOnlyList<long> SortedCodeIds { get; init; } = Array.Empty<long>();
    public IReadOnlyList<NetherActiveCodeErosionEntry> Entries { get; init; } =
        Array.Empty<NetherActiveCodeErosionEntry>();
    public IReadOnlyList<NetherActiveCodeCategoryErosionEntry> CategorySkillEntries { get; init; } =
        Array.Empty<NetherActiveCodeCategoryErosionEntry>();
    public IReadOnlyList<NetherCodeEffect> ErosionEffects { get; init; } = Array.Empty<NetherCodeEffect>();
    public string CodeHash { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// Builds a fail-closed erosion projection from live possession codes, their exact master rows,
/// and every category-skill threshold for the current Nether. Effect types 1/2/12 are confirmed
/// non-erosion inputs: they stay in the fingerprint but produce no modifier. Types 6–9 map
/// directly to the existing <see cref="NetherCodeEffect"/> model. No code ID, including 30024 or
/// 40024, has a special erosion meaning here.
/// </summary>
internal sealed class NetherActiveCodeErosionProjectionMapper
{
    public NetherActiveCodeErosionProjection Map(
        IReadOnlyList<NetherPossessionCodeErosionInput>? possessions,
        IReadOnlyList<NetherCodeErosionMasterInput>? masters
    ) => MapCore(possessions, masters, null, 0, includeCategorySkills: false);

    public NetherActiveCodeErosionProjection Map(
        IReadOnlyList<NetherPossessionCodeErosionInput>? possessions,
        IReadOnlyList<NetherCodeErosionMasterInput>? masters,
        IReadOnlyList<NetherCodeCategoryErosionMasterInput>? categorySkills,
        long activeNetherId
    ) => MapCore(possessions, masters, categorySkills, activeNetherId, includeCategorySkills: true);

    private static NetherActiveCodeErosionProjection MapCore(
        IReadOnlyList<NetherPossessionCodeErosionInput>? possessions,
        IReadOnlyList<NetherCodeErosionMasterInput>? masters,
        IReadOnlyList<NetherCodeCategoryErosionMasterInput>? categorySkills,
        long activeNetherId,
        bool includeCategorySkills
    )
    {
        if (possessions == null)
            return Unknown("missing-possession-nether-codes");
        if (possessions.Count == 0)
            return Known(
                Array.Empty<NetherActiveCodeErosionEntry>(),
                Array.Empty<NetherCodeEffect>(),
                Array.Empty<NetherActiveCodeCategoryErosionEntry>()
            );
        if (masters == null)
            return Unknown("missing-m-nether-codes");
        if (includeCategorySkills && activeNetherId <= 0)
            return Unknown("invalid-active-nether-id");
        if (includeCategorySkills && categorySkills == null)
            return Unknown("missing-m-nether-code-category-skills");

        var possessionById = new Dictionary<long, NetherPossessionCodeErosionInput>();
        foreach (NetherPossessionCodeErosionInput possession in possessions)
        {
            if (!possession.HasRequiredFields || possession.CodeId <= 0 || possession.Amount < 0)
                return Unknown("invalid-possession-nether-code");
            if (!possessionById.TryAdd(possession.CodeId, possession))
                return Unknown("duplicate-possession-nether-code:" + possession.CodeId);
        }

        var mastersByActiveCodeId = new Dictionary<long, List<NetherCodeErosionMasterInput>>();
        foreach (NetherCodeErosionMasterInput master in masters)
        {
            if (includeCategorySkills && master.NetherId != activeNetherId)
                continue;
            if (!possessionById.ContainsKey(master.CodeId))
                continue;
            if (!mastersByActiveCodeId.TryGetValue(master.CodeId, out List<NetherCodeErosionMasterInput>? matches))
            {
                matches = new List<NetherCodeErosionMasterInput>();
                mastersByActiveCodeId.Add(master.CodeId, matches);
            }
            matches.Add(master);
        }

        var entries = new List<NetherActiveCodeErosionEntry>(possessionById.Count);
        var effects = new List<NetherCodeEffect>();
        foreach (NetherPossessionCodeErosionInput possession in possessionById.Values.OrderBy(code => code.CodeId))
        {
            if (!mastersByActiveCodeId.TryGetValue(possession.CodeId, out List<NetherCodeErosionMasterInput>? matches)
                || matches.Count == 0)
            {
                return Unknown("missing-m-nether-code:" + possession.CodeId);
            }
            if (matches.Count != 1)
                return Unknown("duplicate-m-nether-code:" + possession.CodeId);

            NetherCodeErosionMasterInput master = matches[0];
            if (!master.HasRequiredFields || master.CodeId != possession.CodeId)
                return Unknown("invalid-m-nether-code:" + possession.CodeId);
            if (includeCategorySkills && (master.NetherId != activeNetherId || master.Category <= 0))
                return Unknown("invalid-m-nether-code-category:" + possession.CodeId);

            entries.Add(new NetherActiveCodeErosionEntry(
                possession.CodeId,
                possession.Amount,
                master.EffectType,
                master.EffectParameter1,
                master.EffectParameter2,
                master.EffectParameter3
            )
            {
                NetherId = master.NetherId,
                Category = master.Category,
            });

            switch (master.EffectType)
            {
                // Confirmed ordinary/party effects and category research-point rewards: their
                // raw values remain in the entry/hash, but they do not alter battle erosion.
                case 1:
                case 2:
                case 12:
                    break;
                case 6:
                case 7:
                case 8:
                case 9:
                    if (!TryMapErosionEffect(
                            master.CodeId,
                            master.EffectType,
                            master.EffectParameter1,
                            master.EffectParameter2,
                            master.EffectParameter3,
                            out NetherCodeEffect effect,
                            out string error
                        ))
                        return Unknown(error + ":" + possession.CodeId);
                    effects.Add(effect);
                    break;
                default:
                    return Unknown("unknown-nether-code-effect-type:" + master.EffectType);
            }
        }

        var categoryEntries = new List<NetherActiveCodeCategoryErosionEntry>();
        if (includeCategorySkills)
        {
            Dictionary<int, int> categoryCounts = entries
                .GroupBy(entry => entry.Category)
                .ToDictionary(group => group.Key, group => group.Count());
            var skillIds = new HashSet<long>();
            foreach (NetherCodeCategoryErosionMasterInput skill in categorySkills!
                .Where(skill => skill.NetherId == activeNetherId)
                .OrderBy(skill => skill.SkillId))
            {
                if (!skill.HasRequiredFields
                    || skill.SkillId <= 0
                    || skill.Counter <= 0
                    || skill.Category <= 0
                    || !skillIds.Add(skill.SkillId))
                {
                    return Unknown("invalid-or-duplicate-m-nether-code-category-skill:" + skill.SkillId);
                }

                bool isActive = categoryCounts.TryGetValue(skill.Category, out int count)
                    && count >= skill.Counter;
                categoryEntries.Add(new NetherActiveCodeCategoryErosionEntry(
                    skill.SkillId,
                    skill.NetherId,
                    skill.Counter,
                    skill.Category,
                    skill.EffectType,
                    skill.EffectParameter1,
                    skill.EffectParameter2,
                    skill.EffectParameter3,
                    isActive
                ));
                if (!isActive)
                    continue;

                switch (skill.EffectType)
                {
                    // Current category combat abilities use type 1 and do not alter erosion.
                    case 1:
                    case 2:
                    case 12:
                        break;
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                        if (!TryMapErosionEffect(
                                skill.SkillId,
                                skill.EffectType,
                                skill.EffectParameter1,
                                skill.EffectParameter2,
                                skill.EffectParameter3,
                                out NetherCodeEffect effect,
                                out string error
                            ))
                        {
                            return Unknown(error + ":category-skill:" + skill.SkillId);
                        }
                        effects.Add(effect);
                        break;
                    default:
                        return Unknown("unknown-nether-code-category-skill-effect-type:" + skill.EffectType);
                }
            }
            if (categoryEntries.Count == 0)
                return Unknown("missing-active-nether-code-category-skills:" + activeNetherId);
        }

        return Known(entries, effects, categoryEntries);
    }

    private static bool TryMapErosionEffect(
        long sourceId,
        int effectType,
        long effectParameter1,
        long effectParameter2,
        long effectParameter3,
        out NetherCodeEffect effect,
        out string error
    )
    {
        effect = default;
        error = string.Empty;
        if (effectParameter1 is <= 0 or > int.MaxValue)
        {
            error = "invalid-nether-code-effect-parameter-1";
            return false;
        }
        // Parameters two and three cannot be projected by NetherErosionPolicy's single amount
        // contract.  Treating them as zero would change a native effect, so only the explicitly
        // unparameterized shape is currently safe.
        if (effectParameter2 != 0 || effectParameter3 != 0)
        {
            error = "unprojectable-nether-code-effect-parameter-2-or-3";
            return false;
        }

        NetherCodeEffectKind kind = effectType switch
        {
            6 => NetherCodeEffectKind.ErosionAdditionUp,
            7 => NetherCodeEffectKind.ErosionAdditionDown,
            8 => NetherCodeEffectKind.ErosionRateUp,
            9 => NetherCodeEffectKind.ErosionRateDown,
            _ => NetherCodeEffectKind.Unknown,
        };
        if (kind == NetherCodeEffectKind.Unknown)
        {
            error = "unknown-nether-code-effect-type";
            return false;
        }

        effect = new NetherCodeEffect(
            sourceId,
            kind,
            checked((int)effectParameter1)
        )
        {
            IsKnown = true,
            OrderKnown = true,
        };
        return true;
    }

    private static NetherActiveCodeErosionProjection Known(
        IReadOnlyList<NetherActiveCodeErosionEntry> entries,
        IReadOnlyList<NetherCodeEffect> effects,
        IReadOnlyList<NetherActiveCodeCategoryErosionEntry> categoryEntries
    ) => new()
    {
        ErosionProjectionKnown = true,
        SortedCodeIds = entries.Select(entry => entry.CodeId).ToArray(),
        Entries = entries,
        CategorySkillEntries = categoryEntries,
        ErosionEffects = effects,
        CodeHash = CreateCodeHash(entries, categoryEntries),
        Detail = string.Empty,
    };

    internal static NetherActiveCodeErosionProjection Unknown(string detail) => new()
    {
        ErosionProjectionKnown = false,
        SortedCodeIds = Array.Empty<long>(),
        Entries = Array.Empty<NetherActiveCodeErosionEntry>(),
        CategorySkillEntries = Array.Empty<NetherActiveCodeCategoryErosionEntry>(),
        ErosionEffects = Array.Empty<NetherCodeEffect>(),
        CodeHash = "nether-codes:unknown",
        Detail = detail,
    };

    private static string CreateCodeHash(
        IReadOnlyList<NetherActiveCodeErosionEntry> entries,
        IReadOnlyList<NetherActiveCodeCategoryErosionEntry> categoryEntries
    )
    {
        if (entries.Count == 0)
            return "nether-codes:none";
        string codes = string.Join(
            ";",
            entries.Select(entry => string.Join(
                ":",
                entry.CodeId.ToString(CultureInfo.InvariantCulture),
                entry.PossessionAmount.ToString(CultureInfo.InvariantCulture),
                entry.EffectType.ToString(CultureInfo.InvariantCulture),
                entry.EffectParameter1.ToString(CultureInfo.InvariantCulture),
                entry.EffectParameter2.ToString(CultureInfo.InvariantCulture),
                entry.EffectParameter3.ToString(CultureInfo.InvariantCulture),
                "n" + entry.NetherId.ToString(CultureInfo.InvariantCulture),
                "c" + entry.Category.ToString(CultureInfo.InvariantCulture)
            ))
        );
        if (categoryEntries.Count == 0)
            return codes;
        string skills = string.Join(
            ";",
            categoryEntries.Select(entry => string.Join(
                ":",
                entry.SkillId.ToString(CultureInfo.InvariantCulture),
                entry.NetherId.ToString(CultureInfo.InvariantCulture),
                entry.Counter.ToString(CultureInfo.InvariantCulture),
                entry.Category.ToString(CultureInfo.InvariantCulture),
                entry.EffectType.ToString(CultureInfo.InvariantCulture),
                entry.EffectParameter1.ToString(CultureInfo.InvariantCulture),
                entry.EffectParameter2.ToString(CultureInfo.InvariantCulture),
                entry.EffectParameter3.ToString(CultureInfo.InvariantCulture),
                entry.IsActive ? "1" : "0"
            ))
        );
        return codes + "|category-skills:" + skills;
    }
}
