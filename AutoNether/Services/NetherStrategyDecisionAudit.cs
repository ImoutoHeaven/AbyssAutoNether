#nullable enable

using System;
using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// Version of the immutable strategy-evidence contract. It is deliberately independent from the
/// native game build hash: the evidence ledger records the latter, while this value rejects a
/// mapper/consumer contract mismatch locally.
/// </summary>
internal static class NetherStrategyEvidenceContract
{
    public const int CurrentVersion = 1;
}

/// <summary>
/// Closed vocabulary for unknown facts that can affect a strategy decision. Human-readable detail
/// remains alongside the code for diagnosis, but policy and audit consumers do not have to parse a
/// free-form string to distinguish a stale owner from an unresolved native semantic row.
/// </summary>
internal enum NetherStrategyUnknownReasonCode
{
    None = 0,
    UnknownEvidence,
    EvidencePackageUnavailable,
    EvidenceVersionMismatch,
    InvalidOwnerBinding,
    RuntimeGenerationMismatch,
    ControllerOwnerMismatch,
    EnteredSubsceneMismatch,
    AuthoritativeSnapshotMismatch,
    PartyEvidenceUnavailable,
    OwnedCodeEvidenceUnavailable,
    ResearchEvidenceUnavailable,
    NativeMechanicsUnavailable,
    VisibleMapUnavailable,
    ResearchTargetConfigurationUnknown,
    ResearchCompletionUnknown,
    RouteVectorInputUnavailable,
    RouteVectorUnknown,
    NativeBattleRouteSafetyUnknown,
    AmbiguousCandidateIdentity,
    CandidateIdentityInvalid,
    MechanismValueUnavailable,
    ErosionHorizonUnavailable,
    ResearchTargetUnavailable,
    ResearchFamilyRetentionUnavailable,
    CrestEvidenceUnavailable,
    CodeMutationValueUnavailable,
    StrictImprovementUnavailable,
    MasterDataUnavailable,
    InventoryEvidenceUnavailable,
    TransactionEvidenceUnavailable,
    RouteSafetyContextUnavailable,
    OptionEvidenceUnavailable,
    RecoveryBranchSafetyUnavailable,
    ConfigurationUnknown,
    TriggerEvidenceUnavailable,
    BuffStrategyEvidenceUnavailable,
    ConfigurationEvidenceUnavailable = ConfigurationUnknown,
    TriggerUnknown = TriggerEvidenceUnavailable,
    BuffStrategyUnknown = BuffStrategyEvidenceUnavailable,
}

internal static class NetherStrategyUnknownReasonCodes
{
    public static NetherStrategyUnknownReasonCode FromDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return NetherStrategyUnknownReasonCode.UnknownEvidence;

        if (detail.Contains("package-unavailable", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("snapshot-unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.EvidencePackageUnavailable;
        }
        if (detail.Contains("version", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.EvidenceVersionMismatch;
        if (detail.Contains("owner-binding", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.InvalidOwnerBinding;
        if (detail.Contains("runtime-generation", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.RuntimeGenerationMismatch;
        if (detail.Contains("controller-owner", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.ControllerOwnerMismatch;
        if (detail.Contains("entered-subscene", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.EnteredSubsceneMismatch;
        if (detail.Contains("snapshot", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.AuthoritativeSnapshotMismatch;
        if (detail.Contains("visible-map", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.VisibleMapUnavailable;
        if (detail.Contains("buff-strategy", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("unknown-buff-strategy", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("native-buff-trigger", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.BuffStrategyEvidenceUnavailable;
        }
        if (detail.Contains("trigger", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("situation", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.TriggerEvidenceUnavailable;
        }
        if (detail.Contains("configuration", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("invalid-run-boundary-settings", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("invalid-max-depth", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("invalid-safety-limits", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("invalid-soft-limit", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("negative-lock-reward", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("invalid-code-transform-strategy-mode", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.ConfigurationUnknown;
        }
        if (detail.Contains("transaction", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("commitment", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("reconcile", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.TransactionEvidenceUnavailable;
        }
        if (detail.Contains("inventory", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("shop-", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("shop-content", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("shop-row", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("item-row", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.InventoryEvidenceUnavailable;
        }
        if (detail.Contains("party", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("active-hp", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.PartyEvidenceUnavailable;
        if (detail.Contains("recovery", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.RecoveryBranchSafetyUnavailable;
        if (detail.Contains("owned-code", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.OwnedCodeEvidenceUnavailable;
        if (detail.Contains("research-completion", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("settlement", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.ResearchCompletionUnknown;
        }
        if (detail.Contains("research", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.ResearchEvidenceUnavailable;
        if (detail.Contains("native-mechanic", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.NativeMechanicsUnavailable;
        if (detail.Contains("event-part", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("floor-master", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("master-data", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("master-row", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("battle-row", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("floor-bounds", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.MasterDataUnavailable;
        }
        if (detail.Contains("mechanism", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.MechanismValueUnavailable;
        if (detail.Contains("horizon", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.ErosionHorizonUnavailable;
        if (detail.Contains("battle-route", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.NativeBattleRouteSafetyUnknown;
        if (detail.Contains("route-vector", StringComparison.OrdinalIgnoreCase))
            return NetherStrategyUnknownReasonCode.RouteVectorUnknown;
        if (detail.Contains("route", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("horizon", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("context", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.RouteSafetyContextUnavailable;
        }
        if (detail.Contains("option", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("event-effect", StringComparison.OrdinalIgnoreCase))
        {
            return NetherStrategyUnknownReasonCode.OptionEvidenceUnavailable;
        }
        return NetherStrategyUnknownReasonCode.UnknownEvidence;
    }
}

internal enum NetherResearchTargetState
{
    NotApplicable = 0,
    Active,
    Complete,
    Unknown,
}

/// <summary>Auditable strategy intent and target resolution attached to one evidence package.</summary>
internal sealed record NetherStrategyEvidenceAudit
{
    public int EvidenceVersion { get; init; } = NetherStrategyEvidenceContract.CurrentVersion;
    public NetherStrategyMode Mode { get; init; } = NetherStrategyMode.Equipment;
    public NetherCodeFamily PrimaryResearchFamily { get; init; } = NetherCodeFamily.Unknown;
    public NetherCodeFamily SecondaryResearchFamily { get; init; } = NetherCodeFamily.Unknown;
    public NetherCodeFamily ActiveResearchFamily { get; init; } = NetherCodeFamily.Unknown;
    public NetherResearchTargetState ResearchTargetState { get; init; } =
        NetherResearchTargetState.NotApplicable;
    public NetherStrategyUnknownReasonCode UnknownReasonCode { get; init; }
    public string UnknownReason { get; init; } = string.Empty;
    public long OwnerGeneration { get; init; }
    public long EnteredSubsceneGeneration { get; init; }
    public NetherSnapshotFingerprint SnapshotFingerprint { get; init; }

    public static NetherStrategyEvidenceAudit Create(
        NetherStrategyEvidenceIdentity identity,
        int evidenceVersion,
        NetherStrategyMode mode,
        NetherCodeFamily primary,
        NetherCodeFamily secondary,
        NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence> research
    )
    {
        var audit = new NetherStrategyEvidenceAudit
        {
            EvidenceVersion = evidenceVersion,
            Mode = mode,
            PrimaryResearchFamily = primary,
            SecondaryResearchFamily = secondary,
            OwnerGeneration = identity.ControllerOwnerGeneration,
            EnteredSubsceneGeneration = identity.EnteredSubsceneGeneration,
            SnapshotFingerprint = identity.SnapshotFingerprint,
            ResearchTargetState = mode == NetherStrategyMode.Research
                ? NetherResearchTargetState.Unknown
                : NetherResearchTargetState.NotApplicable,
        };
        if (mode != NetherStrategyMode.Research)
            return audit;
        if (!research.IsKnown || research.Value?.Families == null)
        {
            return audit with
            {
                UnknownReasonCode = NetherStrategyUnknownReasonCode.ResearchEvidenceUnavailable,
                UnknownReason = string.IsNullOrWhiteSpace(research.UnknownReason)
                    ? "research-evidence-unavailable"
                    : research.UnknownReason,
            };
        }

        if (TryResolve(primary, research.Value.Families, out NetherResearchTargetState primaryState,
                out string primaryReason))
        {
            if (primaryState == NetherResearchTargetState.Unknown)
            {
                return audit with
                {
                    UnknownReasonCode = NetherStrategyUnknownReasonCode.ResearchCompletionUnknown,
                    UnknownReason = primaryReason,
                };
            }
            if (primaryState == NetherResearchTargetState.Active)
            {
                return audit with
                {
                    ActiveResearchFamily = primary,
                    ResearchTargetState = NetherResearchTargetState.Active,
                };
            }
        }
        else if (primary != NetherCodeFamily.Unknown)
        {
            return audit with
            {
                UnknownReasonCode = NetherStrategyUnknownReasonCode.ResearchTargetConfigurationUnknown,
                UnknownReason = primaryReason,
            };
        }

        if (TryResolve(secondary, research.Value.Families, out NetherResearchTargetState secondaryState,
                out string secondaryReason))
        {
            if (secondaryState == NetherResearchTargetState.Unknown)
            {
                return audit with
                {
                    UnknownReasonCode = NetherStrategyUnknownReasonCode.ResearchCompletionUnknown,
                    UnknownReason = secondaryReason,
                };
            }
            if (secondaryState == NetherResearchTargetState.Active)
            {
                return audit with
                {
                    ActiveResearchFamily = secondary,
                    ResearchTargetState = NetherResearchTargetState.Active,
                };
            }
        }
        else if (secondary != NetherCodeFamily.Unknown)
        {
            return audit with
            {
                UnknownReasonCode = NetherStrategyUnknownReasonCode.ResearchTargetConfigurationUnknown,
                UnknownReason = secondaryReason,
            };
        }

        return audit with { ResearchTargetState = NetherResearchTargetState.Complete };
    }

    private static bool TryResolve(
        NetherCodeFamily family,
        IReadOnlyList<NetherStrategyResearchFamilyState> research,
        out NetherResearchTargetState state,
        out string reason
    )
    {
        state = NetherResearchTargetState.Complete;
        reason = string.Empty;
        if (family == NetherCodeFamily.Unknown)
            return true;
        NetherStrategyResearchFamilyState[] matches = new List<NetherStrategyResearchFamilyState>(research)
            .FindAll(row => row.Family == family)
            .ToArray();
        if (matches.Length != 1)
        {
            state = NetherResearchTargetState.Unknown;
            reason = "research-target-family-row-unavailable";
            return false;
        }
        NetherStrategyResearchFamilyState row = matches[0];
        if (!row.IsProjectedNormalSettlementKnown)
        {
            state = NetherResearchTargetState.Unknown;
            reason = string.IsNullOrWhiteSpace(row.ProjectionUnknownReason)
                ? "research-completion-projection-unknown"
                : row.ProjectionUnknownReason;
            return true;
        }
        state = (long)row.WalletPoints + row.ProjectedNormalSettlementPoints < 20_000
            ? NetherResearchTargetState.Active
            : NetherResearchTargetState.Complete;
        return true;
    }
}

/// <summary>
/// Stable, bounded projection of strategy identity for detailed decision/route audit records.
/// The controller may have no accepted package during a recovered-parent probe; in that case the
/// snapshot identity remains observable while the package-owned fields are explicitly unknown.
/// </summary>
internal static class NetherStrategyAuditFormatting
{
    public static NetherDetailedAuditField[] Context(
        NetherStrategyEvidenceAudit? audit,
        NetherSnapshotFingerprint fallbackSnapshot
    )
    {
        NetherSnapshotFingerprint snapshot = audit?.SnapshotFingerprint ?? fallbackSnapshot;
        return new[]
        {
            new NetherDetailedAuditField("evidenceVersion", audit?.EvidenceVersion.ToString() ?? "unknown"),
            new NetherDetailedAuditField("mode", audit?.Mode.ToString() ?? "unknown"),
            new NetherDetailedAuditField(
                "primaryResearchTarget",
                audit?.PrimaryResearchFamily.ToString() ?? "unknown"
            ),
            new NetherDetailedAuditField(
                "secondaryResearchTarget",
                audit?.SecondaryResearchFamily.ToString() ?? "unknown"
            ),
            new NetherDetailedAuditField(
                "activeResearchTarget",
                audit?.ActiveResearchFamily.ToString() ?? "unknown"
            ),
            new NetherDetailedAuditField(
                "researchTargetState",
                audit?.ResearchTargetState.ToString() ?? "unknown"
            ),
            new NetherDetailedAuditField(
                "strategyUnknownReasonCode",
                audit?.UnknownReasonCode.ToString() ?? "UnknownEvidence"
            ),
            new NetherDetailedAuditField(
                "ownerGeneration",
                audit?.OwnerGeneration.ToString() ?? "unknown"
            ),
            new NetherDetailedAuditField(
                "enteredSubsceneGeneration",
                audit?.EnteredSubsceneGeneration.ToString() ?? "unknown"
            ),
            new NetherDetailedAuditField("snapshotFingerprint", FormatSnapshot(snapshot)),
        };
    }

    public static string SemanticVector(NetherRouteEncounterVector? vector)
    {
        if (vector == null)
            return "known=false|unknown=RouteVectorInputUnavailable";

        return string.Join(
            "|",
            "known=" + vector.IsKnown,
            "unknown=" + vector.UnknownReasonCode,
            "boss=" + vector.ImmediateTerminalBossCount,
            "red5=" + vector.RedRankFiveTreasureCount,
            "gold=" + vector.GoldRankFiveTreasureCount,
            "gold5=" + vector.GoldRankFiveTreasureCount,
            "uncoloured5=" + vector.UncolouredRankFiveTreasureCount,
            "lateShop=" + vector.EligibleLateShopCount,
            "eventBoss=" + vector.EventBossCount,
            "elite=" + vector.EliteCount,
            "normal=" + vector.NormalBattleCount,
            "directCode=" + vector.DirectCodeOfferCount,
            "eventReward=" + vector.OrdinaryEventRewardCount,
            "recovery=" + vector.RecoveryCount,
            "ineligibleShop=" + vector.IneligibleShopCount,
            "treasure=" + vector.OtherTreasureCount
        );
    }

    private static string FormatSnapshot(NetherSnapshotFingerprint fingerprint) => string.Join(
        "|",
        "status:" + fingerprint.Status,
        "nether:" + fingerprint.NetherId,
        "map:" + fingerprint.MapId,
        "floor:" + fingerprint.FloorLevel,
        "index:" + fingerprint.FloorIndex,
        "currentFloor:" + fingerprint.CurrentFloorId,
        "currentNode:" + fingerprint.CurrentNodeId,
        "erosion:" + fingerprint.ErosionPoint,
        "ticket:" + fingerprint.TicketCount,
        "keys:" + fingerprint.TreasureKeyCount,
        "gold:" + fingerprint.NetherGold,
        "reload:" + fingerprint.CodeReloadCount,
        "lockReward:" + fingerprint.LockReward,
        "hpHash:" + fingerprint.CharacterHpHash,
        "codeHash:" + fingerprint.CodeHash,
        "mapHash:" + fingerprint.MapHash
    );
}

internal enum NetherRouteCandidateHardGate
{
    None = 0,
    TargetDepth,
    Locked,
    TerminalReachability,
    NativeNodeSemantics,
    HardSafety,
    HpSafety,
    TerminalErosion,
    VisibleHorizon,
    VisibleSemanticVector,
    ResearchCompletion,
}

internal enum NetherRouteSemanticTier
{
    None = 0,
    ImmediateTerminalBoss,
    RedRankFiveTreasure,
    GoldObjective,
    GoldRankFiveTreasure,
    UncolouredRankFiveTreasure,
    EventBoss,
    Elite,
    DirectCodeOffer,
    NormalBattle,
    OrdinaryEventReward,
    Recovery,
    IneligibleShop,
    OtherTreasure,
}

/// <summary>Selection-time route proof retained with the plan for audit and downstream binding.</summary>
internal sealed record NetherRouteSelectionEvidence
{
    public NetherRouteEncounterVector? SemanticVector { get; init; }
    public bool SemanticVectorKnown { get; init; }
    public string SemanticVectorUnknownReason { get; init; } = string.Empty;
    public NetherRouteSemanticTier SelectedSemanticTier { get; init; }
    public bool SafetyProjectionKnown { get; init; }
    public bool HardSafe { get; init; }
    public bool HpSafe { get; init; }
    public int TerminalWorstCaseErosion { get; init; }
    public int ProjectedErosionDelta { get; init; }
    public int ProjectedHpDelta { get; init; }
    public int ProcurementCommitmentCount { get; init; }
    public string TieBreakOrder { get; init; } = string.Empty;
    public IReadOnlyList<NetherRouteCandidateAudit> CandidateAudits { get; init; } =
        Array.Empty<NetherRouteCandidateAudit>();
}

internal enum NetherCodeCandidateHardGate
{
    None = 0,
    CandidateIdentity,
    AmbiguousCandidateIdentity,
    NativeMechanics,
    MechanismValue,
    RiskRule,
    ErosionHorizon,
    ResearchTarget,
    ResearchFamilyRetention,
    CrestCompatibility,
}

internal enum NetherCodeDecisionTier
{
    None = 0,
    RetainedPortfolioStrictImprovement,
    ResearchTargetProgression,
    ResearchCapacityReplacement,
    ResearchStrictCombatSwap,
    ThresholdRepair,
}

internal readonly record struct NetherCodeCandidateAudit(
    long CodeId,
    NetherCodeCandidateHardGate FirstFailingHardGate,
    string Detail
)
{
    public NetherStrategyUnknownReasonCode UnknownReasonCode { get; init; }
    public NetherCodeDecisionTier SelectionTier { get; init; }
    public bool IsEligible => FirstFailingHardGate == NetherCodeCandidateHardGate.None;
}
