using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherNativePortfolioValuationTests
{
    [Fact]
    public void Allow_combines_only_matching_simultaneous_windows_and_clips_positive_group_limit()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // BuffGroup.GetSumValue sums enabled matching values; GetMaxLimit takes the greatest native
        // positive limit. Distinct BuffType groups are not combined by BuffGroup.AddBuff.
        NetherNativeBuffWindow held = Window(
            codeId: 1,
            buffType: 10,
            value: 400,
            start: 0,
            duration: 10,
            coexistence: NetherStrategyBuffCoexistenceKind.Allow,
            limit: 500
        );
        NetherNativeBuffWindow candidate = Window(
            codeId: 2,
            buffType: 10,
            value: 300,
            start: 0,
            duration: 10,
            coexistence: NetherStrategyBuffCoexistenceKind.Allow,
            limit: 500
        );
        NetherNativeBuffWindow differentGroup = Window(
            codeId: 3,
            buffType: 20,
            value: 900,
            start: 0,
            duration: 10,
            coexistence: NetherStrategyBuffCoexistenceKind.Allow
        );

        NetherNativePortfolioValue value = new NetherNativePortfolioValuation().Evaluate(
            new NetherNativePortfolioTimelineInput(
                [held],
                [candidate, differentGroup],
                BossDurationSeconds: 10
            )
        );

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        NetherNativeMetricExposure exposure = Assert.Single(value.Exposures);
        Assert.Equal(4_000, exposure.BeforePermilleSeconds);
        Assert.Equal(14_000, exposure.AfterPermilleSeconds);
        Assert.Equal(10_000, exposure.MarginalPermilleSeconds);
    }

    [Fact]
    public void Higher_value_replaces_on_value_or_equal_longer_window_and_displaced_effect_never_resumes()
    {
        // Fresh BuffController.CheckCoexistenceHigherValue compares the incoming value against the
        // highest enabled group value; equality is accepted only when IsLongerRemainTime is true.
        // Acceptance calls BuffGroup.RemoveAllBuff before the incoming window starts.
        NetherNativeBuffWindow weak = Window(
            codeId: 10,
            buffType: 10,
            value: 200,
            start: 0,
            duration: 20,
            coexistence: NetherStrategyBuffCoexistenceKind.HigherValue
        );
        NetherNativeBuffWindow stronger = Window(
            codeId: 11,
            buffType: 10,
            value: 300,
            start: 5,
            duration: 5,
            coexistence: NetherStrategyBuffCoexistenceKind.HigherValue
        );

        NetherNativePortfolioValue value = new NetherNativePortfolioValuation().Evaluate(
            new NetherNativePortfolioTimelineInput([weak], [stronger], BossDurationSeconds: 20)
        );

        NetherNativeMetricExposure exposure = Assert.Single(value.Exposures);
        Assert.Equal(4_000, exposure.BeforePermilleSeconds);
        Assert.Equal(2_500, exposure.AfterPermilleSeconds);
        Assert.Equal(-1_500, exposure.MarginalPermilleSeconds);
    }

    [Fact]
    public void Critical_clips_at_guaranteed_threshold_but_continuous_attack_uses_finite_decreasing_ladder()
    {
        // Fresh GameAssembly 573fa800...c1fb: CriticalRate.CalculateCritical samples
        // BattleRandom.Range(0,1000) and accepts random <= probability, so 999 is guaranteed.
        // UnitAttackContinuous accepts random < probability, decrements probability by exactly
        // 100 after each success, and is bounded by the live ICharacterStatus maximum count.
        var valuation = new NetherNativePortfolioValuation();

        int criticalMarginal = valuation.CriticalProbabilityMarginalPermille(950, 100);
        long continuousBefore = valuation.ContinuousAttackExpectedAdditionalMicros(950, 3);
        long continuousAfter = valuation.ContinuousAttackExpectedAdditionalMicros(1_050, 3);

        Assert.Equal(49, criticalMarginal);
        Assert.Equal(2_363_125, continuousBefore);
        Assert.Equal(2_757_500, continuousAfter);
    }

    [Fact]
    public void Defense_comparison_uses_exact_relative_ehp_then_rear_coverage_weakest_gain_and_aggregate()
    {
        var valuation = new NetherNativePortfolioValuation();
        NetherDefenseComparison sameRecipient = valuation.CompareDefense(
            [EffectiveHp(1, NetherPartyPosition.Back, before: 100, after: 130)],
            [EffectiveHp(1, NetherPartyPosition.Back, before: 1_000, after: 1_250)]
        );
        NetherDefenseComparison differentRecipients = valuation.CompareDefense(
            [
                EffectiveHp(1, NetherPartyPosition.Back, before: 100, after: 110),
                EffectiveHp(2, NetherPartyPosition.Back, before: 100, after: 110),
            ],
            [EffectiveHp(3, NetherPartyPosition.Back, before: 100, after: 150)]
        );

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, sameRecipient.Kind);
        Assert.Equal(1, sameRecipient.Preferred);
        Assert.Equal(1, differentRecipients.Preferred);
    }

    [Fact]
    public void Delayed_repeatable_boss_windows_remain_relevant_and_unknown_trigger_is_candidate_local_unquantified()
    {
        NetherNativeBuffWindow held = Window(
            codeId: 30,
            buffType: 10,
            value: 100,
            start: 0,
            duration: 30,
            coexistence: NetherStrategyBuffCoexistenceKind.Allow
        );
        NetherNativeBuffWindow delayed = Window(
            codeId: 31,
            buffType: 20,
            value: 500,
            start: 15,
            duration: 5,
            coexistence: NetherStrategyBuffCoexistenceKind.Allow
        );
        NetherNativePortfolioValue valued = new NetherNativePortfolioValuation().Evaluate(
            new NetherNativePortfolioTimelineInput([held], [delayed], BossDurationSeconds: 30)
        );
        NetherNativePortfolioValue unknown = new NetherNativePortfolioValuation().Evaluate(
            new NetherNativePortfolioTimelineInput(
                [held],
                [delayed with { TriggerKnown = false }],
                BossDurationSeconds: 30
            )
        );

        Assert.Equal(2_500, Assert.Single(valued.Exposures).MarginalPermilleSeconds);
        Assert.Equal(NetherCombatValueEvidenceKind.ReachableUnquantified, unknown.Kind);
        Assert.Contains("unavailable", unknown.Detail);
    }

    [Fact]
    public void Equal_higher_value_requires_strictly_longer_remaining_time()
    {
        NetherNativeBuffWindow held = Window(
            codeId: 40,
            buffType: 10,
            value: 500,
            start: 0,
            duration: 5,
            coexistence: NetherStrategyBuffCoexistenceKind.HigherValue
        );
        NetherNativeBuffWindow shorter = Window(
            codeId: 41,
            buffType: 10,
            value: 500,
            start: 1,
            duration: 3,
            coexistence: NetherStrategyBuffCoexistenceKind.HigherValue
        );
        NetherNativeBuffWindow longer = shorter with { CodeId = 42, DurationSeconds = 10 };

        NetherNativePortfolioValue rejected = new NetherNativePortfolioValuation().Evaluate(
            new NetherNativePortfolioTimelineInput([held], [shorter], BossDurationSeconds: 11)
        );
        NetherNativePortfolioValue replaced = new NetherNativePortfolioValuation().Evaluate(
            new NetherNativePortfolioTimelineInput([held], [longer], BossDurationSeconds: 11)
        );

        Assert.Equal(0, Assert.Single(rejected.Exposures).MarginalPermilleSeconds);
        Assert.Equal(3_000, Assert.Single(replaced.Exposures).MarginalPermilleSeconds);
    }

    [Fact]
    public void Missing_metric_relationship_is_unquantified_without_erasing_a_known_other_candidate()
    {
        NetherNativeBuffWindow known = Window(
            codeId: 50,
            buffType: 10,
            value: 100,
            start: 0,
            duration: 10,
            coexistence: NetherStrategyBuffCoexistenceKind.Allow
        ) with { Metric = NetherCombatMetricKind.DamageModifier };
        NetherNativeBuffWindow unknown = Window(
            codeId: 51,
            buffType: 20,
            value: 9_999,
            start: 0,
            duration: 10,
            coexistence: NetherStrategyBuffCoexistenceKind.Allow
        ) with
        {
            Metric = NetherCombatMetricKind.ElementDamage,
            MetricInputsKnown = false,
        };

        NetherNativePortfolioValue knownValue = new NetherNativePortfolioValuation().Evaluate(
            new NetherNativePortfolioTimelineInput([], [known], BossDurationSeconds: 10)
        );
        NetherNativePortfolioValue unknownValue = new NetherNativePortfolioValuation().Evaluate(
            new NetherNativePortfolioTimelineInput([], [unknown], BossDurationSeconds: 10)
        );

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, knownValue.Kind);
        Assert.Equal(1_000, Assert.Single(knownValue.Exposures).MarginalPermilleSeconds);
        Assert.Equal(NetherCombatValueEvidenceKind.ReachableUnquantified, unknownValue.Kind);
    }

    [Theory]
    [InlineData((int)NetherCombatMetricKind.Attack)]
    [InlineData((int)NetherCombatMetricKind.Defence)]
    [InlineData((int)NetherCombatMetricKind.MaxHp)]
    [InlineData((int)NetherCombatMetricKind.TakenDamage)]
    [InlineData((int)NetherCombatMetricKind.Resistance)]
    [InlineData((int)NetherCombatMetricKind.ElementDamage)]
    [InlineData((int)NetherCombatMetricKind.CriticalProbability)]
    [InlineData((int)NetherCombatMetricKind.ContinuousAttackProbability)]
    [InlineData((int)NetherCombatMetricKind.DamageModifier)]
    public void Every_supported_combat_metric_requires_its_native_parameter_relationship(
        int metricValue
    )
    {
        NetherCombatMetricKind metric = (NetherCombatMetricKind)metricValue;
        NetherNativeBuffWindow candidate = Window(
            codeId: 60,
            buffType: 10,
            value: 100,
            start: 0,
            duration: 10,
            coexistence: NetherStrategyBuffCoexistenceKind.Allow
        ) with { Metric = metric };
        var valuation = new NetherNativePortfolioValuation();

        NetherNativePortfolioValue known = valuation.Evaluate(
            new NetherNativePortfolioTimelineInput([], [candidate], BossDurationSeconds: 10)
        );
        NetherNativePortfolioValue missing = valuation.Evaluate(
            new NetherNativePortfolioTimelineInput(
                [],
                [candidate with { MetricInputsKnown = false }],
                BossDurationSeconds: 10
            )
        );

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, known.Kind);
        Assert.Equal(1_000, Assert.Single(known.Exposures).MarginalPermilleSeconds);
        Assert.Equal(NetherCombatValueEvidenceKind.ReachableUnquantified, missing.Kind);
    }

    [Fact]
    public void Comparison_input_values_complete_retained_before_and_after_portfolios()
    {
        NetherNativeBuffWindow retained = Window(
            codeId: 70,
            buffType: 10,
            value: 100,
            start: 0,
            duration: 10,
            coexistence: NetherStrategyBuffCoexistenceKind.Allow
        );
        NetherNativeBuffWindow removed = retained with { CodeId = 71, ValuePermille = 200 };
        NetherNativeBuffWindow candidate = retained with { CodeId = 72, ValuePermille = 300 };

        NetherNativePortfolioValue value = new NetherNativePortfolioValuation().EvaluateComparison(
            new NetherNativePortfolioComparisonInput(
                [retained, removed],
                [retained, candidate],
                BossDurationSeconds: 10
            )
        );

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        Assert.Equal(1_000, Assert.Single(value.Exposures).MarginalPermilleSeconds);
    }

    private static NetherNativeBuffWindow Window(
        long codeId,
        int buffType,
        int value,
        int start,
        int duration,
        NetherStrategyBuffCoexistenceKind coexistence,
        int limit = 0
    ) => new(
        codeId,
        RecipientCharacterId: 100,
        new NetherStrategyBuffType(buffType),
        NetherStrategyBuffEffectKind.Buff,
        coexistence,
        NetherCombatMetricKind.Attack,
        value,
        start,
        duration
    )
    {
        PositiveCumulativeLimit = limit,
    };

    private static NetherCharacterEffectiveHpEvidence EffectiveHp(
        long id,
        NetherPartyPosition position,
        long before,
        long after
    ) => new(id, position, before, after, IsKnown: true);
}
