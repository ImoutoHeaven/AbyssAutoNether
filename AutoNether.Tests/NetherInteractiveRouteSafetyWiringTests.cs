using System.Collections.Generic;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

/// <summary>
/// Characterization of the exact production route seam used by
/// NetherAutoClimbController.PlanRoute: raw runtime/master capture -> pre-entry proof ->
/// RouteSafetyProductionCoordinator -> SelectFloor decision.  It deliberately does not make
/// a helper-only safety assertion, so removing the coordinator consumption of a rejected proof
/// turns these into unsafe route selections.
/// </summary>
public class NetherInteractiveRouteSafetyWiringTests
{
    [Fact]
    public void ControllerRouteWiring_SelectsEventWhenTheNativeResolvedRowHasASafeExit()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Event,
            events:
            [
                Event(42, 2, 1, 1001),
                Event(43, 2, 1, 1002),
            ],
            parts:
            [
                Part(1001, (int)NetherEffectKind.Heal, 1),
                Part(1002, (int)NetherEffectKind.NetherGoldUsed, 0),
            ]
        );

        NetherAutoClimbRouteSafetyDecision decision = Decide(NetherFloorNodeType.Event, capture);

        Assert.True(capture.Safety.IsSafe);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(decision.Route.SelectedNode).FloorId);
        Assert.True(decision.Context.KnownNodeByFloorId[2]);
        Assert.NotNull(decision.SelectFloorAction);
    }

    [Fact]
    public void ControllerRouteWiring_RejectsEventWhenTheNativeResolvedRowIsUnsafe()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Event,
            events:
            [
                Event(42, 2, 1, 1002),
                Event(43, 2, 1, 1001),
            ],
            parts:
            [
                Part(1001, (int)NetherEffectKind.Heal, 1),
                Part(1002, (int)NetherEffectKind.Damage, 500),
            ]
        );

        NetherAutoClimbRouteSafetyDecision decision = Decide(NetherFloorNodeType.Event, capture);

        Assert.False(capture.Safety.IsSafe);
        Assert.False(decision.Route.HasSelection);
        Assert.False(decision.Context.KnownNodeByFloorId[2]);
        Assert.Null(decision.SelectFloorAction);
    }

    [Fact]
    public void ControllerRouteWiring_AllowsNeutralRecoveryThroughThePreEntryProof()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Recovery,
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, (int)NetherEffectKind.NetherGoldUsed, 0)]
        );

        NetherAutoClimbRouteSafetyDecision decision = Decide(NetherFloorNodeType.Recovery, capture);

        Assert.True(capture.Safety.IsSafe);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(decision.Route.SelectedNode).FloorId);
        Assert.True(decision.Context.KnownNodeByFloorId[2]);
    }

    [Fact]
    public void ControllerRouteWiring_RequiresExactShopCloseBindingForShopOff()
    {
        NetherRuntimeInteractivePreEntryCaptureResult unavailable = Capture(
            NetherFloorNodeType.Shop,
            events: [],
            parts: [],
            canCloseShop: false
        );
        NetherRuntimeInteractivePreEntryCaptureResult available = Capture(
            NetherFloorNodeType.Shop,
            events: [],
            parts: [],
            canCloseShop: true
        );

        Assert.False(Decide(NetherFloorNodeType.Shop, unavailable).Route.HasSelection);
        Assert.True(Decide(NetherFloorNodeType.Shop, available).Route.HasSelection);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void ControllerRouteWiring_UsesVerifiedKeyTreasureOptionWhenAvailable(int keys, bool expectedSelection)
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Treasure,
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, (int)NetherEffectKind.TreasureKeyUsed, 1)],
            keys: keys
        );

        NetherAutoClimbRouteSafetyDecision decision = Decide(
            NetherFloorNodeType.Treasure,
            capture,
            keys: keys
        );

        Assert.Equal(expectedSelection, decision.Route.HasSelection);
        Assert.Equal(expectedSelection, decision.Context.KnownNodeByFloorId[2]);
    }

    [Fact]
    public void ControllerRouteWiring_KeepsShopToTreasureRouteWhenNativeHpPaymentIsSafe()
    {
        NetherRuntimeInteractivePreEntryCaptureResult shop = Capture(
            NetherFloorNodeType.Shop,
            events: [],
            parts: [],
            keys: 0,
            canCloseShop: true,
            hp: 704,
            floorMasterId: 2
        );
        NetherRuntimeInteractivePreEntryCaptureResult treasure = Capture(
            NetherFloorNodeType.Treasure,
            events: [Event(42, 3, 1, 1001, 1002)],
            parts:
            [
                Part(1001, (int)NetherEffectKind.TreasureKeyUsed, 1),
                Part(
                    1002,
                    (int)NetherEffectKind.Damage,
                    300,
                    targetType2: (int)NetherEffectKind.ErosionHeal,
                    parameter2: 30
                ),
            ],
            keys: 0,
            hp: 704,
            floorMasterId: 3
        );

        NetherAutoClimbRouteSafetyDecision decision = DecideShopThenTreasure(shop, treasure);

        Assert.True(shop.Safety.IsSafe, shop.Safety.PauseReason + ":" + shop.Safety.Detail);
        Assert.True(treasure.Safety.IsSafe, treasure.Safety.PauseReason + ":" + treasure.Safety.Detail);
        Assert.Equal(2, treasure.Safety.SafeOptionNumberByEventId[42]);
        Assert.Equal(-300, treasure.Safety.WorstCaseProjection!.Value.HpDelta);
        Assert.True(decision.Route.HasSelection, decision.Route.PauseReason + ":" + decision.Route.PauseDetail);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(decision.Route.SelectedNode).FloorId);
        Assert.True(decision.Context.KnownNodeByFloorId[2]);
        Assert.True(decision.Context.KnownNodeByFloorId[3]);
    }

    [Fact]
    public void ControllerRouteWiring_RejectsMissingInteractiveMasterInsteadOfFallingBackToPermissiveMaps()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Event,
            mapRows: Array.Empty<object>(),
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, (int)NetherEffectKind.Heal, 1)]
        );

        NetherAutoClimbRouteSafetyDecision decision = Decide(NetherFloorNodeType.Event, capture);

        Assert.True(capture.IsCaptured);
        Assert.False(capture.Safety.IsSafe);
        Assert.False(decision.Route.HasSelection);
        Assert.False(decision.Context.KnownNodeByFloorId[2]);
    }

    [Fact]
    public void ControllerRouteWiring_CannotBypassThePreEntryEvaluatorWithOtherwisePlausibleRawBounds()
    {
        NetherRuntimeInteractivePreEntryCaptureResult safeCapture = Capture(
            NetherFloorNodeType.Event,
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, (int)NetherEffectKind.Heal, 1)]
        );
        NetherRuntimeInteractivePreEntryCaptureResult rejectedCapture = safeCapture with
        {
            Safety = NetherInteractiveFloorPreEntrySafetyResult.Pause(
                NetherPauseReason.NoSafeRoute,
                "mutation-proof-rejected-preentry"
            ),
        };

        NetherAutoClimbRouteSafetyDecision decision = Decide(NetherFloorNodeType.Event, rejectedCapture);

        Assert.True(safeCapture.Safety.IsSafe);
        Assert.False(decision.Route.HasSelection);
        Assert.False(decision.Context.KnownNodeByFloorId[2]);
        Assert.Contains(
            "interactive:safety:NoSafeRoute:mutation-proof-rejected-preentry",
            decision.Context.UnknownDetail(2)
        );
    }

    [Fact]
    public void ControllerRouteWiring_BudgetsEventProjectionWithTheFixedBattleBaseCost()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Event,
            mapRows: [new MapFloorFixture { id = 2, min_erosion_point = 0, max_erosion_point = 0 }],
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, (int)NetherEffectKind.Erosion, 60)]
        );

        NetherAutoClimbRouteSafetyDecision decision = DecideWorstEventBudget(capture);

        Assert.True(capture.Safety.IsSafe);
        Assert.True(decision.Route.HasSelection);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(decision.Route.SelectedNode).FloorId);
    }

    [Fact]
    public void ControllerRouteWiring_SelectsProvedRecoveryBeforeTheHpIneligibleNecessaryBoss()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Recovery,
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, (int)NetherEffectKind.Heal, 1)],
            hp: 299
        );

        NetherAutoClimbRouteSafetyDecision decision = DecideRecoveryBeforeBoss(capture);

        Assert.True(capture.Safety.IsSafe);
        Assert.Equal(1, capture.Safety.WorstCaseProjection!.Value.HpDelta);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(decision.Route.SelectedNode).FloorId);
        Assert.True(decision.Context.HpSafeByFloorId[2]);
        Assert.False(decision.Context.HpSafeByFloorId[3]);
    }

    private static NetherAutoClimbRouteSafetyDecision Decide(
        NetherFloorNodeType interactiveKind,
        NetherRuntimeInteractivePreEntryCaptureResult capture,
        int keys = 1,
        int hp = 500
    ) => new NetherAutoClimbRouteSafetyWiring().Plan(
        new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            MapId = 1,
            CurrentFloorId = 1,
            ErosionPoint = 20,
            NetherGold = 100,
            TreasureKeyCount = keys,
            Characters = [new NetherCharacterState(1, hp) { IsActive = true }],
            Floors =
            [
                Floor(1, 1, NetherFloorNodeType.Recovery),
                Floor(2, 2, interactiveKind, previous: [1]),
                Floor(3, 3, NetherFloorNodeType.Boss, previous: [2]),
            ],
        },
        Settings(),
        effectiveMaximumDepth: 130,
        runtime: new NetherRuntimeRouteSafetyData
        {
            FloorBoundsByFloorId = new Dictionary<long, NetherFloorMasterBounds>
            {
                [3] = new NetherFloorMasterBounds(3, 0, 0, IsKnown: true, Detail: string.Empty),
            },
            ActivePartyHp = new NetherActivePartyHpSafety(true, hp, string.Empty),
            ActiveCodeErosion = new NetherActiveCodeErosionProjection
            {
                ErosionProjectionKnown = true,
                CodeHash = "nether-codes:none",
                ErosionEffects = Array.Empty<NetherCodeEffect>(),
            },
        },
        interactivePreEntry: NetherRuntimeInteractivePreEntryInputsResult.Success(
            new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult> { [2] = capture }
        )
    );

    private static NetherAutoClimbRouteSafetyDecision DecideShopThenTreasure(
        NetherRuntimeInteractivePreEntryCaptureResult shop,
        NetherRuntimeInteractivePreEntryCaptureResult treasure
    ) => new NetherAutoClimbRouteSafetyWiring().Plan(
        new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            MapId = 1,
            CurrentFloorId = 1,
            ErosionPoint = 20,
            NetherGold = 100,
            TreasureKeyCount = 0,
            Characters = [new NetherCharacterState(1, 704) { IsActive = true }],
            Floors =
            [
                Floor(1, 114, NetherFloorNodeType.MiniBoss),
                Floor(2, 115, NetherFloorNodeType.Shop, previous: [1]),
                Floor(3, 116, NetherFloorNodeType.Treasure, previous: [2]),
                Floor(4, 117, NetherFloorNodeType.Boss, previous: [3]),
            ],
        },
        Settings(),
        effectiveMaximumDepth: 130,
        runtime: new NetherRuntimeRouteSafetyData
        {
            FloorBoundsByFloorId = new Dictionary<long, NetherFloorMasterBounds>
            {
                [4] = new NetherFloorMasterBounds(4, 0, 0, IsKnown: true, Detail: string.Empty),
            },
            ActivePartyHp = new NetherActivePartyHpSafety(true, 704, string.Empty),
            ActiveCodeErosion = new NetherActiveCodeErosionProjection
            {
                ErosionProjectionKnown = true,
                CodeHash = "nether-codes:none",
                ErosionEffects = Array.Empty<NetherCodeEffect>(),
            },
        },
        interactivePreEntry: NetherRuntimeInteractivePreEntryInputsResult.Success(
            new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
            {
                [2] = shop,
                [3] = treasure,
            }
        )
    );

    private static NetherAutoClimbRouteSafetyDecision DecideWorstEventBudget(
        NetherRuntimeInteractivePreEntryCaptureResult capture
    ) => new NetherAutoClimbRouteSafetyWiring().Plan(
        new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            MapId = 1,
            CurrentFloorId = 1,
            ErosionPoint = 20,
            NetherGold = 100,
            TreasureKeyCount = 1,
            Characters = [new NetherCharacterState(1, 500) { IsActive = true }],
            Floors =
            [
                Floor(1, 1, NetherFloorNodeType.Recovery),
                Floor(2, 2, NetherFloorNodeType.Event, previous: [1]),
                Floor(3, 3, NetherFloorNodeType.Boss, previous: [2]),
            ],
        },
        Settings(),
        effectiveMaximumDepth: 130,
        runtime: new NetherRuntimeRouteSafetyData
        {
            FloorBoundsByFloorId = new Dictionary<long, NetherFloorMasterBounds>
            {
                [3] = new NetherFloorMasterBounds(3, 0, 100, IsKnown: true, Detail: string.Empty),
            },
            ActivePartyHp = new NetherActivePartyHpSafety(true, 500, string.Empty),
            ActiveCodeErosion = new NetherActiveCodeErosionProjection
            {
                ErosionProjectionKnown = true,
                CodeHash = "nether-codes:none",
                ErosionEffects = Array.Empty<NetherCodeEffect>(),
            },
        },
        interactivePreEntry: NetherRuntimeInteractivePreEntryInputsResult.Success(
            new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult> { [2] = capture }
        )
    );

    private static NetherAutoClimbRouteSafetyDecision DecideRecoveryBeforeBoss(
        NetherRuntimeInteractivePreEntryCaptureResult capture
    ) => new NetherAutoClimbRouteSafetyWiring().Plan(
        new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            MapId = 1,
            CurrentFloorId = 1,
            ErosionPoint = 20,
            NetherGold = 100,
            TreasureKeyCount = 1,
            Characters = [new NetherCharacterState(1, 299) { IsActive = true }],
            Floors =
            [
                Floor(1, 1, NetherFloorNodeType.Recovery),
                Floor(2, 2, NetherFloorNodeType.Recovery, previous: [1]),
                Floor(3, 3, NetherFloorNodeType.Boss, previous: [2]),
            ],
        },
        Settings(),
        effectiveMaximumDepth: 130,
        runtime: new NetherRuntimeRouteSafetyData
        {
            FloorBoundsByFloorId = new Dictionary<long, NetherFloorMasterBounds>
            {
                [3] = new NetherFloorMasterBounds(3, 0, 0, IsKnown: true, Detail: string.Empty),
            },
            ActivePartyHp = new NetherActivePartyHpSafety(true, 299, string.Empty),
            ActiveCodeErosion = new NetherActiveCodeErosionProjection
            {
                ErosionProjectionKnown = true,
                CodeHash = "nether-codes:none",
                ErosionEffects = Array.Empty<NetherCodeEffect>(),
            },
        },
        interactivePreEntry: NetherRuntimeInteractivePreEntryInputsResult.Success(
            new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult> { [2] = capture }
        )
    );

    private static NetherRuntimeInteractivePreEntryCaptureResult Capture(
        NetherFloorNodeType kind,
        object[]? mapRows = null,
        object[]? events = null,
        object[]? parts = null,
        int keys = 1,
        bool canCloseShop = false,
        int hp = 500,
        long floorMasterId = 2
    ) => new NetherRuntimeInteractivePreEntryInputCapture().Capture(new NetherRuntimeInteractivePreEntryCaptureRequest(
        FloorModel: new FloorFixture
        {
            MNetherMapFloorId = floorMasterId,
            ExtendId = kind is NetherFloorNodeType.Event or NetherFloorNodeType.Recovery ? 42 : 0,
            FloorType = (int)kind,
        },
        MapFloorRows: mapRows ?? [new MapFloorFixture { id = floorMasterId, min_erosion_point = 0, max_erosion_point = 10 }],
        EventRows: events ?? [Event(42, floorMasterId, 1, 1001)],
        EventPartRows: parts ?? [Part(1001, (int)NetherEffectKind.Heal, 1)],
        CurrentErosion: 20,
        ActiveHpPermille: [hp],
        CurrentNetherGold: 100,
        CurrentTreasureKeys: keys,
        Settings: Settings(),
        CanCloseShop: canCloseShop
    ));

    private static EventFixture Event(long id, long floorMasterId, int weight, params long[] partIds) => new()
    {
        id = id,
        m_nether_map_floor_id = floorMasterId,
        weight = weight,
        type = 4,
        m_nether_floor_event_part_id_1 = partIds.ElementAtOrDefault(0),
        m_nether_floor_event_part_id_2 = partIds.ElementAtOrDefault(1),
        m_nether_floor_event_part_id_3 = partIds.ElementAtOrDefault(2),
        m_nether_floor_event_part_id_4 = partIds.ElementAtOrDefault(3),
    };

    private static PartFixture Part(
        long id,
        int targetType,
        long parameter,
        int targetType2 = 0,
        long parameter2 = 0
    ) => new()
    {
        id = id,
        target_type_1 = targetType,
        select_parameter_1 = parameter,
        target_type_2 = targetType2,
        select_parameter_2 = parameter2,
    };

    private static NetherFloorNode Floor(long id, int level, NetherFloorNodeType type, long[]? previous = null) => new(id, level, (int)id, type)
    {
        IsUnlocked = true,
        PreviousFloorIds = previous ?? [],
    };

    private static NetherAutoClimbSettings Settings() => new()
    {
        MaxDepth = 130,
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = 300,
        ShopMode = NetherShopMode.Off,
        TreasureMode = NetherTreasureMode.KeyOnly,
    };

    private sealed class FloorFixture
    {
        public long MNetherMapFloorId { get; init; }
        public long ExtendId { get; init; }
        public int FloorType { get; init; }
    }

    private sealed class MapFloorFixture
    {
        public long id;
        public int min_erosion_point;
        public int max_erosion_point;
    }

    private sealed class EventFixture
    {
        public long id;
        public long m_nether_map_floor_id;
        public int weight;
        public int type;
        public long m_nether_floor_event_part_id_1;
        public long m_nether_floor_event_part_id_2;
        public long m_nether_floor_event_part_id_3;
        public long m_nether_floor_event_part_id_4;
    }

    private sealed class PartFixture
    {
        public long id;
        public int target_type_1;
        public long select_parameter_1;
        public int target_type_2;
        public long select_parameter_2;
        public int target_type_3;
        public long select_parameter_3;
        public int content_type;
        public long content_id;
        public long amount;
    }
}
