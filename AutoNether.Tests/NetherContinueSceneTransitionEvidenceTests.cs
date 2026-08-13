using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherContinueSceneTransitionEvidenceTests
{
    private const string FloorSelectionType =
        "Project.Nether.FloorSelection.SubViewController";

    [Fact]
    public void Exact_new_owner_after_owned_teardown_settles_once()
    {
        var evidence = new NetherContinueSceneTransitionEvidence();

        Assert.True(evidence.Begin(ownerGeneration: 10));
        evidence.ObserveFloorOwnerTerminated();

        Assert.True(evidence.TrySettle(
            currentGeneration: 11,
            controllerType: FloorSelectionType,
            expectedControllerType: FloorSelectionType
        ));
        Assert.True(evidence.FloorOwnerTerminated);
        Assert.True(evidence.IsSettledBySceneTransition);
        Assert.False(evidence.TrySettle(12, FloorSelectionType, FloorSelectionType));
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
            FloorSelectionType
        ));
        Assert.False(evidence.IsSettledBySceneTransition);
    }

    [Fact]
    public void Reset_clears_all_transition_evidence()
    {
        var evidence = new NetherContinueSceneTransitionEvidence();
        Assert.True(evidence.Begin(ownerGeneration: 10));
        evidence.ObserveFloorOwnerTerminated();
        Assert.True(evidence.TrySettle(11, FloorSelectionType, FloorSelectionType));

        evidence.Reset();

        Assert.Equal(0, evidence.OwnerGeneration);
        Assert.False(evidence.FloorOwnerTerminated);
        Assert.False(evidence.IsSettledBySceneTransition);
    }
}
