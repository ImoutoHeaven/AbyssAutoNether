#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodeListInitializationTaskEvidenceTests
{
    [Fact]
    public void Registration_alone_does_not_claim_that_the_model_is_initialized()
    {
        var evidence = new NetherCodeListInitializationTaskEvidence();
        object controller = new();
        object popup = new();

        Assert.False(evidence.ObserveRegistration(
            controller,
            popup,
            NetherActionKind.BattleSettlement,
            ownerGeneration: 7,
            sequence: 11
        ));
        Assert.False(evidence.TryGetTask(
            controller,
            popup,
            NetherActionKind.BattleSettlement,
            ownerGeneration: 7,
            sequence: 11,
            out _
        ));
    }

    [Fact]
    public void Registration_then_returned_task_unlocks_only_the_exact_owned_popup()
    {
        var evidence = new NetherCodeListInitializationTaskEvidence();
        object controller = new();
        object popup = new();
        object task = new();

        Assert.False(evidence.ObserveRegistration(
            controller,
            popup,
            NetherActionKind.BattleSettlement,
            ownerGeneration: 7,
            sequence: 11
        ));
        Assert.True(evidence.ObserveTask(controller, popup, task));
        Assert.True(evidence.TryGetTask(
            controller,
            popup,
            NetherActionKind.BattleSettlement,
            ownerGeneration: 7,
            sequence: 11,
            out object? observed
        ));
        Assert.Same(task, observed);
    }

    [Fact]
    public void Returned_task_then_registration_handles_postfix_ordering_without_losing_evidence()
    {
        var evidence = new NetherCodeListInitializationTaskEvidence();
        object controller = new();
        object popup = new();
        object task = new();

        Assert.False(evidence.ObserveTask(controller, popup, task));
        Assert.True(evidence.ObserveRegistration(
            controller,
            popup,
            NetherActionKind.RecoveredCodeOffer,
            ownerGeneration: 4,
            sequence: 19
        ));
        Assert.True(evidence.TryGetTask(
            controller,
            popup,
            NetherActionKind.RecoveredCodeOffer,
            ownerGeneration: 4,
            sequence: 19,
            out object? observed
        ));
        Assert.Same(task, observed);
    }

    [Fact]
    public void Stale_identity_or_reregistered_sequence_cannot_reuse_an_old_task()
    {
        var evidence = new NetherCodeListInitializationTaskEvidence();
        object controller = new();
        object popup = new();
        object task = new();

        evidence.ObserveRegistration(
            controller,
            popup,
            NetherActionKind.BattleSettlement,
            ownerGeneration: 7,
            sequence: 11
        );
        evidence.ObserveTask(controller, popup, task);

        Assert.False(evidence.TryGetTask(
            new object(),
            popup,
            NetherActionKind.BattleSettlement,
            ownerGeneration: 7,
            sequence: 11,
            out _
        ));
        Assert.False(evidence.TryGetTask(
            controller,
            popup,
            NetherActionKind.SelectFloor,
            ownerGeneration: 7,
            sequence: 11,
            out _
        ));
        Assert.False(evidence.ObserveRegistration(
            controller,
            popup,
            NetherActionKind.BattleSettlement,
            ownerGeneration: 8,
            sequence: 12
        ));
        Assert.False(evidence.TryGetTask(
            controller,
            popup,
            NetherActionKind.BattleSettlement,
            ownerGeneration: 8,
            sequence: 12,
            out _
        ));
    }

    [Fact]
    public void Invalidation_and_reset_remove_task_evidence()
    {
        var evidence = new NetherCodeListInitializationTaskEvidence();
        object controller = new();
        object popup = new();

        evidence.ObserveRegistration(
            controller,
            popup,
            NetherActionKind.SelectFloor,
            ownerGeneration: 3,
            sequence: 9
        );
        evidence.ObserveTask(controller, popup, new object());
        Assert.True(evidence.InvalidatePopup(popup));
        Assert.False(evidence.TryGetTask(
            controller,
            popup,
            NetherActionKind.SelectFloor,
            ownerGeneration: 3,
            sequence: 9,
            out _
        ));

        object nextPopup = new();
        evidence.ObserveTask(controller, nextPopup, new object());
        evidence.ObserveRegistration(
            controller,
            nextPopup,
            NetherActionKind.SelectFloor,
            ownerGeneration: 3,
            sequence: 10
        );
        evidence.Reset();
        Assert.False(evidence.TryGetTask(
            controller,
            nextPopup,
            NetherActionKind.SelectFloor,
            ownerGeneration: 3,
            sequence: 10,
            out _
        ));
    }
}
