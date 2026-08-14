#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodeListSelectionMappingTests
{
    [Fact]
    public void Strength_code_in_first_model_bucket_uses_category_to_tab_mapping()
    {
        var tabIndexes = new[]
        {
            new NetherCodeListTabMappingEntry(Category: 1, TabIndex: 1),
            new NetherCodeListTabMappingEntry(Category: 2, TabIndex: 0),
            new NetherCodeListTabMappingEntry(Category: 3, TabIndex: 2),
            new NetherCodeListTabMappingEntry(Category: 4, TabIndex: 3),
        };

        bool resolved = NetherCodeListSelectionMapping.TryResolveTabIndex(
            modelBucketKey: 0,
            modelCategory: 2,
            tabIndexes,
            out int tabIndex,
            out string error
        );

        Assert.True(resolved, error);
        Assert.Equal(0, tabIndex);
    }

    [Fact]
    public void Model_bucket_must_agree_with_the_category_mapping()
    {
        bool resolved = NetherCodeListSelectionMapping.TryResolveTabIndex(
            modelBucketKey: 2,
            modelCategory: 2,
            new[] { new NetherCodeListTabMappingEntry(Category: 2, TabIndex: 0) },
            out int tabIndex,
            out string error
        );

        Assert.False(resolved);
        Assert.Equal(-1, tabIndex);
        Assert.Equal("code-list-tab-bucket-mismatch:category_2:bucket_2:mapped_0", error);
    }

    [Fact]
    public void Duplicate_category_or_tab_coordinates_fail_closed()
    {
        Assert.False(NetherCodeListSelectionMapping.TryResolveTabIndex(
            modelBucketKey: 0,
            modelCategory: 2,
            new[]
            {
                new NetherCodeListTabMappingEntry(Category: 2, TabIndex: 0),
                new NetherCodeListTabMappingEntry(Category: 2, TabIndex: 1),
            },
            out _,
            out string duplicateCategory
        ));
        Assert.Equal("duplicate-code-list-tab-category", duplicateCategory);

        Assert.False(NetherCodeListSelectionMapping.TryResolveTabIndex(
            modelBucketKey: 0,
            modelCategory: 2,
            new[]
            {
                new NetherCodeListTabMappingEntry(Category: 1, TabIndex: 0),
                new NetherCodeListTabMappingEntry(Category: 2, TabIndex: 0),
            },
            out _,
            out string duplicateTab
        ));
        Assert.Equal("duplicate-code-list-tab-index", duplicateTab);
    }

    [Fact]
    public void Missing_selected_category_fails_closed()
    {
        bool resolved = NetherCodeListSelectionMapping.TryResolveTabIndex(
            modelBucketKey: 0,
            modelCategory: 2,
            new[] { new NetherCodeListTabMappingEntry(Category: 1, TabIndex: 0) },
            out _,
            out string error
        );

        Assert.False(resolved);
        Assert.Equal("missing-code-list-tab-category:2", error);
    }
}
