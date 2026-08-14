#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodeTransformPolicyTests
{
    [Fact]
    public void Conversion_removes_the_card_whose_absence_preserves_the_best_evidenced_portfolio()
    {
        NetherCodeTransformDecision decision = Decide(
            Code(1, NetherCodeFamily.Safe, power: 1),
            Code(2, NetherCodeFamily.Rush, power: 5),
            Code(3, NetherCodeFamily.Risk, power: 10)
        );

        Assert.True(decision.CanTransform, decision.Detail);
        Assert.Equal(1, decision.RemoveCodeId);
    }

    [Fact]
    public void Paired_counter_coherence_beats_static_power_when_choosing_a_conversion_source()
    {
        NetherCodeTransformDecision decision = Decide(
            Code(1, NetherCodeFamily.Rush, power: 1),
            Code(2, NetherCodeFamily.Rush, power: 1),
            Code(3, NetherCodeFamily.Impact, power: 9999)
        );

        Assert.True(decision.CanTransform, decision.Detail);
        Assert.Equal(3, decision.RemoveCodeId);
    }

    [Fact]
    public void Safe_and_risk_have_no_magic_protection_or_forced_removal()
    {
        NetherCodeTransformDecision decision = Decide(
            Code(30024, NetherCodeFamily.Safe, power: 10),
            Code(40024, NetherCodeFamily.Risk, power: 1)
        );

        Assert.True(decision.CanTransform, decision.Detail);
        Assert.Equal(30024, decision.RemoveCodeId);
    }

    [Fact]
    public void Invalid_or_duplicate_portfolio_fails_closed()
    {
        Assert.Equal(
            NetherPauseReason.UnknownMasterData,
            new NetherCodeTransformPolicy().Decide(
                [Code(1, NetherCodeFamily.Rush), Code(1, NetherCodeFamily.Rush)],
                capacity: 5
            ).PauseReason
        );
        Assert.Equal(
            NetherPauseReason.UnknownMasterData,
            new NetherCodeTransformPolicy().Decide(
                [Code(1, NetherCodeFamily.Rush) with { IsKnown = false }],
                capacity: 5
            ).PauseReason
        );
    }

    private static NetherCodeTransformDecision Decide(params NetherCodeState[] codes) =>
        new NetherCodeTransformPolicy().Decide(codes, capacity: 5);

    private static NetherCodeState Code(
        long id,
        NetherCodeFamily family,
        int rarity = 1,
        int abilityLevel = 1,
        int power = 0
    ) => new(id, family, abilityLevel)
    {
        IsKnown = true,
        Category = family switch
        {
            NetherCodeFamily.Rush => NetherCodeCategory.Rush,
            NetherCodeFamily.Impact => NetherCodeCategory.Impact,
            NetherCodeFamily.Safe => NetherCodeCategory.Safe,
            NetherCodeFamily.Risk => NetherCodeCategory.Risk,
            _ => NetherCodeCategory.Unknown,
        },
        Rarity = rarity,
        Power = power,
    };
}
