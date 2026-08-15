#nullable enable

using System;
using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherActivePartyHpSafetyMapperTests
{
    [Theory]
    [InlineData(0.299d, 299)]
    [InlineData(0.300d, 300)]
    public void AuthoritativeRatio_UsesCanonicalCheckedPermilleQuantization(double hpRatio, int expectedPermille)
    {
        NetherActivePartyHpSafety safety = Map(Member(1, hpRatio));

        Assert.True(safety.IsKnown);
        Assert.Equal(expectedPermille, safety.MinimumHpPermille);
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
    public void DeadRosterMember_IsExcludedFromTheCurrentLivingMinimum()
    {
        NetherActivePartyHpSafety safety = Map(
            Member(1, 0.800d),
            Member(2, 0d, isAlive: false)
        );

        Assert.True(safety.IsKnown);
        Assert.Equal(800, safety.MinimumHpPermille);
    }

    [Fact]
    public void PartyWithNoLivingMember_IsUnknown()
    {
        NetherActivePartyHpSafety safety = Map(
            Member(1, 0d, isAlive: false),
            Member(2, 0d, isAlive: false)
        );

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
        Assert.Contains("living", safety.Detail);
    }

    [Fact]
    public void LivingRows_AreCharacterIdSortedAndImmutable()
    {
        NetherActivePartyHpSafety safety = Map(
            Member(20, 0.800d),
            Member(30, 0d, isAlive: false),
            Member(10, 0.500d)
        );

        IList<NetherActiveLivingMemberHp> living =
            Assert.IsAssignableFrom<IList<NetherActiveLivingMemberHp>>(safety.LivingMembers);
        Assert.Equal(10, living[0].CharacterId);
        Assert.Equal(500, living[0].HpPermille);
        Assert.Equal(20, living[1].CharacterId);
        Assert.Equal(800, living[1].HpPermille);
        Assert.True(living.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => living.Add(new NetherActiveLivingMemberHp(40, 900)));
    }

    [Fact]
    public void EveryRuntimePartyMember_ContributesToTheMinimum()
    {
        NetherActivePartyHpSafety safety = Map(
            Member(1, 0.800d),
            Member(2, 0d)
        );

        Assert.True(safety.IsKnown);
        Assert.Equal(0, safety.MinimumHpPermille);
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
