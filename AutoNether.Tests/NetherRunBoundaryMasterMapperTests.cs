using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherRunBoundaryMasterMapperTests
{
    [Fact]
    public void Fresh_master_rows_map_only_exact_battle_boss_orders_for_the_current_nether()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300:
        // MNetherMaps exposes id/m_nether_id/max_floor_num; MNetherMapFloors exposes
        // m_nether_map_id/min_order/max_order/type; NetherFloorType.BattleBoss is value 2.
        NetherRunBoundaryMasterMapResult result = NetherRunBoundaryMasterMapper.Map(
            currentMapId: 101,
            maps: new[]
            {
                new NetherRunBoundaryMapMasterRow(101, 7, 80),
                new NetherRunBoundaryMapMasterRow(102, 7, 80),
                new NetherRunBoundaryMapMasterRow(201, 8, 130),
            },
            floors: new[]
            {
                new NetherRunBoundaryFloorMasterRow(1, 101, 10, 10, rawType: 2),
                new NetherRunBoundaryFloorMasterRow(2, 101, 11, 19, rawType: 1),
                new NetherRunBoundaryFloorMasterRow(3, 102, 20, 20, rawType: 2),
                new NetherRunBoundaryFloorMasterRow(4, 201, 30, 30, rawType: 2),
            }
        );

        Assert.True(result.IsMapped);
        Assert.Equal(80, result.MasterMaxFloorLevel);
        Assert.Equal(new[] { 10, 20 }, result.BossFloorLevels);
    }

    [Fact]
    public void Ranged_or_out_of_cap_boss_rows_fail_closed_instead_of_inventing_a_floor()
    {
        NetherRunBoundaryMasterMapResult result = NetherRunBoundaryMasterMapper.Map(
            currentMapId: 101,
            maps: new[] { new NetherRunBoundaryMapMasterRow(101, 7, 80) },
            floors: new[]
            {
                new NetherRunBoundaryFloorMasterRow(1, 101, 70, 80, rawType: 2),
            }
        );

        Assert.False(result.IsMapped);
        Assert.Equal("non-exact-authoritative-boss-order:1", result.Detail);
    }
}
