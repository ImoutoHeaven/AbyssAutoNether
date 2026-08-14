#nullable enable

namespace AutoNether.Services;

internal enum NetherCodeSelectionNativeStage
{
    Idle,
    AwaitingConfirmationTask,
    AwaitingReplacementPopup,
    AwaitingReplacementConfirmation,
    AwaitingCompletion,
    Completed,
}

/// <summary>
/// Models the native code-offer continuation.  The generated confirmation task is authoritative:
/// it encompasses the optional replacement popup and the subsequent server fix-code request.
/// </summary>
internal sealed class NetherCodeSelectionNativeFlow
{
    private long _selectedCodeId;
    private long _replaceCodeId;
    private long _popupSequenceBaseline;
    private long _replacementPopupSequence;
    private long _replacementConfirmationSequence;
    private bool _replacementCompleteDismissed;

    public NetherCodeSelectionNativeStage Stage { get; private set; } = NetherCodeSelectionNativeStage.Idle;

    public long SelectedCodeId => _selectedCodeId;

    public long ReplacementCodeId => _replaceCodeId;

    public long ReplacementConfirmationSequence => _replacementConfirmationSequence;

    public bool ReplacementCompleteDismissed => _replacementCompleteDismissed;

    public bool Begin(long codeId, long replaceCodeId, long popupSequenceBaseline)
    {
        if (codeId <= 0 || replaceCodeId < 0 || codeId == replaceCodeId
            || Stage is not (NetherCodeSelectionNativeStage.Idle or NetherCodeSelectionNativeStage.Completed))
        {
            return false;
        }

        _selectedCodeId = codeId;
        _replaceCodeId = replaceCodeId;
        _popupSequenceBaseline = popupSequenceBaseline;
        _replacementPopupSequence = 0;
        _replacementConfirmationSequence = 0;
        _replacementCompleteDismissed = false;
        Stage = NetherCodeSelectionNativeStage.AwaitingConfirmationTask;
        return true;
    }

    public bool ObserveConfirmationTask()
    {
        if (Stage != NetherCodeSelectionNativeStage.AwaitingConfirmationTask)
            return false;
        Stage = _replaceCodeId > 0
            ? NetherCodeSelectionNativeStage.AwaitingReplacementPopup
            : NetherCodeSelectionNativeStage.AwaitingCompletion;
        return true;
    }

    public bool CanSubmitReplacement(long popupSequence) =>
        Stage == NetherCodeSelectionNativeStage.AwaitingReplacementPopup
        && popupSequence > _popupSequenceBaseline;

    public bool SubmitReplacement(long popupSequence)
    {
        if (!CanSubmitReplacement(popupSequence))
            return false;
        _replacementPopupSequence = popupSequence;
        Stage = NetherCodeSelectionNativeStage.AwaitingReplacementConfirmation;
        return true;
    }

    public bool CanConfirmReplacement(long popupSequence) =>
        Stage == NetherCodeSelectionNativeStage.AwaitingReplacementConfirmation
        && popupSequence > _replacementPopupSequence;

    public bool ConfirmReplacement(long popupSequence)
    {
        if (!CanConfirmReplacement(popupSequence))
            return false;
        _replacementConfirmationSequence = popupSequence;
        Stage = NetherCodeSelectionNativeStage.AwaitingCompletion;
        return true;
    }

    public bool CanDismissReplacementComplete(long popupSequence) =>
        Stage == NetherCodeSelectionNativeStage.AwaitingCompletion
        && _replaceCodeId > 0
        && _replacementConfirmationSequence > 0
        && !_replacementCompleteDismissed
        && popupSequence > _replacementConfirmationSequence;

    public bool DismissReplacementComplete(long popupSequence)
    {
        if (!CanDismissReplacementComplete(popupSequence))
            return false;
        _replacementCompleteDismissed = true;
        return true;
    }

    public bool CompleteConfirmationTask()
    {
        if (Stage != NetherCodeSelectionNativeStage.AwaitingCompletion
            || (_replaceCodeId > 0 && !_replacementCompleteDismissed))
            return false;
        _selectedCodeId = 0;
        _replaceCodeId = 0;
        _popupSequenceBaseline = 0;
        _replacementPopupSequence = 0;
        _replacementConfirmationSequence = 0;
        _replacementCompleteDismissed = false;
        Stage = NetherCodeSelectionNativeStage.Completed;
        return true;
    }

    public void Clear()
    {
        _selectedCodeId = 0;
        _replaceCodeId = 0;
        _popupSequenceBaseline = 0;
        _replacementPopupSequence = 0;
        _replacementConfirmationSequence = 0;
        _replacementCompleteDismissed = false;
        Stage = NetherCodeSelectionNativeStage.Idle;
    }
}
