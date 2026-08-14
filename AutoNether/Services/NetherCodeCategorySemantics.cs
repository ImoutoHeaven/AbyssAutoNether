#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// A category-derived semantic copied from MNetherCodes.category.  It intentionally separates
/// what the packaged enum and NetherText table prove (category, group, displayed family, pair)
/// from ability semantics they do not prove (party coverage and trigger suitability).
/// </summary>
internal readonly record struct NetherCodeMasterSemantic(
    NetherCodeCategory Category,
    NetherCodeCategoryGroup Group,
    NetherCodeCategory PairedCategory,
    NetherCodeFamily Family,
    bool IsKnown
);

/// <summary>
/// Exact translation of Project.NetherCodeCategoryTypeExtensions:
/// Native Technique/Strength are displayed as Rush/Impact and pair with each other;
/// ErosionResistance/ErosionEnhancement are displayed as Safe/Risk and pair with each other.
/// Pairing affects GetCategoryRawCount; it does not prohibit both cards from coexisting.
/// </summary>
internal static class NetherCodeCategorySemantics
{
    public static NetherCodeMasterSemantic Resolve(long codeId, int rawCategory)
    {
        // MNetherCodes.category and effect_type are independent native axes. CreateModel
        // constructs a category-bearing NetherCodeEffectModel even for effect values this
        // plugin has not decoded, so an unknown effect must never erase a proven colour.
        if (codeId <= 0)
            return Unknown();

        if (!Enum.IsDefined(typeof(NetherCodeCategory), rawCategory)
            || rawCategory == (int)NetherCodeCategory.Unknown)
        {
            return Unknown();
        }

        NetherCodeCategory category = (NetherCodeCategory)rawCategory;
        return category switch
        {
            NetherCodeCategory.Rush => Known(
                category,
                NetherCodeCategoryGroup.Tactics,
                NetherCodeCategory.Impact,
                NetherCodeFamily.Rush
            ),
            NetherCodeCategory.Impact => Known(
                category,
                NetherCodeCategoryGroup.Tactics,
                NetherCodeCategory.Rush,
                NetherCodeFamily.Impact
            ),
            NetherCodeCategory.Safe => Known(
                category,
                NetherCodeCategoryGroup.Erosion,
                NetherCodeCategory.Risk,
                NetherCodeFamily.Safe
            ),
            NetherCodeCategory.Risk => Known(
                category,
                NetherCodeCategoryGroup.Erosion,
                NetherCodeCategory.Safe,
                NetherCodeFamily.Risk
            ),
            _ => Unknown(),
        };
    }

    public static NetherCodeCategory GetPairedCategory(NetherCodeCategory category) => category switch
    {
        NetherCodeCategory.Rush => NetherCodeCategory.Impact,
        NetherCodeCategory.Impact => NetherCodeCategory.Rush,
        NetherCodeCategory.Safe => NetherCodeCategory.Risk,
        NetherCodeCategory.Risk => NetherCodeCategory.Safe,
        _ => NetherCodeCategory.Unknown,
    };

    public static NetherCodeCategoryGroup GetGroup(NetherCodeCategory category) => category switch
    {
        NetherCodeCategory.Rush or NetherCodeCategory.Impact => NetherCodeCategoryGroup.Tactics,
        NetherCodeCategory.Safe or NetherCodeCategory.Risk => NetherCodeCategoryGroup.Erosion,
        _ => NetherCodeCategoryGroup.Unknown,
    };

    /// <summary>
    /// Canonical player-facing family label. Enum.ToString() is not stable for duplicate-valued
    /// native aliases such as Rush/Technique, so diagnostics must format the proven display
    /// concept explicitly.
    /// </summary>
    public static string GetDisplayName(NetherCodeCategory category) => category switch
    {
        NetherCodeCategory.Rush => nameof(NetherCodeFamily.Rush),
        NetherCodeCategory.Impact => nameof(NetherCodeFamily.Impact),
        NetherCodeCategory.Safe => nameof(NetherCodeFamily.Safe),
        NetherCodeCategory.Risk => nameof(NetherCodeFamily.Risk),
        _ => nameof(NetherCodeFamily.Unknown),
    };

    public static bool ArePairedCounterCategories(NetherCodeCategory left, NetherCodeCategory right)
    {
        if (left == NetherCodeCategory.Unknown || right == NetherCodeCategory.Unknown || left == right)
            return false;
        NetherCodeCategoryGroup group = GetGroup(left);
        return group != NetherCodeCategoryGroup.Unknown && group == GetGroup(right);
    }

    private static NetherCodeMasterSemantic Known(
        NetherCodeCategory category,
        NetherCodeCategoryGroup group,
        NetherCodeCategory paired,
        NetherCodeFamily family
    ) => new(category, group, paired, family, true);

    public static bool IsKnownEffectType(int effectType) => effectType is
        (int)NetherCodeMasterEffectType.NetherAbility
        or (int)NetherCodeMasterEffectType.CommonAbility
        or (int)NetherCodeMasterEffectType.ErosionAdditionUp
        or (int)NetherCodeMasterEffectType.ErosionAdditionDown
        or (int)NetherCodeMasterEffectType.ErosionRateUp
        or (int)NetherCodeMasterEffectType.ErosionRateDown;

    private static NetherCodeMasterSemantic Unknown() => new(
        NetherCodeCategory.Unknown,
        NetherCodeCategoryGroup.Unknown,
        NetherCodeCategory.Unknown,
        NetherCodeFamily.Unknown,
        false
    );
}
