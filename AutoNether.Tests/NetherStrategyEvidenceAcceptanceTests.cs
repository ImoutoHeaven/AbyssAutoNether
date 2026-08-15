#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherStrategyEvidenceAcceptanceTests
{
    [Fact]
    public void Package_is_accepted_only_for_current_generation_owner_snapshot_and_entered_subscene()
    {
        // Fresh current-client evidence: FloorSelection.SubScene owns `_subViewController` and
        // protected OnEntered(); SubViewController owns `_netherModel`.  The transaction may use
        // evidence only when all four identities still describe that exact live owner.
        NetherSnapshotFingerprint fingerprint = Fingerprint(21);
        var package = new NetherStrategyEvidencePackage
        {
            Identity = new NetherStrategyEvidenceIdentity(
                RuntimeGeneration: 8,
                ControllerOwnerGeneration: 8,
                EnteredSubsceneGeneration: 8,
                SnapshotFingerprint: fingerprint
            ),
        };

        NetherStrategyEvidenceAcceptanceDecision accepted =
            NetherStrategyEvidenceAcceptance.Evaluate(package, 8, 8, 8, fingerprint);
        Assert.True(accepted.IsAccepted);

        Assert.Equal(
            "strategy-evidence-runtime-generation-mismatch",
            NetherStrategyEvidenceAcceptance.Evaluate(package, 9, 8, 8, fingerprint).Detail
        );
        Assert.Equal(
            "strategy-evidence-controller-owner-mismatch",
            NetherStrategyEvidenceAcceptance.Evaluate(package, 8, 9, 8, fingerprint).Detail
        );
        Assert.Equal(
            "strategy-evidence-entered-subscene-mismatch",
            NetherStrategyEvidenceAcceptance.Evaluate(package, 8, 8, 9, fingerprint).Detail
        );
        Assert.Equal(
            "strategy-evidence-authoritative-snapshot-mismatch",
            NetherStrategyEvidenceAcceptance.Evaluate(package, 8, 8, 8, Fingerprint(22)).Detail
        );
    }

    private static NetherSnapshotFingerprint Fingerprint(int floorLevel) => new(
        NetherSessionStatus.Play,
        101,
        202,
        floorLevel,
        0,
        20,
        "party",
        "codes",
        "map",
        currentFloorId: 303,
        currentNodeId: 404
    );
}
