using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherContinueSceneTransitionEvidenceTests
{
    private const string FloorSelectionType =
        "Project.Nether.FloorSelection.SubViewController";
    private const string SceneRegistrationSource = "subscene-initialize:scene-member";

    [Fact]
    public void Exact_new_owner_after_owned_teardown_settles_once()
    {
        var evidence = new NetherContinueSceneTransitionEvidence();

        Assert.True(evidence.Begin(ownerGeneration: 10));
        evidence.ObserveFloorOwnerTerminated();

        Assert.True(evidence.TrySettle(
            currentGeneration: 11,
            controllerType: FloorSelectionType,
            expectedControllerType: FloorSelectionType,
            registrationSource: SceneRegistrationSource
        ));
        Assert.True(evidence.FloorOwnerTerminated);
        Assert.True(evidence.IsSettledBySceneTransition);
        Assert.False(evidence.TrySettle(
            12,
            FloorSelectionType,
            FloorSelectionType,
            SceneRegistrationSource
        ));
    }

    [Fact]
    public void Native_parent_completion_before_owned_teardown_keeps_transition_evidence_armed()
    {
        var evidence = new NetherContinueSceneTransitionEvidence();
        Assert.True(evidence.Begin(ownerGeneration: 10));

        evidence.ObserveNativeParentCompleted();
        evidence.ObserveFloorOwnerTerminated();

        Assert.True(evidence.FloorOwnerTerminated);
        Assert.True(evidence.TrySettle(
            currentGeneration: 11,
            controllerType: FloorSelectionType,
            expectedControllerType: FloorSelectionType,
            registrationSource: SceneRegistrationSource
        ));
    }

    [Fact]
    public void Canceled_start_status_registration_cannot_settle_before_scene_lifecycle_registration()
    {
        var evidence = new NetherContinueSceneTransitionEvidence();
        Assert.True(evidence.Begin(ownerGeneration: 4));
        evidence.ObserveNativeParentCompleted();
        evidence.ObserveFloorOwnerTerminated();

        Assert.False(evidence.TrySettle(
            currentGeneration: 5,
            controllerType: FloorSelectionType,
            expectedControllerType: FloorSelectionType,
            registrationSource: "start-status-state-machine-enter"
        ));
        Assert.False(evidence.IsSettledBySceneTransition);

        Assert.True(evidence.TrySettle(
            currentGeneration: 6,
            controllerType: FloorSelectionType,
            expectedControllerType: FloorSelectionType,
            registrationSource: "subscene-initialize:scene-member"
        ));
        Assert.True(evidence.IsSettledBySceneTransition);
    }

    [Fact]
    public void Destroy_token_parent_cancellation_preserves_lease_but_cannot_settle_scene()
    {
        var evidence = new NetherContinueSceneTransitionEvidence();
        NetherNativeActionResult canceled = NetherNativeActionResult.UnknownOutcome(
            "native-start-status-terminal-canceled"
        );
        Assert.True(evidence.Begin(ownerGeneration: 10));

        Assert.False(evidence.TryObserveCanceledNativeParentAfterOwnerTransition(canceled));
        Assert.True(evidence.NativeParentPending);

        evidence.ObserveFloorOwnerTerminated();

        Assert.False(evidence.TryObserveCanceledNativeParentAfterOwnerTransition(
            NetherNativeActionResult.UnknownOutcome("native-start-status-terminal-faulted")
        ));
        Assert.True(evidence.TryObserveCanceledNativeParentAfterOwnerTransition(canceled));
        Assert.False(evidence.NativeParentPending);
        Assert.True(evidence.FloorOwnerTerminated);
        Assert.False(evidence.IsSettledBySceneTransition);
        Assert.False(evidence.TrySettle(
            currentGeneration: 11,
            controllerType: FloorSelectionType,
            expectedControllerType: FloorSelectionType,
            registrationSource: "start-status-state-machine-enter"
        ));
        Assert.True(evidence.TrySettle(
            currentGeneration: 11,
            controllerType: FloorSelectionType,
            expectedControllerType: FloorSelectionType,
            registrationSource: SceneRegistrationSource
        ));
    }

    [Theory]
    [InlineData(false, 11, FloorSelectionType)]
    [InlineData(true, 10, FloorSelectionType)]
    [InlineData(true, 9, FloorSelectionType)]
    [InlineData(true, 11, "Project.Nether.FloorSelection.OtherController")]
    [InlineData(true, 11, null)]
    public void Incomplete_or_inexact_transition_does_not_settle(
        bool ownerTerminated,
        long currentGeneration,
        string? controllerType
    )
    {
        var evidence = new NetherContinueSceneTransitionEvidence();
        Assert.True(evidence.Begin(ownerGeneration: 10));
        if (ownerTerminated)
            evidence.ObserveFloorOwnerTerminated();

        Assert.False(evidence.TrySettle(
            currentGeneration,
            controllerType,
            FloorSelectionType,
            SceneRegistrationSource
        ));
        Assert.False(evidence.IsSettledBySceneTransition);
    }

    [Fact]
    public void Reset_clears_all_transition_evidence()
    {
        var evidence = new NetherContinueSceneTransitionEvidence();
        Assert.True(evidence.Begin(ownerGeneration: 10));
        evidence.ObserveFloorOwnerTerminated();
        Assert.True(evidence.TrySettle(
            11,
            FloorSelectionType,
            FloorSelectionType,
            SceneRegistrationSource
        ));

        evidence.Reset();

        Assert.Equal(0, evidence.OwnerGeneration);
        Assert.False(evidence.NativeParentPending);
        Assert.False(evidence.FloorOwnerTerminated);
        Assert.False(evidence.IsSettledBySceneTransition);
    }
}
