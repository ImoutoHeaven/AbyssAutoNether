#nullable enable

using System;
using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// Checked projection of the two Event resources.  The native rows provide amounts, but an
/// overflowing or negative projection is not a usable safety fact and must remain unknown.
/// </summary>
internal static class NetherEventResourceProjection
{
    public static bool TryProject(
        int currentGold,
        int currentKeys,
        IReadOnlyList<NetherEffect>? effects,
        out int projectedGold,
        out int projectedKeys
    )
    {
        projectedGold = 0;
        projectedKeys = 0;
        if (currentGold < 0 || currentKeys < 0 || effects == null)
            return false;

        try
        {
            int goldDelta = 0;
            int keyDelta = 0;
            foreach (NetherEffect effect in effects)
            {
                if (effect == null || effect.Amount < 0)
                    return false;

                switch (effect.Kind)
                {
                    case NetherEffectKind.NetherGoldUsed:
                        goldDelta = checked(goldDelta - effect.Amount);
                        break;
                    case NetherEffectKind.NetherGoldGain:
                        goldDelta = checked(goldDelta + effect.Amount);
                        break;
                    case NetherEffectKind.TreasureKeyUsed:
                        keyDelta = checked(keyDelta - effect.Amount);
                        break;
                    case NetherEffectKind.TreasureKeyGain:
                        keyDelta = checked(keyDelta + effect.Amount);
                        break;
                }
            }

            projectedGold = checked(currentGold + goldDelta);
            projectedKeys = checked(currentKeys + keyDelta);
            return projectedGold >= 0 && projectedKeys >= 0;
        }
        catch (OverflowException)
        {
            projectedGold = 0;
            projectedKeys = 0;
            return false;
        }
    }
}
