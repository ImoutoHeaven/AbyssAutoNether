#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// HP payment semantics are explicit at the horizon boundary.  The two group-survival cases are
/// commitments used by the later Treasure/key policies; they never relax ordinary route costs.
/// </summary>
internal enum NetherRouteHpRule
{
    OrdinaryAllLivingSurvive = 0,
    TreasureGroupSurvival = 1,
    HpPaidKeyGroupSurvival = 2,
}

/// <summary>
/// One exact, visible step on a single branch ending at the next terminal Boss.  The native
/// server exposes character HP as current_hp_ratio/HpRatio, so deterministic party-wide effects
/// remain in permille rather than being converted to guessed absolute HP.
/// </summary>
internal sealed record NetherRouteHorizonStep(
    long NodeId,
    NetherFloorNodeType NodeType,
    int BaseErosionDelta,
    int? HpDeltaPermille,
    IReadOnlyList<NetherErosionModifier> ErosionModifiers
)
{
    public bool IsOutcomeCertain { get; init; } = true;
    public bool HasExactPreEntryHpEvidence { get; init; } = true;
    public bool IsConfirmedRecovery { get; init; }
    public bool IsNecessaryCombat { get; init; }
    public bool IsTerminalBoss { get; init; }
    public int MinimumCombatEntryHpPermille { get; init; }
    public NetherRouteHpRule HpRule { get; init; } = NetherRouteHpRule.OrdinaryAllLivingSurvive;
}

internal sealed record NetherRouteHorizonSafetyInput(
    int CurrentErosion,
    IReadOnlyList<int> ActiveCharacterHpPermille,
    IReadOnlyList<NetherRouteHorizonStep> Steps,
    int SoftErosionLimit,
    int HardErosionLimit
)
{
    public bool IsVisibleHorizonComplete { get; init; }
    public NetherStrategyMode StrategyMode { get; init; } = NetherStrategyMode.Equipment;
    public NetherCodeFamily PrimaryResearchFamily { get; init; } = NetherCodeFamily.Unknown;
}

internal readonly record struct NetherRouteHorizonStepAudit(
    long NodeId,
    int StartErosion,
    int ProjectedErosion,
    int MinimumActiveCharacterHpPermille
);

internal readonly record struct NetherRouteHorizonRejection(long NodeId, string Reason);

/// <summary>
/// Safety eligibility is intentionally independent from reward ordering.  Later route ordering
/// may consume these metrics only after <see cref="IsEligible"/> is true.
/// </summary>
internal sealed record NetherRouteHorizonSafetyEvaluation
{
    public bool IsEligible { get; init; }
    public bool RequiresUserPause { get; init; }
    public int? FinalErosion { get; init; }
    public int? PeakErosion { get; init; }
    public int? MinimumActiveCharacterHpPermille { get; init; }
    public bool HasConfirmedRecoveryToOperatingBand { get; init; }
    public bool IsDedicatedRiskResearch { get; init; }
    public bool FinalErosionIsInPreferredRiskBand { get; init; }
    public bool MayRaiseErosionForRiskPayoff => false;
    public bool RequiresAuthoritativePostBattleReplan { get; init; }
    public string RejectionDetail { get; init; } = string.Empty;
    public IReadOnlyList<NetherRouteHorizonRejection> Rejections { get; init; } =
        Array.Empty<NetherRouteHorizonRejection>();
    public IReadOnlyList<NetherRouteHorizonStepAudit> Steps { get; init; } =
        Array.Empty<NetherRouteHorizonStepAudit>();
}

/// <summary>
/// Projects one complete visible branch in execution order.  It does not rank encounters or
/// invent outcomes beyond the visible graph: an unknown/random step rejects this branch only.
/// </summary>
internal sealed class NetherRouteHorizonSafetyPolicy
{
    private const int RiskPreferredMinimum = 50;
    private const int RiskOperatingMaximum = 70;
    private readonly NetherErosionPolicy _erosionPolicy = new();

    public NetherRouteHorizonSafetyEvaluation Evaluate(NetherRouteHorizonSafetyInput? input)
    {
        if (input == null)
            return Reject(0, "route-horizon-input-unavailable", requiresPause: true);
        if (!input.IsVisibleHorizonComplete)
            return Reject(0, "visible-horizon-incomplete", requiresPause: true);
        if (input.Steps == null || input.Steps.Count == 0
            || input.Steps[^1] == null
            || !input.Steps[^1].IsTerminalBoss
            || input.Steps[^1].NodeType != NetherFloorNodeType.Boss)
        {
            return Reject(0, "visible-terminal-boss-unavailable", requiresPause: true);
        }
        if (input.CurrentErosion is < 0
            || input.HardErosionLimit <= RiskOperatingMaximum
            || input.SoftErosionLimit != RiskOperatingMaximum
            || input.CurrentErosion >= input.HardErosionLimit)
        {
            return Reject(0, "invalid-route-horizon-erosion-input", requiresPause: true);
        }
        if (input.ActiveCharacterHpPermille == null
            || input.ActiveCharacterHpPermille.Count == 0
            || input.ActiveCharacterHpPermille.Any(hp => hp is <= 0 or > 1000))
        {
            return Reject(0, "invalid-route-horizon-active-hp", requiresPause: true);
        }

        int erosion = input.CurrentErosion;
        int peak = erosion;
        var hp = input.ActiveCharacterHpPermille.ToArray();
        int minimumHp = hp.Min();
        var audits = new List<NetherRouteHorizonStepAudit>(input.Steps.Count);
        var recoveryIndexes = new List<int>();
        int lastHighIndex = erosion >= RiskOperatingMaximum ? -1 : int.MinValue;
        bool requiresPostBattleReplan = false;

        for (int index = 0; index < input.Steps.Count; index++)
        {
            NetherRouteHorizonStep? step = input.Steps[index];
            if (step == null || step.NodeId <= 0
                || step.NodeType is NetherFloorNodeType.Unknown or NetherFloorNodeType.Default
                || step.ErosionModifiers == null)
            {
                return RejectWithState(step?.NodeId ?? 0, "unknown-route-step", true, erosion, peak, minimumHp, audits);
            }
            if (!step.IsOutcomeCertain)
            {
                return RejectWithState(
                    step.NodeId,
                    "unknown-route-outcome:" + step.NodeId,
                    true,
                    erosion,
                    peak,
                    minimumHp,
                    audits
                );
            }
            if (IsCombat(step.NodeType)
                && !step.HasExactPreEntryHpEvidence
                && !requiresPostBattleReplan)
            {
                return RejectWithState(
                    step.NodeId,
                    "combat-preentry-hp-unavailable:" + step.NodeId,
                    true,
                    erosion,
                    peak,
                    minimumHp,
                    audits
                );
            }
            if (IsCombat(step.NodeType)
                && step.HasExactPreEntryHpEvidence
                && (step.MinimumCombatEntryHpPermille is < 0 or > 1000
                    || hp.Where(value => value > 0)
                        .Any(value => value < step.MinimumCombatEntryHpPermille)))
            {
                return RejectWithState(
                    step.NodeId,
                    "combat-preentry-hp-below-safety-floor:" + step.NodeId,
                    false,
                    erosion,
                    peak,
                    minimumHp,
                    audits
                );
            }

            int startErosion = erosion;
            NetherErosionProjection projected = _erosionPolicy.ProjectBattle(
                erosion,
                step.BaseErosionDelta,
                step.ErosionModifiers,
                input.SoftErosionLimit,
                isMandatoryBoss: true
            );
            if (projected.PauseReason is NetherPauseReason.UnknownEffect or NetherPauseReason.InvalidConfiguration)
            {
                return RejectWithState(
                    step.NodeId,
                    "unknown-erosion-projection:" + step.NodeId + ":" + projected.Detail,
                    true,
                    erosion,
                    peak,
                    minimumHp,
                    audits
                );
            }
            erosion = projected.ProjectedErosion;
            peak = Math.Max(peak, erosion);

            int[]? projectedHp = null;
            string hpFailure = string.Empty;
            if (!requiresPostBattleReplan
                && !TryApplyHp(step, hp, out projectedHp, out hpFailure))
            {
                return RejectWithState(
                    step.NodeId,
                    hpFailure + ":" + step.NodeId,
                    false,
                    erosion,
                    peak,
                    minimumHp,
                    audits
                );
            }
            if (!requiresPostBattleReplan)
                hp = projectedHp!;
            int stepMinimumHp = step.HpRule is NetherRouteHpRule.TreasureGroupSurvival
                or NetherRouteHpRule.HpPaidKeyGroupSurvival
                    ? hp.Min()
                    : hp.Where(value => value > 0).DefaultIfEmpty(0).Min();
            minimumHp = Math.Min(minimumHp, stepMinimumHp);
            audits.Add(new NetherRouteHorizonStepAudit(step.NodeId, startErosion, erosion, stepMinimumHp));

            if (erosion >= input.HardErosionLimit)
            {
                return RejectWithState(
                    step.NodeId,
                    "lethal-erosion:" + step.NodeId,
                    false,
                    erosion,
                    peak,
                    minimumHp,
                    audits
                );
            }
            if (erosion > RiskOperatingMaximum)
                lastHighIndex = index;
            if (step.IsConfirmedRecovery
                && erosion < startErosion
                && erosion <= RiskOperatingMaximum)
            {
                recoveryIndexes.Add(index);
            }
            if (IsCombat(step.NodeType))
                requiresPostBattleReplan = true;
        }

        bool hasRecoveryAfterHigh = recoveryIndexes.Any(index => index > lastHighIndex);
        if (input.CurrentErosion >= RiskOperatingMaximum && !hasRecoveryAfterHigh)
        {
            return RejectWithState(
                0,
                "erosion-70-without-confirmed-recovery",
                true,
                erosion,
                peak,
                minimumHp,
                audits
            );
        }
        if (erosion > RiskOperatingMaximum)
        {
            return RejectWithState(
                0,
                "route-finishes-above-70",
                true,
                erosion,
                peak,
                minimumHp,
                audits
            );
        }
        if (peak > RiskOperatingMaximum && !hasRecoveryAfterHigh)
        {
            return RejectWithState(
                0,
                "transient-erosion-above-70-without-confirmed-recovery",
                true,
                erosion,
                peak,
                minimumHp,
                audits
            );
        }

        bool dedicatedRisk = input.StrategyMode == NetherStrategyMode.Research
            && input.PrimaryResearchFamily == NetherCodeFamily.Risk;
        return new NetherRouteHorizonSafetyEvaluation
        {
            IsEligible = true,
            FinalErosion = erosion,
            PeakErosion = peak,
            MinimumActiveCharacterHpPermille = minimumHp,
            HasConfirmedRecoveryToOperatingBand = recoveryIndexes.Count > 0,
            IsDedicatedRiskResearch = dedicatedRisk,
            FinalErosionIsInPreferredRiskBand = dedicatedRisk
                && erosion is >= RiskPreferredMinimum and <= RiskOperatingMaximum,
            RequiresAuthoritativePostBattleReplan = requiresPostBattleReplan,
            Steps = Array.AsReadOnly(audits.ToArray()),
        };
    }

    private static bool TryApplyHp(
        NetherRouteHorizonStep step,
        IReadOnlyList<int> current,
        out int[]? projected,
        out string failure
    )
    {
        projected = null;
        failure = string.Empty;
        try
        {
            if (!step.HpDeltaPermille.HasValue)
            {
                if (IsCombat(step.NodeType))
                {
                    projected = current.ToArray();
                    return true;
                }
                failure = "hp-projection-unavailable";
                return false;
            }
            int[] values = current.Select(hp => hp <= 0
                ? 0
                : checked(hp + step.HpDeltaPermille.Value)).ToArray();
            bool anySurvives = values.Any(hp => hp > 0);
            bool everyLivingSurvives = current
                .Select((hp, index) => (hp, index))
                .Where(entry => entry.hp > 0)
                .All(entry => values[entry.index] > 0);
            switch (step.HpRule)
            {
                case NetherRouteHpRule.OrdinaryAllLivingSurvive when !everyLivingSurvives:
                    failure = "ordinary-hp-cost-lethal";
                    return false;
                case NetherRouteHpRule.TreasureGroupSurvival when !anySurvives:
                    failure = "treasure-hp-cost-party-lethal";
                    return false;
                case NetherRouteHpRule.HpPaidKeyGroupSurvival when !anySurvives:
                    failure = "hp-paid-key-cost-party-lethal";
                    return false;
                case NetherRouteHpRule.OrdinaryAllLivingSurvive:
                case NetherRouteHpRule.TreasureGroupSurvival:
                case NetherRouteHpRule.HpPaidKeyGroupSurvival:
                    break;
                default:
                    failure = "unknown-hp-rule";
                    return false;
            }
            for (int index = 0; index < values.Length; index++)
                values[index] = Math.Clamp(values[index], 0, 1000);
            projected = values;
            return true;
        }
        catch (OverflowException)
        {
            failure = "hp-projection-overflow";
            return false;
        }
    }

    private static bool IsCombat(NetherFloorNodeType type) => type is
        NetherFloorNodeType.Battle or NetherFloorNodeType.MiniBoss or NetherFloorNodeType.Boss;

    private static NetherRouteHorizonSafetyEvaluation Reject(
        long nodeId,
        string reason,
        bool requiresPause
    ) => new()
    {
        RequiresUserPause = requiresPause,
        RejectionDetail = reason,
        Rejections = new[] { new NetherRouteHorizonRejection(nodeId, reason) },
    };

    private static NetherRouteHorizonSafetyEvaluation RejectWithState(
        long nodeId,
        string reason,
        bool requiresPause,
        int erosion,
        int peak,
        int minimumHp,
        IReadOnlyList<NetherRouteHorizonStepAudit> audits
    ) => new()
    {
        RequiresUserPause = requiresPause,
        FinalErosion = erosion,
        PeakErosion = peak,
        MinimumActiveCharacterHpPermille = minimumHp,
        RejectionDetail = reason,
        Rejections = new[] { new NetherRouteHorizonRejection(nodeId, reason) },
        Steps = Array.AsReadOnly(audits.ToArray()),
    };
}
