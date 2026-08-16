#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal enum NetherCombatMetricKind
{
    Unknown = 0,
    Attack,
    Defence,
    MaxHp,
    TakenDamage,
    Resistance,
    ElementDamage,
    CriticalProbability,
    ContinuousAttackProbability,
    DamageModifier,
}

internal enum NetherCombatValueEvidenceKind
{
    Missing = 0,
    ReachableUnquantified,
    Quantified,
    QualitativePriority,
}

internal sealed record NetherNativeBuffWindow(
    long CodeId,
    long RecipientCharacterId,
    NetherStrategyBuffType BuffType,
    NetherStrategyBuffEffectKind EffectKind,
    NetherStrategyBuffCoexistenceKind Coexistence,
    NetherCombatMetricKind Metric,
    int ValuePermille,
    int StartSecond,
    int DurationSeconds
)
{
    public IReadOnlyList<NetherStrategyBuffType> MatchedBuffTypes { get; init; } =
        Array.Empty<NetherStrategyBuffType>();
    public int PositiveCumulativeLimit { get; init; }
    public bool TriggerKnown { get; init; } = true;
    public bool TriggerReachable { get; init; } = true;
    public bool TriggerOrderKnown { get; init; } = true;
    public int TriggerOrder { get; init; }
    public bool MetricInputsKnown { get; init; } = true;
}

internal sealed record NetherNativePortfolioTimelineInput(
    IReadOnlyList<NetherNativeBuffWindow> HeldWindows,
    IReadOnlyList<NetherNativeBuffWindow> CandidateWindows,
    int BossDurationSeconds
);

internal sealed record NetherNativePortfolioComparisonInput(
    IReadOnlyList<NetherNativeBuffWindow> BeforeWindows,
    IReadOnlyList<NetherNativeBuffWindow> AfterWindows,
    int BossDurationSeconds
);

internal readonly record struct NetherNativeMetricExposure(
    long RecipientCharacterId,
    NetherCombatMetricKind Metric,
    long BeforePermilleSeconds,
    long AfterPermilleSeconds
)
{
    public long MarginalPermilleSeconds => AfterPermilleSeconds - BeforePermilleSeconds;
}

internal sealed record NetherNativePortfolioValue(
    NetherCombatValueEvidenceKind Kind,
    IReadOnlyList<NetherNativeMetricExposure> Exposures,
    string Detail
);

internal sealed record NetherCharacterEffectiveHpEvidence(
    long CharacterId,
    NetherPartyPosition PartyPosition,
    decimal BeforeEffectiveHp,
    decimal AfterEffectiveHp,
    bool IsKnown
);

internal readonly record struct NetherDefenseComparison(
    NetherCombatValueEvidenceKind Kind,
    int Preferred,
    string Detail
);

/// <summary>
/// Pure public policy seam for the current native BuffController/BuffGroup timeline. The first
/// RED intentionally lands against this conservative placeholder; implementation is evidence-led.
/// </summary>
internal sealed class NetherNativePortfolioValuation
{
    public int CriticalProbabilityMarginalPermille(int currentPermille, int additionPermille)
    {
        if (currentPermille < 0 || additionPermille < 0)
            throw new ArgumentOutOfRangeException(nameof(currentPermille));
        int before = Math.Min(999, currentPermille);
        int after = Math.Min(999, checked(currentPermille + additionPermille));
        return after - before;
    }

    public long ContinuousAttackExpectedAdditionalMicros(
        int probabilityPermille,
        int liveMaximumCount,
        int decreaseProbabilityPermille = 100
    )
    {
        if (probabilityPermille < 0
            || liveMaximumCount < 0
            || decreaseProbabilityPermille <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(probabilityPermille));
        }

        decimal reachProbability = 1m;
        decimal expectedAdditionalAttacks = 0m;
        int current = probabilityPermille;
        for (int count = 0; count < liveMaximumCount; count++)
        {
            decimal success = Math.Min(1m, current / 1000m);
            reachProbability *= success;
            expectedAdditionalAttacks += reachProbability;
            current = Math.Max(0, current - decreaseProbabilityPermille);
        }
        return checked((long)decimal.Round(
            expectedAdditionalAttacks * 1_000_000m,
            0,
            MidpointRounding.AwayFromZero
        ));
    }

    public NetherDefenseComparison CompareDefense(
        IReadOnlyList<NetherCharacterEffectiveHpEvidence> left,
        IReadOnlyList<NetherCharacterEffectiveHpEvidence> right
    )
    {
        if (!TryValidateDefense(left) || !TryValidateDefense(right))
        {
            return new NetherDefenseComparison(
                NetherCombatValueEvidenceKind.ReachableUnquantified,
                0,
                "effective-hp-relationship-unavailable"
            );
        }

        NetherDefenseVector leftVector = DefenseVector(left);
        NetherDefenseVector rightVector = DefenseVector(right);
        int preferred = leftVector.RearCoverage.CompareTo(rightVector.RearCoverage);
        if (preferred == 0)
            preferred = CompareDecimal(leftVector.WeakestRearGain, rightVector.WeakestRearGain);
        if (preferred == 0)
            preferred = CompareDecimal(leftVector.AggregateGain, rightVector.AggregateGain);
        return new NetherDefenseComparison(
            NetherCombatValueEvidenceKind.Quantified,
            Math.Sign(preferred),
            "exact-relative-effective-hp"
        );
    }

    public NetherNativePortfolioValue Evaluate(NetherNativePortfolioTimelineInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (input.HeldWindows == null
            || input.CandidateWindows == null
            || input.BossDurationSeconds <= 0)
        {
            return Missing("invalid-native-portfolio-timeline");
        }

        return EvaluateComparison(new NetherNativePortfolioComparisonInput(
            input.HeldWindows,
            input.HeldWindows.Concat(input.CandidateWindows).ToArray(),
            input.BossDurationSeconds
        ));
    }

    public NetherNativePortfolioValue EvaluateComparison(
        NetherNativePortfolioComparisonInput input
    )
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (input.BeforeWindows == null
            || input.AfterWindows == null
            || input.BossDurationSeconds <= 0)
        {
            return Missing("invalid-native-portfolio-comparison");
        }

        NetherTimelineProjection before = Simulate(
            input.BeforeWindows,
            input.BossDurationSeconds
        );
        NetherTimelineProjection after = Simulate(input.AfterWindows, input.BossDurationSeconds);
        if (!before.IsKnown || !after.IsKnown)
        {
            return new NetherNativePortfolioValue(
                NetherCombatValueEvidenceKind.ReachableUnquantified,
                Array.Empty<NetherNativeMetricExposure>(),
                !before.IsKnown ? before.Detail : after.Detail
            );
        }

        var keys = new HashSet<(long Recipient, NetherCombatMetricKind Metric)>(
            before.Exposure.Keys
        );
        keys.UnionWith(after.Exposure.Keys);
        NetherNativeMetricExposure[] exposures = keys
            .OrderBy(key => key.Recipient)
            .ThenBy(key => key.Metric)
            .Select(key => new NetherNativeMetricExposure(
                key.Recipient,
                key.Metric,
                before.Exposure.TryGetValue(key, out long beforeValue) ? beforeValue : 0,
                after.Exposure.TryGetValue(key, out long afterValue) ? afterValue : 0
            ))
            .ToArray();
        return new NetherNativePortfolioValue(
            NetherCombatValueEvidenceKind.Quantified,
            exposures,
            "native-retained-portfolio-timeline"
        );
    }

    private static NetherTimelineProjection Simulate(
        IReadOnlyList<NetherNativeBuffWindow> windows,
        int durationSeconds
    )
    {
        if (windows.Any(window => !IsStructurallyKnown(window)))
        {
            return NetherTimelineProjection.Unknown(
                "native-buff-window-relationship-unavailable"
            );
        }

        NetherNativeBuffWindow[] reachable = windows
            .Where(window => window.TriggerReachable)
            .ToArray();
        var exposure = new Dictionary<(long Recipient, NetherCombatMetricKind Metric), long>();
        foreach (NetherWindowGroup group in Group(reachable))
        {
            if (!TrySimulateGroup(group.Windows, durationSeconds, out long value, out string detail))
                return NetherTimelineProjection.Unknown(detail);
            var key = (group.RecipientCharacterId, group.Metric);
            exposure[key] = exposure.TryGetValue(key, out long current)
                ? current + value
                : value;
        }
        return NetherTimelineProjection.Known(exposure);
    }

    private static bool IsStructurallyKnown(NetherNativeBuffWindow window) => window != null
        && window.CodeId > 0
        && window.RecipientCharacterId > 0
        && window.BuffType.IsKnown
        && window.EffectKind != NetherStrategyBuffEffectKind.Unknown
        && window.Coexistence is NetherStrategyBuffCoexistenceKind.Allow
            or NetherStrategyBuffCoexistenceKind.HigherValue
        && window.Metric != NetherCombatMetricKind.Unknown
        && window.ValuePermille >= 0
        && window.StartSecond >= 0
        && window.DurationSeconds > 0
        && window.PositiveCumulativeLimit >= 0
        && window.TriggerKnown
        && window.TriggerOrderKnown
        && window.MetricInputsKnown;

    private static IReadOnlyList<NetherWindowGroup> Group(
        IReadOnlyList<NetherNativeBuffWindow> windows
    )
    {
        var groups = new List<NetherWindowGroup>();
        foreach (NetherNativeBuffWindow window in windows)
        {
            HashSet<int> types = Types(window);
            NetherWindowGroup[] matches = groups
                .Where(group => group.RecipientCharacterId == window.RecipientCharacterId
                    && group.Metric == window.Metric
                    && group.EffectKind == window.EffectKind
                    && group.BuffTypes.Overlaps(types))
                .ToArray();
            if (matches.Length == 0)
            {
                groups.Add(new NetherWindowGroup(
                    window.RecipientCharacterId,
                    window.Metric,
                    window.EffectKind,
                    types,
                    new List<NetherNativeBuffWindow> { window }
                ));
                continue;
            }

            NetherWindowGroup target = matches[0];
            target.BuffTypes.UnionWith(types);
            target.Windows.Add(window);
            foreach (NetherWindowGroup merged in matches.Skip(1))
            {
                target.BuffTypes.UnionWith(merged.BuffTypes);
                target.Windows.AddRange(merged.Windows);
                groups.Remove(merged);
            }
        }
        return groups;
    }

    private static HashSet<int> Types(NetherNativeBuffWindow window)
    {
        var result = new HashSet<int> { window.BuffType.Value };
        result.UnionWith(window.MatchedBuffTypes.Select(type => type.Value));
        return result;
    }

    private static bool TrySimulateGroup(
        IReadOnlyList<NetherNativeBuffWindow> windows,
        int durationSeconds,
        out long exposure,
        out string detail
    )
    {
        exposure = 0;
        detail = string.Empty;
        NetherStrategyBuffCoexistenceKind[] strategies = windows
            .Select(window => window.Coexistence)
            .Distinct()
            .ToArray();
        if (strategies.Length != 1)
        {
            detail = "mixed-native-coexistence-strategy";
            return false;
        }

        NetherStrategyBuffCoexistenceKind strategy = strategies[0];
        var active = new List<NetherNativeBuffWindow>();
        for (int second = 0; second < durationSeconds; second++)
        {
            active.RemoveAll(window => window.StartSecond + window.DurationSeconds <= second);
            foreach (NetherNativeBuffWindow incoming in windows
                         .Where(window => window.StartSecond == second)
                         .OrderBy(window => window.TriggerOrder)
                         .ThenBy(window => window.CodeId))
            {
                if (strategy == NetherStrategyBuffCoexistenceKind.Allow)
                {
                    active.Add(incoming);
                    continue;
                }

                NetherNativeBuffWindow? highest = active
                    .OrderByDescending(window => window.ValuePermille)
                    .ThenBy(window => window.StartSecond)
                    .ThenBy(window => window.CodeId)
                    .FirstOrDefault();
                if (highest == null)
                {
                    active.Add(incoming);
                    continue;
                }
                int incomingRemaining = incoming.StartSecond + incoming.DurationSeconds - second;
                int highestRemaining = highest.StartSecond + highest.DurationSeconds - second;
                if (incoming.ValuePermille > highest.ValuePermille
                    || incoming.ValuePermille == highest.ValuePermille
                        && incomingRemaining > highestRemaining)
                {
                    // Native CheckCoexistenceHigherValue removes the whole matched group. A weaker
                    // displaced window is gone; it is not suspended for later resumption.
                    active.Clear();
                    active.Add(incoming);
                }
            }

            int current = strategy == NetherStrategyBuffCoexistenceKind.Allow
                ? active.Sum(window => window.ValuePermille)
                : active.Count == 0 ? 0 : active.Max(window => window.ValuePermille);
            if (strategy == NetherStrategyBuffCoexistenceKind.Allow)
            {
                int limit = active.Count == 0
                    ? 0
                    : active.Max(window => window.PositiveCumulativeLimit);
                if (limit > 0)
                    current = Math.Min(current, limit);
            }
            exposure += current;
        }
        return true;
    }

    private static NetherNativePortfolioValue Missing(string detail) => new(
        NetherCombatValueEvidenceKind.Missing,
        Array.Empty<NetherNativeMetricExposure>(),
        detail
    );

    private static bool TryValidateDefense(
        IReadOnlyList<NetherCharacterEffectiveHpEvidence>? rows
    ) => rows != null
        && rows.Count > 0
        && rows.Select(row => row.CharacterId).Distinct().Count() == rows.Count
        && rows.All(row => row != null
            && row.IsKnown
            && row.CharacterId > 0
            && row.PartyPosition is NetherPartyPosition.Forward
                or NetherPartyPosition.Back
                or NetherPartyPosition.Assist
            && row.BeforeEffectiveHp > 0
            && row.AfterEffectiveHp >= 0);

    private static decimal RelativeEffectiveHpGain(
        NetherCharacterEffectiveHpEvidence row
    ) => (row.AfterEffectiveHp - row.BeforeEffectiveHp) / (decimal)row.BeforeEffectiveHp;

    private static NetherDefenseVector DefenseVector(
        IReadOnlyList<NetherCharacterEffectiveHpEvidence> rows
    )
    {
        decimal[] rearGains = rows
            .Where(row => row.PartyPosition == NetherPartyPosition.Back)
            .Select(RelativeEffectiveHpGain)
            .Where(gain => gain > 0)
            .ToArray();
        return new NetherDefenseVector(
            rearGains.Length,
            rearGains.Length == 0 ? 0 : rearGains.Min(),
            rows.Sum(RelativeEffectiveHpGain)
        );
    }

    private static int CompareDecimal(decimal left, decimal right) => left.CompareTo(right);

    private sealed record NetherWindowGroup(
        long RecipientCharacterId,
        NetherCombatMetricKind Metric,
        NetherStrategyBuffEffectKind EffectKind,
        HashSet<int> BuffTypes,
        List<NetherNativeBuffWindow> Windows
    );

    private sealed record NetherTimelineProjection(
        bool IsKnown,
        IReadOnlyDictionary<(long Recipient, NetherCombatMetricKind Metric), long> Exposure,
        string Detail
    )
    {
        public static NetherTimelineProjection Known(
            IReadOnlyDictionary<(long Recipient, NetherCombatMetricKind Metric), long> exposure
        ) => new(true, exposure, string.Empty);

        public static NetherTimelineProjection Unknown(string detail) => new(
            false,
            new Dictionary<(long Recipient, NetherCombatMetricKind Metric), long>(),
            detail
        );
    }

    private readonly record struct NetherDefenseVector(
        int RearCoverage,
        decimal WeakestRearGain,
        decimal AggregateGain
    );
}
