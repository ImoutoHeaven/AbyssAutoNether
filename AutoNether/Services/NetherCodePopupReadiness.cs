#nullable enable

namespace AutoNether.Services;

internal enum NetherCodePopupReadinessKind
{
    Invalid,
    Ready,
    Pending,
    Unavailable,
}

internal readonly record struct NetherCodePopupReadinessResult
{
    private NetherCodePopupReadinessResult(
        NetherCodePopupReadinessKind kind,
        string detail
    )
    {
        Kind = kind;
        Detail = detail ?? string.Empty;
    }

    public NetherCodePopupReadinessKind Kind { get; }
    public string Detail { get; }

    public bool IsReady => Kind == NetherCodePopupReadinessKind.Ready;
    public bool IsPending => Kind == NetherCodePopupReadinessKind.Pending;

    public static NetherCodePopupReadinessResult Ready() =>
        new(NetherCodePopupReadinessKind.Ready, string.Empty);

    public static NetherCodePopupReadinessResult Pending(string detail) =>
        new(NetherCodePopupReadinessKind.Pending, detail);

    public static NetherCodePopupReadinessResult Unavailable(string detail) =>
        new(NetherCodePopupReadinessKind.Unavailable, detail);
}

internal readonly record struct NetherPopupReadinessIdentity(
    long RuntimeGeneration,
    NetherActionKind OwnerAction,
    long OwnerGeneration,
    long Sequence
)
{
    public bool IsValid => RuntimeGeneration > 0
        && OwnerAction != NetherActionKind.None
        && OwnerGeneration > 0
        // Sequence zero is the bounded owner-scoped gap before that owner's popup controller
        // registers.  A registered popup always carries a strictly positive sequence, which
        // advances the identity and starts a fresh model-initialization budget.
        && Sequence >= 0;

    public static NetherPopupReadinessIdentity From(NetherRuntimePopupContext popup) => new(
        popup.RuntimeGeneration,
        popup.OwnerAction,
        popup.OwnerGeneration,
        popup.Sequence
    );
}

/// <summary>
/// Bounds one exact native popup registration or initialization gap. A new runtime, owner, or
/// popup sequence starts a fresh budget; callers must clear the gate at every scene and
/// automation boundary.
/// </summary>
internal sealed class NetherPopupReadinessGate
{
    private readonly NetherNativeWaitGate _wait;
    private NetherPopupReadinessIdentity? _identity;

    public NetherPopupReadinessGate(int maximumPendingPolls) =>
        _wait = new NetherNativeWaitGate(maximumPendingPolls);

    public NetherNativeActionResult Await(
        NetherPopupReadinessIdentity identity,
        string boundary
    )
    {
        if (!identity.IsValid)
        {
            Clear();
            return NetherNativeActionResult.BindingUnavailable(
                "native-" + boundary + "-identity-unavailable"
            );
        }

        if (_identity != identity)
        {
            _wait.Clear();
            _identity = identity;
        }
        return _wait.AwaitRegistration(boundary);
    }

    public void ObserveReady() => Clear();

    public void Clear()
    {
        _identity = null;
        _wait.Clear();
    }
}

internal static class NetherCodePopupReadiness
{
    public static NetherCodePopupReadinessResult Evaluate(
        bool offerIdsReadable,
        int offerIdCount,
        bool modelReadable,
        bool hasModel
    )
    {
        if (!offerIdsReadable)
            return NetherCodePopupReadinessResult.Unavailable("code-offer-ids-member-unavailable");
        if (offerIdCount <= 0)
            return NetherCodePopupReadinessResult.Unavailable("code-offer-ids-empty");
        if (!modelReadable)
            return NetherCodePopupReadinessResult.Unavailable("code-offer-model-member-unavailable");
        if (!hasModel)
            return NetherCodePopupReadinessResult.Pending("code-offer-model-not-ready");
        return NetherCodePopupReadinessResult.Ready();
    }
}

internal enum NetherOwnedCodePopupRegistrationKind
{
    Invalid,
    Ready,
    AwaitingFirstRegistration,
    Unavailable,
}

internal readonly record struct NetherOwnedCodePopupRegistrationDecision
{
    private NetherOwnedCodePopupRegistrationDecision(
        NetherOwnedCodePopupRegistrationKind kind,
        string detail
    )
    {
        Kind = kind;
        Detail = detail ?? string.Empty;
    }

    public NetherOwnedCodePopupRegistrationKind Kind { get; }
    public string Detail { get; }

    public bool IsReady => Kind == NetherOwnedCodePopupRegistrationKind.Ready;
    public bool IsAwaiting => Kind == NetherOwnedCodePopupRegistrationKind.AwaitingFirstRegistration;

    public static NetherOwnedCodePopupRegistrationDecision Ready() =>
        new(NetherOwnedCodePopupRegistrationKind.Ready, string.Empty);

    public static NetherOwnedCodePopupRegistrationDecision Awaiting(string detail) =>
        new(NetherOwnedCodePopupRegistrationKind.AwaitingFirstRegistration, detail);

    public static NetherOwnedCodePopupRegistrationDecision Unavailable(string detail) =>
        new(NetherOwnedCodePopupRegistrationKind.Unavailable, detail);
}

/// <summary>
/// Separates a live owner's initial or replacement-registration gap from a stale or conflicting
/// registration. Native Code-popup initialization awaits live data after registration, so a
/// closed early popup may be replaced before its exact owner completes. The caller bounds this
/// gap and never manufactures a ready popup.
/// </summary>
internal static class NetherOwnedCodePopupRegistrationReadiness
{
    public static NetherOwnedCodePopupRegistrationDecision Evaluate(
        string boundary,
        long currentRuntimeGeneration,
        NetherActionKind expectedOwnerAction,
        long expectedOwnerGeneration,
        long observedSequence,
        bool hasRegistration,
        bool registrationIsLive,
        long registrationRuntimeGeneration,
        NetherActionKind registrationOwnerAction,
        long registrationOwnerGeneration,
        long registrationSequence
    )
    {
        if (currentRuntimeGeneration <= 0 || expectedOwnerGeneration <= 0)
        {
            return NetherOwnedCodePopupRegistrationDecision.Unavailable(
                boundary + "-owner-unavailable:generation=" + expectedOwnerGeneration
            );
        }

        if (!hasRegistration)
        {
            return observedSequence > 0
                ? NetherOwnedCodePopupRegistrationDecision.Awaiting(
                    "awaiting-live-" + boundary + "-popup-replacement:generation="
                        + expectedOwnerGeneration + ":previous-sequence=" + observedSequence
                )
                : NetherOwnedCodePopupRegistrationDecision.Awaiting(
                    "awaiting-live-" + boundary + "-popup:generation=" + expectedOwnerGeneration
                );
        }

        if (!registrationIsLive
            || registrationRuntimeGeneration != currentRuntimeGeneration
            || registrationOwnerAction != expectedOwnerAction
            || registrationOwnerGeneration != expectedOwnerGeneration
            || observedSequence <= 0
            || registrationSequence != observedSequence)
        {
            return NetherOwnedCodePopupRegistrationDecision.Unavailable(
                boundary
                    + "-popup-owner-mismatch:registered-runtime="
                    + registrationRuntimeGeneration
                    + ":current-runtime="
                    + currentRuntimeGeneration
                    + ":registered-owner="
                    + registrationOwnerAction
                    + ":registered-generation="
                    + registrationOwnerGeneration
                    + ":expected-generation="
                    + expectedOwnerGeneration
                    + ":registered-sequence="
                    + registrationSequence
                    + ":observed-sequence="
                    + observedSequence
            );
        }

        return NetherOwnedCodePopupRegistrationDecision.Ready();
    }
}
