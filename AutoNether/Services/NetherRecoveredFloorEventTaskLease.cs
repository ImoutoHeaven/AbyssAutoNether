#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Correlates an exact floor-event sequence with its one popup even when native callbacks expose
/// those two pieces of evidence in either order.  A task can be claimed once only, and only by
/// the same live FloorSelection controller generation and exact popup instance/sequence.
/// </summary>
internal sealed class NetherRecoveredFloorEventTaskLease
{
    private object? _controller;
    private object? _task;
    private object? _popup;
    private long _generation;
    private long _popupSequenceBaseline;
    private long _popupSequence;

    public bool HasBoundPopup => _task != null && _popup != null;

    public bool ObserveSequence(
        object? controller,
        long generation,
        object? task,
        long popupSequenceBaseline
    )
    {
        if (controller == null || task == null || generation < 1 || popupSequenceBaseline < 0)
            return false;

        bool adoptsExistingPopup = _task == null
            && _popup != null
            && ReferenceEquals(_controller, controller)
            && _generation == generation
            && _popupSequence == popupSequenceBaseline;
        object? existingPopup = adoptsExistingPopup ? _popup : null;
        long existingPopupSequence = adoptsExistingPopup ? _popupSequence : 0;

        Reset();
        _controller = controller;
        _generation = generation;
        _task = task;
        _popupSequenceBaseline = popupSequenceBaseline;
        _popup = existingPopup;
        _popupSequence = existingPopupSequence;
        return true;
    }

    public bool ObservePopup(
        object? controller,
        long generation,
        object? popup,
        long sequence
    )
    {
        if (controller == null || popup == null || generation < 1 || sequence < 1)
            return false;

        if (_task == null)
        {
            Reset();
            _controller = controller;
            _generation = generation;
            _popup = popup;
            _popupSequenceBaseline = sequence;
            _popupSequence = sequence;
            return false;
        }

        if (!ReferenceEquals(_controller, controller)
            || _generation != generation
            || sequence <= _popupSequenceBaseline)
        {
            return false;
        }

        _popup = popup;
        _popupSequence = sequence;
        return true;
    }

    public bool CanClaim(
        object? controller,
        long generation,
        object? popup,
        long sequence
    ) => _task != null
        && controller != null
        && popup != null
        && ReferenceEquals(_controller, controller)
        && generation == _generation
        && ReferenceEquals(_popup, popup)
        && sequence == _popupSequence;

    public bool TryClaim(
        object? controller,
        long generation,
        object? popup,
        long sequence,
        out object? task
    )
    {
        task = null;
        if (!CanClaim(controller, generation, popup, sequence))
            return false;

        task = _task;
        Reset();
        return true;
    }

    public bool InvalidatePopup(object? popup)
    {
        if (popup == null || !ReferenceEquals(_popup, popup))
            return false;
        Reset();
        return true;
    }

    public void Reset()
    {
        _controller = null;
        _task = null;
        _popup = null;
        _generation = 0;
        _popupSequenceBaseline = 0;
        _popupSequence = 0;
    }
}
