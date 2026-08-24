#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodeReloadAvailabilityTests
{
    [Theory]
    [InlineData(3, 0, 3)]
    [InlineData(3, 1, 2)]
    [InlineData(3, 2, 1)]
    [InlineData(3, 3, 0)]
    public void Native_used_count_is_mapped_to_remaining_rerolls(
        int maximumRerolls,
        int usedRerolls,
        int expectedRemaining
    )
    {
        Assert.Equal(
            expectedRemaining,
            NetherCodeReloadAvailability.FromNative(maximumRerolls, usedRerolls)
        );
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, -1)]
    [InlineData(3, 4)]
    public void Invalid_native_counters_fail_closed_without_inventing_rerolls(
        int maximumRerolls,
        int usedRerolls
    )
    {
        Assert.Equal(
            0,
            NetherCodeReloadAvailability.FromNative(maximumRerolls, usedRerolls)
        );
    }
}
