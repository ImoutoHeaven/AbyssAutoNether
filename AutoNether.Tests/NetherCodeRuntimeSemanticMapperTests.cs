#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherCodeRuntimeSemanticMapperTests
{
    [Fact]
    public void Ability_effect_uses_p1_as_asset_p2_as_level_and_keeps_power_separate()
    {
        NetherCodeCandidate candidate = NetherCodeRuntimeSemanticMapper.MapCandidate(
            codeId: 51001,
            rawCategory: (int)NetherCodeCategory.Rush,
            effectType: (int)NetherCodeMasterEffectType.NetherAbility,
            effectParameter1: 100006,
            effectParameter2: 2,
            effectParameter3: 77,
            rarity: 3,
            power: 900
        );

        Assert.True(candidate.IsKnown);
        Assert.Equal(NetherCodeFamily.Rush, candidate.Family);
        Assert.Equal(100006, candidate.AbilityAssetId);
        Assert.Equal(2, candidate.AbilityLevel);
        Assert.Equal(900, candidate.Power);
        Assert.Equal(77, candidate.EffectParameter3);
        Assert.False(candidate.PartyCoverageKnown);
    }

    [Fact]
    public void Possession_amount_does_not_become_ability_level_or_category_card_count()
    {
        NetherCodeState safe = NetherCodeRuntimeSemanticMapper.MapState(
            codeId: 51003,
            rawCategory: (int)NetherCodeCategory.Safe,
            effectType: (int)NetherCodeMasterEffectType.CommonAbility,
            effectParameter1: 200007,
            effectParameter2: 3,
            effectParameter3: 0,
            rarity: 2,
            power: 400,
            possessionAmount: 500
        );
        NetherCodeState risk = NetherCodeRuntimeSemanticMapper.MapState(
            codeId: 51004,
            rawCategory: (int)NetherCodeCategory.Risk,
            effectType: (int)NetherCodeMasterEffectType.CommonAbility,
            effectParameter1: 200008,
            effectParameter2: 1,
            effectParameter3: 0,
            rarity: 2,
            power: 400,
            possessionAmount: 1
        );

        Assert.Equal(3, safe.AbilityLevel);
        Assert.Equal(500, safe.PossessionAmount);
        NetherCodeEffectiveLevels levels = NetherCodePolicy.CalculateEffectiveLevels([safe, risk]);
        Assert.Equal(0, levels.Safe);
        Assert.Equal(0, levels.Risk);
    }

    [Fact]
    public void Unpublished_effect_type_preserves_proven_category_but_marks_effect_semantics_unknown()
    {
        NetherCodeCandidate candidate = NetherCodeRuntimeSemanticMapper.MapCandidate(
            codeId: 51012,
            rawCategory: (int)NetherCodeCategory.Impact,
            effectType: 12,
            effectParameter1: (int)NetherCodeCategory.Safe,
            effectParameter2: 40,
            effectParameter3: 0,
            rarity: 4,
            power: 0
        );

        Assert.True(candidate.IsKnown);
        Assert.False(candidate.EffectSemanticsKnown);
        Assert.Equal(NetherCodeFamily.Impact, candidate.Family);
        Assert.Equal((NetherCodeMasterEffectType)12, candidate.MasterEffectType);
        Assert.Equal((int)NetherCodeCategory.Safe, candidate.EffectParameter1);
        Assert.Equal(40, candidate.EffectParameter2);
        Assert.Equal(0, candidate.AbilityLevel);
        Assert.Equal(0, candidate.AbilityAssetId);
    }

    [Fact]
    public void Invalid_effect_shape_does_not_erase_the_independent_category_axis()
    {
        NetherCodeCandidate candidate = NetherCodeRuntimeSemanticMapper.MapCandidate(
            codeId: 30024,
            rawCategory: (int)NetherCodeCategory.Safe,
            effectType: 99,
            effectParameter1: 1,
            effectParameter2: 1,
            effectParameter3: 0,
            rarity: 4,
            power: 500
        );

        Assert.True(candidate.IsKnown);
        Assert.False(candidate.EffectSemanticsKnown);
        Assert.Equal(NetherCodeFamily.Safe, candidate.Family);
    }

    [Theory]
    [InlineData((int)NetherCodeMasterEffectType.ErosionAdditionUp)]
    [InlineData((int)NetherCodeMasterEffectType.ErosionAdditionDown)]
    [InlineData((int)NetherCodeMasterEffectType.ErosionRateUp)]
    [InlineData((int)NetherCodeMasterEffectType.ErosionRateDown)]
    public void Erosion_effect_identity_is_known_but_its_parameters_are_service_authoritative(
        int rawEffectType
    )
    {
        NetherCodeCandidate candidate = NetherCodeRuntimeSemanticMapper.MapCandidate(
            codeId: 30024,
            rawCategory: (int)NetherCodeCategory.Safe,
            effectType: rawEffectType,
            effectParameter1: 5,
            effectParameter2: 0,
            effectParameter3: 0,
            rarity: 4,
            power: 500
        );

        Assert.True(candidate.IsKnown);
        Assert.False(candidate.EffectSemanticsKnown);
        Assert.Equal(NetherCodeFamily.Safe, candidate.Family);
        Assert.Equal((NetherCodeMasterEffectType)rawEffectType, candidate.MasterEffectType);
    }
}
