#nullable enable

namespace AutoNether.Services;

/// <summary>
/// Maps the native Nether reroll counters to the remaining count consumed by strategy policy.
/// </summary>
internal static class NetherCodeReloadAvailability
{
    internal static int FromNative(int maximumRerolls, int usedRerolls)
    {
        if (maximumRerolls < 0 || usedRerolls < 0 || usedRerolls > maximumRerolls)
            return 0;

        return maximumRerolls - usedRerolls;
    }
}
