#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodeReceivedConfirmLeaseTests
{
    [Theory]
    [InlineData((int)NetherActionKind.SelectFloor)]
    [InlineData((int)NetherActionKind.BattleSettlement)]
    [InlineData((int)NetherActionKind.RecoveredCodeOffer)]
    public void Current_code_flow_can_claim_its_received_overlay_once(int rawOwnerAction)
    {
        var ownerAction = (NetherActionKind)rawOwnerAction;
        var popup = new object();
        var close = new object();
        var lease = new NetherCodeReceivedConfirmLease();

        Assert.True(lease.Begin(ownerAction, ownerGeneration: 7, codeId: 10020));
        Assert.True(lease.TryGetOwner(out NetherActionKind actualOwner, out long actualGeneration));
        Assert.Equal(ownerAction, actualOwner);
        Assert.Equal(7, actualGeneration);
        Assert.True(lease.RegisterPopup(popup, close, sequence: 19, codeId: 10020));

        NetherCodeReceivedConfirmClaim claimed = lease.Claim(ownerAction, 7, 10020);
        Assert.Equal(NetherCodeReceivedConfirmClaimKind.Claimed, claimed.Kind);
        Assert.Same(close, claimed.Close);
        Assert.Equal(19, claimed.Sequence);
        Assert.Equal(NetherCodeReceivedConfirmClaimKind.None, lease.Claim(ownerAction, 7, 10020).Kind);
    }

    [Fact]
    public void Stale_owner_or_wrong_code_cannot_claim_or_bind_the_overlay()
    {
        var lease = new NetherCodeReceivedConfirmLease();
        Assert.True(lease.Begin(NetherActionKind.RecoveredCodeOffer, ownerGeneration: 4, codeId: 10020));

        Assert.False(lease.RegisterPopup(new object(), new object(), sequence: 5, codeId: 10021));
        Assert.Equal(
            NetherCodeReceivedConfirmClaimKind.None,
            lease.Claim(NetherActionKind.RecoveredCodeOffer, 4, 10020).Kind
        );

        Assert.True(lease.RegisterPopup(new object(), new object(), sequence: 6, codeId: 10020));
        Assert.Equal(
            NetherCodeReceivedConfirmClaimKind.CorrelationMismatch,
            lease.Claim(NetherActionKind.RecoveredCodeOffer, 3, 10020).Kind
        );
        Assert.Equal(
            NetherCodeReceivedConfirmClaimKind.CorrelationMismatch,
            lease.Claim(NetherActionKind.BattleSettlement, 4, 10020).Kind
        );
    }

    [Fact]
    public void Invalid_owner_missing_close_and_popup_invalidation_fail_closed()
    {
        var lease = new NetherCodeReceivedConfirmLease();
        Assert.False(lease.Begin(NetherActionKind.None, ownerGeneration: 1, codeId: 10020));
        Assert.False(lease.Begin(NetherActionKind.SelectFloor, ownerGeneration: 0, codeId: 10020));

        Assert.True(lease.Begin(NetherActionKind.SelectFloor, ownerGeneration: 2, codeId: 10020));
        var popup = new object();
        Assert.True(lease.RegisterPopup(popup, close: null, sequence: 8, codeId: 10020));
        Assert.Equal(
            NetherCodeReceivedConfirmClaimKind.MissingClose,
            lease.Claim(NetherActionKind.SelectFloor, 2, 10020).Kind
        );
        Assert.Equal(
            NetherCodeReceivedConfirmClaimKind.None,
            lease.Claim(NetherActionKind.SelectFloor, 2, 10020).Kind
        );

        lease.Reset();
        Assert.True(lease.Begin(NetherActionKind.SelectFloor, ownerGeneration: 3, codeId: 10020));
        Assert.True(lease.RegisterPopup(popup, new object(), sequence: 9, codeId: 10020));
        Assert.False(lease.InvalidatePopup(new object()));
        Assert.True(lease.InvalidatePopup(popup));
        Assert.Equal(
            NetherCodeReceivedConfirmClaimKind.None,
            lease.Claim(NetherActionKind.SelectFloor, 3, 10020).Kind
        );
    }
}
