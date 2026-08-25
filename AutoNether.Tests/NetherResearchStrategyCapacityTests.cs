using System.Collections.Generic;
using System.Linq;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherResearchStrategyCapacityTests
{
    [Fact]
    public void Research_prefers_secondary_after_primary_objective_is_complete()
    {
        NetherCodeCandidate primary = Candidate(996001, NetherCodeFamily.Rush, power: 99_999);
        NetherCodeCandidate secondary = Candidate(996002, NetherCodeFamily.Safe, power: 1);
        NetherCodePolicyEvidence evidence = Evidence(primary, secondary) with
        {
            Research = ResearchRows(
                primaryWallet: NetherResearchObjectivePolicy.CompletionPoints,
                secondaryWallet: 0
            ),
            ActiveResearchFamily = NetherCodeFamily.Safe,
        };

        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(capacity: 5),
            [primary, secondary],
            ResearchSettings(),
            evidence
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(secondary.CodeId, decision.SelectedCodeId);
        Assert.Equal(0, decision.RemoveCodeId);
        Assert.False(decision.DisplayPowerUsedForDecision);
        Assert.Contains("secondary-family", decision.Detail);
    }

    [Fact]
    public void Research_full_portfolio_keeps_without_replacement_or_reroll()
    {
        NetherCodeState heldPrimary = Code(996010, NetherCodeFamily.Rush);
        NetherCodeState heldSecondary = Code(996011, NetherCodeFamily.Safe);
        NetherCodeCandidate offeredSecondary = Candidate(996012, NetherCodeFamily.Safe);
        NetherCodePolicyEvidence evidence = Evidence(offeredSecondary) with
        {
            Research = ResearchRows(
                primaryWallet: NetherResearchObjectivePolicy.CompletionPoints,
                secondaryWallet: 0
            ),
            ActiveResearchFamily = NetherCodeFamily.Safe,
        };

        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(
                capacity: 2,
                reloadCount: 3,
                decisionEpoch: 0,
                current: [heldPrimary, heldSecondary]
            ),
            [offeredSecondary],
            ResearchSettings(),
            evidence
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
        Assert.Equal(0, decision.SelectedCodeId);
        Assert.Equal(0, decision.RemoveCodeId);
        Assert.Empty(decision.RemovableCodeIds);
        Assert.Contains("capacity-saturated", decision.Detail);
    }

    [Theory]
    [InlineData((int)NetherCodeTargetRow.Forward, (int)NetherCodeDecisionKind.Select)]
    [InlineData((int)NetherCodeTargetRow.ForwardAndAssist, (int)NetherCodeDecisionKind.Select)]
    [InlineData((int)NetherCodeTargetRow.Back, (int)NetherCodeDecisionKind.Keep)]
    [InlineData((int)NetherCodeTargetRow.BackAndAssist, (int)NetherCodeDecisionKind.Keep)]
    [InlineData((int)NetherCodeTargetRow.All, (int)NetherCodeDecisionKind.Keep)]
    public void Research_uniform_crest_grant_checks_only_exact_native_scope_recipients(
        int targetRowRaw,
        int expectedRaw
    )
    {
        NetherCodeTargetRow targetRow = (NetherCodeTargetRow)targetRowRaw;
        NetherCodeDecisionKind expected = (NetherCodeDecisionKind)expectedRaw;
        NetherCodeCandidate candidate = Candidate(996020 + (int)targetRow, NetherCodeFamily.Impact);
        NetherCodePolicyEvidence evidence = Evidence(candidate) with
        {
            MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
            {
                [candidate.CodeId] = new()
                {
                    IsKnown = true,
                    UniformCrestFamily = NetherCodeFamily.Impact,
                    UniformCrestTargetRow = targetRow,
                },
            },
            ActiveParty =
            [
                Member(1, 0, NetherPartyPosition.Forward, NetherCrestIdentity.Impact),
                Member(2, 1, NetherPartyPosition.Back, NetherCrestIdentity.Impact),
                Member(3, 2, NetherPartyPosition.Back, NetherCrestIdentity.Passion),
                Member(4, 3, NetherPartyPosition.Assist, NetherCrestIdentity.Impact),
            ],
            Research = ResearchRows(
                primaryWallet: NetherResearchObjectivePolicy.CompletionPoints,
                secondaryWallet: 0
            ),
            ActiveResearchFamily = NetherCodeFamily.Safe,
        };

        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(capacity: 5, reloadCount: 0, decisionEpoch: 1),
            [candidate],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Impact,
                ResearchSecondaryFamily = NetherCodeFamily.Safe,
            },
            evidence
        );

        Assert.Equal(expected, decision.Kind);
        NetherCodeCandidateAudit audit = Assert.Single(decision.CandidateAudits);
        Assert.Equal(
            expected == NetherCodeDecisionKind.Select
                ? NetherCodeCandidateHardGate.None
                : NetherCodeCandidateHardGate.CrestCompatibility,
            audit.FirstFailingHardGate
        );
    }

    [Theory]
    [InlineData((int)NetherCodeFamily.Rush)]
    [InlineData((int)NetherCodeFamily.Impact)]
    public void Research_rejects_both_uniform_crest_families_for_a_mixed_recipient_scope(
        int familyRaw
    )
    {
        NetherCodeFamily family = (NetherCodeFamily)familyRaw;
        NetherCodeCandidate candidate = Candidate(996100 + (int)family, family);
        NetherCodePolicyEvidence evidence = Evidence(candidate) with
        {
            MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
            {
                [candidate.CodeId] = new()
                {
                    IsKnown = true,
                    UniformCrestFamily = family,
                    UniformCrestTargetRow = NetherCodeTargetRow.Back,
                },
            },
            ActiveParty =
            [
                Member(1, 0, NetherPartyPosition.Back, NetherCrestIdentity.Passion),
                Member(2, 1, NetherPartyPosition.Back, NetherCrestIdentity.Impact),
            ],
            Research = ResearchRows(0, 0),
            ActiveResearchFamily = family,
        };

        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(capacity: 5, reloadCount: 0, decisionEpoch: 1),
            [candidate],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = family,
                ResearchSecondaryFamily = NetherCodeFamily.Unknown,
            },
            evidence
        );

        Assert.Equal(NetherCodeDecisionKind.Keep, decision.Kind);
        Assert.Equal(
            NetherCodeCandidateHardGate.CrestCompatibility,
            Assert.Single(decision.CandidateAudits).FirstFailingHardGate
        );
    }

    [Fact]
    public void Research_uses_safe_secondary_when_the_preferred_primary_has_an_incompatible_crest_scope()
    {
        NetherCodeCandidate primary = Candidate(996201, NetherCodeFamily.Impact);
        NetherCodeCandidate secondary = Candidate(996202, NetherCodeFamily.Safe);
        NetherCodePolicyEvidence evidence = Evidence(primary, secondary) with
        {
            MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
            {
                [primary.CodeId] = new()
                {
                    IsKnown = true,
                    UniformCrestFamily = NetherCodeFamily.Impact,
                    UniformCrestTargetRow = NetherCodeTargetRow.Back,
                },
                [secondary.CodeId] = new() { IsKnown = true },
            },
            ActiveParty =
            [
                Member(1, 0, NetherPartyPosition.Back, NetherCrestIdentity.Impact),
                Member(2, 1, NetherPartyPosition.Back, NetherCrestIdentity.Passion),
            ],
            Research = ResearchRows(0, 0),
            ActiveResearchFamily = NetherCodeFamily.Impact,
        };

        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            Portfolio(capacity: 5, reloadCount: 1),
            [primary, secondary],
            new NetherAutoClimbSettings
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Impact,
                ResearchSecondaryFamily = NetherCodeFamily.Safe,
            },
            evidence
        );

        Assert.Equal(NetherCodeDecisionKind.Select, decision.Kind);
        Assert.Equal(secondary.CodeId, decision.SelectedCodeId);
        Assert.Equal(
            NetherCodeCandidateHardGate.CrestCompatibility,
            Assert.Single(decision.CandidateAudits, audit => audit.CodeId == primary.CodeId)
                .FirstFailingHardGate
        );
    }

    private static NetherAutoClimbSettings ResearchSettings() => new()
    {
        StrategyMode = NetherStrategyMode.Research,
        ResearchPrimaryFamily = NetherCodeFamily.Rush,
        ResearchSecondaryFamily = NetherCodeFamily.Safe,
        CodeReloadReserve = 99,
    };

    private static NetherCodePortfolio Portfolio(
        int capacity,
        int reloadCount = 1,
        long decisionEpoch = 0,
        IReadOnlyList<NetherCodeState>? current = null
    ) => new()
    {
        Capacity = capacity,
        ReloadCount = reloadCount,
        DecisionEpoch = decisionEpoch,
        IsMasterComplete = true,
        CurrentCodes = current ?? [],
    };

    private static NetherCodePolicyEvidence Evidence(params NetherCodeCandidate[] candidates) => new()
    {
        MechanicsByCodeId = candidates.ToDictionary(
            candidate => candidate.CodeId,
            _ => new NetherCodeHardEligibilityEvidence { IsKnown = true }
        ),
        ActiveParty =
        [
            Member(1, 0, NetherPartyPosition.Forward, NetherCrestIdentity.Impact),
        ],
    };

    private static IReadOnlyList<NetherStrategyResearchFamilyState> ResearchRows(
        int primaryWallet,
        int secondaryWallet
    ) =>
    [
        ResearchRow(NetherCodeFamily.Rush, primaryWallet),
        ResearchRow(NetherCodeFamily.Impact, 0),
        ResearchRow(NetherCodeFamily.Safe, secondaryWallet),
        ResearchRow(NetherCodeFamily.Risk, 0),
    ];

    private static NetherStrategyResearchFamilyState ResearchRow(
        NetherCodeFamily family,
        int wallet
    ) => new(family, wallet, 0, 0)
    {
        IsProjectedNormalSettlementKnown = true,
    };

    private static NetherStrategyPartyMember Member(
        long id,
        int partyIndex,
        NetherPartyPosition position,
        NetherCrestIdentity crest
    ) => new(id, partyIndex, position, 1, crest, 1000, true, 1, 0);

    private static NetherCodeState Code(long id, NetherCodeFamily family) => new(id, family, 1)
    {
        Category = Category(family),
        PossessionAmount = 1,
    };

    private static NetherCodeCandidate Candidate(
        long id,
        NetherCodeFamily family,
        int power = 0
    ) => new(id, family, 1)
    {
        Category = Category(family),
        Power = power,
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
