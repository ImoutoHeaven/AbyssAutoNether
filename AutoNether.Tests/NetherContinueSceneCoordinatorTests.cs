using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherContinueSceneCoordinatorTests
{
    [Fact]
    public void Parent_terminal_then_owned_teardown_rebind_and_one_get_reconcile_completes()
    {
        var driver = new FakeDriver(
            parent: new[]
            {
                NetherNativeActionResult.Started("continue-parent-pending"),
                NetherNativeActionResult.Completed("continue-parent-terminal"),
            },
            appliedSnapshot: AppliedSnapshot()
        )
        {
            CurrentRuntimeGeneration = 41,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 41));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);

        driver.FloorOwnerTerminated = true;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);

        driver.CurrentRuntimeGeneration = 42;
        driver.IsExpectedNetherTopScene = true;
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Complete, terminal.Kind);
        Assert.Equal(3, terminal.Snapshot!.MapId);
        Assert.Equal(33, terminal.Snapshot.CurrentFloorId);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
        Assert.Equal(0, driver.StartOrMutationCalls);

        Assert.Equal(NetherContinueSceneStepKind.Complete, coordinator.Pump().Kind);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
    }

    [Fact]
    public void Server_assigned_destination_completes_when_ticket_status_segment_and_new_identity_are_exact()
    {
        var driver = ReadyForReconcileDriver(AppliedSnapshot());
        var coordinator = new NetherContinueSceneCoordinator(driver);
        var contract = new NetherContinueSceneContract(
            ExpectedMapId: 0,
            ExpectedFloorId: 0,
            ExpectedSegmentFloorLevel: 10,
            TicketCost: 1,
            ExpectedStatus: NetherSessionStatus.Play
        );

        Assert.True(coordinator.Begin(contract, BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        driver.FloorOwnerTerminated = true;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        driver.CurrentRuntimeGeneration = 11;
        driver.IsExpectedNetherTopScene = true;
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Complete, terminal.Kind);
        Assert.Equal(3, terminal.Snapshot!.MapId);
        Assert.Equal(33, terminal.Snapshot.CurrentFloorId);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
    }

    [Fact]
    public void Server_assigned_destination_accepts_reused_identifiers_after_complete_new_scene_evidence()
    {
        NetherSnapshot reusedDestination = AppliedSnapshot() with
        {
            MapId = BeforeSnapshot().MapId,
            CurrentFloorId = BeforeSnapshot().CurrentFloorId,
            MapHash = BeforeSnapshot().MapHash,
        };
        var driver = ReadyForReconcileDriver(reusedDestination);
        var coordinator = new NetherContinueSceneCoordinator(driver);
        var contract = new NetherContinueSceneContract(
            ExpectedMapId: 0,
            ExpectedFloorId: 0,
            ExpectedSegmentFloorLevel: 10,
            TicketCost: 1,
            ExpectedStatus: NetherSessionStatus.Play
        );

        NetherContinueSceneStep terminal = DriveToTerminal(coordinator, driver, contract);

        Assert.Equal(NetherContinueSceneStepKind.Complete, terminal.Kind);
        Assert.Equal(11, driver.CurrentRuntimeGeneration);
        Assert.True(driver.HasEnteredCurrentGeneration);
        Assert.True(driver.HasAuthoritativeSnapshot);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
    }

    [Fact]
    public void Server_assigned_zero_floor_settlement_converges_by_coordinate_to_full_new_presentation()
    {
        const long completedNodeId = 90194313218;
        const long entryNodeId = 94489280513;
        const long nextNodeId = 98784247810;
        NetherSnapshot before = Snapshot(
            NetherSessionStatus.Sleep,
            mapId: 1,
            floorId: 96,
            floorLevel: 20,
            ticketCount: 62,
            mapHash: "completed-segment-20"
        ) with
        {
            CurrentNodeId = completedNodeId,
            FloorIndex = 1,
            Characters = new[] { new NetherCharacterState(1100012, 954, true) },
            Floors = new[]
            {
                new NetherFloorNode(96, 20, 1, NetherFloorNodeType.Boss)
                {
                    NodeId = completedNodeId,
                    ApiFloorIndex = 1,
                    IsUnlocked = true,
                },
            },
        };
        NetherSnapshot presentation = Snapshot(
            NetherSessionStatus.Play,
            mapId: 1,
            floorId: 101,
            floorLevel: 20,
            ticketCount: 61,
            mapHash: "new-segment-21"
        ) with
        {
            CurrentNodeId = entryNodeId,
            FloorIndex = 1,
            Characters = before.Characters,
            Floors = new[]
            {
                new NetherFloorNode(101, 20, 1, NetherFloorNodeType.Event)
                {
                    NodeId = entryNodeId,
                    ApiFloorIndex = 1,
                    IsUnlocked = true,
                },
                new NetherFloorNode(102, 21, 0, NetherFloorNodeType.Boss)
                {
                    NodeId = nextNodeId,
                    ApiFloorIndex = 0,
                    IsUnlocked = true,
                    PreviousFloorIds = new[] { entryNodeId },
                },
            },
        };
        var cache = new NetherTransitionSnapshotCache();
        cache.ObserveFullSnapshot(before);
        var driver = new FakeDriver(
            parent: new[] { NetherNativeActionResult.Completed("continue-parent-terminal") },
            appliedSnapshot: presentation,
            continueSnapshotCache: cache,
            continueAuthoritativeState: new NetherAuthoritativeTransitionState
            {
                Status = NetherSessionStatus.Play,
                NetherId = 1,
                MapId = 1,
                CurrentFloorId = 0,
                FloorLevel = 20,
                FloorIndex = 1,
                MaxFloorLevel = 130,
                ContinuanceFloorLevel = 20,
                MasterMaxFloorLevel = 130,
                ErosionPoint = presentation.ErosionPoint,
                TicketCount = 61,
                TreasureKeyCount = presentation.TreasureKeyCount,
                NetherGold = presentation.NetherGold,
                CodeReloadCount = presentation.CodeReloadCount,
                LockReward = presentation.LockReward,
                Codes = Array.Empty<NetherCodeState>(),
                AcquiredItems = Array.Empty<NetherRewardItem>(),
            }
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver);
        var contract = new NetherContinueSceneContract(
            ExpectedMapId: 0,
            ExpectedFloorId: 0,
            ExpectedSegmentFloorLevel: 20,
            TicketCost: 1,
            ExpectedStatus: NetherSessionStatus.Play
        );

        Assert.True(coordinator.Begin(contract, before, ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        driver.FloorOwnerTerminated = true;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        driver.CurrentRuntimeGeneration = 11;
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Complete, terminal.Kind);
        Assert.Equal(101, terminal.Snapshot!.CurrentFloorId);
        Assert.Equal(entryNodeId, terminal.Snapshot.CurrentNodeId);
        Assert.Equal(2, terminal.Snapshot.Floors.Count);
        Assert.Equal("new-segment-21", terminal.Snapshot.MapHash);
        Assert.Equal(1, driver.ContinueAppliedSnapshotReads);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
        Assert.Equal(0, driver.StartOrMutationCalls);

        NetherRoutePlan route = new NetherRoutePlanner().Plan(
            terminal.Snapshot,
            new NetherRouteSafetyContext()
        );
        Assert.True(route.HasSelection, route.PauseDetail);
        Assert.Equal(nextNodeId, route.SelectedNode!.NodeId);
    }

    [Fact]
    public void Coordinate_only_settlement_waits_for_a_graph_resolved_current_node()
    {
        NetherSnapshot applied = AppliedSnapshot() with
        {
            CurrentFloorId = 0,
            CurrentNodeId = 0,
            Floors = Array.Empty<NetherFloorNode>(),
            MapHash = string.Empty,
        };
        NetherSnapshot unresolvedPresentation = AppliedSnapshot() with
        {
            CurrentNodeId = 141733920769,
            Floors = Array.Empty<NetherFloorNode>(),
        };
        var driver = new FakeDriver(
            parent: new[] { NetherNativeActionResult.Completed("continue-parent-terminal") },
            appliedSnapshot: unresolvedPresentation,
            continueAppliedSnapshots: new[]
            {
                NetherReadOnlySnapshotResult.Success(applied),
            }
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver);
        var contract = new NetherContinueSceneContract(
            ExpectedMapId: 0,
            ExpectedFloorId: 0,
            ExpectedSegmentFloorLevel: 10,
            TicketCost: 1,
            ExpectedStatus: NetherSessionStatus.Play
        );

        NetherContinueSceneStep waiting = DriveToTerminal(coordinator, driver, contract);

        Assert.Equal(NetherContinueSceneStepKind.WaitForPresentation, waiting.Kind);
        Assert.Contains("applied-awaiting-presentation", waiting.Detail);
        Assert.Equal(1, driver.ContinueAppliedSnapshotReads);
        Assert.Equal(0, driver.StartOrMutationCalls);
    }

    [Fact]
    public void Predicted_destination_does_not_accept_coordinate_only_zero_floor()
    {
        NetherSnapshot coordinateOnly = AppliedSnapshot() with
        {
            CurrentFloorId = 0,
            CurrentNodeId = 0,
            Floors = Array.Empty<NetherFloorNode>(),
            MapHash = string.Empty,
        };
        var driver = ReadyForReconcileDriver(coordinateOnly);
        var coordinator = new NetherContinueSceneCoordinator(driver);

        NetherContinueSceneStep terminal = DriveToTerminal(coordinator, driver, Contract());

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("wrong-floor", terminal.Detail);
    }

    [Fact]
    public void Exact_scene_transition_completes_handoff_when_recovered_parent_stays_pending()
    {
        var driver = new FakeDriver(
            parent: new[]
            {
                NetherNativeActionResult.Started("native-start-status-wrapper-pending:Pending"),
            },
            appliedSnapshot: AppliedSnapshot()
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);

        driver.FloorOwnerTerminated = true;
        driver.CurrentRuntimeGeneration = 11;
        driver.IsExpectedNetherTopScene = true;

        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        Assert.True(coordinator.ParentTerminalObserved);
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Complete, terminal.Kind);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
        Assert.Equal(0, driver.StartOrMutationCalls);
    }

    [Fact]
    public void New_runtime_registered_before_nether_model_waits_for_snapshot_without_a_second_get()
    {
        var driver = new FakeDriver(
            parent: new[] { NetherNativeActionResult.Completed("continue-parent-terminal") },
            appliedSnapshot: AppliedSnapshot()
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver, maximumMissingTicks: 2);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);

        driver.FloorOwnerTerminated = true;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        driver.CurrentRuntimeGeneration = 11;
        driver.IsExpectedNetherTopScene = true;
        driver.HasAuthoritativeSnapshot = false;

        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        Assert.Equal(0, driver.GetOnlyBeginCalls);

        driver.HasAuthoritativeSnapshot = true;
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);

        Assert.Equal(NetherContinueSceneStepKind.Complete, coordinator.Pump().Kind);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
        Assert.Equal(1, driver.AppliedSnapshotReads);
        Assert.Equal(0, driver.StartOrMutationCalls);
    }

    [Fact]
    public void New_controller_waits_for_matching_subscene_on_entered_before_get()
    {
        var driver = new FakeDriver(
            parent: new[] { NetherNativeActionResult.Completed("continue-parent-terminal") },
            appliedSnapshot: AppliedSnapshot()
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver, maximumMissingTicks: 2);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        driver.FloorOwnerTerminated = true;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        driver.CurrentRuntimeGeneration = 11;
        driver.IsExpectedNetherTopScene = true;
        driver.HasEnteredCurrentGeneration = false;

        NetherContinueSceneStep waiting = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, waiting.Kind);
        Assert.Contains("awaiting-subscene-entered", waiting.Detail);
        Assert.Equal(0, driver.GetOnlyBeginCalls);

        driver.HasEnteredCurrentGeneration = true;
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Complete, coordinator.Pump().Kind);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
    }

    [Fact]
    public void Continue_rebind_waits_for_scene_model_then_uses_applied_datastore_over_stale_presentation()
    {
        var driver = new FakeDriver(
            parent: new[]
            {
                NetherNativeActionResult.Started("native-start-status-wrapper-pending:Pending"),
            },
            appliedSnapshot: AppliedSnapshot(),
            readySnapshots: new[]
            {
                NetherFloorSceneSnapshotResult.Waiting(11, "awaiting-authoritative-snapshot"),
                NetherFloorSceneSnapshotResult.Ready(
                    11,
                    BeforeSnapshot() with { TicketCount = AppliedSnapshot().TicketCount }
                ),
                NetherFloorSceneSnapshotResult.Ready(11, AppliedSnapshot()),
            }
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver, maximumMissingTicks: 3);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);

        driver.FloorOwnerTerminated = true;
        driver.CurrentRuntimeGeneration = 11;
        driver.IsExpectedNetherTopScene = true;

        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);

        NetherContinueSceneStep fenced = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.WaitForPresentation, fenced.Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Complete, terminal.Kind);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
        Assert.Equal(1, driver.AppliedSnapshotReads);
        Assert.Equal(1, driver.ContinueAppliedSnapshotReads);
        Assert.Equal(3, driver.ReadySnapshotReadsAfterGet);
        Assert.Equal(0, driver.StartOrMutationCalls);
    }

    [Fact]
    public void Continue_settlement_uses_authoritative_datastore_when_rebound_floor_model_is_stale()
    {
        NetherSnapshot stalePresentation = BeforeSnapshot() with
        {
            TicketCount = AppliedSnapshot().TicketCount,
        };
        var driver = new FakeDriver(
            parent: new[] { NetherNativeActionResult.Completed("continue-parent-terminal") },
            appliedSnapshot: stalePresentation,
            readySnapshots: new[]
            {
                NetherFloorSceneSnapshotResult.Ready(11, stalePresentation),
                NetherFloorSceneSnapshotResult.Ready(11, AppliedSnapshot()),
            },
            continueAppliedSnapshots: new[]
            {
                NetherReadOnlySnapshotResult.Success(AppliedSnapshot()),
            }
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver);

        NetherContinueSceneStep fenced = DriveToTerminal(coordinator, driver);

        Assert.Equal(NetherContinueSceneStepKind.WaitForPresentation, fenced.Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Complete, terminal.Kind);
        Assert.Equal(NetherSessionStatus.Play, terminal.Snapshot!.Status);
        Assert.Equal(2, terminal.Snapshot.TicketCount);
        Assert.Equal(1, driver.ContinueAppliedSnapshotReads);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(0, driver.StartOrMutationCalls);
    }

    [Fact]
    public void Continue_wait_repolls_authoritative_datastore_instead_of_stale_floor_model()
    {
        NetherSnapshot stalePresentation = BeforeSnapshot() with
        {
            TicketCount = AppliedSnapshot().TicketCount,
        };
        var driver = new FakeDriver(
            parent: new[] { NetherNativeActionResult.Completed("continue-parent-terminal") },
            appliedSnapshot: stalePresentation,
            readySnapshots: new[]
            {
                NetherFloorSceneSnapshotResult.Ready(11, stalePresentation),
                NetherFloorSceneSnapshotResult.Ready(11, stalePresentation),
                NetherFloorSceneSnapshotResult.Ready(11, AppliedSnapshot()),
            },
            continueAppliedSnapshots: new[]
            {
                NetherReadOnlySnapshotResult.Success(stalePresentation),
                NetherReadOnlySnapshotResult.Success(AppliedSnapshot()),
            }
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver, maximumMissingTicks: 3);

        NetherContinueSceneStep waiting = DriveToTerminal(coordinator, driver);

        Assert.Equal(NetherContinueSceneStepKind.Reconcile, waiting.Kind);
        Assert.Contains("awaiting-applied-snapshot", waiting.Detail);

        NetherContinueSceneStep fenced = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.WaitForPresentation, fenced.Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Complete, terminal.Kind);
        Assert.Equal(2, driver.ContinueAppliedSnapshotReads);
        Assert.Equal(3, driver.ReadySnapshotReadsAfterGet);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(0, driver.StartOrMutationCalls);
    }

    [Fact]
    public void Missing_nether_model_snapshot_wait_is_bounded_and_never_repeats_the_get()
    {
        var driver = new FakeDriver(
            parent: new[]
            {
                NetherNativeActionResult.Started("native-start-status-wrapper-pending:Pending"),
            },
            appliedSnapshot: AppliedSnapshot(),
            readySnapshots: new[]
            {
                NetherFloorSceneSnapshotResult.Waiting(11, "awaiting-authoritative-snapshot"),
                NetherFloorSceneSnapshotResult.Waiting(11, "awaiting-authoritative-snapshot"),
            }
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver, maximumMissingTicks: 1);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);

        driver.FloorOwnerTerminated = true;
        driver.CurrentRuntimeGeneration = 11;
        driver.IsExpectedNetherTopScene = true;

        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("snapshot-timeout", terminal.Detail);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
        Assert.Equal(1, driver.AppliedSnapshotReads);
        Assert.Equal(2, driver.ReadySnapshotReadsAfterGet);
        Assert.Equal(0, driver.StartOrMutationCalls);
    }

    [Fact]
    public void Non_transient_applied_snapshot_binding_failure_still_pauses_immediately()
    {
        var driver = new FakeDriver(
            parent: new[] { NetherNativeActionResult.Completed("continue-parent-terminal") },
            appliedSnapshot: AppliedSnapshot(),
            appliedSnapshots: new[]
            {
                NetherReadOnlySnapshotResult.Failure("missing-nether-data-store"),
            }
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver, maximumMissingTicks: 2);

        NetherContinueSceneStep terminal = DriveToTerminal(coordinator, driver);

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("missing-nether-data-store", terminal.Detail);
        Assert.DoesNotContain("awaiting-snapshot", terminal.Detail);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
        Assert.Equal(1, driver.AppliedSnapshotReads);
        Assert.Equal(0, driver.StartOrMutationCalls);
    }

    [Fact]
    public void Server_assigned_destination_rejects_nonpositive_identifiers()
    {
        NetherSnapshot unchangedIdentity = AppliedSnapshot() with
        {
            MapId = 0,
            CurrentFloorId = 0,
        };
        var driver = ReadyForReconcileDriver(unchangedIdentity);
        var coordinator = new NetherContinueSceneCoordinator(driver);
        var contract = new NetherContinueSceneContract(
            ExpectedMapId: 0,
            ExpectedFloorId: 0,
            ExpectedSegmentFloorLevel: 10,
            TicketCost: 1,
            ExpectedStatus: NetherSessionStatus.Play
        );

        NetherContinueSceneStep terminal = DriveToTerminal(
            coordinator,
            driver,
            contract
        );

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("wrong-destination", terminal.Detail);
    }

    [Fact]
    public void Continue_rejects_a_response_that_fabricates_first_floor_progress()
    {
        NetherSnapshot advancedWithoutSelectingNode = AppliedSnapshot() with { FloorLevel = 11 };
        var driver = ReadyForReconcileDriver(advancedWithoutSelectingNode);
        var coordinator = new NetherContinueSceneCoordinator(driver);

        NetherContinueSceneStep terminal = DriveToTerminal(
            coordinator,
            driver,
            Contract()
        );

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("wrong-segment", terminal.Detail);
    }

    [Fact]
    public void Rebind_in_a_wrong_scene_pauses_before_get_reconcile()
    {
        var driver = TerminalParentDriver();
        var coordinator = new NetherContinueSceneCoordinator(driver);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        driver.FloorOwnerTerminated = true;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        driver.CurrentRuntimeGeneration = 11;
        driver.IsExpectedNetherTopScene = false;

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("wrong-scene", terminal.Detail);
        Assert.Equal(0, driver.GetOnlyBeginCalls);
    }

    [Fact]
    public void Rebind_with_the_old_or_wrong_generation_pauses_before_get_reconcile()
    {
        var driver = TerminalParentDriver();
        var coordinator = new NetherContinueSceneCoordinator(driver);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        driver.FloorOwnerTerminated = true;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        driver.CurrentRuntimeGeneration = 10;
        driver.IsExpectedNetherTopScene = true;

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("wrong-generation", terminal.Detail);
        Assert.Equal(0, driver.GetOnlyBeginCalls);
    }

    [Fact]
    public void One_absent_generation_tick_between_teardown_and_new_owner_waits_then_rebinds()
    {
        var driver = TerminalParentDriver();
        var coordinator = new NetherContinueSceneCoordinator(driver, maximumMissingTicks: 2);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        driver.FloorOwnerTerminated = true;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);

        // Production reports absence (0), not the retained monotonic old-owner number.
        driver.CurrentRuntimeGeneration = 0;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);

        driver.CurrentRuntimeGeneration = 11;
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Complete, coordinator.Pump().Kind);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
    }

    [Fact]
    public void Missing_owned_teardown_is_bounded_and_pauses_without_a_get()
    {
        var driver = TerminalParentDriver();
        var coordinator = new NetherContinueSceneCoordinator(driver, maximumMissingTicks: 1);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("teardown-timeout", terminal.Detail);
        Assert.Equal(0, driver.GetOnlyBeginCalls);
    }

    [Fact]
    public void Ticket_not_exactly_minus_one_pauses_after_the_single_authoritative_reconcile()
    {
        NetherSnapshot wrongTicket = AppliedSnapshot() with { TicketCount = 1 };
        var driver = ReadyForReconcileDriver(wrongTicket);
        var coordinator = new NetherContinueSceneCoordinator(driver);

        NetherContinueSceneStep terminal = DriveToTerminal(coordinator, driver);

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("wrong-ticket", terminal.Detail);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
    }

    [Fact]
    public void Wrong_destination_map_pauses_after_the_single_authoritative_reconcile()
    {
        NetherSnapshot wrongMap = AppliedSnapshot() with { MapId = 4 };
        var driver = ReadyForReconcileDriver(wrongMap);
        var coordinator = new NetherContinueSceneCoordinator(driver);

        NetherContinueSceneStep terminal = DriveToTerminal(coordinator, driver);

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains("wrong-map", terminal.Detail);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
    }

    [Fact]
    public void Parent_canceled_by_exact_owner_teardown_waits_for_complete_new_scene_evidence()
    {
        var driver = new FakeDriver(
            parent: new[]
            {
                NetherNativeActionResult.Started("native-start-status-parent-pending"),
                NetherNativeActionResult.UnknownOutcome("native-start-status-terminal-canceled"),
            },
            appliedSnapshot: AppliedSnapshot()
        )
        {
            CurrentRuntimeGeneration = 10,
        };
        var coordinator = new NetherContinueSceneCoordinator(driver);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);

        // Current-client native order: Continue changes scene and then awaits WaitWhile with
        // this exact controller's GetCancellationTokenOnDestroy token. Its generated parent is
        // therefore Canceled when the old owner is destroyed even though handoff has begun.
        driver.FloorOwnerTerminated = true;
        NetherContinueSceneStep canceledAfterTeardown = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, canceledAfterTeardown.Kind);
        Assert.Contains("canceled-after-owner-transition", canceledAfterTeardown.Detail);
        Assert.True(coordinator.ParentTerminalObserved);
        Assert.Equal(0, driver.GetOnlyBeginCalls);

        // Owner teardown alone is not success. A strictly newer controller, its matching
        // SubScene.OnEntered, and an authoritative snapshot are all required before one GET.
        driver.CurrentRuntimeGeneration = 11;
        driver.HasEnteredCurrentGeneration = false;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        Assert.Equal(0, driver.GetOnlyBeginCalls);

        driver.HasEnteredCurrentGeneration = true;
        driver.HasAuthoritativeSnapshot = false;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        Assert.Equal(0, driver.GetOnlyBeginCalls);

        driver.HasAuthoritativeSnapshot = true;
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);

        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Complete, terminal.Kind);
        Assert.Equal(1, driver.GetOnlyBeginCalls);
        Assert.Equal(1, driver.GetOnlyPollCalls);
        Assert.Equal(0, driver.StartOrMutationCalls);
    }

    [Theory]
    [InlineData("native-result-faulted", "parent-fault")]
    [InlineData("native-result-canceled", "parent-canceled")]
    public void Parent_fault_or_cancel_is_named_pause_and_never_reconciles(string detail, string expected)
    {
        var driver = new FakeDriver(
            parent: new[] { NetherNativeActionResult.UnknownOutcome(detail) },
            appliedSnapshot: AppliedSnapshot()
        );
        var coordinator = new NetherContinueSceneCoordinator(driver);

        Assert.True(coordinator.Begin(Contract(), BeforeSnapshot(), ownerGeneration: 10));
        NetherContinueSceneStep terminal = coordinator.Pump();

        Assert.Equal(NetherContinueSceneStepKind.Pause, terminal.Kind);
        Assert.Contains(expected, terminal.Detail);
        Assert.Equal(0, driver.GetOnlyBeginCalls);
    }

    private static NetherContinueSceneStep DriveToTerminal(NetherContinueSceneCoordinator coordinator, FakeDriver driver)
        => DriveToTerminal(coordinator, driver, Contract());

    private static NetherContinueSceneStep DriveToTerminal(
        NetherContinueSceneCoordinator coordinator,
        FakeDriver driver,
        NetherContinueSceneContract contract
    )
    {
        Assert.True(coordinator.Begin(contract, BeforeSnapshot(), ownerGeneration: 10));
        Assert.Equal(NetherContinueSceneStepKind.WaitForTeardown, coordinator.Pump().Kind);
        driver.FloorOwnerTerminated = true;
        Assert.Equal(NetherContinueSceneStepKind.WaitForRebind, coordinator.Pump().Kind);
        driver.CurrentRuntimeGeneration = 11;
        driver.IsExpectedNetherTopScene = true;
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        Assert.Equal(NetherContinueSceneStepKind.Reconcile, coordinator.Pump().Kind);
        return coordinator.Pump();
    }

    private static FakeDriver TerminalParentDriver() => new(
        parent: new[] { NetherNativeActionResult.Completed("continue-parent-terminal") },
        appliedSnapshot: AppliedSnapshot()
    )
    {
        CurrentRuntimeGeneration = 10,
    };

    private static FakeDriver ReadyForReconcileDriver(NetherSnapshot after) => new(
        parent: new[] { NetherNativeActionResult.Completed("continue-parent-terminal") },
        appliedSnapshot: after
    )
    {
        CurrentRuntimeGeneration = 10,
    };

    private static NetherContinueSceneContract Contract() => new(
        ExpectedMapId: 3,
        ExpectedFloorId: 33,
        ExpectedSegmentFloorLevel: 10,
        TicketCost: 1,
        ExpectedStatus: NetherSessionStatus.Play
    );

    private static NetherSnapshot BeforeSnapshot() => Snapshot(
        status: NetherSessionStatus.Sleep,
        mapId: 2,
        floorId: 23,
        floorLevel: 10,
        ticketCount: 3,
        mapHash: "map-2"
    );

    private static NetherSnapshot AppliedSnapshot() => Snapshot(
        status: NetherSessionStatus.Play,
        mapId: 3,
        floorId: 33,
        floorLevel: 10,
        ticketCount: 2,
        mapHash: "map-3"
    );

    private static NetherSnapshot Snapshot(
        NetherSessionStatus status,
        long mapId,
        long floorId,
        int floorLevel,
        int ticketCount,
        string mapHash
    ) => new()
    {
        Status = status,
        NetherId = 1,
        MapId = mapId,
        CurrentFloorId = floorId,
        FloorLevel = floorLevel,
        FloorIndex = 0,
        ErosionPoint = 20,
        TicketCount = ticketCount,
        TreasureKeyCount = 1,
        NetherGold = 100,
        CodeReloadCount = 2,
        LockReward = 1,
        CharacterHpHash = "1:1000:1",
        CodeHash = "30024:5:1",
        MapHash = mapHash,
    };

    private sealed class FakeDriver : INetherContinueSceneDriver
    {
        private readonly Queue<NetherNativeActionResult> _parent;
        private readonly Queue<NetherNativeActionResult> _polls;
        private readonly Queue<NetherReadOnlySnapshotResult> _appliedSnapshots;
        private readonly Queue<NetherReadOnlySnapshotResult> _continueAppliedSnapshots;
        private readonly Queue<NetherFloorSceneSnapshotResult> _readySnapshots;
        private readonly NetherSnapshot _defaultReadySnapshot;
        private readonly NetherTransitionSnapshotCache? _continueSnapshotCache;
        private readonly NetherAuthoritativeTransitionState? _continueAuthoritativeState;

        public FakeDriver(
            IEnumerable<NetherNativeActionResult> parent,
            NetherSnapshot appliedSnapshot,
            IEnumerable<NetherReadOnlySnapshotResult>? appliedSnapshots = null,
            IEnumerable<NetherFloorSceneSnapshotResult>? readySnapshots = null,
            IEnumerable<NetherReadOnlySnapshotResult>? continueAppliedSnapshots = null,
            NetherTransitionSnapshotCache? continueSnapshotCache = null,
            NetherAuthoritativeTransitionState? continueAuthoritativeState = null
        )
        {
            if ((continueSnapshotCache == null) != (continueAuthoritativeState == null))
            {
                throw new ArgumentException(
                    "Continue snapshot cache and authoritative state must be supplied together."
                );
            }
            _parent = new Queue<NetherNativeActionResult>(parent);
            _polls = new Queue<NetherNativeActionResult>(new[]
            {
                NetherNativeActionResult.Completed("native-nether-sync-complete"),
            });
            _appliedSnapshots = new Queue<NetherReadOnlySnapshotResult>(
                appliedSnapshots ?? new[] { NetherReadOnlySnapshotResult.Success(appliedSnapshot) }
            );
            _continueAppliedSnapshots = new Queue<NetherReadOnlySnapshotResult>(
                continueAppliedSnapshots ?? new[] { NetherReadOnlySnapshotResult.Success(appliedSnapshot) }
            );
            _readySnapshots = new Queue<NetherFloorSceneSnapshotResult>(
                readySnapshots ?? Array.Empty<NetherFloorSceneSnapshotResult>()
            );
            _defaultReadySnapshot = appliedSnapshot;
            _continueSnapshotCache = continueSnapshotCache;
            _continueAuthoritativeState = continueAuthoritativeState;
        }

        public bool FloorOwnerTerminated { get; set; }
        public long CurrentRuntimeGeneration { get; set; }
        public bool IsExpectedNetherTopScene { get; set; } = true;
        public bool HasEnteredCurrentGeneration { get; set; } = true;
        public bool HasAuthoritativeSnapshot { get; set; } = true;
        public int GetOnlyBeginCalls { get; private set; }
        public int GetOnlyPollCalls { get; private set; }
        public int AppliedSnapshotReads { get; private set; }
        public int ContinueAppliedSnapshotReads { get; private set; }
        public int ReadySnapshotReadsAfterGet { get; private set; }
        public int StartOrMutationCalls { get; private set; }

        public NetherNativeActionResult PollContinueParent() => _parent.Count > 0
            ? _parent.Dequeue()
            : NetherNativeActionResult.Started("continue-parent-still-pending");

        public NetherNativeActionResult BeginGetOnlyRefresh()
        {
            GetOnlyBeginCalls++;
            return NetherNativeActionResult.Started("native-nether-sync");
        }

        public NetherNativeActionResult PollGetOnlyRefresh()
        {
            GetOnlyPollCalls++;
            return _polls.Dequeue();
        }

        public NetherReadOnlySnapshotResult TryCaptureAppliedSnapshot()
        {
            AppliedSnapshotReads++;
            return _appliedSnapshots.Dequeue();
        }

        public NetherReadOnlySnapshotResult TryCaptureContinueAppliedSnapshot()
        {
            ContinueAppliedSnapshotReads++;
            if (_continueSnapshotCache != null && _continueAuthoritativeState != null)
            {
                NetherRuntimeSnapshotResult composed =
                    NetherTransitionSnapshotCompositionPolicy.Compose(
                        _continueSnapshotCache,
                        _continueAuthoritativeState,
                        requireFreshBattleCharacters: false,
                        purpose: NetherTransitionSnapshotPurpose.ContinueSettlement
                    );
                return composed.IsSuccess
                    ? NetherReadOnlySnapshotResult.Success(composed.Snapshot!)
                    : NetherReadOnlySnapshotResult.Failure(composed.Detail);
            }
            return _continueAppliedSnapshots.Dequeue();
        }

        public NetherFloorSceneSnapshotResult TryCaptureReadyFloorSceneSnapshot(
            long minimumGenerationExclusive = 0
        )
        {
            NetherFloorSceneReadinessDecision readiness = NetherFloorSceneReadiness.Evaluate(new(
                minimumGenerationExclusive,
                CurrentRuntimeGeneration,
                HasCurrentController: CurrentRuntimeGeneration > 0,
                IsExpectedCurrentController: IsExpectedNetherTopScene,
                HasEnteredCurrentGeneration,
                HasAuthoritativeSnapshot,
                CaptureStayedOnCurrentController: true
            ));
            if (!readiness.IsReady)
            {
                return NetherFloorSceneSnapshotResult.Waiting(
                    CurrentRuntimeGeneration,
                    readiness.Detail
                );
            }

            if (GetOnlyBeginCalls == 0 || _readySnapshots.Count == 0)
            {
                return NetherFloorSceneSnapshotResult.Ready(
                    CurrentRuntimeGeneration,
                    _defaultReadySnapshot
                );
            }

            ReadySnapshotReadsAfterGet++;
            return _readySnapshots.Dequeue();
        }
    }
}
