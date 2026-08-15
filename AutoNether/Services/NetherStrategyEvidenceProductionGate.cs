#nullable enable

namespace AutoNether.Services;

/// <summary>
/// One observation of the native FloorSelection owner.  The controller reference is retained
/// only for the duration of capture; it is never exposed to strategy policy.
/// </summary>
internal readonly record struct NetherStrategyEvidenceCaptureBoundary(
    object? Controller,
    long RuntimeGeneration,
    long ObservedSubsceneGeneration,
    long EnteredSubsceneGeneration
);

internal readonly record struct NetherRuntimeStrategyEvidenceResult(
    NetherStrategyEvidencePackage? Package,
    string Detail
)
{
    public bool IsSuccess => Package != null && Detail.Length == 0;

    public static NetherRuntimeStrategyEvidenceResult Success(NetherStrategyEvidencePackage package) =>
        new(package, string.Empty);

    public static NetherRuntimeStrategyEvidenceResult Failure(string detail) =>
        new(null, detail);
}

/// <summary>
/// Production-wiring gate around the immutable mapper.  It proves the capture did not straddle a
/// controller replacement, then reuses the same generation/owner/OnEntered/snapshot acceptance
/// contract that guards strategy execution.
/// </summary>
internal static class NetherStrategyEvidenceProductionGate
{
    public static NetherRuntimeStrategyEvidenceResult Bind(
        NetherStrategyEvidenceMapResult mapped,
        NetherStrategyEvidenceCaptureBoundary before,
        NetherStrategyEvidenceCaptureBoundary after,
        NetherSnapshotFingerprint authoritativeSnapshot
    )
    {
        if (before.Controller == null || after.Controller == null)
            return NetherRuntimeStrategyEvidenceResult.Failure("strategy-evidence-controller-unavailable");
        if (!ReferenceEquals(before.Controller, after.Controller)
            || before.RuntimeGeneration != after.RuntimeGeneration)
        {
            return NetherRuntimeStrategyEvidenceResult.Failure(
                "strategy-evidence-controller-replaced-during-capture"
            );
        }
        if (before.ObservedSubsceneGeneration != before.RuntimeGeneration
            || after.ObservedSubsceneGeneration != after.RuntimeGeneration)
        {
            return NetherRuntimeStrategyEvidenceResult.Failure(
                "strategy-evidence-observed-subscene-mismatch"
            );
        }
        if (!mapped.IsMapped || mapped.Package == null)
        {
            return NetherRuntimeStrategyEvidenceResult.Failure(
                "strategy-evidence-map:" + mapped.Detail
            );
        }

        NetherStrategyEvidenceAcceptanceDecision accepted = NetherStrategyEvidenceAcceptance.Evaluate(
            mapped.Package,
            after.RuntimeGeneration,
            after.RuntimeGeneration,
            after.EnteredSubsceneGeneration,
            authoritativeSnapshot
        );
        return accepted.IsAccepted
            ? NetherRuntimeStrategyEvidenceResult.Success(mapped.Package)
            : NetherRuntimeStrategyEvidenceResult.Failure(accepted.Detail);
    }
}
