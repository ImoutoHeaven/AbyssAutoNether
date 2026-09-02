#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherStartRunNativeBindingTests
{
    [Fact]
    public void Native_invocation_preserves_current_party_ticket_start_floor_position_order()
    {
        // Fresh Project.dll 075179ed...07ea:
        // NetherUtility.TransitionNetherFloorSelectionSceneFromPartyAsync
        // is (partyNo, useTicket, startFloorLevel, ct). Deliberately distinct values keep
        // semantic request fields from masking a positional swap.
        NetherStartRunNativeInvocation invocation = NetherStartRunNativeBinding.ToNativeInvocation(
            new NetherStartRunNativeRequest(
                PartyNumber: 7,
                StartFloorLevel: 70,
                TicketCount: 3
            )
        );

        Assert.Equal(new NetherStartRunNativeInvocation(7, 3, 70), invocation);
    }

    [Fact]
    public void Binding_consumes_the_policy_selected_checkpoint_as_the_exact_native_start_floor()
    {
        NetherStartRunNativeRequest? observed = null;
        NetherNativeActionResult result = NetherStartRunNativeBinding.Invoke(
            new NetherPlannedAction(NetherActionKind.StartRun)
            {
                ExpectedBeforeStatus = NetherSessionStatus.NotPlayed,
                ExpectedAfterStatus = NetherSessionStatus.Play,
                FloorLevel = 70,
            },
            request =>
            {
                observed = request;
                return NetherNativeActionResult.Started("fake-native-start");
            }
        );

        Assert.Equal(NetherNativeActionResultKind.Started, result.Kind);
        Assert.Equal(new NetherStartRunNativeRequest(1, 70, 1), observed);
    }

    [Fact]
    public void Binding_rejects_a_non_start_contract_without_invoking_native_code()
    {
        bool invoked = false;
        NetherNativeActionResult result = NetherStartRunNativeBinding.Invoke(
            new NetherPlannedAction(NetherActionKind.StartRun)
            {
                ExpectedBeforeStatus = NetherSessionStatus.Play,
                ExpectedAfterStatus = NetherSessionStatus.Play,
                FloorLevel = 70,
            },
            _ =>
            {
                invoked = true;
                return NetherNativeActionResult.Started("must-not-run");
            }
        );

        Assert.False(invoked);
        Assert.Equal(NetherNativeActionResultKind.Rejected, result.Kind);
        Assert.Equal("invalid-native-run-start-contract", result.Detail);
    }
}
