#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherRouteHorizonSafetyPolicyTests
{
    [Fact]
    public void Complete_visible_branch_projects_each_combat_from_its_projected_start_state()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300: NetherData.ErosionPoint is the
        // authoritative run value; NetherCharacterEntity.current_hp_ratio feeds
        // NetherPartyCharacterModel.HpRatio; clear-battle request/response carries exact
        // character HP settlement.  Route policy must therefore retain each projected start.
        NetherErosionModifier[] modifiers =
        [
            new(NetherErosionOperation.Addition, amount: 2),
        ];
        NetherRouteHorizonSafetyEvaluation result = Evaluate(
            erosion: 50,
            Step(2, NetherFloorNodeType.Battle, erosionDelta: 5, modifiers: modifiers),
            Step(3, NetherFloorNodeType.Boss, erosionDelta: 5, modifiers: modifiers, terminal: true)
        );

        Assert.True(result.IsEligible, result.RejectionDetail);
        Assert.False(result.RequiresUserPause);
        Assert.Equal(64, result.FinalErosion);
        Assert.Equal(64, result.PeakErosion);
        Assert.Equal(800, result.MinimumActiveCharacterHpPermille);
        Assert.Equal(50, result.Steps[0].StartErosion);
        Assert.Equal(57, result.Steps[0].ProjectedErosion);
        Assert.Equal(57, result.Steps[1].StartErosion);
    }

    [Fact]
    public void Erosion_seventy_is_eligible_only_with_a_confirmed_recovery_route()
    {
        NetherRouteHorizonSafetyEvaluation recovered = Evaluate(
            erosion: 70,
            Step(2, NetherFloorNodeType.Recovery, erosionDelta: -20, recovery: true),
            Step(3, NetherFloorNodeType.Boss, erosionDelta: 5, terminal: true)
        );
        NetherRouteHorizonSafetyEvaluation stranded = Evaluate(
            erosion: 70,
            Step(3, NetherFloorNodeType.Boss, erosionDelta: 5, terminal: true)
        );

        Assert.True(recovered.IsEligible, recovered.RejectionDetail);
        Assert.True(recovered.HasConfirmedRecoveryToOperatingBand);
        Assert.False(stranded.IsEligible);
        Assert.True(stranded.RequiresUserPause);
        Assert.Equal("erosion-70-without-confirmed-recovery", stranded.RejectionDetail);
    }

    [Fact]
    public void Transient_above_seventy_and_necessary_combat_are_legal_on_a_certain_recovery_branch()
    {
        NetherRouteHorizonSafetyEvaluation result = Evaluate(
            erosion: 72,
            Step(2, NetherFloorNodeType.Battle, erosionDelta: 5, necessaryCombat: true),
            Step(3, NetherFloorNodeType.Recovery, erosionDelta: -20, recovery: true),
            Step(4, NetherFloorNodeType.Boss, erosionDelta: 5, terminal: true)
        );

        Assert.True(result.IsEligible, result.RejectionDetail);
        Assert.Equal(77, result.PeakErosion);
        Assert.Equal(62, result.FinalErosion);
        Assert.True(result.HasConfirmedRecoveryToOperatingBand);
    }

    [Fact]
    public void Random_or_missing_future_outcome_never_proves_recovery()
    {
        NetherRouteHorizonSafetyEvaluation randomRecovery = Evaluate(
            erosion: 70,
            Step(2, NetherFloorNodeType.Recovery, erosionDelta: -20, recovery: true, certain: false),
            Step(3, NetherFloorNodeType.Boss, erosionDelta: 5, terminal: true)
        );
        NetherRouteHorizonSafetyEvaluation missingHorizon = new NetherRouteHorizonSafetyPolicy().Evaluate(
            Input(50, [Step(2, NetherFloorNodeType.Battle, erosionDelta: 5)]) with
            {
                IsVisibleHorizonComplete = false,
            }
        );

        Assert.False(randomRecovery.IsEligible);
        Assert.Equal("unknown-route-outcome:2", randomRecovery.RejectionDetail);
        Assert.False(missingHorizon.IsEligible);
        Assert.Equal("visible-horizon-incomplete", missingHorizon.RejectionDetail);
    }

    [Fact]
    public void Lethal_erosion_is_a_hard_rejection_even_for_terminal_boss()
    {
        NetherRouteHorizonSafetyEvaluation result = Evaluate(
            erosion: 95,
            Step(3, NetherFloorNodeType.Boss, erosionDelta: 5, terminal: true)
        );

        Assert.False(result.IsEligible);
        Assert.Equal("lethal-erosion:3", result.RejectionDetail);
        Assert.Equal(100, result.PeakErosion);
    }

    [Fact]
    public void Ordinary_hp_cost_requires_every_living_character_to_survive()
    {
        NetherRouteHorizonSafetyEvaluation result = new NetherRouteHorizonSafetyPolicy().Evaluate(
            Input(50, [Step(2, NetherFloorNodeType.Event, hpDelta: -250), Step(3, NetherFloorNodeType.Boss, 5, terminal: true)]) with
            {
                ActiveCharacterHpPermille = new[] { 800, 200 },
            }
        );

        Assert.False(result.IsEligible);
        Assert.Equal("ordinary-hp-cost-lethal:2", result.RejectionDetail);
    }

    [Fact]
    public void Treasure_and_hp_paid_key_exceptions_require_group_survival_without_relaxing_ordinary_costs()
    {
        NetherRouteHorizonStep treasure = Step(2, NetherFloorNodeType.Treasure, hpDelta: -250) with
        {
            HpRule = NetherRouteHpRule.TreasureGroupSurvival,
        };
        NetherRouteHorizonSafetyEvaluation survives = new NetherRouteHorizonSafetyPolicy().Evaluate(
            Input(50, [treasure, Step(3, NetherFloorNodeType.Boss, 5, terminal: true)]) with
            {
                ActiveCharacterHpPermille = new[] { 800, 200 },
            }
        );
        NetherRouteHorizonSafetyEvaluation partyLethal = new NetherRouteHorizonSafetyPolicy().Evaluate(
            Input(50, [treasure, Step(3, NetherFloorNodeType.Boss, 5, terminal: true)]) with
            {
                ActiveCharacterHpPermille = new[] { 200, 100 },
            }
        );

        Assert.True(survives.IsEligible, survives.RejectionDetail);
        Assert.False(partyLethal.IsEligible);
        Assert.Equal("treasure-hp-cost-party-lethal:2", partyLethal.RejectionDetail);
    }

    [Fact]
    public void Confirmed_combat_without_exact_preentry_hp_is_rejected_locally()
    {
        NetherRouteHorizonStep combat = Step(2, NetherFloorNodeType.Battle, 5) with
        {
            HasExactPreEntryHpEvidence = false,
        };

        NetherRouteHorizonSafetyEvaluation result = Evaluate(
            50,
            combat,
            Step(3, NetherFloorNodeType.Boss, 5, terminal: true)
        );

        Assert.False(result.IsEligible);
        Assert.Equal("combat-preentry-hp-unavailable:2", result.RejectionDetail);
    }

    [Fact]
    public void Dedicated_risk_research_exposes_band_preference_without_authorizing_payoff_driven_erosion()
    {
        NetherRouteHorizonSafetyEvaluation result = new NetherRouteHorizonSafetyPolicy().Evaluate(
            Input(50, [Step(3, NetherFloorNodeType.Boss, erosionDelta: 5, terminal: true)]) with
            {
                StrategyMode = NetherStrategyMode.Research,
                PrimaryResearchFamily = NetherCodeFamily.Risk,
            }
        );

        Assert.True(result.IsEligible, result.RejectionDetail);
        Assert.True(result.IsDedicatedRiskResearch);
        Assert.True(result.FinalErosionIsInPreferredRiskBand);
        Assert.False(result.MayRaiseErosionForRiskPayoff);
    }

    private static NetherRouteHorizonSafetyEvaluation Evaluate(
        int erosion,
        params NetherRouteHorizonStep[] steps
    ) => new NetherRouteHorizonSafetyPolicy().Evaluate(Input(erosion, steps));

    private static NetherRouteHorizonSafetyInput Input(
        int erosion,
        IReadOnlyList<NetherRouteHorizonStep> steps
    ) => new(
        CurrentErosion: erosion,
        ActiveCharacterHpPermille: new[] { 900, 800 },
        Steps: steps,
        SoftErosionLimit: 70,
        HardErosionLimit: 100
    )
    {
        IsVisibleHorizonComplete = true,
        StrategyMode = NetherStrategyMode.Equipment,
    };

    private static NetherRouteHorizonStep Step(
        long id,
        NetherFloorNodeType kind,
        int erosionDelta = 0,
        int hpDelta = 0,
        IReadOnlyList<NetherErosionModifier>? modifiers = null,
        bool recovery = false,
        bool necessaryCombat = false,
        bool terminal = false,
        bool certain = true
    ) => new(
        NodeId: id,
        NodeType: kind,
        BaseErosionDelta: erosionDelta,
        HpDeltaPermille: hpDelta,
        ErosionModifiers: modifiers ?? Array.Empty<NetherErosionModifier>()
    )
    {
        IsOutcomeCertain = certain,
        IsConfirmedRecovery = recovery,
        IsNecessaryCombat = necessaryCombat,
        IsTerminalBoss = terminal,
        HpRule = NetherRouteHpRule.OrdinaryAllLivingSurvive,
    };
}
