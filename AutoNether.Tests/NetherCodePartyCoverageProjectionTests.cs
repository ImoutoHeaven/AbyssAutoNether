using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherCodePartyCoverageProjectionTests
{
    [Fact]
    public void Applies_known_zero_without_mutating_the_authoritative_snapshot_record()
    {
        NetherCodeState original = Code(1);

        IReadOnlyList<NetherCodeState> projected = NetherCodePartyCoverageProjection.Apply(
            [original],
            new Dictionary<long, int> { [1] = 0 }
        );

        Assert.False(original.PartyCoverageKnown);
        Assert.True(projected[0].PartyCoverageKnown);
        Assert.Equal(0, projected[0].PartyCoverage);
        Assert.NotSame(original, projected[0]);
    }

    [Fact]
    public void Missing_or_negative_coverage_remains_unknown()
    {
        NetherCodeState missing = Code(1);
        NetherCodeState negative = Code(2);

        IReadOnlyList<NetherCodeState> projected = NetherCodePartyCoverageProjection.Apply(
            [missing, negative],
            new Dictionary<long, int> { [2] = -1 }
        );

        Assert.Same(missing, projected[0]);
        Assert.Same(negative, projected[1]);
        Assert.False(projected[0].PartyCoverageKnown);
        Assert.False(projected[1].PartyCoverageKnown);
    }

    private static NetherCodeState Code(long id) => new(id, NetherCodeFamily.Safe, 1)
    {
        Category = NetherCodeCategory.Safe,
        Power = 10,
        PossessionAmount = 1,
        PartyCoverageKnown = false,
        PartyCoverage = 0,
    };
}
