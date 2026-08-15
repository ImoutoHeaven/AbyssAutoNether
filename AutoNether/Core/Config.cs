using AutoNether.Services;
using BepInEx.Configuration;

namespace AutoNether;

public static class Config
{
    internal static ConfigEntry<int> NetherAutoClimbMaxDepth { get; private set; } = null!;
    internal static ConfigEntry<NetherStrategyMode> NetherAutoClimbStrategyMode { get; private set; } = null!;
    internal static ConfigEntry<NetherCodeFamily> NetherAutoClimbResearchPrimaryFamily { get; private set; } = null!;
    internal static ConfigEntry<NetherCodeFamily> NetherAutoClimbResearchSecondaryFamily { get; private set; } = null!;
    internal static ConfigEntry<int> NetherAutoClimbSoftErosionLimit { get; private set; } = null!;
    internal static ConfigEntry<int> NetherAutoClimbMinimumCharacterHpPermille { get; private set; } = null!;
    internal static ConfigEntry<NetherCombatLane> NetherAutoClimbCombatLane { get; private set; } = null!;
    internal static ConfigEntry<int> NetherAutoClimbCodeReloadReserve { get; private set; } = null!;
    internal static ConfigEntry<NetherTreasureMode> NetherAutoClimbTreasureMode { get; private set; } = null!;
    internal static ConfigEntry<NetherShopMode> NetherAutoClimbShopMode { get; private set; } = null!;
    internal static ConfigEntry<bool> NetherAutoClimbDetailedLogging { get; private set; } = null!;
    internal static ConfigEntry<string> CheckpointPreserveItemIds { get; private set; } = null!;

    public static void Initialize()
    {
        NetherAutoClimbMaxDepth = Plugin.ConfigFile.Bind(
            "AutoNether",
            "MaximumDepth",
            130,
            "Maximum floor to climb. The effective limit is also bounded by server/master-data limits."
        );
        NetherAutoClimbStrategyMode = Plugin.ConfigFile.Bind(
            "AutoNether",
            "StrategyMode",
            NetherStrategyMode.Equipment,
            "Explicit strategy mode. Equipment is the default; Research must be selected by the user and is never auto-detected."
        );
        NetherAutoClimbResearchPrimaryFamily = Plugin.ConfigFile.Bind(
            "AutoNether",
            "ResearchPrimaryFamily",
            NetherCodeFamily.Unknown,
            "Primary Rush, Impact, Safe, or Risk family for Research mode. Required when StrategyMode is Research."
        );
        NetherAutoClimbResearchSecondaryFamily = Plugin.ConfigFile.Bind(
            "AutoNether",
            "ResearchSecondaryFamily",
            NetherCodeFamily.Unknown,
            "Optional secondary family for Research mode. Unknown disables it; opposed family pairs are rejected."
        );
        NetherAutoClimbSoftErosionLimit = Plugin.ConfigFile.Bind(
            "AutoNether",
            "SoftErosionLimit",
            90,
            "Pause before a projected route reaches this erosion percentage; 100 is always a hard stop."
        );
        NetherAutoClimbMinimumCharacterHpPermille = Plugin.ConfigFile.Bind(
            "AutoNether",
            "MinimumCharacterHpPermille",
            300,
            "Minimum active-character HP in permille (1-1000) accepted by route safety checks."
        );
        NetherAutoClimbCombatLane = Plugin.ConfigFile.Bind(
            "AutoNether",
            "CombatLane",
            NetherCombatLane.Auto,
            "Preferred Nether Code combat lane: Auto, Rush, or Impact. Unknown semantics pause safely."
        );
        NetherAutoClimbCodeReloadReserve = Plugin.ConfigFile.Bind(
            "AutoNether",
            "CodeReloadReserve",
            1,
            "Number of native Nether Code rerolls to keep in reserve."
        );
        NetherAutoClimbTreasureMode = Plugin.ConfigFile.Bind(
            "AutoNether",
            "TreasureMode",
            NetherTreasureMode.KeyOnly,
            "Treasure strategy. KeyOnly prefers a verified one-key option and, without a key, permits only an exact HP option that leaves at least one character alive; Off pauses safely."
        );
        NetherAutoClimbShopMode = Plugin.ConfigFile.Bind(
            "AutoNether",
            "ShopMode",
            NetherShopMode.Off,
            "Shop strategy. Off leaves through the verified native callback; EquipmentBags buys verified affordable bags."
        );
        NetherAutoClimbDetailedLogging = Plugin.ConfigFile.Bind(
            "AutoNether",
            "DetailedLogging",
            true,
            "Enable bounded [F12][AutoNether] diagnostics for snapshots, routes, native tasks, and reconciliation."
        );
        CheckpointPreserveItemIds = Plugin.ConfigFile.Bind(
            "AutoNether",
            "CheckpointPreserveItemIds",
            string.Empty,
            "Comma/semicolon/space-separated decimal item IDs to return at a ten-floor checkpoint. Empty uses the native safe default."
        );

        Plugin.ConfigFile.SettingChanged += (_, e) =>
        {
            var setting = e.ChangedSetting;
            Plugin.Log.LogInfo(
                $"[{setting.Definition.Section}] {setting.Definition.Key} => {setting.BoxedValue}"
            );
        };
    }
}
