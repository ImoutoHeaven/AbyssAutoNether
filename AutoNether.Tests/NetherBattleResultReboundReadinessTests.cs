#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherBattleResultReboundReadinessTests
{
    [Theory]
    [InlineData((int)NetherSessionStatus.Play, false, false, false)]
    [InlineData((int)NetherSessionStatus.Play, false, true, true)]
    [InlineData((int)NetherSessionStatus.Battle, false, true, true)]
    [InlineData((int)NetherSessionStatus.Wait, true, true, true)]
    [InlineData((int)NetherSessionStatus.Wait, false, true, false)]
    public void Result_handoff_waits_for_scene_entry_and_any_wait_modal(
        int statusValue,
        bool hasPopup,
        bool hasSceneEntry,
        bool expected
    )
    {
        NetherSessionStatus status = (NetherSessionStatus)statusValue;
        Assert.Equal(expected, NetherBattleResultReboundReadiness.IsReady(
            status,
            hasPopup,
            hasSceneEntry
        ));
    }
}
