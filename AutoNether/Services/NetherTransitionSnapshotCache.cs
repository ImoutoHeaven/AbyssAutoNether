#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// The fields which remain server-authoritative after FloorSelection has intentionally been
/// destroyed.  The bridge maps these values only after the packaged GET-only Nether datastore
/// sync has completed; this type contains no native callbacks or endpoint access.
/// </summary>
internal sealed record NetherAuthoritativeTransitionState
{
    public NetherSessionStatus Status { get; init; }
    public long NetherId { get; init; }
    public long MapId { get; init; }
    public long CurrentFloorId { get; init; }
    public int FloorLevel { get; init; }
    public int FloorIndex { get; init; }
    public int MaxFloorLevel { get; init; }
    public int ContinuanceFloorLevel { get; init; }
    public int MasterMaxFloorLevel { get; init; }
    public int ErosionPoint { get; init; }
    public int TicketCount { get; init; }
    public int SignalCount { get; init; }
    public int TreasureKeyCount { get; init; }
    public int NetherGold { get; init; }
    public int CodeReloadCount { get; init; }
    public int CodeCapacity { get; init; }
    public int LockReward { get; init; }
    public NetherContinuationTarget? ContinuationTarget { get; init; }
    public IReadOnlyList<NetherCodeState> Codes { get; init; } = Array.Empty<NetherCodeState>();
    public IReadOnlyList<NetherRewardItem> AcquiredItems { get; init; } = Array.Empty<NetherRewardItem>();
}

internal enum NetherTransitionSnapshotPurpose
{
    BattleSettlement,
    ContinueSettlement,
}

/// <summary>
/// Keeps the runtime bridge's snapshot-purpose dispatch in the testable transition seam.  Battle
/// settlement must retain its strict cached graph ownership checks; Continue settlement has a
/// different contract because the authoritative response may install the next map before the
/// rebound FloorSelection presentation model catches up.
/// </summary>
internal static class NetherTransitionSnapshotCompositionPolicy
{
    public static NetherRuntimeSnapshotResult Compose(
        NetherTransitionSnapshotCache cache,
        NetherAuthoritativeTransitionState state,
        bool requireFreshBattleCharacters,
        NetherTransitionSnapshotPurpose purpose
    ) => purpose == NetherTransitionSnapshotPurpose.ContinueSettlement
        ? cache.TryComposeContinueApplied(state)
        : cache.TryCompose(state, requireFreshBattleCharacters);
}

/// <summary>
/// Retains only the last fully validated FloorSelection graph/presentation snapshot.  During a
/// battle scene the graph no longer has a live controller, but the GET-only response still owns
/// session status, current floor coordinates, resources and code portfolio.  This cache joins
/// those two sources only when Nether/map identity and the exact current node coordinate agree.
/// A zero master floor ID is tolerated only during Battle, or on its result-page Play/Sleep
/// state when fresh battle-result characters prove that the transition belongs to this combat.
/// </summary>
internal sealed class NetherTransitionSnapshotCache
{
    private readonly object _gate = new();
    private NetherSnapshot? _lastFullSnapshot;
    private IReadOnlyList<NetherCharacterState>? _battleResultCharacters;

    public void ObserveFullSnapshot(NetherSnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        lock (_gate)
        {
            _lastFullSnapshot = snapshot;
            // A rebuilt FloorSelection model supersedes the transient battle result payload.
            _battleResultCharacters = null;
        }
    }

    public void BeginBattle()
    {
        lock (_gate)
            _battleResultCharacters = null;
    }

    public bool ObserveBattleResultCharacters(IReadOnlyList<NetherCharacterState>? characters)
    {
        if (!TryValidateCharacters(characters, out NetherCharacterState[]? copied))
            return false;
        lock (_gate)
            _battleResultCharacters = copied;
        return true;
    }

    public NetherRuntimeSnapshotResult TryCompose(
        NetherAuthoritativeTransitionState state,
        bool requireFreshBattleCharacters
    )
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        NetherSnapshot? cached;
        IReadOnlyList<NetherCharacterState>? battleCharacters;
        lock (_gate)
        {
            cached = _lastFullSnapshot;
            battleCharacters = _battleResultCharacters;
        }

        if (cached == null)
            return NetherRuntimeSnapshotResult.Failure("missing-cached-floor-selection-snapshot");
        if (state.NetherId <= 0 || state.MapId <= 0
            || state.NetherId != cached.NetherId || state.MapId != cached.MapId)
        {
            return NetherRuntimeSnapshotResult.Failure(
                "cached-transition-owner-mismatch:cached=" + cached.NetherId + ":" + cached.MapId
                + ":fresh=" + state.NetherId + ":" + state.MapId
            );
        }
        // The packaged client clears m_nether_map_floor_id to zero twice around a combat:
        // while Status=Battle, and again on the result page after the clear response has
        // already changed Status to Play or (for a segment-ending Boss) Sleep.  Those latter
        // states are distinguishable from invalid ordinary snapshots only by fresh, validated
        // battle-result characters owned by this cache.  All transitions may recover the
        // master floor solely from one exact cached (floor_level, floor_index) coordinate.
        bool battleCoordinateFallback = state.Status == NetherSessionStatus.Battle
            && state.CurrentFloorId == 0;
        bool postBattleCoordinateFallback = state.Status is (
                NetherSessionStatus.Play or NetherSessionStatus.Sleep
            )
            && state.CurrentFloorId == 0
            && requireFreshBattleCharacters
            && battleCharacters != null;
        bool coordinateFallback = battleCoordinateFallback || postBattleCoordinateFallback;
        if (state.FloorLevel < 0 || state.FloorIndex < 0
            || state.CurrentFloorId <= 0 && !coordinateFallback)
            return NetherRuntimeSnapshotResult.Failure("invalid-authoritative-current-floor");
        if (state.Codes == null || state.AcquiredItems == null)
            return NetherRuntimeSnapshotResult.Failure("missing-authoritative-transition-collections");

        NetherFloorNode[] current = cached.Floors
            .Where(floor => floor != null
                && (coordinateFallback || floor.FloorId == state.CurrentFloorId)
                && floor.FloorLevel == state.FloorLevel
                && floor.ApiFloorIndex == state.FloorIndex)
            .ToArray();
        if (current.Length != 1 || current[0].NodeId <= 0)
        {
            if (coordinateFallback)
            {
                return NetherRuntimeSnapshotResult.Failure(
                    (battleCoordinateFallback
                        ? "authoritative-battle-coordinate-not-unique:level="
                        : "authoritative-postbattle-coordinate-not-unique:level=")
                    + state.FloorLevel
                    + ":api-index=" + state.FloorIndex
                    + ":matches=" + current.Length
                );
            }
            return NetherRuntimeSnapshotResult.Failure(
                "authoritative-current-node-not-unique:master=" + state.CurrentFloorId
                + ":level=" + state.FloorLevel
                + ":api-index=" + state.FloorIndex
                + ":matches=" + current.Length
            );
        }

        IReadOnlyList<NetherCharacterState> characters;
        if (requireFreshBattleCharacters)
        {
            if (battleCharacters == null)
                return NetherRuntimeSnapshotResult.Failure("missing-authoritative-battle-result-characters");
            characters = battleCharacters;
        }
        else
        {
            characters = battleCharacters ?? cached.Characters;
        }
        if (!TryValidateCharacters(characters, out NetherCharacterState[]? validatedCharacters))
            return NetherRuntimeSnapshotResult.Failure("invalid-transition-characters");

        NetherCodeState[] codes = state.Codes.ToArray();
        if (codes.Any(code => code == null || code.CodeId <= 0)
            || codes.GroupBy(code => code.CodeId).Any(group => group.Count() != 1))
        {
            return NetherRuntimeSnapshotResult.Failure("invalid-authoritative-transition-codes");
        }

        var snapshot = new NetherSnapshot
        {
            Status = state.Status,
            NetherId = state.NetherId,
            MapId = state.MapId,
            // Live Battle and its result-page Play/Sleep transition intentionally report
            // m_nether_map_floor_id=0.  Post-battle cases are admitted only with fresh result
            // characters; all cases require one exact cached coordinate.  Ordinary
            // Play/Wait/Sleep snapshots cannot silently drift to another map node.
            CurrentFloorId = current[0].FloorId,
            CurrentNodeId = current[0].NodeId,
            FloorLevel = state.FloorLevel,
            FloorIndex = state.FloorIndex,
            MaxFloorLevel = state.MaxFloorLevel,
            ContinuanceFloorLevel = state.ContinuanceFloorLevel,
            MasterMaxFloorLevel = cached.MasterMaxFloorLevel,
            ErosionPoint = state.ErosionPoint,
            TicketCount = state.TicketCount,
            SignalCount = state.SignalCount,
            TreasureKeyCount = state.TreasureKeyCount,
            NetherGold = state.NetherGold,
            CodeReloadCount = state.CodeReloadCount,
            CodeCapacity = state.CodeCapacity,
            LockReward = state.LockReward,
            ContinuationTarget = state.ContinuationTarget,
            Characters = validatedCharacters!,
            Codes = codes,
            Floors = cached.Floors,
            AcquiredItems = state.AcquiredItems.ToArray(),
            CharacterHpHash = CreateCharacterHash(validatedCharacters!),
            CodeHash = CreateCodeHash(codes),
            MapHash = cached.MapHash,
        };
        return NetherRuntimeSnapshotResult.Success(snapshot);
    }

    /// <summary>
    /// Builds the narrow datastore-authoritative snapshot used only to prove a completed
    /// Continue.  A valid Continue can replace the map before the rebound controller's private
    /// presentation model is rebuilt, so the old map ID, graph and current node must not be used
    /// as ownership evidence.  The Nether session identity remains stable and is still checked.
    /// </summary>
    public NetherRuntimeSnapshotResult TryComposeContinueApplied(
        NetherAuthoritativeTransitionState state
    )
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        NetherSnapshot? cached;
        lock (_gate)
            cached = _lastFullSnapshot;

        if (cached == null)
            return NetherRuntimeSnapshotResult.Failure("missing-cached-floor-selection-snapshot");
        if (state.NetherId <= 0 || state.NetherId != cached.NetherId)
        {
            return NetherRuntimeSnapshotResult.Failure(
                "cached-continue-owner-mismatch:cached=" + cached.NetherId
                + ":fresh=" + state.NetherId
            );
        }
        // The packaged client can enter a new Continue segment before any node in that segment
        // has been selected. NetherData.Apply preserves MNetherMapFloorId == 0 while the rebound
        // NetherModel independently resolves CurrentFloorModel from (FloorLevel, FloorIndex).
        // Keep this exception purpose-specific and status-specific: zero is mutation evidence for
        // that Play boundary, never a graph/node identity and never valid for another status.
        bool isCoordinateOnlyPlayEntry = state.Status == NetherSessionStatus.Play
            && state.CurrentFloorId == 0;
        if (state.Status == NetherSessionStatus.Unknown
            || state.MapId <= 0
            || (state.CurrentFloorId <= 0 && !isCoordinateOnlyPlayEntry)
            || state.FloorLevel <= 0
            || state.FloorIndex < 0
            || state.TicketCount < 0)
        {
            return NetherRuntimeSnapshotResult.Failure("invalid-authoritative-continue-state");
        }
        if (state.Codes == null || state.AcquiredItems == null)
            return NetherRuntimeSnapshotResult.Failure("missing-authoritative-transition-collections");
        if (!TryValidateCharacters(cached.Characters, out NetherCharacterState[]? characters))
            return NetherRuntimeSnapshotResult.Failure("invalid-transition-characters");

        NetherCodeState[] codes = state.Codes.ToArray();
        if (codes.Any(code => code == null || code.CodeId <= 0)
            || codes.GroupBy(code => code.CodeId).Any(group => group.Count() != 1))
        {
            return NetherRuntimeSnapshotResult.Failure("invalid-authoritative-transition-codes");
        }

        var snapshot = new NetherSnapshot
        {
            Status = state.Status,
            NetherId = state.NetherId,
            MapId = state.MapId,
            CurrentFloorId = state.CurrentFloorId,
            // The new presentation graph is deliberately not synthesized from the prior map.
            // The normal full snapshot path will supply its node identity after _netherModel
            // catches up; Continue settlement validates only datastore-owned postconditions.
            CurrentNodeId = 0,
            FloorLevel = state.FloorLevel,
            FloorIndex = state.FloorIndex,
            MaxFloorLevel = state.MaxFloorLevel,
            ContinuanceFloorLevel = state.ContinuanceFloorLevel,
            MasterMaxFloorLevel = state.MasterMaxFloorLevel,
            ErosionPoint = state.ErosionPoint,
            TicketCount = state.TicketCount,
            SignalCount = state.SignalCount,
            TreasureKeyCount = state.TreasureKeyCount,
            NetherGold = state.NetherGold,
            CodeReloadCount = state.CodeReloadCount,
            CodeCapacity = state.CodeCapacity,
            LockReward = state.LockReward,
            ContinuationTarget = state.ContinuationTarget,
            // Characters are not part of the Continue postcondition and are absent from the
            // datastore response. Retaining the last validated party keeps the snapshot shape
            // valid without claiming that an old map graph belongs to the new map.
            Characters = characters!,
            Codes = codes,
            Floors = Array.Empty<NetherFloorNode>(),
            AcquiredItems = state.AcquiredItems.ToArray(),
            CharacterHpHash = CreateCharacterHash(characters!),
            CodeHash = CreateCodeHash(codes),
            MapHash = string.Empty,
        };
        return NetherRuntimeSnapshotResult.Success(snapshot);
    }

    public void Clear()
    {
        lock (_gate)
        {
            _lastFullSnapshot = null;
            _battleResultCharacters = null;
        }
    }

    private static bool TryValidateCharacters(
        IReadOnlyList<NetherCharacterState>? characters,
        out NetherCharacterState[]? copied
    )
    {
        copied = null;
        if (characters == null || characters.Count == 0)
            return false;
        NetherCharacterState[] values = characters.ToArray();
        if (values.Any(character => character.CharacterId <= 0
                || character.HpPermille is < 0 or > 1000)
            || values.GroupBy(character => character.CharacterId).Any(group => group.Count() != 1))
        {
            return false;
        }
        copied = values;
        return true;
    }

    internal static string CreateCharacterHash(IEnumerable<NetherCharacterState> characters) =>
        string.Join(
            ";",
            characters.OrderBy(character => character.CharacterId).Select(character =>
                character.CharacterId.ToString(CultureInfo.InvariantCulture) + ":"
                + character.HpPermille.ToString(CultureInfo.InvariantCulture) + ":"
                + (character.IsActive ? "1" : "0")
            )
        );

    internal static string CreateCodeHash(IEnumerable<NetherCodeState> codes) =>
        NetherCodeIdentity.CreatePortfolio(codes);
}
