using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherCodePolicyTests
{
    [Fact]
    public void Effective_category_counts_count_cards_not_ability_level_power_or_possession_amount()
    {
        NetherCodeEffectiveLevels levels = NetherCodePolicy.CalculateEffectiveLevels(
            [
                Code(1, NetherCodeFamily.Safe, abilityLevel: 500, power: 900, possessionAmount: 999),
                Code(2, NetherCodeFamily.Risk, abilityLevel: 1, power: 1, possessionAmount: 1),
                Code(3, NetherCodeFamily.Rush, abilityLevel: 20),
                Code(4, NetherCodeFamily.Rush, abilityLevel: 1),
                Code(5, NetherCodeFamily.Impact, abilityLevel: 1),
            ]
        );

        Assert.Equal(0, levels.Safe);
        Assert.Equal(0, levels.Risk);
        Assert.Equal(1, levels.Rush);
        Assert.Equal(0, levels.Impact);
    }

    [Fact]
    public void Risk_family_is_not_globally_rejected()
    {
        NetherCodeDecision decision = Decide(
            Portfolio(),
            Candidate(40024, NetherCodeFamily.Risk, power: 500)
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(40024, decision.SelectedCodeId);
    }

    [Fact]
    public void Historical_code_ids_have_no_priority_override()
    {
        NetherCodeDecision decision = Decide(
            Portfolio(),
            Candidate(30024, NetherCodeFamily.Safe, power: 1, coverage: 1),
            Candidate(30025, NetherCodeFamily.Safe, power: 100, coverage: 1)
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(30025, decision.SelectedCodeId);
    }

    [Fact]
    public void Safe_and_risk_are_ranked_as_peer_families_when_evidence_is_equal()
    {
        NetherCodeDecision decision = Decide(
            Portfolio(),
            Candidate(20, NetherCodeFamily.Safe, power: 5),
            Candidate(10, NetherCodeFamily.Risk, power: 5)
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(10, decision.SelectedCodeId);
    }

    [Fact]
    public void Auto_lane_ignores_offer_composition_and_stale_lock_but_uses_proven_current_party_coverage()
    {
        NetherCodeDecision first = Decide(
            Portfolio(),
            Candidate(31001, NetherCodeFamily.Rush, coverage: 5),
            Candidate(32001, NetherCodeFamily.Impact, coverage: 1)
        );
        NetherCodeDecision second = Decide(
            Portfolio(
                lockedLane: NetherCombatLane.Rush,
                current: [Code(32000, NetherCodeFamily.Impact, coverage: 5)]
            ),
            Candidate(31002, NetherCodeFamily.Rush, coverage: 1),
            Candidate(32002, NetherCodeFamily.Impact, coverage: 99)
        );

        Assert.Equal(NetherCombatLane.Auto, first.LockedLane);
        Assert.Equal(31001, first.SelectedCodeId);
        Assert.Equal(NetherCombatLane.Impact, second.LockedLane);
        Assert.Equal(32002, second.SelectedCodeId);
    }

    [Fact]
    public void Explicit_lane_beats_static_power_from_the_opposing_lane()
    {
        NetherCodeDecision decision = Decide(
            Portfolio(),
            NetherCombatLane.Impact,
            Candidate(1, NetherCodeFamily.Rush, power: 9999),
            Candidate(2, NetherCodeFamily.Impact, power: 1)
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(2, decision.SelectedCodeId);
        Assert.Equal(NetherCombatLane.Impact, decision.LockedLane);
    }

    [Fact]
    public void Full_portfolio_accepts_only_an_evidence_backed_upgrade()
    {
        NetherCodeDecision upgrade = Decide(
            Portfolio(capacity: 1, current: [Code(1, NetherCodeFamily.Safe, power: 10, coverage: 1)]),
            Candidate(2, NetherCodeFamily.Risk, power: 100, coverage: 1)
        );
        NetherCodeDecision downgrade = Decide(
            Portfolio(capacity: 1, current: [Code(3, NetherCodeFamily.Safe, power: 100, coverage: 1)]),
            Candidate(4, NetherCodeFamily.Risk, power: 10, coverage: 1)
        );

        Assert.Equal(NetherCodeDecisionKind.Select, upgrade.Kind);
        Assert.Equal(1, upgrade.RemoveCodeId);
        Assert.Equal(NetherCodeDecisionKind.Keep, downgrade.Kind);
    }

    [Fact]
    public void Unknown_native_display_coverage_is_not_ranked_as_zero()
    {
        NetherCodeDecision decision = Decide(
            Portfolio(
                capacity: 1,
                current:
                [
                    Code(
                        1,
                        NetherCodeFamily.Safe,
                        power: 10,
                        coverageKnown: false
                    ),
                ]
            ),
            Candidate(2, NetherCodeFamily.Risk, power: 999, coverage: 99)
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
    }

    [Fact]
    public void Auto_lane_stays_neutral_when_any_current_combat_lane_coverage_is_unknown()
    {
        NetherCodeDecision decision = Decide(
            Portfolio(
                current:
                [
                    Code(1, NetherCodeFamily.Rush, coverage: 5),
                    Code(2, NetherCodeFamily.Impact, coverageKnown: false),
                ]
            ),
            Candidate(3, NetherCodeFamily.Safe, power: 1, coverage: 1)
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(NetherCombatLane.Auto, decision.LockedLane);
    }

    [Fact]
    public void Reload_reserve_is_honored_when_no_new_candidate_exists()
    {
        NetherCodeState existing = Code(1, NetherCodeFamily.Safe);
        NetherCodeDecision reload = Decide(
            Portfolio(reloadCount: 2, current: [existing]),
            Candidate(1, NetherCodeFamily.Safe)
        );
        NetherCodeDecision keep = Decide(
            Portfolio(reloadCount: 1, current: [existing]),
            Candidate(1, NetherCodeFamily.Safe)
        );

        Assert.Equal(NetherCodeDecisionKind.Reload, reload.Kind);
        Assert.Equal(NetherCodeDecisionKind.Keep, keep.Kind);
    }

    [Fact]
    public void Duplicate_offer_is_not_assigned_an_unproven_stack_value_even_when_inventory_has_space()
    {
        NetherCodeState existing = Code(1, NetherCodeFamily.Safe, possessionAmount: 2);

        NetherCodeDecision decision = Decide(
            Portfolio(capacity: 5, reloadCount: 1, current: [existing]),
            Candidate(1, NetherCodeFamily.Safe, power: 999, coverage: 99)
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
        Assert.Equal(0, decision.SelectedCodeId);
    }

    [Fact]
    public void Unknown_effect_semantics_do_not_erase_a_proven_category_card()
    {
        NetherCodeCandidate candidate = Candidate(12, NetherCodeFamily.Impact) with
        {
            EffectSemanticsKnown = false,
            MasterEffectType = (NetherCodeMasterEffectType)12,
        };

        NetherCodeDecision decision = Decide(Portfolio(), candidate);

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(12, decision.SelectedCodeId);
    }

    [Fact]
    public void Missing_master_over_capacity_or_unknown_family_pauses()
    {
        NetherCodeDecision missingMaster = Decide(
            Portfolio(masterComplete: false),
            Candidate(1, NetherCodeFamily.Safe)
        );
        NetherCodeDecision overCapacity = Decide(
            Portfolio(capacity: 1, current: [Code(1, NetherCodeFamily.Rush), Code(2, NetherCodeFamily.Impact)]),
            Candidate(3, NetherCodeFamily.Safe)
        );
        NetherCodeDecision unknown = Decide(
            Portfolio(),
            Candidate(4, NetherCodeFamily.Unknown)
        );

        Assert.Equal(NetherPauseReason.UnknownMasterData, missingMaster.PauseReason);
        Assert.Equal(NetherPauseReason.UnknownMasterData, overCapacity.PauseReason);
        Assert.Equal(NetherPauseReason.UnknownMasterData, unknown.PauseReason);
    }

    private static NetherCodeDecision Decide(
        NetherCodePortfolio portfolio,
        params NetherCodeCandidate[] candidates
    ) => Decide(portfolio, NetherCombatLane.Auto, candidates);

    private static NetherCodeDecision Decide(
        NetherCodePortfolio portfolio,
        NetherCombatLane lane,
        params NetherCodeCandidate[] candidates
    ) => new NetherCodePolicy().Decide(
        portfolio,
        candidates,
        new NetherAutoClimbSettings { CombatLane = lane, CodeReloadReserve = 1 }
    );

    private static NetherCodePortfolio Portfolio(
        int capacity = 5,
        int reloadCount = 1,
        bool masterComplete = true,
        NetherCombatLane? lockedLane = null,
        IReadOnlyList<NetherCodeState>? current = null
    ) => new()
    {
        Capacity = capacity,
        ReloadCount = reloadCount,
        IsMasterComplete = masterComplete,
        LockedLane = lockedLane,
        CurrentCodes = current ?? [],
    };

    private static NetherCodeState Code(
        long id,
        NetherCodeFamily family,
        int abilityLevel = 1,
        int rarity = 0,
        int power = 0,
        int coverage = 0,
        int possessionAmount = 1,
        bool coverageKnown = true
    ) => new(id, family, abilityLevel)
    {
        Category = Category(family),
        Rarity = rarity,
        Power = power,
        PossessionAmount = possessionAmount,
        PartyCoverageKnown = coverageKnown,
        PartyCoverage = coverage,
    };

    private static NetherCodeCandidate Candidate(
        long id,
        NetherCodeFamily family,
        int abilityLevel = 1,
        int rarity = 0,
        int power = 0,
        int coverage = 0,
        bool coverageKnown = true
    ) => new(id, family, abilityLevel)
    {
        Category = Category(family),
        Rarity = rarity,
        Power = power,
        PartyCoverageKnown = coverageKnown,
        PartyCoverage = coverage,
    };

    private static NetherCodeCategory Category(NetherCodeFamily family) => family switch
    {
        NetherCodeFamily.Rush => NetherCodeCategory.Rush,
        NetherCodeFamily.Impact => NetherCodeCategory.Impact,
        NetherCodeFamily.Safe => NetherCodeCategory.Safe,
        NetherCodeFamily.Risk => NetherCodeCategory.Risk,
        _ => NetherCodeCategory.Unknown,
    };
}
