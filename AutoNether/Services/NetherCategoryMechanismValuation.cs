#nullable enable

using System.Collections.Generic;

namespace AutoNether.Services;

internal sealed class NetherCategoryMechanismValuation
{
    public NetherMechanismValue Evaluate(NetherCategoryThresholdInput input)
    {
        if (input == null
            || input.Effects == null
            || input.BeforeEffectiveCount < 0
            || input.AfterEffectiveCount < 0)
        {
            return NetherMechanismValue.Missing("category-threshold-input-unavailable");
        }

        var requiredCounts = new HashSet<int>();
        decimal before = 0;
        decimal after = 0;
        foreach (NetherCategoryThresholdEffect effect in input.Effects)
        {
            if (effect.RequiredCount <= 0 || !requiredCounts.Add(effect.RequiredCount))
                return NetherMechanismValue.Missing("category-threshold-relationship-unavailable");
            if (input.BeforeEffectiveCount >= effect.RequiredCount)
                before += effect.ActiveValue;
            if (input.AfterEffectiveCount >= effect.RequiredCount)
                after += effect.ActiveValue;
        }
        return NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.CategoryThresholdPayoff,
            after - before,
            "immediate-category-thresholds"
        );
    }
}
