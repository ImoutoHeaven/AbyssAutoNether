#nullable enable

using System;

namespace AutoNether.Services;

internal enum NetherGeneratedSequencePhase
{
    None,
    Started,
    Completed,
    Faulted,
}

/// <summary>
/// One observation of a generated sequence.  <see cref="Token"/> is the arm sequence returned
/// when a flow started the native mutation, so an observation that belongs to an earlier
/// invocation (for example a player-initiated confirm) can never satisfy a newer flow.
/// </summary>
internal readonly record struct NetherGeneratedSequenceObservation(
    NetherGeneratedSequenceFamily Family,
    long Token,
    object? Controller,
    long ControllerIdentity,
    long CodeId,
    NetherGeneratedSequencePhase Phase,
    string Detail
)
{
    public bool IsTerminal => Phase is NetherGeneratedSequencePhase.Completed
        or NetherGeneratedSequencePhase.Faulted;
}

/// <summary>
/// Shared start/completion contract for the transform, confirm, and cancel sequences.  Entry is
/// observed when the generated <c>MoveNext</c> is entered with <c>__1__state == -1</c>;
/// completion when a later invocation returns with <c>__1__state == -2</c> and a terminal
/// builder status; failure from the MoveNext finalizer or a faulted/canceled builder status.
/// The terminal phase wins and is never downgraded.
/// </summary>
internal sealed class NetherGeneratedSequenceObservationRegistry
{
    private readonly object _gate = new();
    private readonly NetherGeneratedSequenceObservation[] _slots = new NetherGeneratedSequenceObservation[3];
    private long _token;

    public long Arm(
        NetherGeneratedSequenceFamily family,
        object? controller,
        long controllerIdentity,
        long codeId
    )
    {
        lock (_gate)
        {
            _token++;
            int index = IndexOf(family);
            _slots[index] = new NetherGeneratedSequenceObservation(
                family,
                _token,
                controller,
                controllerIdentity,
                codeId,
                NetherGeneratedSequencePhase.None,
                "armed"
            );
            return _token;
        }
    }

    public void Clear(NetherGeneratedSequenceFamily family)
    {
        lock (_gate)
            _slots[IndexOf(family)] = default;
    }

    public NetherGeneratedSequenceObservation Current(NetherGeneratedSequenceFamily family)
    {
        lock (_gate)
            return _slots[IndexOf(family)];
    }

    /// <summary>
    /// Accepts the first MoveNext entry of the armed invocation.  A resume invocation, an
    /// unarmed family, a different controller, or a different code id is ignored.
    /// </summary>
    public bool ObserveEntry(
        NetherGeneratedSequenceFamily family,
        object? controller,
        long controllerIdentity,
        long codeId,
        out NetherGeneratedSequenceObservation observation
    )
    {
        lock (_gate)
        {
            int index = IndexOf(family);
            NetherGeneratedSequenceObservation slot = _slots[index];
            observation = slot;
            if (slot.Token <= 0
                || slot.Phase != NetherGeneratedSequencePhase.None
                || !MatchesController(slot, controller, controllerIdentity)
                || !MatchesCodeId(slot, codeId))
            {
                return false;
            }

            observation = slot with
            {
                Controller = controller ?? slot.Controller,
                ControllerIdentity = controllerIdentity > 0 ? controllerIdentity : slot.ControllerIdentity,
                CodeId = codeId > 0 ? codeId : slot.CodeId,
                Phase = NetherGeneratedSequencePhase.Started,
                Detail = "state-machine-entered",
            };
            _slots[index] = observation;
            return true;
        }
    }

    /// <summary>
    /// Records the terminal phase of the armed invocation.  Completion is final; a later
    /// observation can never turn it into a fault.
    /// </summary>
    public bool ObserveTerminal(
        NetherGeneratedSequenceFamily family,
        object? controller,
        long controllerIdentity,
        long codeId,
        NetherGeneratedSequencePhase phase,
        string detail,
        out NetherGeneratedSequenceObservation observation
    )
    {
        if (phase is not (NetherGeneratedSequencePhase.Completed or NetherGeneratedSequencePhase.Faulted))
            throw new ArgumentOutOfRangeException(nameof(phase));

        lock (_gate)
        {
            int index = IndexOf(family);
            NetherGeneratedSequenceObservation slot = _slots[index];
            observation = slot;
            if (slot.Token <= 0
                || slot.Phase != NetherGeneratedSequencePhase.Started
                || !MatchesController(slot, controller, controllerIdentity)
                || !MatchesCodeId(slot, codeId))
            {
                return false;
            }

            observation = slot with
            {
                Phase = phase,
                Detail = detail,
            };
            _slots[index] = observation;
            return true;
        }
    }

    private static int IndexOf(NetherGeneratedSequenceFamily family) => family switch
    {
        NetherGeneratedSequenceFamily.Transform => 0,
        NetherGeneratedSequenceFamily.Confirm => 1,
        NetherGeneratedSequenceFamily.Cancel => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(family)),
    };

    private static bool MatchesController(
        NetherGeneratedSequenceObservation slot,
        object? controller,
        long controllerIdentity
    )
    {
        if (controllerIdentity > 0 && slot.ControllerIdentity > 0)
            return controllerIdentity == slot.ControllerIdentity;
        if (slot.Controller == null)
            return true;
        return controller != null && ReferenceEquals(controller, slot.Controller);
    }

    private static bool MatchesCodeId(NetherGeneratedSequenceObservation slot, long codeId) =>
        slot.CodeId <= 0 || codeId <= 0 || slot.CodeId == codeId;
}
