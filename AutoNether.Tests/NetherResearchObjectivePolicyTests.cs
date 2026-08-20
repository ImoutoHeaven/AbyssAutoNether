#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherResearchObjectivePolicyTests
{
    [Fact]
    public void Unknown_future_settlement_keeps_the_first_nonfull_wallet_active_conservatively()
    {
        NetherStrategyResearchFamilyState[] families = Families();
        families[3] = Family(NetherCodeFamily.Risk, 10_000, projectedKnown: false);
        NetherResearchObjectiveResolution result = NetherResearchObjectivePolicy.Resolve(
            NetherCodeFamily.Risk,
            NetherCodeFamily.Impact,
            families
        );

        Assert.True(result.IsValid);
        Assert.True(result.HasIncompleteTargets);
        Assert.True(result.UsesConservativePriority);
        Assert.Equal(NetherCodeFamily.Risk, result.ActiveFamily);
        Assert.Equal("native-result-only", result.Detail);
    }

    [Fact]
    public void Full_wallet_proves_completion_without_waiting_for_an_unknown_future_result()
    {
        NetherStrategyResearchFamilyState[] families = Families();
        families[0] = Family(NetherCodeFamily.Rush, 20_000, projectedKnown: false);

        NetherResearchObjectiveResolution result = NetherResearchObjectivePolicy.Resolve(
            NetherCodeFamily.Rush,
            NetherCodeFamily.Safe,
            families
        );

        Assert.True(result.IsValid);
        Assert.True(result.HasIncompleteTargets);
        Assert.False(result.UsesConservativePriority);
        Assert.Equal(NetherCodeFamily.Safe, result.ActiveFamily);
    }

    [Fact]
    public void Full_configured_wallets_are_complete_even_when_future_results_are_unknown()
    {
        NetherStrategyResearchFamilyState[] families = Families();
        families[0] = Family(NetherCodeFamily.Rush, 20_000, projectedKnown: false);
        families[2] = Family(NetherCodeFamily.Safe, 20_000, projectedKnown: false);

        NetherResearchObjectiveResolution result = NetherResearchObjectivePolicy.Resolve(
            NetherCodeFamily.Rush,
            NetherCodeFamily.Safe,
            families
        );

        Assert.True(result.IsValid);
        Assert.False(result.HasIncompleteTargets);
        Assert.Equal(NetherCodeFamily.Unknown, result.ActiveFamily);
    }

    [Fact]
    public void Missing_configured_family_row_remains_invalid_instead_of_using_the_fallback()
    {
        NetherResearchObjectiveResolution result = NetherResearchObjectivePolicy.Resolve(
            NetherCodeFamily.Rush,
            NetherCodeFamily.Unknown,
            [Family(NetherCodeFamily.Safe, 0, projectedKnown: false)]
        );

        Assert.False(result.IsValid);
        Assert.False(result.HasIncompleteTargets);
        Assert.Contains("family-row-unavailable", result.Detail);
    }

    [Theory]
    [InlineData((int)NetherStrategyMode.Equipment, (int)NetherResearchTargetState.Unknown, false)]
    [InlineData((int)NetherStrategyMode.Research, (int)NetherResearchTargetState.Active, true)]
    [InlineData((int)NetherStrategyMode.Research, (int)NetherResearchTargetState.Complete, false)]
    public void Route_priority_is_derived_from_the_accepted_strategy_audit(
        int rawMode,
        int rawState,
        bool expected
    )
    {
        bool? result = NetherResearchObjectivePolicy.ToRouteIncomplete(
            new NetherStrategyEvidenceAudit
            {
                Mode = (NetherStrategyMode)rawMode,
                ResearchTargetState = (NetherResearchTargetState)rawState,
            }
        );

        Assert.Equal(expected, result);
    }

    private static NetherStrategyResearchFamilyState[] Families() =>
    [
        Family(NetherCodeFamily.Rush, 0, projectedKnown: true),
        Family(NetherCodeFamily.Impact, 0, projectedKnown: true),
        Family(NetherCodeFamily.Safe, 0, projectedKnown: true),
        Family(NetherCodeFamily.Risk, 0, projectedKnown: true),
    ];

    private static NetherStrategyResearchFamilyState Family(
        NetherCodeFamily family,
        int wallet,
        bool projectedKnown
    ) => new(family, wallet, 0, 0)
    {
        IsProjectedNormalSettlementKnown = projectedKnown,
        ProjectionUnknownReason = projectedKnown ? string.Empty : "native-result-only",
    };
}
