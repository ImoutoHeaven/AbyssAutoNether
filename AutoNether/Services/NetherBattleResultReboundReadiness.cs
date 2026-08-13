#nullable enable

namespace AutoNether.Services;

/// <summary>
/// Battle-specific evidence layered on top of the shared FloorSelection scene-readiness gate.
/// A Wait snapshot is modal by definition and additionally requires its popup registration.
/// </summary>
internal static class NetherBattleResultReboundReadiness
{
    public static bool IsModalReady(
        NetherSessionStatus status,
        bool hasActivePopup
    ) => status != NetherSessionStatus.Wait || hasActivePopup;
}
