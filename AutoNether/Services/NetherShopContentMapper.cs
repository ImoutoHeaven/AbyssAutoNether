#nullable enable

using System;
using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// Data-only representation of one native MNetherFloorShopContents row.  ContentId is the
/// shop-row ID sent back to OnPurchaseContentAsync; ItemId is the optional payload ID.  The
/// latter is legitimately zero for ID-less non-item products such as currencies/effects.
/// </summary>
internal readonly record struct NetherRawShopContent(
    long ContentId,
    int RawContentType,
    long ItemId,
    int Price,
    bool UsesNetherGold,
    int Amount
);

internal readonly record struct NetherShopItemMaster(
    long ItemId,
    int ItemType,
    NetherRewardRarity Rarity
);

internal readonly record struct NetherShopContentMapResult(
    IReadOnlyList<NetherShopContent> Contents,
    string Detail
)
{
    public bool IsSuccess => Detail.Length == 0;

    public static NetherShopContentMapResult Success(IReadOnlyList<NetherShopContent> contents) =>
        new(contents, string.Empty);

    public static NetherShopContentMapResult Failure(string detail) =>
        new(Array.Empty<NetherShopContent>(), detail);
}

/// <summary>
/// Converts the heterogeneous native shop catalogue into the narrow EquipmentBags policy
/// model.  Raw content types 30/31 are MItems-backed.  The only known non-item families are the
/// native-proven code-offer/resource content types; every other raw type remains unknown so an
/// unproven sibling cannot make a Shop look eligible.
/// </summary>
internal static class NetherShopContentMapper
{
    private const int ItemContentType = 30;
    private const int LimitedItemContentType = 31;

    private static bool IsKnownNonItem(NetherRawShopContent row) => row.RawContentType switch
    {
        160 => NetherEventNativeMapping.IsCodeOfferContentId(row.ItemId),
        165 or 166 => NetherEventNativeMapping.IsValidResourceContentId(row.ItemId),
        _ => false,
    };

    public static NetherShopContentMapResult Map(
        IReadOnlyList<NetherRawShopContent> rows,
        IReadOnlyDictionary<long, NetherShopItemMaster> itemById,
        NetherStrategyTypedSemanticProviderEvidence? typedSemanticProvider = null
    )
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));
        if (itemById == null)
            throw new ArgumentNullException(nameof(itemById));

        NetherStrategySemanticTierLookup semanticTiers =
            NetherStrategySemanticTierLookup.Create(typedSemanticProvider);
        var mapped = new List<NetherShopContent>(rows.Count);
        var seenContentIds = new HashSet<long>();
        foreach (NetherRawShopContent row in rows)
        {
            if (row.ContentId <= 0 || row.Amount <= 0 || row.Price < 0)
                return NetherShopContentMapResult.Failure("invalid-shop-row:" + row.ContentId);
            if (!seenContentIds.Add(row.ContentId))
                return NetherShopContentMapResult.Failure("duplicate-shop-row:" + row.ContentId);

            bool isItem = row.RawContentType is ItemContentType or LimitedItemContentType;
            if (!isItem)
            {
                bool knownNonItem = IsKnownNonItem(row);
                bool typedShopKey = semanticTiers.TryGetShopKey(
                    row.ContentId,
                    row.RawContentType,
                    row.ItemId,
                    row.Amount,
                    out long shopKeyIdentity
                );
                mapped.Add(new NetherShopContent(
                    row.ContentId,
                    row.ItemId,
                    0,
                    NetherRewardRarity.NoEffect,
                    row.Price,
                    row.UsesNetherGold,
                    row.Amount,
                    known: knownNonItem
                )
                {
                    RawContentType = row.RawContentType,
                    RawItemType = 0,
                    RawRarity = NetherRewardRarity.NoEffect,
                    IsTreasureKey = typedShopKey,
                    ShopKeyIdentity = typedShopKey ? shopKeyIdentity : 0,
                });
                continue;
            }

            if (row.ItemId <= 0)
                return NetherShopContentMapResult.Failure("invalid-shop-item-id:" + row.ContentId);
            if (!itemById.TryGetValue(row.ItemId, out NetherShopItemMaster item))
                return NetherShopContentMapResult.Failure("missing-shop-item-master:" + row.ItemId);

            bool typedItemShopKey = semanticTiers.TryGetShopKey(
                row.ContentId,
                row.RawContentType,
                row.ItemId,
                row.Amount,
                out long itemShopKeyIdentity
            );
            bool typedCanonical = semanticTiers.TryGetCanonicalRewardEvidence(
                item.ItemId,
                out NetherCanonicalRewardTier tier,
                out int typedItemType,
                out NetherRewardRarity typedRarity
            );
            mapped.Add(new NetherShopContent(
                row.ContentId,
                row.ItemId,
                typedCanonical ? typedItemType : 0,
                typedCanonical ? typedRarity : NetherRewardRarity.NoEffect,
                row.Price,
                row.UsesNetherGold,
                row.Amount,
                known: true
            )
            {
                CanonicalRewardTier = typedCanonical ? tier : NetherCanonicalRewardTier.Unknown,
                RawItemType = item.ItemType,
                RawRarity = item.Rarity,
                RawContentType = row.RawContentType,
                IsTreasureKey = typedItemShopKey,
                ShopKeyIdentity = typedItemShopKey ? itemShopKeyIdentity : 0,
            });
        }

        return NetherShopContentMapResult.Success(mapped);
    }
}
