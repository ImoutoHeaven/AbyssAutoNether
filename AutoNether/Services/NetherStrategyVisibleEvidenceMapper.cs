#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>Exact current MNetherFloorBattles fields used by NetherFloorModel.CreateModel.</summary>
internal readonly record struct NetherStrategyBattleMasterRow(
    long Id,
    long MapFloorMasterId,
    int BattleType,
    long BattleStageId,
    int CodeDropRatio
)
{
    public bool HasRequiredFields { get; init; } = true;
}

/// <summary>Legacy MNetherFloorTreasures relation retained for non-current capture callers.</summary>
internal readonly record struct NetherStrategyTreasureMasterRow(long Id, long MapFloorMasterId);

/// <summary>Exact non-localized MItems reward fields visible from an Event/Treasure part.</summary>
internal readonly record struct NetherStrategyItemMasterRow(
    long Id,
    long ItemType,
    int Rarity,
    int Value,
    int PossessionLimit
)
{
    public bool HasRequiredFields { get; init; } = true;
}

internal sealed record NetherStrategyShopInventoryCapture(
    bool IsMaterialized,
    IReadOnlyList<NetherShopContent> Contents,
    string UnknownReason
);

/// <summary>
/// Normalized lookup for the only semantic tier authority accepted by production mapping. Raw
/// rows never enter this lookup, and an identity with duplicate/conflicting provider evidence is
/// deliberately kept ambiguous instead of selecting an arbitrary entry.
/// </summary>
internal sealed class NetherStrategySemanticTierLookup
{
    private readonly record struct ShopKeyLookupKey(
        long ShopContentId,
        int RawContentType,
        long ItemId,
        int Amount
    );

    private readonly record struct BattleRouteSafetyLookupKey(
        long EventId,
        long EventPartId,
        int OptionNumber,
        long FloorId,
        long NodeId,
        long BattleId
    );

    private readonly IReadOnlyDictionary<long, NetherCanonicalRewardTierProviderEvidence> _canonicalRewardEvidence;
    private readonly IReadOnlySet<long> _ambiguousCanonicalRewardIds;
    private readonly IReadOnlyDictionary<long, NetherEventBattleTier> _eventBattleTiers;
    private readonly IReadOnlySet<long> _ambiguousEventBattleIds;
    private readonly IReadOnlyDictionary<BattleRouteSafetyLookupKey, NetherEventBattleRouteSafetyProviderEvidence>
        _eventBattleRouteSafety;
    private readonly IReadOnlySet<BattleRouteSafetyLookupKey> _ambiguousEventBattleRouteSafety;
    private readonly IReadOnlyDictionary<ShopKeyLookupKey, long> _shopKeyIdentities;
    private readonly IReadOnlySet<ShopKeyLookupKey> _ambiguousShopKeyIdentities;

    private NetherStrategySemanticTierLookup(
        IReadOnlyDictionary<long, NetherCanonicalRewardTierProviderEvidence> canonicalRewardEvidence,
        IReadOnlySet<long> ambiguousCanonicalRewardIds,
        IReadOnlyDictionary<long, NetherEventBattleTier> eventBattleTiers,
        IReadOnlySet<long> ambiguousEventBattleIds,
        IReadOnlyDictionary<BattleRouteSafetyLookupKey, NetherEventBattleRouteSafetyProviderEvidence>
            eventBattleRouteSafety,
        IReadOnlySet<BattleRouteSafetyLookupKey> ambiguousEventBattleRouteSafety,
        IReadOnlyDictionary<ShopKeyLookupKey, long> shopKeyIdentities,
        IReadOnlySet<ShopKeyLookupKey> ambiguousShopKeyIdentities
    )
    {
        _canonicalRewardEvidence = canonicalRewardEvidence;
        _ambiguousCanonicalRewardIds = ambiguousCanonicalRewardIds;
        _eventBattleTiers = eventBattleTiers;
        _ambiguousEventBattleIds = ambiguousEventBattleIds;
        _eventBattleRouteSafety = eventBattleRouteSafety;
        _ambiguousEventBattleRouteSafety = ambiguousEventBattleRouteSafety;
        _shopKeyIdentities = shopKeyIdentities;
        _ambiguousShopKeyIdentities = ambiguousShopKeyIdentities;
    }

    public static NetherStrategySemanticTierLookup Create(
        NetherStrategyTypedSemanticProviderEvidence? provider
    )
    {
        (Dictionary<long, NetherCanonicalRewardTierProviderEvidence> canonical, HashSet<long> ambiguousCanonical) =
            BuildLookup(
                provider?.CanonicalRewardTiers,
                evidence => evidence.ItemId,
                evidence => evidence,
                evidence => evidence.ItemType == 91
                    && evidence.Tier is
                        NetherCanonicalRewardTier.GoldRankFive
                        or NetherCanonicalRewardTier.RedRankFive
                        or NetherCanonicalRewardTier.UncolouredRankFive
            );
        (Dictionary<long, NetherEventBattleTier> battles, HashSet<long> ambiguousBattles) =
            BuildLookup(
                provider?.EventBattleTiers,
                evidence => evidence.BattleId,
                evidence => evidence.Tier,
                evidence => evidence.Tier is
                    NetherEventBattleTier.Boss
                    or NetherEventBattleTier.MiniBoss
                    or NetherEventBattleTier.NormalBattle
            );
        (Dictionary<BattleRouteSafetyLookupKey, NetherEventBattleRouteSafetyProviderEvidence> routeSafety,
            HashSet<BattleRouteSafetyLookupKey> ambiguousRouteSafety) =
            BuildBattleRouteSafetyLookup(provider?.EventBattleRouteSafety);
        (Dictionary<ShopKeyLookupKey, long> shopKeys, HashSet<ShopKeyLookupKey> ambiguousShopKeys) =
            BuildShopKeyLookup(provider?.ShopKeyIdentities);
        return new NetherStrategySemanticTierLookup(
            canonical,
            ambiguousCanonical,
            battles,
            ambiguousBattles,
            routeSafety,
            ambiguousRouteSafety,
            shopKeys,
            ambiguousShopKeys
        );
    }

    public bool TryGetCanonicalRewardTier(
        long itemId,
        out NetherCanonicalRewardTier tier
    )
    {
        tier = NetherCanonicalRewardTier.Unknown;
        return TryGetCanonicalRewardEvidence(itemId, out NetherCanonicalRewardTierProviderEvidence evidence)
            && (tier = evidence.Tier) != NetherCanonicalRewardTier.Unknown;
    }

    public bool TryGetCanonicalRewardEvidence(
        long itemId,
        out NetherCanonicalRewardTier tier,
        out int itemType,
        out NetherRewardRarity rarity
    )
    {
        tier = NetherCanonicalRewardTier.Unknown;
        itemType = 0;
        rarity = NetherRewardRarity.NoEffect;
        if (!TryGetCanonicalRewardEvidence(itemId, out NetherCanonicalRewardTierProviderEvidence evidence))
            return false;
        tier = evidence.Tier;
        itemType = evidence.ItemType;
        rarity = tier switch
        {
            NetherCanonicalRewardTier.GoldRankFive => NetherRewardRarity.Gold,
            NetherCanonicalRewardTier.RedRankFive => NetherRewardRarity.Red,
            NetherCanonicalRewardTier.UncolouredRankFive => NetherRewardRarity.UniqueWeapon,
            _ => NetherRewardRarity.NoEffect,
        };
        return rarity != NetherRewardRarity.NoEffect;
    }

    private bool TryGetCanonicalRewardEvidence(
        long itemId,
        out NetherCanonicalRewardTierProviderEvidence evidence
    )
    {
        evidence = default;
        return itemId > 0
            && !_ambiguousCanonicalRewardIds.Contains(itemId)
            && _canonicalRewardEvidence.TryGetValue(itemId, out evidence);
    }

    public bool TryGetEventBattleTier(
        long battleId,
        out NetherEventBattleTier tier
    )
    {
        tier = NetherEventBattleTier.Unknown;
        return battleId > 0
            && !_ambiguousEventBattleIds.Contains(battleId)
            && _eventBattleTiers.TryGetValue(battleId, out tier);
    }

    public bool TryGetEventBattleRouteSafety(
        long eventId,
        long eventPartId,
        int optionNumber,
        long floorId,
        long nodeId,
        long battleId,
        out NetherEventBattleRouteSafetyProviderEvidence evidence
    )
    {
        evidence = default;
        BattleRouteSafetyLookupKey key = new(
            eventId,
            eventPartId,
            optionNumber,
            floorId,
            nodeId,
            battleId
        );
        return !_ambiguousEventBattleRouteSafety.Contains(key)
            && _eventBattleRouteSafety.TryGetValue(key, out evidence)
            && evidence.IsValid;
    }

    public bool TryGetShopKey(
        long shopContentId,
        int rawContentType,
        long itemId,
        int amount,
        out long keyIdentity
    )
    {
        keyIdentity = 0;
        if (shopContentId < 0 || rawContentType < 0 || itemId < 0 || amount <= 0)
            return false;
        ShopKeyLookupKey key = new(shopContentId, rawContentType, itemId, amount);
        return !_ambiguousShopKeyIdentities.Contains(key)
            && _shopKeyIdentities.TryGetValue(key, out keyIdentity)
            && keyIdentity > 0;
    }

    private static (Dictionary<long, TValue> Known, HashSet<long> Ambiguous) BuildLookup<TEvidence, TValue>(
        IEnumerable<TEvidence>? source,
        Func<TEvidence, long> keySelector,
        Func<TEvidence, TValue> valueSelector,
        Func<TEvidence, bool> isValid
    )
        where TValue : struct
    {
        var known = new Dictionary<long, TValue>();
        var ambiguous = new HashSet<long>();
        foreach (TEvidence evidence in source ?? Array.Empty<TEvidence>())
        {
            long key = keySelector(evidence);
            if (key <= 0)
                continue;
            if (!isValid(evidence))
            {
                known.Remove(key);
                ambiguous.Add(key);
                continue;
            }
            if (ambiguous.Contains(key))
                continue;
            if (!known.TryAdd(key, valueSelector(evidence)))
            {
                known.Remove(key);
                ambiguous.Add(key);
            }
        }
        return (known, ambiguous);
    }

    private static (
        Dictionary<ShopKeyLookupKey, long> Known,
        HashSet<ShopKeyLookupKey> Ambiguous
    ) BuildShopKeyLookup(IEnumerable<NetherShopKeyProviderEvidence>? source)
    {
        var known = new Dictionary<ShopKeyLookupKey, long>();
        var ambiguous = new HashSet<ShopKeyLookupKey>();
        foreach (NetherShopKeyProviderEvidence evidence in source ?? Array.Empty<NetherShopKeyProviderEvidence>())
        {
            if (evidence.ShopContentId < 0
                || evidence.RawContentType < 0
                || evidence.ItemId < 0
                || evidence.Amount <= 0)
            {
                continue;
            }
            ShopKeyLookupKey key = new(
                evidence.ShopContentId,
                evidence.RawContentType,
                evidence.ItemId,
                evidence.Amount
            );
            if (evidence.KeyIdentity <= 0 || ambiguous.Contains(key))
            {
                known.Remove(key);
                ambiguous.Add(key);
                continue;
            }
            if (!known.TryAdd(key, evidence.KeyIdentity))
            {
                known.Remove(key);
                ambiguous.Add(key);
            }
        }
        return (known, ambiguous);
    }

    private static (
        Dictionary<BattleRouteSafetyLookupKey, NetherEventBattleRouteSafetyProviderEvidence> Known,
        HashSet<BattleRouteSafetyLookupKey> Ambiguous
    ) BuildBattleRouteSafetyLookup(
        IEnumerable<NetherEventBattleRouteSafetyProviderEvidence>? source
    )
    {
        var known = new Dictionary<BattleRouteSafetyLookupKey, NetherEventBattleRouteSafetyProviderEvidence>();
        var ambiguous = new HashSet<BattleRouteSafetyLookupKey>();
        foreach (NetherEventBattleRouteSafetyProviderEvidence evidence in source
            ?? Array.Empty<NetherEventBattleRouteSafetyProviderEvidence>())
        {
            BattleRouteSafetyLookupKey key = new(
                evidence.EventId,
                evidence.EventPartId,
                evidence.OptionNumber,
                evidence.FloorId,
                evidence.NodeId,
                evidence.BattleId
            );
            if (!evidence.IsValid || ambiguous.Contains(key))
            {
                known.Remove(key);
                ambiguous.Add(key);
                continue;
            }
            if (!known.TryAdd(key, evidence))
            {
                known.Remove(key);
                ambiguous.Add(key);
            }
        }
        return (known, ambiguous);
    }
}

internal sealed record NetherStrategyVisibleEvidenceCaptureRequest(
    IReadOnlyList<NetherFloorNode> Floors,
    IReadOnlyList<NetherStrategyBattleMasterRow> BattleRows,
    IReadOnlyList<NetherStrategyTreasureMasterRow> TreasureRows,
    IReadOnlyList<NetherFloorEventMasterRow> EventRows,
    IReadOnlyList<NetherFloorEventPartMasterRow> EventPartRows,
    IReadOnlyList<NetherStrategyItemMasterRow> ItemRows
)
{
    /// <summary>
    /// Optional authoritative semantic provider. The production RuntimeBridge supplies this only
    /// when a snapshot-scoped authoritative adapter is registered; its default remains null
    /// because fresh native evidence exposes only raw item/battle fields.
    /// </summary>
    public NetherStrategyTypedSemanticProviderEvidence? TypedSemanticProvider { get; init; }
    /// <summary>
    /// Current-native contract: Treasure identity/value comes from the exact live ExtendId and
    /// MNetherFloorEvents/Parts/Items chain, without consulting the residual Treasure cache.
    /// </summary>
    public bool UsesCurrentNativeTreasureEventAuthority { get; init; }
    public IReadOnlyDictionary<long, long> ExtendIdByNodeId { get; init; } =
        new Dictionary<long, long>();
    public IReadOnlyDictionary<long, NetherStrategyShopInventoryCapture> ShopInventoryByNodeId { get; init; } =
        new Dictionary<long, NetherStrategyShopInventoryCapture>();
}

internal sealed record NetherStrategyVisibleEvidenceCaptureResult(
    NetherStrategyVisibleMapEvidence? Evidence,
    string Detail
)
{
    public bool IsSuccess => Evidence != null && Detail.Length == 0;

    public static NetherStrategyVisibleEvidenceCaptureResult Success(
        NetherStrategyVisibleMapEvidence evidence
    ) => new(evidence, string.Empty);

    public static NetherStrategyVisibleEvidenceCaptureResult Failure(string detail) => new(null, detail);
}

/// <summary>
/// Pure production mapper for lifecycle-visible floor content. It follows current native master
/// relations, never substitutes a floor ID for a battle/treasure/content row ID, and leaves
/// not-yet-materialized Shop inventory explicitly unknown.
/// </summary>
internal static class NetherStrategyVisibleEvidenceMapper
{
    public static NetherStrategyVisibleEvidenceCaptureResult Map(
        NetherStrategyVisibleEvidenceCaptureRequest? request
    )
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (request.Floors == null
            || request.BattleRows == null
            || request.TreasureRows == null
            || request.EventRows == null
            || request.EventPartRows == null
            || request.ItemRows == null
            || request.ExtendIdByNodeId == null
            || request.ShopInventoryByNodeId == null)
        {
            return NetherStrategyVisibleEvidenceCaptureResult.Failure(
                "invalid-visible-evidence-capture-contract"
            );
        }
        if (!TryUniqueOptionMaster(request.BattleRows, row => row.Id, row => row.HasRequiredFields, out Dictionary<long, NetherStrategyBattleMasterRow> battleById)
            || !TryUnique(request.TreasureRows, row => row.Id, out Dictionary<long, NetherStrategyTreasureMasterRow> treasureById)
            || !TryUniqueOptionMaster(request.EventRows, row => row.EventId, row => row.HasRequiredFields, out Dictionary<long, NetherFloorEventMasterRow> eventById)
            || !TryUniqueEventPartRows(request.EventPartRows, out Dictionary<long, NetherFloorEventPartMasterRow> partById)
            || !TryUniqueOptionMaster(request.ItemRows, row => row.Id, row => row.HasRequiredFields, out Dictionary<long, NetherStrategyItemMasterRow> itemById))
        {
            return NetherStrategyVisibleEvidenceCaptureResult.Failure(
                "duplicate-or-invalid-visible-master-row"
            );
        }

        NetherStrategySemanticTierLookup semanticTiers =
            NetherStrategySemanticTierLookup.Create(request.TypedSemanticProvider);

        var rows = new List<NetherStrategyVisibleContentRow>();
        foreach (NetherFloorNode floor in request.Floors.Where(floor =>
            floor != null && floor.IsUnlocked && !floor.IsHidden))
        {
            switch (floor.NodeType)
            {
                case NetherFloorNodeType.Battle:
                case NetherFloorNodeType.MiniBoss:
                    AppendDirectBattle(rows, floor, request.BattleRows, isBoss: false);
                    break;
                case NetherFloorNodeType.Boss:
                    AppendDirectBattle(rows, floor, request.BattleRows, isBoss: true);
                    break;
                case NetherFloorNodeType.Treasure:
                    AppendTreasure(
                        rows,
                        floor,
                        request,
                        treasureById,
                        eventById,
                        partById,
                        itemById,
                        battleById,
                        semanticTiers
                    );
                    break;
                case NetherFloorNodeType.Event:
                case NetherFloorNodeType.Recovery:
                    AppendFloorEvents(
                        rows,
                        floor,
                        request,
                        eventById,
                        partById,
                        itemById,
                        battleById,
                        semanticTiers
                    );
                    break;
                case NetherFloorNodeType.Shop:
                    AppendShop(rows, floor, request.ShopInventoryByNodeId, semanticTiers);
                    break;
            }
        }

        return NetherStrategyVisibleEvidenceCaptureResult.Success(
            new NetherStrategyVisibleMapEvidence(request.Floors.ToArray(), rows)
        );
    }

    private static void AppendDirectBattle(
        ICollection<NetherStrategyVisibleContentRow> rows,
        NetherFloorNode floor,
        IReadOnlyList<NetherStrategyBattleMasterRow> masters,
        bool isBoss
    )
    {
        NetherStrategyBattleMasterRow[] matches = masters
            .Where(row => row.MapFloorMasterId == floor.FloorId)
            .ToArray();
        if (matches.Length != 1)
        {
            rows.Add(Unknown(
                isBoss ? NetherStrategyVisibleContentKind.Boss : NetherStrategyVisibleContentKind.Battle,
                floor,
                matches.Length == 0
                    ? "battle-master-row-unavailable-for-map-floor:" + floor.FloorId
                    : "ambiguous-battle-master-row-for-map-floor:" + floor.FloorId
            ));
            return;
        }
        rows.Add(Battle(floor, matches[0], isBoss, eventId: 0, eventPartId: 0));
    }

    private static void AppendTreasure(
        ICollection<NetherStrategyVisibleContentRow> rows,
        NetherFloorNode floor,
        NetherStrategyVisibleEvidenceCaptureRequest request,
        IReadOnlyDictionary<long, NetherStrategyTreasureMasterRow> treasureById,
        IReadOnlyDictionary<long, NetherFloorEventMasterRow> eventById,
        IReadOnlyDictionary<long, NetherFloorEventPartMasterRow> partById,
        IReadOnlyDictionary<long, NetherStrategyItemMasterRow> itemById,
        IReadOnlyDictionary<long, NetherStrategyBattleMasterRow> battleById,
        NetherStrategySemanticTierLookup semanticTiers
    )
    {
        NetherStrategyTreasureMasterRow[] matches = treasureById.Values
            .Where(row => row.MapFloorMasterId == floor.FloorId)
            .ToArray();
        if (!request.UsesCurrentNativeTreasureEventAuthority && matches.Length != 1)
        {
            rows.Add(Unknown(
                NetherStrategyVisibleContentKind.Treasure,
                floor,
                matches.Length == 0
                    ? "treasure-master-row-unavailable-for-map-floor:" + floor.FloorId
                    : "ambiguous-treasure-master-row-for-map-floor:" + floor.FloorId
            ));
            return;
        }
        long treasureMasterId = matches.Length == 1 ? matches[0].Id : 0;
        if (!TryResolveEvents(floor, request, eventById, out NetherFloorEventMasterRow[] events, out string error))
        {
            rows.Add(new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Treasure,
                floor.NodeId,
                treasureMasterId,
                0
            )
            {
                MapFloorMasterId = floor.FloorId,
                IsKnown = false,
                UnknownReason = error,
            });
            return;
        }
        foreach (NetherFloorEventMasterRow eventRow in events)
        {
            long authoritativeMasterId = request.UsesCurrentNativeTreasureEventAuthority
                ? eventRow.EventId
                : treasureMasterId;
            rows.Add(new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Treasure,
                floor.NodeId,
                authoritativeMasterId,
                eventRow.EventId
            )
            {
                MapFloorMasterId = floor.FloorId,
                EventId = eventRow.EventId,
                Weight = Math.Max(0, eventRow.Weight),
            });
            AppendEventParts(rows, floor, eventRow, partById, itemById, battleById, semanticTiers);
        }
    }

    private static void AppendFloorEvents(
        ICollection<NetherStrategyVisibleContentRow> rows,
        NetherFloorNode floor,
        NetherStrategyVisibleEvidenceCaptureRequest request,
        IReadOnlyDictionary<long, NetherFloorEventMasterRow> eventById,
        IReadOnlyDictionary<long, NetherFloorEventPartMasterRow> partById,
        IReadOnlyDictionary<long, NetherStrategyItemMasterRow> itemById,
        IReadOnlyDictionary<long, NetherStrategyBattleMasterRow> battleById,
        NetherStrategySemanticTierLookup semanticTiers
    )
    {
        if (!TryResolveEvents(floor, request, eventById, out NetherFloorEventMasterRow[] events, out string error))
        {
            rows.Add(Unknown(NetherStrategyVisibleContentKind.Event, floor, error));
            return;
        }
        foreach (NetherFloorEventMasterRow eventRow in events)
            AppendEventParts(rows, floor, eventRow, partById, itemById, battleById, semanticTiers);
    }

    private static bool TryResolveEvents(
        NetherFloorNode floor,
        NetherStrategyVisibleEvidenceCaptureRequest request,
        IReadOnlyDictionary<long, NetherFloorEventMasterRow> eventById,
        out NetherFloorEventMasterRow[] events,
        out string error
    )
    {
        request.ExtendIdByNodeId.TryGetValue(floor.NodeId, out long extendId);
        if (extendId > 0)
        {
            if (eventById.TryGetValue(extendId, out NetherFloorEventMasterRow exact))
            {
                events = new[] { exact };
                error = string.Empty;
                return true;
            }
            events = Array.Empty<NetherFloorEventMasterRow>();
            error = "event-master-row-unavailable-for-extend-id:" + extendId;
            return false;
        }
        events = eventById.Values.Where(row => row.MapFloorMasterId == floor.FloorId).ToArray();
        if (events.Length == 1)
        {
            error = string.Empty;
            return true;
        }
        error = events.Length == 0
            ? "event-master-row-unavailable-for-map-floor:" + floor.FloorId
            : "ambiguous-event-master-row-for-map-floor:" + floor.FloorId;
        return false;
    }

    private static void AppendEventParts(
        ICollection<NetherStrategyVisibleContentRow> rows,
        NetherFloorNode floor,
        NetherFloorEventMasterRow eventRow,
        IReadOnlyDictionary<long, NetherFloorEventPartMasterRow> partById,
        IReadOnlyDictionary<long, NetherStrategyItemMasterRow> itemById,
        IReadOnlyDictionary<long, NetherStrategyBattleMasterRow> battleById,
        NetherStrategySemanticTierLookup semanticTiers
    )
    {
        long[] partIds = { eventRow.PartId1, eventRow.PartId2, eventRow.PartId3, eventRow.PartId4 };
        var seenPartIds = new HashSet<long>();
        bool foundEmptyPart = false;
        for (int index = 0; index < partIds.Length; index++)
        {
            long partId = partIds[index];
            if (partId == 0)
            {
                foundEmptyPart = true;
                continue;
            }
            if (partId < 0 || foundEmptyPart || !seenPartIds.Add(partId))
            {
                rows.Add(Unknown(
                    NetherStrategyVisibleContentKind.Event,
                    floor,
                    partId < 0
                        ? "invalid-event-part-reference:" + partId
                        : foundEmptyPart
                            ? "noncontiguous-event-part-reference:" + partId
                            : "duplicate-event-part-reference:" + partId,
                    masterRowId: eventRow.EventId,
                    contentId: Math.Max(0, partId),
                    eventId: eventRow.EventId,
                    eventPartId: Math.Max(0, partId)
                ));
                continue;
            }
            if (!partById.TryGetValue(partId, out NetherFloorEventPartMasterRow part))
            {
                rows.Add(Unknown(
                    NetherStrategyVisibleContentKind.Event,
                    floor,
                    "event-part-row-unavailable:" + partId,
                    masterRowId: eventRow.EventId,
                    contentId: partId
                ));
                continue;
            }
            int amount = part.Amount is >= 0 and <= int.MaxValue ? checked((int)part.Amount) : 0;
            bool amountKnown = part.Amount is >= 0 and <= int.MaxValue;
            rows.Add(new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Event,
                floor.NodeId,
                eventRow.EventId,
                part.PartId
            )
            {
                MapFloorMasterId = floor.FloorId,
                EventId = eventRow.EventId,
                EventPartId = part.PartId,
                ContentType = part.ContentType,
                Amount = amount,
                Weight = Math.Max(0, eventRow.Weight),
                IsKnown = amountKnown,
                UnknownReason = amountKnown ? string.Empty : "invalid-event-part-amount:" + part.PartId,
                RawValues = new[] { new NetherStrategyNamedValue("OptionNumber", index + 1) },
                EventOptions =
                [
                    new NetherStrategyVisibleEventOptionEvidence(
                        index + 1,
                        part.PartId,
                        MapEventEffects(part, semanticTiers)
                    ),
                ],
            });
            if (part.ContentType is 30 or 31)
                AppendItem(rows, floor, eventRow, part, itemById, semanticTiers, amount, amountKnown);
            else if (part.ContentType is 160 or 165 or 166)
                AppendResource(rows, floor, eventRow, part, amount, amountKnown);

            foreach (long battleId in new[]
            {
                part.TargetType1 == 8 ? part.SelectParameter1 : 0,
                part.TargetType2 == 8 ? part.SelectParameter2 : 0,
                part.TargetType3 == 8 ? part.SelectParameter3 : 0,
            }.Where(value => value > 0).Distinct())
            {
                if (battleById.TryGetValue(battleId, out NetherStrategyBattleMasterRow battle))
                {
                    rows.Add(Battle(
                        floor,
                        battle,
                        isBoss: false,
                        eventRow.EventId,
                        part.PartId,
                        semanticTiers
                    ));
                }
                else
                {
                    rows.Add(Unknown(
                        NetherStrategyVisibleContentKind.Battle,
                        floor,
                        "event-battle-master-row-unavailable:" + battleId,
                        masterRowId: battleId,
                        contentId: 0,
                        eventId: eventRow.EventId,
                        eventPartId: part.PartId
                    ));
                }
            }
        }
    }

    private static IReadOnlyList<NetherStrategyVisibleEventEffectEvidence> MapEventEffects(
        NetherFloorEventPartMasterRow part,
        NetherStrategySemanticTierLookup semanticTiers
    ) =>
    [
        MapTargetEffect(
            NetherStrategyVisibleEventEffectSource.Target1,
            part.TargetType1,
            part.SelectParameter1,
            part.PartId,
            semanticTiers
        ),
        MapTargetEffect(
            NetherStrategyVisibleEventEffectSource.Target2,
            part.TargetType2,
            part.SelectParameter2,
            part.PartId,
            semanticTiers
        ),
        MapTargetEffect(
            NetherStrategyVisibleEventEffectSource.Target3,
            part.TargetType3,
            part.SelectParameter3,
            part.PartId,
            semanticTiers
        ),
        MapContentEffect(part),
    ];

    private static NetherStrategyVisibleEventEffectEvidence MapTargetEffect(
        NetherStrategyVisibleEventEffectSource source,
        int rawType,
        long parameter,
        long partId,
        NetherStrategySemanticTierLookup semanticTiers
    )
    {
        if (rawType == 0)
        {
            bool knownAbsent = parameter == 0;
            return new NetherStrategyVisibleEventEffectEvidence(source, rawType, parameter)
            {
                IsKnown = knownAbsent,
                UnknownReason = knownAbsent
                    ? string.Empty
                    : "absent-event-target-has-parameter:" + partId + ":" + (int)source,
            };
        }
        bool mapped = NetherEventNativeMapping.TryMapTargetType(
            rawType,
            parameter,
            out NetherEffectKind mappedKind,
            out int mappedAmount,
            out string mappingDetail
        );
        bool battleSemanticKnown = mapped && rawType != (int)NetherEffectKind.Battle;
        if (mapped && rawType == (int)NetherEffectKind.Battle)
        {
            battleSemanticKnown = parameter == 0
                || semanticTiers.TryGetEventBattleTier(parameter, out _);
        }
        return new NetherStrategyVisibleEventEffectEvidence(source, rawType, parameter)
        {
            EffectKind = mapped ? mappedKind : NetherEffectKind.Unknown,
            Amount = mapped ? mappedAmount : parameter,
            IsPresent = true,
            IsKnown = battleSemanticKnown,
            UnknownReason = !mapped
                ? mappingDetail + ":part=" + partId
                : battleSemanticKnown
                    ? string.Empty
                    : "event-battle-semantic-tier-unavailable-for-raw-type:" + partId,
        };
    }

    private static NetherStrategyVisibleEventEffectEvidence MapContentEffect(
        NetherFloorEventPartMasterRow part
    )
    {
        if (part.ContentType == 0)
        {
            bool knownAbsent = part.ContentId == 0 && part.Amount == 0;
            return new NetherStrategyVisibleEventEffectEvidence(
                NetherStrategyVisibleEventEffectSource.Content,
                part.ContentType,
                part.ContentId
            )
            {
                ContentId = part.ContentId,
                Amount = part.Amount,
                IsKnown = knownAbsent,
                UnknownReason = knownAbsent
                    ? string.Empty
                    : "absent-event-content-has-values:" + part.PartId,
            };
        }
        NetherEffectKind kind = part.ContentType switch
        {
            30 or 31 when part.ContentId > 0 => NetherEffectKind.Item,
            160 when NetherEventNativeMapping.IsCodeOfferContentId(part.ContentId) => NetherEffectKind.AbyssCodeOffer,
            165 when NetherEventNativeMapping.IsValidResourceContentId(part.ContentId) => NetherEffectKind.NetherGoldGain,
            166 when NetherEventNativeMapping.IsValidResourceContentId(part.ContentId) => NetherEffectKind.TreasureKeyGain,
            _ => NetherEffectKind.Unknown,
        };
        bool known = kind != NetherEffectKind.Unknown && part.Amount >= 0;
        return new NetherStrategyVisibleEventEffectEvidence(
            NetherStrategyVisibleEventEffectSource.Content,
            part.ContentType,
            part.ContentId
        )
        {
            ContentId = part.ContentId,
            Amount = part.Amount,
            EffectKind = kind,
            IsPresent = true,
            IsKnown = known,
            UnknownReason = known
                ? string.Empty
                : "unsupported-event-content-type-or-value:" + part.PartId + ":" + part.ContentType,
        };
    }

    private static void AppendItem(
        ICollection<NetherStrategyVisibleContentRow> rows,
        NetherFloorNode floor,
        NetherFloorEventMasterRow eventRow,
        NetherFloorEventPartMasterRow part,
        IReadOnlyDictionary<long, NetherStrategyItemMasterRow> itemById,
        NetherStrategySemanticTierLookup semanticTiers,
        int amount,
        bool amountKnown
    )
    {
        NetherStrategyItemMasterRow item = default;
        NetherCanonicalRewardTier tier = NetherCanonicalRewardTier.Unknown;
        int typedItemType = 0;
        NetherRewardRarity typedRarity = NetherRewardRarity.NoEffect;
        bool itemMasterKnown = part.ContentId > 0
            && itemById.TryGetValue(part.ContentId, out item)
            && item.HasRequiredFields
            && item.Id > 0;
        bool typedRewardKnown = itemMasterKnown
            && semanticTiers.TryGetCanonicalRewardEvidence(
                item.Id,
                out tier,
                out typedItemType,
                out typedRarity
            );
        bool itemKnown = amountKnown && typedRewardKnown;
        rows.Add(new NetherStrategyVisibleContentRow(
            NetherStrategyVisibleContentKind.Item,
            floor.NodeId,
            itemMasterKnown ? item.Id : Math.Max(0, part.ContentId),
            Math.Max(0, part.ContentId)
        )
        {
            MapFloorMasterId = floor.FloorId,
            EventId = eventRow.EventId,
            EventPartId = part.PartId,
            ContentType = part.ContentType,
            Amount = amount,
            Weight = Math.Max(0, eventRow.Weight),
            ItemType = typedRewardKnown ? typedItemType : 0,
            ItemRarity = typedRewardKnown ? (int)typedRarity : 0,
            RawItemType = itemMasterKnown ? item.ItemType : 0,
            RawItemRarity = itemMasterKnown ? item.Rarity : 0,
            ItemValue = itemMasterKnown ? item.Value : 0,
            ItemPossessionLimit = itemMasterKnown ? item.PossessionLimit : 0,
            CanonicalRewardTier = typedRewardKnown ? tier : NetherCanonicalRewardTier.Unknown,
            IsKnown = itemKnown,
            UnknownReason = itemKnown
                ? string.Empty
                : !amountKnown
                    ? "invalid-event-item-amount:" + part.PartId
                    : !itemMasterKnown
                        ? "event-item-master-row-unavailable:" + part.ContentId
                        : "event-item-canonical-semantic-unavailable:" + part.ContentId,
        });
    }

    private static void AppendResource(
        ICollection<NetherStrategyVisibleContentRow> rows,
        NetherFloorNode floor,
        NetherFloorEventMasterRow eventRow,
        NetherFloorEventPartMasterRow part,
        int amount,
        bool amountKnown
    )
    {
        bool contentKnown = part.ContentId >= 0;
        rows.Add(new NetherStrategyVisibleContentRow(
            NetherStrategyVisibleContentKind.Resource,
            floor.NodeId,
            part.PartId,
            Math.Max(0, part.ContentId)
        )
        {
            MapFloorMasterId = floor.FloorId,
            EventId = eventRow.EventId,
            EventPartId = part.PartId,
            ContentType = part.ContentType,
            Amount = amount,
            Weight = Math.Max(0, eventRow.Weight),
            IsKnown = amountKnown && contentKnown,
            UnknownReason = amountKnown && contentKnown
                ? string.Empty
                : "invalid-event-resource-payload:" + part.PartId,
        });
    }

    private static NetherStrategyVisibleContentRow Battle(
        NetherFloorNode floor,
        NetherStrategyBattleMasterRow battle,
        bool isBoss,
        long eventId,
        long eventPartId,
        NetherStrategySemanticTierLookup? semanticTiers = null
    ) => new(
        isBoss ? NetherStrategyVisibleContentKind.Boss : NetherStrategyVisibleContentKind.Battle,
        floor.NodeId,
        battle.Id,
        battle.BattleStageId
    )
    {
        MapFloorMasterId = battle.MapFloorMasterId,
        EventId = eventId,
        EventPartId = eventPartId,
        BattleType = battle.BattleType,
        BattleStageId = battle.BattleStageId,
        CodeDropRatio = battle.CodeDropRatio,
        EventBattleTier = semanticTiers != null
            && semanticTiers.TryGetEventBattleTier(battle.Id, out NetherEventBattleTier tier)
            && battle.HasRequiredFields
            && battle.Id > 0
            && battle.BattleStageId > 0
            && battle.CodeDropRatio >= 0
            ? tier
            : NetherEventBattleTier.Unknown,
        IsKnown = battle.HasRequiredFields
            && battle.Id > 0
            && battle.BattleStageId > 0
            && battle.CodeDropRatio >= 0
            && semanticTiers != null
            && semanticTiers.TryGetEventBattleTier(battle.Id, out _),
        UnknownReason = battle.HasRequiredFields
                && battle.Id > 0
                && battle.BattleStageId > 0
                && battle.CodeDropRatio >= 0
            ? semanticTiers != null
                && semanticTiers.TryGetEventBattleTier(battle.Id, out _)
                ? string.Empty
                : "event-battle-semantic-tier-unavailable-for-raw-type:" + battle.BattleType
            : "invalid-battle-master-row:" + battle.Id,
    };

    private static void AppendShop(
        ICollection<NetherStrategyVisibleContentRow> rows,
        NetherFloorNode floor,
        IReadOnlyDictionary<long, NetherStrategyShopInventoryCapture> captures,
        NetherStrategySemanticTierLookup semanticTiers
    )
    {
        if (!captures.TryGetValue(floor.NodeId, out NetherStrategyShopInventoryCapture? capture)
            || !capture.IsMaterialized)
        {
            rows.Add(Unknown(
                NetherStrategyVisibleContentKind.ShopInventory,
                floor,
                capture?.UnknownReason is { Length: > 0 } exact
                    ? exact
                    : "shop-inventory-not-materialized-before-entry"
            ));
            return;
        }
        foreach (NetherShopContent content in capture.Contents)
        {
            bool typedShopKey = semanticTiers.TryGetShopKey(
                content.ContentId,
                content.RawContentType,
                content.ItemId,
                content.Amount,
                out long shopKeyIdentity
            );
            bool known = content.Known
                && (content.ContentId > 0 || typedShopKey)
                && content.ItemId >= 0
                && content.Amount > 0
                && content.Price >= 0;
            rows.Add(new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.ShopInventory,
                floor.NodeId,
                Math.Max(0, content.ItemId),
                content.ContentId
            )
            {
                MapFloorMasterId = floor.FloorId,
                ContentType = content.RawContentType,
                Amount = Math.Max(0, content.Amount),
                Cost = Math.Max(0, content.Price),
                Rank = Math.Max(0, (int)content.Rarity),
                ItemRarity = Math.Max(0, (int)content.Rarity),
                RawItemType = content.RawItemType,
                RawItemRarity = (int)content.RawRarity,
                ItemType = content.ItemType,
                CanonicalRewardTier = semanticTiers.TryGetCanonicalRewardTier(
                    content.ItemId,
                    out NetherCanonicalRewardTier tier
                ) ? tier : NetherCanonicalRewardTier.Unknown,
                UsesNetherGold = content.UsesNetherGold,
                IsTreasureKey = typedShopKey,
                ShopKeyIdentity = typedShopKey ? shopKeyIdentity : 0,
                IsKnown = known,
                UnknownReason = known ? string.Empty : "invalid-shop-inventory-row:" + content.ContentId,
            });
        }
    }

    private static NetherStrategyVisibleContentRow Unknown(
        NetherStrategyVisibleContentKind kind,
        NetherFloorNode floor,
        string reason,
        long masterRowId = 0,
        long contentId = 0,
        long eventId = 0,
        long eventPartId = 0
    ) => new(kind, floor.NodeId, masterRowId, contentId)
    {
        MapFloorMasterId = floor.FloorId,
        EventId = eventId,
        EventPartId = eventPartId,
        IsKnown = false,
        UnknownReason = reason,
    };

    private static bool TryUnique<T>(
        IEnumerable<T> source,
        Func<T, long> keySelector,
        out Dictionary<long, T> mapped
    )
    {
        mapped = new Dictionary<long, T>();
        foreach (T row in source)
        {
            long key = keySelector(row);
            if (key <= 0 || !mapped.TryAdd(key, row))
                return false;
        }
        return true;
    }

    private static bool TryUniqueOptionMaster<T>(
        IEnumerable<T> source,
        Func<T, long> keySelector,
        Func<T, bool> isValid,
        out Dictionary<long, T> mapped
    )
    {
        mapped = new Dictionary<long, T>();
        var ambiguous = new HashSet<long>();
        foreach (T row in source)
        {
            long key = keySelector(row);
            if (key <= 0)
                continue;
            if (!isValid(row))
            {
                mapped.Remove(key);
                ambiguous.Add(key);
                continue;
            }
            if (ambiguous.Contains(key))
                continue;
            if (!mapped.TryAdd(key, row))
            {
                mapped.Remove(key);
                ambiguous.Add(key);
            }
        }
        return true;
    }

    private static bool TryUniqueEventPartRows(
        IEnumerable<NetherFloorEventPartMasterRow> source,
        out Dictionary<long, NetherFloorEventPartMasterRow> mapped
    )
    {
        mapped = new Dictionary<long, NetherFloorEventPartMasterRow>();
        var ambiguous = new HashSet<long>();
        foreach (NetherFloorEventPartMasterRow row in source)
        {
            if (!row.HasRequiredFields)
            {
                if (row.PartId > 0)
                {
                    mapped.Remove(row.PartId);
                    ambiguous.Add(row.PartId);
                }
                continue;
            }
            if (row.PartId <= 0 || ambiguous.Contains(row.PartId))
                continue;
            if (!mapped.TryAdd(row.PartId, row))
            {
                mapped.Remove(row.PartId);
                ambiguous.Add(row.PartId);
            }
        }
        return true;
    }
}
