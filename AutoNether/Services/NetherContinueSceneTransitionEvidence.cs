#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Retains the exact lifecycle evidence for a Continue scene transition.  The native parent can
/// become terminal before Unity destroys its old FloorSelection owner, so completing that task
/// must not disarm the later owner-termination/rebind proof.
/// </summary>
internal sealed class NetherContinueSceneTransitionEvidence
{
    public long OwnerGeneration { get; private set; }

    public bool NativeParentPending { get; private set; }

    public bool FloorOwnerTerminated { get; private set; }

    public bool IsSettledBySceneTransition { get; private set; }

    public bool Begin(long ownerGeneration)
    {
        if (ownerGeneration < 1)
            return false;

        OwnerGeneration = ownerGeneration;
        NativeParentPending = true;
        FloorOwnerTerminated = false;
        IsSettledBySceneTransition = false;
        return true;
    }

    public void ObserveNativeParentCompleted()
    {
        if (OwnerGeneration > 0)
            NativeParentPending = false;
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
        NativeParentPending = false;
        FloorOwnerTerminated = false;
        IsSettledBySceneTransition = false;
    }
}
