#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherFloorSceneReadinessTests
{
    [Theory]
    [InlineData(10, 10, true, true, true, true, true, "awaiting-new-generation")]
    [InlineData(10, 11, false, true, true, true, true, "awaiting-current-controller")]
    [InlineData(10, 11, true, false, true, true, true, "awaiting-authoritative-controller")]
    [InlineData(10, 11, true, true, false, true, true, "awaiting-subscene-entered")]
    [InlineData(10, 11, true, true, true, false, true, "awaiting-authoritative-snapshot")]
    [InlineData(10, 11, true, true, true, true, false, "controller-changed-during-snapshot")]
    public void Incomplete_scene_proof_never_opens_the_gate(
        long minimumGenerationExclusive,
        long runtimeGeneration,
        bool hasController,
        bool expectedController,
        bool entered,
        bool hasSnapshot,
        bool stableCapture,
        string expectedDetail
    )
    {
        NetherFloorSceneReadinessDecision decision = NetherFloorSceneReadiness.Evaluate(new(
            minimumGenerationExclusive,
            runtimeGeneration,
            hasController,
            expectedController,
            entered,
            hasSnapshot,
            stableCapture
        ));

        Assert.False(decision.IsReady);
        Assert.Contains(expectedDetail, decision.Detail);
    }

    [Fact]
    public void Current_new_controller_snapshot_and_matching_on_entered_open_one_gate()
    {
        NetherFloorSceneReadinessDecision decision = NetherFloorSceneReadiness.Evaluate(new(
            MinimumGenerationExclusive: 10,
            RuntimeGeneration: 11,
            HasCurrentController: true,
            IsExpectedCurrentController: true,
            HasEnteredCurrentGeneration: true,
            HasAuthoritativeSnapshot: true,
            CaptureStayedOnCurrentController: true
        ));

        Assert.True(decision.IsReady);
        Assert.Equal("floor-scene-ready:generation=11", decision.Detail);
    }
}
