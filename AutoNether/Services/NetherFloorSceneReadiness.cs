#nullable enable

namespace AutoNether.Services;

/// <summary>
/// Complete proof required before any FloorSelection reconciliation or planning decision.
/// A controller registration alone is deliberately insufficient: the controller must own the
/// current generation, its matching SubScene.OnEntered must have run, and one authoritative
/// snapshot must remain bound to that same controller/generation for the whole capture.
/// </summary>
internal readonly record struct NetherFloorSceneReadinessEvidence(
    long MinimumGenerationExclusive,
    long RuntimeGeneration,
    bool HasCurrentController,
    bool IsExpectedCurrentController,
    bool HasEnteredCurrentGeneration,
    bool HasAuthoritativeSnapshot,
    bool CaptureStayedOnCurrentController
);

internal readonly record struct NetherFloorSceneReadinessDecision(bool IsReady, string Detail);

internal static class NetherFloorSceneReadiness
{
    public static NetherFloorSceneReadinessDecision Evaluate(
        NetherFloorSceneReadinessEvidence evidence
    )
    {
        if (!evidence.HasCurrentController)
            return Waiting("awaiting-current-controller");
        if (evidence.RuntimeGeneration <= evidence.MinimumGenerationExclusive)
        {
            return Waiting(
                "awaiting-new-generation:minimum-exclusive="
                    + evidence.MinimumGenerationExclusive
                    + ":observed="
                    + evidence.RuntimeGeneration
            );
        }
        if (!evidence.IsExpectedCurrentController)
        {
            return Waiting(
                "awaiting-authoritative-controller:generation=" + evidence.RuntimeGeneration
            );
        }
        if (!evidence.HasEnteredCurrentGeneration)
        {
            return Waiting(
                "awaiting-subscene-entered:generation=" + evidence.RuntimeGeneration
            );
        }
        if (!evidence.HasAuthoritativeSnapshot)
        {
            return Waiting(
                "awaiting-authoritative-snapshot:generation=" + evidence.RuntimeGeneration
            );
        }
        if (!evidence.CaptureStayedOnCurrentController)
        {
            return Waiting(
                "controller-changed-during-snapshot:generation=" + evidence.RuntimeGeneration
            );
        }

        return new(true, "floor-scene-ready:generation=" + evidence.RuntimeGeneration);
    }

    private static NetherFloorSceneReadinessDecision Waiting(string detail) => new(false, detail);
}

internal readonly record struct NetherFloorSceneSnapshotResult(
    long RuntimeGeneration,
    NetherSnapshot? Snapshot,
    string Detail
)
{
    public bool IsReady => Snapshot != null;

    public static NetherFloorSceneSnapshotResult Ready(
        long runtimeGeneration,
        NetherSnapshot snapshot
    ) => new(
        runtimeGeneration,
        snapshot,
        "floor-scene-ready:generation=" + runtimeGeneration
    );

    public static NetherFloorSceneSnapshotResult Waiting(
        long runtimeGeneration,
        string detail
    ) => new(runtimeGeneration, null, detail ?? string.Empty);
}

internal interface INetherFloorSceneReadinessDriver
{
    NetherFloorSceneSnapshotResult TryCaptureReadyFloorSceneSnapshot(
        long minimumGenerationExclusive = 0
    );
}
