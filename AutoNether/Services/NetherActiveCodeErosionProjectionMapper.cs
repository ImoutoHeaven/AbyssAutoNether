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
/// All raw effect fields from one <c>MNetherCodes</c> master row. Parameter two and three are
/// deliberately retained rather than discarded: the native addition effects use the exposed
/// first parameter, while any additional parameter makes that shape non-canonical and fails closed.
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
/// effect is active only when native GetCategoryCount reaches <see cref="Counter"/>. That count
/// is max(0, cards in this category - cards in its paired category).
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
/// and every category-skill threshold for the current Nether. Effect types 1/2 are confirmed
/// non-erosion inputs and stay in the fingerprint. The current native enum confines erosion
/// identities to 6–9, while raw type 12 remains a generic effect model; type 12 is therefore an
/// opaque non-erosion input here and retains every raw parameter in the fingerprint. The native
/// enum and category-effect model directly identify types 6/7 as addition up/down and expose p1
/// as Parameter1, so the canonical p1/0/0 shape maps to an integer addition modifier. Types 8/9
/// remain unavailable because the current client does not expose their rate unit contract. No
/// code ID, including 30024 or 40024, has a special erosion meaning here.
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
            if (includeCategorySkills
                && (master.NetherId != activeNetherId
                    || !Enum.IsDefined(typeof(NetherCodeCategory), master.Category)
                    || master.Category == (int)NetherCodeCategory.Unknown))
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
                // Confirmed ordinary/party ability effects and the current client's opaque raw
                // type 12 retain every raw value in the entry/hash, but are not erosion effects.
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
            Dictionary<NetherCodeCategory, int> categoryCounts = entries
                .GroupBy(entry => (NetherCodeCategory)entry.Category)
                .ToDictionary(group => group.Key, group => group.Count());
            var skillIds = new HashSet<long>();
            foreach (NetherCodeCategoryErosionMasterInput skill in categorySkills!
                .Where(skill => skill.NetherId == activeNetherId)
                .OrderBy(skill => skill.SkillId))
            {
                if (!skill.HasRequiredFields
                    || skill.SkillId <= 0
                    || skill.Counter <= 0
                    || !Enum.IsDefined(typeof(NetherCodeCategory), skill.Category)
                    || skill.Category == (int)NetherCodeCategory.Unknown
                    || !skillIds.Add(skill.SkillId))
                {
                    return Unknown("invalid-or-duplicate-m-nether-code-category-skill:" + skill.SkillId);
                }

                NetherCodeCategory category = (NetherCodeCategory)skill.Category;
                NetherCodeCategory paired = NetherCodeCategorySemantics.GetPairedCategory(category);
                categoryCounts.TryGetValue(category, out int ownCount);
                categoryCounts.TryGetValue(paired, out int pairedCount);
                int effectiveCount = Math.Max(0, ownCount - pairedCount);
                bool isActive = effectiveCount >= skill.Counter;
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
        if (effectType is 8 or 9)
        {
            error = DescribeUnavailableEffect(
                "service-authoritative-nether-code-erosion-rate",
                sourceId,
                effectType,
                effectParameter1,
                effectParameter2,
                effectParameter3
            );
            return false;
        }
        if (effectType is not (6 or 7))
        {
            error = "unknown-nether-code-effect-type:" + effectType;
            return false;
        }
        if (effectParameter1 is < 0 or > int.MaxValue
            || effectParameter2 != 0
            || effectParameter3 != 0)
        {
            error = DescribeUnavailableEffect(
                "unsupported-nether-code-erosion-addition-shape",
                sourceId,
                effectType,
                effectParameter1,
                effectParameter2,
                effectParameter3
            );
            return false;
        }

        NetherCodeEffectKind effectKind = effectType == 6
            ? NetherCodeEffectKind.ErosionAdditionUp
            : NetherCodeEffectKind.ErosionAdditionDown;
        effect = new NetherCodeEffect(sourceId, effectKind, (int)effectParameter1);
        error = string.Empty;
        return true;
    }

    private static string DescribeUnavailableEffect(
        string reason,
        long sourceId,
        int effectType,
        long effectParameter1,
        long effectParameter2,
        long effectParameter3
    ) => reason
        + ":type="
        + effectType
        + ":source="
        + sourceId
        + ":p1="
        + effectParameter1
        + ":p2="
        + effectParameter2
        + ":p3="
        + effectParameter3;

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
