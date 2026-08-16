#nullable enable

using System;

namespace AutoNether.Services;

internal sealed class NetherChargeMechanismValuation
{
    private const float SharedManaMinimum = 0f;
    private const float SharedManaMaximum = 10f;
    private const float FloatEvidenceTolerance = 0.0001f;

    public NetherMechanismValue EvaluateSharedMana(NetherSharedManaInjectionInput input)
    {
        if (input == null
            || input.RegisteredModifierSteps == null
            || input.CurrentSharedEnergy < SharedManaMinimum
            || input.CurrentSharedEnergy > SharedManaMaximum
            || input.RawEnergyPerRecipient < 0
            || input.ScopeMatchCount < 0)
        {
            return NetherMechanismValue.Missing("shared-mana-input-unavailable");
        }

        // Project.dll 53806a5b...1300 AbilityChargeMana.ExecuteInternal applies BuffType
        // ChargeManaRate (190), group 63, only when its summed modifier is strictly positive.
        float perRecipient = input.RawEnergyPerRecipient;
        if (input.AbilityChargeModifierPermille > 0)
            perRecipient *= 1f + input.AbilityChargeModifierPermille / 1000f;
        foreach (NetherSharedManaModifierStep modifier in input.RegisteredModifierSteps)
        {
            if (Math.Abs(modifier.InputEnergy - perRecipient) > FloatEvidenceTolerance
                || modifier.OutputEnergy < SharedManaMinimum)
            {
                return NetherMechanismValue.ReachableUnquantified(
                    "shared-mana-modifier-chain-unavailable"
                );
            }
            perRecipient = modifier.OutputEnergy;
        }

        float proposed = Math.Max(SharedManaMinimum, perRecipient * input.ScopeMatchCount);
        float remaining = SharedManaMaximum - input.CurrentSharedEnergy;
        return NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.SharedManaEnergy,
            (decimal)Math.Clamp(proposed, SharedManaMinimum, remaining),
            "native-shared-mana-capacity"
        );
    }

    public NetherMechanismValue EvaluateInitialSkillCharge(NetherInitialSkillChargeInput input)
    {
        if (input == null || input.Recipients == null || input.ChargePermille < 0)
            return NetherMechanismValue.Missing("initial-skill-charge-input-unavailable");

        decimal total = 0;
        foreach (NetherSkillChargeRecipient recipient in input.Recipients)
        {
            if (!IsValid(recipient))
                return NetherMechanismValue.Missing("skill-charge-recipient-unavailable");
            int rawCharge = (int)Math.Floor(recipient.MaxCharge * (input.ChargePermille / 1000m));
            decimal multiplier = Math.Max(
                0,
                1m + (recipient.PositiveModifierPermille - recipient.NegativeModifierPermille) / 1000m
            );
            decimal effective = rawCharge
                * multiplier
                * (recipient.ChargeEfficiencyPermille / 1000m);
            total += Math.Min(effective, recipient.MaxCharge - (decimal)recipient.CurrentCharge);
        }
        return NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.InitialSkillCharge,
            total,
            "native-per-recipient-ready-threshold"
        );
    }

    public NetherMechanismValue EvaluateRecurringSkillCharge(NetherRecurringSkillChargeInput input)
    {
        if (input == null || input.Segments == null || input.ModifierPermille < -1000)
            return NetherMechanismValue.Missing("recurring-skill-charge-input-unavailable");

        decimal multiplier = 1m + input.ModifierPermille / 1000m;
        decimal total = 0;
        NetherSkillChargeTimelineSegment? previous = null;
        foreach (NetherSkillChargeTimelineSegment segment in input.Segments)
        {
            if (segment.CharacterId <= 0
                || segment.StartingCharge < 0
                || segment.MaxCharge <= 0
                || segment.StartingCharge > segment.MaxCharge
                || segment.NativeBaseCharge < 0)
            {
                return NetherMechanismValue.Missing("skill-charge-timeline-segment-unavailable");
            }
            if (previous is NetherSkillChargeTimelineSegment prior
                && prior.CharacterId == segment.CharacterId
                && prior.ResetAfterSegment
                && segment.StartingCharge != 0)
            {
                return NetherMechanismValue.ReachableUnquantified(
                    "post-reset-skill-charge-state-unavailable"
                );
            }

            decimal baselineEnd = Math.Min(
                segment.MaxCharge,
                (decimal)segment.StartingCharge + (decimal)segment.NativeBaseCharge
            );
            decimal candidateEnd = Math.Min(
                segment.MaxCharge,
                (decimal)segment.StartingCharge + (decimal)segment.NativeBaseCharge * multiplier
            );
            total += candidateEnd - baselineEnd;
            previous = segment;
        }
        return NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.RecurringSkillCharge,
            total,
            "native-recurring-charge-timeline"
        );
    }

    private static bool IsValid(NetherSkillChargeRecipient? recipient) => recipient != null
        && recipient.CharacterId > 0
        && recipient.CurrentCharge >= 0
        && recipient.MaxCharge > 0
        && recipient.CurrentCharge <= recipient.MaxCharge
        && recipient.ChargeEfficiencyPermille >= 0
        && recipient.PositiveModifierPermille >= 0
        && recipient.NegativeModifierPermille >= 0;
}
