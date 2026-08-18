#nullable enable

using System;
using System.Collections.Generic;

namespace AutoNether.Services;

internal enum NetherSessionStatus
{
    Unknown = 0,
    NotPlayed = 1,
    Play = 2,
    Wait = 3,
    Battle = 5,
    Sleep = 6,
    Lose = 7,
    Clear = 8,
}

internal enum NetherFloorNodeType
{
    Unknown = 0,
    Battle = 1,
    Boss = 2,
    MiniBoss = 3,
    Event = 4,
    Recovery = 5,
    Shop = 6,
    Treasure = 7,
    Default = 8,
}

internal enum NetherAutoClimbPhase
{
    Disabled,
    Reconciling,
    Stable,
    ExecutingNativeAction,
    AwaitingBattleSceneHandoff,
    AwaitingContinueSceneHandoff,
    AwaitingBattle,
    AwaitingBattleSettlement,
    AwaitingBattleResultContinuation,
    AwaitingSceneChange,
    Paused,
    Completed,
}

internal enum NetherActionKind
{
    None,
    Reconcile,
    /// <summary>Native NotPlayed run start at the mode-derived authoritative floor.</summary>
    StartRun,
    SelectFloor,
    SelectEventOption,
    LeaveShop,
    BuyShopItem,
    SelectCode,
    ReloadCode,
    /// <summary>
    /// Exact Abyss-code offer cancel flow.  This is terminal for the owned CodeOffer only
    /// after the generated HandleCancelSequenceAsync UniTask has completed; it never means a
    /// visual popup close.
    /// </summary>
    KeepCode,
    Continue,
    FinishAtCheckpoint,
    SelectReturnItems,
    AwaitNativeFlow,
    BattleSettlement,
    /// <summary>
    /// A foreground code offer owned by the exact
    /// FloorSelection.HandleStartEventByStatusAsync task captured after resume/restart.
    /// </summary>
    RecoveredCodeOffer,
    RestoreBattleSettings,
    /// <summary>Native floor-event Abyss-code conversion: remove one current code and receive one server-selected code.</summary>
    TransformCode,
}

internal enum NetherPauseReason
{
    None,
    NotInNether,
    NotPlayed,
    UnknownStatus,
    AmbiguousServerOutcome,
    BindingUnavailable,
    InvalidGraph,
    NoSafeRoute,
    UnknownFloor,
    UnsafeErosion,
    ErosionDrift,
    UnsafeHp,
    UnknownEffect,
    UnknownMasterData,
    UnsupportedPopup,
    InvalidConfiguration,
    BattleSettingsLeaseFault,
    BattleLifecycleFault,
    BattleLifecycleCanceled,
    BattleSettlementUnchanged,
    BattleSettlementWrongTarget,
    /// <summary>The authoritative post-battle snapshot cannot prove the immutable safety projection.</summary>
    BattleProjectionUnknown,
    /// <summary>The authoritative post-battle state exceeded or contradicted the immutable safety projection.</summary>
    BattleProjectionDrift,
    BattleSceneLost,
    ContinueLifecycleFault,
    ContinueLifecycleCanceled,
    ContinueTeardownTimeout,
    ContinueRebindTimeout,
    ContinueRebindWrongScene,
    ContinueSettlementWrongTarget,
    /// <summary>The foreground Event no longer matches the immutable option commitment.</summary>
    StaleEventCommitment,
    ResultLifecycleFault,
    ResultLifecycleCanceled,
    TargetReachedOutsideCheckpoint,
    Lose,
    UserDisabled,
}

internal enum NetherActionOutcome
{
    Applied,
    NotApplied,
    Ambiguous,
}

/// <summary>
/// The bridge never treats an unavailable native binding as a failed action: a failed
/// binding means that no request was made and the coordinator must pause safely.
/// </summary>
internal enum NetherNativeActionResultKind
{
    Started,
    Completed,
    Rejected,
    UnknownOutcome,
    BindingUnavailable,
}

internal readonly record struct NetherNativeActionResult(
    NetherNativeActionResultKind Kind,
    string Detail
)
{
    public static NetherNativeActionResult Started(string detail) => new(NetherNativeActionResultKind.Started, detail);

    public static NetherNativeActionResult Completed(string detail) => new(NetherNativeActionResultKind.Completed, detail);

    public static NetherNativeActionResult Rejected(string detail) => new(NetherNativeActionResultKind.Rejected, detail);

    public static NetherNativeActionResult UnknownOutcome(string detail) => new(NetherNativeActionResultKind.UnknownOutcome, detail);

    public static NetherNativeActionResult BindingUnavailable(string detail) => new(NetherNativeActionResultKind.BindingUnavailable, detail);
}

/// <summary>
/// A versioned native signature, represented without a reflection dependency so its
/// fail-closed matching rules can be characterized in the pure test project.
/// </summary>
internal sealed record NetherNativeMethodDescriptor(
    string Name,
    IReadOnlyList<string> ParameterTypeNames,
    string ReturnTypeName
)
{
    public int Arity => ParameterTypeNames.Count;

    /// <summary>
    /// Optional because existing non-reflection policy descriptors intentionally describe only
    /// a callable shape.  Exact generated callbacks may additionally require their proven
    /// static/instance ownership, which prevents an adjacent compiler-generated method from
    /// satisfying a superficially identical signature.
    /// </summary>
    public bool? IsStatic { get; init; }
}

internal readonly record struct NetherNativeBindingSelection(
    NetherNativeActionResultKind ResultKind,
    NetherNativeMethodDescriptor? Method,
    string Detail
);

internal static class NetherNativeMethodBindingSelector
{
    public static NetherNativeBindingSelection Select(
        NetherNativeMethodDescriptor expected,
        IEnumerable<NetherNativeMethodDescriptor> candidates
    )
    {
        if (expected is null)
            throw new ArgumentNullException(nameof(expected));
        if (candidates is null)
            throw new ArgumentNullException(nameof(candidates));

        List<NetherNativeMethodDescriptor> exact = new();
        foreach (NetherNativeMethodDescriptor candidate in candidates)
        {
            if (candidate is null || !Matches(expected, candidate))
                continue;
            exact.Add(candidate);
        }

        return exact.Count switch
        {
            1 => new NetherNativeBindingSelection(
                NetherNativeActionResultKind.Started,
                exact[0],
                "exact-signature"
            ),
            0 => new NetherNativeBindingSelection(
                NetherNativeActionResultKind.BindingUnavailable,
                null,
                "no-exact-signature"
            ),
            _ => new NetherNativeBindingSelection(
                NetherNativeActionResultKind.BindingUnavailable,
                null,
                "ambiguous-exact-signature"
            ),
        };
    }

    private static bool Matches(NetherNativeMethodDescriptor expected, NetherNativeMethodDescriptor candidate)
    {
        if (
            !string.Equals(expected.Name, candidate.Name, StringComparison.Ordinal)
            || expected.Arity != candidate.Arity
            || !string.Equals(expected.ReturnTypeName, candidate.ReturnTypeName, StringComparison.Ordinal)
            || (expected.IsStatic.HasValue && candidate.IsStatic != expected.IsStatic)
        )
            return false;

        for (int index = 0; index < expected.Arity; index++)
        {
            if (
                !string.Equals(
                    expected.ParameterTypeNames[index],
                    candidate.ParameterTypeNames[index],
                    StringComparison.Ordinal
                )
            )
                return false;
        }

        return true;
    }
}

internal enum NetherCombatLane
{
    Auto,
    Rush,
    Impact,
}

/// <summary>
/// User-selected strategy intent. The current client exposes authoritative run/code state but no
/// native field that chooses between research progression and equipment farming for the plugin.
/// </summary>
internal enum NetherStrategyMode
{
    Equipment = 0,
    Research = 1,
}

internal enum NetherTreasureMode
{
    Off,
    KeyOnly,
}

internal enum NetherShopMode
{
    Off,
    EquipmentBags,
}

internal enum NetherEffectKind
{
    Unknown = 0,
    Heal = 1,
    Damage = 2,
    Erosion = 3,
    ErosionHeal = 4,
    NetherGoldUsed = 5,
    TreasureKeyUsed = 6,
    /// <summary>Native target_type=7: open the code-conversion list; its parameter is not a code ID.</summary>
    AbyssCodeTransform = 7,
    Battle = 8,
    Item = 9,
    NetherGoldGain = 10,
    TreasureKeyGain = 11,
    /// <summary>Native content_type=160: open the server-provided Abyss-code offer flow.</summary>
    AbyssCodeOffer = 12,
}

internal enum NetherCodeEffectKind
{
    Unknown = 0,
    ErosionAdditionUp = 1,
    ErosionAdditionDown = 2,
    ErosionRateUp = 3,
    ErosionRateDown = 4,
}

/// <summary>
/// The four displayed Abyss-code families.  These come from MNetherCodes.category and are a
/// separate axis from MNetherCodes.effect_type; a Risk-family card can still carry an ordinary
/// ability or a direct erosion modifier.
/// </summary>
internal enum NetherCodeFamily
{
    Unknown = 0,
    Rush = 1,
    Impact = 2,
    Safe = 3,
    Risk = 4,
}

/// <summary>
/// Exact public Project.NetherCodeEffectType values observed in the current client.  Unknown raw
/// values remain preserved in the state object but are never assigned invented semantics.
/// </summary>
internal enum NetherCodeMasterEffectType
{
    Unknown = 0,
    NetherAbility = 1,
    CommonAbility = 2,
    ErosionAdditionUp = 6,
    ErosionAdditionDown = 7,
    ErosionRateUp = 8,
    ErosionRateDown = 9,
}

/// <summary>
/// Exact numeric values from Project.NetherCodeCategoryType in the packaged client.
/// </summary>
internal enum NetherCodeCategory
{
    Unknown = 0,
    // Native enum member: Technique.  NetherText maps it to ラッシュコード系統.
    Rush = 1,
    Technique = Rush,
    // Native enum member: Strength.  NetherText maps it to インパクトコード系統.
    Impact = 2,
    Strength = Impact,
    // Native enum member: ErosionResistance.
    Safe = 3,
    ErosionResistance = Safe,
    // Native enum member: ErosionEnhancement.
    Risk = 4,
    ErosionEnhancement = Risk,
}

/// <summary>Exact Project.NetherCodeCategoryGroupType values, kept local to the policy seam.</summary>
internal enum NetherCodeCategoryGroup
{
    Unknown = -1,
    Tactics = 0,
    Erosion = 1,
}

internal enum NetherRewardRarity
{
    NoEffect = 0,
    Silver = 1,
    Purple = 2,
    Gold = 3,
    Red = 4,
    UniqueWeapon = 5,
}

internal readonly record struct NetherSnapshotFingerprint(
    NetherSessionStatus status,
    long netherId,
    long mapId,
    int floorLevel,
    int floorIndex,
    int erosionPoint,
    string characterHpHash,
    string codeHash,
    string mapHash,
    long currentFloorId = 0,
    int ticketCount = 0,
    int treasureKeyCount = 0,
    int netherGold = 0,
    int codeReloadCount = 0,
    int lockReward = 0,
    long currentNodeId = 0
)
{
    public NetherSessionStatus Status => status;
    public long NetherId => netherId;
    public long MapId => mapId;
    public int FloorLevel => floorLevel;
    public int FloorIndex => floorIndex;
    public int ErosionPoint => erosionPoint;
    public string CharacterHpHash => characterHpHash ?? string.Empty;
    public string CodeHash => codeHash ?? string.Empty;
    public string MapHash => mapHash ?? string.Empty;
    public long CurrentFloorId => currentFloorId;
    public int TicketCount => ticketCount;
    public int TreasureKeyCount => treasureKeyCount;
    public int NetherGold => netherGold;
    public int CodeReloadCount => codeReloadCount;
    public int LockReward => lockReward;
    public long CurrentNodeId => currentNodeId;
}

internal sealed record NetherFloorNode(
    long FloorId,
    int FloorLevel,
    int FloorIndex,
    NetherFloorNodeType NodeType
)
{
    /// <summary>
    /// Stable server-coordinate identity for this rendered node.  <see cref="FloorId"/> is the
    /// reusable MNetherMapFloors/master ID and is not globally unique in a Nether map.
    /// Tests and non-runtime callers retain the historical default for compact fixtures.
    /// </summary>
    public long NodeId { get; init; } = FloorId;
    /// <summary>Server/API floor_index (native FloorPosition), distinct from the per-level UI index.</summary>
    public int ApiFloorIndex { get; init; } = FloorIndex;
    public bool IsHidden { get; init; }
    public bool IsUnlocked { get; init; }
    public IReadOnlyList<long> PreviousFloorIds { get; init; } = Array.Empty<long>();
    public int RewardTier { get; init; }
    public int OptionalCombatCount { get; init; }
}

internal readonly record struct NetherCharacterState(
    long CharacterId,
    int HpPermille,
    bool IsActive = true
);

internal sealed record NetherCodeState(long CodeId, NetherCodeFamily Family, int AbilityLevel)
{
    public bool IsKnown { get; init; } = true;
    /// <summary>
    /// False when category/master identity is authoritative but this plugin has not decoded the
    /// native effect shape.  Category policy may still count the card; effect-specific policy may
    /// not infer a trigger, target, erosion delta, or ability value from it.
    /// </summary>
    public bool EffectSemanticsKnown { get; init; } = true;
    public NetherCodeCategory Category { get; init; }
    public int Rarity { get; init; }
    /// <summary>Static MNetherCodes.power.  It is display/reference power, not proven party DPS.</summary>
    public int Power { get; init; }
    public NetherCodeMasterEffectType MasterEffectType { get; init; }
    public long EffectParameter1 { get; init; }
    public long EffectParameter2 { get; init; }
    public long EffectParameter3 { get; init; }
    /// <summary>p1 for effect types 1/2; zero for non-ability effects.</summary>
    public long AbilityAssetId { get; init; }
    /// <summary>Server possession amount. Native category counters deliberately ignore it.</summary>
    public int PossessionAmount { get; init; } = 1;
    /// <summary>
    /// A numeric zero is not evidence that no party member benefits.  Runtime mapping keeps
    /// this false until an exact ability/party authority proves the value.
    /// </summary>
    public bool PartyCoverageKnown { get; init; }
    public int PartyCoverage { get; init; }
}

internal sealed record NetherEffect(NetherEffectKind Kind, int Amount)
{
    public bool Known { get; init; } = true;
    public bool ContentKnown { get; init; } = true;
    public int RatePermille { get; init; } = 1000;
    public long ContentId { get; init; }
    public long ReplacementCodeId { get; init; }
    public bool IsOptionalBattle { get; init; }
    /// <summary>
    /// Exact native battle row for target_type=8.  A missing row is option-local unknown evidence;
    /// the integer Amount remains the raw server parameter for audit output.
    /// </summary>
    public NetherEventBattleEvidence? BattleEvidence { get; init; }
    /// <summary>Exact MItems/content identity when this effect is an Event reward.</summary>
    public NetherEventRewardEvidence? RewardEvidence { get; init; }
}

internal sealed record NetherRewardItem(long ItemId, int Amount)
{
    public bool HasMasterData { get; init; } = true;
    /// <summary>
    /// The acquisition datastore does not carry the server-return-popup rarity.  A false
    /// value is deliberately not ranked as <see cref="NetherRewardRarity.NoEffect"/>: the
    /// item must be remapped from the freshly created native return popup before use.
    /// </summary>
    public bool HasVerifiedDropRarity { get; init; } = true;
    public int ItemType { get; init; }
    public NetherRewardRarity DropRarity { get; init; }
    public int MasterRarity { get; init; }
}

internal sealed record NetherAutoClimbSettings
{
    public NetherStrategyMode StrategyMode { get; init; } = NetherStrategyMode.Equipment;
    public NetherCodeFamily ResearchPrimaryFamily { get; init; } = NetherCodeFamily.Unknown;
    public NetherCodeFamily ResearchSecondaryFamily { get; init; } = NetherCodeFamily.Unknown;
    public int MaxDepth { get; init; } = 130;
    public int SoftErosionLimit { get; init; } = 90;
    public int MinimumCharacterHpPermille { get; init; } = 300;
    public NetherCombatLane CombatLane { get; init; } = NetherCombatLane.Auto;
    public int CodeReloadReserve { get; init; } = 1;
    public NetherTreasureMode TreasureMode { get; init; } = NetherTreasureMode.KeyOnly;
    public NetherShopMode ShopMode { get; init; } = NetherShopMode.Off;
    /// <summary>
    /// Explicit Equipment-only opt-in for the server-random Recovery Code transform. Even when
    /// enabled, policy still requires exact zero-value Rest/Purification and a hard-excluded held
    /// Code. The default remains false.
    /// </summary>
    public bool EquipmentRecoveryCodeTransformEnabled { get; init; }
    public bool DetailedLogging { get; init; } = true;
}

/// <summary>
/// Optional identity prediction for a Sleep continuation, derived from the current packaged
/// map-floor master chain.  RequestNetherContinueAsync carries the current absolute floor plus
/// the exact one-ticket count (and optional return items). Continue installs the next map without
/// itself clearing its first node, so the segment-entry floor remains the completed checkpoint
/// floor. The response remains authoritative for map/floor identity when the local master has no
/// next-floor link.
/// </summary>
internal sealed record NetherContinuationTarget(
    long MapId,
    long FloorId,
    int SegmentFloorLevel
);

internal sealed record NetherSnapshot
{
    public NetherSessionStatus Status { get; init; }
    public long NetherId { get; init; }
    public long MapId { get; init; }
    public long CurrentFloorId { get; init; }
    /// <summary>Coordinate identity of the current rendered node; FloorId remains the master ID.</summary>
    public long CurrentNodeId { get; init; }
    public int FloorLevel { get; init; }
    public int FloorIndex { get; init; }
    public int MaxFloorLevel { get; init; }
    public int ContinuanceFloorLevel { get; init; }
    public int MasterMaxFloorLevel { get; init; }
    /// <summary>
    /// Live unlocked elevator/checkpoint authority from NetherPointData.RecoveryFloorLevel.
    /// It is never inferred from the reached-floor record.
    /// </summary>
    public int RecoveryFloorLevel { get; init; }
    /// <summary>
    /// Exact Boss floor levels derived from current MNetherMapFloors BattleBoss rows. Policy
    /// normalizes against this authority instead of assuming ten-floor arithmetic or floor 130.
    /// </summary>
    public IReadOnlyList<int> AuthoritativeBossFloorLevels { get; init; } = Array.Empty<int>();
    public int ErosionPoint { get; init; }
    public int TicketCount { get; init; }
    public int SignalCount { get; init; }
    public int TreasureKeyCount { get; init; }
    public int NetherGold { get; init; }
    public int CodeReloadCount { get; init; }
    public int CodeCapacity { get; init; }
    public int LockReward { get; init; }
    public NetherContinuationTarget? ContinuationTarget { get; init; }
    public IReadOnlyList<NetherCharacterState> Characters { get; init; } = Array.Empty<NetherCharacterState>();
    public IReadOnlyList<NetherCodeState> Codes { get; init; } = Array.Empty<NetherCodeState>();
    public IReadOnlyList<NetherFloorNode> Floors { get; init; } = Array.Empty<NetherFloorNode>();
    public IReadOnlyList<NetherRewardItem> AcquiredItems { get; init; } = Array.Empty<NetherRewardItem>();
    public string CharacterHpHash { get; init; } = string.Empty;
    public string CodeHash { get; init; } = string.Empty;
    public string MapHash { get; init; } = string.Empty;

    public NetherSnapshotFingerprint Fingerprint => new(
        Status,
        NetherId,
        MapId,
        FloorLevel,
        FloorIndex,
        ErosionPoint,
        CharacterHpHash,
        CodeHash,
        MapHash,
        CurrentFloorId,
        TicketCount,
        TreasureKeyCount,
        NetherGold,
        CodeReloadCount,
        LockReward,
        CurrentNodeId
    );
}

internal sealed record NetherBattleSettlementContract(
    long EntryMapId,
    long EntryFloorId,
    NetherSessionStatus EntryStatus,
    long ExpectedMapId,
    long ExpectedFloorId,
    NetherSessionStatus ExpectedStatus,
    string ProjectionIdentity
)
{
    public NetherBattleProjectionPayload? EntryProjection { get; init; }
}

/// <summary>
/// Immutable combat safety evidence captured immediately before the native floor click.  The
/// battle-settlement action keeps this payload rather than recomputing against a changed code
/// portfolio or erosion value after the server has accepted the node.
/// </summary>
internal sealed record NetherBattleProjectionPayload(
    long MapId,
    long FloorId,
    int PreBattleErosion,
    int FloorMinimumErosion,
    int FloorMaximumErosion,
    int ProjectedMinimumErosion,
    int ProjectedMaximumErosion,
    string CodeHash,
    string ProjectionIdentity
)
{
    /// <summary>
    /// Exact server status expected after this combat is settled. Ordinary battles and
    /// minibosses return to Play; a segment-ending Boss enters the Sleep checkpoint flow.
    /// </summary>
    public NetherSessionStatus ExpectedSettlementStatus { get; init; } = NetherSessionStatus.Play;
}

/// <summary>
/// One immutable, owned modal stage in a SelectFloor native parent chain.  A floor parent can
/// legitimately create more than one popup (for example Event -> Change Code -> Code Select),
/// so the final read-only reconcile must retain every stage rather than replacing the first
/// popup contract with the most recent one.
/// </summary>
internal sealed record NetherFloorPopupStage(
    NetherRuntimePopupKind PopupKind,
    NetherActionKind ActionKind,
    long OwnerGeneration,
    long Sequence,
    NetherSessionStatus ExpectedAfterStatus,
    int OptionNumber,
    IReadOnlyList<NetherEffect> ExpectedEffects,
    long ContentId,
    int ContentAmount,
    int GoldCost,
    long CodeId,
    long ReplaceCodeId,
    long DecisionEpoch = 0,
    long TargetCharacterId = 0
)
{
    /// <summary>True when erosion includes active code/category modifiers, not only raw effects.</summary>
    public bool HasExpectedErosionDelta { get; init; }
    public int ExpectedErosionDelta { get; init; }
    public int ProjectedErosion { get; init; }
    public int ProjectedHpDelta { get; init; }
    public int ProjectedNetherGold { get; init; }
    public int ProjectedTreasureKeys { get; init; }
    public int CommittedGoldMinimum { get; init; }
    public int CommittedKeyMinimum { get; init; }
    public long EventId { get; init; }
    public long EventPartId { get; init; }
    public NetherEventCommitment? EventCommitment { get; init; }
    public NetherShopProcurementCommitment? ShopProcurementCommitment { get; init; }
}

internal readonly record struct NetherPlannedAction(NetherActionKind Kind)
{
    public long FloorId { get; init; }
    public int FloorLevel { get; init; }
    public int FloorIndex { get; init; }
    /// <summary>Exact server-owned status required before the selected floor action.</summary>
    public NetherSessionStatus ExpectedBeforeStatus { get; init; } = NetherSessionStatus.Unknown;
    /// <summary>Exact server-owned status required after the selected floor action.</summary>
    public NetherSessionStatus ExpectedAfterStatus { get; init; } = NetherSessionStatus.Unknown;
    public int OptionNumber { get; init; }
    /// <summary>
    /// Native Event popup presentation character. The update API has no character-id input, so
    /// this is correlation/telemetry evidence and never the server-owned HP effect scope.
    /// </summary>
    public long TargetCharacterId { get; init; }
    /// <summary>Only fully mapped effects may be used to prove an event postcondition.</summary>
    public IReadOnlyList<NetherEffect> ExpectedEffects { get; init; } = Array.Empty<NetherEffect>();
    /// <summary>Exact projected erosion delta after active code/category modifiers.</summary>
    public bool HasExpectedErosionDelta { get; init; }
    public int ExpectedErosionDelta { get; init; }
    /// <summary>Exact projected Event state retained for downstream commitment validation.</summary>
    public int ProjectedErosion { get; init; }
    public int ProjectedHpDelta { get; init; }
    public int ProjectedNetherGold { get; init; }
    public int ProjectedTreasureKeys { get; init; }
    public int CommittedGoldMinimum { get; init; }
    public int CommittedKeyMinimum { get; init; }
    public long ContentId { get; init; }
    public int ContentAmount { get; init; }
    public int GoldCost { get; init; }
    public long CodeId { get; init; }
    public long ReplaceCodeId { get; init; }
    public int TicketCount { get; init; }
    public int TicketCost { get; init; }
    public long ExpectedMapId { get; init; }
    /// <summary>Exact post-Continue floor ID, not the source floor-selection ID.</summary>
    public long ExpectedFloorId { get; init; }
    /// <summary>
    /// Exact completed floor at post-Continue segment entry; never raw continuance_floor_level.
    /// It remains equal to the checkpoint until the first node in the new map is selected.
    /// </summary>
    public int ExpectedSegmentFloorLevel { get; init; }
    /// <summary>
    /// Once a SelectFloor parent has opened an owned popup, these two fields retain the
    /// immutable child contract used for the single parent reconciliation.  They prevent a
    /// visual popup close from being treated as an untyped successful floor selection.
    /// </summary>
    public NetherRuntimePopupKind OwnedPopupKind { get; init; }
    public NetherActionKind OwnedPopupActionKind { get; init; }
    /// <summary>
    /// Ordered immutable proof for every owned modal dispatched by this one SelectFloor
    /// parent.  Legacy scalar fields above mirror the final stage for compact audit output;
    /// reconciliation uses this collection whenever it is populated.
    /// </summary>
    public IReadOnlyList<NetherFloorPopupStage> OwnedPopupStages { get; init; }
        = Array.Empty<NetherFloorPopupStage>();
    /// <summary>Exact Event identity carried through the owned parent transaction.</summary>
    public long EventId { get; init; }
    public long EventPartId { get; init; }
    public long EventFloorId { get; init; }
    public long EventNodeId { get; init; }
    public NetherEventCommitment? EventCommitment { get; init; }
    public NetherShopProcurementCommitment? ShopProcurementCommitment { get; init; }
    public NetherBattleSettlementContract? BattleSettlement { get; init; }
    /// <summary>Set only for a safety-approved combat floor before its native selection parent begins.</summary>
    public NetherBattleProjectionPayload? BattleProjection { get; init; }
    /// <summary>
    /// A checkpoint continuation carries only its lock count and explicit user preserve IDs.
    /// The live datastore preflight is captured before starting the native Continue parent and
    /// the fresh native return popup must match this contract before it can be confirmed.
    /// </summary>
    public int ReturnLockReward { get; init; }
    public IReadOnlyList<long> ReturnPreserveItemIds { get; init; } = Array.Empty<long>();
    public int ReturnPreflightSelectionLimit { get; init; }
    public string ReturnExpectedPristineHash { get; init; } = string.Empty;
    public IReadOnlyList<NetherCheckpointReturnPreflightItem> ReturnPreflightWholeEntrySelection { get; init; }
        = Array.Empty<NetherCheckpointReturnPreflightItem>();
}
