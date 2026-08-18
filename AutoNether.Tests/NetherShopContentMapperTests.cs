#nullable enable

using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherShopContentMapperTests
{
    [Fact]
    public void Idless_non_item_rows_are_known_but_ineligible_while_equipment_bags_keep_item_metadata()
    {
        NetherShopContentMapResult result = NetherShopContentMapper.Map(
            [
                new NetherRawShopContent(10, 160, 0, 30, true, 1),
                new NetherRawShopContent(11, 31, 210001, 100, true, 1),
            ],
            new Dictionary<long, NetherShopItemMaster>
            {
                [210001] = new NetherShopItemMaster(210001, 91, NetherRewardRarity.Gold),
            }
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(2, result.Contents.Count);
        NetherShopContent nonItem = result.Contents[0];
        Assert.True(nonItem.Known);
        Assert.Equal(0, nonItem.ItemId);
        Assert.Equal(0, nonItem.ItemType);
        NetherShopContent equipmentBag = result.Contents[1];
        Assert.True(equipmentBag.Known);
        Assert.Equal(210001, equipmentBag.ItemId);
        Assert.Equal(0, equipmentBag.ItemType);
        Assert.Equal(NetherRewardRarity.NoEffect, equipmentBag.Rarity);
        Assert.Equal(91, equipmentBag.RawItemType);
        Assert.Equal(NetherRewardRarity.Gold, equipmentBag.RawRarity);
        Assert.Equal(NetherCanonicalRewardTier.Unknown, equipmentBag.CanonicalRewardTier);
    }

    [Fact]
    public void Typed_provider_marks_the_exact_shop_item_as_canonical_without_using_raw_rarity()
    {
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            CanonicalRewardTiers =
            [new NetherCanonicalRewardTierProviderEvidence(210001, NetherCanonicalRewardTier.GoldRankFive, 91)],
        };
        NetherShopContentMapResult result = NetherShopContentMapper.Map(
            [new NetherRawShopContent(10, 31, 210001, 300, true, 1)],
            new Dictionary<long, NetherShopItemMaster>
            {
                [210001] = new NetherShopItemMaster(210001, 91, NetherRewardRarity.Red),
            },
            provider
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(NetherCanonicalRewardTier.GoldRankFive, Assert.Single(result.Contents).CanonicalRewardTier);
    }

    [Fact]
    public void Typed_provider_marks_an_idless_key_by_exact_shop_identity()
    {
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            ShopKeyIdentities =
            [new NetherShopKeyProviderEvidence(10, 166, 0, 1, 7001)],
        };
        NetherShopContentMapResult result = NetherShopContentMapper.Map(
            [new NetherRawShopContent(10, 166, 0, 200, true, 1)],
            new Dictionary<long, NetherShopItemMaster>(),
            provider
        );

        Assert.True(result.IsSuccess, result.Detail);
        NetherShopContent key = Assert.Single(result.Contents);
        Assert.True(key.Known);
        Assert.True(key.IsTreasureKey);
        Assert.Equal(7001, key.ShopKeyIdentity);
        Assert.Equal(0, key.ItemId);
    }

    [Fact]
    public void Unknown_raw_non_item_sibling_stays_unknown_beside_a_typed_canonical_bag()
    {
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            CanonicalRewardTiers =
            [new NetherCanonicalRewardTierProviderEvidence(210001, NetherCanonicalRewardTier.GoldRankFive, 91)],
        };
        NetherShopContentMapResult result = NetherShopContentMapper.Map(
            [
                new NetherRawShopContent(10, 31, 210001, 300, true, 1),
                new NetherRawShopContent(11, 999, 0, 1, true, 1),
            ],
            new Dictionary<long, NetherShopItemMaster>
            {
                [210001] = new NetherShopItemMaster(210001, 91, NetherRewardRarity.Red),
            },
            provider
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.True(result.Contents[0].Known);
        Assert.Equal(NetherCanonicalRewardTier.GoldRankFive, result.Contents[0].CanonicalRewardTier);
        Assert.False(result.Contents[1].Known);
    }

    [Fact]
    public void Duplicate_or_conflicting_provider_shop_evidence_remains_unknown()
    {
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            CanonicalRewardTiers =
            [
                new NetherCanonicalRewardTierProviderEvidence(210001, NetherCanonicalRewardTier.GoldRankFive, 91),
                new NetherCanonicalRewardTierProviderEvidence(210001, NetherCanonicalRewardTier.GoldRankFive, 91),
                new NetherCanonicalRewardTierProviderEvidence(210001, NetherCanonicalRewardTier.RedRankFive, 91),
            ],
        };
        NetherShopContentMapResult result = NetherShopContentMapper.Map(
            [new NetherRawShopContent(10, 31, 210001, 300, true, 1)],
            new Dictionary<long, NetherShopItemMaster>
            {
                [210001] = new NetherShopItemMaster(210001, 91, NetherRewardRarity.Gold),
            },
            provider
        );

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(NetherCanonicalRewardTier.Unknown, Assert.Single(result.Contents).CanonicalRewardTier);
    }

    [Fact]
    public void Missing_item_master_remains_a_named_failure_instead_of_becoming_an_ignored_product()
    {
        NetherShopContentMapResult result = NetherShopContentMapper.Map(
            [new NetherRawShopContent(12, 31, 999999, 30, true, 1)],
            new Dictionary<long, NetherShopItemMaster>()
        );

        Assert.False(result.IsSuccess);
        Assert.Equal("missing-shop-item-master:999999", result.Detail);
    }
}
