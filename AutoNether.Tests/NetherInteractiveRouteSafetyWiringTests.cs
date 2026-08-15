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

    [Fact]
    public void ControllerRouteWiring_PrefersKeyWhenItIsAvailable()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Treasure,
            events: [Event(42, 2, 1, 1001, 1002)],
            parts:
            [
                Part(1001, (int)NetherEffectKind.TreasureKeyUsed, 1),
                Part(1002, (int)NetherEffectKind.Damage, 200),
            ],
            keys: 1
        );

        NetherAutoClimbRouteSafetyDecision decision = Decide(
            NetherFloorNodeType.Treasure,
            capture,
            keys: 1
        );

        Assert.True(capture.Safety.IsSafe, capture.Safety.PauseReason + ":" + capture.Safety.Detail);
        Assert.Equal(1, capture.Safety.SafeOptionNumberByEventId[42]);
        Assert.True(decision.Route.HasSelection);
        Assert.True(decision.Context.KnownNodeByFloorId[2]);
    }

    [Fact]
    public void Production_preentry_rejects_no_key_treasure_hp_shape_without_route_objective_proof()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Treasure,
            events: [Event(42, 2, 1, 1001, 1002)],
            parts:
            [
                Part(1001, (int)NetherEffectKind.TreasureKeyUsed, 1),
                Part(1002, (int)NetherEffectKind.Damage, 200),
            ],
            keys: 0,
            activeHp: [100, 500]
        );

        NetherAutoClimbRouteSafetyDecision decision = DecideExactHpCost(
            NetherFloorNodeType.Treasure,
            capture,
            [100, 500]
        );

        Assert.False(capture.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.NoSafeRoute, capture.Safety.PauseReason);
        Assert.False(decision.Route.HasSelection);
    }

    [Fact]
    public void Production_preentry_rejects_hp_paid_key_shape_without_route_objective_proof()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Event,
            events: [Event(42, 2, 1, 1001)],
            parts:
            [
                Part(
                    1001,
                    (int)NetherEffectKind.Damage,
                    200,
                    contentType: 166,
                    amount: 1
                ),
            ],
            keys: 0,
            activeHp: [100, 500]
        );

        NetherAutoClimbRouteSafetyDecision decision = DecideExactHpCost(
            NetherFloorNodeType.Event,
            capture,
            [100, 500]
        );

        Assert.False(capture.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.UnsafeHp, capture.Safety.PauseReason);
        Assert.False(decision.Route.HasSelection);
    }

    [Theory]
    [InlineData((int)NetherFloorNodeType.Treasure, false)]
    [InlineData((int)NetherFloorNodeType.Event, true)]
    public void Production_preentry_allows_exact_group_survival_when_one_character_survives(
        int rawFloorKind,
        bool grantsTreasureKey
    )
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...1fb:
        // NetherTreasurePopupController.InitializeView(mapFloorId, extendId) resolves
        // MNetherFloorEvents and MNetherFloorEventParts. NetherFloorEventType.Damage is 2,
        // ContentType.NetherKey is 166, and NetherTreasurePanelType distinguishes Key/Hp/Abyss.
        // Only Treasure+Damage or Event+Damage+NetherKey may relax all-character survival.
        NetherFloorNodeType floorKind = (NetherFloorNodeType)rawFloorKind;
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            floorKind,
            events: [Event(42, 2, 1, 1001)],
            parts:
            [
                Part(
                    1001,
                    (int)NetherEffectKind.Damage,
                    200,
                    contentType: grantsTreasureKey ? 166 : 0,
                    amount: grantsTreasureKey ? 1 : 0
                ),
            ],
            keys: 0,
            activeHp: [100, 500],
            partialDeathEligibility: [Eligibility(floorKind, 42, 1001)]
        );

        NetherAutoClimbRouteSafetyDecision decision = DecideExactHpCost(
            floorKind,
            capture,
            [100, 500]
        );

        Assert.True(capture.Safety.IsSafe, capture.Safety.PauseReason + ":" + capture.Safety.Detail);
        Assert.True(decision.Route.HasSelection, decision.Route.PauseReason + ":" + decision.Route.PauseDetail);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(decision.Route.SelectedNode).NodeId);
        Assert.Equal(0, decision.Context.MinimumActiveCharacterHpPermille(2));
    }

    [Theory]
    [InlineData((int)NetherFloorNodeType.Treasure, false)]
    [InlineData((int)NetherFloorNodeType.Event, true)]
    public void Production_preentry_rejects_exact_group_survival_when_every_character_dies(
        int rawFloorKind,
        bool grantsTreasureKey
    )
    {
        NetherFloorNodeType floorKind = (NetherFloorNodeType)rawFloorKind;
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            floorKind,
            events: [Event(42, 2, 1, 1001)],
            parts:
            [
                Part(
                    1001,
                    (int)NetherEffectKind.Damage,
                    200,
                    contentType: grantsTreasureKey ? 166 : 0,
                    amount: grantsTreasureKey ? 1 : 0
                ),
            ],
            keys: 0,
            activeHp: [100, 150],
            partialDeathEligibility: [Eligibility(floorKind, 42, 1001)]
        );

        NetherAutoClimbRouteSafetyDecision decision = DecideExactHpCost(
            floorKind,
            capture,
            [100, 150]
        );

        Assert.False(capture.Safety.IsSafe);
        Assert.False(decision.Route.HasSelection);
    }

    [Fact]
    public void Production_preentry_keeps_ordinary_or_unknown_damage_ineligible()
    {
        NetherRuntimeInteractivePreEntryCaptureResult ordinary = Capture(
            NetherFloorNodeType.Event,
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, (int)NetherEffectKind.Damage, 200)],
            keys: 0,
            activeHp: [100, 500]
        );
        NetherRuntimeInteractivePreEntryCaptureResult unknown = Capture(
            NetherFloorNodeType.Event,
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, 99, 200)],
            keys: 0,
            activeHp: [100, 500]
        );

        Assert.False(ordinary.Safety.IsSafe);
        Assert.False(DecideExactHpCost(NetherFloorNodeType.Event, ordinary, [100, 500]).Route.HasSelection);
        Assert.False(unknown.Safety.IsSafe);
        Assert.False(DecideExactHpCost(NetherFloorNodeType.Event, unknown, [100, 500]).Route.HasSelection);
    }

    [Fact]
    public void Production_preentry_accepts_ordinary_event_below_soft_hp_when_every_character_survives()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Event,
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, (int)NetherEffectKind.Damage, 201)],
            keys: 0,
            activeHp: [500]
        );

        Assert.True(capture.Safety.IsSafe, capture.Safety.PauseReason + ":" + capture.Safety.Detail);
        Assert.Equal(-201, capture.Safety.WorstCaseProjection!.Value.HpDelta);
        Assert.Equal(1, capture.Safety.SafeOptionNumberByEventId[42]);
    }

    [Fact]
    public void Production_preentry_rejects_ordinary_event_when_a_character_reaches_zero()
    {
        NetherRuntimeInteractivePreEntryCaptureResult capture = Capture(
            NetherFloorNodeType.Event,
            events: [Event(42, 2, 1, 1001)],
            parts: [Part(1001, (int)NetherEffectKind.Damage, 500)],
            keys: 0,
            activeHp: [500]
        );

        NetherAutoClimbRouteSafetyDecision decision = DecideExactHpCost(
            NetherFloorNodeType.Event,
            capture,
            [500]
        );

        Assert.False(capture.Safety.IsSafe);
        Assert.Equal(NetherPauseReason.UnsafeHp, capture.Safety.PauseReason);
        Assert.False(decision.Route.HasSelection);
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
            parts: [Part(1001, (int)NetherEffectKind.Erosion, 40)]
        );

        NetherAutoClimbRouteSafetyDecision decision = DecideWorstEventBudget(capture);

        Assert.True(capture.Safety.IsSafe);
        Assert.True(decision.Route.HasSelection);
        Assert.Equal(2, Assert.IsType<NetherFloorNode>(decision.Route.SelectedNode).FloorId);
        Assert.Equal(65, decision.Context.PeakErosion(2));
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
        int keys = 1
    ) => new NetherAutoClimbRouteSafetyWiring().Plan(
        new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            MapId = 1,
            CurrentFloorId = 1,
            ErosionPoint = 20,
            NetherGold = 100,
            TreasureKeyCount = keys,
            Characters = [new NetherCharacterState(1, 500) { IsActive = true }],
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
            ActivePartyHp = NetherRouteSafetyHpTestEvidence.Single(1, 500),
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
            ActivePartyHp = NetherRouteSafetyHpTestEvidence.Single(1, 500),
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

    private static NetherAutoClimbRouteSafetyDecision DecideExactHpCost(
        NetherFloorNodeType interactiveKind,
        NetherRuntimeInteractivePreEntryCaptureResult capture,
        IReadOnlyList<int> activeHp
    ) => new NetherAutoClimbRouteSafetyWiring().Plan(
        new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            MapId = 1,
            CurrentFloorId = 1,
            ErosionPoint = 20,
            NetherGold = 100,
            TreasureKeyCount = 0,
            Characters = activeHp
                .Select((hp, index) => new NetherCharacterState(index + 1L, hp) { IsActive = true })
                .ToArray(),
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
            ActivePartyHp = NetherRouteSafetyHpTestEvidence.FromStates(
                activeHp.Select((hp, index) => new NetherCharacterState(index + 1L, hp))
            ),
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
            ActivePartyHp = NetherRouteSafetyHpTestEvidence.Single(1, 299),
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
        IReadOnlyList<int>? activeHp = null,
        IReadOnlyList<NetherInteractivePartialDeathEligibility>? partialDeathEligibility = null
    ) => new NetherRuntimeInteractivePreEntryInputCapture().Capture(new NetherRuntimeInteractivePreEntryCaptureRequest(
        FloorModel: new FloorFixture
        {
            MNetherMapFloorId = 2,
            ExtendId = kind is NetherFloorNodeType.Event or NetherFloorNodeType.Recovery ? 42 : 0,
            FloorType = (int)kind,
        },
        MapFloorRows: mapRows ?? [new MapFloorFixture { id = 2, min_erosion_point = 0, max_erosion_point = 10 }],
        EventRows: events ?? [Event(42, 2, 1, 1001)],
        EventPartRows: parts ?? [Part(1001, (int)NetherEffectKind.Heal, 1)],
        CurrentErosion: 20,
        ActiveHpPermille: activeHp ?? [hp],
        CurrentNetherGold: 100,
        CurrentTreasureKeys: keys,
        Settings: Settings(),
        CanCloseShop: canCloseShop
    )
    {
        PartialDeathEligibility = partialDeathEligibility ?? [],
    });

    private static NetherInteractivePartialDeathEligibility Eligibility(
        NetherFloorNodeType floorKind,
        long eventId,
        long partId
    ) => new(
        floorKind == NetherFloorNodeType.Treasure
            ? NetherInteractivePartialDeathObjectiveKind.TreasureHpPayment
            : NetherInteractivePartialDeathObjectiveKind.HpPaidEventKeyForRank5Treasure,
        eventId,
        partId,
        ObjectiveNodeId: 3
    )
    {
        IsKnown = true,
        ObjectiveReachable = true,
        ExactTreasureRank = 5,
        NoBetterAffordableCurrencyKeySource = true,
    };

    private static EventFixture Event(
        long id,
        long floorMasterId,
        int weight,
        long part1,
        long part2 = 0,
        long part3 = 0,
        long part4 = 0
    ) => new()
    {
        id = id,
        m_nether_map_floor_id = floorMasterId,
        weight = weight,
        type = 4,
        m_nether_floor_event_part_id_1 = part1,
        m_nether_floor_event_part_id_2 = part2,
        m_nether_floor_event_part_id_3 = part3,
        m_nether_floor_event_part_id_4 = part4,
    };

    private static PartFixture Part(
        long id,
        int targetType,
        long parameter,
        int contentType = 0,
        long contentId = 0,
        long amount = 0
    ) => new()
    {
        id = id,
        target_type_1 = targetType,
        select_parameter_1 = parameter,
        content_type = contentType,
        content_id = contentId,
        amount = amount,
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
        public long id { get; init; }
        public int min_erosion_point { get; init; }
        public int max_erosion_point { get; init; }
    }

    private sealed class EventFixture
    {
        public long id { get; init; }
        public long m_nether_map_floor_id { get; init; }
        public int weight { get; init; }
        public int type { get; init; }
        public long m_nether_floor_event_part_id_1 { get; init; }
        public long m_nether_floor_event_part_id_2 { get; init; }
        public long m_nether_floor_event_part_id_3 { get; init; }
        public long m_nether_floor_event_part_id_4 { get; init; }
    }

    private sealed class PartFixture
    {
        public long id { get; init; }
        public int target_type_1 { get; init; }
        public long select_parameter_1 { get; init; }
        public int target_type_2 { get; init; }
        public long select_parameter_2 { get; init; }
        public int target_type_3 { get; init; }
        public long select_parameter_3 { get; init; }
        public int content_type { get; init; }
        public long content_id { get; init; }
        public long amount { get; init; }
    }
}
