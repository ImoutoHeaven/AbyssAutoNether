#nullable enable

using System;

namespace AutoNether.Services;

internal enum NetherTreasureConfirmStage
{
    Idle,
    AwaitingSkipInvocation,
    AwaitingSkipTask,
    AwaitingResumePump,
    AwaitingConfirmTap,
    Completed,
}

internal readonly record struct NetherTreasureConfirmOwner(
    object? Popup,
    NetherActionKind OwnerAction,
    long OwnerGeneration,
    long RuntimeGeneration,
    long Sequence
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
        Stage = NetherTreasureConfirmStage.AwaitingSkipInvocation;
        return true;
    }

    public bool TryGetOwner(out NetherTreasureConfirmOwner owner)
    {
        owner = _owner;
        return Stage != NetherTreasureConfirmStage.Idle && _owner.IsValid;
    }

    public bool TryClaimSkip(NetherTreasureConfirmOwner owner) =>
        Transition(
            owner,
            NetherTreasureConfirmStage.AwaitingSkipInvocation,
            NetherTreasureConfirmStage.AwaitingSkipTask
        );

    public bool ObserveSkipTaskCompleted(NetherTreasureConfirmOwner owner) =>
        Transition(
            owner,
            NetherTreasureConfirmStage.AwaitingSkipTask,
            NetherTreasureConfirmStage.AwaitingResumePump
        );

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
