#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// One fail-closed interpretation of the raw fields exposed by
/// <c>MNetherFloorEventParts</c>.  The packaged client exposes target type and select parameter,
/// but does not prove a selectable code ID for target type 7.  Keeping this mapping in one seam
/// prevents the production bridge, pre-entry safety, and visible evidence from disagreeing.
/// </summary>
internal static class NetherEventNativeMapping
{
    public static bool TryMapTargetType(
        int rawType,
        long parameter,
        out NetherEffectKind kind,
        out int amount,
        out string detail
    )
    {
        kind = NetherEffectKind.Unknown;
        amount = 0;
        detail = string.Empty;

        if (rawType == 0)
        {
            if (parameter != 0)
            {
                detail = "unsupported-event-target-parameter:" + parameter;
                return false;
            }
            return true;
        }

        if (rawType is < 1 or > 8 || parameter < 0 || parameter > int.MaxValue)
        {
            detail = "unsupported-event-target-type-or-parameter:" + rawType;
            return false;
        }

        kind = (NetherEffectKind)rawType;
        if (kind == NetherEffectKind.AbyssCodeTransform && parameter != 0)
        {
            // Fresh native evidence proves only the raw integer fields and the target_type=7
            // flow flag; it does not prove that select_parameter_1 is a code identity.
            detail = "unsupported-event-target-parameter:type=7:" + parameter;
            kind = NetherEffectKind.Unknown;
            return false;
        }

        amount = kind == NetherEffectKind.AbyssCodeTransform
            ? 0
            : checked((int)parameter);
        return true;
    }

    public static bool IsCodeOfferContentId(long contentId) => contentId == 0;

    public static bool IsValidResourceContentId(long contentId) => contentId >= 0;

    public static bool IsValidResourceEffectContentId(NetherEffectKind kind, long contentId) =>
        kind is NetherEffectKind.NetherGoldGain or NetherEffectKind.TreasureKeyGain
            ? IsValidResourceContentId(contentId)
            : true;

    /// <summary>
    /// Maps the raw native MItems.type transport field without narrowing outside the
    /// repository's int-valued item evidence seam.  The native field is a long; no narrower
    /// closed item-type domain is proven, so values outside the int domain remain unknown.
    /// </summary>
    public static bool TryMapItemType(long rawType, out int itemType)
    {
        itemType = 0;
        if (rawType < int.MinValue || rawType > int.MaxValue)
            return false;
        itemType = (int)rawType;
        return true;
    }

    /// <summary>
    /// The current native DropRarityLevel enum is the closed range NoEffect(0) through
    /// UniqueWeapon(5). MItems.rarity is a raw int, so values outside that proven domain remain
    /// unknown instead of being normalized to the highest known rarity.
    /// </summary>
    public static bool TryMapRewardRarity(int rawRarity, out NetherRewardRarity rarity)
    {
        rarity = NetherRewardRarity.NoEffect;
        if (rawRarity < (int)NetherRewardRarity.NoEffect
            || rawRarity > (int)NetherRewardRarity.UniqueWeapon)
        {
            return false;
        }
        rarity = (NetherRewardRarity)rawRarity;
        return true;
    }

    public static bool IsKnownRewardRarity(NetherRewardRarity rarity) =>
        TryMapRewardRarity((int)rarity, out _);
}
