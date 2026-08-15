#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Maps exact native event/content identities to the only two HP-payment rules that permit an
/// individual active character to fall. Fresh client evidence resolves Treasure choices through
/// MNetherFloorEvents/MNetherFloorEventParts, whose target type 2 is Damage, while content type
/// 166 is ContentType.NetherKey. Anything less exact retains ordinary all-living survival.
/// </summary>
internal static class NetherRouteHpRuleMapper
{
    public static NetherRouteHpRule Map(
        NetherFloorNodeType floorKind,
        IEnumerable<NetherInteractiveOptionProjection>? exactSelectedOptions,
        NetherInteractiveWorstCaseProjection worstCase
    )
    {
        NetherInteractiveOptionProjection[] options = exactSelectedOptions?.ToArray()
            ?? Array.Empty<NetherInteractiveOptionProjection>();
        if (options.Length == 0
            || options.Any(option => !HasExactDeterministicHpProjection(option))
            || options.Min(option => option.HpDelta) != worstCase.HpDelta)
        {
            return NetherRouteHpRule.OrdinaryAllLivingSurvive;
        }

        if (floorKind == NetherFloorNodeType.Treasure
            && options.All(IsExactTreasureHpPayment))
        {
            return NetherRouteHpRule.TreasureGroupSurvival;
        }
        if (floorKind == NetherFloorNodeType.Event
            && options.All(IsExactHpPaidKeyEvent))
        {
            return NetherRouteHpRule.HpPaidKeyGroupSurvival;
        }
        return NetherRouteHpRule.OrdinaryAllLivingSurvive;
    }

    private static bool HasExactDeterministicHpProjection(NetherInteractiveOptionProjection option)
    {
        if (option == null
            || option.ExpectedEffects == null
            || option.ExpectedEffects.Count == 0
            || option.ExpectedEffects.Any(effect => effect == null
                || !effect.Known
                || !effect.ContentKnown
                || effect.Amount < 0))
        {
            return false;
        }

        try
        {
            int exactHpDelta = option.ExpectedEffects.Sum(effect => effect.Kind switch
            {
                NetherEffectKind.Heal => effect.Amount,
                NetherEffectKind.Damage => checked(-effect.Amount),
                _ => 0,
            });
            return exactHpDelta == option.HpDelta;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static bool IsExactTreasureHpPayment(NetherInteractiveOptionProjection option) =>
        option.AllowsPartialActiveDeaths
        && option.HpDelta < 0
        && option.ExpectedEffects.Count == 1
        && option.ExpectedEffects[0].Kind == NetherEffectKind.Damage
        && option.ExpectedEffects[0].Amount > 0;

    private static bool IsExactHpPaidKeyEvent(NetherInteractiveOptionProjection option) =>
        option.AllowsPartialActiveDeaths
        && option.HpDelta < 0
        && option.ExpectedEffects.Count == 2
        && option.ExpectedEffects.Count(effect => effect.Kind == NetherEffectKind.Damage
            && effect.Amount > 0) == 1
        && option.ExpectedEffects.Count(effect => effect.Kind == NetherEffectKind.TreasureKeyGain
            && effect.Amount == 1) == 1;
}
