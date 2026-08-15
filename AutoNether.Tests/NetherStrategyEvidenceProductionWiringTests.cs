#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherStrategyEvidenceProductionWiringTests
{
    [Fact]
    public void Production_gate_accepts_only_a_stable_current_controller_and_full_entered_snapshot_binding()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300: FloorSelection.SubScene owns
        // `_subViewController` and protected virtual OnEntered(); SubViewController owns
        // `_netherModel`. A server snapshot cannot substitute for either owner fact.
        var controller = new object();
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(
                new NetherStrategyEvidenceIdentity(7, 7, 7, snapshot.Fingerprint),
                snapshot
            )
        );

        NetherRuntimeStrategyEvidenceResult accepted = NetherStrategyEvidenceProductionGate.Bind(
            mapped,
            new NetherStrategyEvidenceCaptureBoundary(controller, 7, 7, 7),
            new NetherStrategyEvidenceCaptureBoundary(controller, 7, 7, 7),
            snapshot.Fingerprint
        );

        Assert.True(accepted.IsSuccess, accepted.Detail);
        Assert.Same(mapped.Package, accepted.Package);

        Assert.Equal(
            "strategy-evidence-controller-replaced-during-capture",
            NetherStrategyEvidenceProductionGate.Bind(
                mapped,
                new NetherStrategyEvidenceCaptureBoundary(controller, 7, 7, 7),
                new NetherStrategyEvidenceCaptureBoundary(new object(), 8, 8, 8),
                snapshot.Fingerprint
            ).Detail
        );
        Assert.Equal(
            "strategy-evidence-entered-subscene-mismatch",
            NetherStrategyEvidenceProductionGate.Bind(
                mapped,
                new NetherStrategyEvidenceCaptureBoundary(controller, 7, 7, 7),
                new NetherStrategyEvidenceCaptureBoundary(controller, 7, 7, 6),
                snapshot.Fingerprint
            ).Detail
        );
    }

    private static NetherSnapshot Snapshot() => new()
    {
        Status = NetherSessionStatus.Play,
        NetherId = 1,
        MapId = 2,
        CurrentFloorId = 3,
        CurrentNodeId = 4,
        FloorLevel = 10,
        MasterMaxFloorLevel = 70,
        AuthoritativeBossFloorLevels = new[] { 10, 20, 30, 40, 50, 60, 70 },
        CharacterHpHash = "party",
        CodeHash = "codes",
        MapHash = "map",
    };
}
