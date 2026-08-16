#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherActivePartyHpSafetyMapperTests
{
    [Theory]
    [InlineData(0.299d, 299)]
    [InlineData(0.300d, 300)]
    public void AuthoritativeRatio_UsesCheckedServerPermilleInverse(double hpRatio, int expectedPermille)
    {
        NetherActivePartyHpSafety safety = Map(Member(1, hpRatio));

        Assert.True(safety.IsKnown);
        Assert.Equal(expectedPermille, safety.MinimumHpPermille);
    }

    [Fact]
    public void ServerPermilleEncodedAsSingle_RoundTripsToTheAuthoritativeInteger()
    {
        NetherActivePartyHpSafety safety = Map(Member(1, (double)(float)0.704f));

        Assert.True(safety.IsKnown);
        Assert.Equal(704, safety.MinimumHpPermille);
    }

    [Fact]
    public void RatioThatCannotComeFromAnIntegerServerPermille_IsUnknown()
    {
        NetherActivePartyHpSafety safety = Map(Member(1, 0.3004d));

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
        Assert.Contains("permille", safety.Detail);
    }

    [Fact]
    public void MultipleActiveMembers_UsesTheLowestPermille()
    {
        NetherActivePartyHpSafety safety = Map(
            Member(1, 0.900d),
            Member(2, 0.300d),
            Member(3, 0.750d)
        );

        Assert.True(safety.IsKnown);
        Assert.Equal(300, safety.MinimumHpPermille);
    }

    [Fact]
    public void ZeroHpNonLivingRosterMember_IsExcludedFromTheLivingMinimum()
    {
        NetherActivePartyHpSafety safety = Map(
            Member(1, 0.800d),
            Member(2, 0d, isAlive: false)
        );

        Assert.True(safety.IsKnown);
        Assert.Equal(800, safety.MinimumHpPermille);
    }

    [Fact]
    public void AliveFlagContradictingTheNativeHpGetter_IsUnknown()
    {
        NetherActivePartyHpSafety safety = Map(
            Member(1, 0.800d),
            Member(2, 0d)
        );

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
        Assert.Contains("contradictory", safety.Detail);
    }

    [Fact]
    public void PartyWithNoLivingMembers_IsUnknown()
    {
        NetherActivePartyHpSafety safety = Map(Member(1, 0d, isAlive: false));

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
        Assert.Contains("empty-living", safety.Detail);
    }

    [Theory]
    [InlineData(-0.001d)]
    [InlineData(1.001d)]
    public void InvalidAuthoritativeRatio_IsUnknown(double hpRatio)
    {
        NetherActivePartyHpSafety safety = Map(Member(1, hpRatio));

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
    }

    [Fact]
    public void DuplicateActiveCharacter_IsUnknown()
    {
        NetherActivePartyHpSafety safety = Map(
            Member(1, 0.500d),
            Member(1, 0.600d)
        );

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
        Assert.Contains("duplicate", safety.Detail);
    }

    [Fact]
    public void NonFiniteAuthoritativeRatio_IsUnknown()
    {
        NetherActivePartyHpSafety safety = Map(Member(1, double.NaN));

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
        Assert.Contains("non-finite", safety.Detail);
    }

    [Fact]
    public void EmptyRuntimeParty_IsUnknown()
    {
        NetherActivePartyHpSafety safety = Map();

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
    }

    private static NetherActivePartyHpSafety Map(params NetherActiveBattleMemberHp[] members) =>
        new NetherActivePartyHpSafetyMapper().Map(members);

    private static NetherActiveBattleMemberHp Member(long characterId, double hpRatio, bool isAlive = true) =>
        new(characterId, hpRatio, isAlive);
}
