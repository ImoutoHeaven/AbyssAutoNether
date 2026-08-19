#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

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
    /// Runtime-controller generation which registered this popup.  OwnerGeneration identifies
    /// the logical native action; RuntimeGeneration prevents a registration from surviving a
    /// controller/scene replacement with the same logical owner.
    /// </summary>
    public long RuntimeGeneration { get; init; }
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
    /// <summary>Exact rendered floor/node identity correlated with the Event popup.</summary>
    public long FloorId { get; init; }
    public long NodeId { get; init; }
    public int RawFloorType { get; init; }
    public IReadOnlyList<NetherEventOption> Options { get; init; } = Array.Empty<NetherEventOption>();
    public IReadOnlyList<NetherShopContent> ShopContents { get; init; } = Array.Empty<NetherShopContent>();
    /// <summary>Optional exact sequential key-then-bag commitment for the current Shop popup.</summary>
    public NetherShopProcurementCommitment? ShopProcurementCommitment { get; init; }
    /// <summary>Exact selected-branch rank-five procurement decision carried into popup policy.</summary>
    public NetherRankFiveKeyProcurementDecision? RankFiveKeyProcurement { get; init; }
    /// <summary>Production captures must not score a Recovery popup without complete branch proof.</summary>
    public bool RequireCompleteRecoveryBranchSafety { get; init; }
    /// <summary>
    /// Optional route commitment captured before the native popup was opened. A live popup must
    /// match this identity before any option payment is dispatched.
    /// </summary>
    public NetherEventCommitment? ExpectedEventCommitment { get; init; }
    /// <summary>
    /// Route/pre-entry commitments keyed by the complete Event/part/floor/node/option identity.
    /// A popup can expose several exact options, while only the option chosen before entry has a
    /// committed projected state.
    /// </summary>
    public IReadOnlyDictionary<NetherEventCommitmentKey, NetherEventCommitment> ExpectedEventCommitments { get; init; } =
        new Dictionary<NetherEventCommitmentKey, NetherEventCommitment>();
    /// <summary>Exact mode/wallet facts used for Event reward ordering when available.</summary>
    public NetherEventStrategyEvidence? EventStrategyEvidence { get; init; }
    /// <summary>
    /// Exact removal committed by the preceding accepted Recovery option. The native Change popup
    /// itself exposes only the owned Code list and cannot reconstruct Rest/Purification value.
    /// </summary>
    public NetherCodeTransformCommitment? CodeTransformCommitment { get; init; }
}

internal enum NetherRuntimePopupResultKind
{
    Invalid,
    Success,
    Pending,
    NativeContinuation,
    Failure,
}

internal readonly record struct NetherRuntimePopupResult
{
    private NetherRuntimePopupResult(
        NetherRuntimePopupResultKind kind,
        NetherRuntimePopupContext? popup,
        string detail
    )
    {
        Kind = kind;
        Popup = popup;
        Detail = detail ?? string.Empty;
    }

    public NetherRuntimePopupResultKind Kind { get; }
    public NetherRuntimePopupContext? Popup { get; }
    public string Detail { get; }

    public bool IsSuccess => Kind == NetherRuntimePopupResultKind.Success && Popup != null;

    public bool IsPending => Kind == NetherRuntimePopupResultKind.Pending && Popup != null;

    /// <summary>
    /// A live owned popup that belongs to an already-dispatched native async continuation.  It
    /// must not be dispatched as a new policy decision and must not block polling that parent.
    /// </summary>
    public bool IsNativeContinuation =>
        Kind == NetherRuntimePopupResultKind.NativeContinuation && Popup != null;

    public bool IsDefinitelyAbsent => Kind == NetherRuntimePopupResultKind.Failure
        && Popup == null
        && Detail == "missing-active-native-popup";

    public static NetherRuntimePopupResult Success(NetherRuntimePopupContext popup) =>
        new(NetherRuntimePopupResultKind.Success, popup, string.Empty);

    public static NetherRuntimePopupResult Failure(string detail) =>
        new(NetherRuntimePopupResultKind.Failure, null, detail ?? string.Empty);

    /// <summary>
    /// An exact native owner exists and is either waiting for its popup controller to register
    /// (sequence zero) or for that registered controller to finish asynchronous initialization
    /// (positive sequence). Callers preserve the owner and wait within a bounded identity gate;
    /// this is neither a generic "popup absent" result nor a permanent binding failure.
    /// </summary>
    public static NetherRuntimePopupResult Pending(NetherRuntimePopupContext popup, string detail) =>
        new(NetherRuntimePopupResultKind.Pending, popup, detail ?? string.Empty);

    public static NetherRuntimePopupResult NativeContinuation(
        NetherRuntimePopupContext popup,
        string detail
    ) => new(NetherRuntimePopupResultKind.NativeContinuation, popup, detail ?? string.Empty);
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
    public bool AllowsPartialActiveDeaths { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
    public IReadOnlyList<NetherEventOptionAudit> EventOptionAudits { get; init; } =
        Array.Empty<NetherEventOptionAudit>();
    public IReadOnlyList<NetherShopOptionAudit> ShopOptionAudits { get; init; } =
        Array.Empty<NetherShopOptionAudit>();
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
        },
        NetherCodeTransformHardExclusionEvidence.Unknown(
            "code-transform-hard-exclusions-not-captured"
        )
    );

    public static NetherPopupDispatchDecision Decide(
        NetherSnapshot snapshot,
        NetherRuntimePopupContext popup,
        NetherAutoClimbSettings settings,
        NetherActiveCodeErosionProjection activeCodeErosion
    ) => Decide(
        snapshot,
        popup,
        settings,
        activeCodeErosion,
        NetherCodeTransformHardExclusionEvidence.Unknown(
            "code-transform-hard-exclusions-not-captured"
        )
    );

    public static NetherPopupDispatchDecision Decide(
        NetherSnapshot snapshot,
        NetherRuntimePopupContext popup,
        NetherAutoClimbSettings settings,
        NetherActiveCodeErosionProjection activeCodeErosion,
        NetherCodeTransformHardExclusionEvidence transformHardExclusions
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

        NetherEventStrategyEvidence? eventStrategyEvidence = popup.EventStrategyEvidence;
        if (eventStrategyEvidence == null && settings.StrategyMode == NetherStrategyMode.Equipment)
        {
            // Equipment is an explicit setting; it does not require a speculative Research
            // settlement projection to activate its exact reward ordering.
            eventStrategyEvidence = new NetherEventStrategyEvidence
            {
                IsKnown = true,
                Mode = NetherStrategyMode.Equipment,
            };
        }
        if (popup.Kind == NetherRuntimePopupKind.Event
            && settings.StrategyMode == NetherStrategyMode.Research
            && (eventStrategyEvidence == null
                || !eventStrategyEvidence.IsUsableFor(NetherStrategyMode.Research))
            && !popup.Options.Any(option =>
                option.StrategyEvidence?.IsUsableFor(NetherStrategyMode.Research) == true))
        {
            return Pause(
                NetherPauseReason.BindingUnavailable,
                "event-strategy-evidence-unavailable:"
                    + (eventStrategyEvidence?.UnknownReason ?? "missing")
            );
        }

        return popup.Kind switch
        {
            NetherRuntimePopupKind.CodeOffer => new NetherPopupDispatchDecision { Kind = NetherPopupDispatchKind.Code },
            NetherRuntimePopupKind.CodeTransform => FromCodeTransform(snapshot, popup),
            NetherRuntimePopupKind.Event when popup.RawFloorType == (int)NetherFloorNodeType.Event =>
                FromEventDecision(
                    EventPolicy.DecideEvent(
                        snapshot,
                        popup.Options,
                        settings,
                        modifiers,
                        eventStrategyEvidence
                    ),
                    popup.TargetCharacterId,
                    popup.ExpectedEventCommitment,
                    popup.ExpectedEventCommitments,
                    requireExpectedCommitment: popup.Options.Any(option => option.RequiresExactBinding)
                ),
            NetherRuntimePopupKind.Event => Pause(NetherPauseReason.UnknownFloor, "event-popup-raw-type-mismatch:" + popup.RawFloorType),
            NetherRuntimePopupKind.Recovery => FromEventDecision(
                EventPolicy.DecideRecovery(
                    snapshot,
                    popup.Options,
                    settings,
                    modifiers,
                    transformHardExclusions,
                    popup.RequireCompleteRecoveryBranchSafety
                ),
                0,
                popup.ExpectedEventCommitment,
                popup.ExpectedEventCommitments,
                requireExpectedCommitment: popup.Options.Any(option => option.RequiresExactBinding)
            ),
            NetherRuntimePopupKind.Treasure => FromEventDecision(
                EventPolicy.DecideTreasure(snapshot, popup.Options, settings, modifiers),
                0,
                popup.ExpectedEventCommitment,
                popup.ExpectedEventCommitments,
                requireExpectedCommitment: popup.Options.Any(option => option.RequiresExactBinding)
            ),
            NetherRuntimePopupKind.Shop => FromShopDecision(EventPolicy.DecideShop(
                snapshot,
                popup.ShopContents,
                settings,
                popup.ShopProcurementCommitment
            )),
            NetherRuntimePopupKind.Continue or NetherRuntimePopupKind.ReturnItems =>
                new NetherPopupDispatchDecision { Kind = NetherPopupDispatchKind.AwaitNativeFlow },
            _ => Pause(NetherPauseReason.UnsupportedPopup, "unsupported-or-missing-native-popup:" + popup.Kind),
        };
    }

    private static NetherPopupDispatchDecision FromCodeTransform(
        NetherSnapshot snapshot,
        NetherRuntimePopupContext popup
    )
    {
        NetherCodeTransformCommitment? commitment = popup.CodeTransformCommitment;
        if (commitment is not NetherCodeTransformCommitment exact
            || !exact.IsValid
            || snapshot.Codes.Count(code => code != null
                && code.IsKnown
                && code.CodeId == exact.RemoveCodeId) != 1)
        {
            return Pause(
                NetherPauseReason.UnknownMasterData,
                "code-transform-recovery-commitment-unavailable"
            );
        }
        return new NetherPopupDispatchDecision
            {
                Kind = NetherPopupDispatchKind.NativeAction,
                Action = new NetherPlannedAction(NetherActionKind.TransformCode)
                {
                    ReplaceCodeId = exact.RemoveCodeId,
                },
                Detail = "popup-code-transform:" + exact.RemoveCodeId,
            };
    }

    private static NetherPopupDispatchDecision FromEventDecision(
        NetherEventDecision decision,
        long presentationCharacterId,
        NetherEventCommitment? expectedCommitment,
        IReadOnlyDictionary<NetherEventCommitmentKey, NetherEventCommitment>? expectedCommitments,
        bool requireExpectedCommitment
    )
    {
        NetherEventCommitment? selectedExpectedCommitment = null;
        if (expectedCommitments is { Count: > 0 })
        {
            expectedCommitments.TryGetValue(
                new NetherEventCommitmentKey(
                    decision.EventId,
                    decision.EventPartId,
                    decision.FloorId,
                    decision.NodeId,
                    decision.OptionNumber
                ),
                out selectedExpectedCommitment
            );
        }
        else
        {
            selectedExpectedCommitment = expectedCommitment;
        }
        if (decision.Kind == NetherEventDecisionKind.Select
            && (requireExpectedCommitment && selectedExpectedCommitment == null
                || selectedExpectedCommitment is NetherEventCommitment expected
                && (decision.Commitment == null
                    || decision.Commitment is not NetherEventCommitment actual
                    || !actual.IsValid
                    || !expected.IsValid
                    || !expected.Matches(actual)
                    || !expected.Matches(new NetherEventOption(
                    decision.OptionNumber,
                    decision.ExpectedEffects
                )
                    {
                        EventId = decision.EventId,
                        EventPartId = decision.EventPartId,
                        FloorId = decision.FloorId,
                        NodeId = decision.NodeId,
                        BattleEvidence = decision.Battle,
                        RewardEvidence = decision.Reward,
                        PartialDeathEligibility = decision.PartialDeathEligibility,
                        AllowsPartialActiveDeaths = decision.AllowsPartialActiveDeaths,
                        CommittedGoldMinimum = decision.CommittedGoldMinimum,
                        CommittedKeyMinimum = decision.CommittedKeyMinimum,
                        RankFiveKeyProcurementCommitment = decision.RankFiveKeyProcurementCommitment,
                        RankFiveTreasureObjective = decision.RankFiveTreasureObjective,
                }, decision.ProjectedErosion, decision.HpDelta, decision.Battle, decision.Reward,
                    decision.ProjectedNetherGold, decision.ProjectedTreasureKeys))))
        {
            return Pause(
                NetherPauseReason.StaleEventCommitment,
                "event-commitment-mismatch-before-payment"
            );
        }

        return decision.Kind switch
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
                ProjectedErosion = decision.ProjectedErosion,
                ProjectedHpDelta = decision.HpDelta,
                ProjectedNetherGold = decision.ProjectedNetherGold,
                ProjectedTreasureKeys = decision.ProjectedTreasureKeys,
                CommittedGoldMinimum = decision.CommittedGoldMinimum,
                CommittedKeyMinimum = decision.CommittedKeyMinimum,
                EventId = decision.EventId,
                EventPartId = decision.EventPartId,
                EventFloorId = decision.FloorId,
                EventNodeId = decision.NodeId,
                EventCommitment = decision.Commitment,
            },
            HasEffectProjection = true,
            ProjectedErosion = decision.ProjectedErosion,
            HpDelta = decision.HpDelta,
            AllowsPartialActiveDeaths = decision.AllowsPartialActiveDeaths,
            Detail = "popup-event:" + decision.OptionNumber,
            EventOptionAudits = decision.OptionAudits,
        },
        _ => Pause(decision.PauseReason, decision.Detail, decision.OptionAudits),
    };
    }

    private static NetherPopupDispatchDecision FromShopDecision(NetherShopDecision decision) => decision.Kind switch
    {
        NetherShopDecisionKind.Leave => new NetherPopupDispatchDecision
        {
            Kind = NetherPopupDispatchKind.NativeAction,
            Action = new NetherPlannedAction(NetherActionKind.LeaveShop),
            Detail = "popup-shop-leave",
            ShopOptionAudits = decision.OptionAudits,
        },
        NetherShopDecisionKind.Buy => new NetherPopupDispatchDecision
        {
            Kind = NetherPopupDispatchKind.NativeAction,
            Action = new NetherPlannedAction(NetherActionKind.BuyShopItem)
            {
                ContentId = decision.ContentId,
                ContentAmount = decision.Amount,
                GoldCost = decision.GoldCost,
                ShopProcurementCommitment = decision.ProcurementCommitment,
            },
            Detail = "popup-shop-buy:" + decision.ContentId + ":" + decision.Amount + ":" + decision.GoldCost,
            ShopOptionAudits = decision.OptionAudits,
        },
        _ => Pause(decision.PauseReason, decision.Detail, shopOptionAudits: decision.OptionAudits),
    };

    private static NetherPopupDispatchDecision Pause(
        NetherPauseReason reason,
        string detail,
        IReadOnlyList<NetherEventOptionAudit>? eventOptionAudits = null,
        IReadOnlyList<NetherShopOptionAudit>? shopOptionAudits = null
    ) => new()
    {
        Kind = NetherPopupDispatchKind.Pause,
        PauseReason = reason,
        Detail = detail,
        EventOptionAudits = eventOptionAudits ?? Array.Empty<NetherEventOptionAudit>(),
        ShopOptionAudits = shopOptionAudits ?? Array.Empty<NetherShopOptionAudit>(),
    };
}
