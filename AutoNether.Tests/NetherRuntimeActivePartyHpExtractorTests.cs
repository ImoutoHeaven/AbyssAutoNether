#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherRuntimeActivePartyHpExtractorTests
{
    [Fact]
    public void NetherModelPartyCharacterModels_AreReadAsTheAuthoritativeHpSurface()
    {
        var netherModel = new FakeNetherModel(
            new FakePartyModel(
                new FakePartyCharacter(10, 0.900d, isAlive: true),
                new FakePartyCharacter(20, 0.299d, isAlive: true)
            )
        );

        NetherActivePartyHpSafety safety = new NetherRuntimeActivePartyHpExtractor().Extract(netherModel);

        Assert.True(safety.IsKnown);
        Assert.Equal(299, safety.MinimumHpPermille);
    }

    [Fact]
    public void DeadRosterMember_IsExcludedFromFreshAuthoritativeLivingHp()
    {
        var netherModel = new FakeNetherModel(
            new FakePartyModel(
                new FakePartyCharacter(10, 0d, isAlive: false),
                new FakePartyCharacter(20, 0.500d, isAlive: true)
            )
        );

        NetherActivePartyHpSafety safety = new NetherRuntimeActivePartyHpExtractor().Extract(netherModel);

        Assert.True(safety.IsKnown);
        Assert.Equal(500, safety.MinimumHpPermille);
    }

    [Fact]
    public void MissingPartyOrCharacters_IsUnknown()
    {
        NetherActivePartyHpSafety safety = new NetherRuntimeActivePartyHpExtractor().Extract(
            new FakeNetherModel(null)
        );

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
        Assert.Contains("party", safety.Detail);
    }

    [Fact]
    public void MissingIsAlive_RemainsUnknown()
    {
        NetherActivePartyHpSafety safety = new NetherRuntimeActivePartyHpExtractor().Extract(
            new FakeNetherModel(
                new FakePartyModel(new FakePartyCharacterWithoutAlive(10, 0.500d))
            )
        );

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
        Assert.Contains("member", safety.Detail);
    }

    [Fact]
    public void DuplicateOrNonFiniteRuntimeCharacter_IsUnknown()
    {
        var netherModel = new FakeNetherModel(
            new FakePartyModel(
                new FakePartyCharacter(10, 0.900d, isAlive: true),
                new FakePartyCharacter(10, double.NaN, isAlive: true)
            )
        );

        NetherActivePartyHpSafety safety = new NetherRuntimeActivePartyHpExtractor().Extract(netherModel);

        Assert.False(safety.IsKnown);
        Assert.Null(safety.MinimumHpPermille);
    }

    private sealed class FakeNetherModel
    {
        public FakeNetherModel(FakePartyModel? partyModel) => PartyModel = partyModel;

        public FakePartyModel? PartyModel { get; }
    }

    private sealed class FakePartyModel
    {
        public FakePartyModel(params object[] characterModels) => CharacterModels = characterModels;

        public object[] CharacterModels { get; }
    }

    private sealed class FakePartyCharacter
    {
        public FakePartyCharacter(long characterId, double hpRatio, bool isAlive)
        {
            MCharacterId = characterId;
            HpRatio = hpRatio;
            IsAlive = isAlive;
        }

        public long MCharacterId { get; }
        public double HpRatio { get; }
        public bool IsAlive { get; }
    }

    private sealed class FakePartyCharacterWithoutAlive
    {
        public FakePartyCharacterWithoutAlive(long characterId, double hpRatio)
        {
            MCharacterId = characterId;
            HpRatio = hpRatio;
        }

        public long MCharacterId { get; }
        public double HpRatio { get; }
    }
}
