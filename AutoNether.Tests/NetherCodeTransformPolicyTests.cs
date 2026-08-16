#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodeTransformPolicyTests
{
    [Fact]
    public void Equipment_transform_uses_hard_reason_then_code_identity_not_reversed_display_power()
    {
        // Fresh Project.dll 53806a5b...1300: target_type=7 sends only the chosen owned Code ID;
        // the replacement is server-random. MNetherCodes.power is display/reference data and the
        // native transform request never consumes it. With equal hard-exclusion reasons, CodeId is
        // therefore the deterministic authority even when displayed Power is reversed.
        NetherCodeTransformDecision decision = new NetherCodeTransformPolicy().Decide(
            [
                Code(10, NetherCodeFamily.Risk, power: 99_999),
                Code(20, NetherCodeFamily.Risk, power: 1),
            ],
            capacity: 5,
            new NetherCodeTransformEligibilityEvidence
            {
                StrategyMode = NetherStrategyMode.Equipment,
                EquipmentOptInEnabled = true,
                IsRecovery = true,
                DeterministicRecoveryChoicesHaveZeroValue = true,
                HardExcludedCodes =
                [
                    new NetherCodeTransformHardExclusion(
                        10,
                        NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment
                    ),
                    new NetherCodeTransformHardExclusion(
                        20,
                        NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment
                    ),
                ],
            }
        );

        Assert.True(decision.CanTransform, decision.Detail);
        Assert.Equal(10, decision.RemoveCodeId);
    }

    [Theory]
    [InlineData((int)NetherStrategyMode.Research, true, true)]
    [InlineData((int)NetherStrategyMode.Equipment, false, true)]
    [InlineData((int)NetherStrategyMode.Equipment, true, false)]
    public void Transform_rejects_research_disabled_opt_in_or_valuable_deterministic_recovery(
        int rawMode,
        bool optIn,
        bool deterministicChoicesAreZero
    )
    {
        NetherCodeTransformDecision decision = new NetherCodeTransformPolicy().Decide(
            [Code(10, NetherCodeFamily.Risk, power: 1)],
            capacity: 5,
            new NetherCodeTransformEligibilityEvidence
            {
                StrategyMode = (NetherStrategyMode)rawMode,
                EquipmentOptInEnabled = optIn,
                IsRecovery = true,
                DeterministicRecoveryChoicesHaveZeroValue = deterministicChoicesAreZero,
                HardExcludedCodes =
                [
                    new NetherCodeTransformHardExclusion(
                        10,
                        NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment
                    ),
                ],
            }
        );

        Assert.False(decision.CanTransform);
    }

    [Fact]
    public void Transform_rejects_when_no_exact_hard_excluded_owned_code_is_proven()
    {
        NetherCodeTransformDecision decision = new NetherCodeTransformPolicy().Decide(
            [Code(10, NetherCodeFamily.Safe, power: 1)],
            capacity: 5,
            new NetherCodeTransformEligibilityEvidence
            {
                StrategyMode = NetherStrategyMode.Equipment,
                EquipmentOptInEnabled = true,
                IsRecovery = true,
                DeterministicRecoveryChoicesHaveZeroValue = true,
                HardExcludedCodes = [],
            }
        );

        Assert.False(decision.CanTransform);
        Assert.Equal(0, decision.RemoveCodeId);
    }

    [Fact]
    public void Invalid_or_duplicate_portfolio_fails_closed()
    {
        Assert.Equal(
            NetherPauseReason.UnknownMasterData,
            new NetherCodeTransformPolicy().Decide(
                [Code(1, NetherCodeFamily.Rush), Code(1, NetherCodeFamily.Rush)],
                capacity: 5,
                Eligible(1)
            ).PauseReason
        );
        Assert.Equal(
            NetherPauseReason.UnknownMasterData,
            new NetherCodeTransformPolicy().Decide(
                [Code(1, NetherCodeFamily.Rush) with { IsKnown = false }],
                capacity: 5,
                Eligible(1)
            ).PauseReason
        );
    }

    private static NetherCodeTransformEligibilityEvidence Eligible(long codeId) => new()
    {
        StrategyMode = NetherStrategyMode.Equipment,
        EquipmentOptInEnabled = true,
        IsRecovery = true,
        DeterministicRecoveryChoicesHaveZeroValue = true,
        HardExcludedCodes =
        [
            new NetherCodeTransformHardExclusion(
                codeId,
                NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment
            ),
        ],
    };

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
