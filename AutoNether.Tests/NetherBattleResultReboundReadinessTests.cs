#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherBattleResultReboundReadinessTests
{
    [Theory]
    [InlineData((int)NetherSessionStatus.Play, false, true)]
    [InlineData((int)NetherSessionStatus.Battle, false, true)]
    [InlineData((int)NetherSessionStatus.Wait, true, true)]
    [InlineData((int)NetherSessionStatus.Wait, false, false)]
    public void Result_handoff_additionally_waits_for_any_wait_modal(
        int statusValue,
        bool hasPopup,
        bool expected
    )
    {
        NetherSessionStatus status = (NetherSessionStatus)statusValue;
        Assert.Equal(expected, NetherBattleResultReboundReadiness.IsModalReady(
            status,
            hasPopup
        ));
    }
}
