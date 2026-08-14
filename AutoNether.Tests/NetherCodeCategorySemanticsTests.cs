using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherCodeCategorySemanticsTests
{
    [Fact]
    public void Native_categories_map_to_the_four_displayed_code_families()
    {
        NetherCodeMasterSemantic rush = NetherCodeCategorySemantics.Resolve(51001, rawCategory: 1);
        NetherCodeMasterSemantic impact = NetherCodeCategorySemantics.Resolve(51002, rawCategory: 2);
        NetherCodeMasterSemantic safe = NetherCodeCategorySemantics.Resolve(51003, rawCategory: 3);
        NetherCodeMasterSemantic risk = NetherCodeCategorySemantics.Resolve(51004, rawCategory: 4);

        Assert.True(rush.IsKnown);
        Assert.Equal(NetherCodeCategory.Rush, rush.Category);
        Assert.Equal(NetherCodeCategoryGroup.Tactics, rush.Group);
        Assert.Equal(NetherCodeCategory.Impact, rush.PairedCategory);
        Assert.Equal(NetherCodeFamily.Rush, rush.Family);
        Assert.Equal(NetherCodeFamily.Impact, impact.Family);
        Assert.Equal(NetherCodeFamily.Safe, safe.Family);
        Assert.Equal(NetherCodeFamily.Risk, risk.Family);
    }

    [Fact]
    public void Native_pairs_affect_category_counters_but_do_not_mean_inventory_exclusion()
    {
        Assert.Equal(
            NetherCodeCategory.Impact,
            NetherCodeCategorySemantics.GetPairedCategory(NetherCodeCategory.Rush)
        );
        Assert.Equal(
            NetherCodeCategory.Risk,
            NetherCodeCategorySemantics.GetPairedCategory(NetherCodeCategory.Safe)
        );
        Assert.True(NetherCodeCategorySemantics.ArePairedCounterCategories(
            NetherCodeCategory.Rush,
            NetherCodeCategory.Impact
        ));
        Assert.True(NetherCodeCategorySemantics.ArePairedCounterCategories(
            NetherCodeCategory.Safe,
            NetherCodeCategory.Risk
        ));
        Assert.False(NetherCodeCategorySemantics.ArePairedCounterCategories(
            NetherCodeCategory.Rush,
            NetherCodeCategory.Safe
        ));
        Assert.False(NetherCodeCategorySemantics.ArePairedCounterCategories(
            NetherCodeCategory.Rush,
            NetherCodeCategory.Rush
        ));

        NetherCodeEffectiveLevels coexistence = NetherCodePolicy.CalculateEffectiveLevels(
            [State(1, NetherCodeFamily.Safe), State(2, NetherCodeFamily.Risk)]
        );
        Assert.Equal(0, coexistence.Safe);
        Assert.Equal(0, coexistence.Risk);
    }

    [Fact]
    public void Code_ids_never_override_an_invalid_master_category()
    {
        Assert.False(NetherCodeCategorySemantics.Resolve(30024, rawCategory: 99).IsKnown);
        Assert.False(NetherCodeCategorySemantics.Resolve(40024, rawCategory: 0).IsKnown);
        Assert.True(NetherCodeCategorySemantics.Resolve(51005, rawCategory: 1).IsKnown);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void Current_native_effect_types_are_explicitly_bounded(int effectType)
    {
        Assert.True(NetherCodeCategorySemantics.IsKnownEffectType(effectType));
    }

    [Fact]
    public void Unpublished_effect_type_twelve_is_not_assigned_invented_semantics()
    {
        Assert.False(NetherCodeCategorySemantics.IsKnownEffectType(12));
    }

    private static NetherCodeState State(long id, NetherCodeFamily family) => new(id, family, 1)
    {
        Category = family switch
        {
            NetherCodeFamily.Rush => NetherCodeCategory.Rush,
            NetherCodeFamily.Impact => NetherCodeCategory.Impact,
            NetherCodeFamily.Safe => NetherCodeCategory.Safe,
            NetherCodeFamily.Risk => NetherCodeCategory.Risk,
            _ => NetherCodeCategory.Unknown,
        },
        PartyCoverageKnown = true,
    };
}
