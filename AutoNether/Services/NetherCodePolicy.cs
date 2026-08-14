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
}

/// <summary>
/// Evidence-bounded baseline policy. Category cohesion follows the native paired-card counters;
/// explicit Rush/Impact configuration is honored, while Safe and Risk remain peers. Static
/// master power and ability level are not compared across abilities.  The packaged UI multiplies
/// master power by Scope-eligible party members; this is only a display projection, not proof of
/// runtime Target/Situations benefit, and it contributes only when every compared value is known.
/// </summary>
internal sealed class NetherCodePolicy
{
    public NetherCodeDecision Decide(
        NetherCodePortfolio portfolio,
        IReadOnlyList<NetherCodeCandidate> candidates,
        NetherAutoClimbSettings settings
    )
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        if (candidates == null)
            throw new ArgumentNullException(nameof(candidates));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (!IsValid(portfolio, candidates))
            return Pause(NetherPauseReason.UnknownMasterData, "incomplete-code-portfolio");

        NetherCombatLane lane = ResolveLane(portfolio, settings.CombatLane);
        var ownedIds = new HashSet<long>(portfolio.CurrentCodes.Select(code => code.CodeId));
        NetherCodeCandidate[] eligible = candidates
            .GroupBy(candidate => candidate.CodeId)
            .Select(group => group.First())
            // Native offers may contain an already-owned ID. The server owns Amount updates, but
            // no inspected client logic proves their strategy value. At capacity, selecting one
            // still enters replacement and sacrifices a different unique slot. Do not invent a
            // stack-value policy: retain/reload until a genuinely new unique card is offered.
            .Where(candidate => !ownedIds.Contains(candidate.CodeId))
            .ToArray();
        if (eligible.Length == 0)
            return ReloadOrKeep(portfolio, settings, lane, "no-new-code-candidate");

        if (portfolio.CurrentCodes.Count < portfolio.Capacity)
        {
            NetherCodeCandidate selected = eligible
                .OrderByDescending(candidate => ScoreAfterAdd(portfolio.CurrentCodes, candidate, lane))
                .ThenBy(candidate => candidate.CodeId)
                .First();
            return ScoreAfterAdd(portfolio.CurrentCodes, selected, lane)
                    .CompareTo(Score(portfolio.CurrentCodes, lane)) > 0
                ? Select(selected, 0, lane, Array.Empty<long>())
                : ReloadOrKeep(
                    portfolio,
                    settings,
                    lane,
                    "candidate-has-no-proven-structural-or-coverage-gain"
                );
        }

        NetherCodePortfolioScore currentScore = Score(portfolio.CurrentCodes, lane);
        ReplacementChoice? best = null;
        foreach (NetherCodeCandidate candidate in eligible)
        {
            foreach (NetherCodeState removal in portfolio.CurrentCodes)
            {
                if (removal.CodeId == candidate.CodeId)
                    continue;
                NetherCodePortfolioScore score = Score(
                    ApplyCandidate(
                        portfolio.CurrentCodes.Where(code => code.CodeId != removal.CodeId),
                        candidate
                    ),
                    lane
                );
                var choice = new ReplacementChoice(candidate, removal, score);
                if (best == null || CompareChoice(choice, best.Value) > 0)
                    best = choice;
            }
        }

        long[] removable = RankRemovals(portfolio.CurrentCodes, lane)
            .Select(code => code.CodeId)
            .ToArray();
        if (best is ReplacementChoice replacement && replacement.Score.CompareTo(currentScore) > 0)
            return Select(replacement.Candidate, replacement.Removal.CodeId, lane, removable);

        return ReloadOrKeep(portfolio, settings, lane, "candidate-not-an-evidence-backed-upgrade", removable);
    }

    public static NetherCodeEffectiveLevels CalculateEffectiveLevels(IReadOnlyList<NetherCodeState> codes)
    {
        if (codes == null)
            throw new ArgumentNullException(nameof(codes));
        return CalculateEffectiveLevels(codes.Select(code => code.Family));
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

    internal static IReadOnlyList<NetherCodeState> RankRemovals(
        IReadOnlyList<NetherCodeState> codes,
        NetherCombatLane lane = NetherCombatLane.Auto
    ) => codes
        .Select(code => new
        {
            Code = code,
            Remaining = Score(codes.Where(other => other.CodeId != code.CodeId), lane),
        })
        .OrderByDescending(item => item.Remaining)
        .ThenBy(item => item.Code.CodeId)
        .Select(item => item.Code)
        .ToArray();

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

    private static NetherCodePortfolioScore ScoreAfterAdd(
        IReadOnlyList<NetherCodeState> current,
        NetherCodeCandidate candidate,
        NetherCombatLane lane
    ) => Score(ApplyCandidate(current, candidate), lane);

    private static IEnumerable<NetherCodeCandidate> ApplyCandidate(
        IEnumerable<NetherCodeState> current,
        NetherCodeCandidate candidate
    ) => ApplyCandidate(current.Select(ToCandidateView), candidate);

    private static IEnumerable<NetherCodeCandidate> ApplyCandidate(
        IEnumerable<NetherCodeCandidate> current,
        NetherCodeCandidate candidate
    )
    {
        NetherCodeCandidate[] existing = current.ToArray();
        return existing.Any(code => code.CodeId == candidate.CodeId)
            ? existing
            : existing.Append(candidate);
    }

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

    private static NetherCodePortfolioScore Score(
        IEnumerable<NetherCodeState> codes,
        NetherCombatLane lane
    ) => Score(codes.Select(ToCandidateView), lane);

    private static NetherCodePortfolioScore Score(
        IEnumerable<NetherCodeCandidate> codes,
        NetherCombatLane lane
    )
    {
        NetherCodeCandidate[] all = codes.ToArray();
        NetherCodeEffectiveLevels effective = CalculateEffectiveLevels(all.Select(code => code.Family));
        int preferredLaneCount = lane switch
        {
            NetherCombatLane.Rush => effective.Rush,
            NetherCombatLane.Impact => effective.Impact,
            _ => Math.Max(effective.Rush, effective.Impact),
        };
        NetherCodeCandidate[] displayEligible = all
            .Where(code => lane == NetherCombatLane.Auto || ToLane(code.Family) == lane)
            .ToArray();
        bool nativeDisplayPowerKnown = displayEligible.All(code => code.PartyCoverageKnown);
        long nativeDisplayPower = nativeDisplayPowerKnown
            ? displayEligible.Sum(code => (long)code.Power * code.PartyCoverage)
            : 0;
        return new NetherCodePortfolioScore(
            preferredLaneCount,
            effective.Safe + effective.Risk + effective.Rush + effective.Impact,
            nativeDisplayPowerKnown,
            nativeDisplayPower
        );
    }

    private static int CompareChoice(ReplacementChoice left, ReplacementChoice right)
    {
        int score = left.Score.CompareTo(right.Score);
        if (score != 0)
            return score;
        int candidate = right.Candidate.CodeId.CompareTo(left.Candidate.CodeId);
        if (candidate != 0)
            return candidate;
        return right.Removal.CodeId.CompareTo(left.Removal.CodeId);
    }

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
        IReadOnlyList<long>? removable = null
    ) => portfolio.ReloadCount > settings.CodeReloadReserve
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

    private readonly record struct ReplacementChoice(
        NetherCodeCandidate Candidate,
        NetherCodeState Removal,
        NetherCodePortfolioScore Score
    );

    private readonly record struct NetherCodePortfolioScore(
        int PreferredLaneCount,
        int CategoryCoherence,
        bool NativeDisplayPowerKnown,
        long NativeDisplayPower
    ) : IComparable<NetherCodePortfolioScore>
    {
        public int CompareTo(NetherCodePortfolioScore other)
        {
            int compared = PreferredLaneCount.CompareTo(other.PreferredLaneCount);
            if (compared != 0) return compared;
            compared = CategoryCoherence.CompareTo(other.CategoryCoherence);
            if (compared != 0) return compared;
            return NativeDisplayPowerKnown && other.NativeDisplayPowerKnown
                ? NativeDisplayPower.CompareTo(other.NativeDisplayPower)
                : 0;
        }
    }
}
