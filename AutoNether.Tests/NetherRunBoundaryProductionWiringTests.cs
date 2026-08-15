#nullable enable

using System;
using System.IO;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherRunBoundaryProductionWiringTests
{
    [Fact]
    public void Runtime_bridge_maps_current_client_run_boundaries_from_master_rows()
    {
        // Fresh Project.dll evidence (SHA-256 53806a5b...1300):
        // MNetherMaps.m_nether_id/max_floor_num and
        // MNetherMapFloors.m_nether_map_id/min_order/max_order/type.
        string source = Read("AutoNether", "Services", "NetherRuntimeBridge.cs");

        Assert.Contains("GetCache<MNetherMapFloors>()", source, StringComparison.Ordinal);
        Assert.Contains("new NetherRunBoundaryMapMasterRow(", source, StringComparison.Ordinal);
        Assert.Contains("new NetherRunBoundaryFloorMasterRow(", source, StringComparison.Ordinal);
        Assert.Contains("NetherRunBoundaryMasterMapper.Map(", source, StringComparison.Ordinal);
        Assert.Contains("AuthoritativeBossFloorLevels = rows.RunBoundary.BossFloorLevels", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Full_and_transition_snapshots_use_fresh_authoritative_run_boundary_evidence()
    {
        // Fresh Project.dll evidence (SHA-256 53806a5b...1300):
        // NetherPointData.RecoveryFloorLevel and NetherData.ContinuanceFloorLevel.
        string bridge = Read("AutoNether", "Services", "NetherRuntimeBridge.cs");
        string cache = Read("AutoNether", "Services", "NetherTransitionSnapshotCache.cs");

        Assert.Contains("RecoveryFloorLevel = pointData.RecoveryFloorLevel", bridge, StringComparison.Ordinal);
        Assert.Contains("MasterMaxFloorLevel = rows!.RunBoundary.MasterMaxFloorLevel", bridge, StringComparison.Ordinal);
        Assert.Contains("RecoveryFloorLevel = state.RecoveryFloorLevel", cache, StringComparison.Ordinal);
        Assert.Contains("MasterMaxFloorLevel = state.MasterMaxFloorLevel", cache, StringComparison.Ordinal);
        Assert.Contains("AuthoritativeBossFloorLevels = state.AuthoritativeBossFloorLevels.ToArray()", cache, StringComparison.Ordinal);
    }

    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(path).ToArray()));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "AutoNether")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
