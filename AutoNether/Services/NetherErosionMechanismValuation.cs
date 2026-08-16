#nullable enable

using System;

namespace AutoNether.Services;

internal sealed class NetherErosionMechanismValuation
{
    public NetherMechanismValue Evaluate(NetherErosionLinkedPayoffInput input)
    {
        if (input == null
            || input.ConfirmedCombats == null
            || input.MinimumErosionPermille < 0
            || input.MaximumErosionPermille <= input.MinimumErosionPermille
            || !input.BuffType.IsKnown
            || input.ParameterReferenceKind == NetherStrategyBuffParameterReferenceKind.Unknown)
        {
            return NetherMechanismValue.Missing("erosion-linked-input-unavailable");
        }

        decimal total = 0;
        foreach (NetherConfirmedCombatErosion combat in input.ConfirmedCombats)
        {
            if (combat.FloorId <= 0 || !combat.IsExact)
            {
                return NetherMechanismValue.ReachableUnquantified(
                    "confirmed-combat-erosion-unavailable"
                );
            }
            float normalized = Math.Clamp(
                (combat.ProjectedErosionPermille - input.MinimumErosionPermille)
                    / (float)(input.MaximumErosionPermille - input.MinimumErosionPermille),
                0f,
                1f
            );
            float interpolated = (float)input.MinimumValue
                + ((float)input.MaximumValue - (float)input.MinimumValue) * normalized;
            total += (decimal)Math.Round((double)interpolated);
        }
        return NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.ErosionLinkedPayoff,
            total,
            "per-confirmed-combat-erosion",
            input.BuffType,
            input.ParameterReferenceKind
        );
    }
}
