#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodeIdentityTests
{
    [Fact]
    public void Portfolio_identity_is_order_independent_and_tracks_possession_effect_and_level_fields()
    {
        NetherCodeState first = State(101);
        NetherCodeState second = State(202) with { AbilityLevel = 2 };

        Assert.Equal(
            NetherCodeIdentity.CreatePortfolio([first, second]),
            NetherCodeIdentity.CreatePortfolio([second, first])
        );
        Assert.NotEqual(
            NetherCodeIdentity.CreatePortfolio([first]),
            NetherCodeIdentity.CreatePortfolio([first with { EffectParameter3 = 99 }])
        );
        Assert.NotEqual(
            NetherCodeIdentity.CreatePortfolio([first]),
            NetherCodeIdentity.CreatePortfolio([first with { AbilityLevel = 3 }])
        );
        Assert.NotEqual(
            NetherCodeIdentity.CreatePortfolio([first]),
            NetherCodeIdentity.CreatePortfolio([first with { PossessionAmount = 4 }])
        );
        Assert.NotEqual(
            NetherCodeIdentity.CreatePortfolio([first]),
            NetherCodeIdentity.CreatePortfolio([first with { EffectSemanticsKnown = false }])
        );
    }

    [Fact]
    public void Candidate_identity_includes_raw_effect_and_party_coverage_evidence()
    {
        NetherCodeCandidate candidate = Candidate(303);

        Assert.NotEqual(
            NetherCodeIdentity.CreateCandidates([candidate]),
            NetherCodeIdentity.CreateCandidates([candidate with { MasterEffectType = (NetherCodeMasterEffectType)12 }])
        );
        Assert.NotEqual(
            NetherCodeIdentity.CreateCandidates([candidate]),
            NetherCodeIdentity.CreateCandidates([candidate with { PartyCoverageKnown = true, PartyCoverage = 2 }])
        );
        Assert.NotEqual(
            NetherCodeIdentity.CreateCandidates([candidate]),
            NetherCodeIdentity.CreateCandidates([candidate with { EffectSemanticsKnown = false }])
        );
    }

    private static NetherCodeState State(long id) => new(id, NetherCodeFamily.Rush, 1)
    {
        IsKnown = true,
        Category = NetherCodeCategory.Rush,
        Rarity = 2,
        Power = 300,
        MasterEffectType = NetherCodeMasterEffectType.NetherAbility,
        EffectParameter1 = 100006,
        EffectParameter2 = 1,
        EffectParameter3 = 7,
        AbilityAssetId = 100006,
        PossessionAmount = 1,
    };

    private static NetherCodeCandidate Candidate(long id) => new(id, NetherCodeFamily.Impact, 1)
    {
        IsKnown = true,
        Category = NetherCodeCategory.Impact,
        Rarity = 2,
        Power = 300,
        MasterEffectType = NetherCodeMasterEffectType.CommonAbility,
        EffectParameter1 = 200007,
        EffectParameter2 = 1,
        EffectParameter3 = 8,
        AbilityAssetId = 200007,
    };
}
