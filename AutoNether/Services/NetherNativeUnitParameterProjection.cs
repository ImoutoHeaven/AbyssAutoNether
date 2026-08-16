#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Pure mirror of the current native <c>ParameterCalculator.Calculate_Unit</c> arithmetic. Callers
/// must validate the zero-delta result against the authoritative effective parameter before using
/// a projected delta, so future native changes fail closed rather than silently changing value.
/// </summary>
internal static class NetherNativeUnitParameterProjection
{
    public static bool TryCalculate(
        NetherStrategyParameterCalculationEvidence input,
        int additionalAllTargetModifier,
        out int value
    )
    {
        value = 0;
        try
        {
            float fixedTotal = input.CharacterValue;
            fixedTotal += input.SelfAbilityFixedValue;
            fixedTotal += input.EquipmentValue;
            fixedTotal += input.AllTargetAbilityFixedValue;
            int truncatedFixed = checked((int)fixedTotal);

            float modifier = input.SelfAbilityModifier;
            modifier += input.AllTargetAbilityModifier;
            modifier += additionalAllTargetModifier;
            modifier += input.EquipmentEnchantModifier;
            modifier += input.TotalBuildingModifier;
            modifier += 1000f;
            float projected = truncatedFixed * modifier * 0.001f;
            projected += input.SupportBuff;
            double rounded = Math.Round(projected, MidpointRounding.ToEven);
            if (rounded < 0 || rounded > int.MaxValue)
                return false;
            value = checked((int)rounded);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
