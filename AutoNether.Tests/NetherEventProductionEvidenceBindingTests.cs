using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

// Drives the NetherRuntimeBridge singleton; shares the serialized runtime collection.
[Collection("nether-managed-popup-runtime")]
public sealed class NetherEventProductionEvidenceBindingTests
{
    [Fact]
    public void Real_runtime_bridge_promotes_bound_route_budget_into_durable_capture_without_a_route_override()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        bridge.ClearRegistrations();
        var budget = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
        {
            [new NetherInteractiveEventOptionKey(9001, 9002, 1)] = new(190, 2),
        };

        try
        {
            bridge.BindEventProcurementCommitments(budget);

            Assert.Equal(budget, bridge.CaptureRouteOwnedEventProcurementCommitments());

            // A later capture with no new route evidence must not erase a committed branch.
            bridge.BindEventProcurementCommitments(null);
            Assert.Equal(budget, bridge.CaptureRouteOwnedEventProcurementCommitments());

            NetherInteractiveFloorPreEntrySafetyInput input = new(
                NetherFloorNodeType.Event,
                FloorMasterId: 1,
                MapFloorRows: [new NetherFloorMasterBoundsRow(1, 0, 0)],
                EventRows: [new NetherFloorEventMasterRow(9001, 1, 1, 9002, 0, 0, 0)],
                EventPartRows:
                [
                    new NetherFloorEventPartMasterRow(
                        9002,
                        (int)NetherEffectKind.NetherGoldUsed,
                        20,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0
                    ),
                ],
                CurrentErosion: 0,
                ActiveHpPermille: [1000],
                CurrentNetherGold: 200,
                CurrentTreasureKeys: 3,
                Settings: new NetherAutoClimbSettings()
            )
            {
                FloorNodeId = 1,
                CommittedProcurementByOption = bridge.CaptureRouteOwnedEventProcurementCommitments(),
            };
            NetherInteractiveFloorPreEntrySafetyResult safety = new NetherInteractiveFloorPreEntrySafety().Evaluate(input);
            Assert.False(safety.IsSafe);
            Assert.Contains("event-committed-budget-would-break", safety.Detail);
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    [Fact]
    public void Real_runtime_bridge_retires_route_budget_after_authoritative_snapshot_change_and_replans()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        bridge.ClearRegistrations();
        NetherSnapshot before = Snapshot() with
        {
            NetherId = 1,
            MapId = 1,
            MapHash = "route-before",
            CurrentFloorId = 1,
            CurrentNodeId = 1,
        };
        NetherSnapshot after = before with
        {
            MapHash = "route-after-event",
            CurrentFloorId = 2,
            CurrentNodeId = 2,
        };
        var firstKey = new NetherInteractiveEventOptionKey(9101, 9102, 1);
        var secondKey = new NetherInteractiveEventOptionKey(9201, 9202, 1);
        NetherRouteBranchIdentity firstIdentity = new(
            before.Fingerprint,
            before.CurrentNodeId,
            2,
            "1>2>3"
        );
        NetherRouteBranchIdentity secondIdentity = new(
            after.Fingerprint,
            after.CurrentNodeId,
            3,
            "2>3>4"
        );

        try
        {
            bridge.BeginRouteReplan(before.Fingerprint);
            bridge.CommitRouteOwnedEventProcurementCommitments(
                firstIdentity,
                new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
                {
                    [firstKey] = new(200, 1),
                }
            );
            Assert.Contains(firstKey, bridge.CaptureRouteOwnedEventProcurementCommitments().Keys);

            bridge.ObserveAuthoritativeRouteSnapshot(after.Fingerprint);
            Assert.Empty(bridge.CaptureRouteOwnedEventProcurementCommitments());

            bridge.BeginRouteReplan(after.Fingerprint);
            bridge.CommitRouteOwnedEventProcurementCommitments(
                secondIdentity,
                new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
                {
                    [secondKey] = new(300, 0),
                }
            );
            Assert.Contains(secondKey, bridge.CaptureRouteOwnedEventProcurementCommitments().Keys);
            Assert.DoesNotContain(firstKey, bridge.CaptureRouteOwnedEventProcurementCommitments().Keys);
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    [Fact]
    public void Real_runtime_bridge_drops_pending_budget_before_new_snapshot_capture()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        bridge.ClearRegistrations();
        NetherSnapshot before = Snapshot() with { MapId = 1, MapHash = "pending-before" };
        NetherSnapshot after = before with { MapHash = "pending-after", CurrentNodeId = 2 };
        var key = new NetherInteractiveEventOptionKey(9301, 9302, 1);
        var budget = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
        {
            [key] = new(200, 1),
        };

        try
        {
            bridge.BeginRouteReplan(before.Fingerprint);
            bridge.BindEventProcurementCommitments(budget);
            Assert.Contains(key, bridge.CaptureRouteOwnedEventProcurementCommitments().Keys);

            bridge.ObserveAuthoritativeRouteSnapshot(after.Fingerprint);

            Assert.Empty(bridge.CaptureRouteOwnedEventProcurementCommitments());
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    [Fact]
    public void Real_runtime_bridge_observing_a_new_snapshot_invalidates_cached_interactive_provider_scope()
    {
        NetherRuntimeBridge bridge = new();
        NetherSnapshot before = Snapshot() with { MapHash = "provider-before" };
        NetherSnapshot after = before with { MapHash = "provider-after" };
        NetherRuntimeInteractivePreEntryInputsResult cached =
            NetherRuntimeInteractivePreEntryInputsResult.Success(
                new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>(),
                before.Fingerprint,
                new NetherStrategyTypedSemanticProviderEvidence
                {
                    CanonicalRewardTiers =
                    [new NetherCanonicalRewardTierProviderEvidence(701, NetherCanonicalRewardTier.GoldRankFive, 91)],
                }
            );
        FieldInfo cachedField = typeof(NetherRuntimeBridge).GetField(
            "_latestInteractivePreEntryInputs",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        FieldInfo fingerprintField = typeof(NetherRuntimeBridge).GetField(
            "_authoritativeRouteSnapshotFingerprint",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        cachedField.SetValue(bridge, cached);
        fingerprintField.SetValue(bridge, before.Fingerprint);

        bridge.ObserveAuthoritativeRouteSnapshot(after.Fingerprint);

        Assert.Null(cachedField.GetValue(bridge));
    }

    [Fact]
    public void Production_binding_preserves_a_branch_local_gold_budget_into_the_commitment()
    {
        NetherEffect spend = new(NetherEffectKind.NetherGoldUsed, 20);
        NetherEventOption popupOption = Popup(711, 712, spend).Options[0] with
        {
            CommittedGoldMinimum = 150,
        };
        NetherRuntimePopupContext popup = Popup(711, 712, spend) with
        {
            Options = [popupOption],
        };
        NetherSnapshot snapshot = Snapshot() with { NetherGold = 200 };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            Package(snapshot, research: null, spend, new NetherAutoClimbSettings()),
            Interactive(snapshot, popup, spend, null),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        NetherEventCommitment commitment = Assert.Single(bound.ExpectedEventCommitments).Value;
        Assert.Equal(150, commitment.CommittedGoldMinimum);
        Assert.True(bound.Options[0].StrategyEvidence!.IsKnown);
    }

    [Fact]
    public void Production_binding_rejects_an_event_that_would_break_its_committed_gold_budget()
    {
        NetherEffect spend = new(NetherEffectKind.NetherGoldUsed, 20);
        NetherEventOption popupOption = Popup(711, 712, spend).Options[0] with
        {
            CommittedGoldMinimum = 150,
        };
        NetherRuntimePopupContext popup = Popup(711, 712, spend) with
        {
            Options = [popupOption],
        };
        NetherSnapshot snapshot = Snapshot() with { NetherGold = 160 };
        NetherAutoClimbSettings settings = new() { StrategyMode = NetherStrategyMode.Equipment };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            Package(snapshot, research: null, spend, settings),
            Interactive(snapshot, popup, spend, null),
            settings
        );
        NetherPopupDispatchDecision dispatch = NetherPopupDispatchPolicy.Decide(
            snapshot,
            bound,
            settings
        );

        NetherEventStrategyEvidence evidence = bound.Options[0].StrategyEvidence!;
        Assert.False(evidence.IsKnown);
        Assert.Contains("committed-budget", evidence.UnknownReason);
        Assert.Empty(bound.ExpectedEventCommitments);
        Assert.Equal(NetherPopupDispatchKind.Pause, dispatch.Kind);
        Assert.NotEqual(NetherPopupDispatchKind.NativeAction, dispatch.Kind);
    }

    [Fact]
    public void Production_binding_preserves_gold_and_key_minima_for_a_compound_spend()
    {
        NetherEffect goldSpend = new(NetherEffectKind.NetherGoldUsed, 20);
        NetherEffect keySpend = new(NetherEffectKind.TreasureKeyUsed, 1);
        NetherEventOption option = new(1, [goldSpend, keySpend])
        {
            EventId = 713,
            EventPartId = 714,
            RequiresExactBinding = true,
            CommittedGoldMinimum = 150,
            CommittedKeyMinimum = 2,
        };
        NetherRuntimePopupContext popup = new()
        {
            Kind = NetherRuntimePopupKind.Event,
            RawFloorType = 4,
            Options = [option],
        };
        NetherSnapshot snapshot = Snapshot() with
        {
            NetherGold = 200,
            TreasureKeyCount = 3,
        };
        NetherAutoClimbSettings settings = new() { StrategyMode = NetherStrategyMode.Equipment };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            PackageWithOptions(snapshot, [], [option]),
            InteractiveOptions(snapshot, [option], option),
            settings
        );

        NetherEventCommitment commitment = Assert.Single(bound.ExpectedEventCommitments).Value;
        Assert.Equal(150, commitment.CommittedGoldMinimum);
        Assert.Equal(2, commitment.CommittedKeyMinimum);
        Assert.True(bound.Options[0].StrategyEvidence!.IsKnown);
    }

    [Fact]
    public void Equipment_dispatch_prefers_gold_reward_that_crosses_its_committed_threshold_over_code_offer()
    {
        NetherEventOption goldThreshold = new(
            1,
            [new NetherEffect(NetherEffectKind.NetherGoldGain, 200)]
        )
        {
            EventId = 720,
            EventPartId = 721,
            FloorId = 10,
            NodeId = 1,
            RequiresExactBinding = true,
        };
        NetherEventOption codeOffer = new(
            2,
            [new NetherEffect(NetherEffectKind.AbyssCodeOffer, 0)]
        )
        {
            EventId = 720,
            EventPartId = 722,
            FloorId = 10,
            NodeId = 1,
            RequiresExactBinding = true,
        };
        NetherSnapshot snapshot = Snapshot() with { NetherGold = 400 };
        NetherAutoClimbSettings settings = new() { StrategyMode = NetherStrategyMode.Equipment };
        NetherEventProcurementBudget budget = new(GoldMinimum: 500, KeyMinimum: 0);
        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                Options = [goldThreshold, codeOffer],
            },
            PackageWithOptions(snapshot, [], [goldThreshold, codeOffer]),
            InteractiveOptionsWithBudget(
                snapshot,
                [goldThreshold, codeOffer],
                new NetherInteractiveEventOptionKey(720, 721, 1),
                budget
            ),
            settings
        );

        NetherPopupDispatchDecision dispatch = NetherPopupDispatchPolicy.Decide(snapshot, bound, settings);

        Assert.Equal(NetherPopupDispatchKind.NativeAction, dispatch.Kind);
        Assert.Equal(1, dispatch.Action.OptionNumber);
        Assert.Equal(500, bound.Options[0].CommittedGoldMinimum);
    }

    [Fact]
    public void Equipment_dispatch_prefers_key_reward_that_crosses_its_committed_threshold_over_code_offer()
    {
        NetherEventOption keyThreshold = new(
            1,
            [new NetherEffect(NetherEffectKind.TreasureKeyGain, 1)]
        )
        {
            EventId = 730,
            EventPartId = 731,
            FloorId = 10,
            NodeId = 1,
            RequiresExactBinding = true,
        };
        NetherEventOption codeOffer = new(
            2,
            [new NetherEffect(NetherEffectKind.AbyssCodeOffer, 0)]
        )
        {
            EventId = 730,
            EventPartId = 732,
            FloorId = 10,
            NodeId = 1,
            RequiresExactBinding = true,
        };
        NetherSnapshot snapshot = Snapshot() with { TreasureKeyCount = 1 };
        NetherAutoClimbSettings settings = new() { StrategyMode = NetherStrategyMode.Equipment };
        NetherEventProcurementBudget budget = new(GoldMinimum: 0, KeyMinimum: 2);
        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                Options = [keyThreshold, codeOffer],
            },
            PackageWithOptions(snapshot, [], [keyThreshold, codeOffer]),
            InteractiveOptionsWithBudget(
                snapshot,
                [keyThreshold, codeOffer],
                new NetherInteractiveEventOptionKey(730, 731, 1),
                budget
            ),
            settings
        );

        NetherPopupDispatchDecision dispatch = NetherPopupDispatchPolicy.Decide(snapshot, bound, settings);

        Assert.Equal(NetherPopupDispatchKind.NativeAction, dispatch.Kind);
        Assert.Equal(1, dispatch.Action.OptionNumber);
        Assert.Equal(2, bound.Options[0].CommittedKeyMinimum);
    }

    [Theory]
    [InlineData((int)NetherEffectKind.NetherGoldGain)]
    [InlineData((int)NetherEffectKind.TreasureKeyGain)]
    public void Production_binding_rejects_negative_resource_content_id_as_unknown(int rawKind)
    {
        NetherEffectKind kind = (NetherEffectKind)rawKind;
        NetherEffect invalidResource = new(kind, 1) { ContentId = -1 };
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            Popup(711, 712, invalidResource),
            Package(snapshot, research: null, invalidResource, new NetherAutoClimbSettings()),
            Interactive(snapshot, Popup(711, 712, invalidResource), invalidResource, null),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        Assert.False(bound.Options[0].StrategyEvidence!.IsKnown);
        Assert.NotEmpty(bound.Options[0].StrategyEvidence!.UnknownReason);
        Assert.Empty(bound.ExpectedEventCommitments);
    }

    [Fact]
    public void Production_binding_rejects_out_of_domain_item_rarity_as_option_local_unknown()
    {
        NetherEventRewardEvidence reward = new(701, 701, 91, (NetherRewardRarity)999, 1);
        NetherEffect item = new(NetherEffectKind.Item, 1)
        {
            ContentId = 701,
            RewardEvidence = reward,
        };
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            Popup(711, 712, item),
            Package(snapshot, research: null, item, new NetherAutoClimbSettings()),
            Interactive(snapshot, Popup(711, 712, item), item, reward),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        Assert.False(bound.Options[0].StrategyEvidence!.IsKnown);
        Assert.NotEmpty(bound.Options[0].StrategyEvidence!.UnknownReason);
        Assert.Empty(bound.ExpectedEventCommitments);
    }

    [Fact]
    public void Production_binding_rejects_positive_out_of_int_item_type_as_option_local_unknown()
    {
        NetherEffect item = new(NetherEffectKind.Item, 1)
        {
            ContentId = 701,
        };
        NetherRuntimePopupContext popup = Popup(701, 702, item);
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidencePackage package = Package(
            snapshot,
            research: null,
            item,
            new NetherAutoClimbSettings()
        );
        NetherStrategyVisibleMapEvidence visible = package.VisibleMap.Value!;
        package = package with
        {
            VisibleMap = NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Known(
                visible with
                {
                    ContentRows = visible.ContentRows
                        .Select(row => row.Kind == NetherStrategyVisibleContentKind.Item
                            && row.EventId == 701
                            && row.EventPartId == 702
                            ? row with
                            {
                                ItemType = (long)int.MaxValue + 1,
                                ItemRarity = (int)NetherRewardRarity.UniqueWeapon,
                                Amount = 1,
                                IsKnown = true,
                            }
                            : row)
                        .ToArray(),
                }
            ),
        };
        Assert.Single(package.VisibleMap.Value!.ContentRows, row =>
            row.Kind == NetherStrategyVisibleContentKind.Item
            && row.EventId == 701
            && row.EventPartId == 702);
        Assert.Equal(
            (long)int.MaxValue + 1,
            package.VisibleMap.Value!.ContentRows.Single(row =>
                row.Kind == NetherStrategyVisibleContentKind.Item
                && row.EventId == 701
                && row.EventPartId == 702).ItemType
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            Interactive(snapshot, popup, item, reward: null),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        Assert.False(bound.Options[0].StrategyEvidence!.IsKnown);
        Assert.NotEmpty(bound.Options[0].StrategyEvidence!.UnknownReason);
        Assert.Empty(bound.ExpectedEventCommitments);
    }

    [Fact]
    public void Production_binding_carries_authoritative_typed_event_battle_into_event_policy()
    {
        NetherEffect battle = new(NetherEffectKind.Battle, 1);
        NetherRuntimePopupContext popup = Popup(711, 712, battle);
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidencePackage package = Package(
            snapshot,
            research: null,
            battle,
            new NetherAutoClimbSettings()
        );
        NetherStrategyVisibleMapEvidence visible = package.VisibleMap.Value!;
        package = package with
        {
            VisibleMap = NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Known(
                visible with
                {
                    ContentRows = visible.ContentRows
                        .Append(new NetherStrategyVisibleContentRow(
                            NetherStrategyVisibleContentKind.Battle,
                            snapshot.CurrentNodeId,
                            1,
                            0
                        )
                        {
                            EventId = 711,
                            EventPartId = 712,
                            IsKnown = true,
                            BattleStageId = 713,
                            BattleType = 9,
                            CodeDropRatio = 1000,
                            EventBattleTier = NetherEventBattleTier.Boss,
                        })
                        .ToArray(),
                }
            ),
        };
        NetherRuntimeInteractivePreEntryInputsResult interactive = Interactive(
            snapshot,
            popup,
            battle,
            reward: null
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            interactive,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        NetherEventBattleEvidence evidence = Assert.IsType<NetherEventBattleEvidence>(
            bound.Options[0].BattleEvidence
        );
        Assert.True(evidence.IsKnown, evidence.UnknownReason);
        Assert.Equal(NetherEventBattleTier.Boss, evidence.SemanticTier);
        NetherEventDecision decision = new NetherEventPolicy().DecideEvent(
            snapshot,
            bound.Options,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment },
            [],
            bound.EventStrategyEvidence
        );
        Assert.True(bound.Options[0].StrategyEvidence?.IsKnown, bound.Options[0].StrategyEvidence?.UnknownReason);
        Assert.True(decision.Kind == NetherEventDecisionKind.Select, decision.Detail);
    }

    [Fact]
    public void Production_binding_prefers_exact_typed_projection_over_native_unknown_battle_evidence()
    {
        NetherEffect battle = new(NetherEffectKind.Battle, 1);
        NetherRuntimePopupContext popup = Popup(711, 712, battle);
        NetherEventBattleEvidence rawUnknown = NetherEventBattleEvidence.Unknown(
            1,
            "native-battle-semantic-tier-unavailable"
        ) with
        {
            BattleStageId = 713,
            BattleType = 9,
            CodeDropRatio = 1000,
        };
        popup = popup with
        {
            Options = [popup.Options[0] with { BattleEvidence = rawUnknown }],
        };
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidencePackage package = Package(
            snapshot,
            research: null,
            battle,
            new NetherAutoClimbSettings()
        );
        NetherStrategyVisibleMapEvidence visible = package.VisibleMap.Value!;
        package = package with
        {
            VisibleMap = NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Known(
                visible with
                {
                    ContentRows = visible.ContentRows
                        .Append(new NetherStrategyVisibleContentRow(
                            NetherStrategyVisibleContentKind.Battle,
                            snapshot.CurrentNodeId,
                            1,
                            0
                        )
                        {
                            EventId = 711,
                            EventPartId = 712,
                            IsKnown = true,
                            BattleStageId = 713,
                            BattleType = 9,
                            CodeDropRatio = 1000,
                            EventBattleTier = NetherEventBattleTier.Unknown,
                        })
                        .ToArray(),
                }
            ),
        };
        NetherEventBattleEvidence typedProjection = new(
            1,
            713,
            9,
            1000,
            NetherEventBattleTier.Boss
        );
        NetherRuntimeInteractivePreEntryInputsResult interactive = Interactive(
            snapshot,
            popup,
            battle,
            reward: null,
            battle: typedProjection
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            interactive,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        NetherEventBattleEvidence evidence = Assert.IsType<NetherEventBattleEvidence>(
            bound.Options[0].BattleEvidence
        );
        Assert.True(evidence.IsKnown, evidence.UnknownReason);
        Assert.Equal(NetherEventBattleTier.Boss, evidence.SemanticTier);
        NetherEventDecision decision = new NetherEventPolicy().DecideEvent(
            snapshot,
            bound.Options,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment },
            [],
            bound.EventStrategyEvidence
        );
        Assert.True(bound.Options[0].StrategyEvidence?.IsKnown, bound.Options[0].StrategyEvidence?.UnknownReason);
        Assert.True(decision.Kind == NetherEventDecisionKind.Select, decision.Detail);
    }

    [Fact]
    public void Production_binding_rejects_known_option_battle_evidence_when_effect_identity_mismatches()
    {
        NetherEffect battle = new(NetherEffectKind.Battle, 2);
        NetherRuntimePopupContext popup = Popup(711, 712, battle) with
        {
            Options =
            [
                Popup(711, 712, battle).Options[0] with
                {
                    BattleEvidence = new NetherEventBattleEvidence(
                        1,
                        713,
                        9,
                        1000,
                        NetherEventBattleTier.Boss
                    ),
                },
            ],
        };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            Package(Snapshot(), research: null, battle, new NetherAutoClimbSettings()),
            Interactive(Snapshot(), popup, battle, reward: null),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        NetherEventBattleEvidence evidence = Assert.IsType<NetherEventBattleEvidence>(
            bound.Options[0].BattleEvidence
        );
        Assert.False(evidence.IsKnown);
        Assert.Contains("identity", evidence.UnknownReason);
        Assert.False(bound.Options[0].StrategyEvidence!.IsKnown);
    }

    [Fact]
    public void Production_event_binding_is_loaded_from_the_AutoNether_assembly_boundary()
    {
        Assert.Equal("AutoNether", typeof(NetherEventProductionEvidenceBinding).Assembly.GetName().Name);
    }

    [Fact]
    public void Equipment_binding_carries_exact_route_and_item_commitment()
    {
        NetherEffect item = new(NetherEffectKind.Item, 1)
        {
            ContentId = 7001,
            RewardEvidence = Reward(7001),
        };
        NetherRuntimePopupContext popup = Popup(701, 702, item);
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidencePackage package = Package(
            snapshot,
            research: null,
            item,
            new NetherAutoClimbSettings()
        );
        NetherRuntimeInteractivePreEntryInputsResult interactive = Interactive(
            snapshot,
            popup,
            item,
            Reward(7001)
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            interactive,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        Assert.NotNull(bound.EventStrategyEvidence);
        Assert.True(bound.EventStrategyEvidence!.IsUsableFor(NetherStrategyMode.Equipment));
        Assert.True(bound.EventStrategyEvidence.HasRouteEvidence);
        Assert.True(bound.EventStrategyEvidence.HasResourceEvidence);
        Assert.True(bound.EventStrategyEvidence.HasSemanticEvidence);
        NetherEventCommitment commitment = Assert.Single(bound.ExpectedEventCommitments).Value;
        Assert.True(commitment.IsValid);
        Assert.Equal(701, commitment.EventId);
        Assert.Equal(702, commitment.EventPartId);
        Assert.Equal(7001, commitment.Reward!.ItemId);
    }

    [Fact]
    public void Research_binding_uses_exact_projected_settlement_when_available()
    {
        NetherEffect offer = new(NetherEffectKind.AbyssCodeOffer, 0);
        NetherRuntimePopupContext popup = Popup(711, 712, offer);
        NetherSnapshot snapshot = Snapshot();
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Rush,
            ResearchSecondaryFamily = NetherCodeFamily.Impact,
        };
        NetherStrategyResearchFamilyState[] families =
        [
            Research(NetherCodeFamily.Rush, projectedKnown: true),
            Research(NetherCodeFamily.Impact, projectedKnown: true),
        ];
        NetherStrategyEvidencePackage package = Package(snapshot, families, offer, settings);
        NetherRuntimeInteractivePreEntryInputsResult interactive = Interactive(snapshot, popup, offer, null);

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            interactive,
            settings
        );

        Assert.NotNull(bound.EventStrategyEvidence);
        Assert.True(bound.EventStrategyEvidence!.IsUsableFor(NetherStrategyMode.Research));
        Assert.True(bound.EventStrategyEvidence.ResearchIncomplete && Assert.Single(bound.Options).StrategyEvidence!.ResearchIncomplete);
        Assert.Empty(bound.EventStrategyEvidence.UnknownReason);
    }

    [Fact]
    public void Research_binding_keeps_primary_priority_when_native_settlement_projection_is_unknown()
    {
        NetherEffect offer = new(NetherEffectKind.AbyssCodeOffer, 0);
        NetherRuntimePopupContext popup = Popup(711, 712, offer);
        NetherSnapshot snapshot = Snapshot();
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Rush,
            ResearchSecondaryFamily = NetherCodeFamily.Impact,
        };
        NetherStrategyEvidencePackage package = Package(
            snapshot,
            [
                Research(NetherCodeFamily.Rush, projectedKnown: false),
                Research(NetherCodeFamily.Impact, projectedKnown: true),
            ],
            offer,
            settings
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            Interactive(snapshot, popup, offer, null),
            settings
        );

        Assert.True(bound.EventStrategyEvidence!.IsUsableFor(NetherStrategyMode.Research) && bound.EventStrategyEvidence.ResearchIncomplete && Assert.Single(bound.Options).StrategyEvidence!.ResearchIncomplete);
        Assert.True(bound.EventStrategyEvidence.UnknownReason.Length == 0 && bound.ExpectedEventCommitments.Count == 1);
    }

    [Fact]
    public void Missing_production_package_keeps_research_event_unknown_and_without_commitment()
    {
        NetherEffect offer = new(NetherEffectKind.AbyssCodeOffer, 0);
        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            Popup(731, 732, offer),
            null,
            null,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Research }
        );

        Assert.False(bound.EventStrategyEvidence!.IsUsableFor(NetherStrategyMode.Research));
        Assert.Contains("package-unavailable", bound.EventStrategyEvidence.UnknownReason);
        Assert.Empty(bound.ExpectedEventCommitments);
    }

    [Fact]
    public void Research_binding_and_dispatch_keep_an_exact_sibling_when_another_option_has_unknown_rows()
    {
        NetherEffect unknownItem = new(NetherEffectKind.Item, 1)
        {
            ContentId = 7401,
        };
        NetherEffect knownOffer = new(NetherEffectKind.AbyssCodeOffer, 0);
        NetherEventOption unknownOption = new(1, [unknownItem])
        {
            EventId = 7410,
            EventPartId = 7411,
            RequiresExactBinding = true,
            UnknownReason = "event-item-master-row-unavailable:7401",
        };
        NetherEventOption knownOption = new(2, [knownOffer])
        {
            EventId = 7410,
            EventPartId = 7412,
            RequiresExactBinding = true,
        };
        NetherSnapshot snapshot = Snapshot();
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Rush,
            ResearchSecondaryFamily = NetherCodeFamily.Impact,
        };
        NetherStrategyResearchFamilyState[] families =
        [
            Research(NetherCodeFamily.Rush, projectedKnown: true),
            Research(NetherCodeFamily.Impact, projectedKnown: true),
        ];
        NetherStrategyEvidencePackage package = PackageWithOptions(snapshot, families, [unknownOption, knownOption]);
        NetherRuntimeInteractivePreEntryInputsResult interactive = InteractiveOptions(
            snapshot,
            [unknownOption, knownOption],
            knownOption
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                Options = [unknownOption, knownOption],
            },
            package,
            interactive,
            settings
        );
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            snapshot,
            bound,
            settings
        );

        Assert.True(bound.Options[1].StrategyEvidence?.IsUsableFor(NetherStrategyMode.Research));
        Assert.False(bound.Options[0].StrategyEvidence?.IsUsableFor(NetherStrategyMode.Research));
        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(2, decision.Action.OptionNumber);
    }

    [Fact]
    public void Research_dispatch_cannot_reselect_an_uncommitted_erosion_increase_offer()
    {
        // Captured current-game event 31 at floor 12: part 20002 is +40 erosion plus
        // content_type=160 (Code Offer); part 20003 spends 30 Gold and heals 100 HP.
        NetherEventOption riskOffer = new(2,
        [
            new NetherEffect(NetherEffectKind.Erosion, 40),
            new NetherEffect(NetherEffectKind.AbyssCodeOffer, 0),
        ])
        {
            EventId = 31,
            EventPartId = 20002,
            RequiresExactBinding = true,
        };
        NetherEventOption safeChoice = new(3,
        [
            new NetherEffect(NetherEffectKind.NetherGoldUsed, 30),
            new NetherEffect(NetherEffectKind.Heal, 100),
        ])
        {
            EventId = 31,
            EventPartId = 20003,
            RequiresExactBinding = true,
        };
        NetherSnapshot snapshot = Snapshot() with
        {
            CurrentFloorId = 58,
            CurrentNodeId = 55834574850,
            ErosionPoint = 45,
            NetherGold = 160,
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Risk,
            ResearchSecondaryFamily = NetherCodeFamily.Impact,
        };
        NetherStrategyResearchFamilyState[] families =
        [
            Research(NetherCodeFamily.Risk, projectedKnown: true),
            Research(NetherCodeFamily.Impact, projectedKnown: true),
        ];
        NetherRuntimePopupContext popup = new()
        {
            Kind = NetherRuntimePopupKind.Event,
            RawFloorType = (int)NetherFloorNodeType.Event,
            TargetCharacterId = 101,
            Options = [riskOffer, safeChoice],
        };

        NetherRuntimeInteractivePreEntryInputsResult interactive =
            InteractiveFloorTwelveAtErosionFortyFive(snapshot, settings);
        NetherInteractiveFloorPreEntrySafetyResult preEntry = Assert.Single(
            interactive.ByFloorNodeId
        ).Value.Safety;
        Assert.Equal(3, preEntry.SafeOptionNumberByEventId[31]);
        Assert.False(preEntry.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(31, 20002, 2)
        ].IsSelected);

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            PackageWithOptions(snapshot, families, [riskOffer, safeChoice]),
            interactive,
            settings
        );
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            snapshot,
            bound,
            settings
        );

        Assert.False(bound.Options[0].StrategyEvidence!.IsUsableFor(NetherStrategyMode.Research));
        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(3, decision.Action.OptionNumber);
    }

    [Fact]
    public void Production_binding_rejects_an_interactive_snapshot_mismatch_before_commitment()
    {
        NetherEffect item = new(NetherEffectKind.Item, 1)
        {
            ContentId = 7501,
            RewardEvidence = Reward(7501),
        };
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimePopupContext popup = Popup(701, 702, item);
        NetherStrategyEvidencePackage package = Package(
            snapshot,
            research: null,
            item,
            new NetherAutoClimbSettings()
        );
        NetherRuntimeInteractivePreEntryInputsResult interactive = Interactive(
            snapshot,
            popup,
            item,
            Reward(7501)
        ) with
        {
            SnapshotFingerprint = new NetherSnapshotFingerprint(
                snapshot.Fingerprint.Status,
                snapshot.Fingerprint.NetherId,
                snapshot.Fingerprint.MapId,
                snapshot.Fingerprint.FloorLevel,
                snapshot.Fingerprint.FloorIndex,
                snapshot.Fingerprint.ErosionPoint + 1,
                snapshot.Fingerprint.CharacterHpHash,
                snapshot.Fingerprint.CodeHash,
                snapshot.Fingerprint.MapHash,
                snapshot.Fingerprint.CurrentFloorId,
                snapshot.Fingerprint.TicketCount,
                snapshot.Fingerprint.TreasureKeyCount,
                snapshot.Fingerprint.NetherGold,
                snapshot.Fingerprint.CodeReloadCount,
                snapshot.Fingerprint.LockReward,
                snapshot.Fingerprint.CurrentNodeId
            ),
        };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            interactive,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        Assert.False(bound.Options[0].StrategyEvidence!.IsKnown);
        Assert.Empty(bound.ExpectedEventCommitments);
    }

    [Fact]
    public void Production_binding_rejects_ambiguous_same_option_projection_across_floor_nodes()
    {
        NetherEffect offer = new(NetherEffectKind.AbyssCodeOffer, 0);
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimePopupContext popup = Popup(711, 712, offer);
        NetherStrategyEvidencePackage package = Package(
            snapshot,
            research: null,
            offer,
            new NetherAutoClimbSettings()
        );
        NetherRuntimeInteractivePreEntryInputsResult original = Interactive(
            snapshot,
            popup,
            offer,
            null
        );
        NetherRuntimeInteractivePreEntryCaptureResult first =
            original.ByFloorNodeId[snapshot.CurrentNodeId];
        NetherInteractiveOptionProjection duplicateProjection = new(
            1,
            ErosionDelta: 0,
            HpDelta: 0,
            ExpectedEffects: [offer]
        )
        {
            EventId = popup.Options[0].EventId,
            EventPartId = popup.Options[0].EventPartId,
            FloorId = snapshot.CurrentFloorId + 1,
            NodeId = snapshot.CurrentNodeId + 1,
            HasRouteSafetyEvidence = true,
            RouteSafetyAllowed = true,
        };
        NetherRuntimeInteractivePreEntryCaptureResult second = first with
        {
            Input = first.Input! with { FloorNodeId = snapshot.CurrentNodeId + 1 },
            Safety = NetherInteractiveFloorPreEntrySafetyResult.Safe(
                new Dictionary<long, int> { [popup.Options[0].EventId] = 1 },
                new Dictionary<long, NetherInteractiveOptionProjection>
                {
                    [popup.Options[0].EventId] = duplicateProjection,
                },
                optionProjections: new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                {
                    [new NetherInteractiveEventOptionKey(
                        popup.Options[0].EventId,
                        popup.Options[0].EventPartId,
                        popup.Options[0].OptionNumber
                    )] = duplicateProjection,
                }
            ),
        };
        NetherRuntimeInteractivePreEntryInputsResult ambiguous = original with
        {
            ByFloorNodeId = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
            {
                [snapshot.CurrentNodeId] = first,
                [snapshot.CurrentNodeId + 1] = second,
            },
        };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            ambiguous,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        Assert.False(bound.Options[0].StrategyEvidence!.IsKnown);
        Assert.Empty(bound.ExpectedEventCommitments);
    }

    [Fact]
    public void Research_binding_rejects_a_non_rank_five_partial_death_gate()
    {
        NetherEffect damage = new(NetherEffectKind.Damage, 1);
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimePopupContext popup = Popup(711, 712, damage) with
        {
            Options =
            [
                Popup(711, 712, damage).Options[0] with
                {
                    PartialDeathEligibility = new NetherInteractivePartialDeathEligibility(
                        NetherInteractivePartialDeathObjectiveKind.HpPaidEventKeyForRank5Treasure,
                        711,
                        712,
                        snapshot.CurrentNodeId
                    )
                    {
                        IsKnown = true,
                        ObjectiveReachable = true,
                        ExactTreasureRank = 4,
                        NoBetterAffordableCurrencyKeySource = true,
                    },
                },
            ],
        };
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Rush,
            ResearchSecondaryFamily = NetherCodeFamily.Impact,
        };
        NetherStrategyEvidencePackage package = Package(
            snapshot,
            [
                Research(NetherCodeFamily.Rush, projectedKnown: true),
                Research(NetherCodeFamily.Impact, projectedKnown: true),
            ],
            damage,
            settings
        );
        NetherRuntimeInteractivePreEntryInputsResult interactive = Interactive(
            snapshot,
            popup,
            damage,
            null,
            allowsPartialActiveDeaths: true
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            interactive,
            settings
        );

        Assert.False(bound.Options[0].StrategyEvidence!.IsKnown);
        Assert.False(bound.Options[0].StrategyEvidence!.HasRankFiveTreasureObjective);
    }

    [Fact]
    public void Production_binding_rejects_partial_death_when_the_exact_option_proof_is_missing()
    {
        NetherEffect damage = new(NetherEffectKind.Damage, 1);
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimePopupContext popup = Popup(715, 716, damage);
        NetherStrategyEvidencePackage package = Package(
            snapshot,
            research: null,
            damage,
            new NetherAutoClimbSettings()
        );
        NetherRuntimeInteractivePreEntryInputsResult interactive = Interactive(
            snapshot,
            popup,
            damage,
            null,
            allowsPartialActiveDeaths: true
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            package,
            interactive,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        Assert.False(bound.Options[0].StrategyEvidence!.HasPartialDeathEvidence);
        Assert.False(bound.Options[0].StrategyEvidence!.IsKnown);
        Assert.Empty(bound.ExpectedEventCommitments);
    }

    [Fact]
    public void Production_binding_carries_exact_rank_five_partial_death_proof_from_preentry_projection()
    {
        NetherEffect damage = new(NetherEffectKind.Damage, 1);
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimePopupContext popup = Popup(711, 712, damage);
        NetherRankFiveTreasureIdentity objective = new(9001, 9002, 9003);
        NetherRankFiveKeyProcurementCommitment procurement = new()
        {
            Objective = objective,
            SourceKind = NetherKeyProcurementSourceKind.HpPaidEventKey,
            SourceNodeId = snapshot.CurrentNodeId,
            SourceEventId = 711,
            SourceEventPartId = 712,
            SourceOptionNumber = 1,
        };
        NetherInteractivePartialDeathEligibility proof = new(
            NetherInteractivePartialDeathObjectiveKind.HpPaidEventKeyForRank5Treasure,
            711,
            712,
            snapshot.CurrentNodeId
        )
        {
            IsKnown = true,
            ObjectiveReachable = true,
            ExactTreasureRank = 5,
            NoBetterAffordableCurrencyKeySource = true,
        };
        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            Package(snapshot, research: null, damage, new NetherAutoClimbSettings()),
            Interactive(
                snapshot,
                popup,
                damage,
                null,
                allowsPartialActiveDeaths: true,
                partialDeathEligibility: proof,
                rankFiveCommitment: procurement,
                rankFiveObjective: objective
            ),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        Assert.Equal(proof, bound.Options[0].PartialDeathEligibility);
        Assert.True(bound.Options[0].StrategyEvidence!.HasPartialDeathEvidence);
        Assert.True(bound.Options[0].StrategyEvidence!.HasRankFiveTreasureObjective);
        NetherEventCommitment commitment = Assert.Single(bound.ExpectedEventCommitments).Value;
        Assert.Equal(proof, commitment.PartialDeathEligibility);
        Assert.Equal(procurement, commitment.RankFiveKeyProcurementCommitment);
        Assert.Equal(objective, commitment.RankFiveTreasureObjective);
        Assert.True(commitment.IsValid);
    }

    [Fact]
    public void Production_rank_five_event_commitment_survives_binding_dispatch_and_reconciliation()
    {
        NetherEffect gain = new(NetherEffectKind.NetherGoldGain, 10);
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimePopupContext popup = Popup(711, 712, gain);
        NetherRankFiveTreasureIdentity objective = new(9001, 9002, 9003);
        NetherRankFiveKeyProcurementCommitment procurement = new()
        {
            Objective = objective,
            SourceKind = NetherKeyProcurementSourceKind.EventGold150,
            SourceNodeId = snapshot.CurrentNodeId,
            SourceEventId = 711,
            SourceEventPartId = 712,
            SourceOptionNumber = 1,
            GoldCost = 150,
        };
        NetherAutoClimbSettings settings = new() { StrategyMode = NetherStrategyMode.Equipment };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            popup,
            Package(snapshot, research: null, gain, settings),
            Interactive(
                snapshot,
                popup,
                gain,
                reward: null,
                rankFiveCommitment: procurement,
                rankFiveObjective: objective
            ),
            settings
        );
        NetherPopupDispatchDecision dispatched = NetherPopupDispatchPolicy.Decide(snapshot, bound, settings);

        Assert.Equal(NetherPopupDispatchKind.NativeAction, dispatched.Kind);
        Assert.Equal(procurement, dispatched.Action.EventCommitment!.RankFiveKeyProcurementCommitment);
        Assert.Equal(objective, dispatched.Action.EventCommitment.RankFiveTreasureObjective);
        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(
                dispatched.Action,
                snapshot,
                snapshot with { NetherGold = 110 }
            )
        );
    }

    [Fact]
    public void Production_rank_five_shop_commitment_survives_binding_dispatch_and_reconciliation()
    {
        NetherSnapshot snapshot = Snapshot() with { FloorLevel = 91 };
        NetherRankFiveTreasureIdentity objective = new(9001, 9002, 9003);
        NetherRankFiveKeyProcurementCommitment procurement = new()
        {
            Objective = objective,
            SourceKind = NetherKeyProcurementSourceKind.ShopGold200,
            SourceNodeId = 3,
            SourceContentId = 3001,
            GoldCost = 200,
        };
        NetherRankFiveKeyProcurementDecision decision = new()
        {
            IsKnown = true,
            HasMandatoryObjective = true,
            Objective = objective,
            SourceKind = NetherKeyProcurementSourceKind.ShopGold200,
            GoldCost = 200,
            Commitment = procurement,
        };
        NetherRuntimePopupContext popup = new()
        {
            Kind = NetherRuntimePopupKind.Shop,
            ShopContents =
            [
                new NetherShopContent(3001, 0, 0, NetherRewardRarity.NoEffect, 200, usesNetherGold: true)
                {
                    IsTreasureKey = true,
                },
                new NetherShopContent(3002, 9002, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true)
                {
                    CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
                },
            ],
        };
        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.BindRankFiveShopCommitment(
            popup,
            decision
        );
        NetherAutoClimbSettings settings = new()
        {
            StrategyMode = NetherStrategyMode.Equipment,
            ShopMode = NetherShopMode.EquipmentBags,
        };
        NetherPopupDispatchDecision dispatched = NetherPopupDispatchPolicy.Decide(
            snapshot with { NetherGold = 300 },
            bound,
            settings
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, dispatched.Kind);
        Assert.Equal(NetherActionKind.BuyShopItem, dispatched.Action.Kind);
        Assert.Equal(bound.ShopProcurementCommitment, dispatched.Action.ShopProcurementCommitment);
        Assert.True(bound.ShopProcurementCommitment!.RequiresRankFiveBag);
        Assert.Equal(
            NetherActionOutcome.Applied,
            NetherActionReconcilePolicy.Evaluate(
                dispatched.Action,
                snapshot with { NetherGold = 300 },
                snapshot with
                {
                    NetherGold = 100,
                    AcquiredItems = [new NetherRewardItem(3001, 1)],
                }
            )
        );
    }

    [Fact]
    public void Production_shop_commitment_rejects_raw_gold_bag_without_typed_provider()
    {
        NetherRankFiveKeyProcurementDecision decision = new()
        {
            IsKnown = true,
            HasMandatoryObjective = true,
            Objective = new NetherRankFiveTreasureIdentity(9001, 9002, 9003),
            SourceKind = NetherKeyProcurementSourceKind.ShopGold200,
            GoldCost = 200,
            Commitment = new NetherRankFiveKeyProcurementCommitment
            {
                Objective = new NetherRankFiveTreasureIdentity(9001, 9002, 9003),
                SourceKind = NetherKeyProcurementSourceKind.ShopGold200,
                SourceNodeId = 3,
                SourceContentId = 3001,
                GoldCost = 200,
            },
        };
        NetherRuntimePopupContext popup = new()
        {
            Kind = NetherRuntimePopupKind.Shop,
            ShopContents =
            [
                new NetherShopContent(3001, 0, 0, NetherRewardRarity.NoEffect, 200, true)
                {
                    IsTreasureKey = true,
                },
                new NetherShopContent(3002, 9002, 91, NetherRewardRarity.Gold, 300, true),
            ],
        };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.BindRankFiveShopCommitment(
            popup,
            decision
        );

        Assert.NotNull(bound.ShopProcurementCommitment);
        Assert.False(bound.ShopProcurementCommitment!.RequiresRankFiveBag);
        Assert.Equal(0, bound.ShopProcurementCommitment.BagContentId);
    }

    [Fact]
    public void Mixed_capture_to_production_popup_keeps_an_exact_sibling_when_item_and_battle_rows_are_malformed_duplicates()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimeInteractivePreEntryCaptureResult captured =
            new NetherRuntimeInteractivePreEntryInputCapture().Capture(
                new NetherRuntimeInteractivePreEntryCaptureRequest(
                    new RuntimeFloorFixture
                    {
                        MNetherMapFloorId = snapshot.CurrentFloorId,
                        ExtendId = 7800,
                        FloorType = (int)NetherFloorNodeType.Event,
                    },
                    new object[] { new RuntimeMapFloorFixture { id = snapshot.CurrentFloorId, min_erosion_point = 0, max_erosion_point = 100 } },
                    new object[] { new RuntimeEventFixture
                    {
                        id = 7800,
                        m_nether_map_floor_id = snapshot.CurrentFloorId,
                        weight = 1,
                        type = 4,
                        m_nether_floor_event_part_id_1 = 7801,
                        m_nether_floor_event_part_id_2 = 7802,
                        m_nether_floor_event_part_id_3 = 7803,
                    } },
                    new object[]
                    {
                        new RuntimePartFixture
                        {
                            id = 7801,
                            content_type = 30,
                            content_id = 8101,
                            amount = 1,
                        },
                        new RuntimePartFixture
                        {
                            id = 7802,
                            target_type_1 = (int)NetherEffectKind.Battle,
                            select_parameter_1 = 8201,
                        },
                        new RuntimePartFixture
                        {
                            id = 7803,
                            target_type_1 = (int)NetherEffectKind.Heal,
                            select_parameter_1 = 1,
                        },
                    },
                    snapshot.ErosionPoint,
                    [1000],
                    snapshot.NetherGold,
                    snapshot.TreasureKeyCount,
                    new NetherAutoClimbSettings
                    {
                        SoftErosionLimit = 90,
                        MinimumCharacterHpPermille = 300,
                    },
                    CanCloseShop: false
                )
                {
                    FloorNodeId = snapshot.CurrentNodeId,
                    ItemRows = new object[]
                    {
                        new RuntimeMalformedItemFixture { id = 8101 },
                        new RuntimeItemFixture { id = 8101, type = 91, rarity = 3, value = 1, possession_limit = 99 },
                    },
                    BattleRows = new object[]
                    {
                        new RuntimeMalformedBattleFixture { id = 8201 },
                        new RuntimeBattleFixture
                        {
                            id = 8201,
                            m_nether_map_floor_id = snapshot.CurrentFloorId,
                            type = 1,
                            m_nether_battle_stage_id = 8202,
                            code_drop_ratio = 100,
                        },
                    },
                }
            );

        Assert.True(captured.IsCaptured);
        Assert.True(captured.Safety.IsSafe, captured.Safety.Detail);
        Assert.False(captured.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(7800, 7801, 1)
        ].IsKnown);
        Assert.False(captured.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(7800, 7802, 2)
        ].IsKnown);
        Assert.True(captured.Safety.OptionProjectionByKey[
            new NetherInteractiveEventOptionKey(7800, 7803, 3)
        ].IsKnown);
        NetherEventOption item = new(1, [new NetherEffect(NetherEffectKind.Item, 1) { ContentId = 8101 }])
        {
            EventId = 7800,
            EventPartId = 7801,
            RequiresExactBinding = true,
            UnknownReason = "event-item-master-row-unavailable:8101",
        };
        NetherEventOption battle = new(2, [new NetherEffect(NetherEffectKind.Battle, 8201)
        {
            IsOptionalBattle = true,
        }])
        {
            EventId = 7800,
            EventPartId = 7802,
            RequiresExactBinding = true,
            UnknownReason = "event-battle-master-row-unavailable:8201",
        };
        NetherEventOption heal = new(3, [new NetherEffect(NetherEffectKind.Heal, 1)])
        {
            EventId = 7800,
            EventPartId = 7803,
            RequiresExactBinding = true,
        };
        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = (int)NetherFloorNodeType.Event,
                TargetCharacterId = 101,
                Options = [item, battle, heal],
            },
            MixedPackage(snapshot, item, battle, heal),
            NetherRuntimeInteractivePreEntryInputsResult.Success(
                new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
                {
                    [snapshot.CurrentNodeId] = captured,
                },
                snapshot.Fingerprint
            ),
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );
        NetherPopupDispatchDecision dispatch = NetherPopupDispatchPolicy.Decide(
            snapshot,
            bound,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment }
        );

        Assert.False(bound.Options[0].StrategyEvidence!.IsKnown);
        Assert.False(bound.Options[1].StrategyEvidence!.IsKnown);
        Assert.True(bound.Options[2].StrategyEvidence!.IsKnown);
        Assert.Equal(NetherPopupDispatchKind.NativeAction, dispatch.Kind);
        Assert.Equal(3, dispatch.Action.OptionNumber);
    }

    private static NetherRuntimePopupContext Popup(long eventId, long partId, NetherEffect effect) =>
        new()
        {
            Kind = NetherRuntimePopupKind.Event,
            RawFloorType = 4,
            TargetCharacterId = 101,
            Options =
            [
                new NetherEventOption(1, [effect])
                {
                    EventId = eventId,
                    EventPartId = partId,
                    RequiresExactBinding = true,
                    RewardEvidence = effect.RewardEvidence,
                },
            ],
        };

    private static NetherRuntimeInteractivePreEntryInputsResult Interactive(
        NetherSnapshot snapshot,
        NetherRuntimePopupContext popup,
        NetherEffect effect,
        NetherEventRewardEvidence? reward,
        NetherEventBattleEvidence? battle = null,
        bool allowsPartialActiveDeaths = false,
        NetherInteractivePartialDeathEligibility? partialDeathEligibility = null,
        NetherRankFiveKeyProcurementCommitment? rankFiveCommitment = null,
        NetherRankFiveTreasureIdentity? rankFiveObjective = null
    )
    {
        NetherEventOption option = popup.Options[0];
        var projection = new NetherInteractiveOptionProjection(
            option.OptionNumber,
            ErosionDelta: 0,
            HpDelta: 0,
            ExpectedEffects: [effect]
        )
        {
            EventId = option.EventId,
            EventPartId = option.EventPartId,
            FloorId = snapshot.CurrentFloorId,
            NodeId = snapshot.CurrentNodeId,
            Reward = reward,
            Battle = battle,
            HasRouteSafetyEvidence = true,
            RouteSafetyAllowed = true,
            AllowsPartialActiveDeaths = allowsPartialActiveDeaths,
            PartialDeathEligibility = partialDeathEligibility,
            IsMandatoryRankFiveKeyObjective = partialDeathEligibility?.AllowsHpPaidEventKey == true
                && partialDeathEligibility.ExactTreasureRank == 5,
            RankFiveKeyProcurementCommitment = rankFiveCommitment,
            RankFiveTreasureObjective = rankFiveObjective,
        };
        return NetherRuntimeInteractivePreEntryInputsResult.Success(
            new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
            {
                [1] = new NetherRuntimeInteractivePreEntryCaptureResult
                {
                    IsCaptured = true,
                    Input = new NetherInteractiveFloorPreEntrySafetyInput(
                        NetherFloorNodeType.Event,
                        snapshot.CurrentFloorId,
                        [],
                        [],
                        [],
                        snapshot.ErosionPoint,
                        [1000],
                        snapshot.NetherGold,
                        snapshot.TreasureKeyCount,
                        new NetherAutoClimbSettings()
                    )
                    {
                        FloorNodeId = snapshot.CurrentNodeId,
                    },
                    Safety = NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [option.EventId] = option.OptionNumber },
                        new Dictionary<long, NetherInteractiveOptionProjection>
                        {
                            [option.EventId] = projection,
                        }
                    ),
                },
            },
            snapshot.Fingerprint
        );
    }

    private static NetherRuntimeInteractivePreEntryInputsResult InteractiveOptions(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherEventOption selected
    )
    {
        var projection = new NetherInteractiveOptionProjection(
            selected.OptionNumber,
            ErosionDelta: 0,
            HpDelta: 0,
            ExpectedEffects: selected.Effects
        )
        {
            EventId = selected.EventId,
            EventPartId = selected.EventPartId,
            FloorId = snapshot.CurrentFloorId,
            NodeId = snapshot.CurrentNodeId,
            HasRouteSafetyEvidence = true,
            RouteSafetyAllowed = true,
        };
        return NetherRuntimeInteractivePreEntryInputsResult.Success(
            new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
            {
                [snapshot.CurrentNodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
                {
                    IsCaptured = true,
                    Input = new NetherInteractiveFloorPreEntrySafetyInput(
                        NetherFloorNodeType.Event,
                        snapshot.CurrentFloorId,
                        [],
                        [],
                        [],
                        snapshot.ErosionPoint,
                        [1000],
                        snapshot.NetherGold,
                        snapshot.TreasureKeyCount,
                        new NetherAutoClimbSettings()
                    )
                    {
                        FloorNodeId = snapshot.CurrentNodeId,
                    },
                    Safety = NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        new Dictionary<long, int> { [selected.EventId] = selected.OptionNumber },
                        new Dictionary<long, NetherInteractiveOptionProjection>
                        {
                            [selected.EventId] = projection,
                        },
                        optionProjections: new Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection>
                        {
                            [new NetherInteractiveEventOptionKey(selected.EventId, selected.EventPartId, selected.OptionNumber)] = projection,
                        }
                    ),
                },
            },
            snapshot.Fingerprint
        );
    }

    private static NetherRuntimeInteractivePreEntryInputsResult InteractiveFloorTwelveAtErosionFortyFive(
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings
    )
    {
        var input = new NetherInteractiveFloorPreEntrySafetyInput(
            NetherFloorNodeType.Event,
            snapshot.CurrentFloorId,
            [new NetherFloorMasterBoundsRow(snapshot.CurrentFloorId, 0, 100)],
            [new NetherFloorEventMasterRow(31, snapshot.CurrentFloorId, 1, 20001, 20002, 20003, 0)],
            [
                new NetherFloorEventPartMasterRow(20001, 2, 100, 0, 0, 0, 0, 31, 210002, 1),
                new NetherFloorEventPartMasterRow(20002, 3, 40, 0, 0, 0, 0, 160, 0, 1),
                new NetherFloorEventPartMasterRow(20003, 5, 30, 1, 100, 0, 0, 0, 0, 0),
            ],
            snapshot.ErosionPoint,
            [1000],
            snapshot.NetherGold,
            snapshot.TreasureKeyCount,
            settings
        )
        {
            FloorExtendId = 31,
            FloorNodeId = snapshot.CurrentNodeId,
            CodeCapacity = 5,
        };
        NetherInteractiveFloorPreEntrySafetyResult safety = new NetherInteractiveFloorPreEntrySafety()
            .Evaluate(input);
        return NetherRuntimeInteractivePreEntryInputsResult.Success(
            new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
            {
                [snapshot.CurrentNodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
                {
                    IsCaptured = true,
                    Input = input,
                    Safety = safety,
                },
            },
            snapshot.Fingerprint
        );
    }

    private static NetherRuntimeInteractivePreEntryInputsResult InteractiveOptionsWithBudget(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherInteractiveEventOptionKey budgetKey,
        NetherEventProcurementBudget budget
    )
    {
        var projections = options.ToDictionary(
            option => new NetherInteractiveEventOptionKey(
                option.EventId,
                option.EventPartId,
                option.OptionNumber
            ),
            option => new NetherInteractiveOptionProjection(
                option.OptionNumber,
                ErosionDelta: 0,
                HpDelta: 0,
                ExpectedEffects: option.Effects
            )
            {
                EventId = option.EventId,
                EventPartId = option.EventPartId,
                FloorId = snapshot.CurrentFloorId,
                NodeId = snapshot.CurrentNodeId,
                HasRouteSafetyEvidence = true,
                RouteSafetyAllowed = true,
                HasCommittedProcurementEvidence = new NetherInteractiveEventOptionKey(
                    option.EventId,
                    option.EventPartId,
                    option.OptionNumber
                ) == budgetKey,
                CommittedGoldMinimum = new NetherInteractiveEventOptionKey(
                    option.EventId,
                    option.EventPartId,
                    option.OptionNumber
                ) == budgetKey ? budget.GoldMinimum : 0,
                CommittedKeyMinimum = new NetherInteractiveEventOptionKey(
                    option.EventId,
                    option.EventPartId,
                    option.OptionNumber
                ) == budgetKey ? budget.KeyMinimum : 0,
            }
        );
        return NetherRuntimeInteractivePreEntryInputsResult.Success(
            new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>
            {
                [snapshot.CurrentNodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
                {
                    IsCaptured = true,
                    Input = new NetherInteractiveFloorPreEntrySafetyInput(
                        NetherFloorNodeType.Event,
                        snapshot.CurrentFloorId,
                        [],
                        [],
                        [],
                        snapshot.ErosionPoint,
                        [1000],
                        snapshot.NetherGold,
                        snapshot.TreasureKeyCount,
                        new NetherAutoClimbSettings()
                    )
                    {
                        FloorNodeId = snapshot.CurrentNodeId,
                    },
                    Safety = NetherInteractiveFloorPreEntrySafetyResult.Safe(
                        options
                            .GroupBy(option => option.EventId)
                            .ToDictionary(group => group.Key, group => group.Min(option => option.OptionNumber)),
                        optionProjections: projections
                    ),
                },
            },
            snapshot.Fingerprint
        );
    }

    private static NetherStrategyEvidencePackage Package(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherStrategyResearchFamilyState>? research,
        NetherEffect effect,
        NetherAutoClimbSettings settings
    )
    {
        NetherEventOption option = new(1, [effect])
        {
            EventId = effect.Kind == NetherEffectKind.Item ? 701 : 711,
            EventPartId = effect.Kind == NetherEffectKind.Item ? 702 : 712,
            RequiresExactBinding = true,
            RewardEvidence = effect.RewardEvidence,
        };
        var visibleRows = new List<NetherStrategyVisibleContentRow>
        {
            new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Event,
                snapshot.CurrentNodeId,
                option.EventId,
                option.EventPartId
            )
            {
                EventId = option.EventId,
                EventPartId = option.EventPartId,
                IsKnown = true,
                EventOptions =
                [
                    new NetherStrategyVisibleEventOptionEvidence(
                        1,
                        option.EventPartId,
                        [new NetherStrategyVisibleEventEffectEvidence(
                            NetherStrategyVisibleEventEffectSource.Content,
                            (int)effect.Kind,
                            effect.ContentId
                        )
                        {
                            EffectKind = effect.Kind,
                            ContentId = effect.ContentId,
                            Amount = effect.Amount,
                            IsKnown = true,
                            IsPresent = true,
                        }]
                    ),
                ],
            },
        };
        if (effect.Kind == NetherEffectKind.Item)
        {
            visibleRows.Add(new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Item,
                snapshot.CurrentNodeId,
                effect.RewardEvidence?.ItemId ?? effect.ContentId,
                effect.ContentId
            )
            {
                EventId = option.EventId,
                EventPartId = option.EventPartId,
                IsKnown = true,
                ItemType = effect.RewardEvidence?.ItemType ?? 0,
                ItemRarity = (int)(effect.RewardEvidence?.Rarity ?? NetherRewardRarity.NoEffect),
                Amount = effect.Amount,
            });
        }
        var visible = new NetherStrategyVisibleMapEvidence([], visibleRows);
        return new NetherStrategyEvidencePackage
        {
            Identity = new NetherStrategyEvidenceIdentity(1, 1, 1, snapshot.Fingerprint),
            Server = new NetherStrategyServerEvidence
            {
                CurrentFloorId = snapshot.CurrentFloorId,
                CurrentNodeId = snapshot.CurrentNodeId,
                NetherGold = snapshot.NetherGold,
                TreasureKeyCount = snapshot.TreasureKeyCount,
                ErosionPoint = snapshot.ErosionPoint,
            },
            Research = research == null
                ? NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence>.Unknown("not-needed")
                : NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence>.Known(
                    new NetherStrategyResearchEvidence(research)
                ),
            VisibleMap = NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Known(visible),
        };
    }

    private static NetherStrategyEvidencePackage PackageWithOptions(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherStrategyResearchFamilyState> research,
        IReadOnlyList<NetherEventOption> options
    )
    {
        var rows = options.Select(option => new NetherStrategyVisibleContentRow(
            NetherStrategyVisibleContentKind.Event,
            snapshot.CurrentNodeId,
            option.EventId,
            option.EventPartId
        )
        {
            EventId = option.EventId,
            EventPartId = option.EventPartId,
            IsKnown = string.IsNullOrWhiteSpace(option.UnknownReason),
            UnknownReason = option.UnknownReason,
            EventOptions =
            [
                new NetherStrategyVisibleEventOptionEvidence(
                    option.OptionNumber,
                    option.EventPartId,
                    option.Effects.Select(effect => new NetherStrategyVisibleEventEffectEvidence(
                        NetherStrategyVisibleEventEffectSource.Content,
                        (int)effect.Kind,
                        effect.ContentId
                    )
                    {
                        EffectKind = effect.Kind,
                        ContentId = effect.ContentId,
                        Amount = effect.Amount,
                        IsKnown = string.IsNullOrWhiteSpace(option.UnknownReason),
                        IsPresent = true,
                    }).ToArray()
                ),
            ],
        }).ToArray();
        return new NetherStrategyEvidencePackage
        {
            Identity = new NetherStrategyEvidenceIdentity(1, 1, 1, snapshot.Fingerprint),
            Server = new NetherStrategyServerEvidence
            {
                CurrentFloorId = snapshot.CurrentFloorId,
                CurrentNodeId = snapshot.CurrentNodeId,
                NetherGold = snapshot.NetherGold,
                TreasureKeyCount = snapshot.TreasureKeyCount,
                ErosionPoint = snapshot.ErosionPoint,
            },
            Research = NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence>.Known(
                new NetherStrategyResearchEvidence(research)
            ),
            VisibleMap = NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Known(
                new NetherStrategyVisibleMapEvidence([], rows)
            ),
        };
    }

    private static NetherStrategyResearchFamilyState Research(
        NetherCodeFamily family,
        bool projectedKnown
    ) => new(family, 0, 0, 1)
    {
        IsProjectedNormalSettlementKnown = projectedKnown,
    };

    private static NetherEventRewardEvidence Reward(long id) =>
        new(id, id, 91, NetherRewardRarity.Gold, 1);

    private static NetherSnapshot Snapshot() => new()
    {
        Status = NetherSessionStatus.Play,
        CurrentFloorId = 10,
        CurrentNodeId = 1,
        ErosionPoint = 20,
        NetherGold = 100,
        Characters = [new NetherCharacterState(101, 1000)],
    };

    private static NetherStrategyEvidencePackage MixedPackage(
        NetherSnapshot snapshot,
        NetherEventOption item,
        NetherEventOption battle,
        NetherEventOption heal
    )
    {
        NetherFloorNode floor = new(
            snapshot.CurrentFloorId,
            FloorLevel: 1,
            FloorIndex: 0,
            NetherFloorNodeType.Event
        )
        {
            NodeId = snapshot.CurrentNodeId,
            IsUnlocked = true,
        };
        NetherStrategyVisibleEvidenceCaptureResult mapped = NetherStrategyVisibleEvidenceMapper.Map(
            new NetherStrategyVisibleEvidenceCaptureRequest(
                [floor],
                [
                new NetherStrategyBattleMasterRow(8201, snapshot.CurrentFloorId, 1, 8202, 100),
                    new NetherStrategyBattleMasterRow(8201, snapshot.CurrentFloorId, 1, 8202, 100)
                    {
                        HasRequiredFields = false,
                    },
                ],
                [],
                [new NetherFloorEventMasterRow(
                    item.EventId,
                    snapshot.CurrentFloorId,
                    1,
                    item.EventPartId,
                    battle.EventPartId,
                    heal.EventPartId,
                    0
                )],
                [
                    new NetherFloorEventPartMasterRow(
                        item.EventPartId,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        30,
                        item.Effects[0].ContentId,
                        item.Effects[0].Amount
                    ),
                    new NetherFloorEventPartMasterRow(
                        battle.EventPartId,
                        (int)NetherEffectKind.Battle,
                        battle.Effects[0].Amount,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0
                    ),
                    new NetherFloorEventPartMasterRow(
                        heal.EventPartId,
                        (int)NetherEffectKind.Heal,
                        heal.Effects[0].Amount,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0
                    ),
                ],
                [
                    new NetherStrategyItemMasterRow(8101, 91, 3, 1, 99)
                    {
                        HasRequiredFields = false,
                    },
                    new NetherStrategyItemMasterRow(8101, 91, 3, 1, 99),
                ]
            )
            {
                ExtendIdByNodeId = new Dictionary<long, long> { [snapshot.CurrentNodeId] = item.EventId },
            }
        );
        if (!mapped.IsSuccess || mapped.Evidence == null)
            throw new System.InvalidOperationException(mapped.Detail);
        return new NetherStrategyEvidencePackage
        {
            Identity = new NetherStrategyEvidenceIdentity(1, 1, 1, snapshot.Fingerprint),
            Server = new NetherStrategyServerEvidence
            {
                CurrentFloorId = snapshot.CurrentFloorId,
                CurrentNodeId = snapshot.CurrentNodeId,
                ErosionPoint = snapshot.ErosionPoint,
                NetherGold = snapshot.NetherGold,
                TreasureKeyCount = snapshot.TreasureKeyCount,
            },
            Research = NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence>.Unknown("not-needed"),
            VisibleMap = NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Known(
                mapped.Evidence
            ),
        };
    }

    private sealed class RuntimeFloorFixture
    {
        public long MNetherMapFloorId { get; init; }
        public long ExtendId { get; init; }
        public int FloorType { get; init; }
    }

    private sealed class RuntimeMapFloorFixture
    {
        public long id;
        public int min_erosion_point;
        public int max_erosion_point;
    }

    private sealed class RuntimeEventFixture
    {
        public long id;
        public long m_nether_map_floor_id;
        public int weight;
        public int type;
        public long m_nether_floor_event_part_id_1;
        public long m_nether_floor_event_part_id_2;
        public long m_nether_floor_event_part_id_3;
        public long m_nether_floor_event_part_id_4 = 0;
    }

    private sealed class RuntimePartFixture
    {
        public long id;
        public int target_type_1;
        public long select_parameter_1;
        public int target_type_2 = 0;
        public long select_parameter_2 = 0;
        public int target_type_3 = 0;
        public long select_parameter_3 = 0;
        public int content_type;
        public long content_id;
        public int amount;
    }

    private sealed class RuntimeItemFixture
    {
        public long id;
        public long type;
        public int rarity;
        public int value;
        public int possession_limit;
    }

    private sealed class RuntimeMalformedItemFixture
    {
        public long id;
    }

    private sealed class RuntimeBattleFixture
    {
        public long id;
        public long m_nether_map_floor_id;
        public int type;
        public long m_nether_battle_stage_id;
        public int code_drop_ratio;
    }

    private sealed class RuntimeMalformedBattleFixture
    {
        public long id;
    }
}
