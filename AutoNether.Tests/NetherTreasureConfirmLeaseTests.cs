#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherTreasureConfirmLeaseTests
{
    [Fact]
    public void Floor_26_treasure_does_not_allow_skip_before_native_open_animation_is_ready()
    {
        // Fresh current-game evidence (GameAssembly SHA-256 573fa800...):
        // - LogOutput.log lines 2399-2402: OnConfirm dispatch is followed by an immediate
        //   SkipOpenTreasureAnimationAsync NullReferenceException and a stalled parent.
        // - Fresh Cpp2IL recovery: HandleEventConfirmedAsync awaits RequestNetherUpdateEventAsync
        //   before PlayOpenTreasureAnimationSequenceAsync starts the popup's Open animation.
        var popup = new object();
        var lease = new NetherTreasureConfirmLease();
        var owner = new NetherTreasureConfirmOwner(
            popup,
            NetherActionKind.SelectFloor,
            OwnerGeneration: 26,
            RuntimeGeneration: 30,
            Sequence: 48
        );

        Assert.True(lease.Begin(owner));

        Assert.False(lease.TryClaimSkip(owner, openAnimationObserved: false));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingOpenAnimation, lease.Stage);
        Assert.True(lease.TryClaimSkip(owner, openAnimationObserved: true));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingSkipTask, lease.Stage);
    }

    [Fact]
    public void Exact_treasure_owner_requires_skip_completion_and_a_resume_pump_before_confirm_tap()
    {
        var popup = new object();
        var lease = new NetherTreasureConfirmLease();
        var owner = new NetherTreasureConfirmOwner(
            popup,
            NetherActionKind.SelectFloor,
            OwnerGeneration: 78,
            RuntimeGeneration: 12,
            Sequence: 138
        );

        Assert.True(lease.Begin(owner));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingOpenAnimation, lease.Stage);
        Assert.False(lease.TryClaimSkip(owner, openAnimationObserved: false));
        Assert.True(lease.TryClaimSkip(owner, openAnimationObserved: true));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingSkipTask, lease.Stage);
        Assert.False(lease.TryClaimConfirm(owner));

        Assert.True(lease.ObserveSkipTaskCompleted(owner));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingResumePump, lease.Stage);
        Assert.False(lease.TryClaimConfirm(owner));

        Assert.True(lease.AdvanceResumePump(owner));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingConfirmTap, lease.Stage);
        Assert.True(lease.TryClaimConfirm(owner));
        Assert.Equal(NetherTreasureConfirmStage.Completed, lease.Stage);
        Assert.False(lease.TryClaimConfirm(owner));
    }

    [Fact]
    public void Stale_popup_runtime_owner_or_sequence_cannot_advance_the_treasure_flow()
    {
        var popup = new object();
        var lease = new NetherTreasureConfirmLease();
        var owner = new NetherTreasureConfirmOwner(
            popup,
            NetherActionKind.None,
            OwnerGeneration: 0,
            RuntimeGeneration: 9,
            Sequence: 41
        );
        Assert.True(lease.Begin(owner));

        Assert.False(lease.TryClaimSkip(
            owner with { Popup = new object() },
            openAnimationObserved: true
        ));
        Assert.False(lease.TryClaimSkip(
            owner with { RuntimeGeneration = 10 },
            openAnimationObserved: true
        ));
        Assert.False(lease.TryClaimSkip(
            owner with { Sequence = 42 },
            openAnimationObserved: true
        ));
        Assert.False(lease.TryClaimSkip(owner with
        {
            OwnerAction = NetherActionKind.SelectFloor,
            OwnerGeneration = 1,
        }, openAnimationObserved: true));

        Assert.Equal(NetherTreasureConfirmStage.AwaitingOpenAnimation, lease.Stage);
        Assert.True(lease.TryClaimSkip(owner, openAnimationObserved: true));
    }

    [Fact]
    public void Only_the_exact_popup_invalidation_resets_the_one_shot_flow()
    {
        var popup = new object();
        var lease = new NetherTreasureConfirmLease();
        var owner = new NetherTreasureConfirmOwner(
            popup,
            NetherActionKind.SelectFloor,
            OwnerGeneration: 5,
            RuntimeGeneration: 7,
            Sequence: 11
        );
        Assert.True(lease.Begin(owner));

        Assert.False(lease.InvalidatePopup(new object()));
        Assert.Equal(NetherTreasureConfirmStage.AwaitingOpenAnimation, lease.Stage);
        Assert.True(lease.InvalidatePopup(popup));
        Assert.Equal(NetherTreasureConfirmStage.Idle, lease.Stage);
        Assert.False(lease.TryGetOwner(out _));
    }

    [Theory]
    [InlineData((int)NetherActionKind.None, 1)]
    [InlineData((int)NetherActionKind.SelectFloor, 0)]
    [InlineData((int)NetherActionKind.SelectEventOption, 1)]
    public void Invalid_owner_shapes_fail_closed(int rawOwnerAction, long ownerGeneration)
    {
        var lease = new NetherTreasureConfirmLease();
        Assert.False(lease.Begin(new NetherTreasureConfirmOwner(
            new object(),
            (NetherActionKind)rawOwnerAction,
            ownerGeneration,
            RuntimeGeneration: 1,
            Sequence: 1
        )));
        Assert.Equal(NetherTreasureConfirmStage.Idle, lease.Stage);
    }
}
