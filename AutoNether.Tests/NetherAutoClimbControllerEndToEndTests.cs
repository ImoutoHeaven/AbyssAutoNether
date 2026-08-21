#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

[CollectionDefinition("nether-controller-runtime", DisableParallelization = true)]
public sealed class NetherControllerRuntimeCollection
{
}

[Collection("nether-controller-runtime")]
public class NetherAutoClimbControllerEndToEndTests
{
    [Theory]
    [InlineData((int)NetherStrategyMode.Research, 95, 80, 0)]
    [InlineData((int)NetherStrategyMode.Equipment, 95, 80, 80)]
    public void Production_controller_starts_not_played_run_from_mode_derived_native_floor(
        int rawMode,
        int configuredTarget,
        int recoveryFloor,
        int expectedStartFloor
    )
    {
        // Fresh Project.dll/CPP2IL evidence: FloorSelection.SubViewController
        // <CreateNetherModelAsync>d__38 owns the NotPlayed start and invokes
        // NetherApiDataStore.RequestNetherStartAsync(1, 1, 0, ct). The party controller's
        // TransitionNetherFloorSelectionSceneAsync flow exposes the same native mutation with
        // an explicit startFloorLevel input; policy must feed that input instead of pausing.
        int previousTarget = Config.NetherAutoClimbMaxDepth.Value;
        NetherStrategyMode previousMode = Config.NetherAutoClimbStrategyMode.Value;
        NetherCodeFamily previousPrimary = Config.NetherAutoClimbResearchPrimaryFamily.Value;
        var bridge = new ScriptedRuntimeBridge();
        bridge.CurrentSnapshot = bridge.PlayBeforeInteractive with
        {
            Status = NetherSessionStatus.NotPlayed,
            CurrentFloorId = 0,
            FloorLevel = 0,
            FloorIndex = 0,
            RecoveryFloorLevel = recoveryFloor,
            MapHash = "not-played-start-boundary",
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );
        Config.NetherAutoClimbMaxDepth.Value = configuredTarget;
        Config.NetherAutoClimbStrategyMode.Value = (NetherStrategyMode)rawMode;
        Config.NetherAutoClimbResearchPrimaryFamily.Value = NetherCodeFamily.Safe;

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            NetherPlannedAction started = Assert.Single(bridge.NativeActions);
            Assert.Equal(NetherActionKind.StartRun, started.Kind);
            Assert.Equal(expectedStartFloor, started.FloorLevel);
            Assert.Equal(NetherSessionStatus.NotPlayed, started.ExpectedBeforeStatus);
            Assert.Equal(NetherSessionStatus.Play, started.ExpectedAfterStatus);
            Assert.Equal(NetherAutoClimbPhase.ExecutingNativeAction, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.None, NetherAutoClimbController.PauseReason);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Config.NetherAutoClimbMaxDepth.Value = previousTarget;
            Config.NetherAutoClimbStrategyMode.Value = previousMode;
            Config.NetherAutoClimbResearchPrimaryFamily.Value = previousPrimary;
        }
    }

    [Fact]
    public void Production_controller_consumes_live_floor_seventy_checkpoint_when_boss_rows_are_non_decimal()
    {
        int previousTarget = Config.NetherAutoClimbMaxDepth.Value;
        NetherStrategyMode previousMode = Config.NetherAutoClimbStrategyMode.Value;
        var bridge = new ScriptedRuntimeBridge();
        bridge.CurrentSnapshot = bridge.PlayBeforeInteractive with
        {
            Status = NetherSessionStatus.NotPlayed,
            CurrentFloorId = 0,
            FloorLevel = 0,
            FloorIndex = 0,
            RecoveryFloorLevel = 70,
            MasterMaxFloorLevel = 75,
            AuthoritativeBossFloorLevels = new[] { 15, 30, 45, 60, 75 },
            MapHash = "not-played-non-decimal-bosses",
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );
        Config.NetherAutoClimbMaxDepth.Value = 75;
        Config.NetherAutoClimbStrategyMode.Value = NetherStrategyMode.Equipment;

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            NetherPlannedAction started = Assert.Single(bridge.NativeActions);
            Assert.Equal(NetherActionKind.StartRun, started.Kind);
            Assert.Equal(70, started.FloorLevel);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Config.NetherAutoClimbMaxDepth.Value = previousTarget;
            Config.NetherAutoClimbStrategyMode.Value = previousMode;
        }
    }

    [Fact]
    public void Production_controller_does_not_plan_until_current_floor_scene_has_entered()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            FloorSceneEntered = false,
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Empty(bridge.Invocations);
            Assert.Equal(0, bridge.BeginFloorParentCount);

            bridge.FloorSceneEntered = true;
            NetherAutoClimbController.Update();

            Assert.Equal(NetherActionKind.SelectFloor, Assert.Single(bridge.Invocations));
            Assert.Equal(1, bridge.BeginFloorParentCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_finishes_recovered_code_parent_before_any_floor_route()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            HasRecoveredCodeOffer = true,
            RecoveredCodePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.RecoveredCodeOffer,
                OwnerGeneration = 17,
                Sequence = 23,
            },
            CodeCandidates = SafeCodeCandidates(30024),
        };
        bridge.RecoveredCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Completed("scripted-recovered-code-terminal")
        );
        bridge.RecoveredCodeParentSteps.Enqueue(
            NetherNativeActionResult.Completed("scripted-start-status-parent-terminal")
        );
        bridge.RecoveredCodeRefreshSteps.Enqueue(
            NetherNativeActionResult.Completed("scripted-recovered-get-terminal")
        );
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            NetherAutoClimbController.Update();
            Assert.Equal(NetherActionKind.SelectCode, Assert.Single(bridge.RecoveredCodeActions).Kind);
            Assert.Equal(0, bridge.BeginFloorParentCount);
            Assert.Equal(0, bridge.RecoveredCodeParentPollCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            NetherAutoClimbController.Update();
            Assert.Equal(0, bridge.RecoveredCodeParentPollCount);
            Assert.Equal(0, bridge.BeginFloorParentCount);

            NetherAutoClimbController.Update();
            Assert.Equal(1, bridge.RecoveredCodeParentPollCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(0, bridge.BeginFloorParentCount);

            NetherAutoClimbController.Update();
            Assert.False(bridge.HasRecoveredCodeOffer);
            Assert.Equal(1, bridge.RecoveredCodeCompletedCount);
            Assert.Equal(0, bridge.BeginFloorParentCount);
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_adopts_recovered_code_parent_at_sleep_and_continues_to_new_segment()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            HasRecoveredCodeOffer = true,
            RecoveredCodePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.RecoveredCodeOffer,
                OwnerGeneration = 53,
                Sequence = 59,
            },
            CodeCandidates = SafeCodeCandidates(30024),
        };
        NetherSnapshot checkpoint = bridge.SleepCheckpoint with
        {
            Codes = new[]
            {
                new NetherCodeState(30024, NetherCodeFamily.Safe, 1)
                {
                    Category = NetherCodeCategory.ErosionResistance,
                    Rarity = 1,
                },
            },
            CodeHash = "30024:1:1",
        };
        bridge.RecoveredCheckpointObservation = NetherRecoveredCheckpointObservation.Ready(
            checkpoint,
            "scripted-existing-continue-popup"
        );
        bridge.RecoveredCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Completed("scripted-recovered-code-terminal")
        );
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            NetherAutoClimbController.Update(); // select recovered code
            NetherAutoClimbController.Update(); // code child terminal
            NetherAutoClimbController.Update(); // parent pending + existing Continue handoff

            Assert.Equal(NetherActionKind.SelectCode, Assert.Single(bridge.RecoveredCodeActions).Kind);
            Assert.Equal(1, bridge.RecoveredCheckpointPollCount);
            Assert.Equal(1, bridge.RecoveredCheckpointHandoffCount);
            Assert.False(bridge.HasRecoveredCodeOffer);
            Assert.Equal(0, bridge.RecoveredCodeCompletedCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.ContinuePreflightCount);
            Assert.Equal(1, bridge.ContinueNativeInvokeCount);
            Assert.Equal(NetherActionKind.Continue, bridge.Invocations.Last());

            NetherAutoClimbController.Update(); // adopted parent still pending
            bridge.ContinueParentCompleted = true;
            NetherAutoClimbController.Update(); // adopted parent terminal
            bridge.FloorOwnerTerminated = true;
            NetherAutoClimbController.Update(); // old FloorSelection terminated
            bridge.CurrentRuntimeGeneration = 2;
            bridge.CurrentSnapshot = bridge.NewSegment with
            {
                Codes = checkpoint.Codes,
                CodeHash = checkpoint.CodeHash,
            };
            NetherAutoClimbController.Update(); // new FloorSelection generation observed
            NetherAutoClimbController.Update(); // one GET-only begin
            NetherAutoClimbController.Update(); // GET terminal and exact settlement

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.None, NetherAutoClimbController.PauseReason);
            Assert.Equal(1, bridge.ContinueReadOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
            Assert.True(bridge.ContinueParentPollCount >= 2);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_adopts_server_assigned_checkpoint_with_reused_identifiers()
    {
        var bridge = new ScriptedRuntimeBridge();
        NetherSnapshot checkpoint = bridge.SleepCheckpoint with
        {
            MapId = 1,
            CurrentFloorId = 95,
            FloorLevel = 20,
            FloorIndex = 1,
            // Live floor-20 snapshots retain continuance_floor_level=10.  It is the prior
            // paid-segment marker, not the new segment's current floor.  Continue installs
            // the 21-30 map while the current completed floor stays 20 until a node is chosen.
            ContinuanceFloorLevel = 10,
            TicketCount = 84,
            ContinuationTarget = null,
            MapHash = "sleep-checkpoint-without-local-next-master",
        };
        NetherSnapshot serverAssignedSegment = bridge.NewSegment with
        {
            // Live paid continuations can keep both identifiers on the completed checkpoint.
            // The new runtime generation and authoritative entered scene prove the handoff;
            // Play plus the exact ticket decrement prove the server mutation.
            MapId = checkpoint.MapId,
            CurrentFloorId = checkpoint.CurrentFloorId,
            FloorLevel = 20,
            FloorIndex = 1,
            TicketCount = 83,
            ContinuationTarget = null,
            MapHash = checkpoint.MapHash,
        };
        bridge.CurrentSnapshot = checkpoint;
        bridge.RecoveredCheckpointObservation = NetherRecoveredCheckpointObservation.Waiting(
            "existing-start-status-parent-pending-before-popup"
        );
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            // This is the live ordering: HandleStartEventByStatusAsync is already pending, but
            // its Continue popup registers one main-thread update later.  Waiting is read-only;
            // it must neither pause nor start a duplicate checkpoint parent.
            NetherAutoClimbController.Update();
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(0, bridge.ContinuePreflightCount);
            Assert.Equal(0, bridge.ContinueNativeInvokeCount);
            Assert.Equal(0, bridge.RecoveredCheckpointHandoffCount);

            bridge.RecoveredCheckpointObservation = NetherRecoveredCheckpointObservation.Ready(
                checkpoint,
                "existing-native-continue-popup"
            );
            NetherAutoClimbController.Update();

            Assert.Equal(1, bridge.RecoveredCheckpointHandoffCount);
            Assert.Equal(1, bridge.ContinuePreflightCount);
            Assert.Equal(1, bridge.ContinueNativeInvokeCount);
            Assert.Equal(NetherActionKind.Continue, bridge.Invocations.Last());

            NetherAutoClimbController.Update(); // adopted parent pending
            bridge.ContinueParentCompleted = true;
            NetherAutoClimbController.Update(); // adopted parent terminal
            bridge.FloorOwnerTerminated = true;
            NetherAutoClimbController.Update(); // old owner teardown
            bridge.CurrentRuntimeGeneration = 2;
            bridge.CurrentSnapshot = serverAssignedSegment;
            NetherAutoClimbController.Update(); // new runtime rebind
            NetherAutoClimbController.Update(); // one GET-only begin
            NetherAutoClimbController.Update(); // GET terminal and strict settlement

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.None, NetherAutoClimbController.PauseReason);
            Assert.Equal(checkpoint.MapId, bridge.CurrentSnapshot.MapId);
            Assert.Equal(checkpoint.CurrentFloorId, bridge.CurrentSnapshot.CurrentFloorId);
            Assert.Equal(1, bridge.ContinueReadOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_reconciles_consumed_recovered_parent_fault_before_resuming_route()
    {
        bool previousDetailedLogging = Config.NetherAutoClimbDetailedLogging.Value;
        var bridge = new ScriptedRuntimeBridge
        {
            HasRecoveredCodeOffer = true,
            RecoveredCodePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.RecoveredCodeOffer,
                OwnerGeneration = 41,
                Sequence = 43,
            },
            CodeCandidates = SafeCodeCandidates(30024),
        };
        bridge.RecoveredCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Completed("scripted-recovered-code-terminal")
        );
        bridge.RecoveredCodeParentSteps.Enqueue(
            NetherNativeActionResult.UnknownOutcome("native-start-status-terminal-faulted")
        );
        bridge.RecoveredCodeRefreshSteps.Enqueue(
            NetherNativeActionResult.Completed("scripted-recovered-get-terminal")
        );
        bridge.RecoveredCodeAppliedSnapshot = bridge.CurrentSnapshot with
        {
            Codes = new[]
            {
                new NetherCodeState(30024, NetherCodeFamily.Safe, 1)
                {
                    Category = NetherCodeCategory.ErosionResistance,
                    Rarity = 1,
                },
            },
            CodeHash = "30024:1:1",
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);
        Config.NetherAutoClimbDetailedLogging.Value = true;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            NetherAutoClimbController.Update(); // invoke code
            NetherAutoClimbController.Update(); // child terminal
            NetherAutoClimbController.Update(); // parent unknown -> GET begin
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);

            NetherAutoClimbController.Update(); // GET terminal -> exact code reconcile
            Assert.False(bridge.HasRecoveredCodeOffer);
            Assert.Equal(1, bridge.RecoveredCodeCompletedCount);
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(0, bridge.BeginFloorParentCount);
            Assert.Contains(Logger.Messages, message =>
                message.Contains("event=recovered-code-reconcile")
                && message.Contains("outcome=Applied")
                && message.Contains("action=SelectCode")
                && message.Contains("target=30024")
                && message.Contains("targetAfter=True")
                && message.Contains("reload=2->2"));
            Assert.Contains(Logger.Messages, message =>
                message.Contains("audit=reconcile")
                && message.Contains("beforeCodes=none")
                && message.Contains("afterCodes=30024"));
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Config.NetherAutoClimbDetailedLogging.Value = previousDetailedLogging;
            Logger.Reset();
        }
    }

    [Fact]
    public void Recovered_code_offer_precedes_persisted_lease_route_gate_then_route_remains_blocked()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            HasRecoveredCodeOffer = true,
            RecoveredCodePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.RecoveredCodeOffer,
                OwnerGeneration = 29,
                Sequence = 31,
            },
            CodeCandidates = SafeCodeCandidates(30024),
        };
        bridge.RecoveredCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Completed("scripted-recovered-code-terminal")
        );
        bridge.RecoveredCodeParentSteps.Enqueue(
            NetherNativeActionResult.Completed("scripted-start-status-parent-terminal")
        );
        bridge.RecoveredCodeRefreshSteps.Enqueue(
            NetherNativeActionResult.Completed("scripted-recovered-get-terminal")
        );
        var lease = new RecordingLeaseDriver(needsRecovery: true);
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            NetherAutoClimbController.Update();
            Assert.Equal(NetherActionKind.SelectCode, Assert.Single(bridge.RecoveredCodeActions).Kind);
            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(0, bridge.BeginFloorParentCount);

            NetherAutoClimbController.Update();
            NetherAutoClimbController.Update();
            NetherAutoClimbController.Update();

            Assert.False(bridge.HasRecoveredCodeOffer);
            Assert.Equal(1, bridge.RecoveredCodeCompletedCount);
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(0, bridge.BeginFloorParentCount);

            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.BindingUnavailable, NetherAutoClimbController.PauseReason);
            Assert.Equal(0, bridge.BeginFloorParentCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void F12_hotkey_logs_request_and_enabled_outcome_when_detailed_logging_is_off()
    {
        var bridge = new ScriptedRuntimeBridge();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);
        Config.NetherAutoClimbDetailedLogging.Value = false;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.ObserveHotkeyInput(accepted: true);
            NetherAutoClimbController.ToggleFromHotkey();

            Assert.True(NetherAutoClimbController.IsEnabled);
            Assert.Contains(Logger.Messages, message =>
                message.Contains("event=hotkey-input")
                && message.Contains("key=F12")
                && message.Contains("accepted=True"));
            Assert.Contains(Logger.Messages, message =>
                message.Contains("event=hotkey-dispatch") && message.Contains("key=F12"));
            Assert.Contains(Logger.Messages, message =>
                message.Contains("event=toggle-result") && message.Contains("outcome=enabled"));
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Logger.Reset();
        }
    }

    [Fact]
    public void F12_hotkey_arms_without_mutation_while_floor_runtime_is_registering()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            HasRegisteredFloorSelection = false,
            HasObservedNetherBattleResult = false,
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);
        Config.NetherAutoClimbDetailedLogging.Value = false;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.ToggleFromHotkey();

            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Empty(bridge.Invocations);
            Assert.Contains(Logger.Messages, message =>
                message.Contains("event=toggle-result")
                && message.Contains("outcome=armed")
                && message.Contains("reason=awaiting-nether-runtime"));
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Logger.Reset();
        }
    }

    [Fact]
    public void Repeated_F12_during_registration_gap_enables_once_when_floor_runtime_arrives()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            HasRegisteredFloorSelection = false,
            HasObservedNetherBattleResult = false,
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);
        Config.NetherAutoClimbDetailedLogging.Value = false;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.ToggleFromHotkey();
            NetherAutoClimbController.ToggleFromHotkey();
            NetherAutoClimbController.Update();

            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Empty(bridge.Invocations);

            bridge.HasRegisteredFloorSelection = true;
            NetherAutoClimbController.Update();

            Assert.True(NetherAutoClimbController.IsEnabled);
            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Contains(Logger.Messages, message =>
                message.Contains("event=deferred-toggle")
                && message.Contains("outcome=activated")
                && message.Contains("source=floor-selection"));
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Logger.Reset();
        }
    }

    [Fact]
    public void Deferred_F12_expires_without_mutation_when_no_nether_runtime_appears()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            HasRegisteredFloorSelection = false,
            HasObservedNetherBattleResult = false,
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);
        Config.NetherAutoClimbDetailedLogging.Value = false;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.ToggleFromHotkey();
            for (int poll = 0; poll < 4000; poll++)
                NetherAutoClimbController.Update();

            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Empty(bridge.Invocations);
            Assert.Contains(Logger.Messages, message =>
                message.Contains("event=deferred-toggle")
                && message.Contains("outcome=expired")
                && message.Contains("reason=nether-runtime-timeout"));
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Logger.Reset();
        }
    }

    [Fact]
    public void Deferred_F12_activates_on_battle_result_and_selects_code_before_next()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            HasRegisteredFloorSelection = false,
            HasObservedNetherBattleResult = false,
            AutoCompleteBattleResultContinuation = false,
            BattleResultRebound = false,
            CodeCandidates = SafeCodeCandidates(30024),
            BattleResultCodePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.BattleSettlement,
                OwnerGeneration = 9,
                Sequence = 20,
            },
        };
        bridge.BattleResultCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Pending("scripted-code-confirm-pending")
        );
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);
        Config.NetherAutoClimbDetailedLogging.Value = false;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.ToggleFromHotkey();
            Assert.False(NetherAutoClimbController.IsEnabled);

            bridge.HasObservedNetherBattleResult = true;
            NetherAutoClimbController.Update();

            Assert.True(NetherAutoClimbController.IsEnabled);
            Assert.Equal(
                new[] { NetherActionKind.SelectCode },
                bridge.BattleResultCodeActions.Select(action => action.Kind)
            );
            Assert.Equal(0, bridge.BattleResultNextInvokeCount);
            Assert.Contains(Logger.Messages, message =>
                message.Contains("event=deferred-toggle")
                && message.Contains("outcome=activated")
                && message.Contains("source=battle-result"));
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Logger.Reset();
        }
    }

    [Fact]
    public void F12_prefers_proven_battle_result_owner_over_stale_floor_registration()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            // The real result view can be observed before the preceding floor owner emits
            // its termination callback.  That stale registration must never route behind
            // the foreground result/code popup.
            HasRegisteredFloorSelection = true,
            HasObservedNetherBattleResult = true,
            AutoCompleteBattleResultContinuation = false,
            BattleResultRebound = false,
            CodeCandidates = SafeCodeCandidates(30024),
            BattleResultCodePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.BattleSettlement,
                OwnerGeneration = 9,
                Sequence = 20,
            },
        };
        bridge.BattleResultCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Pending("scripted-code-confirm-pending")
        );
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.ToggleFromHotkey();
            Assert.True(
                NetherAutoClimbController.Phase == NetherAutoClimbPhase.AwaitingBattleResultContinuation,
                NetherAutoClimbController.PauseReason + ":" + NetherAutoClimbController.PauseDetail
            );

            NetherAutoClimbController.Update();

            Assert.Equal(
                new[] { NetherActionKind.SelectCode },
                bridge.BattleResultCodeActions.Select(action => action.Kind)
            );
            Assert.Equal(0, bridge.BattleResultNextInvokeCount);
            Assert.Empty(bridge.Invocations);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Battle_result_poll_diagnostics_emit_transitions_instead_of_every_update()
    {
        bool previousDetailedLogging = Config.NetherAutoClimbDetailedLogging.Value;
        var bridge = new ScriptedRuntimeBridge
        {
            HasRegisteredFloorSelection = false,
            HasObservedNetherBattleResult = true,
            AutoCompleteBattleResultContinuation = false,
            BattleResultRebound = false,
            CodeCandidates = SafeCodeCandidates(30024),
            BattleResultCodePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.BattleSettlement,
                OwnerGeneration = 9,
                Sequence = 20,
            },
        };
        bridge.BattleResultCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Completed("scripted-result-code-complete")
        );
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );
        Config.NetherAutoClimbDetailedLogging.Value = false;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            for (int update = 0; update < 6; update++)
                NetherAutoClimbController.Update();

            Assert.Single(Logger.Messages, message =>
                message.Contains("event=battle-result-code")
                && message.Contains("step=Completed")
                && message.Contains("detail=battle-result-code-completed")
            );
            Assert.Single(Logger.Messages, message =>
                message.Contains("event=battle-result-continuation")
                && message.Contains("detail=scripted-awaiting-floor-rebind")
            );
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Config.NetherAutoClimbDetailedLogging.Value = previousDetailedLogging;
            Logger.Reset();
        }
    }

    [Fact]
    public void Unknown_combat_route_logs_runtime_inputs_and_per_candidate_component_failure()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            RouteSafetyOverride = new NetherRuntimeRouteSafetyData
            {
                FloorBoundsByFloorId = new Dictionary<long, NetherFloorMasterBounds>
                {
                    [3] = new NetherFloorMasterBounds(3, 0, 0, IsKnown: true, Detail: string.Empty),
                },
                ActivePartyHp = new NetherActivePartyHpSafety(false, null, "missing-party-model"),
                ActiveCodeErosion = new NetherActiveCodeErosionProjection
                {
                    ErosionProjectionKnown = true,
                    CodeHash = "nether-codes:none",
                    ErosionEffects = Array.Empty<NetherCodeEffect>(),
                },
                Detail = "runtime-capture-partial",
            },
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);
        Config.NetherAutoClimbDetailedLogging.Value = true;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.ToggleFromHotkey();
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Contains(Logger.Messages, message =>
                message.Contains("audit=route")
                && message.Contains("key=route-inputs:")
                && message.Contains("boundsKnown=1/3")
                && message.Contains("hpKnown=False")
                && message.Contains("hpDetail=missing-party-model")
                && message.Contains("codesKnown=True"));
            Assert.Contains(Logger.Messages, message =>
                message.Contains("audit=route")
                && message.Contains("key=route-candidate:2")
                && message.Contains("nodeId=2")
                && message.Contains("masterId=2")
                && message.Contains("reason=unknown-node")
                && message.Contains("detail=bounds:missing-runtime-node|hp:missing-party-model|codes:known|pro"));
            Assert.Contains(Logger.Messages, message =>
                message.Contains("audit=route")
                && message.Contains("key=route-node:3:")
                && message.Contains("nodeId=3")
                && message.Contains("nodeType=Boss")
                && message.Contains("known=False")
                && message.Contains("hardSafe=False")
                && message.Contains("projectedErosionDelta=unknown")
                && message.Contains("terminalWorstCase=unknown")
                && message.Contains("hp:missing-party-model"));
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Logger.Reset();
        }
    }

    [Fact]
    public void Unknown_event_route_logs_the_native_resolved_master_and_raw_part_fields()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
                NetherSessionStatus.Play,
                floorId: 1,
                gold: 40
            ),
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
        };
        bridge.InteractivePreEntryFactory = (snapshot, settings) =>
        {
            var input = new NetherInteractiveFloorPreEntrySafetyInput(
                NetherFloorNodeType.Event,
                FloorMasterId: 2,
                MapFloorRows: new[] { new NetherFloorMasterBoundsRow(2, 0, 100) },
                EventRows: new[] { new NetherFloorEventMasterRow(42, 2, 0, 10002, 0, 0, 0) { Type = 7 } },
                EventPartRows: new[]
                {
                    new NetherFloorEventPartMasterRow(
                        10002,
                        TargetType1: 99,
                        SelectParameter1: 123456,
                        TargetType2: 8,
                        SelectParameter2: 90,
                        TargetType3: 0,
                        SelectParameter3: 0,
                        ContentType: 165,
                        ContentId: 1,
                        Amount: 40
                    ),
                },
                CurrentErosion: snapshot.ErosionPoint,
                ActiveHpPermille: new[] { 500 },
                CurrentNetherGold: snapshot.NetherGold,
                CurrentTreasureKeys: snapshot.TreasureKeyCount,
                Settings: settings
            )
            {
                FloorExtendId = 42,
            };
            return ScriptedRuntimeBridge.MergeInteractiveCapture(
                snapshot,
                settings,
                2,
                input,
                new NetherInteractiveFloorPreEntrySafety().Evaluate(input)
            );
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);
        Config.NetherAutoClimbDetailedLogging.Value = true;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.ToggleFromHotkey();
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Contains(Logger.Messages, message =>
                message.Contains("audit=interactive")
                && message.Contains("key=preentry-floor:2:")
                && message.Contains("masterId=2")
                && message.Contains("extendId=42")
                && message.Contains("safetyReason=UnknownMasterData"));
            Assert.Contains(Logger.Messages, message =>
                message.Contains("audit=interactive")
                && message.Contains("key=event-master:2:42:")
                && message.Contains("eventId=42")
                && message.Contains("eventType=7")
                && message.Contains("weight=0")
                && message.Contains("part1=10002"));
            Assert.Contains(Logger.Messages, message =>
                message.Contains("audit=interactive")
                && message.Contains("key=event-part:2:42:10002:")
                && message.Contains("target1=99")
                && message.Contains("parameter1=123456")
                && message.Contains("target2=8")
                && message.Contains("parameter2=90")
                && message.Contains("content=165:1:40"));
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Logger.Reset();
        }
    }

    [Fact]
    public void Production_controller_drives_play_popup_battle_sleep_continue_new_segment_and_result()
    {
        var bridge = new ScriptedRuntimeBridge();
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Assert.True(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);

            // The production RouteSafety coordinator chooses the Battle node and stores its
            // immutable projection before the native SelectFloor parent starts.
            NetherAutoClimbController.Update();
            Assert.Equal(
                new[] { NetherActionKind.SelectFloor },
                bridge.Invocations
            );
            Assert.Equal(1, bridge.BeginFloorParentCount);
            Assert.Equal(0, bridge.OwnedPopupInvokeCount);

            Pump(3);
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, NetherAutoClimbController.Phase);
            Assert.Equal(NetherSessionStatus.Battle, bridge.CurrentSnapshot.Status);

            // A clean NetherTop session has no battle-only BottomRight accessor yet.  The
            // first route action must still have happened; the exact accessor appears only
            // when the first battle view exists, before automation starts that battle.
            NetherAutoClimbController.OnBattleSettingsAccessorRegistered();

            // A separate native battle clear plus a fresh read-only snapshot settles battle;
            // lease force/restore happens once for this battle, not at F12 enable time.
            NetherAutoClimbController.Update();
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, NetherAutoClimbController.Phase);
            Assert.Equal(1, lease.AcquireCalls);
            NetherAutoClimbController.Update();
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSettlement, NetherAutoClimbController.Phase);
            NetherAutoClimbController.Update();
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSettlement, NetherAutoClimbController.Phase);
            NetherAutoClimbController.Update();
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(1, lease.RestoreCalls);
            Assert.Equal(NetherSessionStatus.Play, bridge.CurrentSnapshot.Status);

            // Destroying the cleanly restored battle-view owner must unregister it, not pause
            // map automation.  The next battle obtains a new exact owner and a new lease.
            NetherAutoClimbController.OnBattleSettingsAccessorUnregistered();
            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);

            // A second battle uses the same production Controller/lease lifecycle.  It must
            // acquire and restore again instead of relying on a session-global restore bit.
            bridge.CurrentSnapshot = bridge.SecondBattleOrigin;
            NetherAutoClimbController.Update();
            Assert.Equal(2, bridge.Invocations.Count(action => action == NetherActionKind.SelectFloor));
            Pump(3);
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, NetherAutoClimbController.Phase);
            NetherAutoClimbController.OnBattleSettingsAccessorRegistered();
            NetherAutoClimbController.Update();
            NetherAutoClimbController.Update();
            NetherAutoClimbController.Update();
            NetherAutoClimbController.Update();
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(2, lease.AcquireCalls);
            Assert.Equal(2, lease.RestoreCalls);

            // Sleep uses one-ticket non-boost Continue, observes the native parent, waits for
            // teardown/rebind, then performs exactly one GET-only segment reconciliation.
            bridge.CurrentSnapshot = bridge.SleepCheckpoint;
            NetherAutoClimbController.Update();
            Assert.Equal(NetherActionKind.Continue, bridge.Invocations.Last());
            Assert.Equal(1, bridge.ContinuePreflightCount);
            Assert.Equal(1, bridge.ContinueNativeInvokeCount);

            NetherAutoClimbController.Update(); // parent pending
            bridge.ContinueParentCompleted = true;
            NetherAutoClimbController.Update(); // parent terminal
            bridge.FloorOwnerTerminated = true;
            NetherAutoClimbController.Update(); // teardown
            bridge.CurrentRuntimeGeneration = 2;
            bridge.CurrentSnapshot = bridge.NewSegment;
            NetherAutoClimbController.Update(); // rebind
            NetherAutoClimbController.Update(); // GET begin
            NetherAutoClimbController.Update(); // GET terminal + Stable

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(2, bridge.CurrentSnapshot.MapId);
            Assert.Equal(20, bridge.CurrentSnapshot.CurrentFloorId);
            Assert.Equal(1, bridge.ContinueReadOnlyBeginCount);

            bridge.CurrentSnapshot = bridge.ClearResult;
            NetherAutoClimbController.Update();
            Assert.Equal(NetherAutoClimbPhase.AwaitingSceneChange, NetherAutoClimbController.Phase);
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Completed, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.ResultPollCount);
            Assert.Equal(2, bridge.Invocations.Count(action => action == NetherActionKind.SelectFloor));
            Assert.Equal(1, bridge.Invocations.Count(action => action == NetherActionKind.Continue));
            Assert.True(bridge.FloorParentPollCount >= 2);
            Assert.True(bridge.ContinueParentPollCount >= 2);
            Assert.True(
                bridge.Trace.IndexOf("continue-preflight") < bridge.Trace.IndexOf("continue-native-invoke"),
                "native Continue must not precede authoritative carry preflight"
            );
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Target_floor_finish_hands_exact_parent_to_result_without_floor_reconcile()
    {
        int previousMaximumDepth = Config.NetherAutoClimbMaxDepth.Value;
        var bridge = new ScriptedRuntimeBridge();
        bridge.CurrentSnapshot = bridge.SleepCheckpoint with
        {
            FloorLevel = 100,
            FloorIndex = 10,
            MaxFloorLevel = 103,
            MasterMaxFloorLevel = 130,
            ContinuanceFloorLevel = 90,
            ContinuationTarget = null,
            MapHash = "target-floor-100-sleep",
        };
        bridge.ResultFlowSteps.Enqueue(
            NetherNativeActionResult.Started("scripted-result-loading")
        );
        bridge.ResultFlowSteps.Enqueue(
            NetherNativeActionResult.Completed("scripted-result-terminal")
        );
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );
        Config.NetherAutoClimbMaxDepth.Value = 100;

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            NetherAutoClimbController.Update(); // authoritative Sleep -> Finish parent starts.
            Assert.Equal(NetherActionKind.FinishAtCheckpoint, bridge.Invocations.Last());
            Assert.Equal(NetherAutoClimbPhase.ExecutingNativeAction, NetherAutoClimbController.Phase);

            NetherAutoClimbController.Update(); // exact Finish parent is still pending.
            Assert.Equal(NetherAutoClimbPhase.ExecutingNativeAction, NetherAutoClimbController.Phase);

            bridge.FinishParentCompleted = true;
            NetherAutoClimbController.Update(); // exact Finish parent terminal -> Result handoff.
            Assert.Equal(NetherAutoClimbPhase.AwaitingSceneChange, NetherAutoClimbController.Phase);
            Assert.Equal(2, bridge.FinishParentPollCount);

            int floorCapturesAtResultHandoff = bridge.FloorSceneSnapshotCaptureCount;
            bridge.HasRegisteredFloorSelection = false;
            NetherAutoClimbController.OnNetherFloorSelectionTerminated();

            NetherAutoClimbController.Update(); // Result task pending.
            NetherAutoClimbController.Update(); // Result task terminal.

            Assert.Equal(NetherAutoClimbPhase.Completed, NetherAutoClimbController.Phase);
            Assert.Equal(2, bridge.ResultPollCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(0, bridge.GetOnlyPollCount);
            Assert.Equal(floorCapturesAtResultHandoff, bridge.FloorSceneSnapshotCaptureCount);
            Assert.Single(
                bridge.Invocations,
                action => action == NetherActionKind.FinishAtCheckpoint
            );
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Config.NetherAutoClimbMaxDepth.Value = previousMaximumDepth;
        }
    }

    [Fact]
    public void Result_timeout_is_retired_only_after_a_new_entered_floor_zero_run_is_authoritative()
    {
        int previousMaximumDepth = Config.NetherAutoClimbMaxDepth.Value;
        var bridge = new ScriptedRuntimeBridge();
        NetherSnapshot finishedRun = bridge.SleepCheckpoint with
        {
            FloorLevel = 100,
            FloorIndex = 10,
            MaxFloorLevel = 103,
            MasterMaxFloorLevel = 130,
            ContinuanceFloorLevel = 90,
            ContinuationTarget = null,
            TicketCount = 2,
            MapHash = "finished-run-floor-100",
        };
        bridge.CurrentSnapshot = finishedRun;
        bridge.ResultFlowSteps.Enqueue(
            NetherNativeActionResult.BindingUnavailable("native-result-task-timeout")
        );
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );
        Config.NetherAutoClimbMaxDepth.Value = 100;

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update(); // Finish parent starts in generation 1.
            NetherAutoClimbController.Update(); // Exact parent remains pending.
            bridge.FinishParentCompleted = true;
            NetherAutoClimbController.Update(); // Parent terminal -> Result handoff.
            bridge.HasRegisteredFloorSelection = false;
            NetherAutoClimbController.OnNetherFloorSelectionTerminated();
            NetherAutoClimbController.Update(); // Result binding times out; Finish evidence remains.

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.BindingUnavailable, NetherAutoClimbController.PauseReason);
            Assert.Contains("native-result-task-timeout", NetherAutoClimbController.PauseDetail);
            Assert.Single(
                bridge.Invocations,
                action => action == NetherActionKind.FinishAtCheckpoint
            );

            // The player settles Result manually and starts a genuinely new run.  This is the
            // production proof bundle: a strictly newer FloorSelection generation, its matching
            // SubScene.OnEntered, and an authoritative Play snapshot at the pristine floor-0
            // boundary with the exact one-ticket entry cost.
            bridge.CurrentRuntimeGeneration = 2;
            bridge.HasRegisteredFloorSelection = true;
            bridge.FloorSceneEntered = false;
            bridge.FloorSceneHasAuthoritativeSnapshot = true;
            bridge.CurrentSnapshot = bridge.PlayBeforeInteractive with
            {
                Status = NetherSessionStatus.Play,
                CurrentFloorId = 1,
                CurrentNodeId = 1,
                FloorLevel = 0,
                FloorIndex = 1,
                ErosionPoint = 0,
                TicketCount = finishedRun.TicketCount - 1,
                TreasureKeyCount = 0,
                NetherGold = 30,
                Codes = Array.Empty<NetherCodeState>(),
                CodeHash = "nether-codes:none",
                MapHash = "new-run-floor-zero",
            };

            NetherAutoClimbController.Toggle(); // OFF from the nonterminal Result pause.
            Assert.False(NetherAutoClimbController.IsEnabled);
            NetherAutoClimbController.Toggle(); // New controller exists, but OnEntered is absent.
            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.Disabled, NetherAutoClimbController.Phase);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            bridge.FloorSceneEntered = true;
            NetherAutoClimbController.Toggle(); // Explicitly enable on the fully proven new run.

            Assert.True(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(0, bridge.GetOnlyPollCount);
            Assert.Single(
                bridge.Invocations,
                action => action == NetherActionKind.FinishAtCheckpoint
            );
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Config.NetherAutoClimbMaxDepth.Value = previousMaximumDepth;
        }
    }

    [Fact]
    public void Target_floor_finish_adopts_the_existing_start_status_parent()
    {
        int previousMaximumDepth = Config.NetherAutoClimbMaxDepth.Value;
        var bridge = new ScriptedRuntimeBridge();
        NetherSnapshot checkpoint = bridge.SleepCheckpoint with
        {
            FloorLevel = 100,
            FloorIndex = 10,
            MaxFloorLevel = 103,
            MasterMaxFloorLevel = 130,
            ContinuanceFloorLevel = 90,
            ContinuationTarget = null,
            MapHash = "target-floor-100-existing-parent",
        };
        bridge.CurrentSnapshot = checkpoint;
        bridge.RecoveredCheckpointObservation = NetherRecoveredCheckpointObservation.Ready(
            checkpoint,
            "scripted-existing-finish-popup"
        );
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );
        Config.NetherAutoClimbMaxDepth.Value = 100;

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.Equal(1, bridge.RecoveredCheckpointPollCount);
            Assert.Equal(1, bridge.RecoveredCheckpointHandoffCount);
            Assert.Equal(NetherActionKind.FinishAtCheckpoint, bridge.Invocations.Last());
            Assert.Equal(
                NetherAutoClimbPhase.ExecutingNativeAction,
                NetherAutoClimbController.Phase
            );
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Config.NetherAutoClimbMaxDepth.Value = previousMaximumDepth;
        }
    }

    [Fact]
    public void Target_floor_finish_drains_exact_parent_after_floor_owner_teardown()
    {
        int previousMaximumDepth = Config.NetherAutoClimbMaxDepth.Value;
        var bridge = new ScriptedRuntimeBridge();
        bridge.CurrentSnapshot = bridge.SleepCheckpoint with
        {
            FloorLevel = 100,
            FloorIndex = 10,
            MaxFloorLevel = 103,
            MasterMaxFloorLevel = 130,
            ContinuanceFloorLevel = 90,
            ContinuationTarget = null,
            MapHash = "target-floor-100-owner-teardown",
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );
        Config.NetherAutoClimbMaxDepth.Value = 100;

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update(); // Finish parent starts.
            NetherAutoClimbController.Update(); // Finish parent remains pending.

            int floorCapturesBeforeTeardown = bridge.FloorSceneSnapshotCaptureCount;
            bridge.HasRegisteredFloorSelection = false;
            NetherAutoClimbController.OnNetherFloorSelectionTerminated();
            bridge.FinishParentCompleted = true;

            NetherAutoClimbController.Update(); // Poll captured parent despite missing owner.
            Assert.Equal(NetherAutoClimbPhase.AwaitingSceneChange, NetherAutoClimbController.Phase);
            NetherAutoClimbController.Update(); // Result terminal.

            Assert.Equal(NetherAutoClimbPhase.Completed, NetherAutoClimbController.Phase);
            Assert.Equal(2, bridge.FinishParentPollCount);
            Assert.Equal(1, bridge.ResultPollCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(0, bridge.GetOnlyPollCount);
            Assert.Equal(floorCapturesBeforeTeardown, bridge.FloorSceneSnapshotCaptureCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Config.NetherAutoClimbMaxDepth.Value = previousMaximumDepth;
        }
    }

    [Fact]
    public void Production_controller_blocks_fresh_route_when_persisted_lease_needs_exact_recovery()
    {
        var bridge = new ScriptedRuntimeBridge();
        var lease = new RecordingLeaseDriver(needsRecovery: true);
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Empty(bridge.Invocations);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_probes_real_persisted_lease_before_accessor_then_releases_route_after_restore()
    {
        using var leaseHarness = new StartupLeaseHarness();
        Assert.Equal(NetherNativeActionResultKind.Completed, leaseHarness.OriginalLease.AcquireAndForce().Kind);
        Assert.True(File.Exists(leaseHarness.LeaseFilePath));

        var recoveryNative = new StartupLeaseNative(autoEnabled: true, speed: 3);
        NetherBattleSettingsLease recoveredLease = leaseHarness.CreateDetachedLease();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(recoveredLease, retryIntervalUpdates: 1);
        var bridge = new ScriptedRuntimeBridge();
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.True(lifecycle.BlocksRoute);
            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Empty(bridge.Invocations);
            Assert.Equal(0, recoveryNative.WriteCalls);

            leaseHarness.Attach(recoveredLease, recoveryNative);
            NetherAutoClimbController.OnBattleSettingsAccessorRegistered();

            Assert.False(lifecycle.BlocksRoute);
            Assert.False(recoveryNative.AutoEnabled);
            Assert.Equal(1, recoveryNative.Speed);
            Assert.Equal(1, recoveryNative.WriteCalls);
            Assert.False(File.Exists(leaseHarness.LeaseFilePath));

            NetherAutoClimbController.Toggle(); // off from paused enabled state
            NetherAutoClimbController.Toggle(); // user explicitly re-enables after recovery
            NetherAutoClimbController.Update();

            Assert.Single(bridge.Invocations, action => action == NetherActionKind.SelectFloor);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_selects_raw_ordinary_code_offer_without_fake_lane_metadata()
    {
        bool previousDetailedLogging = Config.NetherAutoClimbDetailedLogging.Value;
        var bridge = new ScriptedRuntimeBridge();
        bridge.CurrentSnapshot = bridge.WaitForInteractivePopup;
        bridge.ActivePopup = new NetherRuntimePopupContext { Kind = NetherRuntimePopupKind.CodeOffer };
        bridge.CodeCandidates = new NetherRuntimeCodeCandidatesResult(
            new[]
            {
                NetherCodeRuntimeSemanticMapper.MapCandidate(
                    codeId: 51001,
                    rawCategory: (int)NetherCodeCategory.Technique,
                    effectType: 1,
                    effectParameter1: 100006,
                    effectParameter2: 2,
                    effectParameter3: 0,
                    rarity: 3,
                    power: 0
                ),
            },
            IsMasterComplete: true,
            Detail: string.Empty
        );
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);
        Config.NetherAutoClimbDetailedLogging.Value = true;
        Logger.Reset();

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Contains(NetherActionKind.SelectCode, bridge.Invocations);
            Assert.Contains(Logger.Messages, message =>
                message.Contains("audit=decision")
                && message.Contains("key=code-policy:direct:")
                && message.Contains("decision=Select")
                && message.Contains("selectedCodeId=51001")
                && message.Contains("lane=Auto"));
            string candidateAudit = Logger.Messages.Single(message =>
                message.Contains("audit=decision")
                && message.Contains("key=code-candidate:direct:51001:"));
            Assert.Contains("category=Rush", candidateAudit);
            Assert.Contains("family=Rush", candidateAudit);
            Assert.Contains("rarity=3", candidateAudit);
            Assert.Contains("effectType=NetherAbility", candidateAudit);
            Assert.Contains("abilityAssetId=100006", candidateAudit);
            Assert.Contains("abilityLevel=2", candidateAudit);
            Assert.Contains("coverageKnown=False", candidateAudit);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
            Config.NetherAutoClimbDetailedLogging.Value = previousDetailedLogging;
            Logger.Reset();
        }
    }

    [Fact]
    public void Production_controller_uses_candidate_local_mechanism_evidence_not_displayed_power()
    {
        NetherCodeCandidate unknownDisplayedHigh = NetherCodeRuntimeSemanticMapper.MapCandidate(
            codeId: 51011,
            rawCategory: (int)NetherCodeCategory.Technique,
            effectType: 1,
            effectParameter1: 100006,
            effectParameter2: 1,
            effectParameter3: 0,
            rarity: 5,
            power: 99_999
        );
        NetherCodeCandidate backForceChain = NetherCodeRuntimeSemanticMapper.MapCandidate(
            codeId: 51012,
            rawCategory: (int)NetherCodeCategory.Technique,
            effectType: 1,
            effectParameter1: 100006,
            effectParameter2: 1,
            effectParameter3: 0,
            rarity: 1,
            power: 1
        );
        NetherMechanismValue forceChainValue = NetherMechanismValue.Qualitative(
            NetherMechanismQualitativePriority.BackForceChainHigh,
            "native-force-chain-completion-message"
        );
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = new ScriptedRuntimeBridge().WaitForInteractivePopup,
            ActivePopup = new NetherRuntimePopupContext { Kind = NetherRuntimePopupKind.CodeOffer },
            CodeCandidates = new NetherRuntimeCodeCandidatesResult(
                [unknownDisplayedHigh, backForceChain],
                IsMasterComplete: true,
                Detail: string.Empty
            ),
            CodePolicyEvidenceFactory = (_, _, _) =>
                NetherRuntimeCodePolicyEvidenceResult.Success(new NetherCodePolicyEvidence
                {
                    MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
                    {
                        [unknownDisplayedHigh.CodeId] = new()
                        {
                            IsKnown = false,
                            UnknownReason = "candidate-native-effect-unavailable",
                        },
                        [backForceChain.CodeId] = new() { IsKnown = true },
                    },
                    MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
                    {
                        [unknownDisplayedHigh.CodeId] = NetherMechanismValue.Missing(
                            "candidate-native-effect-unavailable"
                        ),
                        [backForceChain.CodeId] = forceChainValue,
                    },
                    EquipmentMutationValuesByKey = new Dictionary<
                        NetherCodeMutationKey,
                        NetherCodeEquipmentMutationEvidence
                    >
                    {
                        [new NetherCodeMutationKey(backForceChain.CodeId, 0)] = new(
                            backForceChain.CodeId,
                            RemoveCodeId: 0,
                            new NetherNativePortfolioComparisonInput([], [], BossDurationSeconds: 1),
                            forceChainValue
                        )
                        {
                            CombatTier = NetherEquipmentCombatTier.BackForceChain,
                            Survival = NetherSurvivalRepairEvidence.Known(false, false),
                            MechanismPortfolio = NetherMechanismPortfolioComparisonEvidence.Known(
                                [],
                                [new NetherMechanismPortfolioEntry(
                                    backForceChain.CodeId,
                                    forceChainValue
                                )]
                            ),
                        },
                    },
                }),
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            NetherPlannedAction selected = Assert.Single(
                bridge.NativeActions,
                action => action.Kind == NetherActionKind.SelectCode
            );
            Assert.Equal(backForceChain.CodeId, selected.CodeId);
            Assert.NotEqual(unknownDisplayedHigh.CodeId, selected.CodeId);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_reconciles_owned_event_floor_as_one_exact_parent_transaction()
    {
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10);
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10);
        NetherSnapshot afterEvent = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 2, gold: 11);
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = afterEvent,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new[] { new NetherEffect(NetherEffectKind.NetherGoldGain, 1) }),
                },
            },
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) => ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            NetherAutoClimbController.Update(); // Play → native SelectFloor parent.
            Assert.True(
                bridge.Invocations.SequenceEqual(new[] { NetherActionKind.SelectFloor }),
                "phase=" + NetherAutoClimbController.Phase
                    + " pause=" + NetherAutoClimbController.PauseReason
                    + " invocations=" + string.Join(",", bridge.Invocations)
            );
            NetherAutoClimbController.Update(); // owned Event option; parent remains pending.
            Assert.Single(bridge.OwnedPopupActions);
            Assert.Equal(NetherActionKind.SelectEventOption, bridge.OwnedPopupActions[0].Kind);
            NetherAutoClimbController.Update(); // parent terminal → exactly one GET reconcile.
            NetherAutoClimbController.Update();
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
            Assert.Single(bridge.Invocations, action => action == NetherActionKind.SelectFloor);
            Assert.Single(bridge.OwnedPopupActions);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_derives_same_branch_gold_and_key_thresholds_without_manual_budget_injection()
    {
        NetherSnapshot routeStart = ScriptedRuntimeBridge.ProcurementRouteSnapshot(NetherSessionStatus.Play);
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            BindRouteSafetyHpToCurrentSnapshot = true,
            VisibleMap = ScriptedRuntimeBridge.ProcurementVisibleMap(routeStart.Floors),
            InteractivePreEntryFactory = ScriptedRuntimeBridge.ProcurementInteractivePreEntry,
        };
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            CanonicalRewardTiers =
            [new NetherCanonicalRewardTierProviderEvidence(4011, NetherCanonicalRewardTier.GoldRankFive, 91)],
            ShopKeyIdentities =
            [new NetherShopKeyProviderEvidence(3001, 166, 3001, 1, 7001)],
        };
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            new NetherBattleSettingsLeaseControllerLifecycle(new RecordingLeaseDriver(), retryIntervalUpdates: 1)
        );

        try
        {
            NetherAutoClimbController.Initialize(_ =>
                new NetherRuntimeTypedSemanticProviderScope(routeStart.Fingerprint, provider));
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.True(bridge.TypedSemanticProviderRegistrationCount > 0);

            NetherInteractiveEventOptionKey goldKey = new(100, 1001, 1);
            NetherInteractiveEventOptionKey keyKey = new(100, 1002, 2);
            Assert.Equal(
                new NetherEventProcurementBudget(200, 0),
                bridge.CaptureRouteOwnedEventProcurementCommitments()[goldKey]
            );
            Assert.Equal(
                new NetherEventProcurementBudget(0, 1),
                bridge.CaptureRouteOwnedEventProcurementCommitments()[keyKey]
            );
            Assert.Contains(
                bridge.BoundEventProcurementCommitments,
                pair => pair.Key == goldKey && pair.Value.GoldMinimum == 200
            );
            Assert.True(bridge.RankFiveKeyProcurementBindCount >= 2);
            Assert.NotNull(bridge.BoundRankFiveKeyProcurement);

            NetherInteractiveFloorPreEntrySafetyResult unsafeSpend =
                new NetherInteractiveFloorPreEntrySafety().Evaluate(
                    ScriptedRuntimeBridge.ProcurementSpendInput(
                        routeStart,
                        goldKey,
                        bridge.CaptureRouteOwnedEventProcurementCommitments()
                    )
                );
            Assert.False(unsafeSpend.IsSafe);
            Assert.Contains("event-committed-budget-would-break", unsafeSpend.Detail);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_recaptures_and_keeps_the_selected_recovery_branch_proof()
    {
        NetherSnapshot routeStart = ScriptedRuntimeBridge.RecoveryProofRouteSnapshot(NetherSessionStatus.Play);
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            BindRouteSafetyHpToCurrentSnapshot = true,
            InteractivePreEntryFactory = ScriptedRuntimeBridge.RecoveryProofInteractivePreEntry,
        };
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            new NetherBattleSettingsLeaseControllerLifecycle(new RecordingLeaseDriver(), retryIntervalUpdates: 1)
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.ExecutingNativeAction, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.None, NetherAutoClimbController.PauseReason);
            Assert.True(bridge.InteractivePreEntryCaptureCount >= 3);
            Assert.True(bridge.RecoveryBranchSafetyBindCount >= 2);
            Assert.NotEmpty(bridge.BoundRecoveryBranchSafetyByPartId);
            Assert.Contains(
                bridge.BoundRecoveryBranchSafetyByPartId.Values,
                proof => proof.IsKnown && proof.IsCompleteVisibleBranch && proof.IsNextVisibleBranchSafe
            );
            Assert.Single(bridge.Invocations, action => action == NetherActionKind.SelectFloor);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_pauses_when_final_recovery_proof_is_missing_from_the_recapture()
    {
        NetherSnapshot routeStart = ScriptedRuntimeBridge.RecoveryProofRouteSnapshot(NetherSessionStatus.Play);
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            BindRouteSafetyHpToCurrentSnapshot = true,
            InteractivePreEntryFactory = ScriptedRuntimeBridge.RecoveryProofInteractivePreEntry,
            DropBoundRecoveryProofOnCaptureNumber = 3,
        };
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            new NetherBattleSettingsLeaseControllerLifecycle(new RecordingLeaseDriver(), retryIntervalUpdates: 1)
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.BindingUnavailable, NetherAutoClimbController.PauseReason);
            Assert.Contains("route-final-proof-handoff-mismatch", NetherAutoClimbController.PauseDetail);
            Assert.Contains("selected-recovery-proof-absent-or-mismatched", NetherAutoClimbController.PauseDetail);
            Assert.Empty(bridge.Invocations);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_rejects_procurement_proof_from_an_alternate_safe_branch()
    {
        NetherSnapshot routeStart = ScriptedRuntimeBridge.ProcurementAlternateBranchSnapshot(NetherSessionStatus.Play);
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            BindRouteSafetyHpToCurrentSnapshot = true,
            VisibleMap = ScriptedRuntimeBridge.ProcurementAlternateVisibleMap(routeStart.Floors),
            InteractivePreEntryFactory = ScriptedRuntimeBridge.ProcurementAlternateInteractivePreEntry,
        };
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            new NetherBattleSettingsLeaseControllerLifecycle(new RecordingLeaseDriver(), retryIntervalUpdates: 1)
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.Empty(bridge.CaptureRouteOwnedEventProcurementCommitments());
            Assert.DoesNotContain(
                bridge.BoundEventProcurementCommitments.Keys,
                key => key.EventId == 100 && key.EventPartId == 1001
            );
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_appends_event_then_code_popup_under_one_parent_and_one_get()
    {
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10);
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10);
        NetherSnapshot afterEvent = popupWait with { NetherGold = 15, MapHash = "event-code-wait" };
        NetherSnapshot afterCode = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 2, gold: 15) with
        {
            Codes = new[]
            {
                NetherCodeRuntimeSemanticMapper.MapState(
                    codeId: 30024,
                    rawCategory: (int)NetherCodeCategory.ErosionResistance,
                    effectType: 1,
                    effectParameter1: 100006,
                    effectParameter2: 1,
                    effectParameter3: 0,
                    rarity: 1,
                    power: 0,
                    possessionAmount: 1
                ),
            },
            CodeHash = "code:30024",
            MapHash = "event-code-play",
        };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = afterEvent,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new NetherEffect[]
                    {
                        new NetherEffect(NetherEffectKind.NetherGoldGain, 5),
                        new NetherEffect(NetherEffectKind.AbyssCodeOffer, 1),
                    }),
                },
            },
            CodeCandidates = new NetherRuntimeCodeCandidatesResult(
                new[]
                {
                    NetherCodeRuntimeSemanticMapper.MapCandidate(
                        codeId: 30024,
                        rawCategory: (int)NetherCodeCategory.ErosionResistance,
                        effectType: 1,
                        effectParameter1: 100006,
                        effectParameter2: 1,
                        effectParameter3: 0,
                        rarity: 1,
                        power: 0
                    ),
                },
                IsMasterComplete: true,
                Detail: string.Empty
            ),
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) => ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
        };
        bridge.EnqueueOwnedPopup(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 2,
            },
            afterCode
        );
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            NetherAutoClimbController.Update(); // SelectFloor parent.
            NetherAutoClimbController.Update(); // Event child, then its CodeOffer is live.
            Assert.Equal(new[] { NetherActionKind.SelectEventOption }, bridge.OwnedPopupActions.Select(action => action.Kind));
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            NetherAutoClimbController.Update(); // Code child; still the original parent.
            Assert.Equal(
                new[] { NetherActionKind.SelectEventOption, NetherActionKind.SelectCode },
                bridge.OwnedPopupActions.Select(action => action.Kind)
            );
            Assert.Equal(1, bridge.Invocations.Count(action => action == NetherActionKind.SelectFloor));
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            NetherAutoClimbController.Update(); // Only now may the native parent terminal.
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            NetherAutoClimbController.Update(); // one GET begin
            NetherAutoClimbController.Update(); // one GET terminal

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
            Assert.Equal(2, bridge.OwnedPopupInvokeCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_waits_for_registered_code_offer_native_model_before_selecting()
    {
        var bridge = new ScriptedRuntimeBridge();
        bridge.CurrentSnapshot = bridge.WaitForInteractivePopup;
        bridge.ActivePopupResultOverride = NetherRuntimePopupResult.Pending(
            PendingActiveCodeOffer(sequence: 1617),
            "code-offer-model-not-ready"
        );
        bridge.CodeCandidates = FamilyCodeCandidates(51001, NetherCodeCategory.Rush);
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Empty(bridge.Invocations);

            bridge.ActivePopupResultOverride = null;
            bridge.ActivePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
            };
            NetherAutoClimbController.Update();

            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(new[] { NetherActionKind.SelectCode }, bridge.Invocations);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_does_not_route_behind_registered_code_offer_whose_model_is_pending()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            ActivePopupResultOverride = NetherRuntimePopupResult.Pending(
                PendingActiveCodeOffer(sequence: 1660),
                "code-offer-model-not-ready"
            ),
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Empty(bridge.Invocations);

            bridge.ActivePopupResultOverride = NetherRuntimePopupResult.Failure("missing-active-native-popup");
            NetherAutoClimbController.Update();

            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(new[] { NetherActionKind.SelectFloor }, bridge.Invocations);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_pauses_when_registered_code_offer_native_model_never_becomes_ready()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = new ScriptedRuntimeBridge().WaitForInteractivePopup,
            ActivePopupResultOverride = NetherRuntimePopupResult.Pending(
                PendingActiveCodeOffer(sequence: 1700),
                "code-offer-model-not-ready"
            ),
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(601);

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.BindingUnavailable, NetherAutoClimbController.PauseReason);
            Assert.Contains(
                "active-code-popup-readiness-timeout",
                NetherAutoClimbController.PauseDetail
            );
            Assert.Empty(bridge.Invocations);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Pending_active_popup_budget_is_cleared_when_f12_is_turned_off()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = new ScriptedRuntimeBridge().WaitForInteractivePopup,
            ActivePopupResultOverride = NetherRuntimePopupResult.Pending(
                PendingActiveCodeOffer(sequence: 1733),
                "code-offer-model-not-ready"
            ),
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(599);
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);

            NetherAutoClimbController.Toggle();
            Assert.False(NetherAutoClimbController.IsEnabled);
            NetherAutoClimbController.Toggle();
            Assert.True(NetherAutoClimbController.IsEnabled);
            Pump(3);

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Empty(bridge.Invocations);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Pending_active_popup_budget_is_cleared_when_floor_scene_owner_terminates()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = new ScriptedRuntimeBridge().WaitForInteractivePopup,
            ActivePopupResultOverride = NetherRuntimePopupResult.Pending(
                PendingActiveCodeOffer(sequence: 1734),
                "code-offer-model-not-ready"
            ),
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(599);
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);

            bridge.HasRegisteredFloorSelection = false;
            NetherAutoClimbController.OnNetherFloorSelectionTerminated();
            bridge.HasRegisteredFloorSelection = true;
            Pump(3);

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Empty(bridge.Invocations);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_reconcile_does_not_repeat_get_when_scene_changes_during_refresh()
    {
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
            NetherSessionStatus.Play,
            floorId: 1,
            gold: 10
        );
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
            NetherSessionStatus.Wait,
            floorId: 2,
            gold: 10
        );
        NetherSnapshot afterEvent = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
            NetherSessionStatus.Play,
            floorId: 2,
            gold: 11
        );
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = afterEvent,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(
                        1,
                        new[] { new NetherEffect(NetherEffectKind.NetherGoldGain, 1) }
                    ),
                },
            },
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) =>
                ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
            DropFloorSceneReadinessOnNextGetPoll = true,
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update(); // SelectFloor parent.
            NetherAutoClimbController.Update(); // Event child.
            NetherAutoClimbController.Update(); // Parent terminal.
            NetherAutoClimbController.Update(); // GET begin.
            NetherAutoClimbController.Update(); // GET terminal; OnEntered proof disappears.

            Assert.Equal(NetherAutoClimbPhase.Reconciling, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);

            NetherAutoClimbController.Update(); // Still waiting; never issue a second GET.
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);

            bridge.FloorSceneEntered = true;
            NetherAutoClimbController.Update();

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_selects_battle_result_code_before_clicking_next_once()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            AutoCompleteBattleResultContinuation = false,
            BattleResultRebound = false,
            BattleResultReboundSnapshot = null,
            BattleResultReboundSceneEntered = false,
            CodeCandidates = SafeCodeCandidates(30024),
            BattleResultCodePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.BattleSettlement,
                OwnerGeneration = 9,
                Sequence = 20,
            },
        };
        bridge.BattleResultCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Pending("scripted-code-confirm-pending")
        );
        bridge.BattleResultCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Completed("scripted-code-confirm-terminal")
        );
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();
            Pump(3);
            NetherAutoClimbController.OnBattleSettingsAccessorRegistered();

            NetherAutoClimbController.Update(); // acquire battle settings
            NetherAutoClimbController.Update(); // observe clear
            NetherAutoClimbController.Update(); // GET begin
            NetherAutoClimbController.Update(); // GET terminal, result handoff starts

            Assert.Equal(
                NetherAutoClimbPhase.AwaitingBattleResultContinuation,
                NetherAutoClimbController.Phase
            );
            Assert.Equal(0, bridge.BattleResultNextInvokeCount);
            Assert.Equal(
                new[] { NetherActionKind.SelectCode },
                bridge.BattleResultCodeActions.Select(action => action.Kind)
            );
            Assert.False(bridge.HasRegisteredFloorSelection);

            NetherAutoClimbController.Update(); // exact code confirmation is still pending
            Assert.Equal(0, bridge.BattleResultNextInvokeCount);
            NetherAutoClimbController.Update(); // exact code confirmation reaches terminal; Next may now run
            Assert.Equal(1, bridge.BattleResultNextInvokeCount);

            bridge.BattleResultReboundSnapshot = bridge.AfterBattle;
            bridge.BattleResultRebound = true;

            NetherAutoClimbController.Update(); // Play snapshot exists before SubScene.OnEntered
            Assert.Equal(
                NetherAutoClimbPhase.AwaitingBattleResultContinuation,
                NetherAutoClimbController.Phase
            );

            bridge.BattleResultReboundSceneEntered = true;
            NetherAutoClimbController.Update(); // exact FloorSelection scene has now entered
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.True(bridge.HasRegisteredFloorSelection);
            Assert.Single(bridge.BattleResultCodeActions);
            Assert.Equal(1, bridge.BattleResultNextInvokeCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_settles_segment_boss_to_sleep_before_selecting_result_code()
    {
        var bridge = new ScriptedRuntimeBridge();
        NetherSnapshot origin = bridge.PlayBeforeInteractive with
        {
            Floors = new[]
            {
                new NetherFloorNode(1, 1, 1, NetherFloorNodeType.Recovery)
                {
                    NodeId = 1,
                    IsUnlocked = true,
                },
                new NetherFloorNode(2, 2, 2, NetherFloorNodeType.Boss)
                {
                    NodeId = 2,
                    IsUnlocked = true,
                    PreviousFloorIds = new[] { 1L },
                },
            },
            CurrentNodeId = 1,
            MapHash = "boss-origin",
        };
        NetherSnapshot battle = origin with
        {
            Status = NetherSessionStatus.Battle,
            CurrentFloorId = 2,
            CurrentNodeId = 2,
            FloorLevel = 2,
            FloorIndex = 2,
            MapHash = "boss-battle",
        };
        NetherSnapshot sleep = battle with
        {
            Status = NetherSessionStatus.Sleep,
            ErosionPoint = battle.ErosionPoint + 5,
            ContinuationTarget = new NetherContinuationTarget(2, 20, 2),
            MapHash = "boss-settled-sleep",
        };
        bridge.CurrentSnapshot = origin;
        bridge.FloorSelectionDispatchSnapshot = battle;
        bridge.BattleSettlementSnapshotOverride = sleep;
        bridge.AutoCompleteBattleResultContinuation = false;
        bridge.BattleResultRebound = false;
        bridge.CodeCandidates = SafeCodeCandidates(30024);
        bridge.BattleResultCodePopup = new NetherRuntimePopupContext
        {
            Kind = NetherRuntimePopupKind.CodeOffer,
            OwnerAction = NetherActionKind.BattleSettlement,
            OwnerGeneration = 9,
            Sequence = 20,
        };
        bridge.BattleResultCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Pending("scripted-boss-code-confirm-pending")
        );
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();
            Pump(3);
            NetherAutoClimbController.OnBattleSettingsAccessorRegistered();

            NetherAutoClimbController.Update(); // acquire battle settings
            NetherAutoClimbController.Update(); // observe Boss clear
            NetherAutoClimbController.Update(); // GET begin
            NetherAutoClimbController.Update(); // GET terminal, Sleep settles, code is selected

            Assert.True(
                NetherAutoClimbController.Phase == NetherAutoClimbPhase.AwaitingBattleResultContinuation,
                NetherAutoClimbController.PauseReason + ":" + NetherAutoClimbController.PauseDetail
            );
            Assert.Equal(NetherSessionStatus.Sleep, bridge.CurrentSnapshot.Status);
            Assert.Equal(
                new[] { NetherActionKind.SelectCode },
                bridge.BattleResultCodeActions.Select(action => action.Kind)
            );
            Assert.Equal(0, bridge.BattleResultNextInvokeCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Battle_view_destroy_restores_forced_settings_before_unbind_and_keeps_code_settlement_running()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            AutoCompleteBattleResultContinuation = false,
            BattleResultRebound = false,
            CodeCandidates = SafeCodeCandidates(30024),
            BattleResultCodePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.BattleSettlement,
                OwnerGeneration = 9,
                Sequence = 20,
            },
        };
        bridge.BattleResultCodeNativeSteps.Enqueue(
            NetherBattleResultCodeNativeStep.Pending("scripted-code-confirm-pending")
        );
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();
            Pump(3);
            NetherAutoClimbController.OnBattleSettingsAccessorRegistered();
            NetherAutoClimbController.Update(); // force Auto/highest speed

            Assert.Equal(NetherBattleSettingsLeasePhase.Forced, lease.Phase);

            // This is the live order: BottomRightView.OnDestroy prefix can still use the
            // accessor, then the postfix unbinds it before the result/code UI appears.
            NetherAutoClimbController.OnBattleSettingsAccessorDestroying();
            NetherAutoClimbController.OnBattleSettingsAccessorUnregistered();

            Assert.Equal(NetherBattleSettingsLeasePhase.Restored, lease.Phase);
            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);

            NetherAutoClimbController.Update(); // observe clear
            NetherAutoClimbController.Update(); // GET begin
            NetherAutoClimbController.Update(); // GET terminal and invoke code selection

            Assert.Equal(
                NetherAutoClimbPhase.AwaitingBattleResultContinuation,
                NetherAutoClimbController.Phase
            );
            Assert.Equal(
                new[] { NetherActionKind.SelectCode },
                bridge.BattleResultCodeActions.Select(action => action.Kind)
            );
            Assert.Equal(1, lease.RestoreCalls);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void F12_on_confirmed_nether_battle_result_resumes_instead_of_rejecting_missing_floor_owner()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            HasRegisteredFloorSelection = false,
            HasObservedNetherBattleResult = true,
            AutoCompleteBattleResultContinuation = false,
            BattleResultRebound = false,
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            Assert.True(NetherAutoClimbController.IsEnabled);
            Assert.Equal(
                NetherAutoClimbPhase.AwaitingBattleResultContinuation,
                NetherAutoClimbController.Phase
            );

            NetherAutoClimbController.Update();
            Assert.Equal(1, bridge.BattleResultNextInvokeCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_survives_real_combat_scene_order_before_StartQuest_registration()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            DelayBattleSnapshotUntilStartTerminal = true,
            HoldBattleOpen = true,
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update(); // native SelectFloor
            Assert.Equal(new[] { NetherActionKind.SelectFloor }, bridge.Invocations);

            NetherAutoClimbController.Update(); // native floor parent terminal / scene change scheduled
            Assert.Equal(
                NetherAutoClimbPhase.AwaitingBattleSceneHandoff,
                NetherAutoClimbController.Phase
            );

            bridge.HasRegisteredFloorSelection = false;
            NetherAutoClimbController.OnNetherFloorSelectionTerminated();
            Assert.Equal(0, lease.RestoreCalls);

            NetherAutoClimbController.Update(); // StartQuest not registered yet
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSceneHandoff, NetherAutoClimbController.Phase);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);

            bridge.BattleStartRegistered = true;
            NetherAutoClimbController.Update(); // exact StartQuest exists but remains Pending
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSceneHandoff, NetherAutoClimbController.Phase);

            bridge.BattleStartCompleted = true;
            NetherAutoClimbController.Update(); // StartQuest terminal -> GET begin
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSceneHandoff, NetherAutoClimbController.Phase);

            NetherAutoClimbController.Update(); // GET terminal -> authoritative Battle -> settlement action
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, NetherAutoClimbController.Phase);
            Assert.Equal(NetherSessionStatus.Battle, bridge.CurrentSnapshot.Status);
            Assert.False(bridge.HasRegisteredFloorSelection);
            Assert.Equal(0, lease.AcquireCalls);

            NetherAutoClimbController.Update(); // clean battle waits for BottomRight accessor
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, NetherAutoClimbController.Phase);
            Assert.Equal(0, lease.AcquireCalls);
            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);

            NetherAutoClimbController.OnBattleSettingsAccessorRegistered();
            NetherAutoClimbController.Update();
            Assert.Equal(1, lease.AcquireCalls);
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, NetherAutoClimbController.Phase);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Noncombat_floor_owner_teardown_releases_parent_before_reconcile_and_next_route()
    {
        NetherSnapshot before = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Play,
            NetherFloorNodeType.Shop,
            floorId: 1,
            gold: 0
        );
        NetherSnapshot applied = before with
        {
            CurrentFloorId = 2,
            CurrentNodeId = 2,
            FloorLevel = 2,
            FloorIndex = 2,
            MapHash = "noncombat-owner-teardown-applied",
        };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = before,
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) =>
                ScriptedRuntimeBridge.OwnedInteractivePreEntry(
                    snapshot,
                    settings,
                    NetherFloorNodeType.Shop,
                    null
                ),
            RequireExplicitFloorParentTerminal = true,
        };
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(
            bridge,
            lifecycle
        );

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update(); // starts the first noncombat SelectFloor parent
            Assert.Equal(1, bridge.BeginFloorParentCount);
            Assert.Single(bridge.Invocations, action => action == NetherActionKind.SelectFloor);

            // Fresh native evidence shows that FloorSelection owns both the exact ISubService
            // lifetime and the async sequence started by OnFloorClickedEventAsync. Once that
            // owner terminates, the original parent can no longer be polled; wait for a fresh
            // entered owner and reconcile its server snapshot rather than pausing as NotInNether.
            bridge.HasRegisteredFloorSelection = false;
            NetherAutoClimbController.OnNetherFloorSelectionTerminated();
            NetherAutoClimbController.Update();
            Assert.True(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.Reconciling, NetherAutoClimbController.Phase);
            Assert.NotEqual(NetherPauseReason.NotInNether, NetherAutoClimbController.PauseReason);

            bridge.HasRegisteredFloorSelection = true;
            bridge.CurrentRuntimeGeneration = 2;
            bridge.CurrentSnapshot = applied;
            Pump(2); // one GET-only reconciliation reaches the fresh stable boundary.
            Assert.True(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);

            // The next route begins a new parent instead of retaining the terminated owner.
            NetherAutoClimbController.Update();
            Assert.Equal(NetherAutoClimbPhase.ExecutingNativeAction, NetherAutoClimbController.Phase);
            Assert.Equal(2, bridge.BeginFloorParentCount);
            Assert.Equal(2, bridge.Invocations.Count(action => action == NetherActionKind.SelectFloor));
            Assert.NotEqual(NetherPauseReason.BindingUnavailable, NetherAutoClimbController.PauseReason);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_keeps_combat_parent_when_floor_owner_dies_before_parent_poll()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            DelayBattleSnapshotUntilStartTerminal = true,
            HoldBattleOpen = true,
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update(); // SelectFloor parent starts.

            bridge.HasRegisteredFloorSelection = false;
            NetherAutoClimbController.OnNetherFloorSelectionTerminated();
            Assert.Equal(0, lease.RestoreCalls);

            NetherAutoClimbController.Update(); // Captured parent reaches terminal after owner teardown.
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSceneHandoff, NetherAutoClimbController.Phase);
            Assert.NotEqual(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);

            bridge.BattleStartRegistered = true;
            bridge.BattleStartCompleted = true;
            NetherAutoClimbController.Update(); // StartQuest terminal -> GET starts.
            NetherAutoClimbController.Update(); // GET terminal -> authoritative Battle.

            Assert.Equal(NetherAutoClimbPhase.AwaitingBattle, NetherAutoClimbController.Phase);
            Assert.Equal(NetherSessionStatus.Battle, bridge.CurrentSnapshot.Status);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(0, lease.AcquireCalls);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_drains_battle_ingress_after_F12_off_without_forcing_settings()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            DelayBattleSnapshotUntilStartTerminal = true,
            HoldBattleOpen = true,
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update(); // SelectFloor starts.
            NetherAutoClimbController.Update(); // Parent terminal -> battle handoff.
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSceneHandoff, NetherAutoClimbController.Phase);

            NetherAutoClimbController.Toggle(); // OFF preserves the exact in-flight evidence.
            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.AwaitingBattleSceneHandoff, NetherAutoClimbController.Phase);

            bridge.HasRegisteredFloorSelection = false;
            NetherAutoClimbController.OnNetherFloorSelectionTerminated();
            bridge.BattleStartRegistered = true;
            bridge.BattleStartCompleted = true;
            NetherAutoClimbController.Update(); // StartQuest terminal -> GET starts.
            NetherAutoClimbController.Update(); // GET terminal -> safe Disabled boundary.

            Assert.Equal(NetherAutoClimbPhase.Disabled, NetherAutoClimbController.Phase);
            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(0, lease.AcquireCalls);
            Assert.Equal(1, lease.RestoreCalls); // F12-off performs its idempotent restore boundary.
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_rejects_unproven_default_transform_before_parent_mutation()
    {
        NetherCodeState beforeRisk = NetherCodeRuntimeSemanticMapper.MapState(
            40024,
            (int)NetherCodeCategory.ErosionEnhancement,
            effectType: 1,
            effectParameter1: 100006,
            effectParameter2: 1,
            effectParameter3: 0,
            rarity: 1,
            power: 0,
            possessionAmount: 1
        );
        NetherCodeState transformed = NetherCodeRuntimeSemanticMapper.MapState(
            51001,
            (int)NetherCodeCategory.Technique,
            effectType: 1,
            effectParameter1: 100006,
            effectParameter2: 2,
            effectParameter3: 0,
            rarity: 2,
            power: 0,
            possessionAmount: 1
        );
        NetherCodeState selected = NetherCodeRuntimeSemanticMapper.MapState(
            30024,
            (int)NetherCodeCategory.ErosionResistance,
            effectType: 1,
            effectParameter1: 100006,
            effectParameter2: 1,
            effectParameter3: 0,
            rarity: 1,
            power: 0,
            possessionAmount: 1
        );
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
            NetherSessionStatus.Play,
            floorId: 1,
            gold: 10
        ) with { Codes = new[] { beforeRisk }, CodeHash = "codes:40024" };
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
            NetherSessionStatus.Wait,
            floorId: 2,
            gold: 10
        ) with { Codes = new[] { beforeRisk }, CodeHash = "codes:40024" };
        NetherSnapshot afterEvent = popupWait with { NetherGold = 15, MapHash = "event-transform-wait" };
        NetherSnapshot afterTransform = afterEvent with
        {
            Codes = new[] { transformed },
            CodeHash = "codes:51001",
            MapHash = "event-transform-complete-wait",
        };
        NetherSnapshot afterOffer = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
            NetherSessionStatus.Play,
            floorId: 2,
            gold: 15
        ) with
        {
            Codes = new[] { transformed, selected },
            CodeHash = "codes:51001|30024",
            MapHash = "event-transform-offer-play",
        };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = afterEvent,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new NetherEffect[]
                    {
                        new(NetherEffectKind.NetherGoldGain, 5),
                        new(NetherEffectKind.AbyssCodeTransform, 0),
                        new(NetherEffectKind.AbyssCodeOffer, 1),
                    }),
                },
            },
            CodeCandidates = SafeCodeCandidates(30024),
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) =>
            {
                var input = new NetherInteractiveFloorPreEntrySafetyInput(
                    NetherFloorNodeType.Event,
                    FloorMasterId: 2,
                    MapFloorRows: new[] { new NetherFloorMasterBoundsRow(2, 0, 0) },
                    EventRows: new[] { new NetherFloorEventMasterRow(42, 2, 1, 1001, 0, 0, 0) },
                    EventPartRows: new[]
                    {
                        new NetherFloorEventPartMasterRow(
                            1001,
                            TargetType1: 7,
                            SelectParameter1: 0,
                            TargetType2: 0,
                            SelectParameter2: 0,
                            TargetType3: 0,
                            SelectParameter3: 0,
                            ContentType: 160,
                            ContentId: 0,
                            Amount: 1
                        ),
                    },
                    CurrentErosion: snapshot.ErosionPoint,
                    ActiveHpPermille: new[] { 500 },
                    CurrentNetherGold: snapshot.NetherGold,
                    CurrentTreasureKeys: snapshot.TreasureKeyCount,
                    Settings: settings
                )
                {
                    CurrentCodes = snapshot.Codes,
                    CodeCapacity = snapshot.CodeCapacity,
                };
                NetherInteractiveFloorPreEntrySafetyResult safety =
                    new NetherInteractiveFloorPreEntrySafety().Evaluate(input);
                return ScriptedRuntimeBridge.MergeInteractiveCapture(
                    snapshot,
                    settings,
                    2,
                    input,
                    safety
                );
            },
        };
        bridge.EnqueueOwnedPopup(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeTransform,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 2,
            },
            afterTransform
        );
        bridge.EnqueueOwnedPopup(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 3,
            },
            afterOffer
        );
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(10);

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.UnknownMasterData, NetherAutoClimbController.PauseReason);
            Assert.Empty(bridge.OwnedPopupActions);
            Assert.Equal(0, bridge.CodeTransformInvokeCount);
            Assert.Equal(0, bridge.CodeTransformConfirmCount);
            Assert.Equal(0, bridge.CodeTransformCompleteCount);
            Assert.Equal(0, bridge.CodeTransformTaskPollCount);
            Assert.Equal(0, bridge.FloorParentTerminalCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(0, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_never_enters_faultable_transform_without_eligibility_proof()
    {
        NetherCodeState beforeRisk = NetherCodeRuntimeSemanticMapper.MapState(
            40024,
            (int)NetherCodeCategory.ErosionEnhancement,
            effectType: 1,
            effectParameter1: 100006,
            effectParameter2: 1,
            effectParameter3: 0,
            rarity: 1,
            power: 0,
            possessionAmount: 1
        );
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
            NetherSessionStatus.Play,
            floorId: 1,
            gold: 10
        ) with { Codes = new[] { beforeRisk }, CodeHash = "codes:40024" };
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
            NetherSessionStatus.Wait,
            floorId: 2,
            gold: 10
        ) with { Codes = new[] { beforeRisk }, CodeHash = "codes:40024" };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = popupWait with { MapHash = "event-transform-fault-wait" },
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new NetherEffect[]
                    {
                        new(NetherEffectKind.AbyssCodeTransform, 0),
                    }),
                },
            },
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) =>
            {
                var input = new NetherInteractiveFloorPreEntrySafetyInput(
                    NetherFloorNodeType.Event,
                    FloorMasterId: 2,
                    MapFloorRows: new[] { new NetherFloorMasterBoundsRow(2, 0, 0) },
                    EventRows: new[] { new NetherFloorEventMasterRow(42, 2, 1, 1001, 0, 0, 0) },
                    EventPartRows: new[]
                    {
                        new NetherFloorEventPartMasterRow(
                            1001,
                            TargetType1: 7,
                            SelectParameter1: 0,
                            TargetType2: 0,
                            SelectParameter2: 0,
                            TargetType3: 0,
                            SelectParameter3: 0,
                            ContentType: 0,
                            ContentId: 0,
                            Amount: 0
                        ),
                    },
                    CurrentErosion: snapshot.ErosionPoint,
                    ActiveHpPermille: new[] { 500 },
                    CurrentNetherGold: snapshot.NetherGold,
                    CurrentTreasureKeys: snapshot.TreasureKeyCount,
                    Settings: settings
                )
                {
                    CurrentCodes = snapshot.Codes,
                    CodeCapacity = snapshot.CodeCapacity,
                };
                NetherInteractiveFloorPreEntrySafetyResult safety =
                    new NetherInteractiveFloorPreEntrySafety().Evaluate(input);
                return ScriptedRuntimeBridge.MergeInteractiveCapture(
                    snapshot,
                    settings,
                    2,
                    input,
                    safety
                );
            },
            CodeTransformTaskPollResult =
                NetherNativeActionResult.UnknownOutcome("scripted-code-transform-fault"),
            RequireExplicitFloorParentTerminal = true,
        };
        bridge.EnqueueOwnedPopup(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeTransform,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 2,
            },
            snapshotAfterInvoke: null
        );
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(
            new RecordingLeaseDriver(),
            retryIntervalUpdates: 1
        );
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(9);

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.UnknownMasterData, NetherAutoClimbController.PauseReason);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(0, bridge.GetOnlyPollCount);
            Assert.Empty(bridge.OwnedPopupActions);
            Assert.Equal(0, bridge.CodeTransformInvokeCount);
            Assert.Equal(0, bridge.CodeTransformConfirmCount);
            Assert.Equal(0, bridge.CodeTransformCompleteCount);
            Assert.Equal(0, bridge.CodeTransformTaskPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_rerolls_same_live_code_offer_then_redispatches_once_before_parent_get()
    {
        NetherCodeState currentRush = RushCodeState(51000, power: 100);
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10)
            with
            {
                CodeReloadCount = 2,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 2,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        NetherSnapshot afterReload = popupWait with
        {
            CodeReloadCount = 1,
            CodeHash = "codes:rush-51000",
            MapHash = "code-reload-wait",
        };
        NetherSnapshot afterSelect = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 1,
                CodeCapacity = 1,
                CodeHash = "codes:rush-52002",
                Codes = new[] { RushCodeState(52002, power: 200) },
            };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = afterSelect,
            CodeReloadAfterSnapshot = afterReload,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
            },
            CodeCandidates = FamilyCodeCandidates(52001, NetherCodeCategory.Impact),
            ReloadCodeCandidates = FamilyCodeCandidates(
                52002,
                NetherCodeCategory.Rush,
                power: 200
            ),
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) => ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(8);

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(
                new[] { NetherActionKind.ReloadCode, NetherActionKind.SelectCode },
                bridge.OwnedPopupActions.Select(action => action.Kind)
            );
            Assert.Equal(1, bridge.CodeReloadInvokeCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
            Assert.Equal(1, bridge.FloorParentTerminalCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_rerolls_once_then_keeps_same_owned_offer_before_parent_get()
    {
        ScriptedRuntimeBridge bridge = CreateOneReloadKeepBridge();
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(6); // SelectFloor -> Reload e0 -> fresh e1 -> Keep -> parent pending.

            Assert.Equal(
                new[] { NetherActionKind.ReloadCode, NetherActionKind.KeepCode },
                bridge.OwnedPopupActions.Select(action => action.Kind)
            );
            Assert.Equal(1, bridge.CodeReloadInvokeCount);
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(0, bridge.FloorParentTerminalCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            bridge.FloorParentCompleted = true;
            Pump(3);
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.CodeReloadInvokeCount);
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_keeps_an_owned_offer_at_reload_reserve_only_after_cancel_task_and_parent_terminal()
    {
        NetherCodeState currentRush = RushCodeState(51000, power: 100);
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10)
            with
            {
                CodeReloadCount = 1,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 1,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        NetherSnapshot afterKeep = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 1,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = afterKeep,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
            },
            CodeCandidates = FamilyCodeCandidates(52001, NetherCodeCategory.Impact),
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) => ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
            RequireExplicitFloorParentTerminal = true,
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();

            NetherAutoClimbController.Update(); // Play → original SelectFloor parent.
            NetherAutoClimbController.Update(); // exact generated cancel callback starts.
            Assert.Equal(new[] { NetherActionKind.KeepCode }, bridge.OwnedPopupActions.Select(action => action.Kind));
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            NetherAutoClimbController.Update(); // cancel task terminal, but parent is still pending.
            Assert.Equal(0, bridge.FloorParentTerminalCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            bridge.FloorParentCompleted = true;
            Pump(3); // parent terminal → one GET → Stable.

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(1, bridge.FloorParentTerminalCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_executes_two_owned_reload_epochs_then_one_select_before_parent_get()
    {
        ScriptedRuntimeBridge bridge = CreateTwoEpochReloadBridge();
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(9); // floor -> reload e0 -> refresh e1 -> reload e1 -> refresh e2 -> select -> parent pending.

            Assert.Equal(
                new[] { NetherActionKind.ReloadCode, NetherActionKind.ReloadCode, NetherActionKind.SelectCode },
                bridge.OwnedPopupActions.Select(action => action.Kind)
            );
            Assert.Equal(2, bridge.CodeReloadInvokeCount);
            Assert.Equal(0, bridge.FloorParentTerminalCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            bridge.FloorParentCompleted = true;
            Pump(3); // parent terminal -> exactly one GET -> Stable.

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(2, bridge.CodeReloadInvokeCount);
            Assert.Equal(1, bridge.FloorParentTerminalCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_executes_two_owned_reload_epochs_then_one_keep_before_parent_get()
    {
        ScriptedRuntimeBridge bridge = CreateTwoEpochReloadBridge(finalKeep: true);
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(9); // floor -> Reload e0/e1 -> exact Keep child task -> original parent still pending.

            Assert.Equal(
                new[] { NetherActionKind.ReloadCode, NetherActionKind.ReloadCode, NetherActionKind.KeepCode },
                bridge.OwnedPopupActions.Select(action => action.Kind)
            );
            Assert.Equal(2, bridge.CodeReloadInvokeCount);
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(0, bridge.FloorParentTerminalCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            bridge.FloorParentCompleted = true;
            Pump(3);

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(2, bridge.CodeReloadInvokeCount);
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(1, bridge.FloorParentTerminalCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_drains_owned_keep_after_off_without_replaying_cancel()
    {
        ScriptedRuntimeBridge bridge = CreateReserveKeepBridge();
        bridge.CodeKeepTaskPollResult = NetherNativeActionResult.Started("scripted-code-keep-pending");
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(3); // SelectFloor -> Keep -> pending generated cancel task.
            Assert.Equal(1, bridge.CodeKeepInvokeCount);

            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Toggle(); // no re-enable over pending owner evidence.
            Assert.False(NetherAutoClimbController.IsEnabled);

            bridge.CodeKeepTaskPollResult = NetherNativeActionResult.Completed("scripted-code-keep-terminal");
            Pump(3); // child -> original parent remains pending.
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            bridge.FloorParentCompleted = true;
            Pump(3);
            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.Disabled, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_pauses_keep_task_fault_without_replaying_cancel_or_starting_get()
    {
        ScriptedRuntimeBridge bridge = CreateReserveKeepBridge();
        bridge.CodeKeepTaskPollResult = NetherNativeActionResult.UnknownOutcome("scripted-code-keep-fault");
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(3); // SelectFloor -> Keep -> task fault.

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.BindingUnavailable, NetherAutoClimbController.PauseReason);
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            Pump(2);
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_pauses_keep_task_timeout_without_replaying_cancel_or_starting_get()
    {
        ScriptedRuntimeBridge bridge = CreateReserveKeepBridge();
        bridge.CodeKeepTaskPollResult = NetherNativeActionResult.Started("scripted-code-keep-never-terminal");
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(605); // exact coordinator's bounded 600-pump task wait expires.

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.BindingUnavailable, NetherAutoClimbController.PauseReason);
            Assert.Equal(1, bridge.CodeKeepInvokeCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(0, bridge.FloorParentTerminalCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_drains_two_epoch_reload_when_disabled_between_epochs_without_replay()
    {
        ScriptedRuntimeBridge bridge = CreateTwoEpochReloadBridge();
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(4); // first RerollAsync is terminal and the changed epoch-1 offer is live.
            Assert.Equal(1, bridge.CodeReloadInvokeCount);

            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Toggle(); // off→on repeat cannot replace the pending parent.
            Assert.False(NetherAutoClimbController.IsEnabled);

            Pump(5); // second reload, fresh epoch-2 select, then explicit parent remains pending.
            Assert.Equal(
                new[] { NetherActionKind.ReloadCode, NetherActionKind.ReloadCode, NetherActionKind.SelectCode },
                bridge.OwnedPopupActions.Select(action => action.Kind)
            );
            Assert.Equal(2, bridge.CodeReloadInvokeCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            bridge.FloorParentCompleted = true;
            Pump(3);

            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.Disabled, NetherAutoClimbController.Phase);
            Assert.Equal(2, bridge.CodeReloadInvokeCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_composes_event_code_battle_and_resource_effects_until_parent_battle_terminal()
    {
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10);
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10);
        NetherSnapshot afterEvent = popupWait with { NetherGold = 15, MapHash = "event-code-battle-wait" };
        NetherSnapshot afterCode = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
            NetherSessionStatus.Battle,
            floorId: 2,
            gold: 15
        ) with
        {
            Codes = new[]
            {
                NetherCodeRuntimeSemanticMapper.MapState(
                    codeId: 30024,
                    rawCategory: (int)NetherCodeCategory.ErosionResistance,
                    effectType: 1,
                    effectParameter1: 100006,
                    effectParameter2: 1,
                    effectParameter3: 0,
                    rarity: 1,
                    power: 0,
                    possessionAmount: 1
                ),
            },
            CodeHash = "code:30024",
            MapHash = "event-code-battle-terminal",
        };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = afterEvent,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new NetherEffect[]
                    {
                        new(NetherEffectKind.NetherGoldGain, 5),
                        new(NetherEffectKind.AbyssCodeOffer, 1),
                        new(NetherEffectKind.Battle, 0),
                    }),
                },
            },
            CodeCandidates = SafeCodeCandidates(30024),
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) => ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
            RequireExplicitFloorParentTerminal = true,
        };
        bridge.EnqueueOwnedPopup(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 2,
            },
            afterCode
        );
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(4); // SelectFloor -> Event -> Code -> original parent remains pending.

            Assert.Equal(
                new[] { NetherActionKind.SelectEventOption, NetherActionKind.SelectCode },
                bridge.OwnedPopupActions.Select(action => action.Kind)
            );
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(0, bridge.FloorParentTerminalCount);

            bridge.FloorParentCompleted = true;
            Pump(3); // final Battle parent terminal -> exactly one authority GET -> stable Battle snapshot.

            // Reconcile establishes the authoritative Battle state; the next frame is the
            // separate battle-lifecycle boundary.  The composition contract must not infer
            // either a Play terminal or a second child mutation while doing so.
            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(NetherSessionStatus.Battle, bridge.CurrentSnapshot.Status);
            Assert.Equal(1, bridge.FloorParentTerminalCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_pauses_before_get_when_parent_terminal_lacks_required_code_stage()
    {
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10);
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10);
        NetherSnapshot prematureParentTerminal = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 2, gold: 15)
            with { MapHash = "event-code-missing-child" };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = prematureParentTerminal,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new NetherEffect[]
                    {
                        new NetherEffect(NetherEffectKind.NetherGoldGain, 5),
                        new NetherEffect(NetherEffectKind.AbyssCodeOffer, 1),
                    }),
                },
            },
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) => ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update(); // SelectFloor
            NetherAutoClimbController.Update(); // Event child
            NetherAutoClimbController.Update(); // premature parent terminal

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Equal(NetherPauseReason.BindingUnavailable, NetherAutoClimbController.PauseReason);
            Assert.Equal("floor-parent-incomplete-owned-popup-stage", NetherAutoClimbController.PauseDetail);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
            Assert.Equal(0, bridge.GetOnlyPollCount);
            Assert.Single(bridge.OwnedPopupActions);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_continues_after_native_server_selected_event_damage_subset()
    {
        NetherSnapshot after = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Play,
            NetherFloorNodeType.Event,
            floorId: 2,
            gold: 10,
            hp: 1000
        ) with
        {
            Characters = new[]
            {
                new NetherCharacterState(1, 600),
                new NetherCharacterState(2, 1000),
            },
            CharacterHpHash = "1:600:1;2:1000:1",
        };

        RunOwnedFloorTransaction(
            NetherFloorNodeType.Event,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = (int)NetherFloorNodeType.Event,
                TargetCharacterId = 1,
                Options = new[]
                {
                    new NetherEventOption(1, new[] { new NetherEffect(NetherEffectKind.Damage, 400) }),
                },
            },
            new NetherFloorEventPartMasterRow(
                20045,
                (int)NetherEffectKind.Damage,
                400,
                0,
                0,
                0,
                0,
                0,
                0,
                0
            ),
            after,
            NetherActionKind.SelectEventOption,
            startHp: 1000,
            assertNextRoute: true,
            startCharacters: new[]
            {
                new NetherCharacterState(1, 1000),
                new NetherCharacterState(2, 1000),
            }
        );
    }

    [Fact]
    public void Production_controller_continues_after_party_wide_event_damage_with_saturated_erosion_heal()
    {
        NetherSnapshot after = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Play,
            NetherFloorNodeType.Event,
            floorId: 2,
            gold: 10,
            hp: 900
        ) with
        {
            ErosionPoint = 0,
            Characters = new[]
            {
                new NetherCharacterState(1, 900),
                new NetherCharacterState(2, 900),
            },
            CharacterHpHash = "1:900:1;2:900:1",
        };

        RunOwnedFloorTransaction(
            NetherFloorNodeType.Event,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = (int)NetherFloorNodeType.Event,
                TargetCharacterId = 1,
                Options = new[]
                {
                    new NetherEventOption(3, new NetherEffect[]
                    {
                        new(NetherEffectKind.Damage, 100),
                        new(NetherEffectKind.ErosionHeal, 10),
                    }),
                },
            },
            new NetherFloorEventPartMasterRow(
                20043,
                (int)NetherEffectKind.Damage,
                100,
                (int)NetherEffectKind.ErosionHeal,
                10,
                0,
                0,
                0,
                0,
                0
            ),
            after,
            NetherActionKind.SelectEventOption,
            startHp: 1000,
            assertNextRoute: true,
            startErosion: 0,
            startCharacters: new[]
            {
                new NetherCharacterState(1, 1000),
                new NetherCharacterState(2, 1000),
            }
        );
    }

    [Fact]
    public void Production_controller_reconciles_authorized_partial_treasure_death_with_a_survivor()
    {
        NetherInteractivePartialDeathEligibility proof = new(
            NetherInteractivePartialDeathObjectiveKind.TreasureHpPayment,
            EventId: 42,
            EventPartId: 20044,
            ObjectiveNodeId: 2
        )
        {
            IsKnown = true,
            ObjectiveReachable = true,
            ExactTreasureRank = 5,
        };
        NetherEffect[] effects = [new NetherEffect(NetherEffectKind.Damage, 80)];
        NetherEventCommitment commitment = new(
            EventId: 42,
            EventPartId: 20044,
            OptionNumber: 1,
            Effects: effects,
            ProjectedErosion: 20,
            HpDelta: -80
        )
        {
            FloorId = 2,
            NodeId = 2,
            ProjectedNetherGold = 10,
            ProjectedTreasureKeys = 0,
            PartialDeathEligibility = proof,
            AllowsPartialActiveDeaths = true,
        };
        NetherSnapshot after = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Play,
            NetherFloorNodeType.Treasure,
            floorId: 2,
            gold: 10,
            keys: 0,
            hp: 300
        ) with
        {
            ErosionPoint = 20,
            Characters =
            [
                new NetherCharacterState(1, 0, IsActive: false),
                new NetherCharacterState(2, 300),
            ],
            CharacterHpHash = "1:0:0;2:300:1",
            Floors = new[]
            {
                new NetherFloorNode(1, 1, 1, NetherFloorNodeType.Recovery)
                {
                    IsUnlocked = true,
                },
                new NetherFloorNode(2, 2, 2, NetherFloorNodeType.Treasure)
                {
                    IsUnlocked = true,
                    PreviousFloorIds = new[] { 1L },
                },
                new NetherFloorNode(3, 3, 3, NetherFloorNodeType.Recovery)
                {
                    IsUnlocked = true,
                    PreviousFloorIds = new[] { 2L },
                },
            },
        };

        RunOwnedFloorTransaction(
            NetherFloorNodeType.Treasure,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Treasure,
                RawFloorType = (int)NetherFloorNodeType.Treasure,
                ExpectedEventCommitment = commitment,
                Options =
                [
                    new NetherEventOption(1, effects)
                    {
                        EventId = 42,
                        EventPartId = 20044,
                        FloorId = 2,
                        NodeId = 2,
                        RequiresExactBinding = true,
                        ProjectedErosion = 20,
            ProjectedHpDelta = -80,
                        ProjectedNetherGold = 10,
                        ProjectedTreasureKeys = 0,
                        PartialDeathEligibility = proof,
                        AllowsPartialActiveDeaths = true,
                    },
                ],
            },
            new NetherFloorEventPartMasterRow(
                20044,
                (int)NetherEffectKind.Damage,
                80,
                0,
                0,
                0,
                0,
                0,
                0,
                0
            ),
            after,
            NetherActionKind.SelectEventOption,
            startHp: 80,
            startCharacters:
            [
                new NetherCharacterState(1, 80),
                new NetherCharacterState(2, 380),
            ],
            partialDeathEligibility: proof,
            startKeys: 0
        );
    }

    [Fact]
    public void Production_controller_reconciles_owned_recovery_with_exact_heal_contract()
    {
        NetherSnapshot after = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Play,
            NetherFloorNodeType.Recovery,
            floorId: 2,
            gold: 10,
            hp: 520
        ) with { CharacterHpHash = "character:1:520" };

        RunOwnedFloorTransaction(
            NetherFloorNodeType.Recovery,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options = new[] { new NetherEventOption(1, new[] { new NetherEffect(NetherEffectKind.Heal, 20) }) },
            },
            new NetherFloorEventPartMasterRow(1002, 1, 20, 0, 0, 0, 0, 0, 0, 0),
            after,
            NetherActionKind.SelectEventOption
        );
    }

    [Fact]
    public void Production_controller_continues_routing_after_recovery_heal_is_capped_at_full_hp()
    {
        NetherSnapshot after = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Play,
            NetherFloorNodeType.Recovery,
            floorId: 2,
            gold: 10,
            hp: 1000
        ) with { CharacterHpHash = "character:1:1000" };

        RunOwnedFloorTransaction(
            NetherFloorNodeType.Recovery,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options = new[] { new NetherEventOption(2, new[] { new NetherEffect(NetherEffectKind.Heal, 300) }) },
            },
            new NetherFloorEventPartMasterRow(1002, 1, 300, 0, 0, 0, 0, 0, 0, 0),
            after,
            NetherActionKind.SelectEventOption,
            startHp: 1000,
            assertNextRoute: true
        );
    }

    [Fact]
    public void Production_controller_continues_routing_after_category_skill_applies_erosion_relief()
    {
        NetherSnapshot after = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Play,
            NetherFloorNodeType.Recovery,
            floorId: 2,
            gold: 10,
            hp: 1000
        ) with
        {
            ErosionPoint = 15,
            CharacterHpHash = "character:1:1000",
        };

        RunOwnedFloorTransaction(
            NetherFloorNodeType.Recovery,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options = new[] { new NetherEventOption(2, new[] { new NetherEffect(NetherEffectKind.Heal, 300) }) },
            },
            new NetherFloorEventPartMasterRow(1002, 1, 300, 0, 0, 0, 0, 0, 0, 0),
            after,
            NetherActionKind.SelectEventOption,
            startHp: 1000,
            assertNextRoute: true,
            activeCodeErosion: new NetherActiveCodeErosionProjection
            {
                ErosionProjectionKnown = true,
                CodeHash = "nether-codes:safe-category-threshold",
                ErosionEffects = new[]
                {
                    new NetherCodeEffect(30000, NetherCodeEffectKind.ErosionAdditionDown, 5),
                },
            }
        );
    }

    [Fact]
    public void Production_controller_reconciles_owned_treasure_with_exact_key_contract()
    {
        NetherSnapshot after = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Play,
            NetherFloorNodeType.Treasure,
            floorId: 2,
            gold: 10,
            keys: 0
        );

        RunOwnedFloorTransaction(
            NetherFloorNodeType.Treasure,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Treasure,
                Options = new[] { new NetherEventOption(1, new[] { new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1) }) },
            },
            new NetherFloorEventPartMasterRow(
                1001,
                (int)NetherEffectKind.TreasureKeyUsed,
                1,
                0,
                0,
                0,
                0,
                0,
                0,
                0
            ),
            after,
            NetherActionKind.SelectEventOption
        );
    }

    [Fact]
    public void Production_controller_closes_unknown_owned_shop_before_replanning()
    {
        NetherShopMode previous = AutoNether.Config.NetherAutoClimbShopMode.Value;
        AutoNether.Config.NetherAutoClimbShopMode.Value = NetherShopMode.EquipmentBags;
        try
        {
            NetherSnapshot after = ScriptedRuntimeBridge.OwnedRouteSnapshot(
                NetherSessionStatus.Play,
                NetherFloorNodeType.Shop,
                floorId: 2,
                gold: 10
            );

            ScriptedRuntimeBridge bridge = RunOwnedFloorTransaction(
                NetherFloorNodeType.Shop,
                new NetherRuntimePopupContext
                {
                    Kind = NetherRuntimePopupKind.Shop,
                    ShopContents =
                    [
                        new NetherShopContent(
                            contentId: 42,
                            itemId: 42,
                            itemType: 91,
                            rarity: NetherRewardRarity.Gold,
                            price: 300,
                            usesNetherGold: true,
                            amount: 1,
                            known: false
                        ),
                    ],
                },
                null,
                after,
                NetherActionKind.LeaveShop
            );

            Assert.Equal(1, bridge.ShopLeaveInvokeCount);
            Assert.Equal(0, bridge.ShopCloseInvokeCount);
            Assert.Single(bridge.OwnedPopupActions, action => action.Kind == NetherActionKind.LeaveShop);
        }
        finally
        {
            AutoNether.Config.NetherAutoClimbShopMode.Value = previous;
        }
    }

    [Fact]
    public void Production_controller_reconciles_owned_shop_buy_with_exact_content_amount_and_cost()
    {
        NetherShopMode previous = AutoNether.Config.NetherAutoClimbShopMode.Value;
        AutoNether.Config.NetherAutoClimbShopMode.Value = NetherShopMode.EquipmentBags;
        try
        {
            NetherSnapshot after = ScriptedRuntimeBridge.OwnedRouteSnapshot(
                NetherSessionStatus.Play,
                NetherFloorNodeType.Shop,
                floorId: 2,
                gold: 0,
                floorLevel: 91
            ) with
            {
                AcquiredItems = new[] { new NetherRewardItem(42, 1) },
            };
            ScriptedRuntimeBridge bridge = RunOwnedFloorTransaction(
                NetherFloorNodeType.Shop,
                new NetherRuntimePopupContext
                {
                    Kind = NetherRuntimePopupKind.Shop,
                    ShopContents = new[]
                    {
                        new NetherShopContent(
                            contentId: 42,
                            itemId: 42,
                            itemType: 91,
                            rarity: NetherRewardRarity.Gold,
                            price: 300,
                            usesNetherGold: true,
                            amount: 1,
                            known: true
                        )
                        {
                            CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
                        },
                    },
                },
                null,
                after,
                NetherActionKind.BuyShopItem,
                targetFloorLevel: 91,
                targetGold: 300,
                startFloorLevel: 91,
                startGold: 300
            );

            // The production coordinator holds the original floor parent across the
            // purchase child and invokes the exact SetupPopupEvent close once.  A second
            // frame after stable must not replay either mutation.
            Assert.Equal(1, bridge.ShopCloseInvokeCount);
            Assert.Equal(1, bridge.FloorParentTerminalCount);
            Assert.Equal(1, bridge.OwnedPopupActions.Count(action => action.Kind == NetherActionKind.BuyShopItem));
        }
        finally
        {
            AutoNether.Config.NetherAutoClimbShopMode.Value = previous;
        }
    }

    [Fact]
    public void Production_controller_pauses_shop_buy_without_close_or_get_when_purchase_child_faults()
    {
        NetherShopMode previous = AutoNether.Config.NetherAutoClimbShopMode.Value;
        AutoNether.Config.NetherAutoClimbShopMode.Value = NetherShopMode.EquipmentBags;
        try
        {
            NetherSnapshot routeStart = ScriptedRuntimeBridge.OwnedRouteSnapshot(
                NetherSessionStatus.Play,
                NetherFloorNodeType.Shop,
                floorId: 1,
                gold: 300,
                floorLevel: 91
            );
            NetherSnapshot popupWait = ScriptedRuntimeBridge.OwnedRouteSnapshot(
                NetherSessionStatus.Wait,
                NetherFloorNodeType.Shop,
                floorId: 2,
                gold: 300,
                floorLevel: 91
            );
            var bridge = new ScriptedRuntimeBridge
            {
                CurrentSnapshot = routeStart,
                FloorSelectionDispatchSnapshot = popupWait,
                OwnedPopup = new NetherRuntimePopupContext
                {
                    Kind = NetherRuntimePopupKind.Shop,
                    OwnerAction = NetherActionKind.SelectFloor,
                    OwnerGeneration = 1,
                    Sequence = 1,
                    ShopContents = new[]
                    {
                        new NetherShopContent(42, 42, 91, NetherRewardRarity.Gold, 300, true, 1, true)
                        {
                            CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
                        },
                    },
                },
                RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
                InteractivePreEntryFactory = (snapshot, settings) =>
                    ScriptedRuntimeBridge.OwnedInteractivePreEntry(snapshot, settings, NetherFloorNodeType.Shop, null),
                ShopPurchaseChildPollResult = NetherNativeActionResult.UnknownOutcome("scripted-buy-child-fault"),
            };
            var lease = new RecordingLeaseDriver();
            var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
            using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

            try
            {
                NetherAutoClimbController.Initialize();
                NetherAutoClimbController.Toggle();
                Pump(4); // SelectFloor -> confirm purchase -> Buy child -> child terminal fault.

                Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
                Assert.Equal(0, bridge.ShopCloseInvokeCount);
                Assert.Equal(0, bridge.GetOnlyBeginCount);
                Assert.Single(bridge.OwnedPopupActions, action => action.Kind == NetherActionKind.BuyShopItem);
            }
            finally
            {
                NetherAutoClimbController.OnPluginUnload();
            }
        }
        finally
        {
            AutoNether.Config.NetherAutoClimbShopMode.Value = previous;
        }
    }

    [Fact]
    public void Production_controller_drains_owned_shop_buy_after_off_without_replaying_buy_or_close()
    {
        NetherShopMode previous = AutoNether.Config.NetherAutoClimbShopMode.Value;
        AutoNether.Config.NetherAutoClimbShopMode.Value = NetherShopMode.EquipmentBags;
        try
        {
            NetherSnapshot routeStart = ScriptedRuntimeBridge.OwnedRouteSnapshot(
                NetherSessionStatus.Play,
                NetherFloorNodeType.Shop,
                floorId: 1,
                gold: 300,
                floorLevel: 91
            );
            NetherSnapshot popupWait = ScriptedRuntimeBridge.OwnedRouteSnapshot(
                NetherSessionStatus.Wait,
                NetherFloorNodeType.Shop,
                floorId: 2,
                gold: 300,
                floorLevel: 91
            );
            NetherSnapshot afterPurchase = ScriptedRuntimeBridge.OwnedRouteSnapshot(
                NetherSessionStatus.Play,
                NetherFloorNodeType.Shop,
                floorId: 2,
                gold: 0,
                floorLevel: 91
            ) with { AcquiredItems = new[] { new NetherRewardItem(42, 1) } };
            var bridge = new ScriptedRuntimeBridge
            {
                CurrentSnapshot = routeStart,
                FloorSelectionDispatchSnapshot = popupWait,
                OwnedPopupAfterSnapshot = afterPurchase,
                OwnedPopup = new NetherRuntimePopupContext
                {
                    Kind = NetherRuntimePopupKind.Shop,
                    OwnerAction = NetherActionKind.SelectFloor,
                    OwnerGeneration = 1,
                    Sequence = 1,
                    ShopContents = new[]
                    {
                        new NetherShopContent(42, 42, 91, NetherRewardRarity.Gold, 300, true, 1, true)
                        {
                            CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
                        },
                    },
                },
                RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
                InteractivePreEntryFactory = (snapshot, settings) =>
                    ScriptedRuntimeBridge.OwnedInteractivePreEntry(snapshot, settings, NetherFloorNodeType.Shop, null),
                ShopPurchaseChildPollResult = NetherNativeActionResult.Started("scripted-shop-purchase-pending"),
                RequireExplicitFloorParentTerminal = true,
            };
            var lease = new RecordingLeaseDriver();
            var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
            using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

            try
            {
                NetherAutoClimbController.Initialize();
                NetherAutoClimbController.Toggle();
                Pump(3); // SelectFloor -> Buy child -> observed child pending.

                Assert.Single(bridge.OwnedPopupActions, action => action.Kind == NetherActionKind.BuyShopItem);
                Assert.Equal(0, bridge.ShopCloseInvokeCount);

                NetherAutoClimbController.Toggle(); // off: preserve the already-sent Buy.
                NetherAutoClimbController.Toggle(); // off->on repeat must be ignored while draining.
                Assert.False(NetherAutoClimbController.IsEnabled);

                bridge.ShopPurchaseChildPollResult = NetherNativeActionResult.Completed("scripted-shop-purchase-terminal");
                Pump(3); // child -> exact close -> original parent remains genuinely pending.

                Assert.Equal(1, bridge.ShopCloseInvokeCount);
                Assert.Equal(0, bridge.FloorParentTerminalCount);
                Assert.Equal(0, bridge.GetOnlyBeginCount);

                bridge.FloorParentCompleted = true;
                Pump(3); // original parent -> one GET -> Disabled.

                Assert.False(NetherAutoClimbController.IsEnabled);
                Assert.Equal(NetherAutoClimbPhase.Disabled, NetherAutoClimbController.Phase);
                Assert.Single(bridge.OwnedPopupActions, action => action.Kind == NetherActionKind.BuyShopItem);
                Assert.Equal(1, bridge.ShopCloseInvokeCount);
                Assert.Equal(1, bridge.FloorParentTerminalCount);
                Assert.Equal(1, bridge.GetOnlyBeginCount);
                Assert.Equal(1, bridge.GetOnlyPollCount);
            }
            finally
            {
                NetherAutoClimbController.OnPluginUnload();
            }
        }
        finally
        {
            AutoNether.Config.NetherAutoClimbShopMode.Value = previous;
        }
    }

    [Fact]
    public void Production_controller_drains_owned_code_reload_after_off_without_a_second_reload()
    {
        NetherCodeState currentRush = RushCodeState(51000, power: 100);
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10)
            with
            {
                CodeReloadCount = 2,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 2,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        NetherSnapshot afterReload = popupWait with
        {
            CodeReloadCount = 1,
            CodeHash = "codes:rush-51000",
            MapHash = "code-reload-off-wait",
        };
        NetherSnapshot afterSelect = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 1,
                CodeCapacity = 1,
                CodeHash = "codes:rush-52002",
                Codes = new[] { RushCodeState(52002, power: 200) },
            };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = afterSelect,
            CodeReloadAfterSnapshot = afterReload,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
            },
            CodeCandidates = FamilyCodeCandidates(52001, NetherCodeCategory.Impact),
            ReloadCodeCandidates = FamilyCodeCandidates(
                52002,
                NetherCodeCategory.Rush,
                power: 200
            ),
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) => ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
            CodeReloadTaskPollResult = NetherNativeActionResult.Started("scripted-reroll-pending"),
            RequireExplicitFloorParentTerminal = true,
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(3); // SelectFloor -> RerollAsync -> observed child pending.

            Assert.Equal(1, bridge.CodeReloadInvokeCount);
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Toggle();
            Assert.False(NetherAutoClimbController.IsEnabled);

            bridge.CodeReloadTaskPollResult = NetherNativeActionResult.Completed("scripted-reroll-terminal");
            Pump(4); // refresh epoch -> Select -> original parent remains genuinely pending.

            Assert.Equal(1, bridge.CodeReloadInvokeCount);
            Assert.Equal(0, bridge.FloorParentTerminalCount);
            Assert.Equal(0, bridge.GetOnlyBeginCount);

            bridge.FloorParentCompleted = true;
            Pump(3); // original parent -> one GET -> Disabled.

            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.Disabled, NetherAutoClimbController.Phase);
            Assert.Equal(1, bridge.CodeReloadInvokeCount);
            Assert.Equal(
                new[] { NetherActionKind.ReloadCode, NetherActionKind.SelectCode },
                bridge.OwnedPopupActions.Select(action => action.Kind)
            );
            Assert.Equal(1, bridge.FloorParentTerminalCount);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_allows_bridge_proven_recovered_event_and_reconciles_once()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = new ScriptedRuntimeBridge().WaitForInteractivePopup,
            ActivePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.None,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new[] { new NetherEffect(NetherEffectKind.NetherGoldGain, 1) }),
                },
            },
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(4);

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(new[] { NetherActionKind.SelectEventOption }, bridge.Invocations);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_allows_bridge_proven_recovered_event_while_snapshot_is_play()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = new ScriptedRuntimeBridge().AfterInteractive,
            ActivePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.None,
                HasRecoveredFloorEventTaskEvidence = true,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new[] { new NetherEffect(NetherEffectKind.NetherGoldGain, 1) }),
                },
            },
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(4);

            Assert.Equal(NetherAutoClimbPhase.Stable, NetherAutoClimbController.Phase);
            Assert.Equal(new[] { NetherActionKind.SelectEventOption }, bridge.Invocations);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_keeps_unproven_foreground_event_fail_closed_while_play()
    {
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = new ScriptedRuntimeBridge().AfterInteractive,
            ActivePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                OwnerAction = NetherActionKind.None,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new[] { new NetherEffect(NetherEffectKind.NetherGoldGain, 1) }),
                },
            },
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(1);

            Assert.Equal(NetherAutoClimbPhase.Paused, NetherAutoClimbController.Phase);
            Assert.Empty(bridge.Invocations);
            Assert.Equal(0, bridge.GetOnlyBeginCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    [Fact]
    public void Production_controller_drains_midflight_off_without_reenable_or_duplicate_mutation()
    {
        var bridge = new ScriptedRuntimeBridge();
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.OnBattleSettingsAccessorRegistered();
            NetherAutoClimbController.Toggle();
            NetherAutoClimbController.Update();
            Assert.Equal(new[] { NetherActionKind.SelectFloor }, bridge.Invocations);

            NetherAutoClimbController.Toggle(); // F12 off while native floor parent is pending.
            NetherAutoClimbController.Toggle(); // A repeat must not re-enable over evidence.
            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.ExecutingNativeAction, NetherAutoClimbController.Phase);

            Pump(3);
            Assert.False(NetherAutoClimbController.IsEnabled);
            Assert.Equal(NetherAutoClimbPhase.Disabled, NetherAutoClimbController.Phase);
            Assert.Single(bridge.Invocations);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    private static void Pump(int updates)
    {
        for (int index = 0; index < updates; index++)
            NetherAutoClimbController.Update();
    }

    private static ScriptedRuntimeBridge CreateTwoEpochReloadBridge(bool finalKeep = false)
    {
        NetherCodeState currentRush = RushCodeState(51000, power: 100);
        const string initialCodeHash = "codes:rush-51000";
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10)
            with
            {
                CodeReloadCount = 3,
                CodeCapacity = 1,
                CodeHash = initialCodeHash,
                Codes = new[] { currentRush },
            };
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 3,
                CodeCapacity = 1,
                CodeHash = initialCodeHash,
                Codes = new[] { currentRush },
            };
        NetherSnapshot afterFirstReload = popupWait with
        {
            CodeReloadCount = 2,
            CodeHash = initialCodeHash,
            MapHash = "code-reload-e1-wait",
        };
        NetherSnapshot afterSecondReload = popupWait with
        {
            CodeReloadCount = 1,
            CodeHash = initialCodeHash,
            MapHash = "code-reload-e2-wait",
        };
        NetherSnapshot afterSelect = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 1,
                CodeCapacity = 1,
                CodeHash = "codes:rush-52003",
                Codes = new[] { RushCodeState(52003, power: 200) },
            };
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = afterSelect,
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
            },
            CodeCandidates = FamilyCodeCandidates(52001, NetherCodeCategory.Impact),
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) => ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
            RequireExplicitFloorParentTerminal = true,
        };
        if (finalKeep)
        {
            bridge.OwnedPopupAfterSnapshot = ScriptedRuntimeBridge.InteractiveRouteSnapshot(
                NetherSessionStatus.Play,
                floorId: 2,
                gold: 10
            ) with
            {
                CodeReloadCount = 1,
                CodeCapacity = 1,
                CodeHash = initialCodeHash,
                Codes = new[] { currentRush },
            };
        }
        bridge.EnqueueCodeReloadRefresh(
            afterFirstReload,
            FamilyCodeCandidates(52002, NetherCodeCategory.Impact)
        );
        bridge.EnqueueCodeReloadRefresh(
            afterSecondReload,
            finalKeep
                ? FamilyCodeCandidates(52004, NetherCodeCategory.Impact)
                : FamilyCodeCandidates(52003, NetherCodeCategory.Rush, power: 200)
        );
        return bridge;
    }

    private static ScriptedRuntimeBridge CreateReserveKeepBridge()
    {
        NetherCodeState currentRush = RushCodeState(51000, power: 100);
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10)
            with
            {
                CodeReloadCount = 1,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 1,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        return new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 2, gold: 10)
                with
                {
                    CodeReloadCount = 1,
                    CodeCapacity = 1,
                    CodeHash = "codes:rush-51000",
                    Codes = new[] { currentRush },
                },
            OwnedPopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeOffer,
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
            },
            CodeCandidates = FamilyCodeCandidates(52001, NetherCodeCategory.Impact),
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(),
            InteractivePreEntryFactory = (snapshot, settings) => ScriptedRuntimeBridge.InteractivePreEntry(snapshot, settings),
            RequireExplicitFloorParentTerminal = true,
        };
    }

    private static ScriptedRuntimeBridge CreateOneReloadKeepBridge()
    {
        ScriptedRuntimeBridge bridge = CreateReserveKeepBridge();
        NetherCodeState currentRush = RushCodeState(51000, power: 100);
        NetherSnapshot routeStart = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Play, floorId: 1, gold: 10)
            with
            {
                CodeReloadCount = 2,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        NetherSnapshot popupWait = ScriptedRuntimeBridge.InteractiveRouteSnapshot(NetherSessionStatus.Wait, floorId: 2, gold: 10)
            with
            {
                CodeReloadCount = 2,
                CodeCapacity = 1,
                CodeHash = "codes:rush-51000",
                Codes = new[] { currentRush },
            };
        bridge.CurrentSnapshot = routeStart;
        bridge.FloorSelectionDispatchSnapshot = popupWait;
        bridge.CodeReloadAfterSnapshot = popupWait with
        {
            CodeReloadCount = 1,
            CodeHash = "codes:rush-51000",
            MapHash = "code-reload-keep-e1-wait",
        };
        bridge.CodeCandidates = FamilyCodeCandidates(52001, NetherCodeCategory.Impact);
        bridge.ReloadCodeCandidates = FamilyCodeCandidates(52002, NetherCodeCategory.Impact);
        return bridge;
    }

    private static NetherRuntimeCodeCandidatesResult SafeCodeCandidates(long codeId) =>
        FamilyCodeCandidates(codeId, NetherCodeCategory.Safe);

    private static NetherRuntimeCodeCandidatesResult FamilyCodeCandidates(
        long codeId,
        NetherCodeCategory category,
        int power = 0
    ) => new(
        new[]
        {
            NetherCodeRuntimeSemanticMapper.MapCandidate(
                codeId,
                (int)category,
                effectType: 1,
                effectParameter1: 100006,
                effectParameter2: 1,
                effectParameter3: 0,
                rarity: 1,
                power: power
            ) with { PartyCoverageKnown = true, PartyCoverage = 1 },
        },
        IsMasterComplete: true,
        Detail: string.Empty
    );

    private static NetherCodeState RushCodeState(long codeId, int power) =>
        NetherCodeRuntimeSemanticMapper.MapState(
            codeId,
            (int)NetherCodeCategory.Rush,
            effectType: 1,
            effectParameter1: 100006,
            effectParameter2: 1,
            effectParameter3: 0,
            rarity: 1,
            power: power,
            possessionAmount: 1
        ) with { PartyCoverageKnown = true, PartyCoverage = 1 };

    private static ScriptedRuntimeBridge RunOwnedFloorTransaction(
        NetherFloorNodeType kind,
        NetherRuntimePopupContext popup,
        NetherFloorEventPartMasterRow? eventPart,
        NetherSnapshot after,
        NetherActionKind expectedChild,
        int startHp = 500,
        bool assertNextRoute = false,
        NetherActiveCodeErosionProjection? activeCodeErosion = null,
        int startErosion = 20,
        IReadOnlyList<NetherCharacterState>? startCharacters = null,
        NetherInteractivePartialDeathEligibility? partialDeathEligibility = null,
        int startKeys = 1,
        int targetFloorLevel = 2,
        int targetGold = 10,
        int startFloorLevel = 1,
        int startGold = 10
    )
    {
        NetherActiveCodeErosionProjection codeProjection =
            activeCodeErosion ?? ScriptedRuntimeBridge.KnownEmptyCodeProjection();
        NetherCodeState[] recoveryCodes = kind == NetherFloorNodeType.Recovery
            ? new[] { ScriptedRuntimeBridge.RecoveryTransformCode() }
            : Array.Empty<NetherCodeState>();
        string codeHash = kind == NetherFloorNodeType.Recovery
            ? activeCodeErosion?.CodeHash ?? "nether-codes:recovery-transform-proof"
            : "nether-codes:none";
        if (kind == NetherFloorNodeType.Recovery && activeCodeErosion == null)
        {
            codeProjection = codeProjection with { CodeHash = codeHash };
        }
        NetherSnapshot routeStart = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Play,
            kind,
            floorId: 1,
            gold: startGold,
            keys: startKeys,
            hp: startHp,
            floorLevel: startFloorLevel
        ) with
        {
            ErosionPoint = startErosion,
            Characters = startCharacters ?? new[] { new NetherCharacterState(1, startHp) },
            CharacterHpHash = startCharacters == null
                ? "character:1:" + startHp
                : string.Join(
                    ";",
                    startCharacters.Select(character =>
                        character.CharacterId + ":" + character.HpPermille + ":" + (character.IsActive ? 1 : 0)
                    )
                ),
            Codes = recoveryCodes,
            CodeHash = codeHash,
        };
        NetherSnapshot popupWait = ScriptedRuntimeBridge.OwnedRouteSnapshot(
            NetherSessionStatus.Wait,
            kind,
            floorId: 2,
            gold: targetGold,
            keys: startKeys,
            hp: startHp,
            floorLevel: targetFloorLevel
        ) with
        {
            ErosionPoint = startErosion,
            Characters = routeStart.Characters,
            CharacterHpHash = routeStart.CharacterHpHash,
            Codes = routeStart.Codes,
            CodeHash = routeStart.CodeHash,
        };
        NetherSnapshot ownedAfter = kind == NetherFloorNodeType.Recovery
            ? after with
            {
                Codes = routeStart.Codes,
                CodeHash = routeStart.CodeHash,
            }
            : after;
        var bridge = new ScriptedRuntimeBridge
        {
            CurrentSnapshot = routeStart,
            FloorSelectionDispatchSnapshot = popupWait,
            OwnedPopupAfterSnapshot = ownedAfter,
            OwnedPopup = popup with
            {
                OwnerAction = NetherActionKind.SelectFloor,
                OwnerGeneration = 1,
                Sequence = 1,
            },
            ActiveCodeErosion = codeProjection,
            RouteSafetyOverride = ScriptedRuntimeBridge.InteractiveRouteSafety(codeProjection),
            BindRouteSafetyHpToCurrentSnapshot = true,
            InteractivePreEntryFactory = (snapshot, settings) =>
                ScriptedRuntimeBridge.OwnedInteractivePreEntry(
                    snapshot,
                    settings,
                    kind,
                    eventPart,
                    partialDeathEligibility
                ),
        };
        var lease = new RecordingLeaseDriver();
        var lifecycle = new NetherBattleSettingsLeaseControllerLifecycle(lease, retryIntervalUpdates: 1);
        using IDisposable scope = NetherAutoClimbController.PushRuntimeBridgeForTests(bridge, lifecycle);

        try
        {
            NetherAutoClimbController.Initialize();
            NetherAutoClimbController.Toggle();
            Pump(expectedChild == NetherActionKind.BuyShopItem ? 8 : 5);

            Assert.True(
                NetherAutoClimbController.Phase == NetherAutoClimbPhase.Stable,
                "phase=" + NetherAutoClimbController.Phase
                    + " pause=" + NetherAutoClimbController.PauseReason
                    + " detail=" + NetherAutoClimbController.PauseDetail
                    + " invocations=" + string.Join(",", bridge.Invocations)
                    + " owned=" + string.Join(",", bridge.OwnedPopupActions.Select(action => action.Kind))
                    + " proofs=" + string.Join(
                        ";",
                        bridge.BoundRecoveryBranchSafetyByPartId.Select(pair =>
                            pair.Key + ":" + pair.Value.BranchKind
                                + ":known=" + pair.Value.IsKnown
                                + ":complete=" + pair.Value.IsCompleteVisibleBranch
                                + ":safe=" + pair.Value.IsNextVisibleBranchSafe
                                + ":reason=" + pair.Value.UnknownReason
                        )
                    )
            );
            Assert.Single(bridge.Invocations, action => action == NetherActionKind.SelectFloor);
            NetherPlannedAction child = Assert.Single(bridge.OwnedPopupActions);
            Assert.Equal(expectedChild, child.Kind);
            Assert.Equal(1, bridge.GetOnlyBeginCount);
            Assert.Equal(1, bridge.GetOnlyPollCount);
            if (assertNextRoute)
            {
                NetherAutoClimbController.Update();
                Assert.Equal(2, bridge.Invocations.Count(action => action == NetherActionKind.SelectFloor));
            }
            return bridge;
        }
        finally
        {
            NetherAutoClimbController.OnPluginUnload();
        }
    }

    private static NetherRuntimePopupContext PendingActiveCodeOffer(long sequence) => new()
    {
        Kind = NetherRuntimePopupKind.CodeOffer,
        RuntimeGeneration = 1,
        OwnerAction = NetherActionKind.SelectFloor,
        OwnerGeneration = 1,
        Sequence = sequence,
    };

    private sealed class ScriptedRuntimeBridge : INetherRuntimeBridge, INetherTypedSemanticProviderRegistration, INetherOwnedPopupNativeStagePort
    {
        private bool _eventNativePending;
        private bool _finishNativePending;
        private bool _battleClearAvailable;
        private bool _floorParentPending;
        private bool _shopPurchaseSnapshotApplied;
        private bool _codeReloadSnapshotApplied;
        private bool _codeKeepSnapshotApplied;
        private bool _battleResultNextInvoked;
        private bool _battleResultContinuationCompleted;
        // The scripted native port deliberately leaves popup and SelectFloor parent pending
        // after every child.  The production core below is therefore the only sequencing
        // implementation exercised by E2E tests.
        private readonly NetherOwnedPopupStageBridgeEntry _ownedPopupStageEntry;
        private NetherRuntimeTypedSemanticProviderFactory? _typedSemanticProviderFactory;
        private NetherStrategyTypedSemanticProviderEvidence? _latestTypedSemanticProvider;

        public ScriptedRuntimeBridge()
        {
            _ownedPopupStageEntry = new NetherOwnedPopupStageBridgeEntry(this, maximumPendingPumps: 8);
            PlayBeforeInteractive = Snapshot(NetherSessionStatus.Play, mapId: 1, floorId: 1, floorLevel: 1, gold: 10, tickets: 2);
            WaitForInteractivePopup = PlayBeforeInteractive with { Status = NetherSessionStatus.Wait, MapHash = "wait-event" };
            AfterInteractive = PlayBeforeInteractive with { NetherGold = 11, MapHash = "after-event" };
            BattleSnapshot = AfterInteractive with
            {
                Status = NetherSessionStatus.Battle,
                CurrentFloorId = 2,
                FloorLevel = 2,
                FloorIndex = 2,
                MapHash = "battle-floor-2",
            };
            AfterBattle = BattleSnapshot with
            {
                Status = NetherSessionStatus.Play,
                ErosionPoint = BattleSnapshot.ErosionPoint + 5,
                MapHash = "battle-settled",
            };
            SecondBattleOrigin = AfterInteractive with { MapHash = "second-battle-origin" };
            SleepCheckpoint = AfterBattle with
            {
                Status = NetherSessionStatus.Sleep,
                FloorLevel = 10,
                FloorIndex = 10,
                TicketCount = 2,
                LockReward = 0,
                ContinuationTarget = new NetherContinuationTarget(2, 20, 10),
                MapHash = "sleep-checkpoint",
            };
            NewSegment = SleepCheckpoint with
            {
                Status = NetherSessionStatus.Play,
                MapId = 2,
                CurrentFloorId = 20,
                FloorLevel = 10,
                FloorIndex = 1,
                TicketCount = 1,
                ContinuationTarget = null,
                MapHash = "segment-2",
            };
            ClearResult = NewSegment with { Status = NetherSessionStatus.Clear, MapHash = "result-clear" };
            InteractivePopup = new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = (int)NetherFloorNodeType.Event,
                Options = new[]
                {
                    new NetherEventOption(1, new[] { new NetherEffect(NetherEffectKind.NetherGoldGain, 1) }),
                },
            };
            CurrentSnapshot = PlayBeforeInteractive;
        }

        public NetherSnapshot PlayBeforeInteractive { get; }
        public NetherSnapshot WaitForInteractivePopup { get; }
        public NetherSnapshot AfterInteractive { get; }
        public NetherSnapshot BattleSnapshot { get; }
        public NetherSnapshot AfterBattle { get; }
        public NetherSnapshot SecondBattleOrigin { get; }
        public NetherSnapshot SleepCheckpoint { get; }
        public NetherSnapshot NewSegment { get; }
        public NetherSnapshot ClearResult { get; }
        public NetherRuntimePopupContext InteractivePopup { get; }
        public NetherSnapshot CurrentSnapshot { get; set; }
        public NetherRuntimePopupContext? ActivePopup { get; set; }
        public NetherRuntimePopupResult? ActivePopupResultOverride { get; set; }
        public NetherRuntimePopupContext? OwnedPopup { get; set; }
        public NetherSnapshot? FloorSelectionDispatchSnapshot { get; set; }
        public NetherSnapshot? OwnedPopupAfterSnapshot { get; set; }
        public NetherSnapshot? CodeReloadAfterSnapshot { get; set; }
        public NetherRuntimeRouteSafetyData? RouteSafetyOverride { get; set; }
        public NetherStrategyVisibleMapEvidence? VisibleMap { get; set; }
        public Func<NetherSnapshot, NetherStrategyVisibleMapEvidence?>? VisibleMapFactory { get; set; }
        /// <summary>
        /// Managed DTO fixture switch. Production has no synthetic fallback; this opt-in test
        /// fixture supplies a complete, snapshot-shaped visible branch through the same capture
        /// seam so controller E2E cases do not accidentally exercise the production pause path.
        /// Set false for an explicit missing/empty-vector regression.
        /// </summary>
        public bool ProvideCompleteVisibleBranchEvidence { get; set; } = true;
        private readonly NetherRouteOwnedEventProcurementProducer _routeOwnedEventProcurementProducer = new();
        public IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> BoundEventProcurementCommitments { get; private set; } =
            new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
        public IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> BoundRecoveryBranchSafetyByPartId { get; private set; } =
            new Dictionary<long, NetherRecoveryBranchSafetyEvidence>();
        public NetherRankFiveKeyProcurementDecision? BoundRankFiveKeyProcurement { get; private set; }
        public int RecoveryBranchSafetyBindCount { get; private set; }
        public int RankFiveKeyProcurementBindCount { get; private set; }
        public int InteractivePreEntryCaptureCount { get; private set; }
        public int TypedSemanticProviderRegistrationCount { get; private set; }
        public bool DropBoundRecoveryProofOnNextCapture { get; set; }
        public int DropBoundRecoveryProofOnCaptureNumber { get; set; }
        public bool BindRouteSafetyHpToCurrentSnapshot { get; set; }
        public NetherActiveCodeErosionProjection ActiveCodeErosion { get; set; } =
            KnownEmptyCodeProjection();
        public Func<NetherSnapshot, NetherAutoClimbSettings, NetherRuntimeInteractivePreEntryInputsResult>? InteractivePreEntryFactory { get; set; }

        public void RegisterTypedSemanticProviderFactory(
            NetherRuntimeTypedSemanticProviderFactory? factory
        )
        {
            _typedSemanticProviderFactory = factory;
            TypedSemanticProviderRegistrationCount++;
        }
        public Func<
            NetherSnapshot,
            NetherRuntimeCodeCandidatesResult,
            NetherAutoClimbSettings,
            NetherRuntimeCodePolicyEvidenceResult
        >? CodePolicyEvidenceFactory { get; set; }
        public NetherRuntimeCodeCandidatesResult CodeCandidates { get; set; } = new(
            Array.Empty<NetherCodeCandidate>(),
            IsMasterComplete: true,
            Detail: string.Empty
        );
        public NetherRuntimeCodeCandidatesResult ReloadCodeCandidates { get; set; } =
            NetherRuntimeCodeCandidatesResult.Failure("e2e-no-reloaded-code-popup");
        public List<NetherActionKind> Invocations { get; } = new();
        public List<NetherPlannedAction> NativeActions { get; } = new();
        public List<NetherPlannedAction> OwnedPopupActions { get; } = new();
        public int BeginFloorParentCount { get; private set; }
        public int OwnedPopupInvokeCount { get; private set; }
        public int ShopLeaveInvokeCount { get; private set; }
        public int ShopCloseInvokeCount { get; private set; }
        public int CodeReloadInvokeCount { get; private set; }
        public int CodeKeepInvokeCount { get; private set; }
        public int CodeTransformInvokeCount { get; private set; }
        public int CodeTransformConfirmCount { get; private set; }
        public int CodeTransformCompleteCount { get; private set; }
        public int CodeTransformTaskPollCount { get; private set; }
        public NetherNativeActionResult ShopPurchaseChildPollResult { get; set; } =
            NetherNativeActionResult.Completed("scripted-shop-purchase-complete");
        public NetherNativeActionResult CodeReloadTaskPollResult { get; set; } =
            NetherNativeActionResult.Completed("scripted-code-reload-complete");
        public NetherNativeActionResult CodeKeepTaskPollResult { get; set; } =
            NetherNativeActionResult.Completed("scripted-code-keep-cancel-complete");
        public NetherNativeActionResult CodeTransformTaskPollResult { get; set; } =
            NetherNativeActionResult.Completed("scripted-code-transform-complete");
        public int GetOnlyBeginCount { get; private set; }
        public int GetOnlyPollCount { get; private set; }
        public int ContinuePreflightCount { get; private set; }
        public int ContinueNativeInvokeCount { get; private set; }
        public int ContinueReadOnlyBeginCount { get; private set; }
        public int ResultPollCount { get; private set; }
        public int FinishParentPollCount { get; private set; }
        public int FloorSceneSnapshotCaptureCount { get; private set; }
        public int FloorParentPollCount { get; private set; }
        public int FloorParentTerminalCount { get; private set; }
        public int ContinueParentPollCount { get; private set; }
        public List<string> Trace { get; } = new();
        public bool ContinueParentCompleted { get; set; }
        public bool FinishParentCompleted { get; set; }
        public Queue<NetherNativeActionResult> ResultFlowSteps { get; } = new();
        /// <summary>
        /// Opt-in truthful parent-task seam for modal E2E tests.  The default remains eager for
        /// legacy fixtures, while transaction tests can prove a child/close never fabricates
        /// the SelectFloor parent terminal or starts GET early.
        /// </summary>
        public bool RequireExplicitFloorParentTerminal { get; set; }
        public bool FloorParentCompleted { get; set; }
        public bool FloorOwnerTerminated { get; set; }
        public long CurrentRuntimeGeneration { get; set; } = 1;
        public bool FloorSceneEntered { get; set; } = true;
        public bool FloorSceneHasAuthoritativeSnapshot { get; set; } = true;
        public bool DropFloorSceneReadinessOnNextGetPoll { get; set; }
        public bool DelayBattleSnapshotUntilStartTerminal { get; set; }
        public bool BattleStartRegistered { get; set; }
        public bool BattleStartCompleted { get; set; }
        public bool HoldBattleOpen { get; set; }
        public int BattleStartCancelCount { get; private set; }
        public int BattleResultNextInvokeCount { get; private set; }
        public bool HasObservedNetherBattleResult { get; set; }
        public bool AutoCompleteBattleResultContinuation { get; set; } = true;
        public bool BattleResultRebound { get; set; } = true;
        public bool BattleResultReboundSceneEntered { get; set; } = true;
        public NetherSnapshot? BattleResultReboundSnapshot { get; set; }
        public NetherRuntimePopupContext? BattleResultReboundPopup { get; set; }
        public NetherSnapshot? BattleSettlementSnapshotOverride { get; set; }
        public NetherRuntimePopupContext? BattleResultCodePopup { get; set; }
        public List<NetherPlannedAction> BattleResultCodeActions { get; } = new();
        public Queue<NetherBattleResultCodeNativeStep> BattleResultCodeNativeSteps { get; } = new();
        public bool HasRecoveredCodeOffer { get; set; }
        public NetherRuntimePopupContext? RecoveredCodePopup { get; set; }
        public List<NetherPlannedAction> RecoveredCodeActions { get; } = new();
        public Queue<NetherBattleResultCodeNativeStep> RecoveredCodeNativeSteps { get; } = new();
        public Queue<NetherNativeActionResult> RecoveredCodeParentSteps { get; } = new();
        public Queue<NetherNativeActionResult> RecoveredCodeRefreshSteps { get; } = new();
        public NetherSnapshot? RecoveredCodeAppliedSnapshot { get; set; }
        public int RecoveredCodeParentPollCount { get; private set; }
        public int RecoveredCodeCompletedCount { get; private set; }
        public NetherRecoveredCheckpointObservation RecoveredCheckpointObservation { get; set; } =
            NetherRecoveredCheckpointObservation.NotObserved("scripted-checkpoint-not-observed");
        public int RecoveredCheckpointPollCount { get; private set; }
        public int RecoveredCheckpointHandoffCount { get; private set; }

        private readonly Queue<NetherRuntimePopupContext> _queuedOwnedPopups = new();
        private readonly Queue<NetherSnapshot?> _queuedOwnedPopupSnapshots = new();
        private readonly Queue<(NetherSnapshot Snapshot, NetherRuntimeCodeCandidatesResult Candidates)> _queuedCodeReloadRefreshes = new();

        public bool HasRegisteredFloorSelection { get; set; } = true;
        public bool IsBattleActive => CurrentSnapshot.Status == NetherSessionStatus.Battle;
        public bool IsResultObserved => CurrentSnapshot.Status == NetherSessionStatus.Clear;
        public bool IsExpectedNetherTopScene => true;

        public NetherFloorSceneSnapshotResult TryCaptureReadyFloorSceneSnapshot(
            long minimumGenerationExclusive = 0
        )
        {
            FloorSceneSnapshotCaptureCount++;
            NetherFloorSceneReadinessDecision readiness = NetherFloorSceneReadiness.Evaluate(new(
                minimumGenerationExclusive,
                CurrentRuntimeGeneration,
                HasCurrentController: HasRegisteredFloorSelection,
                IsExpectedCurrentController: IsExpectedNetherTopScene,
                HasEnteredCurrentGeneration: FloorSceneEntered,
                HasAuthoritativeSnapshot: FloorSceneHasAuthoritativeSnapshot,
                CaptureStayedOnCurrentController: true
            ));
            return readiness.IsReady
                ? NetherFloorSceneSnapshotResult.Ready(
                    CurrentRuntimeGeneration,
                    CaptureSnapshotWithCurrentNode()
                )
                : NetherFloorSceneSnapshotResult.Waiting(
                    CurrentRuntimeGeneration,
                    readiness.Detail
                );
        }

        public NetherRuntimeSnapshotResult TryCaptureSnapshot() =>
            NetherRuntimeSnapshotResult.Success(CaptureSnapshotWithCurrentNode());

        private NetherSnapshot CaptureSnapshotWithCurrentNode()
        {
            // The production visible-vector gate requires the authoritative current node to be
            // present in the captured floor graph.  Older managed DTO fixtures omitted that field;
            // materialize it at the public capture seam from the current floor, without enabling
            // the legacy comparator or inferring any reward/battle semantic tier.
            if (CurrentSnapshot.CurrentNodeId > 0)
                return CurrentSnapshot;

            NetherFloorNode? currentFloor = CurrentSnapshot.Floors.FirstOrDefault(floor =>
                floor.FloorId == CurrentSnapshot.CurrentFloorId
            ) ?? CurrentSnapshot.Floors.FirstOrDefault();
            return currentFloor == null
                ? CurrentSnapshot
                : CurrentSnapshot with { CurrentNodeId = currentFloor.NodeId };
        }

        public NetherRuntimeStrategyEvidenceResult TryCaptureStrategyEvidence(
            NetherSnapshot snapshot,
            NetherAutoClimbSettings settings
        )
        {
            NetherStrategyVisibleMapEvidence? visibleMap =
                BindProviderBackedCanonicalEvidence(
                    CaptureVisibleMap(snapshot),
                    _latestTypedSemanticProvider
                );
            NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
                new NetherStrategyEvidenceMapRequest(
                    new NetherStrategyEvidenceIdentity(
                        CurrentRuntimeGeneration,
                        CurrentRuntimeGeneration,
                        CurrentRuntimeGeneration,
                        snapshot.Fingerprint
                    ),
                    snapshot
                )
                {
                    VisibleMap = visibleMap,
                }
            );
            return mapped.IsMapped
                ? NetherRuntimeStrategyEvidenceResult.Success(mapped.Package!)
                : NetherRuntimeStrategyEvidenceResult.Failure(mapped.Detail);
        }

        public void BeginRouteReplan(NetherSnapshotFingerprint snapshotFingerprint)
        {
            _routeOwnedEventProcurementProducer.Clear();
            BoundEventProcurementCommitments =
                new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>();
        }

        public void ObserveAuthoritativeRouteSnapshot(NetherSnapshotFingerprint snapshotFingerprint) =>
            _routeOwnedEventProcurementProducer.InvalidateForSnapshot(snapshotFingerprint);

        public NetherRuntimeRouteSafetyData TryCaptureRouteSafety(NetherSnapshot snapshot)
        {
            NetherStrategyTypedSemanticProviderEvidence? provider = null;
            if (_typedSemanticProviderFactory != null)
            {
                try
                {
                    NetherRuntimeTypedSemanticProviderScope? scope = _typedSemanticProviderFactory(snapshot);
                    if (scope?.Evidence != null && scope.SnapshotFingerprint == snapshot.Fingerprint)
                        provider = scope.Evidence;
                }
                catch
                {
                    provider = null;
                }
            }
            NetherRuntimeRouteSafetyData captured = TryCaptureRouteSafety(snapshot.Floors);
            return captured with
            {
                EventProcurementCommitments = _routeOwnedEventProcurementProducer.CaptureForSnapshot(
                    snapshot.Fingerprint
                ),
                RouteIdentity = _routeOwnedEventProcurementProducer.IdentityForSnapshot(snapshot.Fingerprint),
                VisibleMap = BindProviderBackedCanonicalEvidence(
                    CaptureVisibleMap(snapshot) ?? captured.VisibleMap,
                    provider
                ),
            };
        }

        public NetherRuntimeRouteSafetyData TryCaptureRouteSafety(IReadOnlyList<NetherFloorNode> floors)
        {
            NetherRuntimeRouteSafetyData captured = RouteSafetyOverride ?? new()
            {
                FloorBoundsByFloorId = floors
                    .Where(floor => floor.NodeType is NetherFloorNodeType.Battle
                        or NetherFloorNodeType.MiniBoss
                        or NetherFloorNodeType.Boss)
                    .GroupBy(floor => floor.NodeId)
                    .Where(group => group.Count() == 1)
                    .ToDictionary(
                        group => group.Key,
                        group => new NetherFloorMasterBounds(
                            group.Single().FloorId,
                            0,
                            0,
                            IsKnown: true,
                            Detail: string.Empty
                        )
                    ),
                ActivePartyHp = NetherRouteSafetyHpTestEvidence.Single(1, 1000),
                ActiveCodeErosion = ActiveCodeErosion,
                EventProcurementCommitments = _routeOwnedEventProcurementProducer.Capture(),
                VisibleMap = VisibleMap,
                // Default scripted routes model an authoritative completed Equipment state;
                // native-unknown Research is covered by explicit null fixtures.
                ResearchIncomplete = false,
            };
            if (!BindRouteSafetyHpToCurrentSnapshot)
                return captured;

            int[] activeHp = CurrentSnapshot.Characters
                .Where(character => character.IsActive)
                .Select(character => character.HpPermille)
                .ToArray();
            return captured with
            {
                ActivePartyHp = activeHp.Length == 0
                    ? new NetherActivePartyHpSafety(false, null, "scripted-active-party-empty")
                    : NetherRouteSafetyHpTestEvidence.FromStates(CurrentSnapshot.Characters),
            };
        }

        public IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
            CaptureRouteOwnedEventProcurementCommitments() =>
            _routeOwnedEventProcurementProducer.Capture();

        public void CommitRouteOwnedEventProcurementCommitments(
            IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? commitments
        ) => _routeOwnedEventProcurementProducer.Commit(commitments);

        public void CommitRouteOwnedEventProcurementCommitments(
            NetherRouteBranchIdentity identity,
            IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? commitments
        ) => _routeOwnedEventProcurementProducer.Commit(identity, commitments);

        public void BindEventProcurementCommitments(
            IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>? commitments
        ) => BoundEventProcurementCommitments = commitments == null
            ? new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>()
            : new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>(commitments);

        public void BindRecoveryBranchSafetyProofs(
            IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence>? proofs
        )
        {
            RecoveryBranchSafetyBindCount++;
            BoundRecoveryBranchSafetyByPartId = proofs == null
                ? new Dictionary<long, NetherRecoveryBranchSafetyEvidence>()
                : new Dictionary<long, NetherRecoveryBranchSafetyEvidence>(proofs);
        }

        public void BindRankFiveKeyProcurement(NetherRankFiveKeyProcurementDecision? decision)
        {
            RankFiveKeyProcurementBindCount++;
            BoundRankFiveKeyProcurement = decision;
        }

        public NetherRuntimeInteractivePreEntryInputsResult TryCaptureInteractivePreEntryInputs(
            NetherSnapshot snapshot,
            NetherAutoClimbSettings settings
        )
        {
            InteractivePreEntryCaptureCount++;
            NetherRuntimeInteractivePreEntryInputsResult captured = InteractivePreEntryFactory?.Invoke(snapshot, settings)
                ?? (ProvideCompleteVisibleBranchEvidence
                    ? CompleteInteractivePreEntry(snapshot, settings)
                    : NetherRuntimeInteractivePreEntryInputsResult.Failure("e2e-no-route-interactive-master-needed"));
            if (!captured.IsSuccess)
                return captured;

            NetherStrategyTypedSemanticProviderEvidence? typedSemanticProvider = null;
            if (_typedSemanticProviderFactory != null)
            {
                try
                {
                    NetherRuntimeTypedSemanticProviderScope? scope = _typedSemanticProviderFactory(snapshot);
                    if (scope?.Evidence != null && scope.SnapshotFingerprint == snapshot.Fingerprint)
                        typedSemanticProvider = scope.Evidence;
                }
                catch
                {
                    typedSemanticProvider = null;
                }
            }
            _latestTypedSemanticProvider = typedSemanticProvider ?? captured.TypedSemanticProvider;

            bool dropRecoveryProof = DropBoundRecoveryProofOnNextCapture
                || DropBoundRecoveryProofOnCaptureNumber == InteractivePreEntryCaptureCount;
            DropBoundRecoveryProofOnNextCapture = false;
            if (DropBoundRecoveryProofOnCaptureNumber == InteractivePreEntryCaptureCount)
                DropBoundRecoveryProofOnCaptureNumber = 0;
            var entries = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>();
            foreach ((long nodeId, NetherRuntimeInteractivePreEntryCaptureResult entry) in captured.ByFloorNodeId)
            {
                NetherInteractiveFloorPreEntrySafetyInput? input = entry.Input;
                if (input == null)
                {
                    entries[nodeId] = entry;
                    continue;
                }
                entries[nodeId] = entry with
                {
                    Input = input with
                    {
                        RecoveryBranchSafetyByPartId = dropRecoveryProof
                            ? new Dictionary<long, NetherRecoveryBranchSafetyEvidence>()
                            : RecoveryBranchSafetyBindCount > 0
                                ? new Dictionary<long, NetherRecoveryBranchSafetyEvidence>(
                                    BoundRecoveryBranchSafetyByPartId
                                )
                                : new Dictionary<long, NetherRecoveryBranchSafetyEvidence>(
                                    input.RecoveryBranchSafetyByPartId
                                ),
                        RequireCompleteRecoveryBranchSafety = RecoveryBranchSafetyBindCount > 0,
                        RankFiveKeyProcurement = BoundRankFiveKeyProcurement,
                        TypedSemanticProvider = typedSemanticProvider ?? input.TypedSemanticProvider,
                    },
                };
            }
            return captured with
            {
                SnapshotFingerprint = captured.SnapshotFingerprint ?? snapshot.Fingerprint,
                ByFloorNodeId = entries,
                TypedSemanticProvider = typedSemanticProvider ?? captured.TypedSemanticProvider,
            };
        }

        public NetherRuntimeCodeCandidatesResult TryGetCodeCandidates() => CodeCandidates;

        public NetherRuntimeSnapshotResult TryCaptureBattleResultCodeSnapshot() =>
            NetherRuntimeSnapshotResult.Success(CurrentSnapshot);

        public NetherRuntimePopupResult TryGetBattleResultCodePopup() =>
            BattleResultCodePopup == null
                ? NetherRuntimePopupResult.Failure("scripted-result-code-popup-missing")
                : NetherRuntimePopupResult.Success(BattleResultCodePopup with
                {
                    RuntimeGeneration = BattleResultCodePopup.RuntimeGeneration > 0
                        ? BattleResultCodePopup.RuntimeGeneration
                        : CurrentRuntimeGeneration,
                });

        public NetherRuntimeCodePolicyEvidenceResult TryCaptureCodePolicyEvidence(
            NetherSnapshot snapshot,
            NetherRuntimeCodeCandidatesResult candidates,
            NetherAutoClimbSettings settings
        ) => CodePolicyEvidenceFactory?.Invoke(snapshot, candidates, settings)
            ?? NetherRuntimeCodePolicyEvidenceResult.Success(
                ScriptedCodePolicyEvidence(snapshot, candidates, settings)
            );

        private static NetherCodePolicyEvidence ScriptedCodePolicyEvidence(
            NetherSnapshot snapshot,
            NetherRuntimeCodeCandidatesResult candidates,
            NetherAutoClimbSettings settings
        )
        {
            NetherNativeBuffWindow[] before = snapshot.Codes
                .Where(code => code.PossessionAmount > 0)
                .Select(code => ScriptedCodeWindow(code.CodeId, 50))
                .ToArray();
            var mutations = new Dictionary<
                NetherCodeMutationKey,
                NetherCodeEquipmentMutationEvidence
            >();
            foreach (NetherCodeCandidate candidate in candidates.Candidates)
            {
                IEnumerable<long> removals = snapshot.Codes.Count < snapshot.CodeCapacity
                    ? [0]
                    : snapshot.Codes.Select(code => code.CodeId);
                foreach (long removal in removals)
                {
                    if (removal > 0
                        && snapshot.Codes.FirstOrDefault(code => code.CodeId == removal) is NetherCodeState held
                        && IsOpposingFamily(candidate.Family, held.Family))
                    {
                        continue;
                    }
                    NetherNativeBuffWindow[] after = before
                        .Where(window => window.CodeId != removal)
                        .Append(ScriptedCodeWindow(candidate.CodeId, 100))
                        .ToArray();
                    mutations[new NetherCodeMutationKey(candidate.CodeId, removal)] = new(
                        candidate.CodeId,
                        removal,
                        new NetherNativePortfolioComparisonInput(before, after, 10),
                        KnownZeroMechanism()
                    )
                    {
                        CombatTier = NetherEquipmentCombatTier.RearOrFullOffense,
                        Survival = NetherSurvivalRepairEvidence.Known(false, false),
                        MechanismPortfolio = NetherMechanismPortfolioComparisonEvidence.Known(
                            [],
                            []
                        ),
                        RecipientPositions = new Dictionary<long, NetherPartyPosition>
                        {
                            [1] = NetherPartyPosition.Back,
                        },
                    };
                }
            }
            NetherCodeFamily activeFamily = settings.StrategyMode == NetherStrategyMode.Research
                ? settings.ResearchPrimaryFamily
                : NetherCodeFamily.Unknown;
            return new NetherCodePolicyEvidence
            {
                MechanicsByCodeId = candidates.Candidates.ToDictionary(
                    candidate => candidate.CodeId,
                    _ => new NetherCodeHardEligibilityEvidence { IsKnown = true }
                ),
                MechanismValuesByCodeId = candidates.Candidates.ToDictionary(
                    candidate => candidate.CodeId,
                    _ => KnownZeroMechanism()
                ),
                EquipmentMutationValuesByKey = mutations,
                ActiveParty = snapshot.Characters
                    .Where(character => character.IsActive)
                    .Select((character, index) => new NetherStrategyPartyMember(
                        character.CharacterId,
                        index,
                        NetherPartyPosition.Back,
                        1,
                        NetherCrestIdentity.Impact,
                        character.HpPermille,
                        true,
                        1,
                        0
                    ))
                    .ToArray(),
                Research = new[]
                {
                    new NetherStrategyResearchFamilyState(NetherCodeFamily.Rush, 0, 0, 0),
                    new NetherStrategyResearchFamilyState(NetherCodeFamily.Impact, 0, 0, 0),
                    new NetherStrategyResearchFamilyState(NetherCodeFamily.Safe, 0, 0, 0),
                    new NetherStrategyResearchFamilyState(NetherCodeFamily.Risk, 0, 0, 0),
                },
                ActiveResearchFamily = activeFamily,
                ErosionHorizonKnown = true,
                ProjectedMinimumErosion = snapshot.ErosionPoint,
                ProjectedMaximumErosion = snapshot.ErosionPoint,
                RecoverableToFiftySeventyBand = true,
            };
        }

        private static bool IsOpposingFamily(NetherCodeFamily candidate, NetherCodeFamily held) =>
            candidate == NetherCodeFamily.Rush && held == NetherCodeFamily.Impact
            || candidate == NetherCodeFamily.Impact && held == NetherCodeFamily.Rush
            || candidate == NetherCodeFamily.Safe && held == NetherCodeFamily.Risk
            || candidate == NetherCodeFamily.Risk && held == NetherCodeFamily.Safe;

        private static NetherMechanismValue KnownZeroMechanism() =>
            NetherMechanismValue.Quantified(
                NetherMechanismQuantityKind.None,
                0,
                "scripted-known-zero-mechanism"
            );

        private static NetherNativeBuffWindow ScriptedCodeWindow(long codeId, int value) => new(
            codeId,
            RecipientCharacterId: 1,
            new NetherStrategyBuffType(10),
            NetherStrategyBuffEffectKind.Buff,
            NetherStrategyBuffCoexistenceKind.Allow,
            NetherCombatMetricKind.Attack,
            value,
            StartSecond: 0,
            DurationSeconds: 10
        );

        public NetherNativeActionResult InvokeBattleResultCode(
            NetherRuntimePopupContext popup,
            NetherPlannedAction action
        )
        {
            BattleResultCodeActions.Add(action);
            return NetherNativeActionResult.Started("scripted-result-code-invoked");
        }

        public NetherBattleResultCodeNativeStep PollBattleResultCodeNative() =>
            BattleResultCodeNativeSteps.Count == 0
                ? NetherBattleResultCodeNativeStep.Pending("scripted-result-code-pending")
                : BattleResultCodeNativeSteps.Dequeue();

        public NetherRuntimeSnapshotResult TryCaptureRecoveredCodeSnapshot() =>
            NetherRuntimeSnapshotResult.Success(CurrentSnapshot);

        public NetherRuntimeCodeCandidatesResult TryGetRecoveredCodeCandidates() => CodeCandidates;

        public NetherRuntimeCodePolicyEvidenceResult TryCaptureRecoveredCodePolicyEvidence(
            NetherSnapshot snapshot,
            NetherRuntimeCodeCandidatesResult candidates,
            NetherAutoClimbSettings settings
        ) => TryCaptureCodePolicyEvidence(snapshot, candidates, settings);

        public NetherRuntimePopupResult TryGetRecoveredCodePopup() => RecoveredCodePopup == null
            ? NetherRuntimePopupResult.Failure("scripted-recovered-code-popup-missing")
            : NetherRuntimePopupResult.Success(RecoveredCodePopup with
            {
                RuntimeGeneration = RecoveredCodePopup.RuntimeGeneration > 0
                    ? RecoveredCodePopup.RuntimeGeneration
                    : CurrentRuntimeGeneration,
            });

        public NetherNativeActionResult InvokeRecoveredCode(
            NetherRuntimePopupContext popup,
            NetherPlannedAction action
        )
        {
            RecoveredCodeActions.Add(action);
            return NetherNativeActionResult.Started("scripted-recovered-code-invoked");
        }

        public NetherBattleResultCodeNativeStep PollRecoveredCodeNative() =>
            RecoveredCodeNativeSteps.Count == 0
                ? NetherBattleResultCodeNativeStep.Pending("scripted-recovered-code-pending")
                : RecoveredCodeNativeSteps.Dequeue();

        public NetherNativeActionResult PollRecoveredCodeParent()
        {
            RecoveredCodeParentPollCount++;
            return RecoveredCodeParentSteps.Count == 0
                ? NetherNativeActionResult.Started("scripted-recovered-parent-pending")
                : RecoveredCodeParentSteps.Dequeue();
        }

        public NetherRecoveredCheckpointObservation ObserveRecoveredCheckpoint()
        {
            RecoveredCheckpointPollCount++;
            return RecoveredCheckpointObservation.Snapshot is NetherSnapshot snapshot
                ? RecoveredCheckpointObservation with
                {
                    Snapshot = CaptureSnapshotWithCurrentNodeFor(snapshot),
                }
                : RecoveredCheckpointObservation;
        }

        private NetherSnapshot CaptureSnapshotWithCurrentNodeFor(NetherSnapshot snapshot)
        {
            if (snapshot.CurrentNodeId > 0)
                return snapshot;
            NetherFloorNode? currentFloor = snapshot.Floors.FirstOrDefault(floor =>
                floor.FloorId == snapshot.CurrentFloorId
            ) ?? snapshot.Floors.FirstOrDefault();
            return currentFloor == null
                ? snapshot
                : snapshot with { CurrentNodeId = currentFloor.NodeId };
        }

        public NetherNativeActionResult PrepareRecoveredCheckpointHandoff()
        {
            RecoveredCheckpointHandoffCount++;
            if (RecoveredCheckpointObservation.Kind != NetherRecoveredCheckpointObservationKind.Ready
                || RecoveredCheckpointObservation.Snapshot == null)
            {
                return NetherNativeActionResult.BindingUnavailable(
                    "scripted-recovered-checkpoint-not-ready"
                );
            }

            CurrentSnapshot = RecoveredCheckpointObservation.Snapshot;
            RecoveredCheckpointObservation = NetherRecoveredCheckpointObservation.NotObserved(
                "scripted-checkpoint-handoff-already-prepared"
            );
            HasRecoveredCodeOffer = false;
            RecoveredCodePopup = null;
            return NetherNativeActionResult.Completed("scripted-recovered-checkpoint-handoff");
        }

        public NetherNativeActionResult BeginRecoveredCodeRefresh()
        {
            GetOnlyBeginCount++;
            return NetherNativeActionResult.Started("scripted-recovered-get-started");
        }

        public NetherNativeActionResult PollRecoveredCodeRefresh()
        {
            GetOnlyPollCount++;
            return RecoveredCodeRefreshSteps.Count == 0
                ? NetherNativeActionResult.Started("scripted-recovered-get-pending")
                : RecoveredCodeRefreshSteps.Dequeue();
        }

        public NetherRuntimeSnapshotResult TryCaptureRecoveredCodeAppliedSnapshot() =>
            NetherRuntimeSnapshotResult.Success(RecoveredCodeAppliedSnapshot ?? CurrentSnapshot);

        public void CompleteRecoveredCodeOffer()
        {
            HasRecoveredCodeOffer = false;
            RecoveredCodePopup = null;
            RecoveredCodeCompletedCount++;
        }

        public NetherRuntimePopupResult TryGetActivePopup() => ActivePopupResultOverride
            ?? (ActivePopup == null
                ? NetherRuntimePopupResult.Failure("missing-active-native-popup")
                : NetherRuntimePopupResult.Success(ActivePopup));

        public NetherRuntimePopupResult TryGetOwnedPopup(NetherPlannedAction parent)
        {
            if (OwnedPopup != null)
            {
                NetherRuntimePopupContext popup = OwnedPopup with
                {
                    RuntimeGeneration = OwnedPopup.RuntimeGeneration > 0
                        ? OwnedPopup.RuntimeGeneration
                        : CurrentRuntimeGeneration,
                };
                if (popup.Kind == NetherRuntimePopupKind.CodeOffer)
                {
                    popup = popup with
                    {
                        DecisionEpoch = _ownedPopupStageEntry.GetDecisionEpoch(
                            new NetherOwnedPopupStageOwner(
                                popup.OwnerAction,
                                popup.OwnerGeneration,
                                popup.Sequence,
                                0
                            )
                        ),
                    };
                }
                return NetherRuntimePopupResult.Success(popup);
            }
            return _queuedOwnedPopups.Count == 0
                ? NetherRuntimePopupResult.Failure("missing-owned-floor-popup")
                : NetherRuntimePopupResult.Success(_queuedOwnedPopups.Peek() with
                {
                    RuntimeGeneration = _queuedOwnedPopups.Peek().RuntimeGeneration > 0
                        ? _queuedOwnedPopups.Peek().RuntimeGeneration
                        : CurrentRuntimeGeneration,
                });
        }

        public void EnqueueOwnedPopup(NetherRuntimePopupContext popup, NetherSnapshot? snapshotAfterInvoke)
        {
            _queuedOwnedPopups.Enqueue(popup);
            _queuedOwnedPopupSnapshots.Enqueue(snapshotAfterInvoke);
        }

        public void EnqueueCodeReloadRefresh(
            NetherSnapshot snapshot,
            NetherRuntimeCodeCandidatesResult candidates
        ) => _queuedCodeReloadRefreshes.Enqueue((snapshot, candidates));

        public bool BeginFloorParent(NetherPlannedAction action, long generation)
        {
            BeginFloorParentCount++;
            Trace.Add("floor-parent-register");
            _floorParentPending = true;
            return action.Kind == NetherActionKind.SelectFloor && generation > 0;
        }

        public void TerminateFloorParent() => _floorParentPending = false;

        public NetherNativeActionResult InvokeOwnedPopup(
            NetherPlannedAction parent,
            NetherRuntimePopupContext popup,
            NetherPlannedAction action
        )
        {
            OwnedPopupInvokeCount++;
            OwnedPopupActions.Add(action);
            return _ownedPopupStageEntry.Dispatch(
                parent,
                popup,
                action,
                DispatchScriptedNonStagePopup,
                DispatchScriptedLeaveShop,
                DispatchScriptedNonStagePopup
            );
        }

        private NetherNativeActionResult DispatchScriptedLeaveShop()
        {
            ShopLeaveInvokeCount++;
            return DispatchScriptedNonStagePopup(new NetherPlannedAction(NetherActionKind.LeaveShop));
        }

        private NetherNativeActionResult DispatchScriptedNonStagePopup(NetherPlannedAction action)
        {
            if (OwnedPopup != null)
            {
                OwnedPopup = null;
                if (OwnedPopupAfterSnapshot != null)
                    CurrentSnapshot = OwnedPopupAfterSnapshot;
            }
            else if (_queuedOwnedPopups.Count > 0)
            {
                _queuedOwnedPopups.Dequeue();
                NetherSnapshot? snapshotAfterInvoke = _queuedOwnedPopupSnapshots.Dequeue();
                if (snapshotAfterInvoke != null)
                    CurrentSnapshot = snapshotAfterInvoke;
            }
            else
            {
                return NetherNativeActionResult.BindingUnavailable("missing-scripted-owned-popup");
            }
            return NetherNativeActionResult.Started("native-owned-popup:" + action.Kind);
        }

        public NetherNativeActionResult Reconcile() => NetherNativeActionResult.Started("unused-direct-reconcile");

        public NetherNativeActionResult Invoke(NetherPlannedAction action)
        {
            NativeActions.Add(action);
            Invocations.Add(action.Kind);
            switch (action.Kind)
            {
                case NetherActionKind.StartRun:
                    return NetherNativeActionResult.Started(
                        "native-run-start-floor:" + action.FloorLevel
                    );
                case NetherActionKind.SelectEventOption:
                    // Keep the old object visible to the fake native layer.  The production
                    // Controller must follow authoritative Play state rather than replay it.
                    ActivePopup = InteractivePopup;
                    CurrentSnapshot = AfterInteractive;
                    _eventNativePending = true;
                    return NetherNativeActionResult.Started("native-event-option");
                case NetherActionKind.SelectFloor:
                    if (!DelayBattleSnapshotUntilStartTerminal)
                        CurrentSnapshot = FloorSelectionDispatchSnapshot ?? BattleSnapshot;
                    return NetherNativeActionResult.Started("native-select-floor-parent");
                case NetherActionKind.Continue:
                    ContinueNativeInvokeCount++;
                    Trace.Add("continue-native-invoke");
                    return NetherNativeActionResult.Started("native-continue-parent");
                case NetherActionKind.FinishAtCheckpoint:
                    _finishNativePending = true;
                    return NetherNativeActionResult.Started("native-finish-parent");
                case NetherActionKind.SelectCode:
                    ActivePopup = null;
                    return NetherNativeActionResult.Started("native-select-code");
                default:
                    return NetherNativeActionResult.BindingUnavailable("unexpected-action:" + action.Kind);
            }
        }

        public NetherNativeActionResult PollNativeFlow()
        {
            if (_finishNativePending)
            {
                FinishParentPollCount++;
                if (!FinishParentCompleted)
                    return NetherNativeActionResult.Started("native-finish-parent-pending");
                _finishNativePending = false;
                return NetherNativeActionResult.Completed("native-finish-parent-terminal");
            }
            if (!_eventNativePending)
                return NetherNativeActionResult.Started("no-direct-native-terminal-yet");
            _eventNativePending = false;
            return NetherNativeActionResult.Completed("native-event-option-terminal");
        }

        public NetherNativeActionResult PollFloorParent()
        {
            FloorParentPollCount++;
            if (!_floorParentPending)
                return NetherNativeActionResult.BindingUnavailable("missing-floor-parent");
            NetherOwnedPopupStageParentGate stage = _ownedPopupStageEntry.PumpBeforeParent();
            if (!stage.MayPollParent)
                return stage.Native;
            if (OwnedPopup != null || _queuedOwnedPopups.Count > 0)
                return NetherNativeActionResult.Started("native-floor-parent-awaiting-owned-popup");
            if (RequireExplicitFloorParentTerminal && !FloorParentCompleted)
                return NetherNativeActionResult.Started("native-floor-parent-still-pending");
            FloorParentTerminalCount++;
            return NetherNativeActionResult.Completed("native-floor-parent-terminal");
        }

        bool INetherOwnedPopupNativeStagePort.IsCurrentOwnedPopup(
            NetherRuntimePopupKind kind,
            NetherOwnedPopupStageOwner owner
        )
        {
            NetherRuntimePopupContext? popup = OwnedPopup ?? (
                _queuedOwnedPopups.Count > 0 ? _queuedOwnedPopups.Peek() : null
            );
            return _floorParentPending
                && popup != null
                && popup.Kind == kind
                && popup.OwnerAction == owner.OwnerAction
                && popup.OwnerGeneration == owner.Generation
                && popup.Sequence == owner.Sequence;
        }

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.InvokeShopPurchase(
            NetherOwnedPopupStageOwner owner,
            NetherPlannedAction action
        ) => NetherNativeActionResult.Started("scripted-shop-purchase-invoked");

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.PollShopPurchaseTask(
            NetherShopPurchaseCloseOwner owner
        )
        {
            NetherNativeActionResult result = ShopPurchaseChildPollResult;
            if (result.Kind == NetherNativeActionResultKind.Completed && !_shopPurchaseSnapshotApplied)
            {
                _shopPurchaseSnapshotApplied = true;
                if (OwnedPopupAfterSnapshot != null)
                    CurrentSnapshot = OwnedPopupAfterSnapshot;
            }
            return result;
        }

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.InvokeShopPurchaseConfirm(
            NetherShopPurchaseCloseOwner owner
        ) => NetherNativeActionResult.Completed("scripted-shop-confirm-invoked");

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.InvokeExactShopClose(
            NetherShopPurchaseCloseOwner owner
        )
        {
            ShopCloseInvokeCount++;
            OwnedPopup = null;
            return NetherNativeActionResult.Started("scripted-shop-close");
        }

        NetherOwnedPopupCodeReloadStart INetherOwnedPopupNativeStagePort.CaptureCodeReloadStart(
            NetherOwnedPopupStageOwner owner
        ) => new(CurrentSnapshot.CodeReloadCount, CodeCandidates, string.Empty);

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.InvokeCodeReload(
            NetherCodeReloadEpochOwner owner
        )
        {
            CodeReloadInvokeCount++;
            return NetherNativeActionResult.Started("scripted-code-reload-invoked");
        }

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.PollCodeReloadTask(
            NetherCodeReloadEpochOwner owner
        )
        {
            NetherNativeActionResult result = CodeReloadTaskPollResult;
            if (result.Kind == NetherNativeActionResultKind.Completed && _queuedCodeReloadRefreshes.Count > 0)
            {
                (NetherSnapshot snapshot, NetherRuntimeCodeCandidatesResult candidates) = _queuedCodeReloadRefreshes.Dequeue();
                CurrentSnapshot = snapshot;
                CodeCandidates = candidates;
            }
            else if (result.Kind == NetherNativeActionResultKind.Completed && !_codeReloadSnapshotApplied)
            {
                _codeReloadSnapshotApplied = true;
                if (CodeReloadAfterSnapshot != null)
                    CurrentSnapshot = CodeReloadAfterSnapshot;
                CodeCandidates = ReloadCodeCandidates;
            }
            return result;
        }

        NetherCodeReloadEpochRefresh INetherOwnedPopupNativeStagePort.CaptureFreshCodeReloadOffer(
            NetherCodeReloadEpochOwner owner
        ) => new(owner, CurrentSnapshot.CodeReloadCount, CodeCandidates);

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.InvokeCodeKeepCancel(
            NetherCodeKeepCancelOwner owner
        )
        {
            CodeKeepInvokeCount++;
            return _ownedPopupStageEntry.ObserveKeepCancelTask(owner)
                ? NetherNativeActionResult.Started("scripted-code-keep-cancel-invoked")
                : NetherNativeActionResult.BindingUnavailable("scripted-code-keep-task-observer-unavailable");
        }

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.PollCodeKeepCancelTask(
            NetherCodeKeepCancelOwner owner
        )
        {
            NetherNativeActionResult result = CodeKeepTaskPollResult;
            if (result.Kind == NetherNativeActionResultKind.Completed && !_codeKeepSnapshotApplied)
            {
                _codeKeepSnapshotApplied = true;
                OwnedPopup = null;
                if (OwnedPopupAfterSnapshot != null)
                    CurrentSnapshot = OwnedPopupAfterSnapshot;
            }
            return result;
        }

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.InvokeCodeTransform(
            NetherCodeTransformOwner owner
        )
        {
            CodeTransformInvokeCount++;
            return _ownedPopupStageEntry.ObserveCodeTransformTask(owner)
                ? NetherNativeActionResult.Started("scripted-code-transform-invoked")
                : NetherNativeActionResult.BindingUnavailable("scripted-code-transform-observer-unavailable");
        }

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.InvokeCodeTransformConfirm(
            NetherCodeTransformOwner owner
        )
        {
            CodeTransformConfirmCount++;
            return NetherNativeActionResult.Completed("scripted-code-transform-confirmed");
        }

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.InvokeCodeTransformCompleteClose(
            NetherCodeTransformOwner owner
        )
        {
            CodeTransformCompleteCount++;
            return NetherNativeActionResult.Completed("scripted-code-transform-complete-closed");
        }

        NetherNativeActionResult INetherOwnedPopupNativeStagePort.PollCodeTransformTask(
            NetherCodeTransformOwner owner
        )
        {
            CodeTransformTaskPollCount++;
            NetherNativeActionResult result = CodeTransformTaskPollResult;
            if (result.Kind == NetherNativeActionResultKind.Completed
                && _queuedOwnedPopups.Count > 0
                && _queuedOwnedPopups.Peek().Kind == NetherRuntimePopupKind.CodeTransform)
            {
                _queuedOwnedPopups.Dequeue();
                NetherSnapshot? snapshotAfterTransform = _queuedOwnedPopupSnapshots.Dequeue();
                if (snapshotAfterTransform != null)
                    CurrentSnapshot = snapshotAfterTransform;
            }
            return result;
        }

        public NetherNativeActionResult BeginGetOnlyRefresh()
        {
            GetOnlyBeginCount++;
            if (ContinueNativeInvokeCount > ContinueReadOnlyBeginCount)
                ContinueReadOnlyBeginCount++;
            return NetherNativeActionResult.Started("native-get-only");
        }

        public NetherNativeActionResult PollGetOnlyRefresh()
        {
            GetOnlyPollCount++;
            if (DropFloorSceneReadinessOnNextGetPoll)
            {
                DropFloorSceneReadinessOnNextGetPoll = false;
                FloorSceneEntered = false;
            }
            return NetherNativeActionResult.Completed("native-get-only-applied");
        }

        public NetherReadOnlySnapshotResult TryCaptureAppliedSnapshot() =>
            NetherReadOnlySnapshotResult.Success(CurrentSnapshot);

        public NetherNativeActionResult PollBattleLifecycle()
        {
            if (HoldBattleOpen)
                return NetherNativeActionResult.Completed("native-battle-active");
            CurrentSnapshot = BattleSettlementSnapshotOverride ?? AfterBattle;
            // The real FloorSelection owner is torn down before the battle-result view and its
            // code popup are presented.  Keep the production E2E seam faithful to that lifecycle
            // so the result-owned flow cannot accidentally depend on a stale map controller.
            HasRegisteredFloorSelection = false;
            HasObservedNetherBattleResult = true;
            _battleResultNextInvoked = false;
            _battleResultContinuationCompleted = false;
            _battleClearAvailable = true;
            return NetherNativeActionResult.Completed("native-battle-clear-terminal");
        }

        public NetherNativeActionResult PollBattleStart()
        {
            if (!DelayBattleSnapshotUntilStartTerminal)
                return NetherNativeActionResult.Completed("native-battle-start-terminal");
            if (!BattleStartRegistered)
                return NetherNativeActionResult.Started("await-battle-start-registration");
            if (!BattleStartCompleted)
                return NetherNativeActionResult.Started("native-battle-start-pending");
            CurrentSnapshot = FloorSelectionDispatchSnapshot ?? BattleSnapshot;
            return NetherNativeActionResult.Completed("native-battle-start-terminal");
        }

        public void CancelBattleStartObservation() => BattleStartCancelCount++;

        public bool TryConsumeBattleClear()
        {
            bool consumed = _battleClearAvailable;
            _battleClearAvailable = false;
            return consumed;
        }

        public bool TryConsumeBattleClose() => false;

        public NetherActiveCodeErosionProjection TryCaptureActiveCodeErosionProjection() => ActiveCodeErosion;

        public bool TryBeginContinueSceneHandoff(out long ownerGeneration)
        {
            ownerGeneration = 1;
            return true;
        }

        public NetherCheckpointReturnPreflightDecision PreflightContinueReturn(NetherPlannedAction action)
        {
            ContinuePreflightCount++;
            Trace.Add("continue-preflight");
            return new NetherCheckpointReturnPreflightDecision
            {
                Kind = NetherCheckpointReturnPreflightKind.NoReturn,
                SelectionLimit = 0,
            };
        }

        public NetherNativeActionResult PollContinueParent()
        {
            ContinueParentPollCount++;
            Trace.Add("continue-parent-poll");
            return ContinueParentCompleted
                ? NetherNativeActionResult.Completed("native-continue-parent-terminal")
                : NetherNativeActionResult.Started("native-continue-parent-pending");
        }

        public NetherNativeActionResult SelectReturnItems(IReadOnlyList<NetherRewardItem> items) =>
            NetherNativeActionResult.BindingUnavailable("no-return-expected");

        public bool TryConsumeResultSuccess() => true;

        public NetherNativeActionResult PollResultFlow()
        {
            ResultPollCount++;
            return ResultFlowSteps.Count == 0
                ? NetherNativeActionResult.Completed("native-result-terminal")
                : ResultFlowSteps.Dequeue();
        }

        public NetherBattleResultContinuationStep PollBattleResultContinuation(bool allowInvoke)
        {
            if (!HasObservedNetherBattleResult)
            {
                return new(
                    NetherBattleResultContinuationStepKind.BindingUnavailable,
                    "scripted-battle-result-not-observed"
                );
            }
            if (!_battleResultNextInvoked)
            {
                if (!allowInvoke)
                {
                    return new(
                        NetherBattleResultContinuationStepKind.CanceledBeforeInvoke,
                        "scripted-disabled-before-next"
                    );
                }
                _battleResultNextInvoked = true;
                BattleResultNextInvokeCount++;
                HasRegisteredFloorSelection = false;
                if (!AutoCompleteBattleResultContinuation)
                {
                    return new(
                        NetherBattleResultContinuationStepKind.AwaitingFloorRebind,
                        "scripted-next-invoked"
                    );
                }
            }

            if (!AutoCompleteBattleResultContinuation && !BattleResultRebound)
            {
                return new(
                    NetherBattleResultContinuationStepKind.AwaitingFloorRebind,
                    "scripted-awaiting-floor-rebind"
                );
            }

            if (!AutoCompleteBattleResultContinuation
                && BattleResultReboundSnapshot != null
                && (!BattleResultReboundSceneEntered
                    || !NetherBattleResultReboundReadiness.IsModalReady(
                        BattleResultReboundSnapshot.Status,
                        BattleResultReboundPopup != null
                    )))
            {
                HasRegisteredFloorSelection = true;
                return new(
                    NetherBattleResultContinuationStepKind.AwaitingFloorRebind,
                    "scripted-awaiting-rebound-popup"
                );
            }

            if (!_battleResultContinuationCompleted)
            {
                _battleResultContinuationCompleted = true;
                HasObservedNetherBattleResult = false;
                HasRegisteredFloorSelection = true;
                CurrentRuntimeGeneration++;
                if (BattleResultReboundSnapshot != null)
                    CurrentSnapshot = BattleResultReboundSnapshot;
                if (BattleResultReboundPopup != null)
                    ActivePopup = BattleResultReboundPopup;
            }
            return new(
                NetherBattleResultContinuationStepKind.Completed,
                "scripted-battle-result-floor-rebound",
                CurrentSnapshot
            );
        }

        public void ClearRegistrations()
        {
            _typedSemanticProviderFactory = null;
            _latestTypedSemanticProvider = null;
            ActivePopup = null;
            OwnedPopup = null;
            VisibleMap = null;
            VisibleMapFactory = null;
            ProvideCompleteVisibleBranchEvidence = true;
            _queuedOwnedPopups.Clear();
            _queuedOwnedPopupSnapshots.Clear();
            _ownedPopupStageEntry.Reset();
            _codeReloadSnapshotApplied = false;
            _codeKeepSnapshotApplied = false;
            _queuedCodeReloadRefreshes.Clear();
            _finishNativePending = false;
            ResultFlowSteps.Clear();
            _floorParentPending = false;
            _battleResultNextInvoked = false;
            _battleResultContinuationCompleted = false;
            HasObservedNetherBattleResult = false;
            BattleResultCodePopup = null;
            BattleResultCodeActions.Clear();
            BattleResultCodeNativeSteps.Clear();
        }

        private static NetherStrategyVisibleMapEvidence? BindProviderBackedCanonicalEvidence(
            NetherStrategyVisibleMapEvidence? visibleMap,
            NetherStrategyTypedSemanticProviderEvidence? provider
        )
        {
            if (visibleMap == null || provider == null)
                return visibleMap;
            NetherStrategySemanticTierLookup semanticTiers = NetherStrategySemanticTierLookup.Create(provider);
            return visibleMap with
            {
                ContentRows = visibleMap.ContentRows
                    .Select(row =>
                    {
                        if (row.Kind == NetherStrategyVisibleContentKind.ShopInventory
                            && semanticTiers.TryGetShopKey(
                                row.ContentId,
                                row.ContentType,
                                row.MasterRowId,
                                row.Amount,
                                out long shopKeyIdentity
                            ))
                        {
                            return row with
                            {
                                IsTreasureKey = true,
                                ShopKeyIdentity = shopKeyIdentity,
                            };
                        }
                        if (row.Kind is not (
                                NetherStrategyVisibleContentKind.Item
                                or NetherStrategyVisibleContentKind.ShopInventory
                            )
                            || !semanticTiers.TryGetCanonicalRewardEvidence(
                                row.Kind == NetherStrategyVisibleContentKind.ShopInventory
                                    ? row.MasterRowId
                                    : row.ContentId,
                                out NetherCanonicalRewardTier tier,
                                out int typedItemType,
                                out NetherRewardRarity typedRarity
                            ))
                        {
                            return row;
                        }
                        return row with
                        {
                            CanonicalRewardTier = tier,
                            ItemType = typedItemType,
                            ItemRarity = (int)typedRarity,
                        };
                    })
                    .ToArray(),
            };
        }

        private NetherStrategyVisibleMapEvidence? CaptureVisibleMap(NetherSnapshot snapshot)
        {
            if (VisibleMapFactory != null)
                return VisibleMapFactory(snapshot);
            if (VisibleMap != null || !ProvideCompleteVisibleBranchEvidence)
                return VisibleMap;
            return CompleteVisibleBranchMap(snapshot);
        }

        private static NetherStrategyVisibleMapEvidence CompleteVisibleBranchMap(
            NetherSnapshot snapshot
        )
        {
            var rows = new List<NetherStrategyVisibleContentRow>();
            foreach (NetherFloorNode floor in snapshot.Floors.Where(node => node.IsUnlocked && !node.IsHidden))
            {
                switch (floor.NodeType)
                {
                    case NetherFloorNodeType.Battle:
                    case NetherFloorNodeType.MiniBoss:
                    case NetherFloorNodeType.Boss:
                        rows.Add(new NetherStrategyVisibleContentRow(
                            floor.NodeType == NetherFloorNodeType.Boss
                                ? NetherStrategyVisibleContentKind.Boss
                                : NetherStrategyVisibleContentKind.Battle,
                            floor.NodeId,
                            floor.FloorId,
                            floor.FloorId
                        )
                        {
                            MapFloorMasterId = floor.FloorId,
                            BattleStageId = 1,
                            CodeDropRatio = 0,
                            IsKnown = true,
                        });
                        break;
                    case NetherFloorNodeType.Event:
                        long eventId = 100_000 + floor.NodeId;
                        long eventPartId = eventId + 1;
                        rows.Add(new NetherStrategyVisibleContentRow(
                            NetherStrategyVisibleContentKind.Event,
                            floor.NodeId,
                            eventId,
                            eventPartId
                        )
                        {
                            MapFloorMasterId = floor.FloorId,
                            EventId = eventId,
                            EventPartId = eventPartId,
                            ContentType = 165,
                            Amount = 1,
                            IsKnown = true,
                            EventOptions =
                            [
                                new NetherStrategyVisibleEventOptionEvidence(
                                    1,
                                    eventPartId,
                                    [
                                        new NetherStrategyVisibleEventEffectEvidence(
                                            NetherStrategyVisibleEventEffectSource.Content,
                                            165,
                                            1
                                        )
                                        {
                                            ContentId = 1,
                                            Amount = 1,
                                            EffectKind = NetherEffectKind.NetherGoldGain,
                                            IsPresent = true,
                                            IsKnown = true,
                                        },
                                    ]
                                ),
                            ],
                        });
                        break;
                    case NetherFloorNodeType.Treasure:
                        rows.Add(new NetherStrategyVisibleContentRow(
                            NetherStrategyVisibleContentKind.Treasure,
                            floor.NodeId,
                            floor.FloorId,
                            floor.FloorId
                        )
                        {
                            MapFloorMasterId = floor.FloorId,
                            IsKnown = true,
                        });
                        break;
                    case NetherFloorNodeType.Shop:
                        rows.Add(new NetherStrategyVisibleContentRow(
                            NetherStrategyVisibleContentKind.ShopInventory,
                            floor.NodeId,
                            floor.FloorId,
                            floor.FloorId
                        )
                        {
                            MapFloorMasterId = floor.FloorId,
                            Amount = 1,
                            Cost = 0,
                            UsesNetherGold = true,
                            IsKnown = true,
                        });
                        break;
                    case NetherFloorNodeType.Recovery:
                        rows.Add(new NetherStrategyVisibleContentRow(
                            NetherStrategyVisibleContentKind.Resource,
                            floor.NodeId,
                            floor.FloorId,
                            0
                        )
                        {
                            MapFloorMasterId = floor.FloorId,
                            IsKnown = true,
                        });
                        break;
                }
            }
            return new NetherStrategyVisibleMapEvidence(snapshot.Floors.ToArray(), rows);
        }

        private static NetherRuntimeInteractivePreEntryInputsResult CompleteInteractivePreEntry(
            NetherSnapshot snapshot,
            NetherAutoClimbSettings settings,
            bool provideCompleteRecoveryBranchEvidence = false
        )
        {
            var entries = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>();
            foreach (NetherFloorNode floor in snapshot.Floors.Where(node =>
                node.IsUnlocked
                && !node.IsHidden
                && node.NodeType is NetherFloorNodeType.Event
                    or NetherFloorNodeType.Recovery
                    or NetherFloorNodeType.Shop
                    or NetherFloorNodeType.Treasure))
            {
                bool isShop = floor.NodeType == NetherFloorNodeType.Shop;
                bool isRecovery = provideCompleteRecoveryBranchEvidence
                    && floor.NodeType == NetherFloorNodeType.Recovery;
                long eventId = 200_000 + floor.NodeId;
                long partId = eventId + 1;
                IReadOnlyList<NetherFloorEventMasterRow> eventRows = isShop
                    ? Array.Empty<NetherFloorEventMasterRow>()
                    : new[]
                    {
                        new NetherFloorEventMasterRow(
                            eventId,
                            floor.FloorId,
                            1,
                            partId,
                            isRecovery ? partId + 1 : 0,
                            isRecovery ? partId + 2 : 0,
                            0
                        ),
                    };
                IReadOnlyList<NetherFloorEventPartMasterRow> eventParts = isShop
                    ? Array.Empty<NetherFloorEventPartMasterRow>()
                    : isRecovery
                        ? new[]
                        {
                            new NetherFloorEventPartMasterRow(
                                partId,
                                (int)NetherEffectKind.Heal,
                                200,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0
                            ),
                            new NetherFloorEventPartMasterRow(
                                partId + 1,
                                (int)NetherEffectKind.ErosionHeal,
                                20,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0
                            ),
                            new NetherFloorEventPartMasterRow(
                                partId + 2,
                                (int)NetherEffectKind.AbyssCodeTransform,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0
                            ),
                        }
                        : new[]
                        {
                            new NetherFloorEventPartMasterRow(
                                partId,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                165,
                                1,
                                1
                            ),
                        };
                NetherInteractiveFloorPreEntrySafetyInput input = new(
                    floor.NodeType,
                    floor.FloorId,
                    new[] { new NetherFloorMasterBoundsRow(floor.FloorId, 0, 0) },
                    eventRows,
                    eventParts,
                    snapshot.ErosionPoint,
                    snapshot.Characters
                        .Where(character => character.IsActive)
                        .Select(character => character.HpPermille)
                        .ToArray(),
                    snapshot.NetherGold,
                    snapshot.TreasureKeyCount,
                    settings
                )
                {
                    CanCloseShop = isShop,
                    FloorExtendId = isShop ? 0 : eventId,
                    FloorNodeId = floor.NodeId,
                    CurrentCodes = snapshot.Codes,
                    CodeCapacity = snapshot.CodeCapacity,
                    RecoveryBranchSafetyByPartId = isRecovery
                        ? CompleteRecoveryBranchProofs(partId, partId + 1, partId + 2, settings)
                        : new Dictionary<long, NetherRecoveryBranchSafetyEvidence>(),
                };
                NetherInteractiveFloorPreEntrySafetyResult safety =
                    new NetherInteractiveFloorPreEntrySafety().Evaluate(input);
                entries[floor.NodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
                {
                    IsCaptured = true,
                    Input = input,
                    Safety = safety,
                    Detail = safety.IsSafe ? string.Empty : safety.Detail,
                };
            }
            return NetherRuntimeInteractivePreEntryInputsResult.Success(entries, snapshot.Fingerprint);
        }

        internal static NetherRuntimeInteractivePreEntryInputsResult MergeInteractiveCapture(
            NetherSnapshot snapshot,
            NetherAutoClimbSettings settings,
            long nodeId,
            NetherInteractiveFloorPreEntrySafetyInput input,
            NetherInteractiveFloorPreEntrySafetyResult safety,
            bool provideCompleteRecoveryBranchEvidence = false
        )
        {
            NetherRuntimeInteractivePreEntryInputsResult complete =
                CompleteInteractivePreEntry(
                    snapshot,
                    settings,
                    provideCompleteRecoveryBranchEvidence
                );
            var entries = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>(
                complete.ByFloorNodeId
            )
            {
                [nodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
                {
                    IsCaptured = true,
                    Input = input,
                    Safety = safety,
                    Detail = safety.IsSafe ? string.Empty : safety.Detail,
                },
            };
            return NetherRuntimeInteractivePreEntryInputsResult.Success(entries, snapshot.Fingerprint);
        }

        internal static NetherSnapshot InteractiveRouteSnapshot(NetherSessionStatus status, long floorId, int gold) => new()
        {
            Status = status,
            NetherId = 7,
            MapId = 1,
            CurrentFloorId = floorId,
            FloorLevel = floorId == 1 ? 1 : 2,
            FloorIndex = floorId == 1 ? 1 : 2,
            MaxFloorLevel = 130,
            MasterMaxFloorLevel = 130,
            AuthoritativeBossFloorLevels = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 },
            ContinuanceFloorLevel = 10,
            ErosionPoint = 20,
            TicketCount = 2,
            TreasureKeyCount = 1,
            NetherGold = gold,
            CodeReloadCount = 2,
            CodeCapacity = 3,
            Characters = new[] { new NetherCharacterState(1, 500) },
            Floors = new[]
            {
                Floor(1, 1, NetherFloorNodeType.Recovery),
                Floor(2, 2, NetherFloorNodeType.Event, new[] { 1L }),
                Floor(3, 3, NetherFloorNodeType.Boss, new[] { 2L }),
            },
            CharacterHpHash = "character:1:500",
            CodeHash = "nether-codes:none",
            MapHash = "interactive:" + status + ":" + floorId + ":" + gold,
        };

        internal static NetherSnapshot RecoveryProofRouteSnapshot(NetherSessionStatus status) =>
            InteractiveRouteSnapshot(status, floorId: 1, gold: 0) with
            {
                CurrentNodeId = 1,
                TreasureKeyCount = 1,
                Floors = new[]
                {
                    Floor(1, 1, NetherFloorNodeType.Event),
                    Floor(2, 2, NetherFloorNodeType.Recovery, new[] { 1L }),
                    Floor(3, 3, NetherFloorNodeType.Boss, new[] { 2L }),
                },
            };

        internal static NetherRuntimeInteractivePreEntryInputsResult RecoveryProofInteractivePreEntry(
            NetherSnapshot snapshot,
            NetherAutoClimbSettings settings
        )
        {
            NetherInteractiveOptionProjection eventProjection = new(
                1,
                ErosionDelta: 0,
                HpDelta: 0,
                ExpectedEffects: new[] { new NetherEffect(NetherEffectKind.Heal, 1) }
            )
            {
                EventId = 100,
                EventPartId = 1001,
                FloorId = 1,
                NodeId = 1,
                IsKnown = true,
                HasRouteSafetyEvidence = true,
                RouteSafetyAllowed = true,
            };
            NetherInteractiveOptionProjection restProjection = new(
                1,
                ErosionDelta: 0,
                HpDelta: 0,
                ExpectedEffects: new[] { new NetherEffect(NetherEffectKind.Heal, 200) }
            )
            {
                EventId = 200,
                EventPartId = 2001,
                FloorId = 2,
                NodeId = 2,
                IsKnown = true,
                HasRouteSafetyEvidence = true,
                RouteSafetyAllowed = true,
            };
            NetherInteractiveOptionProjection purificationProjection = new(
                2,
                ErosionDelta: -20,
                HpDelta: 0,
                ExpectedEffects: new[] { new NetherEffect(NetherEffectKind.ErosionHeal, 20) }
            )
            {
                EventId = 200,
                EventPartId = 2002,
                FloorId = 2,
                NodeId = 2,
                IsKnown = true,
                HasRouteSafetyEvidence = true,
                RouteSafetyAllowed = true,
            };
            var entries = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>();
            foreach (NetherFloorNode floor in snapshot.Floors.Where(node =>
                node.NodeType is NetherFloorNodeType.Event or NetherFloorNodeType.Recovery))
            {
                NetherInteractiveFloorPreEntrySafetyResult safety = floor.NodeType == NetherFloorNodeType.Event
                    ? NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [100] = 1 },
                        new Dictionary<long, NetherInteractiveOptionProjection> { [100] = eventProjection },
                        new NetherInteractiveWorstCaseProjection(0, 0),
                        new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                        {
                            [new NetherInteractiveEventOptionKey(100, 1001, 1)] = eventProjection,
                        }
                    )
                    : NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [200] = 1 },
                        new Dictionary<long, NetherInteractiveOptionProjection> { [200] = restProjection },
                        new NetherInteractiveWorstCaseProjection(0, 0),
                        new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                        {
                            [new NetherInteractiveEventOptionKey(200, 2001, 1)] = restProjection,
                            [new NetherInteractiveEventOptionKey(200, 2002, 2)] = purificationProjection,
                        }
                    );
                NetherInteractiveFloorPreEntrySafetyInput input = new(
                    floor.NodeType,
                    floor.FloorId,
                    new[] { new NetherFloorMasterBoundsRow(floor.FloorId, 0, 0) },
                    Array.Empty<NetherFloorEventMasterRow>(),
                    Array.Empty<NetherFloorEventPartMasterRow>(),
                    snapshot.ErosionPoint,
                    snapshot.Characters.Where(character => character.IsActive).Select(character => character.HpPermille).ToArray(),
                    snapshot.NetherGold,
                    snapshot.TreasureKeyCount,
                    settings
                )
                {
                    FloorNodeId = floor.NodeId,
                };
                entries[floor.NodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
                {
                    IsCaptured = true,
                    Input = input,
                    Safety = safety,
                };
            }
            return NetherRuntimeInteractivePreEntryInputsResult.Success(entries, snapshot.Fingerprint);
        }

        internal static NetherSnapshot ProcurementRouteSnapshot(NetherSessionStatus status) =>
            InteractiveRouteSnapshot(status, floorId: 1, gold: 0) with
            {
                CurrentNodeId = 1,
                TreasureKeyCount = 0,
                Floors = new[]
                {
                    Floor(1, 1, NetherFloorNodeType.Recovery),
                    Floor(2, 2, NetherFloorNodeType.Event, new[] { 1L }),
                    Floor(3, 3, NetherFloorNodeType.Shop, new[] { 2L }),
                    Floor(4, 4, NetherFloorNodeType.Treasure, new[] { 3L }),
                    Floor(5, 5, NetherFloorNodeType.Boss, new[] { 4L }),
                },
            };

        internal static NetherSnapshot ProcurementAlternateBranchSnapshot(NetherSessionStatus status) =>
            ProcurementRouteSnapshot(status) with
            {
                Floors = new[]
                {
                    Floor(1, 1, NetherFloorNodeType.Recovery),
                    Floor(2, 2, NetherFloorNodeType.Event, new[] { 1L }),
                    Floor(3, 3, NetherFloorNodeType.Shop, new[] { 2L }),
                    Floor(4, 4, NetherFloorNodeType.Treasure, new[] { 3L }),
                    Floor(5, 5, NetherFloorNodeType.Boss, new[] { 4L }),
                    Floor(6, 3, NetherFloorNodeType.Shop, new[] { 2L }),
                    Floor(7, 4, NetherFloorNodeType.Treasure, new[] { 6L }),
                    Floor(8, 5, NetherFloorNodeType.Boss, new[] { 7L }),
                },
            };

        internal static NetherStrategyVisibleMapEvidence ProcurementVisibleMap(
            IReadOnlyList<NetherFloorNode> floors
        ) => new(
            floors,
            new NetherStrategyVisibleContentRow[]
            {
                ProcurementVisibleEvent(2, 100, 1001, 1, NetherEffectKind.NetherGoldGain, 200),
                ProcurementVisibleEvent(2, 100, 1002, 2, NetherEffectKind.TreasureKeyGain, 1),
                new NetherStrategyVisibleContentRow(
                    NetherStrategyVisibleContentKind.ShopInventory,
                    3,
                    3001,
                    3001
                )
                {
                    IsKnown = true,
                    ContentType = 166,
                    Cost = 200,
                    Amount = 1,
                    UsesNetherGold = true,
                },
                new NetherStrategyVisibleContentRow(
                    NetherStrategyVisibleContentKind.Treasure,
                    4,
                    4001,
                    401
                )
                {
                    IsKnown = true,
                    EventId = 401,
                    EventPartId = 4011,
                },
                new NetherStrategyVisibleContentRow(
                    NetherStrategyVisibleContentKind.Item,
                    4,
                    4011,
                    4011
                )
                {
                    IsKnown = true,
                    EventId = 401,
                    EventPartId = 4011,
                    ItemType = 91,
                    ItemRarity = 5,
                    Amount = 1,
                },
            }
        );

        internal static NetherStrategyVisibleMapEvidence ProcurementAlternateVisibleMap(
            IReadOnlyList<NetherFloorNode> floors
        ) => new(
            floors,
            new NetherStrategyVisibleContentRow[]
            {
                ProcurementVisibleEvent(2, 100, 1001, 1, NetherEffectKind.NetherGoldGain, 200),
                new NetherStrategyVisibleContentRow(
                    NetherStrategyVisibleContentKind.ShopInventory,
                    6,
                    6001,
                    6001
                )
                {
                    IsKnown = true,
                    Cost = 200,
                    Amount = 1,
                    UsesNetherGold = true,
                },
            }
        );

        internal static NetherRuntimeInteractivePreEntryInputsResult ProcurementInteractivePreEntry(
            NetherSnapshot snapshot,
            NetherAutoClimbSettings settings
        )
        {
            NetherInteractiveOptionProjection goldProjection = new(
                1,
                ErosionDelta: 0,
                HpDelta: 0,
                ExpectedEffects: new[] { new NetherEffect(NetherEffectKind.NetherGoldGain, 200) }
            )
            {
                EventId = 100,
                EventPartId = 1001,
                FloorId = 2,
                NodeId = 2,
                IsKnown = true,
                HasRouteSafetyEvidence = true,
                RouteSafetyAllowed = true,
            };
            NetherInteractiveOptionProjection keyProjection = new(
                2,
                ErosionDelta: 0,
                HpDelta: 0,
                ExpectedEffects: new[] { new NetherEffect(NetherEffectKind.TreasureKeyGain, 1) }
            )
            {
                EventId = 100,
                EventPartId = 1002,
                FloorId = 2,
                NodeId = 2,
                IsKnown = true,
                HasRouteSafetyEvidence = true,
                RouteSafetyAllowed = true,
            };
            NetherInteractiveOptionProjection treasureProjection = new(
                1,
                ErosionDelta: 0,
                HpDelta: 0,
                ExpectedEffects: new[] { new NetherEffect(NetherEffectKind.TreasureKeyGain, 1) }
            )
            {
                EventId = 401,
                EventPartId = 4011,
                FloorId = 4,
                NodeId = 4,
                IsKnown = true,
                HasRouteSafetyEvidence = true,
                RouteSafetyAllowed = true,
            };
            var eventOptions = new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
            {
                [new NetherInteractiveEventOptionKey(100, 1001, 1)] = goldProjection,
                [new NetherInteractiveEventOptionKey(100, 1002, 2)] = keyProjection,
            };
            var entries = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>();
            foreach (NetherFloorNode floor in snapshot.Floors.Where(node =>
                node.NodeType is NetherFloorNodeType.Event
                    or NetherFloorNodeType.Recovery
                    or NetherFloorNodeType.Shop
                    or NetherFloorNodeType.Treasure))
            {
                NetherInteractiveFloorPreEntrySafetyInput input = new(
                    floor.NodeType,
                    floor.FloorId,
                    new[] { new NetherFloorMasterBoundsRow(floor.FloorId, 0, 0) },
                    Array.Empty<NetherFloorEventMasterRow>(),
                    Array.Empty<NetherFloorEventPartMasterRow>(),
                    snapshot.ErosionPoint,
                    snapshot.Characters.Where(character => character.IsActive).Select(character => character.HpPermille).ToArray(),
                    snapshot.NetherGold,
                    snapshot.TreasureKeyCount,
                    settings
                )
                {
                    FloorNodeId = floor.NodeId,
                };
                NetherInteractiveFloorPreEntrySafetyResult safety = floor.NodeType switch
                {
                    NetherFloorNodeType.Event => NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [100] = 1 },
                        new Dictionary<long, NetherInteractiveOptionProjection> { [100] = goldProjection },
                        new NetherInteractiveWorstCaseProjection(0, 0),
                        eventOptions
                    ),
                    NetherFloorNodeType.Treasure => NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [401] = 1 },
                        new Dictionary<long, NetherInteractiveOptionProjection> { [401] = treasureProjection },
                        new NetherInteractiveWorstCaseProjection(0, 0),
                        new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                        {
                            [new NetherInteractiveEventOptionKey(401, 4011, 1)] = treasureProjection,
                        }
                    ),
                    _ => NetherInteractiveFloorPreEntrySafetyResult.SafeNeutral(),
                };
                entries[floor.NodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
                {
                    IsCaptured = true,
                    Input = input,
                    Safety = safety,
                };
            }
            return NetherRuntimeInteractivePreEntryInputsResult.Success(entries, snapshot.Fingerprint);
        }

        internal static NetherRuntimeInteractivePreEntryInputsResult ProcurementAlternateInteractivePreEntry(
            NetherSnapshot snapshot,
            NetherAutoClimbSettings settings
        ) => ProcurementInteractivePreEntry(snapshot, settings);

        internal static NetherInteractiveFloorPreEntrySafetyInput ProcurementSpendInput(
            NetherSnapshot snapshot,
            NetherInteractiveEventOptionKey key,
            IReadOnlyDictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget> commitments
        ) => new(
            NetherFloorNodeType.Event,
            2,
            new[] { new NetherFloorMasterBoundsRow(2, 0, 0) },
            new[] { new NetherFloorEventMasterRow(key.EventId, 2, 1, key.EventPartId, 0, 0, 0) },
            new[]
            {
                new NetherFloorEventPartMasterRow(
                    key.EventPartId,
                    (int)NetherEffectKind.NetherGoldUsed,
                    200,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0
                ),
            },
            snapshot.ErosionPoint,
            new[] { 500 },
            200,
            snapshot.TreasureKeyCount,
            new NetherAutoClimbSettings()
        )
        {
            FloorNodeId = 2,
            CommittedProcurementByOption = commitments,
        };

        private static NetherStrategyVisibleContentRow ProcurementVisibleEvent(
            long nodeId,
            long eventId,
            long partId,
            int optionNumber,
            NetherEffectKind kind,
            int amount
        ) => new(NetherStrategyVisibleContentKind.Event, nodeId, eventId, partId)
        {
            EventId = eventId,
            EventPartId = partId,
            IsKnown = true,
            EventOptions = new[]
            {
                new NetherStrategyVisibleEventOptionEvidence(
                    optionNumber,
                    partId,
                    new[]
                    {
                        new NetherStrategyVisibleEventEffectEvidence(
                            NetherStrategyVisibleEventEffectSource.Content,
                            kind == NetherEffectKind.NetherGoldGain ? 165 : 166,
                            0
                        )
                        {
                            EffectKind = kind,
                            Amount = amount,
                            ContentId = 0,
                            IsPresent = true,
                            IsKnown = true,
                        },
                    }
                ),
            },
        };

        internal static NetherRuntimeRouteSafetyData InteractiveRouteSafety(
            NetherActiveCodeErosionProjection? activeCodeErosion = null
        ) => new()
        {
            FloorBoundsByFloorId = new Dictionary<long, NetherFloorMasterBounds>
            {
                [3] = new NetherFloorMasterBounds(3, 0, 0, IsKnown: true, Detail: string.Empty),
            },
            ActivePartyHp = NetherRouteSafetyHpTestEvidence.Single(1, 500),
            ActiveCodeErosion = activeCodeErosion ?? KnownEmptyCodeProjection(),
            // The scripted Equipment route is an authoritative completed-state fixture. Tests
            // covering native-unknown Research pass null explicitly and assert the pause.
            ResearchIncomplete = false,
        };

        internal static NetherActiveCodeErosionProjection KnownEmptyCodeProjection() => new()
        {
            ErosionProjectionKnown = true,
            CodeHash = "nether-codes:none",
            ErosionEffects = Array.Empty<NetherCodeEffect>(),
        };

        internal static NetherCodeState RecoveryTransformCode() => new(
            30024,
            NetherCodeFamily.Safe,
            1
        )
        {
            IsKnown = true,
            EffectSemanticsKnown = true,
            Category = NetherCodeCategory.ErosionResistance,
            Rarity = 1,
            Power = 0,
            PossessionAmount = 1,
        };

        private static IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence>
            CompleteRecoveryBranchProofs(
                long restPartId,
                long purificationPartId,
                long transformPartId,
                NetherAutoClimbSettings settings
            )
        {
            // This is an explicit managed DTO/provider fixture. It represents the same
            // snapshot-scoped typed hard-exclusion evidence that production must receive from an
            // authoritative adapter; it never infers a Recovery branch from native raw fields.
            var transformEligibility = new NetherCodeTransformEligibilityEvidence
            {
                IsKnown = true,
                StrategyMode = settings.StrategyMode,
                EquipmentOptInEnabled = settings.EquipmentRecoveryCodeTransformEnabled,
                IsRecovery = true,
                DeterministicRecoveryChoicesHaveZeroValue = false,
                HardExcludedCodes = Array.Empty<NetherCodeTransformHardExclusion>(),
            };
            return new Dictionary<long, NetherRecoveryBranchSafetyEvidence>
            {
                [restPartId] = new NetherRecoveryBranchSafetyEvidence
                {
                    BranchKind = NetherRecoveryBranchKind.Rest,
                    IsKnown = true,
                    IsCompleteVisibleBranch = true,
                    IsNextVisibleBranchSafe = true,
                    TransformEligibility = transformEligibility,
                },
                [purificationPartId] = new NetherRecoveryBranchSafetyEvidence
                {
                    BranchKind = NetherRecoveryBranchKind.Purification,
                    IsKnown = true,
                    IsCompleteVisibleBranch = true,
                    IsNextVisibleBranchSafe = true,
                    TransformEligibility = transformEligibility,
                },
                [transformPartId] = new NetherRecoveryBranchSafetyEvidence
                {
                    BranchKind = NetherRecoveryBranchKind.Transform,
                    IsKnown = true,
                    IsCompleteVisibleBranch = true,
                    IsNextVisibleBranchSafe = true,
                    TransformEligibility = transformEligibility,
                },
            };
        }

        internal static NetherRuntimeInteractivePreEntryInputsResult InteractivePreEntry(
            NetherSnapshot snapshot,
            NetherAutoClimbSettings settings
        )
        {
            var input = new NetherInteractiveFloorPreEntrySafetyInput(
                NetherFloorNodeType.Event,
                FloorMasterId: 2,
                MapFloorRows: new[] { new NetherFloorMasterBoundsRow(2, 0, 0) },
                EventRows: new[] { new NetherFloorEventMasterRow(42, 2, 1, 1001, 0, 0, 0) },
                EventPartRows: new[]
                {
                    new NetherFloorEventPartMasterRow(
                        1001,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        // MNetherFloorEventParts.content_type: 165 maps to NetherGoldGain.
                        165,
                        1,
                        1
                    ),
                },
                CurrentErosion: snapshot.ErosionPoint,
                ActiveHpPermille: new[] { 500 },
                CurrentNetherGold: snapshot.NetherGold,
                CurrentTreasureKeys: snapshot.TreasureKeyCount,
                Settings: settings
            );
                NetherInteractiveFloorPreEntrySafetyResult safety = new NetherInteractiveFloorPreEntrySafety().Evaluate(input);
            return MergeInteractiveCapture(snapshot, settings, 2, input, safety);
        }

        internal static NetherSnapshot OwnedRouteSnapshot(
            NetherSessionStatus status,
            NetherFloorNodeType targetKind,
            long floorId,
            int gold,
            int keys = 1,
            int hp = 500,
            int? floorLevel = null
        ) => new()
        {
            Status = status,
            NetherId = 7,
            MapId = 1,
            CurrentFloorId = floorId,
            FloorLevel = floorLevel ?? (floorId == 1 ? 1 : 2),
            FloorIndex = floorId == 1 ? 1 : 2,
            MaxFloorLevel = 130,
            MasterMaxFloorLevel = 130,
            AuthoritativeBossFloorLevels = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 },
            ContinuanceFloorLevel = 10,
            ErosionPoint = 20,
            TicketCount = 2,
            TreasureKeyCount = keys,
            NetherGold = gold,
            CodeReloadCount = 2,
            CodeCapacity = 3,
            Characters = new[] { new NetherCharacterState(1, hp) },
            Floors = new[]
            {
                Floor(1, 1, NetherFloorNodeType.Recovery),
                Floor(2, 2, targetKind, new[] { 1L }),
                Floor(3, 3, NetherFloorNodeType.Boss, new[] { 2L }),
            },
            CharacterHpHash = "character:1:" + hp,
            CodeHash = "nether-codes:none",
            MapHash = "owned:" + targetKind + ":" + status + ":" + floorId + ":" + gold + ":" + keys + ":" + hp,
        };

        internal static NetherRuntimeInteractivePreEntryInputsResult OwnedInteractivePreEntry(
            NetherSnapshot snapshot,
            NetherAutoClimbSettings settings,
            NetherFloorNodeType kind,
            NetherFloorEventPartMasterRow? eventPart = null,
            NetherInteractivePartialDeathEligibility? partialDeathEligibility = null
        )
        {
            IReadOnlyList<NetherFloorEventMasterRow>? eventRows = null;
            IReadOnlyList<NetherFloorEventPartMasterRow>? parts = null;
            if (kind is NetherFloorNodeType.Event
                or NetherFloorNodeType.Recovery
                or NetherFloorNodeType.Treasure)
            {
                if (eventPart is not NetherFloorEventPartMasterRow part)
                    return NetherRuntimeInteractivePreEntryInputsResult.Failure("missing-e2e-event-part");
                if (kind == NetherFloorNodeType.Recovery)
                {
                    // Route proof must cover the complete native Recovery branch, not only the
                    // popup child that the fixture later exposes. These are exact managed DTO
                    // shapes evaluated through the same public pre-entry safety seam.
                    NetherFloorEventPartMasterRow purification = new(
                        1003,
                        (int)NetherEffectKind.ErosionHeal,
                        20,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0
                    );
                    NetherFloorEventPartMasterRow transform = new(
                        1004,
                        (int)NetherEffectKind.AbyssCodeTransform,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0
                    );
                    eventRows = new[] { new NetherFloorEventMasterRow(42, 2, 1, part.PartId, 1003, 1004, 0) };
                    parts = new[] { part, purification, transform };
                }
                else
                {
                    eventRows = new[] { new NetherFloorEventMasterRow(42, 2, 1, part.PartId, 0, 0, 0) };
                    parts = new[] { part };
                }
            }

            var input = new NetherInteractiveFloorPreEntrySafetyInput(
                kind,
                FloorMasterId: 2,
                MapFloorRows: new[] { new NetherFloorMasterBoundsRow(2, 0, 0) },
                EventRows: eventRows,
                EventPartRows: parts,
                CurrentErosion: snapshot.ErosionPoint,
                ActiveHpPermille: snapshot.Characters.Where(character => character.IsActive).Select(character => character.HpPermille).ToArray(),
                CurrentNetherGold: snapshot.NetherGold,
                CurrentTreasureKeys: snapshot.TreasureKeyCount,
                Settings: settings
            )
            {
                FloorNodeId = 2,
                CanCloseShop = kind == NetherFloorNodeType.Shop,
                    CurrentCodes = snapshot.Codes,
                    CodeCapacity = snapshot.CodeCapacity,
                    RecoveryBranchSafetyByPartId = kind == NetherFloorNodeType.Recovery
                        ? CompleteRecoveryBranchProofs(
                            eventPart?.PartId ?? 0,
                            1003,
                            1004,
                            settings
                        )
                        : new Dictionary<long, NetherRecoveryBranchSafetyEvidence>(),
                    PartialDeathEligibility = partialDeathEligibility == null
                        ? Array.Empty<NetherInteractivePartialDeathEligibility>()
                        : new[] { partialDeathEligibility },
            };
            NetherInteractiveFloorPreEntrySafetyResult safety = new NetherInteractiveFloorPreEntrySafety().Evaluate(input);
            return MergeInteractiveCapture(
                snapshot,
                settings,
                2,
                input,
                safety,
                provideCompleteRecoveryBranchEvidence: kind == NetherFloorNodeType.Recovery
            );
        }

        private static NetherSnapshot Snapshot(
            NetherSessionStatus status,
            long mapId,
            long floorId,
            int floorLevel,
            int gold,
            int tickets
        ) => new()
        {
            Status = status,
            NetherId = 1,
            MapId = mapId,
            CurrentFloorId = floorId,
            FloorLevel = floorLevel,
            FloorIndex = floorLevel,
            MaxFloorLevel = 130,
            MasterMaxFloorLevel = 130,
            AuthoritativeBossFloorLevels = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 },
            ContinuanceFloorLevel = 10,
            ErosionPoint = 10,
            TicketCount = tickets,
            TreasureKeyCount = 1,
            NetherGold = gold,
            CodeReloadCount = 2,
            CodeCapacity = 3,
            LockReward = 0,
            Characters = new[] { new NetherCharacterState(1, 1000) },
            Codes = Array.Empty<NetherCodeState>(),
            Floors = new[]
            {
                Floor(1, 1, NetherFloorNodeType.Recovery),
                Floor(2, 2, NetherFloorNodeType.Battle, new[] { 1L }),
                Floor(3, 3, NetherFloorNodeType.Boss, new[] { 2L }),
            },
            CharacterHpHash = "character:1:1000",
            CodeHash = "nether-codes:none",
            MapHash = "map:" + mapId + ":" + floorId + ":" + floorLevel + ":" + status + ":" + gold + ":" + tickets,
        };

        private static NetherFloorNode Floor(
            long id,
            int level,
            NetherFloorNodeType type,
            IReadOnlyList<long>? previous = null
        ) => new(id, level, level, type)
        {
            IsUnlocked = true,
            PreviousFloorIds = previous ?? Array.Empty<long>(),
        };
    }

    private sealed class RecordingLeaseDriver : INetherBattleSettingsLeaseDriver
    {
        public RecordingLeaseDriver(bool needsRecovery = false)
        {
            NeedsRecovery = needsRecovery;
            Phase = needsRecovery
                ? NetherBattleSettingsLeasePhase.RestorePending
                : NetherBattleSettingsLeasePhase.Empty;
        }

        public int AcquireCalls { get; private set; }
        public int RestoreCalls { get; private set; }
        public NetherBattleSettingsLeasePhase Phase { get; private set; }
        public bool NeedsRecovery { get; private set; }

        public NetherNativeActionResult ProbePersistedLease() => NeedsRecovery
            ? NetherNativeActionResult.Started("e2e-persisted-lease-awaiting-accessor")
            : NetherNativeActionResult.Completed("e2e-no-persisted-lease");

        public NetherNativeActionResult AcquireAndForce()
        {
            AcquireCalls++;
            Phase = NetherBattleSettingsLeasePhase.Forced;
            return NetherNativeActionResult.Completed("e2e-force");
        }

        public NetherNativeActionResult Restore(string reason)
        {
            RestoreCalls++;
            Phase = NetherBattleSettingsLeasePhase.Restored;
            NeedsRecovery = false;
            return NetherNativeActionResult.Completed("e2e-restore:" + reason);
        }

        public NetherNativeActionResult RecoverOnLoad()
        {
            Phase = NetherBattleSettingsLeasePhase.Restored;
            NeedsRecovery = false;
            return NetherNativeActionResult.Completed("e2e-startup-recovery");
        }

        public NetherNativeActionResult RetryRestoreAfterNativeAccessorRegistered() =>
            NetherNativeActionResult.Completed("e2e-no-retry-needed");
    }

    private sealed class StartupLeaseHarness : IDisposable
    {
        private readonly string? _previousConfigPath;

        public StartupLeaseHarness()
        {
            _previousConfigPath = NetherBattleSettingsLease.ConfigPathOverrideForTests;
            ConfigPath = Path.Combine(Path.GetTempPath(), "abyssmod-round4-startup-lease-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ConfigPath);
            NetherBattleSettingsLease.ConfigPathOverrideForTests = ConfigPath;
            OriginalLease = CreateLease(new StartupLeaseNative(autoEnabled: false, speed: 1));
        }

        public string ConfigPath { get; }
        public string LeaseFilePath => Path.Combine(ConfigPath, "Abyss.AutoNether", "battle-settings-lease.json");
        public NetherBattleSettingsLease OriginalLease { get; }

        public NetherBattleSettingsLease CreateDetachedLease() => (NetherBattleSettingsLease)Activator.CreateInstance(
            typeof(NetherBattleSettingsLease),
            nonPublic: true
        )!;

        public void Attach(NetherBattleSettingsLease lease, INetherBattleSettingsNative native) => typeof(NetherBattleSettingsLease)
            .GetField("_native", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(lease, native);

        public void Dispose()
        {
            NetherBattleSettingsLease.ConfigPathOverrideForTests = _previousConfigPath;
            if (Directory.Exists(ConfigPath))
                Directory.Delete(ConfigPath, recursive: true);
        }

        private static NetherBattleSettingsLease CreateLease(INetherBattleSettingsNative native)
        {
            var lease = (NetherBattleSettingsLease)Activator.CreateInstance(
                typeof(NetherBattleSettingsLease),
                nonPublic: true
            )!;
            typeof(NetherBattleSettingsLease)
                .GetField("_native", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(lease, native);
            return lease;
        }
    }

    private sealed class StartupLeaseNative : INetherBattleSettingsNative
    {
        public StartupLeaseNative(bool autoEnabled, int speed)
        {
            AutoEnabled = autoEnabled;
            Speed = speed;
        }

        public bool AutoEnabled { get; private set; }
        public int Speed { get; private set; }
        public int WriteCalls { get; private set; }

        public bool TryRead(out bool autoEnabled, out int speed, out string error)
        {
            autoEnabled = AutoEnabled;
            speed = Speed;
            error = string.Empty;
            return true;
        }

        public bool TryForceAutoAndHighestSpeed(out string error)
        {
            AutoEnabled = true;
            Speed = 3;
            error = string.Empty;
            return true;
        }

        public bool TryWrite(bool autoEnabled, int speed, out string error)
        {
            WriteCalls++;
            AutoEnabled = autoEnabled;
            Speed = speed;
            error = string.Empty;
            return true;
        }
    }
}
