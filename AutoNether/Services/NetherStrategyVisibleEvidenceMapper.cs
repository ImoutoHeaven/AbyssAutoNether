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
);

/// <summary>Exact current MNetherFloorTreasures relation.</summary>
internal readonly record struct NetherStrategyTreasureMasterRow(long Id, long MapFloorMasterId);

/// <summary>Exact non-localized MItems reward fields visible from an Event/Treasure part.</summary>
internal readonly record struct NetherStrategyItemMasterRow(
    long Id,
    long ItemType,
    int Rarity,
    int Value,
    int PossessionLimit
);

internal sealed record NetherStrategyShopInventoryCapture(
    bool IsMaterialized,
    IReadOnlyList<NetherShopContent> Contents,
    string UnknownReason
);

internal sealed record NetherStrategyVisibleEvidenceCaptureRequest(
    IReadOnlyList<NetherFloorNode> Floors,
    IReadOnlyList<NetherStrategyBattleMasterRow> BattleRows,
    IReadOnlyList<NetherStrategyTreasureMasterRow> TreasureRows,
    IReadOnlyList<NetherFloorEventMasterRow> EventRows,
    IReadOnlyList<NetherFloorEventPartMasterRow> EventPartRows,
    IReadOnlyList<NetherStrategyItemMasterRow> ItemRows
)
{
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
        if (!TryUnique(request.BattleRows, row => row.Id, out Dictionary<long, NetherStrategyBattleMasterRow> battleById)
            || !TryUnique(request.TreasureRows, row => row.Id, out Dictionary<long, NetherStrategyTreasureMasterRow> treasureById)
            || !TryUnique(request.EventRows, row => row.EventId, out Dictionary<long, NetherFloorEventMasterRow> eventById)
            || !TryUnique(request.EventPartRows, row => row.PartId, out Dictionary<long, NetherFloorEventPartMasterRow> partById)
            || !TryUnique(request.ItemRows, row => row.Id, out Dictionary<long, NetherStrategyItemMasterRow> itemById))
        {
            return NetherStrategyVisibleEvidenceCaptureResult.Failure(
                "duplicate-or-invalid-visible-master-row"
            );
        }

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
                        battleById
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
                        battleById
                    );
                    break;
                case NetherFloorNodeType.Shop:
                    AppendShop(rows, floor, request.ShopInventoryByNodeId);
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
        IReadOnlyDictionary<long, NetherStrategyBattleMasterRow> battleById
    )
    {
        NetherStrategyTreasureMasterRow[] matches = treasureById.Values
            .Where(row => row.MapFloorMasterId == floor.FloorId)
            .ToArray();
        if (matches.Length != 1)
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
        if (!TryResolveEvents(floor, request, eventById, out NetherFloorEventMasterRow[] events, out string error))
        {
            rows.Add(new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Treasure,
                floor.NodeId,
                matches[0].Id,
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
            rows.Add(new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Treasure,
                floor.NodeId,
                matches[0].Id,
                eventRow.EventId
            )
            {
                MapFloorMasterId = floor.FloorId,
                EventId = eventRow.EventId,
                Weight = Math.Max(0, eventRow.Weight),
            });
            AppendEventParts(rows, floor, eventRow, partById, itemById, battleById);
        }
    }

    private static void AppendFloorEvents(
        ICollection<NetherStrategyVisibleContentRow> rows,
        NetherFloorNode floor,
        NetherStrategyVisibleEvidenceCaptureRequest request,
        IReadOnlyDictionary<long, NetherFloorEventMasterRow> eventById,
        IReadOnlyDictionary<long, NetherFloorEventPartMasterRow> partById,
        IReadOnlyDictionary<long, NetherStrategyItemMasterRow> itemById,
        IReadOnlyDictionary<long, NetherStrategyBattleMasterRow> battleById
    )
    {
        if (!TryResolveEvents(floor, request, eventById, out NetherFloorEventMasterRow[] events, out string error))
        {
            rows.Add(Unknown(NetherStrategyVisibleContentKind.Event, floor, error));
            return;
        }
        foreach (NetherFloorEventMasterRow eventRow in events)
            AppendEventParts(rows, floor, eventRow, partById, itemById, battleById);
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
        IReadOnlyDictionary<long, NetherStrategyBattleMasterRow> battleById
    )
    {
        long[] partIds = { eventRow.PartId1, eventRow.PartId2, eventRow.PartId3, eventRow.PartId4 };
        for (int index = 0; index < partIds.Length; index++)
        {
            long partId = partIds[index];
            if (partId <= 0)
                continue;
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
                        MapEventEffects(part)
                    ),
                ],
            });
            if (part.ContentType is 30 or 31)
                AppendItem(rows, floor, eventRow, part, itemById, amount, amountKnown);
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
                    rows.Add(Battle(floor, battle, isBoss: false, eventRow.EventId, part.PartId));
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
        NetherFloorEventPartMasterRow part
    ) =>
    [
        MapTargetEffect(
            NetherStrategyVisibleEventEffectSource.Target1,
            part.TargetType1,
            part.SelectParameter1,
            part.PartId
        ),
        MapTargetEffect(
            NetherStrategyVisibleEventEffectSource.Target2,
            part.TargetType2,
            part.SelectParameter2,
            part.PartId
        ),
        MapTargetEffect(
            NetherStrategyVisibleEventEffectSource.Target3,
            part.TargetType3,
            part.SelectParameter3,
            part.PartId
        ),
        MapContentEffect(part),
    ];

    private static NetherStrategyVisibleEventEffectEvidence MapTargetEffect(
        NetherStrategyVisibleEventEffectSource source,
        int rawType,
        long parameter,
        long partId
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
        bool known = rawType is >= 1 and <= 8 && parameter >= 0;
        return new NetherStrategyVisibleEventEffectEvidence(source, rawType, parameter)
        {
            EffectKind = known ? (NetherEffectKind)rawType : NetherEffectKind.Unknown,
            IsPresent = true,
            IsKnown = known,
            UnknownReason = known
                ? string.Empty
                : "unsupported-event-target-type-or-parameter:" + partId + ":" + rawType,
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
            160 when part.ContentId == 0 => NetherEffectKind.AbyssCodeOffer,
            165 when part.ContentId >= 0 => NetherEffectKind.NetherGoldGain,
            166 when part.ContentId >= 0 => NetherEffectKind.TreasureKeyGain,
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
        int amount,
        bool amountKnown
    )
    {
        NetherStrategyItemMasterRow item = default;
        bool itemKnown = part.ContentId > 0 && itemById.TryGetValue(part.ContentId, out item);
        rows.Add(new NetherStrategyVisibleContentRow(
            NetherStrategyVisibleContentKind.Item,
            floor.NodeId,
            itemKnown ? item.Id : Math.Max(0, part.ContentId),
            Math.Max(0, part.ContentId)
        )
        {
            MapFloorMasterId = floor.FloorId,
            EventId = eventRow.EventId,
            EventPartId = part.PartId,
            ContentType = part.ContentType,
            Amount = amount,
            Weight = Math.Max(0, eventRow.Weight),
            ItemType = itemKnown ? item.ItemType : 0,
            ItemRarity = itemKnown ? item.Rarity : 0,
            ItemValue = itemKnown ? item.Value : 0,
            ItemPossessionLimit = itemKnown ? item.PossessionLimit : 0,
            IsKnown = amountKnown && itemKnown,
            UnknownReason = amountKnown && itemKnown
                ? string.Empty
                : !amountKnown
                    ? "invalid-event-item-amount:" + part.PartId
                    : "event-item-master-row-unavailable:" + part.ContentId,
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
        long eventPartId
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
        IsKnown = battle.Id > 0 && battle.BattleStageId > 0 && battle.CodeDropRatio >= 0,
        UnknownReason = battle.Id > 0 && battle.BattleStageId > 0 && battle.CodeDropRatio >= 0
            ? string.Empty
            : "invalid-battle-master-row:" + battle.Id,
    };

    private static void AppendShop(
        ICollection<NetherStrategyVisibleContentRow> rows,
        NetherFloorNode floor,
        IReadOnlyDictionary<long, NetherStrategyShopInventoryCapture> captures
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
            bool known = content.Known
                && content.ContentId > 0
                && content.ItemId >= 0
                && content.Amount > 0
                && content.Price >= 0;
            rows.Add(new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.ShopInventory,
                floor.NodeId,
                content.ContentId,
                Math.Max(0, content.ItemId)
            )
            {
                MapFloorMasterId = floor.FloorId,
                ContentType = content.RawContentType,
                Amount = Math.Max(0, content.Amount),
                Cost = Math.Max(0, content.Price),
                Rank = Math.Max(0, (int)content.Rarity),
                ItemType = content.ItemType,
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
}
