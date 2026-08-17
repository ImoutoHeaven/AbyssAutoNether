using AutoNether.Services;
using BepInEx.Configuration;

namespace AutoNether;

internal interface IConfigValue<T>
{
    T Value { get; set; }
}

internal sealed class DefaultConfigValue<T> : IConfigValue<T>
{
    public DefaultConfigValue(T value) => Value = value;

    public T Value { get; set; }
}

internal sealed class BoundConfigValue<T> : IConfigValue<T>
{
    private readonly ConfigEntry<T> _entry;

    public BoundConfigValue(ConfigEntry<T> entry) => _entry = entry;

    public T Value
    {
        get => _entry.Value;
        set => _entry.Value = value;
    }
}

public static class Config
{
    internal static IConfigValue<int> NetherAutoClimbMaxDepth { get; private set; } = new DefaultConfigValue<int>(130);
    internal static IConfigValue<NetherStrategyMode> NetherAutoClimbStrategyMode { get; private set; } = new DefaultConfigValue<NetherStrategyMode>(NetherStrategyMode.Equipment);
    internal static IConfigValue<NetherCodeFamily> NetherAutoClimbResearchPrimaryFamily { get; private set; } = new DefaultConfigValue<NetherCodeFamily>(NetherCodeFamily.Unknown);
    internal static IConfigValue<NetherCodeFamily> NetherAutoClimbResearchSecondaryFamily { get; private set; } = new DefaultConfigValue<NetherCodeFamily>(NetherCodeFamily.Unknown);
    internal static IConfigValue<int> NetherAutoClimbSoftErosionLimit { get; private set; } = new DefaultConfigValue<int>(90);
    internal static IConfigValue<int> NetherAutoClimbMinimumCharacterHpPermille { get; private set; } = new DefaultConfigValue<int>(300);
    internal static IConfigValue<NetherCombatLane> NetherAutoClimbCombatLane { get; private set; } = new DefaultConfigValue<NetherCombatLane>(NetherCombatLane.Auto);
    internal static IConfigValue<int> NetherAutoClimbCodeReloadReserve { get; private set; } = new DefaultConfigValue<int>(1);
    internal static IConfigValue<NetherTreasureMode> NetherAutoClimbTreasureMode { get; private set; } = new DefaultConfigValue<NetherTreasureMode>(NetherTreasureMode.KeyOnly);
    internal static IConfigValue<NetherShopMode> NetherAutoClimbShopMode { get; private set; } = new DefaultConfigValue<NetherShopMode>(NetherShopMode.Off);
    internal static IConfigValue<bool> NetherAutoClimbEquipmentRecoveryCodeTransformEnabled { get; private set; } = new DefaultConfigValue<bool>(false);
    internal static IConfigValue<bool> NetherAutoClimbDetailedLogging { get; private set; } = new DefaultConfigValue<bool>(true);
    internal static IConfigValue<string> CheckpointPreserveItemIds { get; private set; } = new DefaultConfigValue<string>(string.Empty);

    public static void Initialize()
    {
        NetherAutoClimbMaxDepth = Bound(
            "AutoNether",
            "MaximumDepth",
            130,
            "Maximum floor to climb. The effective limit is also bounded by server/master-data limits."
        );
        NetherAutoClimbStrategyMode = Bound(
            "AutoNether",
            "StrategyMode",
            NetherStrategyMode.Equipment,
            "Explicit strategy mode. Equipment is the default; Research must be selected by the user and is never auto-detected."
        );
        NetherAutoClimbResearchPrimaryFamily = Bound(
            "AutoNether",
            "ResearchPrimaryFamily",
            NetherCodeFamily.Unknown,
            "Primary Rush, Impact, Safe, or Risk family for Research mode. Required when StrategyMode is Research."
        );
        NetherAutoClimbResearchSecondaryFamily = Bound(
            "AutoNether",
            "ResearchSecondaryFamily",
            NetherCodeFamily.Unknown,
            "Optional secondary family for Research mode. Unknown disables it; opposed family pairs are rejected."
        );
        NetherAutoClimbSoftErosionLimit = Bound(
            "AutoNether",
            "SoftErosionLimit",
            90,
            "Pause before a projected route reaches this erosion percentage; 100 is always a hard stop."
        );
        NetherAutoClimbMinimumCharacterHpPermille = Bound(
            "AutoNether",
            "MinimumCharacterHpPermille",
            300,
            "Minimum active-character HP in permille (1-1000) accepted by route safety checks."
        );
        NetherAutoClimbCombatLane = Bound(
            "AutoNether",
            "CombatLane",
            NetherCombatLane.Auto,
            "Preferred Nether Code combat lane: Auto, Rush, or Impact. Unknown semantics pause safely."
        );
        NetherAutoClimbCodeReloadReserve = Bound(
            "AutoNether",
            "CodeReloadReserve",
            1,
            "Number of native Nether Code rerolls to keep in reserve."
        );
        NetherAutoClimbTreasureMode = Bound(
            "AutoNether",
            "TreasureMode",
            NetherTreasureMode.KeyOnly,
            "Treasure strategy. KeyOnly prefers a verified one-key option and, without a key, permits only an exact HP option that leaves at least one character alive; Off pauses safely."
        );
        NetherAutoClimbShopMode = Bound(
            "AutoNether",
            "ShopMode",
            NetherShopMode.Off,
            "Shop strategy. Off leaves through the verified native callback; EquipmentBags buys verified affordable bags."
        );
        NetherAutoClimbEquipmentRecoveryCodeTransformEnabled = Bound(
            "AutoNether",
            "EquipmentRecoveryCodeTransformEnabled",
            false,
            "Equipment-only opt-in for Recovery Code transform. It remains disabled unless exact Rest/Purification zero-value and a hard-excluded held Code are proven."
        );
        NetherAutoClimbDetailedLogging = Bound(
            "AutoNether",
            "DetailedLogging",
            true,
            "Enable bounded [F12][AutoNether] diagnostics for snapshots, routes, native tasks, and reconciliation."
        );
        CheckpointPreserveItemIds = Bound(
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

    private static IConfigValue<T> Bound<T>(
        string section,
        string key,
        T defaultValue,
        string description
    ) => new BoundConfigValue<T>(Plugin.ConfigFile.Bind(section, key, defaultValue, description));
}
