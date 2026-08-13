#nullable enable

namespace AutoNether.Services;

/// <summary>
/// Defines when the FloorSelection owner created by a battle-result Next transition is ready
/// to return to ordinary planning. A readable Play snapshot can precede SubScene.OnEntered;
/// releasing it at that point races the game's StartStatus flow and can execute the next floor
/// twice. A Wait snapshot is modal by definition and also requires its popup registration.
/// </summary>
internal static class NetherBattleResultReboundReadiness
{
    public static bool IsReady(
        NetherSessionStatus status,
        bool hasActivePopup,
        bool hasSceneEntry
    ) => hasSceneEntry && (status != NetherSessionStatus.Wait || hasActivePopup);
}
