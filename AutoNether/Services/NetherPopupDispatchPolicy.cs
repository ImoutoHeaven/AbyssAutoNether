#nullable enable

using System;
using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>Native popup category observed by the bridge after its controller has initialized.</summary>
internal enum NetherRuntimePopupKind
{
    None,
    CodeOffer,
    Event,
    Recovery,
    Treasure,
    Shop,
    Continue,
    ReturnItems,
    /// <summary>AbyssCodeListPopupType.Change created by a native target_type=7 event.</summary>
    CodeTransform,
}

/// <summary>
/// A fully mapped native popup.  The bridge returns a failure rather than placing guessed data
/// here; policy code can therefore remain purely deterministic and fail closed.
/// </summary>
internal sealed record NetherRuntimePopupContext
{
    public NetherRuntimePopupKind Kind { get; init; }
    /// <summary>
    /// A popup may be consumed only by the native parent action which created it.  The bridge
    /// stamps this immutable ownership tuple at registration time; a later floor click or an
    /// out-of-order close can therefore never replay a stale Wait popup.
    /// </summary>
    public NetherActionKind OwnerAction { get; init; }
    public long OwnerGeneration { get; init; }
    public long Sequence { get; init; }
    /// <summary>
    /// True only when the bridge has correlated this otherwise ownerless popup with an exact
    /// floor-event sequence task from the current FloorSelection controller/runtime generation.
    /// This lets a native multi-stage event remain actionable when the server has already
    /// returned to Play, without relaxing fail-closed handling for arbitrary foreground popups.
    /// </summary>
    public bool HasRecoveredFloorEventTaskEvidence { get; init; }
    /// <summary>
    /// A CodeOffer can remain the same live native popup while its exact RerollAsync task
    /// rebuilds the server-provided candidates.  The bridge advances this only after that task
    /// and a fresh authoritative candidate read both succeed; it is never a visual-frame
    /// counter.  All other popup kinds remain at epoch zero.
    /// </summary>
    public long DecisionEpoch { get; init; }
    /// <summary>
    /// Playable character used to present the native Event popup. It is not submitted by
    /// NetherUpdateEventRequestEntity and therefore must not be used as the HP effect scope.
    /// Recovery and Treasure do not carry this presentation argument and retain zero.
    /// </summary>
    public long TargetCharacterId { get; init; }
    public int RawFloorType { get; init; }
    public IReadOnlyList<NetherEventOption> Options { get; init; } = Array.Empty<NetherEventOption>();
    public IReadOnlyList<NetherShopContent> ShopContents { get; init; } = Array.Empty<NetherShopContent>();
}

internal readonly record struct NetherRuntimePopupResult(NetherRuntimePopupContext? Popup, string Detail)
{
    public bool IsSuccess => Popup != null && Detail.Length == 0;

    public static NetherRuntimePopupResult Success(NetherRuntimePopupContext popup) => new(popup, string.Empty);

    public static NetherRuntimePopupResult Failure(string detail) => new(null, detail);
}

internal enum NetherPopupDispatchKind
{
    Code,
    NativeAction,
    AwaitNativeFlow,
    Pause,
}

internal sealed record NetherPopupDispatchDecision
{
    public NetherPopupDispatchKind Kind { get; init; }
    public NetherPlannedAction Action { get; init; }
    public bool HasEffectProjection { get; init; }
    public int ProjectedErosion { get; init; }
    public int HpDelta { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// Routes the actual currently-open popup to exactly one policy.  In particular a raw Nether
/// floor type of 4 is Event, not Battle; no generic Wait-to-code shortcut is allowed here.
/// </summary>
internal static class NetherPopupDispatchPolicy
{
    private static readonly NetherEventPolicy EventPolicy = new();

    public static NetherPopupDispatchDecision Decide(
        NetherSnapshot snapshot,
        NetherRuntimePopupContext popup,
        NetherAutoClimbSettings settings
    ) => Decide(
        snapshot,
        popup,
        settings,
        new NetherActiveCodeErosionProjection
        {
            ErosionProjectionKnown = true,
            CodeHash = "nether-codes:none",
            ErosionEffects = Array.Empty<NetherCodeEffect>(),
        }
    );

    public static NetherPopupDispatchDecision Decide(
        NetherSnapshot snapshot,
        NetherRuntimePopupContext popup,
        NetherAutoClimbSettings settings,
        NetherActiveCodeErosionProjection activeCodeErosion
    )
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        if (popup == null)
            throw new ArgumentNullException(nameof(popup));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        IReadOnlyList<NetherErosionModifier> modifiers = Array.Empty<NetherErosionModifier>();
        if (popup.Kind is NetherRuntimePopupKind.Event
            or NetherRuntimePopupKind.Recovery
            or NetherRuntimePopupKind.Treasure)
        {
            if (activeCodeErosion == null || !activeCodeErosion.ErosionProjectionKnown)
            {
                return Pause(
                    NetherPauseReason.UnknownEffect,
                    "active-code-erosion:" + (activeCodeErosion?.Detail ?? "missing")
                );
            }
            if (!NetherBattleRouteProjectionBuilder.TryMapModifiers(
                    activeCodeErosion.ErosionEffects,
                    out IReadOnlyList<NetherErosionModifier>? mapped,
                    out string modifierError
                ))
            {
                return Pause(NetherPauseReason.UnknownEffect, "active-code-erosion:" + modifierError);
            }
            modifiers = mapped!;
        }

        return popup.Kind switch
        {
            NetherRuntimePopupKind.CodeOffer => new NetherPopupDispatchDecision { Kind = NetherPopupDispatchKind.Code },
            NetherRuntimePopupKind.CodeTransform => FromCodeTransform(snapshot),
            NetherRuntimePopupKind.Event when popup.RawFloorType == (int)NetherFloorNodeType.Event =>
                FromEventDecision(EventPolicy.DecideEvent(snapshot, popup.Options, settings, modifiers), popup.TargetCharacterId),
            NetherRuntimePopupKind.Event => Pause(NetherPauseReason.UnknownFloor, "event-popup-raw-type-mismatch:" + popup.RawFloorType),
            NetherRuntimePopupKind.Recovery => FromEventDecision(EventPolicy.DecideRecovery(snapshot, popup.Options, settings, modifiers), 0),
            NetherRuntimePopupKind.Treasure => FromEventDecision(EventPolicy.DecideTreasure(snapshot, popup.Options, settings, modifiers), 0),
            NetherRuntimePopupKind.Shop => FromShopDecision(EventPolicy.DecideShop(snapshot, popup.ShopContents, settings)),
            NetherRuntimePopupKind.Continue or NetherRuntimePopupKind.ReturnItems =>
                new NetherPopupDispatchDecision { Kind = NetherPopupDispatchKind.AwaitNativeFlow },
            _ => Pause(NetherPauseReason.UnsupportedPopup, "unsupported-or-missing-native-popup:" + popup.Kind),
        };
    }

    private static NetherPopupDispatchDecision FromCodeTransform(NetherSnapshot snapshot)
    {
        NetherCodeTransformDecision decision = new NetherCodeTransformPolicy().Decide(
            snapshot.Codes,
            snapshot.CodeCapacity
        );
        return decision.CanTransform
            ? new NetherPopupDispatchDecision
            {
                Kind = NetherPopupDispatchKind.NativeAction,
                Action = new NetherPlannedAction(NetherActionKind.TransformCode)
                {
                    ReplaceCodeId = decision.RemoveCodeId,
                },
                Detail = "popup-code-transform:" + decision.RemoveCodeId,
            }
            : Pause(decision.PauseReason, decision.Detail);
    }

    private static NetherPopupDispatchDecision FromEventDecision(
        NetherEventDecision decision,
        long presentationCharacterId
    ) => decision.Kind switch
    {
        NetherEventDecisionKind.Select => new NetherPopupDispatchDecision
        {
            Kind = NetherPopupDispatchKind.NativeAction,
            Action = new NetherPlannedAction(NetherActionKind.SelectEventOption)
            {
                OptionNumber = decision.OptionNumber,
                CodeId = decision.ReplacementCodeId,
                TargetCharacterId = presentationCharacterId,
                ExpectedEffects = decision.ExpectedEffects,
                HasExpectedErosionDelta = true,
                ExpectedErosionDelta = decision.ExpectedErosionDelta,
            },
            HasEffectProjection = true,
            ProjectedErosion = decision.ProjectedErosion,
            HpDelta = decision.HpDelta,
            Detail = "popup-event:" + decision.OptionNumber,
        },
        _ => Pause(decision.PauseReason, decision.Detail),
    };

    private static NetherPopupDispatchDecision FromShopDecision(NetherShopDecision decision) => decision.Kind switch
    {
        NetherShopDecisionKind.Leave => new NetherPopupDispatchDecision
        {
            Kind = NetherPopupDispatchKind.NativeAction,
            Action = new NetherPlannedAction(NetherActionKind.LeaveShop),
            Detail = "popup-shop-leave",
        },
        NetherShopDecisionKind.Buy => new NetherPopupDispatchDecision
        {
            Kind = NetherPopupDispatchKind.NativeAction,
            Action = new NetherPlannedAction(NetherActionKind.BuyShopItem)
            {
                ContentId = decision.ContentId,
                ContentAmount = decision.Amount,
                GoldCost = decision.GoldCost,
            },
            Detail = "popup-shop-buy:" + decision.ContentId + ":" + decision.Amount + ":" + decision.GoldCost,
        },
        _ => Pause(decision.PauseReason, decision.Detail),
    };

    private static NetherPopupDispatchDecision Pause(NetherPauseReason reason, string detail) => new()
    {
        Kind = NetherPopupDispatchKind.Pause,
        PauseReason = reason,
        Detail = detail,
    };
}
