#nullable enable

using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

/// <summary>
/// Ticket 16/17 characterization seam. Each test is deliberately expressed against the immutable
/// mapper, route planner, code policy, and bounded audit stream rather than controller internals.
/// Fresh native evidence for this RED is recorded in
/// docs/agents/evidence-backed-strategy-modes-16-17-evidence.md under
/// task16-17-fresh-20260819-a.
/// </summary>
public sealed class NetherStrategyModes1617Tests
{
    [Fact]
    public void Evidence_records_mode_active_research_target_owner_and_contract_version()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(Identity(snapshot), snapshot)
            {
                EvidenceVersion = NetherStrategyEvidenceContract.CurrentVersion,
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Rush,
                ResearchSecondaryFamily = NetherCodeFamily.Impact,
                Research = Research(primaryProjectionKnown: true),
            }
        );

        Assert.True(mapped.IsMapped, mapped.Detail);
        NetherStrategyEvidencePackage package = mapped.Package!;
        Assert.Equal(NetherStrategyEvidenceContract.CurrentVersion, package.EvidenceVersion);
        Assert.Equal(NetherStrategyMode.Research, package.EvidenceAudit.Mode);
        Assert.Equal(NetherCodeFamily.Rush, package.EvidenceAudit.ActiveResearchFamily);
        Assert.Equal(NetherResearchTargetState.Active, package.EvidenceAudit.ResearchTargetState);
        Assert.Equal(package.Identity.ControllerOwnerGeneration, package.EvidenceAudit.OwnerGeneration);
        Assert.Equal(package.Identity.SnapshotFingerprint, package.EvidenceAudit.SnapshotFingerprint);
    }

    [Fact]
    public void Unknown_native_settlement_projection_keeps_primary_active_with_typed_fallback_audit()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(Identity(snapshot), snapshot)
            {
                StrategyMode = NetherStrategyMode.Research,
                ResearchPrimaryFamily = NetherCodeFamily.Rush,
                ResearchSecondaryFamily = NetherCodeFamily.Impact,
                Research = Research(primaryProjectionKnown: false),
            }
        );

        Assert.True(mapped.IsMapped, mapped.Detail);
        NetherStrategyEvidenceAudit audit = mapped.Package!.EvidenceAudit;
        Assert.Equal(NetherResearchTargetState.Active, audit.ResearchTargetState);
        Assert.Equal(NetherCodeFamily.Rush, audit.ActiveResearchFamily);
        Assert.Equal(
            NetherStrategyUnknownReasonCode.ResearchCompletionUnknown,
            audit.UnknownReasonCode
        );
        Assert.Equal("native-settlement-not-known", audit.UnknownReason);
    }

    [Fact]
    public void Stale_evidence_version_is_rejected_locally_with_a_typed_reason()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(Identity(snapshot), snapshot)
            {
                EvidenceVersion = NetherStrategyEvidenceContract.CurrentVersion + 1,
            }
        );

        Assert.False(mapped.IsMapped);
        Assert.Equal(
            NetherStrategyUnknownReasonCode.EvidenceVersionMismatch,
            mapped.UnknownReasonCode
        );

        NetherStrategyEvidenceAcceptanceDecision accepted = NetherStrategyEvidenceAcceptance.Evaluate(
            new NetherStrategyEvidencePackage
            {
                Identity = Identity(snapshot),
                EvidenceVersion = NetherStrategyEvidenceContract.CurrentVersion + 1,
                EvidenceAudit = new NetherStrategyEvidenceAudit
                {
                    EvidenceVersion = NetherStrategyEvidenceContract.CurrentVersion + 1,
                },
            },
            currentRuntimeGeneration: 8,
            currentControllerOwnerGeneration: 8,
            currentEnteredSubsceneGeneration: 8,
            currentAuthoritativeSnapshot: snapshot.Fingerprint
        );

        Assert.False(accepted.IsAccepted);
        Assert.Equal(
            NetherStrategyUnknownReasonCode.EvidenceVersionMismatch,
            accepted.UnknownReasonCode
        );
    }

    [Fact]
    public void Route_audit_retains_first_hard_gate_and_vector_semantic_tier()
    {
        NetherFloorNode current = Node(1, 1, NetherFloorNodeType.Recovery);
        NetherFloorNode locked = Node(2, 2, NetherFloorNodeType.Recovery, 1) with
        {
            IsUnlocked = false,
        };
        NetherFloorNode boss = Node(3, 3, NetherFloorNodeType.Boss, 2);
        NetherSnapshot snapshot = Snapshot() with
        {
            CurrentFloorId = 1,
            CurrentNodeId = 1,
            Floors = new[] { current, locked, boss },
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            snapshot,
            new NetherRouteSafetyContext
            {
                AllowLegacyComparatorCompatibility = true,
            }
        );

        NetherRouteCandidateAudit audit = Assert.Single(plan.Audit);
        Assert.Equal(NetherRouteCandidateHardGate.Locked, audit.FirstFailingHardGate);
        Assert.Equal(NetherRouteSemanticTier.None, audit.SemanticTier);

        NetherRouteEncounterVector vector = new()
        {
            ImmediateTerminalBossCount = 1,
            RedRankFiveTreasureCount = 5,
        };
        Assert.Equal(NetherRouteSemanticTier.ImmediateTerminalBoss, vector.HighestSemanticTier(true));

        NetherRouteEncounterVector unknown = NetherRouteEncounterVectorPolicy.Build(
            snapshot,
            null!,
            null!,
            null!
        );
        Assert.False(unknown.IsKnown);
        Assert.Equal(NetherStrategyUnknownReasonCode.RouteVectorInputUnavailable, unknown.UnknownReasonCode);
    }

    [Fact]
    public void Selected_route_retains_its_safety_projection_and_tie_break_contract()
    {
        NetherFloorNode current = Node(1, 1, NetherFloorNodeType.Recovery);
        NetherFloorNode recovery = Node(2, 2, NetherFloorNodeType.Recovery, 1);
        NetherFloorNode boss = Node(3, 3, NetherFloorNodeType.Boss, 2);
        NetherSnapshot snapshot = Snapshot() with
        {
            CurrentFloorId = 1,
            CurrentNodeId = 1,
            Floors = new[] { current, recovery, boss },
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            snapshot,
            new NetherRouteSafetyContext
            {
                AllowLegacyComparatorCompatibility = true,
            }
        );

        Assert.Equal(2, plan.SelectedNode!.NodeId);
        NetherRouteSelectionEvidence evidence = Assert.IsType<NetherRouteSelectionEvidence>(
            plan.SelectionEvidence
        );
        Assert.False(evidence.SemanticVectorKnown);
        Assert.Equal(NetherRouteSemanticTier.None, evidence.SelectedSemanticTier);
        Assert.False(evidence.SafetyProjectionKnown);
        Assert.Equal(0, evidence.ProcurementCommitmentCount);
        Assert.Equal(
            "legacy-safety>objective>erosion>hp>coordinates",
            evidence.TieBreakOrder
        );
    }

    [Fact]
    public void Code_candidate_audit_reports_first_unknown_hard_gate_and_never_uses_display_power()
    {
        var candidate = new NetherCodeCandidate(1001, NetherCodeFamily.Rush, 1)
        {
            Power = 999999,
        };
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            new NetherCodePortfolio
            {
                IsMasterComplete = true,
                Capacity = 1,
            },
            new[] { candidate },
            new NetherAutoClimbSettings { CodeReloadReserve = 1 },
            new NetherCodePolicyEvidence()
        );

        NetherCodeCandidateAudit audit = Assert.Single(decision.CandidateAudits);
        Assert.Equal(NetherCodeCandidateHardGate.NativeMechanics, audit.FirstFailingHardGate);
        Assert.Equal(NetherStrategyUnknownReasonCode.NativeMechanicsUnavailable, audit.UnknownReasonCode);
        Assert.False(decision.DisplayPowerUsedForDecision);
    }

    [Fact]
    public void Duplicate_code_options_pause_with_candidate_local_ambiguity()
    {
        NetherCodeCandidate first = new(1001, NetherCodeFamily.Rush, 1);
        NetherCodeCandidate duplicate = new(1001, NetherCodeFamily.Rush, 1)
        {
            Power = 999999,
        };
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            new NetherCodePortfolio
            {
                IsMasterComplete = true,
                Capacity = 1,
            },
            new[] { first, duplicate },
            new NetherAutoClimbSettings { CodeReloadReserve = 1 },
            new NetherCodePolicyEvidence()
        );

        Assert.Equal(NetherCodeDecisionKind.Pause, decision.Kind);
        Assert.Equal(
            NetherCodeCandidateHardGate.AmbiguousCandidateIdentity,
            decision.FirstFailingHardGate
        );
        Assert.Equal(
            NetherStrategyUnknownReasonCode.AmbiguousCandidateIdentity,
            decision.UnknownReasonCode
        );
        Assert.Equal(2, decision.CandidateAudits.Count);
        Assert.All(
            decision.CandidateAudits,
            audit => Assert.Equal(
                NetherCodeCandidateHardGate.AmbiguousCandidateIdentity,
                audit.FirstFailingHardGate
            )
        );
    }

    [Fact]
    public void Invalid_code_portfolio_emits_exactly_one_audit_for_each_presented_candidate()
    {
        NetherCodeCandidate invalid = new(1001, NetherCodeFamily.Rush, 1)
        {
            IsKnown = false,
        };
        NetherCodeCandidate valid = new(1002, NetherCodeFamily.Impact, 1);
        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            new NetherCodePortfolio
            {
                IsMasterComplete = false,
                Capacity = 1,
            },
            new[] { invalid, valid },
            new NetherAutoClimbSettings { CodeReloadReserve = 1 },
            new NetherCodePolicyEvidence()
        );

        Assert.Equal(NetherCodeDecisionKind.Pause, decision.Kind);
        Assert.Equal(2, decision.CandidateAudits.Count);
        Assert.Equal(new long[] { 1001, 1002 }, decision.CandidateAudits.Select(audit => audit.CodeId));
        Assert.All(
            decision.CandidateAudits,
            audit =>
            {
                Assert.Equal(NetherCodeCandidateHardGate.CandidateIdentity, audit.FirstFailingHardGate);
                Assert.Equal(NetherStrategyUnknownReasonCode.CandidateIdentityInvalid, audit.UnknownReasonCode);
            }
        );
    }

    [Fact]
    public void Detailed_audit_context_identifies_strategy_target_owner_snapshot_and_vector()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidenceAudit audit = new()
        {
            EvidenceVersion = NetherStrategyEvidenceContract.CurrentVersion,
            Mode = NetherStrategyMode.Research,
            PrimaryResearchFamily = NetherCodeFamily.Rush,
            SecondaryResearchFamily = NetherCodeFamily.Impact,
            ActiveResearchFamily = NetherCodeFamily.Rush,
            ResearchTargetState = NetherResearchTargetState.Active,
            OwnerGeneration = 8,
            EnteredSubsceneGeneration = 9,
            SnapshotFingerprint = snapshot.Fingerprint,
        };

        IReadOnlyList<NetherDetailedAuditField> fields = NetherStrategyAuditFormatting.Context(
            audit,
            snapshot.Fingerprint
        );

        Assert.Contains(fields, field => field.Name == "evidenceVersion" && field.Value == "1");
        Assert.Contains(fields, field => field.Name == "mode" && field.Value == "Research");
        Assert.Contains(fields, field => field.Name == "activeResearchTarget" && field.Value == "Rush");
        Assert.Contains(fields, field => field.Name == "ownerGeneration" && field.Value == "8");
        Assert.Contains(fields, field => field.Name == "enteredSubsceneGeneration" && field.Value == "9");
        Assert.Contains(fields, field => field.Name == "snapshotFingerprint");

        string vector = NetherStrategyAuditFormatting.SemanticVector(
            new NetherRouteEncounterVector
            {
                ImmediateTerminalBossCount = 1,
                RedRankFiveTreasureCount = 2,
                NormalBattleCount = 3,
            }
        );
        Assert.Contains("boss=1", vector);
        Assert.Contains("red5=2", vector);
        Assert.Contains("normal=3", vector);
    }

    [Fact]
    public void Detailed_decision_and_transition_families_are_bounded_and_poll_deduplicated()
    {
        var entries = new List<string>();
        var logger = new NetherDetailedAuditLogger(entries.Add);
        var gate = new NetherDiagnosticTransitionGate();

        Assert.True(gate.ShouldEmit("decision", "select:1001"));
        logger.Emit(
            enabled: true,
            NetherDetailedAuditKind.Decision,
            "code-policy:1001",
            new NetherDetailedAuditField("tier", "retained-portfolio-strict-improvement")
        );
        Assert.False(gate.ShouldEmit("decision", "select:1001"));
        Assert.True(gate.ShouldEmit("decision", "pause:unknown-native"));
        logger.Emit(
            enabled: true,
            NetherDetailedAuditKind.Transition,
            "state:pause",
            new NetherDetailedAuditField("reason", "unknown-native")
        );

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, entry => entry.Contains("audit=decision"));
        Assert.Contains(entries, entry => entry.Contains("audit=transition"));
    }

    [Fact]
    public void Mixed_duplicate_code_options_emit_one_audit_for_every_presented_candidate()
    {
        NetherCodeCandidate first = new(1001, NetherCodeFamily.Rush, 1);
        NetherCodeCandidate duplicate = new(1001, NetherCodeFamily.Rush, 1)
        {
            Power = 999999,
        };
        NetherCodeCandidate unique = new(1002, NetherCodeFamily.Impact, 1);

        NetherCodeDecision decision = new NetherCodePolicy().Decide(
            new NetherCodePortfolio
            {
                IsMasterComplete = true,
                Capacity = 1,
            },
            new[] { first, duplicate, unique },
            new NetherAutoClimbSettings { CodeReloadReserve = 1 },
            new NetherCodePolicyEvidence()
        );

        Assert.Equal(NetherCodeDecisionKind.Pause, decision.Kind);
        Assert.Equal(3, decision.CandidateAudits.Count);
        Assert.Equal(
            new long[] { 1001, 1001, 1002 },
            decision.CandidateAudits.Select(audit => audit.CodeId)
        );
        Assert.All(
            decision.CandidateAudits,
            audit => Assert.Equal(
                NetherCodeCandidateHardGate.AmbiguousCandidateIdentity,
                audit.FirstFailingHardGate
            )
        );
    }

    private static NetherSnapshot Snapshot() => new()
    {
        Status = NetherSessionStatus.Play,
        NetherId = 11,
        MapId = 22,
        CurrentFloorId = 33,
        CurrentNodeId = 44,
        FloorLevel = 0,
        FloorIndex = 0,
        MasterMaxFloorLevel = 70,
        AuthoritativeBossFloorLevels = new[] { 70 },
        CharacterHpHash = "party",
        CodeHash = "codes",
        MapHash = "map",
    };

    private static NetherStrategyEvidenceIdentity Identity(NetherSnapshot snapshot) => new(
        RuntimeGeneration: 8,
        ControllerOwnerGeneration: 8,
        EnteredSubsceneGeneration: 8,
        SnapshotFingerprint: snapshot.Fingerprint
    );

    private static IReadOnlyList<NetherStrategyResearchFamilyState> Research(
        bool primaryProjectionKnown
    ) => new[]
    {
        new NetherStrategyResearchFamilyState(NetherCodeFamily.Rush, 100, 100, 15)
        {
            IsProjectedNormalSettlementKnown = primaryProjectionKnown,
            ProjectionUnknownReason = primaryProjectionKnown
                ? string.Empty
                : "native-settlement-not-known",
        },
        new NetherStrategyResearchFamilyState(NetherCodeFamily.Impact, 0, 0, 15),
        new NetherStrategyResearchFamilyState(NetherCodeFamily.Safe, 0, 0, 15),
        new NetherStrategyResearchFamilyState(NetherCodeFamily.Risk, 0, 0, 15),
    };

    private static NetherFloorNode Node(
        long id,
        int level,
        NetherFloorNodeType type,
        params long[] previousIds
    ) => new(id, level, (int)id, type)
    {
        IsUnlocked = true,
        PreviousFloorIds = previousIds,
    };
}
