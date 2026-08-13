#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherRecoveredFloorEventTaskLeaseTests
{
    [Fact]
    public void Exact_current_controller_task_can_be_claimed_once_by_its_new_popup()
    {
        var controller = new object();
        var task = new object();
        var popup = new object();
        var lease = new NetherRecoveredFloorEventTaskLease();

        Assert.True(lease.ObserveSequence(controller, generation: 2, task, popupSequenceBaseline: 4));
        Assert.True(lease.ObservePopup(controller, generation: 2, popup, sequence: 5));
        Assert.True(lease.TryClaim(controller, generation: 2, popup, sequence: 5, out object? claimed));
        Assert.Same(task, claimed);
        Assert.False(lease.TryClaim(controller, generation: 2, popup, sequence: 5, out _));
    }

    [Fact]
    public void Stale_controller_generation_or_popup_cannot_claim_the_task()
    {
        var controller = new object();
        var task = new object();
        var popup = new object();
        var lease = new NetherRecoveredFloorEventTaskLease();

        Assert.True(lease.ObserveSequence(controller, generation: 3, task, popupSequenceBaseline: 8));
        Assert.True(lease.ObservePopup(controller, generation: 3, popup, sequence: 9));

        Assert.False(lease.TryClaim(new object(), generation: 3, popup, sequence: 9, out _));
        Assert.False(lease.TryClaim(controller, generation: 2, popup, sequence: 9, out _));
        Assert.False(lease.TryClaim(controller, generation: 3, new object(), sequence: 9, out _));
        Assert.False(lease.TryClaim(controller, generation: 3, popup, sequence: 10, out _));
        Assert.True(lease.TryClaim(controller, generation: 3, popup, sequence: 9, out object? claimed));
        Assert.Same(task, claimed);
    }

    [Fact]
    public void New_unclaimed_sequence_supersedes_old_evidence_and_popup_must_be_newer()
    {
        var controller = new object();
        var firstTask = new object();
        var secondTask = new object();
        var oldPopup = new object();
        var newPopup = new object();
        var lease = new NetherRecoveredFloorEventTaskLease();

        Assert.True(lease.ObserveSequence(controller, generation: 4, firstTask, popupSequenceBaseline: 10));
        Assert.False(lease.ObservePopup(controller, generation: 4, oldPopup, sequence: 10));
        Assert.True(lease.ObserveSequence(controller, generation: 4, secondTask, popupSequenceBaseline: 12));
        Assert.True(lease.ObservePopup(controller, generation: 4, newPopup, sequence: 13));

        Assert.False(lease.TryClaim(controller, generation: 4, oldPopup, sequence: 10, out _));
        Assert.True(lease.TryClaim(controller, generation: 4, newPopup, sequence: 13, out object? claimed));
        Assert.Same(secondTask, claimed);
    }

    [Fact]
    public void Popup_observed_immediately_before_sequence_can_be_claimed_once()
    {
        var controller = new object();
        var task = new object();
        var popup = new object();
        var lease = new NetherRecoveredFloorEventTaskLease();

        Assert.False(lease.ObservePopup(controller, generation: 6, popup, sequence: 7));
        Assert.True(lease.ObserveSequence(controller, generation: 6, task, popupSequenceBaseline: 7));

        Assert.True(lease.HasBoundPopup);
        Assert.True(lease.CanClaim(controller, generation: 6, popup, sequence: 7));
        Assert.True(lease.TryClaim(controller, generation: 6, popup, sequence: 7, out object? claimed));
        Assert.Same(task, claimed);
        Assert.False(lease.TryClaim(controller, generation: 6, popup, sequence: 7, out _));
    }

    [Fact]
    public void Popup_first_recovery_rejects_non_immediate_or_mismatched_evidence()
    {
        var controller = new object();
        var task = new object();
        var oldPopup = new object();
        var currentPopup = new object();
        var lease = new NetherRecoveredFloorEventTaskLease();

        Assert.False(lease.ObservePopup(controller, generation: 6, oldPopup, sequence: 7));
        Assert.True(lease.ObserveSequence(controller, generation: 6, task, popupSequenceBaseline: 8));
        Assert.False(lease.TryClaim(controller, generation: 6, oldPopup, sequence: 7, out _));

        Assert.True(lease.ObservePopup(controller, generation: 6, currentPopup, sequence: 9));
        Assert.False(lease.TryClaim(new object(), generation: 6, currentPopup, sequence: 9, out _));
        Assert.False(lease.TryClaim(controller, generation: 5, currentPopup, sequence: 9, out _));
        Assert.False(lease.TryClaim(controller, generation: 6, new object(), sequence: 9, out _));
        Assert.False(lease.TryClaim(controller, generation: 6, currentPopup, sequence: 10, out _));
        Assert.True(lease.TryClaim(controller, generation: 6, currentPopup, sequence: 9, out _));
    }
}
