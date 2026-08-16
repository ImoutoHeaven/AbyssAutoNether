#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal readonly record struct NetherCodeMutationKey(long CandidateCodeId, long RemoveCodeId);

internal enum NetherEquipmentCombatTier
{
    None = 0,
    FrontFallback,
    RearOrFullNonessentialDefense,
    RearOrFullOffense,
    BackForceChain,
    SurvivalRepair,
}

/// <summary>
/// Single strategy-tier authority for typed native metrics, special outcomes, and qualitative
/// mechanisms. Callers must first preserve the exact recipient; this classifier never infers one
/// from a candidate-wide or family-wide maximum.
/// </summary>
internal static class NetherEquipmentCombatTierClassifier
{
    public static NetherEquipmentCombatTier ForMetric(
        NetherCombatMetricKind metric,
        NetherPartyPosition position
    )
    {
        if (position == NetherPartyPosition.Forward)
            return NetherEquipmentCombatTier.FrontFallback;
        if (position is not (NetherPartyPosition.Back or NetherPartyPosition.Assist))
            return NetherEquipmentCombatTier.None;
        return metric is NetherCombatMetricKind.Attack
                or NetherCombatMetricKind.DamageModifier
                or NetherCombatMetricKind.ElementDamage
                or NetherCombatMetricKind.CriticalProbability
                or NetherCombatMetricKind.ContinuousAttackProbability
            ? NetherEquipmentCombatTier.RearOrFullOffense
            : metric is NetherCombatMetricKind.Defence
                or NetherCombatMetricKind.MaxHp
                or NetherCombatMetricKind.TakenDamage
                or NetherCombatMetricKind.Resistance
                ? NetherEquipmentCombatTier.RearOrFullNonessentialDefense
                : NetherEquipmentCombatTier.None;
    }

    public static NetherEquipmentCombatTier ForMetric(
        NetherCombatMetricKind metric,
        IEnumerable<NetherPartyPosition> positions
    )
    {
        NetherPartyPosition[] rows = positions?.ToArray() ?? Array.Empty<NetherPartyPosition>();
        if (rows.Length == 0)
            return NetherEquipmentCombatTier.None;
        NetherEquipmentCombatTier[] tiers = rows.Select(position => ForMetric(metric, position))
            .ToArray();
        return tiers.Any(tier => tier == NetherEquipmentCombatTier.None)
            ? NetherEquipmentCombatTier.None
            : tiers.Max();
    }

    public static NetherEquipmentCombatTier ForSpecial(
        NetherNativeSpecialComparisonKind kind,
        NetherPartyPosition position
    ) => kind switch
    {
        NetherNativeSpecialComparisonKind.CriticalProbability =>
            ForMetric(NetherCombatMetricKind.CriticalProbability, position),
        NetherNativeSpecialComparisonKind.ContinuousAttackProbability =>
            ForMetric(NetherCombatMetricKind.ContinuousAttackProbability, position),
        NetherNativeSpecialComparisonKind.DefenseEffectiveHp =>
            ForMetric(NetherCombatMetricKind.Defence, position),
        _ => NetherEquipmentCombatTier.None,
    };

    public static NetherEquipmentCombatTier ForQualitative(
        NetherMechanismQualitativePriority priority
    ) => priority switch
    {
        NetherMechanismQualitativePriority.BackForceChainHigh =>
            NetherEquipmentCombatTier.BackForceChain,
        NetherMechanismQualitativePriority.FrontForceChainFallback =>
            NetherEquipmentCombatTier.FrontFallback,
        _ => NetherEquipmentCombatTier.None,
    };
}

internal readonly record struct NetherSurvivalRepairEvidence(
    bool IsKnown,
    bool HasDeficit,
    bool RepairsDeficit,
    string UnknownReason
)
{
    public static NetherSurvivalRepairEvidence Unknown => UnknownFor(
        hasDeficit: false,
        "survival-deficit-evidence-unavailable"
    );

    public static NetherSurvivalRepairEvidence UnknownFor(bool hasDeficit, string reason) =>
        new(
            IsKnown: false,
            HasDeficit: hasDeficit,
            RepairsDeficit: false,
            UnknownReason: string.IsNullOrWhiteSpace(reason)
                ? "survival-repair-evidence-unavailable"
                : reason
        );

    public static NetherSurvivalRepairEvidence Known(bool hasDeficit, bool repairsDeficit) =>
        new(true, hasDeficit, repairsDeficit, string.Empty);
}

internal enum NetherNativeSpecialComparisonKind
{
    None = 0,
    CriticalProbability,
    ContinuousAttackProbability,
    DefenseEffectiveHp,
}

internal readonly record struct NetherCharacterProbabilityEvidence(
    long CharacterId,
    int BeforeProbabilityPermille,
    int AfterProbabilityPermille,
    int LiveMaximumCount,
    NetherPartyPosition PartyPosition
);

internal readonly record struct NetherNativeSpecialOutcomeIdentity(
    NetherNativeSpecialComparisonKind Kind,
    long CharacterId,
    NetherPartyPosition PartyPosition
);

/// <summary>
/// Exact native inputs whose units/control flow differ from ordinary permille-duration windows.
/// The comparison kind is retained so only commensurate alternatives can compare magnitudes.
/// </summary>
internal sealed record NetherNativeSpecialComparisonEvidence
{
    public NetherNativeSpecialComparisonKind Kind { get; init; }
    public int BeforeProbabilityPermille { get; init; }
    public int AfterProbabilityPermille { get; init; }
    public int LiveMaximumCount { get; init; }
    public IReadOnlyList<NetherCharacterProbabilityEvidence> ProbabilityRows { get; init; } =
        Array.Empty<NetherCharacterProbabilityEvidence>();
    public IReadOnlyList<NetherCharacterEffectiveHpEvidence> DefenseRows { get; init; } =
        Array.Empty<NetherCharacterEffectiveHpEvidence>();

    public static NetherNativeSpecialComparisonEvidence None { get; } = new();

    public static NetherNativeSpecialComparisonEvidence Critical(
        int beforePermille,
        int afterPermille
    ) => new()
    {
        Kind = NetherNativeSpecialComparisonKind.CriticalProbability,
        BeforeProbabilityPermille = beforePermille,
        AfterProbabilityPermille = afterPermille,
        ProbabilityRows =
        [
            new NetherCharacterProbabilityEvidence(
                1,
                beforePermille,
                afterPermille,
                0,
                NetherPartyPosition.Back
            ),
        ],
    };

    public static NetherNativeSpecialComparisonEvidence Critical(
        IReadOnlyList<NetherCharacterProbabilityEvidence> rows
    ) => new()
    {
        Kind = NetherNativeSpecialComparisonKind.CriticalProbability,
        ProbabilityRows = rows ?? Array.Empty<NetherCharacterProbabilityEvidence>(),
    };

    public static NetherNativeSpecialComparisonEvidence Continuous(
        int beforePermille,
        int afterPermille,
        int liveMaximumCount
    ) => new()
    {
        Kind = NetherNativeSpecialComparisonKind.ContinuousAttackProbability,
        BeforeProbabilityPermille = beforePermille,
        AfterProbabilityPermille = afterPermille,
        LiveMaximumCount = liveMaximumCount,
        ProbabilityRows =
        [
            new NetherCharacterProbabilityEvidence(
                1,
                beforePermille,
                afterPermille,
                liveMaximumCount,
                NetherPartyPosition.Back
            ),
        ],
    };

    public static NetherNativeSpecialComparisonEvidence Continuous(
        IReadOnlyList<NetherCharacterProbabilityEvidence> rows
    ) => new()
    {
        Kind = NetherNativeSpecialComparisonKind.ContinuousAttackProbability,
        ProbabilityRows = rows ?? Array.Empty<NetherCharacterProbabilityEvidence>(),
    };

    public static NetherNativeSpecialComparisonEvidence Defense(
        IReadOnlyList<NetherCharacterEffectiveHpEvidence> rows
    ) => new()
    {
        Kind = NetherNativeSpecialComparisonKind.DefenseEffectiveHp,
        DefenseRows = rows ?? Array.Empty<NetherCharacterEffectiveHpEvidence>(),
    };
}

internal sealed record NetherCodeEquipmentMutationEvidence(
    long CandidateCodeId,
    long RemoveCodeId,
    NetherNativePortfolioComparisonInput NativePortfolio,
    NetherMechanismValue MechanismValue
)
{
    public NetherEquipmentCombatTier CombatTier { get; init; }
    public NetherEquipmentCombatTier RemovedCombatTier { get; init; }
    public NetherSurvivalRepairEvidence Survival { get; init; }
    public NetherNativeSpecialComparisonEvidence NativeComparison { get; init; } =
        NetherNativeSpecialComparisonEvidence.None;
    public IReadOnlyList<NetherNativeSpecialComparisonEvidence> NativeComparisons { get; init; } =
        Array.Empty<NetherNativeSpecialComparisonEvidence>();
    public NetherMechanismPortfolioComparisonEvidence MechanismPortfolio { get; init; } =
        NetherMechanismPortfolioComparisonEvidence.Unknown(
            "complete-mechanism-portfolio-unavailable"
        );
    public IReadOnlyDictionary<long, NetherPartyPosition> RecipientPositions { get; init; } =
        new Dictionary<long, NetherPartyPosition>();
}

internal readonly record struct NetherMechanismPortfolioEntry(
    long CodeId,
    NetherMechanismValue Value
);

/// <summary>
/// Complete typed before/after portfolio for native mechanisms that are not ordinary buff
/// windows. Entries retain their native unit tags; the evaluator never sums unlike units.
/// </summary>
internal sealed record NetherMechanismPortfolioComparisonEvidence
{
    public bool IsKnown { get; init; }
    public IReadOnlyList<NetherMechanismPortfolioEntry> Before { get; init; } =
        Array.Empty<NetherMechanismPortfolioEntry>();
    public IReadOnlyList<NetherMechanismPortfolioEntry> After { get; init; } =
        Array.Empty<NetherMechanismPortfolioEntry>();
    public string Detail { get; init; } = string.Empty;

    public static NetherMechanismPortfolioComparisonEvidence Known(
        IReadOnlyList<NetherMechanismPortfolioEntry> before,
        IReadOnlyList<NetherMechanismPortfolioEntry> after
    ) => new()
    {
        IsKnown = true,
        Before = before ?? throw new ArgumentNullException(nameof(before)),
        After = after ?? throw new ArgumentNullException(nameof(after)),
    };

    public static NetherMechanismPortfolioComparisonEvidence Unknown(string detail) => new()
    {
        Detail = string.IsNullOrWhiteSpace(detail)
            ? "complete-mechanism-portfolio-unavailable"
            : detail,
    };
}

internal enum NetherCompletePortfolioPreference
{
    Unknown = 0,
    Left,
    Equal,
    Right,
}

internal readonly record struct NetherCompletePortfolioComparison(
    NetherCompletePortfolioPreference Preference,
    string Detail
);

internal enum NetherEquipmentMutationValueKind
{
    Missing = 0,
    ReachableUnquantified,
    NonPositive,
    StrictQuantifiedImprovement,
    QualitativeImprovement,
}

internal sealed record NetherEquipmentMutationValue(
    NetherEquipmentMutationValueKind Kind,
    NetherMechanismQualitativePriority QualitativePriority,
    NetherEquipmentCombatTier CombatTier,
    IReadOnlyDictionary<(long Recipient, NetherCombatMetricKind Metric), long> NativeMarginals,
    NetherMechanismQuantity MechanismQuantity,
    NetherNativeSpecialComparisonEvidence NativeComparison,
    string Detail
)
{
    public bool CanSelect => Kind is NetherEquipmentMutationValueKind.StrictQuantifiedImprovement
        or NetherEquipmentMutationValueKind.QualitativeImprovement;
    public IReadOnlyDictionary<NetherMechanismQuantityIdentity, decimal> MechanismMarginals
        { get; init; } = new Dictionary<NetherMechanismQuantityIdentity, decimal>();
    public IReadOnlyDictionary<NetherMechanismRecipientQuantityIdentity, decimal>
        MechanismRecipientMarginals { get; init; } =
            new Dictionary<NetherMechanismRecipientQuantityIdentity, decimal>();
    public IReadOnlyDictionary<NetherMechanismQualitativePriority, int> QualitativeMarginals
        { get; init; } = new Dictionary<NetherMechanismQualitativePriority, int>();
    public IReadOnlyDictionary<NetherNativeSpecialOutcomeIdentity, decimal> NativeSpecialMarginals
        { get; init; } = new Dictionary<NetherNativeSpecialOutcomeIdentity, decimal>();
    public IReadOnlyList<NetherNativeSpecialComparisonEvidence> NativeComparisons { get; init; } =
        Array.Empty<NetherNativeSpecialComparisonEvidence>();
}

/// <summary>
/// Deep comparison seam joining native retained-portfolio simulation with mechanism-specific
/// evidence. It proves only Pareto improvements or an approved Force Chain qualitative tier;
/// incomparable and reachable-unquantified outcomes never acquire an invented scalar.
/// </summary>
internal sealed class NetherEquipmentCodeValuePolicy
{
    private readonly NetherNativePortfolioValuation _portfolioValuation = new();

    public NetherCompletePortfolioComparison CompareCompletePortfolios(
        NetherNativePortfolioComparisonInput leftToRight,
        NetherMechanismPortfolioComparisonEvidence leftToRightMechanisms,
        IReadOnlyList<NetherNativeSpecialComparisonEvidence> leftToRightSpecials,
        IReadOnlyDictionary<long, NetherPartyPosition> recipientPositions
    )
    {
        if (leftToRight == null || leftToRightMechanisms == null
            || leftToRightSpecials == null || recipientPositions == null)
        {
            return new NetherCompletePortfolioComparison(
                NetherCompletePortfolioPreference.Unknown,
                "complete-retained-family-portfolio-unavailable"
            );
        }
        NetherNativePortfolioValue native = _portfolioValuation.EvaluateComparison(leftToRight);
        NetherMechanismPortfolioDelta mechanisms = EvaluateMechanismPortfolio(
            leftToRightMechanisms
        );
        var specialOutcomes = new Dictionary<NetherNativeSpecialOutcomeIdentity, decimal>();
        var specialKinds = new HashSet<NetherNativeSpecialComparisonKind>();
        foreach (NetherNativeSpecialComparisonEvidence special in leftToRightSpecials)
        {
            string specialError = "complete-retained-special-portfolio-unavailable";
            if (special == null
                || special.Kind == NetherNativeSpecialComparisonKind.None
                || !specialKinds.Add(special.Kind)
                || !TryEvaluateSpecial(
                    special,
                    out IReadOnlyDictionary<NetherNativeSpecialOutcomeIdentity, decimal> outcomes,
                    out specialError
                ))
            {
                return new NetherCompletePortfolioComparison(
                    NetherCompletePortfolioPreference.Unknown,
                    specialError
                );
            }
            foreach ((NetherNativeSpecialOutcomeIdentity identity, decimal marginal) in outcomes)
            {
                if (!specialOutcomes.TryAdd(identity, marginal))
                {
                    return new NetherCompletePortfolioComparison(
                        NetherCompletePortfolioPreference.Unknown,
                        "duplicate-native-special-recipient-outcome"
                    );
                }
            }
        }
        if (native.Kind is NetherCombatValueEvidenceKind.Missing
                or NetherCombatValueEvidenceKind.ReachableUnquantified
            || mechanisms.Kind is NetherCombatValueEvidenceKind.Missing
                or NetherCombatValueEvidenceKind.ReachableUnquantified)
        {
            return new NetherCompletePortfolioComparison(
                NetherCompletePortfolioPreference.Unknown,
                native.Kind is NetherCombatValueEvidenceKind.Missing
                    or NetherCombatValueEvidenceKind.ReachableUnquantified
                        ? native.Detail
                        : mechanisms.Detail
            );
        }

        NetherTieredOutcomeDirection direction = ResolveTieredOutcome(
            native.Exposures,
            recipientPositions,
            mechanisms,
            specialOutcomes,
            positiveUnscopedTier: NetherEquipmentCombatTier.None,
            negativeUnscopedTier: NetherEquipmentCombatTier.None,
            promotePositiveToSurvivalRepair: false,
            out _
        );
        if (direction == NetherTieredOutcomeDirection.Unknown)
        {
            return new NetherCompletePortfolioComparison(
                NetherCompletePortfolioPreference.Unknown,
                "complete-retained-family-portfolios-incomparable"
            );
        }
        return new NetherCompletePortfolioComparison(
            direction == NetherTieredOutcomeDirection.Positive
                ? NetherCompletePortfolioPreference.Right
                : direction == NetherTieredOutcomeDirection.Negative
                    ? NetherCompletePortfolioPreference.Left
                    : NetherCompletePortfolioPreference.Equal,
            "complete-typed-retained-family-portfolios"
        );
    }

    public NetherEquipmentMutationValue Evaluate(NetherCodeEquipmentMutationEvidence evidence)
    {
        if (evidence == null
            || evidence.CandidateCodeId <= 0
            || evidence.RemoveCodeId < 0
            || evidence.NativePortfolio == null)
        {
            return Missing("equipment-mutation-evidence-unavailable");
        }
        if (!evidence.Survival.IsKnown)
            return Missing(evidence.Survival.UnknownReason);
        if (evidence.Survival.HasDeficit && !evidence.Survival.RepairsDeficit)
            return NonPositive("candidate-does-not-repair-authoritative-survival-deficit");

        NetherNativePortfolioValue portfolio = _portfolioValuation.EvaluateComparison(
            evidence.NativePortfolio
        );
        NetherMechanismPortfolioDelta mechanismPortfolio = EvaluateMechanismPortfolio(
            evidence.MechanismPortfolio
        );
        if (portfolio.Kind == NetherCombatValueEvidenceKind.Missing
            || evidence.MechanismValue.Kind == NetherCombatValueEvidenceKind.Missing
            || mechanismPortfolio.Kind == NetherCombatValueEvidenceKind.Missing)
        {
            return Missing(portfolio.Kind == NetherCombatValueEvidenceKind.Missing
                ? portfolio.Detail
                : evidence.MechanismValue.Kind == NetherCombatValueEvidenceKind.Missing
                    ? evidence.MechanismValue.Detail
                    : mechanismPortfolio.Detail);
        }
        if (portfolio.Kind == NetherCombatValueEvidenceKind.ReachableUnquantified
            || evidence.MechanismValue.Kind == NetherCombatValueEvidenceKind.ReachableUnquantified
            || mechanismPortfolio.Kind == NetherCombatValueEvidenceKind.ReachableUnquantified)
        {
            return Unquantified(portfolio.Kind == NetherCombatValueEvidenceKind.ReachableUnquantified
                ? portfolio.Detail
                : evidence.MechanismValue.Kind == NetherCombatValueEvidenceKind.ReachableUnquantified
                    ? evidence.MechanismValue.Detail
                    : mechanismPortfolio.Detail);
        }

        NetherNativeSpecialComparisonEvidence[] specialComparisons = SpecialComparisons(evidence);
        var representedSpecials = specialComparisons.Select(row => row.Kind).ToHashSet();
        IReadOnlyDictionary<(long Recipient, NetherCombatMetricKind Metric), long> marginals =
            portfolio.Exposures
            .Where(exposure => !IsRepresentedBySpecial(
                exposure.Metric,
                representedSpecials
            ))
            .ToDictionary(
                exposure => (exposure.RecipientCharacterId, exposure.Metric),
                exposure => exposure.MarginalPermilleSeconds
            );
        var nativeSpecialMarginals =
            new Dictionary<NetherNativeSpecialOutcomeIdentity, decimal>();
        var nativeSpecialKinds = new HashSet<NetherNativeSpecialComparisonKind>();
        foreach (NetherNativeSpecialComparisonEvidence special in specialComparisons)
        {
            if (!nativeSpecialKinds.Add(special.Kind))
                return Unquantified("duplicate-native-special-comparison-kind");
            if (!TryEvaluateSpecial(
                    special,
                    out IReadOnlyDictionary<NetherNativeSpecialOutcomeIdentity, decimal> outcomes,
                    out string nativeSpecialError
                ))
            {
                return Unquantified(nativeSpecialError);
            }
            foreach ((NetherNativeSpecialOutcomeIdentity identity, decimal marginal) in outcomes)
            {
                if (!nativeSpecialMarginals.TryAdd(identity, marginal))
                    return Unquantified("duplicate-native-special-recipient-outcome");
            }
        }
        NetherEquipmentCombatTier candidateTier = evidence.Survival.HasDeficit
            ? NetherEquipmentCombatTier.SurvivalRepair
            : evidence.CombatTier;
        NetherTieredOutcomeDirection direction = ResolveTieredOutcome(
            portfolio.Exposures.Where(exposure => !IsRepresentedBySpecial(
                exposure.Metric,
                representedSpecials
            )).ToArray(),
            evidence.RecipientPositions,
            mechanismPortfolio,
            nativeSpecialMarginals,
            positiveUnscopedTier: candidateTier,
            negativeUnscopedTier: evidence.RemovedCombatTier,
            promotePositiveToSurvivalRepair: evidence.Survival.HasDeficit,
            out NetherEquipmentCombatTier effectiveTier
        );
        if (direction == NetherTieredOutcomeDirection.Unknown)
        {
            return Unquantified("same-tier-or-unknown-combat-portfolios-incomparable");
        }
        if (direction == NetherTieredOutcomeDirection.Negative)
        {
            return new NetherEquipmentMutationValue(
                NetherEquipmentMutationValueKind.NonPositive,
                NetherMechanismQualitativePriority.None,
                effectiveTier,
                marginals,
                evidence.MechanismValue.Quantity,
                evidence.NativeComparison,
                "retained-portfolio-has-negative-marginal"
            )
            {
                MechanismMarginals = mechanismPortfolio.QuantityMarginals,
                MechanismRecipientMarginals = mechanismPortfolio.RecipientQuantityMarginals,
                QualitativeMarginals = mechanismPortfolio.QualitativeMarginals,
                NativeSpecialMarginals = nativeSpecialMarginals,
                NativeComparisons = specialComparisons,
            };
        }

        if (evidence.MechanismValue.Kind == NetherCombatValueEvidenceKind.QualitativePriority
            && mechanismPortfolio.HasPositive)
        {
            if (evidence.MechanismValue.QualitativePriority is not (
                    NetherMechanismQualitativePriority.BackForceChainHigh
                    or NetherMechanismQualitativePriority.FrontForceChainFallback
                ))
            {
                return Unquantified("unsupported-qualitative-mechanism-priority");
            }
            return new NetherEquipmentMutationValue(
                NetherEquipmentMutationValueKind.QualitativeImprovement,
                evidence.MechanismValue.QualitativePriority,
                effectiveTier,
                marginals,
                default,
                evidence.NativeComparison,
                evidence.MechanismValue.Detail
            )
            {
                MechanismMarginals = mechanismPortfolio.QuantityMarginals,
                MechanismRecipientMarginals = mechanismPortfolio.RecipientQuantityMarginals,
                QualitativeMarginals = mechanismPortfolio.QualitativeMarginals,
                NativeSpecialMarginals = nativeSpecialMarginals,
                NativeComparisons = specialComparisons,
            };
        }

        return new NetherEquipmentMutationValue(
            direction == NetherTieredOutcomeDirection.Positive
                ? NetherEquipmentMutationValueKind.StrictQuantifiedImprovement
                : NetherEquipmentMutationValueKind.NonPositive,
            NetherMechanismQualitativePriority.None,
            effectiveTier,
            marginals,
            evidence.MechanismValue.Quantity,
            evidence.NativeComparison,
            direction == NetherTieredOutcomeDirection.Positive
                ? "strict-native-retained-portfolio-improvement"
                : "zero-native-retained-portfolio-marginal"
        )
        {
            MechanismMarginals = mechanismPortfolio.QuantityMarginals,
            MechanismRecipientMarginals = mechanismPortfolio.RecipientQuantityMarginals,
            QualitativeMarginals = mechanismPortfolio.QualitativeMarginals,
            NativeSpecialMarginals = nativeSpecialMarginals,
            NativeComparisons = specialComparisons,
        };
    }

    public int Compare(NetherEquipmentMutationValue left, NetherEquipmentMutationValue right)
    {
        int rank = Rank(left).CompareTo(Rank(right));
        if (rank != 0)
            return rank;
        if (!left.CanSelect || !right.CanSelect)
            return 0;

        Dictionary<NetherNativeSpecialComparisonKind, NetherNativeSpecialComparisonEvidence> leftSpecials =
            left.NativeComparisons.ToDictionary(row => row.Kind);
        Dictionary<NetherNativeSpecialComparisonKind, NetherNativeSpecialComparisonEvidence> rightSpecials =
            right.NativeComparisons.ToDictionary(row => row.Kind);
        var specialKinds = new HashSet<NetherNativeSpecialComparisonKind>(leftSpecials.Keys);
        specialKinds.UnionWith(rightSpecials.Keys);
        if (specialKinds.Any(kind =>
                !leftSpecials.ContainsKey(kind) || !rightSpecials.ContainsKey(kind)))
        {
            // Critical, continuous-attack, and ordinary parameter windows do not share a native
            // outcome unit at this lifecycle. A missing special dimension is therefore unknown,
            // not numeric zero; the caller applies only its deterministic CodeId tie-break.
            return 0;
        }
        NetherCompletePortfolioPreference specialPreference = CompareSpecialOfferOutcomes(
            left,
            right,
            specialKinds,
            leftSpecials,
            rightSpecials
        );
        if (specialPreference == NetherCompletePortfolioPreference.Unknown)
            return 0;
        if (specialPreference == NetherCompletePortfolioPreference.Left)
            return 1;
        if (specialPreference == NetherCompletePortfolioPreference.Right)
            return -1;

        bool leftBetter = false;
        bool rightBetter = false;
        var keys = new HashSet<(long Recipient, NetherCombatMetricKind Metric)>(
            left.NativeMarginals.Keys
        );
        keys.UnionWith(right.NativeMarginals.Keys);
        foreach ((long Recipient, NetherCombatMetricKind Metric) key in keys)
        {
            long leftValue = left.NativeMarginals.TryGetValue(key, out long leftMarginal)
                ? leftMarginal
                : 0;
            long rightValue = right.NativeMarginals.TryGetValue(key, out long rightMarginal)
                ? rightMarginal
                : 0;
            leftBetter |= leftValue > rightValue;
            rightBetter |= rightValue > leftValue;
        }
        var mechanismKinds = new HashSet<NetherMechanismQuantityIdentity>(
            left.MechanismMarginals.Keys
        );
        mechanismKinds.UnionWith(right.MechanismMarginals.Keys);
        foreach (NetherMechanismQuantityIdentity kind in mechanismKinds)
        {
            decimal leftValue = left.MechanismMarginals.TryGetValue(kind, out decimal leftMarginal)
                ? leftMarginal
                : 0;
            decimal rightValue = right.MechanismMarginals.TryGetValue(kind, out decimal rightMarginal)
                ? rightMarginal
                : 0;
            leftBetter |= leftValue > rightValue;
            rightBetter |= rightValue > leftValue;
        }
        var recipientMechanismKinds = new HashSet<NetherMechanismRecipientQuantityIdentity>(
            left.MechanismRecipientMarginals.Keys
        );
        recipientMechanismKinds.UnionWith(right.MechanismRecipientMarginals.Keys);
        foreach (NetherMechanismRecipientQuantityIdentity kind in recipientMechanismKinds)
        {
            decimal leftValue = left.MechanismRecipientMarginals.TryGetValue(
                kind,
                out decimal leftMarginal
            ) ? leftMarginal : 0;
            decimal rightValue = right.MechanismRecipientMarginals.TryGetValue(
                kind,
                out decimal rightMarginal
            ) ? rightMarginal : 0;
            leftBetter |= leftValue > rightValue;
            rightBetter |= rightValue > leftValue;
        }
        if (leftBetter == rightBetter)
            return 0;
        return leftBetter ? 1 : -1;
    }

    private static NetherMechanismPortfolioDelta EvaluateMechanismPortfolio(
        NetherMechanismPortfolioComparisonEvidence evidence
    )
    {
        if (evidence == null || !evidence.IsKnown
            || evidence.Before == null || evidence.After == null)
        {
            return NetherMechanismPortfolioDelta.Missing(
                evidence?.Detail ?? "complete-mechanism-portfolio-unavailable"
            );
        }
        if (!TryIndexMechanisms(evidence.Before, out Dictionary<long, NetherMechanismValue> before)
            || !TryIndexMechanisms(evidence.After, out Dictionary<long, NetherMechanismValue> after))
        {
            return NetherMechanismPortfolioDelta.Missing(
                "invalid-complete-mechanism-portfolio"
            );
        }

        foreach (long unchangedId in before.Keys.Intersect(after.Keys).ToArray())
        {
            if (!MechanismValuesEquivalent(before[unchangedId], after[unchangedId]))
            {
                return NetherMechanismPortfolioDelta.Unquantified(
                    "retained-mechanism-evidence-changed-without-replacement:"
                        + unchangedId
                );
            }
            before.Remove(unchangedId);
            after.Remove(unchangedId);
        }
        if (before.Values.Concat(after.Values).Any(value =>
                value.Kind == NetherCombatValueEvidenceKind.Missing))
        {
            return NetherMechanismPortfolioDelta.Missing(
                "changed-mechanism-portfolio-component-missing"
            );
        }
        if (before.Values.Concat(after.Values).Any(value =>
                value.Kind == NetherCombatValueEvidenceKind.ReachableUnquantified))
        {
            return NetherMechanismPortfolioDelta.Unquantified(
                "changed-mechanism-portfolio-component-unquantified"
            );
        }

        Dictionary<NetherMechanismQuantityIdentity, decimal> beforeQuantities = SumQuantities(
            before.Values
        );
        Dictionary<NetherMechanismQuantityIdentity, decimal> afterQuantities = SumQuantities(
            after.Values
        );
        var quantityKinds = new HashSet<NetherMechanismQuantityIdentity>(beforeQuantities.Keys);
        quantityKinds.UnionWith(afterQuantities.Keys);
        var quantityMarginals = quantityKinds.ToDictionary(
            kind => kind,
            kind => (afterQuantities.TryGetValue(kind, out decimal afterValue) ? afterValue : 0)
                - (beforeQuantities.TryGetValue(kind, out decimal beforeValue) ? beforeValue : 0)
        );
        Dictionary<NetherMechanismRecipientQuantityIdentity, decimal> beforeRecipientQuantities =
            SumRecipientQuantities(before.Values);
        Dictionary<NetherMechanismRecipientQuantityIdentity, decimal> afterRecipientQuantities =
            SumRecipientQuantities(after.Values);
        var recipientQuantityKinds = new HashSet<NetherMechanismRecipientQuantityIdentity>(
            beforeRecipientQuantities.Keys
        );
        recipientQuantityKinds.UnionWith(afterRecipientQuantities.Keys);
        var recipientQuantityMarginals = recipientQuantityKinds.ToDictionary(
            kind => kind,
            kind => (afterRecipientQuantities.TryGetValue(kind, out decimal afterValue)
                    ? afterValue
                    : 0)
                - (beforeRecipientQuantities.TryGetValue(kind, out decimal beforeValue)
                    ? beforeValue
                    : 0)
        );

        Dictionary<NetherMechanismQualitativePriority, int> beforeQualitative =
            CountQualitative(before.Values);
        Dictionary<NetherMechanismQualitativePriority, int> afterQualitative =
            CountQualitative(after.Values);
        var qualitativeKinds = new HashSet<NetherMechanismQualitativePriority>(
            beforeQualitative.Keys
        );
        qualitativeKinds.UnionWith(afterQualitative.Keys);
        var qualitativeMarginals = qualitativeKinds.ToDictionary(
            priority => priority,
            priority => (afterQualitative.TryGetValue(priority, out int afterCount) ? afterCount : 0)
                - (beforeQualitative.TryGetValue(priority, out int beforeCount) ? beforeCount : 0)
        );

        bool quantityNegative = quantityMarginals.Values.Any(value => value < 0);
        bool quantityPositive = quantityMarginals.Values.Any(value => value > 0);
        if (quantityNegative && quantityPositive)
        {
            return NetherMechanismPortfolioDelta.Unquantified(
                "changed-mechanism-portfolios-incommensurate"
            );
        }
        return new NetherMechanismPortfolioDelta(
            NetherCombatValueEvidenceKind.Quantified,
            quantityMarginals,
            recipientQuantityMarginals,
            qualitativeMarginals,
            "complete-typed-mechanism-portfolio"
        );
    }

    private static bool TryIndexMechanisms(
        IReadOnlyList<NetherMechanismPortfolioEntry> entries,
        out Dictionary<long, NetherMechanismValue> indexed
    )
    {
        indexed = new Dictionary<long, NetherMechanismValue>();
        foreach (NetherMechanismPortfolioEntry entry in entries)
        {
            if (entry.CodeId <= 0 || !indexed.TryAdd(entry.CodeId, entry.Value))
                return false;
        }
        return true;
    }

    private static Dictionary<NetherMechanismQuantityIdentity, decimal> SumQuantities(
        IEnumerable<NetherMechanismValue> values
    ) => values
        .Where(value => value.Kind == NetherCombatValueEvidenceKind.Quantified
            && value.Quantity.Kind != NetherMechanismQuantityKind.None
            && value.RecipientQuantities.Count == 0)
        .GroupBy(value => value.Quantity.Identity)
        .ToDictionary(group => group.Key, group => group.Sum(value => value.Quantity.Value));

    private static Dictionary<NetherMechanismRecipientQuantityIdentity, decimal>
        SumRecipientQuantities(IEnumerable<NetherMechanismValue> values) => values
            .Where(value => value.Kind == NetherCombatValueEvidenceKind.Quantified)
            .SelectMany(value => value.RecipientQuantities)
            .GroupBy(value => value.Identity)
            .ToDictionary(group => group.Key, group => group.Sum(value => value.Quantity.Value));

    private static bool MechanismValuesEquivalent(
        NetherMechanismValue left,
        NetherMechanismValue right
    ) => left.Kind == right.Kind
        && left.Quantity == right.Quantity
        && left.QualitativePriority == right.QualitativePriority
        && left.Detail == right.Detail
        && left.RecipientQuantities.SequenceEqual(right.RecipientQuantities);

    private static Dictionary<NetherMechanismQualitativePriority, int> CountQualitative(
        IEnumerable<NetherMechanismValue> values
    ) => values
        .Where(value => value.Kind == NetherCombatValueEvidenceKind.QualitativePriority
            && value.QualitativePriority != NetherMechanismQualitativePriority.None)
        .GroupBy(value => value.QualitativePriority)
        .ToDictionary(group => group.Key, group => group.Count());

    private static int Rank(NetherEquipmentMutationValue value) =>
        value.CanSelect ? (int)value.CombatTier : 0;

    private sealed record NetherMechanismPortfolioDelta(
        NetherCombatValueEvidenceKind Kind,
        IReadOnlyDictionary<NetherMechanismQuantityIdentity, decimal> QuantityMarginals,
        IReadOnlyDictionary<NetherMechanismRecipientQuantityIdentity, decimal>
            RecipientQuantityMarginals,
        IReadOnlyDictionary<NetherMechanismQualitativePriority, int> QualitativeMarginals,
        string Detail
    )
    {
        public bool HasNegative => QuantityMarginals.Values.Any(value => value < 0)
            || RecipientQuantityMarginals.Values.Any(value => value < 0)
            || QualitativeMarginals.Values.Any(value => value < 0);
        public bool HasPositive => QuantityMarginals.Values.Any(value => value > 0)
            || RecipientQuantityMarginals.Values.Any(value => value > 0)
            || QualitativeMarginals.Values.Any(value => value > 0);

        public static NetherMechanismPortfolioDelta Missing(string detail) => new(
            NetherCombatValueEvidenceKind.Missing,
            new Dictionary<NetherMechanismQuantityIdentity, decimal>(),
            new Dictionary<NetherMechanismRecipientQuantityIdentity, decimal>(),
            new Dictionary<NetherMechanismQualitativePriority, int>(),
            detail
        );

        public static NetherMechanismPortfolioDelta Unquantified(string detail) => new(
            NetherCombatValueEvidenceKind.ReachableUnquantified,
            new Dictionary<NetherMechanismQuantityIdentity, decimal>(),
            new Dictionary<NetherMechanismRecipientQuantityIdentity, decimal>(),
            new Dictionary<NetherMechanismQualitativePriority, int>(),
            detail
        );
    }

    private static NetherEquipmentMutationValue Missing(string detail) => new(
        NetherEquipmentMutationValueKind.Missing,
        NetherMechanismQualitativePriority.None,
        NetherEquipmentCombatTier.None,
        new Dictionary<(long, NetherCombatMetricKind), long>(),
        default,
        NetherNativeSpecialComparisonEvidence.None,
        detail
    );

    private static NetherEquipmentMutationValue Unquantified(string detail) => new(
        NetherEquipmentMutationValueKind.ReachableUnquantified,
        NetherMechanismQualitativePriority.None,
        NetherEquipmentCombatTier.None,
        new Dictionary<(long, NetherCombatMetricKind), long>(),
        default,
        NetherNativeSpecialComparisonEvidence.None,
        detail
    );

    private static NetherEquipmentMutationValue NonPositive(string detail) => new(
        NetherEquipmentMutationValueKind.NonPositive,
        NetherMechanismQualitativePriority.None,
        NetherEquipmentCombatTier.None,
        new Dictionary<(long, NetherCombatMetricKind), long>(),
        default,
        NetherNativeSpecialComparisonEvidence.None,
        detail
    );

    private bool TryEvaluateSpecial(
        NetherNativeSpecialComparisonEvidence evidence,
        out IReadOnlyDictionary<NetherNativeSpecialOutcomeIdentity, decimal> outcomes,
        out string error
    )
    {
        var mapped = new Dictionary<NetherNativeSpecialOutcomeIdentity, decimal>();
        outcomes = mapped;
        error = string.Empty;
        if (evidence == null)
        {
            error = "native-special-comparison-unavailable";
            return false;
        }

        switch (evidence.Kind)
        {
            case NetherNativeSpecialComparisonKind.None:
                return true;
            case NetherNativeSpecialComparisonKind.CriticalProbability:
                if (!TryValidateProbabilityRows(evidence.ProbabilityRows, requireLiveMaximum: false))
                {
                    error = "critical-probability-relationship-unavailable";
                    return false;
                }
                foreach (NetherCharacterProbabilityEvidence row in evidence.ProbabilityRows)
                {
                    int magnitude = _portfolioValuation.CriticalProbabilityMarginalPermille(
                        Math.Min(row.BeforeProbabilityPermille, row.AfterProbabilityPermille),
                        Math.Abs(row.AfterProbabilityPermille - row.BeforeProbabilityPermille)
                    );
                    decimal marginal = row.AfterProbabilityPermille < row.BeforeProbabilityPermille
                        ? -magnitude
                        : magnitude;
                    mapped.Add(
                        new NetherNativeSpecialOutcomeIdentity(
                            evidence.Kind,
                            row.CharacterId,
                            row.PartyPosition
                        ),
                        marginal
                    );
                }
                return true;
            case NetherNativeSpecialComparisonKind.ContinuousAttackProbability:
                if (!TryValidateProbabilityRows(evidence.ProbabilityRows, requireLiveMaximum: true))
                {
                    error = "continuous-attack-relationship-unavailable";
                    return false;
                }
                foreach (NetherCharacterProbabilityEvidence row in evidence.ProbabilityRows)
                {
                    long marginal = _portfolioValuation.ContinuousAttackExpectedAdditionalMicros(
                            row.AfterProbabilityPermille,
                            row.LiveMaximumCount
                        ) - _portfolioValuation.ContinuousAttackExpectedAdditionalMicros(
                            row.BeforeProbabilityPermille,
                            row.LiveMaximumCount
                        );
                    mapped.Add(
                        new NetherNativeSpecialOutcomeIdentity(
                            evidence.Kind,
                            row.CharacterId,
                            row.PartyPosition
                        ),
                        marginal
                    );
                }
                return true;
            case NetherNativeSpecialComparisonKind.DefenseEffectiveHp:
                NetherDefenseComparison validation = _portfolioValuation.CompareDefense(
                    evidence.DefenseRows,
                    evidence.DefenseRows
                );
                if (validation.Kind != NetherCombatValueEvidenceKind.Quantified)
                {
                    error = validation.Detail;
                    return false;
                }
                foreach (NetherCharacterEffectiveHpEvidence row in evidence.DefenseRows)
                {
                    mapped.Add(
                        new NetherNativeSpecialOutcomeIdentity(
                            evidence.Kind,
                            row.CharacterId,
                            row.PartyPosition
                        ),
                        row.AfterEffectiveHp - row.BeforeEffectiveHp
                    );
                }
                return true;
            default:
                error = "unsupported-native-special-comparison";
                return false;
        }
    }

    private static NetherTieredOutcomeDirection ResolveTieredOutcome(
        IReadOnlyList<NetherNativeMetricExposure> nativeExposures,
        IReadOnlyDictionary<long, NetherPartyPosition> recipientPositions,
        NetherMechanismPortfolioDelta mechanisms,
        IReadOnlyDictionary<NetherNativeSpecialOutcomeIdentity, decimal> specialOutcomes,
        NetherEquipmentCombatTier positiveUnscopedTier,
        NetherEquipmentCombatTier negativeUnscopedTier,
        bool promotePositiveToSurvivalRepair,
        out NetherEquipmentCombatTier effectiveTier
    )
    {
        effectiveTier = NetherEquipmentCombatTier.None;
        var byTier = new Dictionary<NetherEquipmentCombatTier, (bool Positive, bool Negative)>();
        foreach (NetherNativeMetricExposure exposure in nativeExposures)
        {
            long marginal = exposure.MarginalPermilleSeconds;
            if (marginal == 0)
                continue;
            if (!recipientPositions.TryGetValue(
                    exposure.RecipientCharacterId,
                    out NetherPartyPosition position
                ))
            {
                return NetherTieredOutcomeDirection.Unknown;
            }
            NetherEquipmentCombatTier tier = NetherEquipmentCombatTierClassifier.ForMetric(
                exposure.Metric,
                position
            );
            if (!TryAddTierDirection(
                    byTier,
                    tier,
                    marginal > 0,
                    promotePositiveToSurvivalRepair
                ))
                return NetherTieredOutcomeDirection.Unknown;
        }
        foreach ((NetherMechanismRecipientQuantityIdentity identity, decimal marginal) in
                 mechanisms.RecipientQuantityMarginals)
        {
            if (marginal == 0)
                continue;
            NetherEquipmentCombatTier tier = NetherEquipmentCombatTierClassifier.ForMetric(
                identity.Metric,
                identity.PartyPosition
            );
            if (!TryAddTierDirection(
                    byTier,
                    tier,
                    marginal > 0,
                    promotePositiveToSurvivalRepair
                ))
                return NetherTieredOutcomeDirection.Unknown;
        }
        foreach ((NetherMechanismQuantityIdentity _, decimal marginal) in
                 mechanisms.QuantityMarginals)
        {
            if (marginal == 0)
                continue;
            NetherEquipmentCombatTier tier = marginal > 0
                ? positiveUnscopedTier
                : negativeUnscopedTier;
            if (!TryAddTierDirection(
                    byTier,
                    tier,
                    marginal > 0,
                    promotePositiveToSurvivalRepair
                ))
                return NetherTieredOutcomeDirection.Unknown;
        }
        foreach ((NetherMechanismQualitativePriority priority, int marginal) in
                 mechanisms.QualitativeMarginals)
        {
            if (marginal == 0)
                continue;
            NetherEquipmentCombatTier tier = NetherEquipmentCombatTierClassifier.ForQualitative(
                priority
            );
            if (!TryAddTierDirection(
                    byTier,
                    tier,
                    marginal > 0,
                    promotePositiveToSurvivalRepair
                ))
                return NetherTieredOutcomeDirection.Unknown;
        }
        foreach ((NetherNativeSpecialOutcomeIdentity identity, decimal marginal) in specialOutcomes)
        {
            if (marginal == 0)
                continue;
            NetherEquipmentCombatTier tier = NetherEquipmentCombatTierClassifier.ForSpecial(
                identity.Kind,
                identity.PartyPosition
            );
            if (!TryAddTierDirection(
                    byTier,
                    tier,
                    marginal > 0,
                    promotePositiveToSurvivalRepair
                ))
                return NetherTieredOutcomeDirection.Unknown;
        }
        foreach ((NetherEquipmentCombatTier tier, (bool positive, bool negative)) in byTier
                     .OrderByDescending(row => row.Key))
        {
            if (positive && negative)
                return NetherTieredOutcomeDirection.Unknown;
            if (positive)
            {
                effectiveTier = tier;
                return NetherTieredOutcomeDirection.Positive;
            }
            if (negative)
            {
                effectiveTier = tier;
                return NetherTieredOutcomeDirection.Negative;
            }
        }
        return NetherTieredOutcomeDirection.Equal;
    }

    private static bool TryAddTierDirection(
        IDictionary<NetherEquipmentCombatTier, (bool Positive, bool Negative)> byTier,
        NetherEquipmentCombatTier tier,
        bool positive,
        bool promotePositiveToSurvivalRepair
    )
    {
        if (tier == NetherEquipmentCombatTier.None)
            return false;
        AddTierDirection(
            byTier,
            positive && promotePositiveToSurvivalRepair
                ? NetherEquipmentCombatTier.SurvivalRepair
                : tier,
            positive
        );
        return true;
    }

    private NetherCompletePortfolioPreference CompareSpecialOfferOutcomes(
        NetherEquipmentMutationValue left,
        NetherEquipmentMutationValue right,
        IReadOnlySet<NetherNativeSpecialComparisonKind> specialKinds,
        IReadOnlyDictionary<NetherNativeSpecialComparisonKind, NetherNativeSpecialComparisonEvidence>
            leftSpecials,
        IReadOnlyDictionary<NetherNativeSpecialComparisonKind, NetherNativeSpecialComparisonEvidence>
            rightSpecials
    )
    {
        if (specialKinds.Count == 0)
            return NetherCompletePortfolioPreference.Equal;
        var byTier = new Dictionary<NetherEquipmentCombatTier, (bool Left, bool Right)>();
        foreach (NetherNativeSpecialComparisonKind kind in specialKinds)
        {
            if (kind == NetherNativeSpecialComparisonKind.DefenseEffectiveHp)
            {
                NetherDefenseComparison defense = _portfolioValuation.CompareDefense(
                    leftSpecials[kind].DefenseRows,
                    rightSpecials[kind].DefenseRows
                );
                if (defense.Kind != NetherCombatValueEvidenceKind.Quantified)
                    return NetherCompletePortfolioPreference.Unknown;
                if (defense.Preferred != 0)
                {
                    NetherNativeSpecialComparisonEvidence preferred = defense.Preferred > 0
                        ? leftSpecials[kind]
                        : rightSpecials[kind];
                    NetherEquipmentCombatTier tier = NetherEquipmentCombatTierClassifier.ForMetric(
                        NetherCombatMetricKind.Defence,
                        preferred.DefenseRows.Select(row => row.PartyPosition)
                    );
                    if (tier == NetherEquipmentCombatTier.None)
                        return NetherCompletePortfolioPreference.Unknown;
                    AddOfferDirection(byTier, tier, left: defense.Preferred > 0);
                }
                continue;
            }

            var identities = new HashSet<NetherNativeSpecialOutcomeIdentity>(
                left.NativeSpecialMarginals.Keys.Where(row => row.Kind == kind)
            );
            identities.UnionWith(
                right.NativeSpecialMarginals.Keys.Where(row => row.Kind == kind)
            );
            if (identities.Count == 0)
                return NetherCompletePortfolioPreference.Unknown;
            foreach (NetherNativeSpecialOutcomeIdentity identity in identities)
            {
                decimal leftValue = left.NativeSpecialMarginals.TryGetValue(
                    identity,
                    out decimal leftMarginal
                ) ? leftMarginal : 0;
                decimal rightValue = right.NativeSpecialMarginals.TryGetValue(
                    identity,
                    out decimal rightMarginal
                ) ? rightMarginal : 0;
                if (leftValue == rightValue)
                    continue;
                NetherEquipmentCombatTier tier = NetherEquipmentCombatTierClassifier.ForSpecial(
                    identity.Kind,
                    identity.PartyPosition
                );
                if (tier == NetherEquipmentCombatTier.None)
                    return NetherCompletePortfolioPreference.Unknown;
                AddOfferDirection(byTier, tier, left: leftValue > rightValue);
            }
        }
        foreach ((_, (bool leftBetter, bool rightBetter)) in byTier
                     .OrderByDescending(row => row.Key))
        {
            if (leftBetter && rightBetter)
                return NetherCompletePortfolioPreference.Unknown;
            if (leftBetter)
                return NetherCompletePortfolioPreference.Left;
            if (rightBetter)
                return NetherCompletePortfolioPreference.Right;
        }
        return NetherCompletePortfolioPreference.Equal;
    }

    private static void AddTierDirection(
        IDictionary<NetherEquipmentCombatTier, (bool Positive, bool Negative)> byTier,
        NetherEquipmentCombatTier tier,
        bool positive
    )
    {
        byTier.TryGetValue(tier, out (bool Positive, bool Negative) current);
        byTier[tier] = positive
            ? (true, current.Negative)
            : (current.Positive, true);
    }

    private static void AddOfferDirection(
        IDictionary<NetherEquipmentCombatTier, (bool Left, bool Right)> byTier,
        NetherEquipmentCombatTier tier,
        bool left
    )
    {
        byTier.TryGetValue(tier, out (bool Left, bool Right) current);
        byTier[tier] = left
            ? (true, current.Right)
            : (current.Left, true);
    }

    private enum NetherTieredOutcomeDirection
    {
        Unknown = 0,
        Equal,
        Positive,
        Negative,
    }

    private static NetherNativeSpecialComparisonEvidence[] SpecialComparisons(
        NetherCodeEquipmentMutationEvidence evidence
    )
    {
        NetherNativeSpecialComparisonEvidence[] explicitRows = evidence.NativeComparisons
            .Where(row => row != null && row.Kind != NetherNativeSpecialComparisonKind.None)
            .ToArray();
        return explicitRows.Length > 0
            ? explicitRows
            : evidence.NativeComparison.Kind == NetherNativeSpecialComparisonKind.None
                ? Array.Empty<NetherNativeSpecialComparisonEvidence>()
                : [evidence.NativeComparison];
    }

    private static bool IsRepresentedBySpecial(
        NetherCombatMetricKind metric,
        IReadOnlySet<NetherNativeSpecialComparisonKind> specials
    ) => specials.Contains(NetherNativeSpecialComparisonKind.CriticalProbability)
            && metric == NetherCombatMetricKind.CriticalProbability
        || specials.Contains(NetherNativeSpecialComparisonKind.ContinuousAttackProbability)
            && metric == NetherCombatMetricKind.ContinuousAttackProbability
        || specials.Contains(NetherNativeSpecialComparisonKind.DefenseEffectiveHp)
            && metric is NetherCombatMetricKind.Defence or
                NetherCombatMetricKind.MaxHp or
                NetherCombatMetricKind.TakenDamage;

    private static bool TryValidateProbabilityRows(
        IReadOnlyList<NetherCharacterProbabilityEvidence>? rows,
        bool requireLiveMaximum
    ) => rows != null
        && rows.Count > 0
        && rows.Select(row => row.CharacterId).Distinct().Count() == rows.Count
        && rows.All(row => row.CharacterId > 0
            && row.BeforeProbabilityPermille >= 0
            && row.AfterProbabilityPermille >= 0
            && row.PartyPosition is NetherPartyPosition.Forward
                or NetherPartyPosition.Back
                or NetherPartyPosition.Assist
            && (!requireLiveMaximum || row.LiveMaximumCount >= 0));
}
