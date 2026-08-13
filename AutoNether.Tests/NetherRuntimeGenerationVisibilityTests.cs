#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherRuntimeGenerationVisibilityTests
{
    [Fact]
    public void Live_generation_is_absent_between_old_owner_teardown_and_new_owner_registration()
    {
        object oldOwner = new();
        object newOwner = new();

        Assert.Equal(0, NetherRuntimeGenerationVisibility.ForLiveFloorSelection(null, monotonicGeneration: 41));
        Assert.Equal(41, NetherRuntimeGenerationVisibility.ForLiveFloorSelection(oldOwner, monotonicGeneration: 41));
        Assert.Equal(0, NetherRuntimeGenerationVisibility.ForLiveFloorSelection(null, monotonicGeneration: 41));
        Assert.Equal(42, NetherRuntimeGenerationVisibility.ForLiveFloorSelection(newOwner, monotonicGeneration: 42));
    }

    [Fact]
    public void Continue_rebind_generation_stays_absent_until_current_owner_is_scene_observed()
    {
        object canceledStartStatusOwner = new();
        object initializedSceneOwner = new();

        Assert.Equal(0, NetherRuntimeGenerationVisibility.ForAuthoritativeFloorSelection(
            canceledStartStatusOwner,
            monotonicGeneration: 5,
            sceneObservedGeneration: 0
        ));
        Assert.Equal(0, NetherRuntimeGenerationVisibility.ForAuthoritativeFloorSelection(
            initializedSceneOwner,
            monotonicGeneration: 6,
            sceneObservedGeneration: 5
        ));
        Assert.Equal(6, NetherRuntimeGenerationVisibility.ForAuthoritativeFloorSelection(
            initializedSceneOwner,
            monotonicGeneration: 6,
            sceneObservedGeneration: 6
        ));
    }
}
