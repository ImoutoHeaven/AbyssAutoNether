#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal readonly record struct NetherRunBoundaryMapMasterRow(
    long MapId,
    long NetherId,
    int MaxFloorLevel
);

internal sealed record NetherRunBoundaryFloorMasterRow
{
    public NetherRunBoundaryFloorMasterRow(
        long floorMasterId,
        long mapId,
        int minimumOrder,
        int maximumOrder,
        int rawType
    )
    {
        FloorMasterId = floorMasterId;
        MapId = mapId;
        MinimumOrder = minimumOrder;
        MaximumOrder = maximumOrder;
        RawType = rawType;
    }

    public long FloorMasterId { get; }
    public long MapId { get; }
    public int MinimumOrder { get; }
    public int MaximumOrder { get; }
    public int RawType { get; }
}

internal sealed record NetherRunBoundaryMasterMapResult
{
    public bool IsMapped { get; init; }
    public int MasterMaxFloorLevel { get; init; }
    public IReadOnlyList<int> BossFloorLevels { get; init; } = Array.Empty<int>();
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// Pure adapter for the exact current-client master fields. BattleBoss is raw native value 2;
/// an exact Boss boundary exists only when its MNetherMapFloors min/max order agree.
/// </summary>
internal static class NetherRunBoundaryMasterMapper
{
    private const int BattleBossRawType = 2;

    public static NetherRunBoundaryMasterMapResult Map(
        long currentMapId,
        IReadOnlyList<NetherRunBoundaryMapMasterRow>? maps,
        IReadOnlyList<NetherRunBoundaryFloorMasterRow>? floors
    )
    {
        if (currentMapId <= 0 || maps == null || floors == null)
            return Failure("missing-run-boundary-master-data");

        NetherRunBoundaryMapMasterRow[] current = maps
            .Where(row => row.MapId == currentMapId)
            .ToArray();
        if (current.Length != 1
            || current[0].NetherId <= 0
            || current[0].MaxFloorLevel < 1)
        {
            return Failure("current-nether-map-master-unavailable");
        }

        NetherRunBoundaryMapMasterRow currentMap = current[0];
        long[] mapIds = maps
            .Where(row => row.NetherId == currentMap.NetherId)
            .Select(row => row.MapId)
            .ToArray();
        if (mapIds.Length == 0
            || mapIds.Any(id => id <= 0)
            || mapIds.Distinct().Count() != mapIds.Length)
        {
            return Failure("invalid-current-nether-map-set");
        }
        var mapIdSet = new HashSet<long>(mapIds);

        var bosses = new SortedSet<int>();
        foreach (NetherRunBoundaryFloorMasterRow? row in floors)
        {
            if (row == null
                || row.FloorMasterId <= 0
                || row.MapId <= 0)
            {
                return Failure("invalid-nether-map-floor-master");
            }
            if (!mapIdSet.Contains(row.MapId) || row.RawType != BattleBossRawType)
                continue;
            if (row.MinimumOrder < 1
                || row.MinimumOrder != row.MaximumOrder
                || row.MaximumOrder > currentMap.MaxFloorLevel)
            {
                return Failure(
                    "non-exact-authoritative-boss-order:" + row.FloorMasterId
                );
            }
            bosses.Add(row.MaximumOrder);
        }

        if (bosses.Count == 0)
            return Failure("authoritative-boss-floor-unavailable");

        return new NetherRunBoundaryMasterMapResult
        {
            IsMapped = true,
            MasterMaxFloorLevel = currentMap.MaxFloorLevel,
            BossFloorLevels = Array.AsReadOnly(bosses.ToArray()),
        };
    }

    private static NetherRunBoundaryMasterMapResult Failure(string detail) => new()
    {
        IsMapped = false,
        Detail = detail,
    };
}
