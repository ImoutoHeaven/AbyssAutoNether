#nullable enable

namespace AutoNether.Services;

internal enum NetherCodeReceivedConfirmClaimKind
{
    None,
    Claimed,
    CorrelationMismatch,
    MissingClose,
}

internal readonly record struct NetherCodeReceivedConfirmClaim(
    NetherCodeReceivedConfirmClaimKind Kind,
    object? Close,
    long Sequence,
    string Detail
);

/// <summary>
/// Correlates the AbyssCodeReceivedPopup opened inside the exact native code-confirmation
/// UniTask with the code offer that started that task. Closing this child overlay only releases
/// the native continuation; the retained confirmation UniTask remains settlement authority.
/// </summary>
internal sealed class NetherCodeReceivedConfirmLease
{
    private NetherActionKind _ownerAction;
    private long _ownerGeneration;
    private long _codeId;
    private object? _popup;
    private object? _close;
    private long _sequence;
    private bool _claimed;

    public bool Begin(NetherActionKind ownerAction, long ownerGeneration, long codeId)
    {
        if (!IsCodeOwner(ownerAction) || ownerGeneration < 1 || codeId < 1)
            return false;

        _ownerAction = ownerAction;
        _ownerGeneration = ownerGeneration;
        _codeId = codeId;
        _popup = null;
        _close = null;
        _sequence = 0;
        _claimed = false;
        return true;
    }

    public bool TryGetOwner(out NetherActionKind ownerAction, out long ownerGeneration)
    {
        ownerAction = _ownerAction;
        ownerGeneration = _ownerGeneration;
        return IsCodeOwner(_ownerAction) && _ownerGeneration > 0 && _codeId > 0;
    }

    public bool RegisterPopup(
        object? popup,
        object? close,
        long sequence,
        long codeId
    )
    {
        if (popup == null || sequence < 1 || codeId != _codeId
            || !IsCodeOwner(_ownerAction) || _ownerGeneration < 1)
        {
            return false;
        }

        _popup = popup;
        _close = close;
        _sequence = sequence;
        _claimed = false;
        return true;
    }

    public NetherCodeReceivedConfirmClaim Claim(
        NetherActionKind ownerAction,
        long ownerGeneration,
        long codeId
    )
    {
        if (_popup == null || _claimed)
            return new(NetherCodeReceivedConfirmClaimKind.None, null, 0, "no-code-received-confirm");
        if (ownerAction != _ownerAction || ownerGeneration != _ownerGeneration || codeId != _codeId)
        {
            return new(
                NetherCodeReceivedConfirmClaimKind.CorrelationMismatch,
                null,
                _sequence,
                "code-received-popup-correlation-mismatch"
            );
        }

        _claimed = true;
        if (_close == null)
        {
            return new(
                NetherCodeReceivedConfirmClaimKind.MissingClose,
                null,
                _sequence,
                "code-received-popup-missing-close"
            );
        }

        return new(
            NetherCodeReceivedConfirmClaimKind.Claimed,
            _close,
            _sequence,
            "code-received-confirm-claimed"
        );
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
        _ownerAction = NetherActionKind.None;
        _ownerGeneration = 0;
        _codeId = 0;
        _popup = null;
        _close = null;
        _sequence = 0;
        _claimed = false;
    }

    private static bool IsCodeOwner(NetherActionKind ownerAction) => ownerAction is
        NetherActionKind.SelectFloor
        or NetherActionKind.BattleSettlement
        or NetherActionKind.RecoveredCodeOffer;
}
