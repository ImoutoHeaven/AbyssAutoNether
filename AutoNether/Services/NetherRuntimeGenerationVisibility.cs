#nullable enable

namespace AutoNether.Services;

/// <summary>
/// Separates the bridge's internal monotonically increasing registration counter from the
/// Continue coordinator's observable liveness contract.  A retained counter is not evidence
/// that the old FloorSelection controller still exists: absence must be represented as zero so
/// the bounded rebind gate can wait for the next exact owner registration.
/// </summary>
internal static class NetherRuntimeGenerationVisibility
{
    public static long ForLiveFloorSelection(object? liveFloorSelection, long monotonicGeneration) =>
        liveFloorSelection == null ? 0 : monotonicGeneration;

    /// <summary>
    /// Continue rebind consumes a stronger signal than ordinary controller liveness. A generated
    /// StartStatus hook may register a canceled controller first, so expose the generation only
    /// after the current controller has also been observed through the scene lifecycle.
    /// </summary>
    public static long ForAuthoritativeFloorSelection(
        object? liveFloorSelection,
        long monotonicGeneration,
        long sceneObservedGeneration
    ) => liveFloorSelection != null
        && monotonicGeneration > 0
        && sceneObservedGeneration == monotonicGeneration
            ? monotonicGeneration
            : 0;
}
