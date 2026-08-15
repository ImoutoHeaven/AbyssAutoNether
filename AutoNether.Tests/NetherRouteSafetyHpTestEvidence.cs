#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using AutoNether.Services;

namespace AutoNether.Tests;

internal static class NetherRouteSafetyHpTestEvidence
{
    public static NetherActivePartyHpSafety Single(long characterId, int hpPermille) =>
        FromStates(new[] { new NetherCharacterState(characterId, hpPermille, IsActive: true) });

    public static NetherActivePartyHpSafety FromStates(
        IEnumerable<NetherCharacterState> characters
    )
    {
        NetherActiveLivingMemberHp[] living = characters
            .Where(character => character.IsActive)
            .OrderBy(character => character.CharacterId)
            .Select(character => new NetherActiveLivingMemberHp(
                character.CharacterId,
                character.HpPermille
            ))
            .ToArray();
        if (living.Length == 0)
            throw new ArgumentException("A known test HP surface requires a living member.", nameof(characters));
        return new NetherActivePartyHpSafety(
            IsKnown: true,
            MinimumHpPermille: living.Min(member => member.HpPermille),
            Detail: string.Empty
        )
        {
            LivingMembers = Array.AsReadOnly(living),
        };
    }
}
