#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Holds an effect prediction only across the one native action that produced it.  The next
/// authoritative snapshot validates erosion exactly and every living character against the
/// safe per-character HP bound (unless code changed, which requires a rebaseline).
/// </summary>
internal readonly record struct NetherProjectionObservation(
    bool IsDrift,
    bool RequiresRebaseline,
    NetherPauseReason PauseReason,
    string Detail
);

/// <summary>
/// The route gate proves that this bound is survivable for every active character. The native
/// Event endpoint later supplies the actual per-character result, so it is not an exact-target
/// promise for every member.
/// </summary>
internal readonly record struct NetherExpectedHpRange(int Before, int Expected);

internal sealed class NetherActionProjectionCalibration
{
    private readonly NetherErosionPolicy _erosionPolicy = new();
    private int? _predictedErosion;
    private string _codeFingerprint = string.Empty;
    private Dictionary<long, NetherExpectedHpRange>? _expectedHp;
    private HashSet<long>? _expectedActiveCharacterIds;
    private bool _allowPartialActiveDeaths;

    public void Expect(NetherEventDecision decision, NetherSnapshot before)
    {
        if (decision == null)
            throw new ArgumentNullException(nameof(decision));
        if (before == null)
            throw new ArgumentNullException(nameof(before));

        _predictedErosion = decision.ProjectedErosion;
        _codeFingerprint = before.CodeHash ?? string.Empty;
        _allowPartialActiveDeaths = decision.AllowsPartialActiveDeaths;
        _expectedHp = decision.HpDelta != 0
            ? before.Characters
                .Where(character => character.IsActive)
                .ToDictionary(
                    character => character.CharacterId,
                    character => new NetherExpectedHpRange(
                        character.HpPermille,
                        Math.Max(0, Math.Min(1000, checked(character.HpPermille + decision.HpDelta)))
                    )
                )
            : null;
        _expectedActiveCharacterIds = _expectedHp == null
            ? null
            : new HashSet<long>(_expectedHp.Keys);
    }

    public NetherProjectionObservation Observe(NetherSnapshot after)
    {
        if (after == null)
            throw new ArgumentNullException(nameof(after));
        if (_predictedErosion == null)
            return new NetherProjectionObservation(false, false, NetherPauseReason.None, string.Empty);

        int predictedErosion = _predictedErosion.Value;
        string codeFingerprint = _codeFingerprint;
        Dictionary<long, NetherExpectedHpRange>? expectedHp = _expectedHp;
        HashSet<long>? expectedActiveCharacterIds = _expectedActiveCharacterIds;
        bool allowPartialActiveDeaths = _allowPartialActiveDeaths;
        Clear();

        NetherErosionObservation erosion = _erosionPolicy.CompareObserved(
            predictedErosion,
            after.ErosionPoint,
            codeFingerprint,
            after.CodeHash
        );
        if (erosion.IsDrift)
            return new NetherProjectionObservation(true, false, erosion.PauseReason, erosion.Detail);

        if (expectedHp != null && expectedActiveCharacterIds != null)
        {
            var observedById = new Dictionary<long, NetherCharacterState>();
            foreach (NetherCharacterState character in after.Characters)
            {
                if (!observedById.TryAdd(character.CharacterId, character))
                    return HpDrift(erosion, "hp-projection-duplicate-character");
            }
            HashSet<long> observedActiveIds = after.Characters
                .Where(character => character.IsActive)
                .Select(character => character.CharacterId)
                .ToHashSet();
            if ((!allowPartialActiveDeaths && !observedActiveIds.SetEquals(expectedActiveCharacterIds))
                || (allowPartialActiveDeaths && observedActiveIds.Any(id => !expectedActiveCharacterIds.Contains(id))))
            {
                return HpDrift(
                    erosion,
                    allowPartialActiveDeaths && observedActiveIds.Count == 0
                        ? "hp-projection-full-party-death"
                        : "hp-projection-active-set-drift"
                );
            }
            foreach ((long characterId, NetherExpectedHpRange expected) in expectedHp)
            {
                if (!observedById.TryGetValue(characterId, out NetherCharacterState observed))
                    return HpDrift(erosion, "hp-projection-missing-character:" + characterId);
                if (expected.Expected <= 0 && !allowPartialActiveDeaths)
                    return HpDrift(erosion, "hp-projection-ordinary-death-not-authorized:" + characterId);
                if (expected.Expected <= 0 && allowPartialActiveDeaths)
                {
                    if (observed.IsActive || observed.HpPermille != 0)
                        return HpDrift(erosion, "hp-projection-authorized-death-state:" + characterId);
                    continue;
                }
                if (allowPartialActiveDeaths)
                {
                    if (!observed.IsActive || observed.HpPermille != expected.Expected)
                        return HpDrift(erosion, "hp-projection-drift:" + characterId);
                    continue;
                }
                if (!observed.IsActive
                    || !IsWithinAuthoritativeEventHpBound(expected, observed.HpPermille))
                {
                    return HpDrift(
                        erosion,
                        "hp-projection-outside-safe-bound:" + characterId
                            + ":observed=" + observed.HpPermille
                            + ":before=" + expected.Before
                            + ":expected=" + expected.Expected
                    );
                }
            }
        }

        return new NetherProjectionObservation(
            false,
            erosion.RequiresRebaseline,
            NetherPauseReason.None,
            erosion.RequiresRebaseline ? erosion.Detail : string.Empty
        );
    }

    public void Clear()
    {
        _predictedErosion = null;
        _codeFingerprint = string.Empty;
        _expectedHp = null;
        _expectedActiveCharacterIds = null;
        _allowPartialActiveDeaths = false;
    }

    private static bool IsWithinAuthoritativeEventHpBound(
        NetherExpectedHpRange expected,
        int observed
    ) => observed >= Math.Min(expected.Before, expected.Expected)
        && observed <= Math.Max(expected.Before, expected.Expected);

    private static NetherProjectionObservation HpDrift(
        NetherErosionObservation erosion,
        string detail
    ) => new(
        true,
        erosion.RequiresRebaseline,
        NetherPauseReason.UnsafeHp,
        detail
    );
}
