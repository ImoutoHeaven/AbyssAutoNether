#nullable enable

using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// Managed DTO copied by an optional host adapter at the native boundary. The shipped bridge still
/// reads Engine/MasterDataStore when this adapter is absent. Keeping the DTO raw prevents a test or
/// host adapter from turning native type/rarity into semantic tiers.
/// </summary>
internal sealed record NetherRuntimeNativeEventPopupCapture(
    long TargetCharacterId,
    long EventId,
    IReadOnlyList<long> DeclaredPartIds,
    IReadOnlyList<NetherRuntimeNativeEventPart> Parts,
    IReadOnlyList<NetherRuntimeNativeBattle> Battles,
    IReadOnlyList<NetherRuntimeNativeItem> Items
)
{
    /// <summary>
    /// Snapshot identity copied by the same scoped adapter registration. A managed capture without
    /// this identity is not safe to bind to a live popup.
    /// </summary>
    public NetherSnapshotFingerprint? SnapshotFingerprint { get; init; }
    public long FloorId { get; init; }
    public long NodeId { get; init; }
}

internal sealed record NetherRuntimeNativeEventPart(
    long Id,
    int TargetType1,
    long SelectParameter1,
    int TargetType2,
    long SelectParameter2,
    int TargetType3,
    long SelectParameter3,
    int ContentType,
    long ContentId,
    int Amount
);

internal sealed record NetherRuntimeNativeBattle(
    long Id,
    int Type,
    long BattleStageId,
    int CodeDropRatio
);

internal sealed record NetherRuntimeNativeItem(
    long Id,
    long Type,
    int Rarity
);

internal delegate NetherRuntimeNativeEventPopupCapture? NetherRuntimeNativeEventPopupCaptureFactory(
    object controller,
    NetherRuntimePopupKind kind
);

/// <summary>
/// Managed DTO for a Shop popup copied by an adapter before the bridge maps it. It deliberately
/// carries only raw Shop content identity and item-master fields; key and reward semantics still
/// require the snapshot-scoped typed provider.
/// </summary>
internal sealed record NetherRuntimeManagedShopPopupCapture(
    IReadOnlyList<NetherRawShopContent> Contents,
    IReadOnlyList<NetherShopItemMaster> Items
)
{
    public NetherSnapshotFingerprint? SnapshotFingerprint { get; init; }
}

internal delegate NetherRuntimeManagedShopPopupCapture? NetherRuntimeManagedShopPopupCaptureFactory(
    object controller
);
