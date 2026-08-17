#nullable enable

using System;

namespace AutoNether.Services;

internal enum NetherTreasureOpenAnimationObservationKind
{
    Waiting,
    Ready,
    BindingUnavailable,
}

internal readonly record struct NetherTreasureOpenAnimationObservation(
    NetherTreasureOpenAnimationObservationKind Kind,
    string Detail
)
{
    public static NetherTreasureOpenAnimationObservation Waiting(string detail) => new(
        NetherTreasureOpenAnimationObservationKind.Waiting,
        detail
    );

    public static NetherTreasureOpenAnimationObservation Ready() => new(
        NetherTreasureOpenAnimationObservationKind.Ready,
        string.Empty
    );

    public static NetherTreasureOpenAnimationObservation BindingUnavailable(string detail) => new(
        NetherTreasureOpenAnimationObservationKind.BindingUnavailable,
        detail
    );
}

internal interface INetherTreasureConfirmationPort
{
    NetherTreasureOpenAnimationObservation ObserveOpenAnimation(
        NetherTreasureConfirmOwner owner
    );

    NetherTreasureOpenAnimationObservation ObserveNativeButtonSubscription(
        NetherTreasureConfirmOwner owner
    );

    NetherNativeActionResult InvokeConfirm(NetherTreasureConfirmOwner owner);

    void LogStage(NetherTreasureConfirmOwner owner, string stage, string outcome);
}

/// <summary>
/// Owns the exact Treasure child sequence beneath a SelectFloor or recovered native parent.
/// The port contains only current-game reflection and task handles; this coordinator owns the
/// no-replay order and bounded readiness wait.
/// </summary>
internal sealed class NetherTreasureConfirmationCoordinator
{
    private readonly INetherTreasureConfirmationPort _port;
    private readonly NetherTreasureConfirmLease _lease = new();
    private readonly int _maximumOpenAnimationPolls;
    private int _openAnimationPolls;
    private int _nativeButtonSubscriptionPolls;

    public NetherTreasureConfirmationCoordinator(
        INetherTreasureConfirmationPort port,
        int maximumOpenAnimationPolls = 600
    )
    {
        _port = port ?? throw new ArgumentNullException(nameof(port));
        if (maximumOpenAnimationPolls < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumOpenAnimationPolls));
        _maximumOpenAnimationPolls = maximumOpenAnimationPolls;
    }

    public NetherTreasureConfirmStage Stage => _lease.Stage;

    public bool Begin(NetherTreasureConfirmOwner owner)
    {
        _openAnimationPolls = 0;
        _nativeButtonSubscriptionPolls = 0;
        return _lease.Begin(owner);
    }

    public bool TryGetOwner(out NetherTreasureConfirmOwner owner) =>
        _lease.TryGetOwner(out owner);

    public bool InvalidatePopup(object? popup)
    {
        bool invalidated = _lease.InvalidatePopup(popup);
        if (invalidated)
        {
            _openAnimationPolls = 0;
            _nativeButtonSubscriptionPolls = 0;
        }
        return invalidated;
    }

    public void Reset()
    {
        _openAnimationPolls = 0;
        _nativeButtonSubscriptionPolls = 0;
        _lease.Reset();
    }

    public NetherNativeActionResult Pump()
    {
        if (!_lease.TryGetOwner(out NetherTreasureConfirmOwner owner))
            return NetherNativeActionResult.Completed("no-treasure-confirm");

        switch (_lease.Stage)
        {
            case NetherTreasureConfirmStage.AwaitingOpenAnimation:
            {
                NetherTreasureOpenAnimationObservation observation =
                    _port.ObserveOpenAnimation(owner);
                if (observation.Kind
                    == NetherTreasureOpenAnimationObservationKind.BindingUnavailable)
                {
                    return NetherNativeActionResult.BindingUnavailable(observation.Detail);
                }
                if (observation.Kind == NetherTreasureOpenAnimationObservationKind.Waiting)
                {
                    _openAnimationPolls++;
                    return _openAnimationPolls <= _maximumOpenAnimationPolls
                        ? NetherNativeActionResult.Started(
                            string.IsNullOrWhiteSpace(observation.Detail)
                                ? "awaiting-native-treasure-open-animation-task"
                                : observation.Detail
                        )
                        : NetherNativeActionResult.BindingUnavailable(
                            "native-treasure-open-animation-task-timeout"
                        );
                }
                if (observation.Kind != NetherTreasureOpenAnimationObservationKind.Ready)
                {
                    return NetherNativeActionResult.BindingUnavailable(
                        "invalid-treasure-open-animation-observation:"
                            + observation.Kind
                    );
                }

                _openAnimationPolls = 0;
                _nativeButtonSubscriptionPolls = 0;
                if (!_lease.ObserveOpenAnimationReady(owner, openAnimationObserved: true))
                {
                    return NetherNativeActionResult.BindingUnavailable(
                        "treasure-confirm-open-observation-rejected"
                    );
                }
                // Fresh Cpp2IL shows that the controller's own SkipAndConfirmButton tap path,
                // not SkipOpenTreasureAnimationAsync, owns native Skip animation behavior.
                _port.LogStage(owner, "open-observed", "Started");
                return NetherNativeActionResult.Started(
                    "native-treasure-open-observed-awaiting-button-subscription"
                );
            }

            case NetherTreasureConfirmStage.AwaitingNativeButtonSubscription:
            {
                NetherTreasureOpenAnimationObservation observation =
                    _port.ObserveNativeButtonSubscription(owner);
                if (observation.Kind
                    == NetherTreasureOpenAnimationObservationKind.BindingUnavailable)
                {
                    return NetherNativeActionResult.BindingUnavailable(observation.Detail);
                }
                if (observation.Kind == NetherTreasureOpenAnimationObservationKind.Waiting)
                {
                    _nativeButtonSubscriptionPolls++;
                    return _nativeButtonSubscriptionPolls <= _maximumOpenAnimationPolls
                        ? NetherNativeActionResult.Started(
                            string.IsNullOrWhiteSpace(observation.Detail)
                                ? "awaiting-native-treasure-button-subscription"
                                : observation.Detail
                        )
                        : NetherNativeActionResult.BindingUnavailable(
                            "native-treasure-button-subscription-timeout"
                        );
                }
                if (observation.Kind != NetherTreasureOpenAnimationObservationKind.Ready)
                {
                    return NetherNativeActionResult.BindingUnavailable(
                        "invalid-treasure-button-subscription-observation:"
                            + observation.Kind
                    );
                }

                _nativeButtonSubscriptionPolls = 0;
                if (!_lease.ObserveNativeButtonSubscription(owner))
                {
                    return NetherNativeActionResult.BindingUnavailable(
                        "treasure-confirm-button-subscription-observation-rejected"
                    );
                }
                _port.LogStage(owner, "native-button-subscription-observed", "Started");
                return NetherNativeActionResult.Started(
                    "native-treasure-button-subscription-observed-awaiting-controller-resume"
                );
            }

            case NetherTreasureConfirmStage.AwaitingResumePump:
                if (!_lease.AdvanceResumePump(owner))
                {
                    return NetherNativeActionResult.BindingUnavailable(
                        "treasure-confirm-resume-pump-rejected"
                    );
                }
                _port.LogStage(owner, "controller-resume-pump", "Started");
                return NetherNativeActionResult.Started(
                    "native-treasure-controller-resume-pump"
                );

            case NetherTreasureConfirmStage.AwaitingConfirmTap:
                if (!_lease.TryClaimConfirm(owner))
                {
                    return NetherNativeActionResult.BindingUnavailable(
                        "treasure-confirm-tap-claim-rejected"
                    );
                }
                return _port.InvokeConfirm(owner);

            case NetherTreasureConfirmStage.Completed:
                return NetherNativeActionResult.Completed("treasure-confirm-tap-completed");

            default:
                return NetherNativeActionResult.BindingUnavailable(
                    "invalid-treasure-confirm-stage:" + _lease.Stage
                );
        }
    }
}
