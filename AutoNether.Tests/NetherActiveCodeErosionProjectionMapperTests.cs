#nullable enable

using System.Collections.Generic;
using System.Linq;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherActiveCodeErosionProjectionMapperTests
{
    [Fact]
    public void HashAndCodeIds_AreOrderIndependentAndIncludeEveryMasterParameter()
    {
        NetherActiveCodeErosionProjection first = Map(
            new[] { Possession(40024, 2), Possession(30024, 1) },
            new[] { Master(30024, 1, 5, 0, 0), Master(40024, 2, 7, 0, 0) }
        );
        NetherActiveCodeErosionProjection second = Map(
            new[] { Possession(30024, 1), Possession(40024, 2) },
            new[] { Master(40024, 2, 7, 0, 0), Master(30024, 1, 5, 0, 0) }
        );

        Assert.True(first.ErosionProjectionKnown);
        Assert.Equal(first.CodeHash, second.CodeHash);
        Assert.Equal(new long[] { 30024, 40024 }, first.SortedCodeIds);
        Assert.Contains("30024:1:1:5:0:0", first.CodeHash);
        Assert.Contains("40024:2:2:7:0:0", first.CodeHash);
    }

    [Fact]
    public void EffectTypesSixThroughNine_AreFailClosedWithoutAClientParameterConsumer()
    {
        foreach (int effectType in new[] { 6, 7, 8, 9 })
        {
            NetherActiveCodeErosionProjection projection = Map(
                new[] { Possession(effectType) },
                new[] { Master(effectType, effectType, 10 + effectType, 0, 0) }
            );

            Assert.False(projection.ErosionProjectionKnown);
            Assert.Empty(projection.ErosionEffects);
            Assert.Contains(
                "service-authoritative-nether-code-erosion-effect:type=" + effectType,
                projection.Detail
            );
        }
    }

    [Fact]
    public void OrdinaryEffectsOneAndTwo_AreKnownButDoNotAlterErosionProjection()
    {
        NetherActiveCodeErosionProjection projection = Map(
            new[] { Possession(1), Possession(2) },
            new[] { Master(1, 1, 99, 88, 77), Master(2, 2, 66, 55, 44) }
        );

        Assert.True(projection.ErosionProjectionKnown);
        Assert.Empty(projection.ErosionEffects);
        Assert.Equal(new long[] { 1, 2 }, projection.SortedCodeIds);
        Assert.Contains("1:1:1:99:88:77", projection.CodeHash);
        Assert.Contains("2:1:2:66:55:44", projection.CodeHash);
    }

    [Fact]
    public void UnpublishedEffectTwelve_IsFailClosedInsteadOfReceivingInventedSemantics()
    {
        NetherActiveCodeErosionProjection projection = Map(
            new[] { Possession(30026) },
            new[] { Master(30026, 12, 3, 100, 0) }
        );

        Assert.False(projection.ErosionProjectionKnown);
        Assert.Empty(projection.ErosionEffects);
        Assert.Contains("unknown-nether-code-effect-type:12", projection.Detail);
    }

    [Theory]
    [InlineData(10, 1, 0, 0)]
    public void UnknownOrInvalidEffectParameters_AreFailClosed(
        int effectType,
        long parameter1,
        long parameter2,
        long parameter3
    )
    {
        NetherActiveCodeErosionProjection projection = Map(
            new[] { Possession(1) },
            new[] { Master(1, effectType, parameter1, parameter2, parameter3) }
        );

        Assert.False(projection.ErosionProjectionKnown);
        Assert.Empty(projection.ErosionEffects);
    }

    [Fact]
    public void DuplicateActiveMaster_IsAmbiguousAndFailClosed()
    {
        NetherActiveCodeErosionProjection projection = Map(
            new[] { Possession(1) },
            new[] { Master(1, 6, 1, 0, 0), Master(1, 6, 1, 0, 0) }
        );

        Assert.False(projection.ErosionProjectionKnown);
        Assert.Contains("duplicate", projection.Detail);
    }

    [Fact]
    public void EmptyPossession_IsKnownWithAnEmptyProjection()
    {
        NetherActiveCodeErosionProjection projection = Map(
            System.Array.Empty<NetherPossessionCodeErosionInput>(),
            System.Array.Empty<NetherCodeErosionMasterInput>()
        );

        Assert.True(projection.ErosionProjectionKnown);
        Assert.Empty(projection.SortedCodeIds);
        Assert.Empty(projection.ErosionEffects);
        Assert.Equal("nether-codes:none", projection.CodeHash);
    }

    [Fact]
    public void ActiveCategorySkill_ProjectsItsErosionModifierAtTheExactCodeThreshold()
    {
        NetherPossessionCodeErosionInput[] possessions = Enumerable.Range(1, 5)
            .Select(id => Possession(id))
            .ToArray();
        NetherCodeErosionMasterInput[] masters = Enumerable.Range(1, 5)
            .Select(id => CategorizedMaster(id, category: 3))
            .ToArray();

        NetherActiveCodeErosionProjection projection = new NetherActiveCodeErosionProjectionMapper().Map(
            possessions,
            masters,
            new[] { CategorySkill(30000, counter: 5, category: 3, effectType: 7, parameter1: 5) },
            activeNetherId: 1
        );

        Assert.False(projection.ErosionProjectionKnown);
        Assert.Empty(projection.ErosionEffects);
        Assert.Contains(
            "service-authoritative-nether-code-erosion-effect:type=7",
            projection.Detail
        );
    }

    [Fact]
    public void CategorySkillBelowThreshold_IsFingerprintKnownButDoesNotModifyErosion()
    {
        NetherActiveCodeErosionProjection projection = new NetherActiveCodeErosionProjectionMapper().Map(
            Enumerable.Range(1, 4).Select(id => Possession(id)).ToArray(),
            Enumerable.Range(1, 4).Select(id => CategorizedMaster(id, category: 3)).ToArray(),
            new[] { CategorySkill(30000, counter: 5, category: 3, effectType: 7, parameter1: 5) },
            activeNetherId: 1
        );

        Assert.True(projection.ErosionProjectionKnown, projection.Detail);
        Assert.Empty(projection.ErosionEffects);
        Assert.False(Assert.Single(projection.CategorySkillEntries).IsActive);
        Assert.Contains("category-skills:30000:1:5:3:7:5:0:0:0", projection.CodeHash);
    }

    [Fact]
    public void Category_skill_threshold_uses_native_paired_difference_not_raw_category_count()
    {
        NetherPossessionCodeErosionInput[] possessions = Enumerable.Range(1, 6)
            .Select(id => Possession(id))
            .ToArray();
        NetherCodeErosionMasterInput[] masters = Enumerable.Range(1, 5)
            .Select(id => CategorizedMaster(id, category: 3))
            .Append(CategorizedMaster(6, category: 4))
            .ToArray();

        NetherActiveCodeErosionProjection projection = new NetherActiveCodeErosionProjectionMapper().Map(
            possessions,
            masters,
            new[] { CategorySkill(30000, counter: 5, category: 3, effectType: 7, parameter1: 5) },
            activeNetherId: 1
        );

        Assert.True(projection.ErosionProjectionKnown, projection.Detail);
        Assert.Empty(projection.ErosionEffects);
        Assert.False(Assert.Single(projection.CategorySkillEntries).IsActive);
    }

    private static NetherActiveCodeErosionProjection Map(
        IReadOnlyList<NetherPossessionCodeErosionInput> possessions,
        IReadOnlyList<NetherCodeErosionMasterInput> masters
    ) => new NetherActiveCodeErosionProjectionMapper().Map(possessions, masters);

    private static NetherPossessionCodeErosionInput Possession(long codeId, int amount = 1) =>
        new(codeId, amount);

    private static NetherCodeErosionMasterInput Master(
        long codeId,
        int effectType,
        long parameter1,
        long parameter2,
        long parameter3
    ) => new(codeId, effectType, parameter1, parameter2, parameter3);

    private static NetherCodeErosionMasterInput CategorizedMaster(long codeId, int category) =>
        new(codeId, 1, 0, 0, 0)
        {
            NetherId = 1,
            Category = category,
        };

    private static NetherCodeCategoryErosionMasterInput CategorySkill(
        long skillId,
        int counter,
        int category,
        int effectType,
        long parameter1
    ) => new(skillId, 1, counter, category, effectType, parameter1, 0, 0);
}
