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

    /// <summary>
    /// The current native Continue path changes scene and then awaits work with the old
    /// FloorSelection owner's destroy token.  Cancellation after that exact owner terminated is
    /// therefore parent-terminal evidence, but it is not scene-settlement evidence.  Keeping this
    /// lease armed lets the normal new-generation lifecycle gate prove the transition later.
    /// </summary>
    public bool TryObserveCanceledNativeParentAfterOwnerTransition(
        NetherNativeActionResult result
    )
    {
        if (OwnerGeneration < 1
            || !NativeParentPending
            || !FloorOwnerTerminated
            || result.Kind != NetherNativeActionResultKind.UnknownOutcome
            || result.Detail.IndexOf("canceled", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        NativeParentPending = false;
        return true;
    }

    public bool TrySettle(
        long currentGeneration,
        string? controllerType,
        string expectedControllerType,
        string? registrationSource
    )
    {
        if (IsSettledBySceneTransition
            || !FloorOwnerTerminated
            || OwnerGeneration < 1
            || currentGeneration <= OwnerGeneration
            || string.IsNullOrEmpty(expectedControllerType)
            || !string.Equals(controllerType, expectedControllerType, StringComparison.Ordinal)
            || !IsAuthoritativeSceneRegistration(registrationSource))
        {
            return false;
        }

        IsSettledBySceneTransition = true;
        return true;
    }

    /// <summary>
    /// Only a FloorSelection controller extracted from the scene lifecycle proves that the
    /// replacement scene exists. StartStatus hooks can expose a canceled, short-lived controller
    /// before the real scene initializes and therefore remain observation-only registrations.
    /// </summary>
    public static bool IsAuthoritativeSceneRegistration(string? source) =>
        source != null
        && (source.StartsWith("subscene-initialize:", StringComparison.Ordinal)
            || source.StartsWith("subscene-refresh:", StringComparison.Ordinal)
            || source.StartsWith("subscene-entered:", StringComparison.Ordinal));

    public void Reset()
    {
        OwnerGeneration = 0;
        NativeParentPending = false;
        FloorOwnerTerminated = false;
        IsSettledBySceneTransition = false;
    }
}
