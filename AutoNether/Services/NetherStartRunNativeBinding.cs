#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Exact arguments consumed by the current controller-owned Nether start mutation.  The native
/// adapter owns reflection/version details; this module owns the run-start contract and constants.
/// </summary>
internal readonly record struct NetherStartRunNativeRequest(
    int PartyNumber,
    int StartFloorLevel,
    int TicketCount
);

/// <summary>
/// Positional arguments of the current party-owned native mutation.  Keep this distinct from
/// <see cref="NetherStartRunNativeRequest"/>: semantic request field order must never be assumed
/// to match the generated IL2CPP wrapper's positional contract.
/// </summary>
internal readonly record struct NetherStartRunNativeInvocation(
    int UseTicket,
    int StartFloorLevel,
    int PartyNumber
);

internal static class NetherStartRunNativeBinding
{
    public static NetherStartRunNativeInvocation ToNativeInvocation(
        NetherStartRunNativeRequest request
    ) => new(
        UseTicket: request.TicketCount,
        StartFloorLevel: request.StartFloorLevel,
        PartyNumber: request.PartyNumber
    );

    public static NetherNativeActionResult Invoke(
        NetherPlannedAction action,
        Func<NetherStartRunNativeRequest, NetherNativeActionResult> nativeInvoker
    )
    {
        if (nativeInvoker == null)
            throw new ArgumentNullException(nameof(nativeInvoker));
        if (action.Kind != NetherActionKind.StartRun
            || action.ExpectedBeforeStatus != NetherSessionStatus.NotPlayed
            || action.ExpectedAfterStatus != NetherSessionStatus.Play
            || action.FloorLevel < 0)
        {
            return NetherNativeActionResult.Rejected("invalid-native-run-start-contract");
        }

        return nativeInvoker(new NetherStartRunNativeRequest(
            PartyNumber: 1,
            StartFloorLevel: action.FloorLevel,
            TicketCount: 1
        ));
    }
}
