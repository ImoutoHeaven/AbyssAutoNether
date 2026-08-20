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

/// <summary>
/// Immutable boundary for Code policy captured from a native battle-result-owned code popup.
/// This owner deliberately has no FloorSelection OnEntered proof: the result view owns it until
/// its Next continuation has been invoked.
/// </summary>
internal readonly record struct NetherBattleResultCodeEvidenceCaptureBoundary(
    object? PopupController,
    object? Popup,
    object? PartyModel,
    long RuntimeGeneration,
    long OwnerGeneration,
    long Sequence,
    bool IsCurrentResultOwner
)
{
    public bool IsUsable => PopupController != null
        && Popup != null
        && PartyModel != null
        && RuntimeGeneration > 0
        && OwnerGeneration > 0
        && Sequence > 0
        && IsCurrentResultOwner;
}

internal readonly record struct NetherBattleResultCodeEvidenceCaptureDecision(
    bool IsAccepted,
    string Detail
)
{
    public static NetherBattleResultCodeEvidenceCaptureDecision Accepted { get; } = new(
        true,
        string.Empty
    );

    public static NetherBattleResultCodeEvidenceCaptureDecision Rejected(string detail) => new(
        false,
        detail
    );
}

/// <summary>
/// Proves a result-owned popup and the exact party object remained current across the read-only
/// evidence capture. It intentionally does not substitute a stale FloorSelection owner.
/// </summary>
internal static class NetherBattleResultCodeEvidenceProductionGate
{
    public static NetherBattleResultCodeEvidenceCaptureDecision Evaluate(
        NetherBattleResultCodeEvidenceCaptureBoundary before,
        NetherBattleResultCodeEvidenceCaptureBoundary after
    )
    {
        if (!before.IsUsable)
        {
            return NetherBattleResultCodeEvidenceCaptureDecision.Rejected(
                "battle-result-code-evidence-owner-unavailable"
            );
        }
        if (!after.IsUsable)
        {
            return NetherBattleResultCodeEvidenceCaptureDecision.Rejected(
                "battle-result-code-evidence-owner-lost-during-capture"
            );
        }
        if (!ReferenceEquals(before.PopupController, after.PopupController)
            || !ReferenceEquals(before.Popup, after.Popup)
            || !ReferenceEquals(before.PartyModel, after.PartyModel)
            || before.RuntimeGeneration != after.RuntimeGeneration
            || before.OwnerGeneration != after.OwnerGeneration
            || before.Sequence != after.Sequence)
        {
            return NetherBattleResultCodeEvidenceCaptureDecision.Rejected(
                "battle-result-code-evidence-owner-replaced-during-capture"
            );
        }
        return NetherBattleResultCodeEvidenceCaptureDecision.Accepted;
    }
}
