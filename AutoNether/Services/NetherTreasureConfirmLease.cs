#nullable enable

using System;

namespace AutoNether.Services;

internal enum NetherTreasureConfirmStage
{
    Idle,
    AwaitingOpenAnimation,
    AwaitingNativeButtonSubscription,
    AwaitingResumePump,
    AwaitingConfirmTap,
    Completed,
}

internal readonly record struct NetherTreasureConfirmOwner(
    object? Popup,
    NetherActionKind OwnerAction,
    long OwnerGeneration,
    long RuntimeGeneration,
    long Sequence,
    int NativeButtonRuntimeListenerBaseline = -1
)
{
    public bool IsValid => Popup != null
        && RuntimeGeneration > 0
        && Sequence > 0
        && (OwnerAction == NetherActionKind.SelectFloor && OwnerGeneration > 0
            || OwnerAction == NetherActionKind.None && OwnerGeneration == 0);
}

/// <summary>
/// Retains the exact Treasure popup while reproducing its packaged two-step confirmation.
/// The native controller first awaits the open/skip animation and only then subscribes to the
/// popup's SkipAndConfirmButton tap.  A separate resume pump prevents the final tap from being
/// emitted before that native subscription exists.
/// </summary>
internal sealed class NetherTreasureConfirmLease
{
    private NetherTreasureConfirmOwner _owner;

    public NetherTreasureConfirmStage Stage { get; private set; }

    public bool Begin(NetherTreasureConfirmOwner owner)
    {
        if (Stage != NetherTreasureConfirmStage.Idle || !owner.IsValid)
            return false;

        _owner = owner;
        Stage = NetherTreasureConfirmStage.AwaitingOpenAnimation;
        return true;
    }

    public bool TryGetOwner(out NetherTreasureConfirmOwner owner)
    {
        owner = _owner;
        return Stage != NetherTreasureConfirmStage.Idle && _owner.IsValid;
    }

    /// <summary>
    /// Records that the popup reached Open. This authorizes waiting for the native
    /// SkipAndConfirmButton subscription; it does not authorize a direct reflection call to
    /// SkipOpenTreasureAnimationAsync.
    /// </summary>
    public bool ObserveOpenAnimationReady(
        NetherTreasureConfirmOwner owner,
        bool openAnimationObserved
    ) => openAnimationObserved
        && Transition(
            owner,
            NetherTreasureConfirmStage.AwaitingOpenAnimation,
            NetherTreasureConfirmStage.AwaitingNativeButtonSubscription
        );

    /// <summary>
    /// Records the exact post-Open UnityEvent listener increase created by the native controller's
    /// SkipAndConfirmButton OnTap subscription.
    /// </summary>
    public bool ObserveNativeButtonSubscription(NetherTreasureConfirmOwner owner) =>
        Transition(
            owner,
            NetherTreasureConfirmStage.AwaitingNativeButtonSubscription,
            NetherTreasureConfirmStage.AwaitingResumePump
        );

    /// <summary>
    /// Preserves a second complete pump before emitting the single native button tap.
    /// </summary>
    public bool AdvanceResumePump(NetherTreasureConfirmOwner owner) =>
        Transition(
            owner,
            NetherTreasureConfirmStage.AwaitingResumePump,
            NetherTreasureConfirmStage.AwaitingConfirmTap
        );

    public bool TryClaimConfirm(NetherTreasureConfirmOwner owner) =>
        Transition(
            owner,
            NetherTreasureConfirmStage.AwaitingConfirmTap,
            NetherTreasureConfirmStage.Completed
        );

    public bool InvalidatePopup(object? popup)
    {
        if (popup == null || !ReferenceEquals(_owner.Popup, popup))
            return false;
        Reset();
        return true;
    }

    public void Reset()
    {
        _owner = default;
        Stage = NetherTreasureConfirmStage.Idle;
    }

    private bool Transition(
        NetherTreasureConfirmOwner owner,
        NetherTreasureConfirmStage expected,
        NetherTreasureConfirmStage next
    )
    {
        if (Stage != expected || !Matches(owner))
            return false;
        Stage = next;
        return true;
    }

    private bool Matches(NetherTreasureConfirmOwner owner) =>
        owner.IsValid
        && ReferenceEquals(_owner.Popup, owner.Popup)
        && _owner.OwnerAction == owner.OwnerAction
        && _owner.OwnerGeneration == owner.OwnerGeneration
        && _owner.RuntimeGeneration == owner.RuntimeGeneration
        && _owner.Sequence == owner.Sequence;
}
