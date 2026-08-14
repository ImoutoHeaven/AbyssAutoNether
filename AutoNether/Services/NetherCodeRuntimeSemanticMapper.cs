#nullable enable

namespace AutoNether.Services;

/// <summary>
/// Decodes the independent category and effect-type axes of one MNetherCodes row.  Native
/// CreateModel uses p1 as the ability asset ID and p2 as its level for effect types 1/2.
/// Unpublished raw effect values remain unknown rather than receiving inferred semantics.
/// </summary>
internal static class NetherCodeRuntimeSemanticMapper
{
    public static NetherCodeCandidate MapCandidate(
        long codeId,
        int rawCategory,
        int effectType,
        long effectParameter1,
        long effectParameter2,
        long effectParameter3,
        int rarity,
        int power
    )
    {
        NetherCodeMasterSemantic semantic = NetherCodeCategorySemantics.Resolve(
            codeId,
            rawCategory
        );
        DecodeEffect(
            effectType,
            effectParameter1,
            effectParameter2,
            out int abilityLevel,
            out long abilityAssetId,
            out bool effectShapeKnown
        );
        return new NetherCodeCandidate(codeId, semantic.Family, abilityLevel)
        {
            IsKnown = semantic.IsKnown,
            EffectSemanticsKnown = effectShapeKnown,
            Category = semantic.Category,
            Rarity = rarity,
            Power = power,
            MasterEffectType = (NetherCodeMasterEffectType)effectType,
            EffectParameter1 = effectParameter1,
            EffectParameter2 = effectParameter2,
            EffectParameter3 = effectParameter3,
            AbilityAssetId = abilityAssetId,
            PartyCoverageKnown = false,
            PartyCoverage = 0,
        };
    }

    public static NetherCodeState MapState(
        long codeId,
        int rawCategory,
        int effectType,
        long effectParameter1,
        long effectParameter2,
        long effectParameter3,
        int rarity,
        int power,
        int possessionAmount
    )
    {
        NetherCodeMasterSemantic semantic = NetherCodeCategorySemantics.Resolve(
            codeId,
            rawCategory
        );
        DecodeEffect(
            effectType,
            effectParameter1,
            effectParameter2,
            out int abilityLevel,
            out long abilityAssetId,
            out bool effectShapeKnown
        );
        return new NetherCodeState(codeId, semantic.Family, abilityLevel)
        {
            IsKnown = semantic.IsKnown && possessionAmount >= 0,
            EffectSemanticsKnown = effectShapeKnown,
            Category = semantic.Category,
            Rarity = rarity,
            Power = power,
            MasterEffectType = (NetherCodeMasterEffectType)effectType,
            EffectParameter1 = effectParameter1,
            EffectParameter2 = effectParameter2,
            EffectParameter3 = effectParameter3,
            AbilityAssetId = abilityAssetId,
            PossessionAmount = possessionAmount,
            PartyCoverageKnown = false,
            PartyCoverage = 0,
        };
    }

    public static bool RequiresBoundedSemanticAudit(NetherCodeCandidate candidate) =>
        candidate != null
        && (!candidate.IsKnown
            || !candidate.EffectSemanticsKnown
            || candidate.MasterEffectType is NetherCodeMasterEffectType.NetherAbility
                or NetherCodeMasterEffectType.CommonAbility);

    private static void DecodeEffect(
        int rawEffectType,
        long p1,
        long p2,
        out int abilityLevel,
        out long abilityAssetId,
        out bool isKnown
    )
    {
        abilityLevel = 0;
        abilityAssetId = 0;
        isKnown = NetherCodeCategorySemantics.IsKnownEffectType(rawEffectType);
        if (!isKnown)
            return;

        switch ((NetherCodeMasterEffectType)rawEffectType)
        {
            case NetherCodeMasterEffectType.NetherAbility:
            case NetherCodeMasterEffectType.CommonAbility:
                isKnown = p1 > 0 && p2 is > 0 and <= int.MaxValue;
                if (isKnown)
                {
                    abilityAssetId = p1;
                    abilityLevel = checked((int)p2);
                }
                return;
            case NetherCodeMasterEffectType.ErosionAdditionUp:
            case NetherCodeMasterEffectType.ErosionAdditionDown:
            case NetherCodeMasterEffectType.ErosionRateUp:
            case NetherCodeMasterEffectType.ErosionRateDown:
                // Current-client code preserves these parameters but never consumes them.  The
                // enum proves only the effect identity, not an amount slot, sign, or unit.
                isKnown = false;
                return;
            default:
                isKnown = false;
                return;
        }
    }
}
