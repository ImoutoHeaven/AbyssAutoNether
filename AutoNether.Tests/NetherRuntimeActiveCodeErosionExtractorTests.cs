#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherRuntimeActiveCodeErosionExtractorTests
{
    [Fact]
    public void LivePossessionAndMasterRows_AreMappedThroughTheExactNativeMemberNames()
    {
        NetherActiveCodeErosionProjection projection = new NetherRuntimeActiveCodeErosionExtractor().Extract(
            new[] { new FakePossessionCode(40024, 3), new FakePossessionCode(30024, 2) },
            new[]
            {
                new FakeMasterCode(30024, 1, 4, 0, 0),
                new FakeMasterCode(40024, 2, 7, 0, 0),
            }
        );

        Assert.True(projection.ErosionProjectionKnown);
        Assert.Equal(new long[] { 30024, 40024 }, projection.SortedCodeIds);
        Assert.Empty(projection.ErosionEffects);
        Assert.Equal(4, projection.Entries[0].EffectParameter1);
        Assert.Equal(7, projection.Entries[1].EffectParameter1);
    }

    [Fact]
    public void MissingExactRuntimeMember_IsUnknown()
    {
        NetherActiveCodeErosionProjection projection = new NetherRuntimeActiveCodeErosionExtractor().Extract(
            new[] { new MissingAmountPossessionCode(30024) },
            new[] { new FakeMasterCode(30024, 6, 4, 0, 0) }
        );

        Assert.False(projection.ErosionProjectionKnown);
        Assert.Contains("possession", projection.Detail);
    }

    [Fact]
    public void LiveCategorySkillRows_UseExactThresholdAndCategoryMemberNames()
    {
        NetherActiveCodeErosionProjection projection = new NetherRuntimeActiveCodeErosionExtractor().Extract(
            new[]
            {
                new FakePossessionCode(1, 1),
                new FakePossessionCode(2, 1),
                new FakePossessionCode(3, 1),
                new FakePossessionCode(4, 1),
                new FakePossessionCode(5, 1),
            },
            new[]
            {
                new FakeMasterCode(1, 1, 0, 0, 0),
                new FakeMasterCode(2, 1, 0, 0, 0),
                new FakeMasterCode(3, 1, 0, 0, 0),
                new FakeMasterCode(4, 1, 0, 0, 0),
                new FakeMasterCode(5, 1, 0, 0, 0),
            },
            new[] { new FakeCategorySkill(30000, 1, 5, 3, 7, 5, 0, 0) },
            activeNetherId: 1
        );

        Assert.True(projection.ErosionProjectionKnown, projection.Detail);
        Assert.True(Assert.Single(projection.CategorySkillEntries).IsActive);
        NetherCodeEffect effect = Assert.Single(projection.ErosionEffects);
        Assert.Equal(30000, effect.CodeId);
        Assert.Equal(NetherCodeEffectKind.ErosionAdditionDown, effect.EffectKind);
        Assert.Equal(5, effect.Amount);
    }

    private sealed class FakePossessionCode
    {
        public FakePossessionCode(long codeId, int amount)
        {
            MNetherCodeId = codeId;
            Amount = amount;
        }

        public long MNetherCodeId { get; }
        public int Amount { get; }
    }

    private sealed class MissingAmountPossessionCode
    {
        public MissingAmountPossessionCode(long codeId) => MNetherCodeId = codeId;

        public long MNetherCodeId { get; }
    }

    private sealed class FakeMasterCode
    {
        public FakeMasterCode(long id, int effectType, long parameter1, long parameter2, long parameter3)
        {
            this.id = id;
            effect_type = effectType;
            effect_parameter_1 = parameter1;
            effect_parameter_2 = parameter2;
            effect_parameter_3 = parameter3;
            m_nether_id = 1;
            category = 3;
        }

        public long id;
        public long m_nether_id;
        public int category;
        public int effect_type;
        public long effect_parameter_1;
        public long effect_parameter_2;
        public long effect_parameter_3;
    }

    private sealed class FakeCategorySkill
    {
        public FakeCategorySkill(
            long id,
            long netherId,
            int counter,
            int category,
            int effectType,
            long parameter1,
            long parameter2,
            long parameter3
        )
        {
            this.id = id;
            m_nether_id = netherId;
            this.counter = counter;
            this.category = category;
            effect_type = effectType;
            effect_parameter_1 = parameter1;
            effect_parameter_2 = parameter2;
            effect_parameter_3 = parameter3;
        }

        public long id;
        public long m_nether_id;
        public int counter;
        public int category;
        public int effect_type;
        public long effect_parameter_1;
        public long effect_parameter_2;
        public long effect_parameter_3;
    }
}
