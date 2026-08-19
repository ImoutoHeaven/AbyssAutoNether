#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal sealed record NetherCodeCandidate(long CodeId, NetherCodeFamily Family, int AbilityLevel)
{
    public bool IsKnown { get; init; } = true;
    public bool EffectSemanticsKnown { get; init; } = true;
    public NetherCodeCategory Category { get; init; }
    public int Rarity { get; init; }
    /// <summary>Static MNetherCodes.power; a deterministic reference, not proven party DPS.</summary>
    public int Power { get; init; }
    public NetherCodeMasterEffectType MasterEffectType { get; init; }
    public long EffectParameter1 { get; init; }
    public long EffectParameter2 { get; init; }
    public long EffectParameter3 { get; init; }
    public long AbilityAssetId { get; init; }
    public bool PartyCoverageKnown { get; init; }
    public int PartyCoverage { get; init; }
}

internal sealed record NetherCodePortfolio
{
    public IReadOnlyList<NetherCodeState> CurrentCodes { get; init; } = Array.Empty<NetherCodeState>();
    public int Capacity { get; init; }
    public int ReloadCount { get; init; }
    public bool IsMasterComplete { get; init; }
    public NetherCombatLane? LockedLane { get; init; }
}

internal enum NetherCodeTargetRow
{
    None = 0,
    Forward,
    Back,
    All,
}

internal enum NetherPartyPosition
{
    Unknown = 0,
    Forward = 1,
    Back = 2,
    Assist = 3,
}

internal enum NetherCrestIdentity
{
    Unknown = 0,
    General = 1,
    Passion = 2,
    Impact = 3,
}

internal enum NetherCodeRiskRule
{
    None = 0,
    MinimumErosionSeventy,
    AdverseErosionAdjustment,
    ConditionalFiftyToSeventy,
}

/// <summary>
/// Candidate-local, typed mechanic classification. Current Code IDs may be used to characterize
/// known assets, but eligibility consumes these native relationships rather than the identifier.
/// </summary>
internal sealed record NetherCodeHardEligibilityEvidence
{
    public bool IsKnown { get; init; }
    public string UnknownReason { get; init; } = string.Empty;
    public NetherCodeFamily UniformCrestFamily { get; init; } = NetherCodeFamily.Unknown;
    public NetherCodeTargetRow UniformCrestTargetRow { get; init; }
    public NetherCodeRiskRule RiskRule { get; init; }
    public int ResearchRateOverwrite { get; init; }
}

internal enum NetherOpposedFamilyPair
{
    RushImpact = 0,
    SafeRisk,
}

internal sealed record NetherFamilyRetentionEvidence
{
    public bool IsKnown { get; init; }
    public NetherCodeFamily PreferredFamily { get; init; } = NetherCodeFamily.Unknown;
    public string Detail { get; init; } = string.Empty;

    public static NetherFamilyRetentionEvidence Known(NetherCodeFamily preferredFamily) => new()
    {
        IsKnown = true,
        PreferredFamily = preferredFamily,
    };

    public static NetherFamilyRetentionEvidence Equal(string detail) => new()
    {
        IsKnown = true,
        PreferredFamily = NetherCodeFamily.Unknown,
        Detail = string.IsNullOrWhiteSpace(detail)
            ? "opposed-family-complete-portfolios-equal"
            : detail + ":equal",
    };

    public static NetherFamilyRetentionEvidence Unknown(string detail) => new()
    {
        Detail = string.IsNullOrWhiteSpace(detail)
            ? "opposed-family-complete-portfolio-unavailable"
            : detail,
    };
}

internal sealed record NetherCodePolicyEvidence
{
    public IReadOnlyDictionary<long, NetherCodeHardEligibilityEvidence> MechanicsByCodeId { get; init; } =
        new Dictionary<long, NetherCodeHardEligibilityEvidence>();
    public IReadOnlyList<NetherStrategyPartyMember>? ActiveParty { get; init; }
    public IReadOnlyList<NetherStrategyResearchFamilyState>? Research { get; init; }
    public IReadOnlyDictionary<long, NetherMechanismValue> MechanismValuesByCodeId { get; init; } =
        new Dictionary<long, NetherMechanismValue>();
    public IReadOnlyDictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
        EquipmentMutationValuesByKey { get; init; } =
            new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>();
    public NetherCodeFamily ActiveResearchFamily { get; init; } = NetherCodeFamily.Unknown;
    public IReadOnlyDictionary<NetherOpposedFamilyPair, NetherFamilyRetentionEvidence>
        FamilyRetentionByPair { get; init; } =
            new Dictionary<NetherOpposedFamilyPair, NetherFamilyRetentionEvidence>();
    /// <summary>Owned Codes whose native mechanics are already hard-excluded.</summary>
    public IReadOnlyList<long> HardExcludedCodeIds { get; init; } = Array.Empty<long>();
    /// <summary>
    /// Completed-family IDs that an authoritative settlement projection proved surplus. Without
    /// this list a completed family is never treated as disposable merely because it is off-target.
    /// </summary>
    public IReadOnlyList<long> ProvablySurplusCompletedCodeIds { get; init; } = Array.Empty<long>();
    public bool ErosionHorizonKnown { get; init; }
    public int ProjectedMinimumErosion { get; init; }
    public int ProjectedMaximumErosion { get; init; }
    public bool RecoverableToFiftySeventyBand { get; init; }
}

internal readonly record struct NetherRuntimeCodePolicyEvidenceResult(
    NetherCodePolicyEvidence? Evidence,
    string Detail
)
{
    public NetherStrategyEvidenceAudit? StrategyAudit { get; init; }
    public bool IsSuccess => Evidence != null && Detail.Length == 0;

    public static NetherRuntimeCodePolicyEvidenceResult Success(NetherCodePolicyEvidence evidence) =>
        new(evidence ?? throw new ArgumentNullException(nameof(evidence)), string.Empty);

    public static NetherRuntimeCodePolicyEvidenceResult Failure(string detail) =>
        new(null, string.IsNullOrWhiteSpace(detail) ? "code-policy-evidence-unavailable" : detail);
}

/// <summary>
/// Native GetCategoryCount values.  Each owned NetherCodeModel contributes exactly one card;
/// its ability level, master power, and server Amount do not multiply this count.
/// </summary>
internal readonly record struct NetherCodeEffectiveLevels(int Safe, int Risk, int Rush, int Impact);

internal enum NetherCodeDecisionKind
{
    Select,
    Reload,
    Keep,
    Pause,
}

internal sealed record NetherCodeDecision
{
    public NetherCodeDecisionKind Kind { get; init; }
    public long SelectedCodeId { get; init; }
    public long RemoveCodeId { get; init; }
    public NetherCombatLane LockedLane { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
    public IReadOnlyList<long> RemovableCodeIds { get; init; } = Array.Empty<long>();
    /// <summary>Every offered option retains its first failing hard gate, if any.</summary>
    public IReadOnlyList<NetherCodeCandidateAudit> CandidateAudits { get; init; } =
        Array.Empty<NetherCodeCandidateAudit>();
    /// <summary>Exact retained portfolio after a selected candidate/removal pair is simulated.</summary>
    public IReadOnlyList<long> RetainedCodeIds { get; init; } = Array.Empty<long>();
    public NetherCodeCandidateHardGate FirstFailingHardGate { get; init; }
    public NetherCodeDecisionTier DecisionTier { get; init; }
    public NetherStrategyUnknownReasonCode UnknownReasonCode { get; init; }
    public NetherEquipmentMutationValueKind MutationValueKind { get; init; }
    public bool StrictImprovementProven { get; init; }
    /// <summary>Static MNetherCodes.power is intentionally never consumed by decision policy.</summary>
    public bool DisplayPowerUsedForDecision { get; init; }
}

/// <summary>
/// Code hard-eligibility and strategy decision seam. Equipment orders only complete native
/// retained-portfolio and mechanism evidence after hard gates; static master power and UI coverage
/// never contribute to an authoritative decision.
/// </summary>
internal sealed class NetherCodePolicy
{
    public NetherCodeDecision Decide(
        NetherCodePortfolio portfolio,
        IReadOnlyList<NetherCodeCandidate> candidates,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence
    )
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        if (candidates == null)
            throw new ArgumentNullException(nameof(candidates));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (evidence == null)
            throw new ArgumentNullException(nameof(evidence));
        if (!IsValid(portfolio, candidates))
        {
            return Pause(NetherPauseReason.UnknownMasterData, "incomplete-code-portfolio") with
            {
                FirstFailingHardGate = NetherCodeCandidateHardGate.CandidateIdentity,
                UnknownReasonCode = NetherStrategyUnknownReasonCode.CandidateIdentityInvalid,
            };
        }

        NetherCodeCandidateAudit[] duplicateAudits = candidates
            .GroupBy(candidate => candidate.CodeId)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(candidate => new NetherCodeCandidateAudit(
                candidate.CodeId,
                NetherCodeCandidateHardGate.AmbiguousCandidateIdentity,
                "duplicate-candidate-code-id"
            )
            {
                UnknownReasonCode = NetherStrategyUnknownReasonCode.AmbiguousCandidateIdentity,
            }))
            .ToArray();
        if (duplicateAudits.Length > 0)
        {
            return Pause(NetherPauseReason.UnknownMasterData, "ambiguous-code-candidate-identity") with
            {
                CandidateAudits = duplicateAudits,
                FirstFailingHardGate = NetherCodeCandidateHardGate.AmbiguousCandidateIdentity,
                UnknownReasonCode = NetherStrategyUnknownReasonCode.AmbiguousCandidateIdentity,
            };
        }

        NetherCodeCandidate[] uniqueCandidates = candidates.ToArray();
        NetherCodeFamily effectiveResearchFamily = ResolveEffectiveResearchFamily(
            portfolio,
            uniqueCandidates,
            settings,
            evidence
        );
        NetherCodeCandidate[] eligible = uniqueCandidates
            .Where(candidate => IsHardEligible(
                candidate,
                portfolio,
                settings,
                evidence,
                effectiveResearchFamily
            ))
            .ToArray();
        NetherCodeCandidateAudit[] candidateAudits = CreateCandidateAudits(
            uniqueCandidates,
            portfolio,
            settings,
            evidence,
            effectiveResearchFamily
        );
        NetherCombatLane lane = ResolveLane(portfolio, settings.CombatLane);
        NetherCodeDecision decision;
        if (TryGetIncompatibleThresholdFamily(
                portfolio.CurrentCodes,
                evidence.ActiveParty,
                out NetherCodeFamily incompatibleFamily
            ))
        {
            if (settings.StrategyMode == NetherStrategyMode.Equipment)
            {
                NetherCodeDecision equipmentRepair = DecideEquipment(
                    portfolio,
                    eligible,
                    settings,
                    evidence,
                    lane
                );
                decision = equipmentRepair.Kind == NetherCodeDecisionKind.Select
                    ? equipmentRepair
                    : Pause(
                        NetherPauseReason.UnknownMasterData,
                        "incompatible-category-five-no-strict-equipment-repair"
                    );
            }
            else
            {
                decision = TryRepairThresholdPortfolio(
                    portfolio,
                    eligible,
                    settings,
                    evidence,
                    incompatibleFamily,
                    lane
                );
            }
        }
        else
        {
            decision = settings.StrategyMode == NetherStrategyMode.Equipment
                ? DecideEquipment(portfolio, eligible, settings, evidence, lane)
                : DecideResearch(
                    portfolio,
                    eligible,
                    settings,
                    evidence,
                    lane,
                    effectiveResearchFamily
                );
        }
        decision = AttachCandidateAudits(decision, candidateAudits);
        if (decision.Kind != NetherCodeDecisionKind.Select)
            return decision;

        NetherCodeCandidate? selected = eligible.FirstOrDefault(
            candidate => candidate.CodeId == decision.SelectedCodeId
        );
        if (selected == null)
        {
            return AttachCandidateAudits(
                Pause(NetherPauseReason.UnknownMasterData, "selected-code-evidence-lost") with
                {
                    FirstFailingHardGate = NetherCodeCandidateHardGate.CandidateIdentity,
                    UnknownReasonCode = NetherStrategyUnknownReasonCode.CandidateIdentityInvalid,
                },
                candidateAudits
            );
        }
        IReadOnlyList<NetherCodeState> after = ApplyDecision(
            portfolio.CurrentCodes,
            selected,
            decision.RemoveCodeId
        );
        bool mutationLegal = settings.StrategyMode == NetherStrategyMode.Equipment
            ? IsEquipmentMutationLegal(
                portfolio.CurrentCodes,
                after,
                selected.Family,
                decision.RemoveCodeId,
                settings,
                evidence
            )
            : IsPortfolioHardSafe(after, evidence.ActiveParty)
                || IsIncrementalOpposedFamilyRepair(
                    portfolio.CurrentCodes,
                    after,
                    selected.Family,
                    decision.RemoveCodeId,
                    effectiveResearchFamily,
                    settings,
                    evidence
                )
            ;
        return mutationLegal
            ? FinalizeSelectedDecision(decision, after, settings, evidence)
            : AttachCandidateAudits(
                ReloadOrKeep(
                    portfolio,
                    settings,
                    lane,
                    "candidate-violates-family-or-crest-integrity",
                    decision.RemovableCodeIds
                ),
                candidateAudits
            );
    }

    private static NetherCodeDecision AttachCandidateAudits(
        NetherCodeDecision decision,
        IReadOnlyList<NetherCodeCandidateAudit> candidateAudits
    )
    {
        NetherCodeCandidateAudit[] audits = candidateAudits?.ToArray()
            ?? Array.Empty<NetherCodeCandidateAudit>();
        NetherCodeDecisionTier tier = ResolveDecisionTier(decision.Detail);
        if (decision.Kind == NetherCodeDecisionKind.Select && decision.SelectedCodeId > 0)
        {
            audits = audits
                .Select(audit => audit.CodeId == decision.SelectedCodeId
                    ? audit with { SelectionTier = tier }
                    : audit)
                .ToArray();
        }

        bool hasFailure = false;
        NetherCodeCandidateAudit firstFailure = default;
        foreach (NetherCodeCandidateAudit audit in audits)
        {
            if (!audit.IsEligible)
            {
                hasFailure = true;
                firstFailure = audit;
                break;
            }
        }
        return decision with
        {
            CandidateAudits = audits,
            FirstFailingHardGate = decision.Kind == NetherCodeDecisionKind.Select
                ? decision.FirstFailingHardGate
                : hasFailure
                ? firstFailure.FirstFailingHardGate
                : decision.FirstFailingHardGate,
            UnknownReasonCode = decision.Kind == NetherCodeDecisionKind.Select
                ? decision.UnknownReasonCode
                : hasFailure && firstFailure.UnknownReasonCode != NetherStrategyUnknownReasonCode.None
                ? firstFailure.UnknownReasonCode
                : decision.UnknownReasonCode,
            DecisionTier = decision.DecisionTier == NetherCodeDecisionTier.None
                ? tier
                : decision.DecisionTier,
        };
    }

    private static NetherCodeDecision FinalizeSelectedDecision(
        NetherCodeDecision decision,
        IReadOnlyList<NetherCodeState> retained,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence
    )
    {
        NetherEquipmentMutationValueKind valueKind = NetherEquipmentMutationValueKind.Missing;
        bool strictImprovement = false;
        if (decision.SelectedCodeId > 0)
        {
            NetherCodeMutationKey key = new(decision.SelectedCodeId, decision.RemoveCodeId);
            if (evidence.EquipmentMutationValuesByKey != null
                && evidence.EquipmentMutationValuesByKey.TryGetValue(
                    key,
                    out NetherCodeEquipmentMutationEvidence? mutation
                )
                && mutation != null)
            {
                NetherEquipmentMutationValue value = new NetherEquipmentCodeValuePolicy().Evaluate(mutation);
                valueKind = value.Kind;
                strictImprovement = value.CanSelect;
            }
        }
        if (settings.StrategyMode == NetherStrategyMode.Research
            && decision.Detail.Contains("same-family-strict-combat-swap", StringComparison.Ordinal))
        {
            strictImprovement = true;
            if (valueKind == NetherEquipmentMutationValueKind.Missing)
                valueKind = NetherEquipmentMutationValueKind.StrictQuantifiedImprovement;
        }

        return decision with
        {
            RetainedCodeIds = retained
                .Where(code => code != null && code.PossessionAmount > 0)
                .Select(code => code.CodeId)
                .Distinct()
                .OrderBy(codeId => codeId)
                .ToArray(),
            DecisionTier = ResolveDecisionTier(decision.Detail),
            MutationValueKind = valueKind,
            StrictImprovementProven = strictImprovement,
            DisplayPowerUsedForDecision = false,
        };
    }

    private static NetherCodeDecisionTier ResolveDecisionTier(string detail)
    {
        if (detail.Contains("native-retained-portfolio", StringComparison.Ordinal))
            return NetherCodeDecisionTier.RetainedPortfolioStrictImprovement;
        if (detail.Contains("same-family-strict-combat-swap", StringComparison.Ordinal))
            return NetherCodeDecisionTier.ResearchStrictCombatSwap;
        if (detail.Contains("ordered-capacity-replacement", StringComparison.Ordinal))
            return NetherCodeDecisionTier.ResearchCapacityReplacement;
        if (detail.Contains("research-target", StringComparison.Ordinal))
            return NetherCodeDecisionTier.ResearchTargetProgression;
        if (detail.Contains("repair-incompatible-category-five", StringComparison.Ordinal))
            return NetherCodeDecisionTier.ThresholdRepair;
        return NetherCodeDecisionTier.None;
    }

    private static NetherCodeCandidateAudit[] CreateCandidateAudits(
        IReadOnlyList<NetherCodeCandidate> candidates,
        NetherCodePortfolio portfolio,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence,
        NetherCodeFamily effectiveResearchFamily
    ) => candidates
        .Select(candidate => CreateCandidateAudit(
            candidate,
            portfolio,
            settings,
            evidence,
            effectiveResearchFamily
        ))
        .ToArray();

    private static NetherCodeCandidateAudit CreateCandidateAudit(
        NetherCodeCandidate candidate,
        NetherCodePortfolio portfolio,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence,
        NetherCodeFamily effectiveResearchFamily
    )
    {
        if (!IsValid(candidate))
            return new(candidate.CodeId, NetherCodeCandidateHardGate.CandidateIdentity, "invalid-code-candidate")
            {
                UnknownReasonCode = NetherStrategyUnknownReasonCode.CandidateIdentityInvalid,
            };

        if (!evidence.MechanicsByCodeId.TryGetValue(candidate.CodeId, out NetherCodeHardEligibilityEvidence? mechanic)
            || mechanic == null
            || !mechanic.IsKnown)
        {
            return new(
                candidate.CodeId,
                NetherCodeCandidateHardGate.NativeMechanics,
                mechanic?.UnknownReason ?? "native-mechanics-unavailable"
            )
            {
                UnknownReasonCode = NetherStrategyUnknownReasonCode.NativeMechanicsUnavailable,
            };
        }

        if (evidence.MechanismValuesByCodeId == null
            || !evidence.MechanismValuesByCodeId.TryGetValue(
                candidate.CodeId,
                out NetherMechanismValue mechanismValue
            )
            || mechanismValue.Kind == NetherCombatValueEvidenceKind.Missing
            || settings.StrategyMode == NetherStrategyMode.Equipment
                && mechanismValue.Kind == NetherCombatValueEvidenceKind.ReachableUnquantified)
        {
            return new(
                candidate.CodeId,
                NetherCodeCandidateHardGate.MechanismValue,
                "candidate-mechanism-value-unavailable"
            )
            {
                UnknownReasonCode = NetherStrategyUnknownReasonCode.MechanismValueUnavailable,
            };
        }

        if (mechanic.RiskRule is NetherCodeRiskRule.MinimumErosionSeventy
            or NetherCodeRiskRule.AdverseErosionAdjustment)
        {
            return new(candidate.CodeId, NetherCodeCandidateHardGate.RiskRule, mechanic.RiskRule.ToString());
        }
        if (mechanic.RiskRule == NetherCodeRiskRule.ConditionalFiftyToSeventy
            && (!evidence.ErosionHorizonKnown
                || evidence.ProjectedMinimumErosion < 50
                || evidence.ProjectedMaximumErosion > 70
                || !evidence.RecoverableToFiftySeventyBand))
        {
            return new(
                candidate.CodeId,
                NetherCodeCandidateHardGate.ErosionHorizon,
                "conditional-risk-horizon-unavailable"
            )
            {
                UnknownReasonCode = !evidence.ErosionHorizonKnown
                    ? NetherStrategyUnknownReasonCode.ErosionHorizonUnavailable
                    : NetherStrategyUnknownReasonCode.None,
            };
        }

        if (mechanic.ResearchRateOverwrite > 0)
        {
            if (settings.StrategyMode != NetherStrategyMode.Research
                || effectiveResearchFamily == NetherCodeFamily.Unknown
                || candidate.Family != effectiveResearchFamily
                || evidence.Research == null)
            {
                return new(
                    candidate.CodeId,
                    NetherCodeCandidateHardGate.ResearchTarget,
                    "research-rate-overwrite-not-authorized"
                )
                {
                    UnknownReasonCode = evidence.ActiveResearchFamily == NetherCodeFamily.Unknown
                        ? NetherStrategyUnknownReasonCode.ResearchTargetUnavailable
                        : NetherStrategyUnknownReasonCode.None,
                };
            }
            NetherStrategyResearchFamilyState[] matching = evidence.Research
                .Where(row => row.Family == candidate.Family)
                .ToArray();
            if (matching.Length != 1)
            {
                return new(
                    candidate.CodeId,
                    NetherCodeCandidateHardGate.ResearchTarget,
                    "research-family-row-unavailable"
                )
                {
                    UnknownReasonCode = NetherStrategyUnknownReasonCode.ResearchCompletionUnknown,
                };
            }
            if (mechanic.ResearchRateOverwrite <= matching[0].TechnologyResearchRate)
                return new(candidate.CodeId, NetherCodeCandidateHardGate.ResearchTarget, "research-rate-not-strict");
        }

        NetherCodeFamily opposing = Opposing(candidate.Family);
        bool hasCandidateFamily = portfolio.CurrentCodes.Any(code => code.Family == candidate.Family);
        bool hasOpposing = portfolio.CurrentCodes.Any(code => code.Family == opposing);
        if (settings.StrategyMode == NetherStrategyMode.Research
            && hasOpposing && hasCandidateFamily
            && ResolveRetainedFamily(
                portfolio,
                settings,
                evidence,
                candidate.Family,
                opposing,
                effectiveResearchFamily
            ) != candidate.Family)
        {
            return new(
                candidate.CodeId,
                NetherCodeCandidateHardGate.ResearchFamilyRetention,
                "active-research-family-retention-reject"
            );
        }

        if (mechanic.UniformCrestTargetRow == NetherCodeTargetRow.None)
            return new(candidate.CodeId, NetherCodeCandidateHardGate.None, "eligible");
        if (mechanic.UniformCrestFamily is not (NetherCodeFamily.Rush or NetherCodeFamily.Impact)
            || evidence.ActiveParty == null)
        {
            return new(
                candidate.CodeId,
                NetherCodeCandidateHardGate.CrestCompatibility,
                "crest-evidence-unavailable"
            )
            {
                UnknownReasonCode = NetherStrategyUnknownReasonCode.CrestEvidenceUnavailable,
            };
        }

        NetherCrestIdentity requiredCrest = CrestForFamily(mechanic.UniformCrestFamily);
        NetherStrategyPartyMember[] recipients = evidence.ActiveParty
            .Where(member => member != null && member.IsAlive)
            .Where(member => mechanic.UniformCrestTargetRow switch
            {
                NetherCodeTargetRow.Forward => PartyPositionOf(member) == NetherPartyPosition.Forward,
                NetherCodeTargetRow.Back => PartyPositionOf(member) == NetherPartyPosition.Back,
                NetherCodeTargetRow.All => PartyPositionOf(member) != NetherPartyPosition.Unknown,
                _ => false,
            })
            .ToArray();
        if (requiredCrest == NetherCrestIdentity.Unknown || recipients.Length == 0)
        {
            return new(
                candidate.CodeId,
                NetherCodeCandidateHardGate.CrestCompatibility,
                "crest-recipient-evidence-unavailable"
            )
            {
                UnknownReasonCode = NetherStrategyUnknownReasonCode.CrestEvidenceUnavailable,
            };
        }
        return recipients.All(member => CrestOf(member) == requiredCrest)
            ? new(candidate.CodeId, NetherCodeCandidateHardGate.None, "eligible")
            : new(candidate.CodeId, NetherCodeCandidateHardGate.CrestCompatibility, "crest-compatibility-rejected");
    }

    private static NetherCodeDecision DecideEquipment(
        NetherCodePortfolio portfolio,
        IReadOnlyList<NetherCodeCandidate> hardEligible,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence,
        NetherCombatLane lane
    )
    {
        var ownedIds = new HashSet<long>(portfolio.CurrentCodes.Select(code => code.CodeId));
        NetherCodeCandidate[] candidates = hardEligible
            .Where(candidate => !ownedIds.Contains(candidate.CodeId))
            .ToArray();
        if (candidates.Length == 0)
            return ReloadOrKeep(portfolio, settings, lane, "no-hard-eligible-new-code-candidate");

        var valuePolicy = new NetherEquipmentCodeValuePolicy();
        EquipmentValueChoice? best = null;
        foreach (NetherCodeCandidate candidate in candidates)
        {
            if (!evidence.MechanismValuesByCodeId.TryGetValue(
                    candidate.CodeId,
                    out NetherMechanismValue candidateMechanism
                )
                || candidateMechanism.Kind
                    == NetherCombatValueEvidenceKind.ReachableUnquantified)
            {
                continue;
            }

            IEnumerable<long> removals = portfolio.CurrentCodes.Count < portfolio.Capacity
                ? new long[] { 0 }
                : portfolio.CurrentCodes
                    .Where(code => code.CodeId != candidate.CodeId)
                    .Select(code => code.CodeId);
            foreach (long removal in removals)
            {
                IReadOnlyList<NetherCodeState> after = ApplyDecision(
                    portfolio.CurrentCodes,
                    candidate,
                    removal
                );
                if (!IsEquipmentMutationLegal(
                        portfolio.CurrentCodes,
                        after,
                        candidate.Family,
                        removal,
                        settings,
                        evidence
                    ))
                    continue;

                var key = new NetherCodeMutationKey(candidate.CodeId, removal);
                if (evidence.EquipmentMutationValuesByKey == null
                    || !evidence.EquipmentMutationValuesByKey.TryGetValue(
                        key,
                        out NetherCodeEquipmentMutationEvidence? mutation
                    )
                    || mutation == null
                    || mutation.CandidateCodeId != candidate.CodeId
                    || mutation.RemoveCodeId != removal)
                {
                    continue;
                }

                NetherEquipmentMutationValue value = valuePolicy.Evaluate(mutation);
                if (!value.CanSelect)
                    continue;
                var choice = new EquipmentValueChoice(
                    candidate,
                    removal,
                    value,
                    GetEquipmentRemovalPriority(portfolio, candidate, removal, evidence)
                );
                if (best == null || CompareEquipmentChoice(choice, best.Value, valuePolicy) > 0)
                    best = choice;
            }
        }

        long[] removable = portfolio.CurrentCodes
            .Select(code => code.CodeId)
            .OrderBy(codeId => codeId)
            .ToArray();
        return best is EquipmentValueChoice selected
            ? Select(selected.Candidate, selected.RemoveCodeId, lane, removable) with
            {
                Detail = "native-retained-portfolio;mechanism-specific-value",
            }
            : ReloadOrKeep(
                portfolio,
                settings,
                lane,
                "no-strict-equipment-value-improvement",
                removable
            );
    }

    private static NetherCodeDecision DecideResearch(
        NetherCodePortfolio portfolio,
        IReadOnlyList<NetherCodeCandidate> hardEligible,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence,
        NetherCombatLane lane,
        NetherCodeFamily effectiveResearchFamily
    )
    {
        if (AreConfiguredResearchTargetsComplete(settings, evidence))
        {
            return DecideEquipment(portfolio, hardEligible, settings, evidence, lane);
        }

        // The authoritative active family is mandatory: displayed Power/coverage and a fixed Code
        // count must never substitute for the wallet plus projected settlement contract.
        if (evidence.ActiveResearchFamily == NetherCodeFamily.Unknown)
        {
            return ReloadOrKeep(
                portfolio,
                settings,
                lane,
                "active-research-family-unavailable"
            );
        }

        NetherCodeFamily activeFamily = evidence.ActiveResearchFamily;
        var ownedIds = new HashSet<long>(portfolio.CurrentCodes.Select(code => code.CodeId));
        NetherCodeFamily targetFamily = effectiveResearchFamily == NetherCodeFamily.Unknown
            ? activeFamily
            : effectiveResearchFamily;
        NetherCodeCandidate[] candidates = hardEligible
            .Where(candidate => candidate.Family == targetFamily)
            .Where(candidate => !ownedIds.Contains(candidate.CodeId))
            .OrderBy(candidate => candidate.CodeId)
            .ToArray();
        if (candidates.Length == 0)
        {
            // Research owns the reroll budget while a configured target is incomplete. The
            // secondary family is not eligible until every currently available reroll has been
            // consumed; this deliberately ignores CodeReloadReserve (Equipment still uses it).
            if (targetFamily == activeFamily && portfolio.ReloadCount > 0)
            {
                return ReloadOrKeep(
                    portfolio,
                    settings,
                    lane,
                    "no-new-active-research-family-candidate",
                    forceReload: true
                );
            }

            if (targetFamily == activeFamily
                && (settings.ResearchSecondaryFamily is NetherCodeFamily.Unknown
                    || settings.ResearchSecondaryFamily == activeFamily))
            {
                return ReloadOrKeep(
                    portfolio,
                    settings,
                    lane,
                    "no-new-active-research-family-candidate",
                    forceReload: true
                );
            }

        }
        if (candidates.Length == 0)
        {
            return ReloadOrKeep(
                portfolio,
                settings,
                lane,
                "no-new-secondary-research-family-candidate",
                forceReload: true
            );
        }

        NetherCodeCandidate selected = candidates[0];
        if (portfolio.CurrentCodes.Count < portfolio.Capacity)
            return Select(selected, 0, lane, Array.Empty<long>());

        long[] removable = portfolio.CurrentCodes
            .Where(code => code.Family != targetFamily
                || evidence.HardExcludedCodeIds.Contains(code.CodeId))
            .Where(code => CanResearchRemove(
                portfolio,
                selected,
                code,
                targetFamily,
                settings,
                evidence
            ))
            .OrderByDescending(code => ResearchRemovalPriority(code, targetFamily, evidence))
            .ThenBy(code => code.CodeId)
            .Select(code => code.CodeId)
            .ToArray();
        if (removable.Length > 0)
        {
            return Select(selected, removable[0], lane, removable) with
            {
                Detail = "research-target;ordered-capacity-replacement",
            };
        }

        EquipmentValueChoice? sameFamily = FindResearchSameFamilySwap(
            portfolio,
            candidates,
            targetFamily,
            settings,
            evidence
        );
        if (sameFamily is EquipmentValueChoice swap)
        {
            return Select(swap.Candidate, swap.RemoveCodeId, lane, new[] { swap.RemoveCodeId }) with
            {
                Detail = "research-target;same-family-strict-combat-swap",
            };
        }
        return ReloadOrKeep(
                portfolio,
                settings,
                lane,
                "active-research-family-portfolio-full",
                removable,
                forceReload: true
            );
    }

    private static int CompareEquipmentChoice(
        EquipmentValueChoice left,
        EquipmentValueChoice right,
        NetherEquipmentCodeValuePolicy valuePolicy
    )
    {
        int value = valuePolicy.Compare(left.Value, right.Value);
        if (value != 0)
            return value;
        int removal = left.RemovalPriority.CompareTo(right.RemovalPriority);
        if (removal != 0)
            return removal;
        int candidate = right.Candidate.CodeId.CompareTo(left.Candidate.CodeId);
        if (candidate != 0)
            return candidate;
        return right.RemoveCodeId.CompareTo(left.RemoveCodeId);
    }

    private const int ResearchCompletionPoints = 20_000;

    private static bool AreConfiguredResearchTargetsComplete(
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence
    ) => settings.StrategyMode == NetherStrategyMode.Research
        && IsResearchFamilyComplete(settings.ResearchPrimaryFamily, evidence)
        && IsResearchFamilyComplete(settings.ResearchSecondaryFamily, evidence);

    private static NetherCodeFamily ResolveEffectiveResearchFamily(
        NetherCodePortfolio portfolio,
        IReadOnlyList<NetherCodeCandidate> candidates,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence
    )
    {
        if (settings.StrategyMode != NetherStrategyMode.Research
            || AreConfiguredResearchTargetsComplete(settings, evidence))
        {
            return NetherCodeFamily.Unknown;
        }
        NetherCodeFamily activeFamily = evidence.ActiveResearchFamily;
        if (activeFamily == NetherCodeFamily.Unknown)
            return NetherCodeFamily.Unknown;

        var ownedIds = new HashSet<long>(portfolio.CurrentCodes.Select(code => code.CodeId));
        bool hasEligibleActiveCandidate = candidates
            .Where(candidate => candidate.Family == activeFamily)
            .Where(candidate => !ownedIds.Contains(candidate.CodeId))
            .Any(candidate => IsHardEligible(
                candidate,
                portfolio,
                settings,
                evidence,
                activeFamily
            ));
        if (hasEligibleActiveCandidate || portfolio.ReloadCount > 0)
            return activeFamily;

        return settings.ResearchSecondaryFamily is NetherCodeFamily.Unknown
            || settings.ResearchSecondaryFamily == activeFamily
            ? activeFamily
            : settings.ResearchSecondaryFamily;
    }

    private static bool IsResearchFamilyComplete(
        NetherCodeFamily family,
        NetherCodePolicyEvidence evidence
    )
    {
        if (family == NetherCodeFamily.Unknown)
            return true;
        if (evidence.Research == null)
            return false;
        NetherStrategyResearchFamilyState[] matches = evidence.Research
            .Where(row => row.Family == family)
            .ToArray();
        return matches.Length == 1
            && matches[0].IsProjectedNormalSettlementKnown
            && (long)matches[0].WalletPoints + matches[0].ProjectedNormalSettlementPoints
                >= ResearchCompletionPoints;
    }

    private static bool CanResearchRemove(
        NetherCodePortfolio portfolio,
        NetherCodeCandidate candidate,
        NetherCodeState removal,
        NetherCodeFamily targetFamily,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence
    )
    {
        if (removal == null || removal.CodeId <= 0)
            return false;

        bool hardExcluded = evidence.HardExcludedCodeIds.Contains(removal.CodeId);
        if (!hardExcluded && removal.Family == targetFamily)
            return false;

        bool completedFamily = settings.StrategyMode == NetherStrategyMode.Research
            && (removal.Family == settings.ResearchPrimaryFamily
                || removal.Family == settings.ResearchSecondaryFamily)
            && IsResearchFamilyComplete(removal.Family, evidence);
        if (completedFamily
            && !hardExcluded
            && !evidence.ProvablySurplusCompletedCodeIds.Contains(removal.CodeId))
        {
            return false;
        }

        IReadOnlyList<NetherCodeState> after = ApplyDecision(
            portfolio.CurrentCodes,
            candidate,
            removal.CodeId
        );
        return IsPortfolioHardSafe(after, evidence.ActiveParty)
            || IsIncrementalOpposedFamilyRepair(
                portfolio.CurrentCodes,
                after,
                candidate.Family,
                removal.CodeId,
                targetFamily,
                settings,
                evidence
            );
    }

    private static int ResearchRemovalPriority(
        NetherCodeState code,
        NetherCodeFamily targetFamily,
        NetherCodePolicyEvidence evidence
    )
    {
        if (evidence.HardExcludedCodeIds.Contains(code.CodeId))
            return 4;
        if (code.Family == Opposing(targetFamily))
            return 3;
        if (evidence.ProvablySurplusCompletedCodeIds.Contains(code.CodeId))
            return 1;
        return 2;
    }

    private static EquipmentValueChoice? FindResearchSameFamilySwap(
        NetherCodePortfolio portfolio,
        IReadOnlyList<NetherCodeCandidate> candidates,
        NetherCodeFamily targetFamily,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence
    )
    {
        var valuePolicy = new NetherEquipmentCodeValuePolicy();
        EquipmentValueChoice? best = null;
        foreach (NetherCodeCandidate candidate in candidates)
        {
            foreach (NetherCodeState removal in portfolio.CurrentCodes
                         .Where(code => code.Family == targetFamily)
                         .OrderBy(code => code.CodeId))
            {
                IReadOnlyList<NetherCodeState> after = ApplyDecision(
                    portfolio.CurrentCodes,
                    candidate,
                    removal.CodeId
                );
                if (!IsPortfolioHardSafe(after, evidence.ActiveParty))
                    continue;
                if (!evidence.EquipmentMutationValuesByKey.TryGetValue(
                        new NetherCodeMutationKey(candidate.CodeId, removal.CodeId),
                        out NetherCodeEquipmentMutationEvidence? mutation
                    )
                    || mutation == null)
                {
                    continue;
                }
                NetherEquipmentMutationValue value = valuePolicy.Evaluate(mutation);
                if (!value.CanSelect)
                    continue;
                var choice = new EquipmentValueChoice(candidate, removal.CodeId, value, 0);
                if (best == null || CompareEquipmentChoice(choice, best.Value, valuePolicy) > 0)
                    best = choice;
            }
        }
        return best;
    }

    private static int GetEquipmentRemovalPriority(
        NetherCodePortfolio portfolio,
        NetherCodeCandidate candidate,
        long removalCodeId,
        NetherCodePolicyEvidence evidence
    )
    {
        if (removalCodeId <= 0)
            return 0;
        NetherCodeState? removal = portfolio.CurrentCodes
            .FirstOrDefault(code => code.CodeId == removalCodeId);
        if (removal == null)
            return 0;
        if (evidence.HardExcludedCodeIds.Contains(removalCodeId))
            return 3;
        if (removal.Family == Opposing(candidate.Family))
            return 2;
        return 1;
    }

    private static bool IsHardEligible(
        NetherCodeCandidate candidate,
        NetherCodePortfolio portfolio,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence,
        NetherCodeFamily effectiveResearchFamily
    )
    {
        if (!evidence.MechanicsByCodeId.TryGetValue(
                candidate.CodeId,
                out NetherCodeHardEligibilityEvidence? mechanic
            )
            || mechanic == null
            || !mechanic.IsKnown)
        {
            return false;
        }
        if (evidence.MechanismValuesByCodeId == null
            || !evidence.MechanismValuesByCodeId.TryGetValue(
                candidate.CodeId,
                out NetherMechanismValue mechanismValue
            )
            || mechanismValue.Kind == NetherCombatValueEvidenceKind.Missing)
        {
            return false;
        }

        if (mechanic.RiskRule is NetherCodeRiskRule.MinimumErosionSeventy
            or NetherCodeRiskRule.AdverseErosionAdjustment)
        {
            return false;
        }
        if (mechanic.RiskRule == NetherCodeRiskRule.ConditionalFiftyToSeventy
            && (!evidence.ErosionHorizonKnown
                || evidence.ProjectedMinimumErosion < 50
                || evidence.ProjectedMaximumErosion > 70
                || !evidence.RecoverableToFiftySeventyBand))
        {
            return false;
        }
        if (mechanic.ResearchRateOverwrite > 0)
        {
            if (settings.StrategyMode != NetherStrategyMode.Research
                || effectiveResearchFamily == NetherCodeFamily.Unknown
                || candidate.Family != effectiveResearchFamily
                || evidence.Research == null)
            {
                return false;
            }
            NetherStrategyResearchFamilyState[] matchingFamilies = evidence.Research
                .Where(row => row.Family == candidate.Family)
                .ToArray();
            if (matchingFamilies.Length != 1
                || mechanic.ResearchRateOverwrite
                    <= matchingFamilies[0].TechnologyResearchRate)
                return false;
        }

        NetherCodeFamily opposing = Opposing(candidate.Family);
        bool hasCandidateFamily = portfolio.CurrentCodes.Any(code => code.Family == candidate.Family);
        bool hasOpposing = portfolio.CurrentCodes.Any(code => code.Family == opposing);
        // Research must preserve the currently active family while its target is incomplete.
        // Equipment is different: its native retention evidence ranks the *complete retained
        // portfolio*, so rejecting here would prevent DecideEquipment from evaluating the legal
        // candidate/removal pair that removes the opposing family. Hard exclusions and the final
        // complete-portfolio safety/value checks still apply below and in DecideEquipment.
        if (settings.StrategyMode == NetherStrategyMode.Research
            && hasOpposing && hasCandidateFamily
            && ResolveRetainedFamily(
                portfolio,
                settings,
                evidence,
                candidate.Family,
                opposing,
                effectiveResearchFamily
            )
                != candidate.Family)
        {
            return false;
        }

        if (mechanic.UniformCrestTargetRow == NetherCodeTargetRow.None)
            return true;
        if (mechanic.UniformCrestFamily is not (
                NetherCodeFamily.Rush or NetherCodeFamily.Impact
            )
            || evidence.ActiveParty == null)
        {
            return false;
        }

        NetherCrestIdentity requiredCrest = CrestForFamily(mechanic.UniformCrestFamily);
        NetherStrategyPartyMember[] recipients = evidence.ActiveParty
            .Where(member => member != null && member.IsAlive)
            .Where(member => mechanic.UniformCrestTargetRow switch
            {
                NetherCodeTargetRow.Forward => PartyPositionOf(member) == NetherPartyPosition.Forward,
                NetherCodeTargetRow.Back => PartyPositionOf(member) == NetherPartyPosition.Back,
                NetherCodeTargetRow.All => PartyPositionOf(member) != NetherPartyPosition.Unknown,
                _ => false,
            })
            .ToArray();
        return requiredCrest != NetherCrestIdentity.Unknown
            && recipients.Length > 0
            && recipients.All(member => CrestOf(member) == requiredCrest);
    }

    private static NetherCodeFamily Opposing(NetherCodeFamily family) => family switch
    {
        NetherCodeFamily.Rush => NetherCodeFamily.Impact,
        NetherCodeFamily.Impact => NetherCodeFamily.Rush,
        NetherCodeFamily.Safe => NetherCodeFamily.Risk,
        NetherCodeFamily.Risk => NetherCodeFamily.Safe,
        _ => NetherCodeFamily.Unknown,
    };

    private static NetherCodeFamily ResolveRetainedFamily(
        NetherCodePortfolio portfolio,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence,
        NetherCodeFamily first,
        NetherCodeFamily second,
        NetherCodeFamily researchTargetFamily
    )
    {
        if (settings.StrategyMode == NetherStrategyMode.Research)
        {
            NetherCodeFamily targetFamily = researchTargetFamily == NetherCodeFamily.Unknown
                ? evidence.ActiveResearchFamily
                : researchTargetFamily;
            return targetFamily == first
                ? first
                : targetFamily == second
                    ? second
                    : NetherCodeFamily.Unknown;
        }

        NetherOpposedFamilyPair? pair = PairOf(first, second);
        if (pair == null
            || evidence.FamilyRetentionByPair == null
            || !evidence.FamilyRetentionByPair.TryGetValue(
                pair.Value,
                out NetherFamilyRetentionEvidence? retention
            )
            || retention == null
            || !retention.IsKnown
            || retention.PreferredFamily is NetherCodeFamily.Unknown)
        {
            return NetherCodeFamily.Unknown;
        }
        return retention.PreferredFamily is var preferred
            && (preferred == first || preferred == second)
                ? preferred
                : NetherCodeFamily.Unknown;
    }

    private static NetherOpposedFamilyPair? PairOf(
        NetherCodeFamily first,
        NetherCodeFamily second
    )
    {
        if (first is NetherCodeFamily.Rush or NetherCodeFamily.Impact
            && second is NetherCodeFamily.Rush or NetherCodeFamily.Impact
            && first != second)
            return NetherOpposedFamilyPair.RushImpact;
        if (first is NetherCodeFamily.Safe or NetherCodeFamily.Risk
            && second is NetherCodeFamily.Safe or NetherCodeFamily.Risk
            && first != second)
            return NetherOpposedFamilyPair.SafeRisk;
        return null;
    }

    private static bool IsIncrementalOpposedFamilyRepair(
        IReadOnlyList<NetherCodeState> before,
        IReadOnlyList<NetherCodeState> after,
        NetherCodeFamily candidateFamily,
        long removeCodeId,
        NetherCodeFamily researchTargetFamily,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence
    )
    {
        NetherCodeFamily opposing = Opposing(candidateFamily);
        if (opposing == NetherCodeFamily.Unknown || removeCodeId <= 0)
            return false;
        NetherCodeFamily retained = ResolveRetainedFamily(
            new NetherCodePortfolio { CurrentCodes = before },
            settings,
            evidence,
            candidateFamily,
            opposing,
            researchTargetFamily
        );
        if (retained != candidateFamily)
            return false;

        NetherCodeState? removed = PositiveDistinct(before)
            .SingleOrDefault(code => code.CodeId == removeCodeId);
        if (removed == null || removed.Family != opposing)
            return false;
        int beforeOpposing = PositiveDistinct(before).Count(code => code.Family == opposing);
        int afterOpposing = PositiveDistinct(after).Count(code => code.Family == opposing);
        int beforeRetained = PositiveDistinct(before).Count(code => code.Family == retained);
        int afterRetained = PositiveDistinct(after).Count(code => code.Family == retained);
        if (beforeOpposing <= 0
            || afterOpposing != beforeOpposing - 1
            || afterRetained < beforeRetained)
        {
            return false;
        }

        // Incremental repair may leave more losing-side Codes for a later offer, but it may not
        // create an incompatible category-five crest threshold while doing so.
        return !TryGetIncompatibleThresholdFamily(after, evidence.ActiveParty, out _);
    }

    private static bool IsEquipmentMutationLegal(
        IReadOnlyList<NetherCodeState> before,
        IReadOnlyList<NetherCodeState> after,
        NetherCodeFamily candidateFamily,
        long removeCodeId,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence
    )
    {
        // Category-five crest compatibility remains an absolute hard gate. Actual Combat Value is
        // evaluated below for the complete retained portfolio, including a portfolio that was
        // already mixed before this replacement.
        if (TryGetIncompatibleThresholdFamily(after, evidence.ActiveParty, out _))
            return false;

        NetherCodeFamily opposing = Opposing(candidateFamily);
        if (opposing == NetherCodeFamily.Unknown)
            return true;

        bool hadCandidateFamily = PositiveDistinct(before).Any(code => code.Family == candidateFamily);
        bool hadOpposingFamily = PositiveDistinct(before).Any(code => code.Family == opposing);
        if (!hadOpposingFamily)
            return true;

        NetherCodeFamily retainedFamily = ResolveRetainedFamily(
                new NetherCodePortfolio { CurrentCodes = before },
                settings,
                evidence,
                candidateFamily,
                opposing,
                NetherCodeFamily.Unknown
            );
        if (retainedFamily == NetherCodeFamily.Unknown)
        {
            return false;
        }
        if (hadCandidateFamily)
            return true;

        // An Equipment offer may repair an all-opposing portfolio, but it may not intentionally
        // add a new family while removing an unrelated code. Existing mixed portfolios are legal
        // inputs: their candidate/removal pair must still reach the native complete-portfolio
        // valuation seam rather than being rejected by family retention first.
        NetherCodeState? removed = PositiveDistinct(before)
            .SingleOrDefault(code => code.CodeId == removeCodeId);
        return removed?.Family == opposing
            && !PositiveDistinct(after).Any(code => code.Family == opposing);
    }

    private static IEnumerable<NetherCodeState> PositiveDistinct(
        IEnumerable<NetherCodeState> codes
    ) => codes
        .Where(code => code != null && code.PossessionAmount > 0)
        .GroupBy(code => code.CodeId)
        .Select(group => group.First());

    private static bool TryGetIncompatibleThresholdFamily(
        IReadOnlyList<NetherCodeState> codes,
        IReadOnlyList<NetherStrategyPartyMember>? party,
        out NetherCodeFamily family
    )
    {
        NetherCodeEffectiveLevels effective = CalculateEffectiveLevels(codes);
        if (effective.Rush >= 5 && !EveryActiveCharacterMatches(party, NetherCodeFamily.Rush))
        {
            family = NetherCodeFamily.Rush;
            return true;
        }
        if (effective.Impact >= 5 && !EveryActiveCharacterMatches(party, NetherCodeFamily.Impact))
        {
            family = NetherCodeFamily.Impact;
            return true;
        }
        family = NetherCodeFamily.Unknown;
        return false;
    }

    private static bool EveryActiveCharacterMatches(
        IReadOnlyList<NetherStrategyPartyMember>? party,
        NetherCodeFamily family
    )
    {
        if (party == null || family is not (NetherCodeFamily.Rush or NetherCodeFamily.Impact))
            return false;
        NetherStrategyPartyMember[] active = party
            .Where(member => member != null && member.IsAlive)
            .ToArray();
        NetherCrestIdentity requiredCrest = CrestForFamily(family);
        return requiredCrest != NetherCrestIdentity.Unknown
            && active.Length > 0
            && active.All(member => CrestOf(member) == requiredCrest);
    }

    private static NetherCrestIdentity CrestForFamily(NetherCodeFamily family) => family switch
    {
        NetherCodeFamily.Rush => NetherCrestIdentity.Passion,
        NetherCodeFamily.Impact => NetherCrestIdentity.Impact,
        _ => NetherCrestIdentity.Unknown,
    };

    private static NetherCrestIdentity CrestOf(NetherStrategyPartyMember member) => member.Crest;

    private static NetherPartyPosition PartyPositionOf(NetherStrategyPartyMember member) =>
        member.PartyPosition;

    private static NetherCodeDecision TryRepairThresholdPortfolio(
        NetherCodePortfolio portfolio,
        IReadOnlyList<NetherCodeCandidate> eligible,
        NetherAutoClimbSettings settings,
        NetherCodePolicyEvidence evidence,
        NetherCodeFamily incompatibleFamily,
        NetherCombatLane lane
    )
    {
        if (portfolio.CurrentCodes.Count < portfolio.Capacity)
        {
            return Pause(
                NetherPauseReason.UnknownMasterData,
                "incompatible-category-five-requires-replacement"
            );
        }

        foreach (NetherCodeCandidate candidate in eligible.OrderBy(candidate => candidate.CodeId))
        {
            foreach (NetherCodeState removal in PositiveDistinct(portfolio.CurrentCodes)
                         .Where(code => code.Family == incompatibleFamily)
                         .OrderBy(code => code.CodeId))
            {
                IReadOnlyList<NetherCodeState> after = ApplyDecision(
                    portfolio.CurrentCodes,
                    candidate,
                    removal.CodeId
                );
                NetherCodeEffectiveLevels effective = CalculateEffectiveLevels(after);
                int repairedCount = incompatibleFamily == NetherCodeFamily.Rush
                    ? effective.Rush
                    : effective.Impact;
                if (repairedCount < 5 && IsPortfolioHardSafe(after, evidence.ActiveParty))
                {
                    return Select(
                        candidate,
                        removal.CodeId,
                        lane,
                        new[] { removal.CodeId }
                    ) with
                    {
                        Detail = "repair-incompatible-category-five",
                    };
                }
            }
        }

        return Pause(
            NetherPauseReason.UnknownMasterData,
            "incompatible-category-five-no-proven-repair"
        );
    }

    private static IReadOnlyList<NetherCodeState> ApplyDecision(
        IReadOnlyList<NetherCodeState> current,
        NetherCodeCandidate candidate,
        long removeCodeId
    )
    {
        NetherCodeState selected = new(candidate.CodeId, candidate.Family, candidate.AbilityLevel)
        {
            IsKnown = candidate.IsKnown,
            EffectSemanticsKnown = candidate.EffectSemanticsKnown,
            Category = candidate.Category,
            Rarity = candidate.Rarity,
            Power = candidate.Power,
            PossessionAmount = 1,
            MasterEffectType = candidate.MasterEffectType,
            EffectParameter1 = candidate.EffectParameter1,
            EffectParameter2 = candidate.EffectParameter2,
            EffectParameter3 = candidate.EffectParameter3,
            AbilityAssetId = candidate.AbilityAssetId,
            PartyCoverageKnown = candidate.PartyCoverageKnown,
            PartyCoverage = candidate.PartyCoverage,
        };
        return current
            .Where(code => removeCodeId <= 0 || code.CodeId != removeCodeId)
            .Append(selected)
            .ToArray();
    }

    private static bool IsPortfolioHardSafe(
        IReadOnlyList<NetherCodeState> codes,
        IReadOnlyList<NetherStrategyPartyMember>? party
    )
    {
        NetherCodeState[] distinct = PositiveDistinct(codes).ToArray();
        bool familySafe = !(distinct.Any(code => code.Family == NetherCodeFamily.Rush)
                && distinct.Any(code => code.Family == NetherCodeFamily.Impact))
            && !(distinct.Any(code => code.Family == NetherCodeFamily.Safe)
                && distinct.Any(code => code.Family == NetherCodeFamily.Risk));
        if (!familySafe)
            return false;

        NetherCodeEffectiveLevels effective = CalculateEffectiveLevels(distinct);
        return (effective.Rush < 5 || EveryActiveCharacterMatches(party, NetherCodeFamily.Rush))
            && (effective.Impact < 5 || EveryActiveCharacterMatches(party, NetherCodeFamily.Impact));
    }

    public static NetherCodeEffectiveLevels CalculateEffectiveLevels(IReadOnlyList<NetherCodeState> codes)
    {
        if (codes == null)
            throw new ArgumentNullException(nameof(codes));
        return CalculateEffectiveLevels(codes
            .Where(code => code != null && code.PossessionAmount > 0)
            .GroupBy(code => code.CodeId)
            .Select(group => group.First().Family));
    }

    internal static NetherCodeEffectiveLevels CalculateEffectiveLevels(IEnumerable<NetherCodeFamily> families)
    {
        NetherCodeFamily[] all = families as NetherCodeFamily[] ?? families.ToArray();
        int safe = all.Count(family => family == NetherCodeFamily.Safe);
        int risk = all.Count(family => family == NetherCodeFamily.Risk);
        int rush = all.Count(family => family == NetherCodeFamily.Rush);
        int impact = all.Count(family => family == NetherCodeFamily.Impact);
        return new NetherCodeEffectiveLevels(
            Math.Max(0, safe - risk),
            Math.Max(0, risk - safe),
            Math.Max(0, rush - impact),
            Math.Max(0, impact - rush)
        );
    }

    private static bool IsValid(
        NetherCodePortfolio portfolio,
        IReadOnlyList<NetherCodeCandidate> candidates
    ) => portfolio.IsMasterComplete
        && portfolio.Capacity > 0
        && portfolio.ReloadCount >= 0
        && portfolio.CurrentCodes.Count <= portfolio.Capacity
        && portfolio.CurrentCodes.Select(code => code.CodeId).Distinct().Count() == portfolio.CurrentCodes.Count
        && portfolio.CurrentCodes.All(IsValid)
        && candidates.All(IsValid);

    private static bool IsValid(NetherCodeState code) => code != null
        && code.IsKnown
        && code.CodeId > 0
        && code.Family != NetherCodeFamily.Unknown
        && code.AbilityLevel >= 0
        && code.Rarity >= 0
        && code.Power >= 0
        && code.PossessionAmount >= 0
        && (!code.PartyCoverageKnown || code.PartyCoverage >= 0);

    private static bool IsValid(NetherCodeCandidate code) => code != null
        && code.IsKnown
        && code.CodeId > 0
        && code.Family != NetherCodeFamily.Unknown
        && code.AbilityLevel >= 0
        && code.Rarity >= 0
        && code.Power >= 0
        && (!code.PartyCoverageKnown || code.PartyCoverage >= 0);

    private static NetherCombatLane ResolveLane(
        NetherCodePortfolio portfolio,
        NetherCombatLane configured
    )
    {
        if (configured != NetherCombatLane.Auto)
            return configured;
        // A previous offer decision and the native paired-card counter do not prove party
        // composition. Auto resolves only from a complete native UI Scope projection for every
        // currently held Rush/Impact card. One unknown value keeps the lane neutral.
        NetherCodeCandidate[] laneEvidence = portfolio.CurrentCodes
            .Select(ToCandidateView)
            .Where(code => ToLane(code.Family) != null)
            .ToArray();
        if (laneEvidence.Length == 0 || laneEvidence.Any(code => !code.PartyCoverageKnown))
            return NetherCombatLane.Auto;

        int rushCoverage = Coverage(laneEvidence, NetherCombatLane.Rush);
        int impactCoverage = Coverage(laneEvidence, NetherCombatLane.Impact);
        if (rushCoverage == impactCoverage)
            return NetherCombatLane.Auto;
        return rushCoverage > impactCoverage ? NetherCombatLane.Rush : NetherCombatLane.Impact;
    }

    private static int Coverage(
        IEnumerable<NetherCodeCandidate> codes,
        NetherCombatLane lane
    ) => codes.Where(code => ToLane(code.Family) == lane)
        .Sum(code => code.PartyCoverage);

    private static NetherCombatLane? ToLane(NetherCodeFamily family) => family switch
    {
        NetherCodeFamily.Rush => NetherCombatLane.Rush,
        NetherCodeFamily.Impact => NetherCombatLane.Impact,
        _ => null,
    };

    private static NetherCodeCandidate ToCandidateView(NetherCodeState code) => new(
        code.CodeId,
        code.Family,
        code.AbilityLevel
    )
    {
        IsKnown = code.IsKnown,
        EffectSemanticsKnown = code.EffectSemanticsKnown,
        Category = code.Category,
        Rarity = code.Rarity,
        Power = code.Power,
        MasterEffectType = code.MasterEffectType,
        EffectParameter1 = code.EffectParameter1,
        EffectParameter2 = code.EffectParameter2,
        EffectParameter3 = code.EffectParameter3,
        AbilityAssetId = code.AbilityAssetId,
        PartyCoverageKnown = code.PartyCoverageKnown,
        PartyCoverage = code.PartyCoverage,
    };

    private static NetherCodeDecision Select(
        NetherCodeCandidate candidate,
        long removal,
        NetherCombatLane lane,
        IReadOnlyList<long> removable
    ) => new()
    {
        Kind = NetherCodeDecisionKind.Select,
        SelectedCodeId = candidate.CodeId,
        RemoveCodeId = removal,
        LockedLane = lane,
        RemovableCodeIds = removable,
        Detail = "native-category-counts;optional-native-ui-display-coverage",
    };

    private static NetherCodeDecision ReloadOrKeep(
        NetherCodePortfolio portfolio,
        NetherAutoClimbSettings settings,
        NetherCombatLane lane,
        string detail,
        IReadOnlyList<long>? removable = null,
        bool forceReload = false
    ) => (forceReload ? portfolio.ReloadCount > 0 : portfolio.ReloadCount > settings.CodeReloadReserve)
        ? new NetherCodeDecision
        {
            Kind = NetherCodeDecisionKind.Reload,
            LockedLane = lane,
            RemovableCodeIds = removable ?? Array.Empty<long>(),
            Detail = detail + ":reload",
        }
        : new NetherCodeDecision
        {
            Kind = NetherCodeDecisionKind.Keep,
            LockedLane = lane,
            RemovableCodeIds = removable ?? Array.Empty<long>(),
            Detail = detail + ":keep",
        };

    private static NetherCodeDecision Pause(NetherPauseReason reason, string detail) => new()
    {
        Kind = NetherCodeDecisionKind.Pause,
        PauseReason = reason,
        Detail = detail,
    };

    private readonly record struct EquipmentValueChoice(
        NetherCodeCandidate Candidate,
        long RemoveCodeId,
        NetherEquipmentMutationValue Value,
        int RemovalPriority
    );

}
