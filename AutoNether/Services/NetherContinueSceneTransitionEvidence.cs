#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Retains the exact lifecycle evidence that can supersede a recovered Continue parent which
/// remains Pending after the game has already crossed into the next Nether segment.
/// </summary>
internal sealed class NetherContinueSceneTransitionEvidence
{
    public long OwnerGeneration { get; private set; }

    public bool FloorOwnerTerminated { get; private set; }

    public bool IsSettledBySceneTransition { get; private set; }

    public bool Begin(long ownerGeneration)
    {
        if (ownerGeneration < 1)
            return false;

        OwnerGeneration = ownerGeneration;
        FloorOwnerTerminated = false;
        IsSettledBySceneTransition = false;
        return true;
    }

    public void ObserveFloorOwnerTerminated()
    {
        if (OwnerGeneration > 0 && !IsSettledBySceneTransition)
            FloorOwnerTerminated = true;
    }

    public bool TrySettle(
        long currentGeneration,
        string? controllerType,
        string expectedControllerType
    )
    {
        if (IsSettledBySceneTransition
            || !FloorOwnerTerminated
            || OwnerGeneration < 1
            || currentGeneration <= OwnerGeneration
            || string.IsNullOrEmpty(expectedControllerType)
            || !string.Equals(controllerType, expectedControllerType, StringComparison.Ordinal))
        {
            return false;
        }

        IsSettledBySceneTransition = true;
        return true;
    }

    public void Reset()
    {
        OwnerGeneration = 0;
        FloorOwnerTerminated = false;
        IsSettledBySceneTransition = false;
    }
}
