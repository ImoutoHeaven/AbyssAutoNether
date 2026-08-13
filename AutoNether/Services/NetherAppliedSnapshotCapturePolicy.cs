#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Selects the authoritative source for a GET-only reconciliation snapshot.  A completed battle
/// is a transition boundary: a FloorSelection controller may still be registered while its
/// presentation model remains at the pre-result Battle state.  Fresh battle-result characters
/// therefore require the datastore-backed transition snapshot and must never fall back to that
/// stale presentation model.
/// </summary>
internal static class NetherAppliedSnapshotCapturePolicy
{
    private const string MissingFloorSelectionController =
        "missing-floor-selection-controller";

    public static NetherRuntimeSnapshotResult Capture(
        bool requireFreshBattleResultCharacters,
        Func<NetherRuntimeSnapshotResult> captureFullSnapshot,
        Func<NetherRuntimeSnapshotResult> captureTransitionSnapshot
    )
    {
        if (captureFullSnapshot == null)
            throw new ArgumentNullException(nameof(captureFullSnapshot));
        if (captureTransitionSnapshot == null)
            throw new ArgumentNullException(nameof(captureTransitionSnapshot));

        if (requireFreshBattleResultCharacters)
            return captureTransitionSnapshot();

        NetherRuntimeSnapshotResult captured = captureFullSnapshot();
        return !captured.IsSuccess
            && string.Equals(
                captured.Detail,
                MissingFloorSelectionController,
                StringComparison.Ordinal
            )
                ? captureTransitionSnapshot()
                : captured;
    }
}
