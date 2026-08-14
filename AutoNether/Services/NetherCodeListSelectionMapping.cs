#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal readonly record struct NetherCodeListTabMappingEntry(int Category, int TabIndex);

/// <summary>
/// Resolves the native tab button index for a thumbnail already found in the code-list model
/// dictionary. The model bucket key and the code category are separate native coordinate spaces.
/// </summary>
internal static class NetherCodeListSelectionMapping
{
    public static bool TryResolveTabIndex(
        int modelBucketKey,
        int modelCategory,
        IReadOnlyList<NetherCodeListTabMappingEntry> tabIndexes,
        out int tabIndex,
        out string error
    )
    {
        tabIndex = -1;
        if (modelBucketKey < 0 || modelCategory < 1 || tabIndexes == null)
        {
            error = "invalid-code-list-selection-coordinate";
            return false;
        }
        if (tabIndexes.Count == 0)
        {
            error = "empty-code-list-tab-index-map";
            return false;
        }
        if (tabIndexes.Any(entry => entry.Category < 1 || entry.TabIndex < 0))
        {
            error = "invalid-code-list-tab-index-map";
            return false;
        }
        if (tabIndexes.Select(entry => entry.Category).Distinct().Count() != tabIndexes.Count)
        {
            error = "duplicate-code-list-tab-category";
            return false;
        }
        if (tabIndexes.Select(entry => entry.TabIndex).Distinct().Count() != tabIndexes.Count)
        {
            error = "duplicate-code-list-tab-index";
            return false;
        }

        // The thumbnail carries the raw MNetherCodes category. TabIndexes translates that
        // category into the UI bucket consumed by OnChangeTab; _modelDictionary is already
        // grouped by that UI bucket and therefore provides an independent consistency check.
        NetherCodeListTabMappingEntry[] matches = tabIndexes
            .Where(entry => entry.Category == modelCategory)
            .ToArray();
        if (matches.Length != 1)
        {
            error = matches.Length == 0
                ? "missing-code-list-tab-category:" + modelCategory
                : "ambiguous-code-list-tab-category:" + modelCategory;
            return false;
        }

        tabIndex = matches[0].TabIndex;
        if (tabIndex != modelBucketKey)
        {
            error = "code-list-tab-bucket-mismatch:category_"
                + modelCategory
                + ":bucket_"
                + modelBucketKey
                + ":mapped_"
                + tabIndex;
            tabIndex = -1;
            return false;
        }
        error = string.Empty;
        return true;
    }
}
