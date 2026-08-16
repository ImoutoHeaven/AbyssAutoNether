using System.Collections.Concurrent;
using System.IO;

namespace BepInEx
{
    // The real lease only needs this framework path boundary. Tests use a disposable directory.
    public static class Paths
    {
        public static string ConfigPath { get; set; } = Path.GetTempPath();
    }
}

namespace AutoNether
{
    // Logging is intentionally outside the behavior asserted by the lease tests.
    public static class Logger
    {
        private static readonly ConcurrentQueue<string> Captured = new();

        public static IReadOnlyCollection<string> Messages => Captured.ToArray();

        public static void Reset()
        {
            while (Captured.TryDequeue(out _)) { }
        }

        public static void Info(string message) => Captured.Enqueue(message);
        public static void Error(string message) => Captured.Enqueue(message);
    }

    public sealed class ControllerTestConfigEntry<T>
    {
        public ControllerTestConfigEntry(T value) => Value = value;

        public T Value { get; set; }
    }

    // These are the exact config reads made by the linked production Controller.  They are
    // simple test values rather than a second controller implementation.
    public static class Config
    {
        internal static ControllerTestConfigEntry<int> NetherAutoClimbMaxDepth { get; } = new(130);
        internal static ControllerTestConfigEntry<AutoNether.Services.NetherStrategyMode> NetherAutoClimbStrategyMode { get; } = new(AutoNether.Services.NetherStrategyMode.Equipment);
        internal static ControllerTestConfigEntry<AutoNether.Services.NetherCodeFamily> NetherAutoClimbResearchPrimaryFamily { get; } = new(AutoNether.Services.NetherCodeFamily.Unknown);
        internal static ControllerTestConfigEntry<AutoNether.Services.NetherCodeFamily> NetherAutoClimbResearchSecondaryFamily { get; } = new(AutoNether.Services.NetherCodeFamily.Unknown);
        internal static ControllerTestConfigEntry<int> NetherAutoClimbSoftErosionLimit { get; } = new(90);
        internal static ControllerTestConfigEntry<int> NetherAutoClimbMinimumCharacterHpPermille { get; } = new(300);
        internal static ControllerTestConfigEntry<AutoNether.Services.NetherCombatLane> NetherAutoClimbCombatLane { get; } = new(AutoNether.Services.NetherCombatLane.Auto);
        internal static ControllerTestConfigEntry<int> NetherAutoClimbCodeReloadReserve { get; } = new(1);
        internal static ControllerTestConfigEntry<AutoNether.Services.NetherTreasureMode> NetherAutoClimbTreasureMode { get; } = new(AutoNether.Services.NetherTreasureMode.KeyOnly);
        internal static ControllerTestConfigEntry<AutoNether.Services.NetherShopMode> NetherAutoClimbShopMode { get; } = new(AutoNether.Services.NetherShopMode.Off);
        internal static ControllerTestConfigEntry<bool> NetherAutoClimbEquipmentRecoveryCodeTransformEnabled { get; } = new(false);
        internal static ControllerTestConfigEntry<bool> NetherAutoClimbDetailedLogging { get; } = new(false);
        internal static ControllerTestConfigEntry<string> CheckpointPreserveItemIds { get; } = new(string.Empty);
    }
}
