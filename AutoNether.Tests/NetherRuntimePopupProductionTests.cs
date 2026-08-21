#nullable enable

using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.Linq;
using System.Threading;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

[CollectionDefinition("nether-managed-popup-runtime", DisableParallelization = true)]
public sealed class NetherManagedPopupRuntimeCollection
{
}

[Collection("nether-managed-popup-runtime")]
public sealed class NetherRuntimePopupProductionTests
{
    [Fact]
    public void Managed_popup_registration_binds_typed_battle_only_for_exact_identity()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        try
        {
            PopupCapture exact = CaptureManagedEventPopup(bridge, typedBattleId: 8201);
            Assert.True(exact.Interactive.IsSuccess, exact.Interactive.Detail);
            Assert.True(exact.Popup.IsSuccess, exact.Popup.Detail);

            NetherRuntimePopupContext exactContext = Assert.IsType<NetherRuntimePopupContext>(exact.Popup.Popup);
            NetherEventOption exactOption = Assert.Single(exactContext.Options);
            NetherEventBattleEvidence exactEvidence = Assert.IsType<NetherEventBattleEvidence>(
                exactOption.BattleEvidence
            );
            Assert.True(exactEvidence.IsKnown, exactEvidence.UnknownReason);
            Assert.Equal(8201, exactEvidence.BattleId);
            Assert.Equal(NetherEventBattleTier.Boss, exactEvidence.SemanticTier);

            NetherEventDecision exactDecision = new NetherEventPolicy().DecideEvent(
                exact.Snapshot,
                exactContext.Options,
                new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment },
                [],
                exactContext.EventStrategyEvidence
            );
            Assert.Equal(NetherEventDecisionKind.Pause, exactDecision.Kind);
            Assert.DoesNotContain("semantic-tier-unavailable", exactDecision.Detail);

            PopupCapture mismatch = CaptureManagedEventPopup(bridge, typedBattleId: 9999);
            Assert.True(mismatch.Interactive.IsSuccess, mismatch.Interactive.Detail);
            Assert.True(mismatch.Popup.IsSuccess, mismatch.Popup.Detail);

            NetherRuntimePopupContext mismatchContext = Assert.IsType<NetherRuntimePopupContext>(
                mismatch.Popup.Popup
            );
            NetherEventOption mismatchOption = Assert.Single(mismatchContext.Options);
            NetherEventBattleEvidence mismatchEvidence = Assert.IsType<NetherEventBattleEvidence>(
                mismatchOption.BattleEvidence
            );
            Assert.False(mismatchEvidence.IsKnown);
            Assert.Contains("semantic-tier-unavailable", mismatchEvidence.UnknownReason);

            NetherEventDecision mismatchDecision = new NetherEventPolicy().DecideEvent(
                mismatch.Snapshot,
                mismatchContext.Options,
                new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment },
                [],
                mismatchContext.EventStrategyEvidence
            );
            Assert.Equal(NetherEventDecisionKind.Pause, mismatchDecision.Kind);
        }
        finally
        {
            bridge.RegisterNativeEventPopupCaptureFactory(null);
            bridge.ClearRegistrations();
            bridge.RegisterTypedSemanticProviderFactory(null);
        }
    }

    [Fact]
    public void Managed_popup_registration_without_typed_provider_keeps_raw_battle_unknown()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        try
        {
            bridge.ClearRegistrations();
            bridge.RegisterTypedSemanticProviderFactory(null);
            NetherSnapshot snapshot = CreateSnapshot();
            bridge.BeginRouteReplan(snapshot.Fingerprint);
            bridge.RegisterNativeEventPopupCaptureFactory((_, _) => Capture(snapshot));
            NetherRuntimeBridge.RegisterFloorSelection(new Project.Nether.FloorSelection.SubViewController());

            NetherRuntimeInteractivePreEntryInputsResult interactive = CaptureManagedPreEntry(
                bridge,
                snapshot,
                provider: null
            );
            Assert.True(interactive.IsSuccess, interactive.Detail);

            RegisterPopup();
            NetherRuntimePopupResult popup = bridge.TryGetActivePopup();
            Assert.True(popup.IsSuccess, popup.Detail);
            NetherEventOption option = Assert.Single(
                Assert.IsType<NetherRuntimePopupContext>(popup.Popup).Options
            );
            Assert.False(Assert.IsType<NetherEventBattleEvidence>(option.BattleEvidence).IsKnown);
        }
        finally
        {
            bridge.RegisterNativeEventPopupCaptureFactory(null);
            bridge.ClearRegistrations();
            bridge.RegisterTypedSemanticProviderFactory(null);
        }
    }

    [Fact]
    public void Managed_popup_raw_item_fields_do_not_promote_reward_without_typed_provider()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        try
        {
            PopupCapture rawGold = CaptureManagedRewardPopup(
                bridge,
                rawRarity: (int)NetherRewardRarity.Gold,
                provider: null
            );
            Assert.True(rawGold.Interactive.IsSuccess, rawGold.Interactive.Detail);
            Assert.True(rawGold.Popup.IsSuccess, rawGold.Popup.Detail);
            NetherEventOption rawGoldOption = Assert.Single(
                Assert.IsType<NetherRuntimePopupContext>(rawGold.Popup.Popup).Options
            );
            Assert.False(rawGoldOption.Effects[0].Known);
            Assert.False(rawGoldOption.RewardEvidence?.IsKnown == true);

            PopupCapture rawRed = CaptureManagedRewardPopup(
                bridge,
                rawRarity: (int)NetherRewardRarity.Red,
                provider: null
            );
            Assert.True(rawRed.Popup.IsSuccess, rawRed.Popup.Detail);
            NetherEventOption rawRedOption = Assert.Single(
                Assert.IsType<NetherRuntimePopupContext>(rawRed.Popup.Popup).Options
            );
            Assert.False(rawRedOption.Effects[0].Known);
            Assert.False(rawRedOption.RewardEvidence?.IsKnown == true);

            PopupCapture typed = CaptureManagedRewardPopup(
                bridge,
                rawRarity: (int)NetherRewardRarity.Red,
                provider: new NetherStrategyTypedSemanticProviderEvidence
                {
                    CanonicalRewardTiers =
                    [new NetherCanonicalRewardTierProviderEvidence(8301, NetherCanonicalRewardTier.RedRankFive, 91)],
                }
            );
            Assert.True(typed.Popup.IsSuccess, typed.Popup.Detail);
            NetherEventOption typedOption = Assert.Single(
                Assert.IsType<NetherRuntimePopupContext>(typed.Popup.Popup).Options
            );
            NetherEventRewardEvidence typedReward = Assert.IsType<NetherEventRewardEvidence>(
                typedOption.RewardEvidence
            );
            Assert.True(typedReward.IsKnown, typedReward.UnknownReason);
            Assert.Equal(91, typedReward.ItemType);
            Assert.Equal(NetherRewardRarity.Red, typedReward.Rarity);
        }
        finally
        {
            bridge.RegisterNativeEventPopupCaptureFactory(null);
            bridge.ClearRegistrations();
            bridge.RegisterTypedSemanticProviderFactory(null);
        }
    }

    [Fact]
    public void Native_popup_fallback_keeps_raw_item_fields_out_of_reward_semantics()
    {
        string root = FindRepositoryRoot();
        string bridgeSource = File.ReadAllText(
            Path.Combine(root, "AutoNether", "Services", "NetherRuntimeBridge.cs")
        );
        int mapperStart = bridgeSource.IndexOf(
            "private static bool TryMapEventPart(",
            StringComparison.Ordinal
        );
        int mapperEnd = bridgeSource.IndexOf(
            "private static bool TryMapTargetEffect(",
            mapperStart,
            StringComparison.Ordinal
        );
        Assert.True(mapperStart >= 0 && mapperEnd > mapperStart);
        string nativePartMapper = bridgeSource[mapperStart..mapperEnd];
        Assert.Contains("TryMapTypedEventReward", nativePartMapper);
        Assert.DoesNotContain("TryMapRewardRarity(item.rarity", nativePartMapper);
    }

    [Fact]
    public void Managed_shop_idless_typed_key_survives_final_evidence_package_mapping()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        try
        {
            ManagedShopRouteCapture captured = CaptureManagedShopRoute(
                bridge,
                gold: 500,
                treasureKeys: 0,
                typedProvider: true,
                includeUnknownSibling: false,
                bindCommitment: true
            );
            NetherSnapshot packagedSnapshot = captured.Snapshot with
            {
                NetherId = 1,
                MapId = 1,
                MasterMaxFloorLevel = 100,
                AuthoritativeBossFloorLevels = [96],
            };
            NetherStrategyEvidenceMapResult packaged = NetherStrategyEvidenceMapper.Map(
                new NetherStrategyEvidenceMapRequest(
                    new NetherStrategyEvidenceIdentity(1, 1, 1, packagedSnapshot.Fingerprint),
                    packagedSnapshot
                )
                {
                    VisibleMap = captured.Visible,
                }
            );

            Assert.True(packaged.IsMapped, packaged.Detail);
            NetherStrategyVisibleMapEvidence packagedVisible =
                Assert.IsType<NetherStrategyVisibleMapEvidence>(packaged.Package!.VisibleMap.Value);
            NetherStrategyVisibleContentRow key = Assert.Single(
                packagedVisible.ContentRows,
                row => row.ContentId == 1001
            );
            Assert.Equal(0, key.MasterRowId);
            Assert.True(key.IsKnown);
            Assert.True(key.IsTreasureKey);
            Assert.True(key.ShopKeyIdentity > 0);
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    [Fact]
    public void Managed_shop_capture_rejects_conflicting_typed_provider_before_route_mapping()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        try
        {
            bridge.ClearRegistrations();
            NetherSnapshot snapshot = CreateShopSnapshot(300, treasureKeys: 0);
            NetherStrategyTypedSemanticProviderEvidence registeredProvider = CreateShopProvider(7001);
            NetherStrategyTypedSemanticProviderEvidence conflictingProvider = CreateShopProvider(7002);

            NetherRuntimeBridge.RegisterFloorSelection(new object());
            bridge.BeginRouteReplan(snapshot.Fingerprint);
            bridge.RegisterTypedSemanticProviderFactory(current =>
                new NetherRuntimeTypedSemanticProviderScope(current.Fingerprint, registeredProvider)
            );
            bridge.RegisterManagedShopPopupCaptureFactory(_ => CreateManagedShopCapture(snapshot));

            NetherRuntimeInteractivePreEntryInputsResult interactive =
                CaptureManagedShopPreEntry(bridge, snapshot);
            Assert.True(interactive.IsSuccess, interactive.Detail);
            Assert.Same(registeredProvider, interactive.TypedSemanticProvider);

            RegisterManagedShopPopup();
            NetherRuntimePopupResult popup = bridge.TryGetActivePopup();
            Assert.True(popup.IsSuccess, popup.Detail);
            NetherRuntimePopupContext popupContext = Assert.IsType<NetherRuntimePopupContext>(popup.Popup);
            Assert.Equal(NetherRuntimePopupKind.Shop, popupContext.Kind);

            NetherStrategyVisibleEvidenceCaptureResult mapped =
                NetherStrategyVisibleEvidenceAssembler.Assemble(
                    new NetherStrategyVisibleEvidenceAssemblyRequest(
                        snapshot,
                        interactive,
                        popup,
                        new NetherStrategyVisibleEvidenceCaptureRequest(
                            snapshot.Floors,
                            [],
                            [],
                            [],
                            [],
                            []
                        )
                        {
                            TypedSemanticProvider = conflictingProvider,
                        }
                    )
                );

            Assert.False(mapped.IsSuccess);
            Assert.Equal("ambiguous-runtime-semantic-provider-evidence", mapped.Detail);
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    [Theory]
    [InlineData(300, false)]
    [InlineData(499, false)]
    [InlineData(500, true)]
    public void Managed_shop_dto_capture_provider_assembles_maps_routes_and_reconciles_key_then_bag(
        int gold,
        bool buysBagAfterKey
    )
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        try
        {
            ManagedShopRouteCapture captured = CaptureManagedShopRoute(
                bridge,
                gold,
                treasureKeys: 0,
                typedProvider: true,
                includeUnknownSibling: false,
                bindCommitment: true
            );
            Assert.True(captured.Interactive.IsSuccess, captured.Interactive.Detail);
            Assert.True(captured.Popup.IsSuccess, captured.Popup.Detail);
            Assert.NotNull(captured.Visible);

            NetherRuntimePopupContext popup = Assert.IsType<NetherRuntimePopupContext>(captured.Popup.Popup);
            NetherStrategyVisibleContentRow[] shopRows = captured.Visible.ContentRows
                .Where(row => row.Kind == NetherStrategyVisibleContentKind.ShopInventory)
                .ToArray();
            Assert.Equal(2, shopRows.Length);

            NetherStrategyVisibleContentRow keyRow = Assert.Single(
                shopRows,
                row => row.ContentId == 1001
            );
            Assert.True(keyRow.IsKnown);
            Assert.True(keyRow.IsTreasureKey);
            Assert.Equal(7001, keyRow.ShopKeyIdentity);
            Assert.Equal(0, keyRow.MasterRowId);

            NetherStrategyVisibleContentRow bagRow = Assert.Single(
                shopRows,
                row => row.ContentId == 1002
            );
            Assert.True(bagRow.IsKnown);
            Assert.Equal(NetherCanonicalRewardTier.GoldRankFive, bagRow.CanonicalRewardTier);
            Assert.Equal(91, bagRow.ItemType);
            Assert.Equal(300, bagRow.Cost);

            NetherRoutePlan route = PlanManagedShopVsNormal(captured.RouteSnapshot, captured.Visible);
            Assert.Equal(
                captured.RouteSnapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Shop).NodeId,
                Assert.IsType<NetherFloorNode>(route.SelectedNode).NodeId
            );

            NetherShopProcurementCommitment commitment = popup.ShopProcurementCommitment!;
            Assert.True(commitment.IsValid);
            Assert.Equal(1001, commitment.KeyContentId);
            Assert.Equal(1002, commitment.BagContentId);

            NetherAutoClimbSettings settings = new()
            {
                StrategyMode = NetherStrategyMode.Equipment,
                ShopMode = NetherShopMode.EquipmentBags,
            };
            NetherShopDecision keyDecision = new NetherEventPolicy().DecideShop(
                captured.Snapshot,
                popup.ShopContents,
                settings,
                commitment
            );
            Assert.Equal(NetherShopDecisionKind.Buy, keyDecision.Kind);
            Assert.Equal(1001, keyDecision.ContentId);
            Assert.Equal(200, keyDecision.GoldCost);
            Assert.Equal(
                NetherActionOutcome.Applied,
                NetherActionReconcilePolicy.Evaluate(
                    ShopAction(keyDecision),
                    captured.Snapshot,
                    captured.Snapshot with
                    {
                        NetherGold = gold - 200,
                        TreasureKeyCount = 1,
                        AcquiredItems = [new NetherRewardItem(1001, 1)],
                    }
                )
            );

            NetherSnapshot afterKey = captured.Snapshot with
            {
                NetherGold = gold - 200,
                TreasureKeyCount = 1,
                AcquiredItems = [new NetherRewardItem(1001, 1)],
            };
            NetherShopDecision nextDecision = new NetherEventPolicy().DecideShop(
                afterKey,
                popup.ShopContents,
                settings,
                commitment
            );
            Assert.Equal(
                buysBagAfterKey ? NetherShopDecisionKind.Buy : NetherShopDecisionKind.Leave,
                nextDecision.Kind
            );
            if (buysBagAfterKey)
            {
                Assert.Equal(1002, nextDecision.ContentId);
                Assert.Equal(300, nextDecision.GoldCost);
                Assert.Equal(
                    NetherActionOutcome.Applied,
                    NetherActionReconcilePolicy.Evaluate(
                        ShopAction(nextDecision),
                        afterKey,
                        afterKey with
                        {
                            NetherGold = 0,
                            AcquiredItems =
                            [
                                new NetherRewardItem(1001, 1),
                                new NetherRewardItem(1002, 1),
                            ],
                        }
                    )
                );
            }
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    [Fact]
    public void Managed_shop_dto_raw_only_capture_keeps_key_bag_and_route_fail_closed()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        try
        {
            ManagedShopRouteCapture captured = CaptureManagedShopRoute(
                bridge,
                gold: 500,
                treasureKeys: 0,
                typedProvider: false,
                includeUnknownSibling: false,
                bindCommitment: false
            );
            NetherRuntimePopupContext popup = Assert.IsType<NetherRuntimePopupContext>(captured.Popup.Popup);
            NetherShopContent rawBag = Assert.Single(
                popup.ShopContents,
                content => content.ContentId == 1002
            );
            NetherShopContent rawKey = Assert.Single(
                popup.ShopContents,
                content => content.ContentId == 1001
            );
            Assert.Equal(NetherCanonicalRewardTier.Unknown, rawBag.CanonicalRewardTier);
            Assert.False(rawKey.IsTreasureKey);
            Assert.Equal(
                NetherShopDecisionKind.Leave,
                new NetherEventPolicy().DecideShop(
                    captured.Snapshot,
                    popup.ShopContents,
                    new NetherAutoClimbSettings
                    {
                        StrategyMode = NetherStrategyMode.Equipment,
                        ShopMode = NetherShopMode.EquipmentBags,
                    }
                ).Kind
            );

            NetherRoutePlan route = PlanManagedShopVsNormal(captured.RouteSnapshot, captured.Visible);
            Assert.Equal(
                captured.RouteSnapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Battle).NodeId,
                Assert.IsType<NetherFloorNode>(route.SelectedNode).NodeId
            );
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    [Fact]
    public void Managed_shop_dto_unknown_sibling_invalidates_typed_late_shop_route_but_leaves_popup()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        try
        {
            ManagedShopRouteCapture captured = CaptureManagedShopRoute(
                bridge,
                gold: 500,
                treasureKeys: 0,
                typedProvider: true,
                includeUnknownSibling: true,
                bindCommitment: false
            );
            NetherRuntimePopupContext popup = Assert.IsType<NetherRuntimePopupContext>(captured.Popup.Popup);
            NetherStrategyVisibleContentRow unknown = Assert.Single(
                captured.Visible.ContentRows,
                row => row.ContentId == 1003
            );
            Assert.False(unknown.IsKnown);
            Assert.Equal(
                NetherShopDecisionKind.Leave,
                new NetherEventPolicy().DecideShop(
                    captured.Snapshot,
                    popup.ShopContents,
                    new NetherAutoClimbSettings
                    {
                        StrategyMode = NetherStrategyMode.Equipment,
                        ShopMode = NetherShopMode.EquipmentBags,
                    }
                ).Kind
            );

            NetherRoutePlan route = PlanManagedShopVsNormal(captured.RouteSnapshot, captured.Visible);
            Assert.Equal(
                captured.RouteSnapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Battle).NodeId,
                Assert.IsType<NetherFloorNode>(route.SelectedNode).NodeId
            );
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    [Fact]
    public void Managed_event_popup_requires_exact_native_four_part_shape()
    {
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        try
        {
            foreach (long[] declaredPartIds in new[]
            {
                new long[] { 712 },
                new long[] { 712, 712, 0, 0 },
                new long[] { 712, 0, 713, 0 },
                new long[] { 712, 0, 0, 0, 0 },
            })
            {
                NetherRuntimePopupResult popup = CaptureManagedEventPopupWithShape(
                    bridge,
                    declaredPartIds
                );
                Assert.False(popup.IsSuccess, string.Join(",", declaredPartIds));
                Assert.Contains("event-part-shape", popup.Detail);
            }
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    private static PopupCapture CaptureManagedEventPopup(
        NetherRuntimeBridge bridge,
        long typedBattleId
    )
    {
        bridge.ClearRegistrations();
        NetherSnapshot snapshot = CreateSnapshot();
        bridge.BeginRouteReplan(snapshot.Fingerprint);
        NetherStrategyTypedSemanticProviderEvidence provider = new()
        {
            EventBattleTiers =
            [new NetherEventBattleTierProviderEvidence(typedBattleId, NetherEventBattleTier.Boss)],
        };
        bridge.RegisterTypedSemanticProviderFactory(current =>
            new NetherRuntimeTypedSemanticProviderScope(current.Fingerprint, provider)
        );
        bridge.RegisterNativeEventPopupCaptureFactory((_, kind) =>
            kind == NetherRuntimePopupKind.Event ? Capture(snapshot) : null
        );
        NetherRuntimeBridge.RegisterFloorSelection(new Project.Nether.FloorSelection.SubViewController());

        NetherRuntimeInteractivePreEntryInputsResult interactive = CaptureManagedPreEntry(
            bridge,
            snapshot,
            provider
        );
        if (!interactive.IsSuccess)
            return new PopupCapture(snapshot, interactive, NetherRuntimePopupResult.Failure(interactive.Detail));

        RegisterPopup();
        return new PopupCapture(snapshot, interactive, bridge.TryGetActivePopup());
    }

    private static NetherRuntimePopupResult CaptureManagedEventPopupWithShape(
        NetherRuntimeBridge bridge,
        IReadOnlyList<long> declaredPartIds
    )
    {
        bridge.ClearRegistrations();
        NetherSnapshot snapshot = CreateSnapshot();
        bridge.BeginRouteReplan(snapshot.Fingerprint);
        bridge.RegisterTypedSemanticProviderFactory(null);
        bridge.RegisterNativeEventPopupCaptureFactory((_, kind) =>
            kind == NetherRuntimePopupKind.Event
                ? Capture(snapshot) with { DeclaredPartIds = declaredPartIds }
                : null
        );
        NetherRuntimeBridge.RegisterFloorSelection(new Project.Nether.FloorSelection.SubViewController());
        NetherRuntimeInteractivePreEntryInputsResult interactive = CaptureManagedPreEntry(
            bridge,
            snapshot,
            provider: null
        );
        Assert.True(interactive.IsSuccess, interactive.Detail);
        RegisterPopup();
        return bridge.TryGetActivePopup();
    }

    private static PopupCapture CaptureManagedRewardPopup(
        NetherRuntimeBridge bridge,
        int rawRarity,
        NetherStrategyTypedSemanticProviderEvidence? provider
    )
    {
        bridge.ClearRegistrations();
        NetherSnapshot snapshot = CreateSnapshot();
        bridge.BeginRouteReplan(snapshot.Fingerprint);
        bridge.RegisterTypedSemanticProviderFactory(provider == null
            ? null
            : current => new NetherRuntimeTypedSemanticProviderScope(current.Fingerprint, provider));
        bridge.RegisterNativeEventPopupCaptureFactory((_, kind) =>
            kind == NetherRuntimePopupKind.Event ? CaptureReward(snapshot, rawRarity) : null
        );
        NetherRuntimeBridge.RegisterFloorSelection(new Project.Nether.FloorSelection.SubViewController());

        NetherRuntimeInteractivePreEntryInputsResult interactive = CaptureManagedPreEntry(
            bridge,
            snapshot,
            provider
        );
        if (!interactive.IsSuccess)
            return new PopupCapture(snapshot, interactive, NetherRuntimePopupResult.Failure(interactive.Detail));

        RegisterPopup();
        return new PopupCapture(snapshot, interactive, bridge.TryGetActivePopup());
    }

    private static NetherRuntimeInteractivePreEntryInputsResult CaptureManagedShopPreEntry(
        NetherRuntimeBridge bridge,
        NetherSnapshot snapshot
    ) => bridge.CaptureManagedInteractivePreEntryFloor(
        snapshot,
        new NetherAutoClimbSettings
        {
            StrategyMode = NetherStrategyMode.Equipment,
            ShopMode = NetherShopMode.EquipmentBags,
        },
        new ManagedShopFloorDto
        {
            MNetherMapFloorId = snapshot.CurrentFloorId,
            ExtendId = 711,
            FloorType = (int)NetherFloorNodeType.Shop,
        },
        mapFloorRows: null,
        eventRows: null,
        eventPartRows: null,
        itemRows: null,
        battleRows: new object[]
        {
            new Project.Nether.FloorSelection.ManagedBattleRow
            {
                id = 9601,
                m_nether_map_floor_id = 960,
                type = 1,
                m_nether_battle_stage_id = 9602,
                code_drop_ratio = 0,
            },
            new Project.Nether.FloorSelection.ManagedBattleRow
            {
                id = 9701,
                m_nether_map_floor_id = 970,
                type = 2,
                m_nether_battle_stage_id = 9702,
                code_drop_ratio = 0,
            },
        },
        floorNodeId: snapshot.CurrentNodeId,
        canCloseShop: false
    );

    private static ManagedShopRouteCapture CaptureManagedShopRoute(
        NetherRuntimeBridge bridge,
        int gold,
        int treasureKeys,
        bool typedProvider,
        bool includeUnknownSibling,
        bool bindCommitment
    )
    {
        bridge.ClearRegistrations();
        NetherSnapshot snapshot = CreateManagedShopRouteCaptureSnapshot(gold, treasureKeys);
        NetherStrategyTypedSemanticProviderEvidence? provider = typedProvider
            ? CreateShopProvider(7001)
            : null;

        NetherRuntimeBridge.RegisterFloorSelection(new object());
        bridge.BeginRouteReplan(snapshot.Fingerprint);
        bridge.RegisterTypedSemanticProviderFactory(provider == null
            ? null
            : current => new NetherRuntimeTypedSemanticProviderScope(current.Fingerprint, provider));
        bridge.RegisterManagedShopPopupCaptureFactory(_ =>
            CreateManagedShopCapture(snapshot, includeUnknownSibling));

        NetherRuntimeInteractivePreEntryInputsResult interactive =
            CaptureManagedShopPreEntry(bridge, snapshot);
        Assert.True(interactive.IsSuccess, interactive.Detail);
        if (bindCommitment)
            bridge.BindRankFiveKeyProcurement(CreateShopProcurementDecision(snapshot));

        RegisterManagedShopPopup();
        NetherRuntimePopupResult popup = bridge.TryGetActivePopup();
        Assert.True(popup.IsSuccess, popup.Detail);

        NetherStrategyVisibleEvidenceCaptureResult mapped =
            NetherStrategyVisibleEvidenceAssembler.Assemble(
                new NetherStrategyVisibleEvidenceAssemblyRequest(
                    snapshot,
                    interactive,
                    popup,
                    new NetherStrategyVisibleEvidenceCaptureRequest(
                        snapshot.Floors,
                        [],
                        [],
                        [],
                        [],
                        []
                    )
                    {
                        TypedSemanticProvider = provider,
                    }
                )
            );
        Assert.True(mapped.IsSuccess, mapped.Detail);
        return new ManagedShopRouteCapture(
            snapshot,
            snapshot with
            {
                CurrentFloorId = snapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Recovery).FloorId,
                CurrentNodeId = snapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Recovery).NodeId,
                FloorLevel = snapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Recovery).FloorLevel,
                FloorIndex = snapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Recovery).ApiFloorIndex,
            },
            interactive,
            popup,
            mapped.Evidence!
        );
    }

    private static NetherRankFiveKeyProcurementDecision CreateShopProcurementDecision(
        NetherSnapshot snapshot
    )
    {
        NetherRankFiveTreasureIdentity objective = new(9001, 9002, 9003);
        return new NetherRankFiveKeyProcurementDecision
        {
            IsKnown = true,
            HasMandatoryObjective = true,
            Objective = objective,
            SourceKind = NetherKeyProcurementSourceKind.ShopGold200,
            GoldCost = 200,
            Commitment = new NetherRankFiveKeyProcurementCommitment
            {
                Objective = objective,
                SourceKind = NetherKeyProcurementSourceKind.ShopGold200,
                SourceNodeId = snapshot.CurrentNodeId,
                SourceContentId = 1001,
                GoldCost = 200,
            },
        };
    }

    private static NetherSnapshot CreateManagedShopRouteCaptureSnapshot(
        int gold,
        int treasureKeys
    )
    {
        NetherFloorNode recovery = new(940, 90, 0, NetherFloorNodeType.Recovery)
        {
            NodeId = ((long)11 << 32) | 1,
            IsUnlocked = true,
        };
        NetherFloorNode shop = new(950, 95, 1, NetherFloorNodeType.Shop)
        {
            NodeId = ((long)11 << 32) | 2,
            PreviousFloorIds = [recovery.NodeId],
            IsUnlocked = true,
        };
        NetherFloorNode normal = new(960, 95, 2, NetherFloorNodeType.Battle)
        {
            NodeId = ((long)11 << 32) | 3,
            PreviousFloorIds = [recovery.NodeId],
            IsUnlocked = true,
        };
        NetherFloorNode boss = new(970, 96, 3, NetherFloorNodeType.Boss)
        {
            NodeId = ((long)11 << 32) | 4,
            PreviousFloorIds = [shop.NodeId, normal.NodeId],
            IsUnlocked = true,
        };
        return new NetherSnapshot
        {
            Status = NetherSessionStatus.Play,
            CurrentFloorId = shop.FloorId,
            CurrentNodeId = shop.NodeId,
            FloorLevel = shop.FloorLevel,
            FloorIndex = shop.ApiFloorIndex,
            NetherGold = gold,
            TreasureKeyCount = treasureKeys,
            ErosionPoint = 20,
            Characters = [new NetherCharacterState(101, 1000)],
            Floors = [recovery, shop, normal, boss],
        };
    }

    private static NetherRoutePlan PlanManagedShopVsNormal(
        NetherSnapshot routeSnapshot,
        NetherStrategyVisibleMapEvidence visible
    )
    {
        NetherFloorNode shop = routeSnapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Shop);
        NetherFloorNode normal = routeSnapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Battle);
        NetherFloorNode boss = routeSnapshot.Floors.Single(floor => floor.NodeType == NetherFloorNodeType.Boss);
        NetherRouteHorizonSafetyEvaluation ShopHorizon(long nodeId) => new()
        {
            IsEligible = true,
            PeakErosion = 20,
            MinimumActiveCharacterHpPermille = 900,
            FinalErosion = 20,
            HorizonSteps =
            [
                new NetherRouteHorizonStep(nodeId, NetherFloorNodeType.Shop, 0, 0, [])
                {
                    MinimumCombatEntryHpPermille = 0,
                },
                new NetherRouteHorizonStep(boss.NodeId, boss.NodeType, 0, 0, [])
                {
                    IsTerminalBoss = true,
                    MinimumCombatEntryHpPermille = 0,
                },
            ],
            Steps =
            [
                new NetherRouteHorizonStepAudit(shop.NodeId, 20, 20, 900),
                new NetherRouteHorizonStepAudit(boss.NodeId, 20, 20, 900),
            ],
        };
        NetherRouteHorizonSafetyEvaluation NormalHorizon = new()
        {
            IsEligible = true,
            PeakErosion = 20,
            MinimumActiveCharacterHpPermille = 900,
            FinalErosion = 20,
            HorizonSteps =
            [
                new NetherRouteHorizonStep(normal.NodeId, normal.NodeType, 0, 0, [])
                {
                    MinimumCombatEntryHpPermille = 0,
                },
                new NetherRouteHorizonStep(boss.NodeId, boss.NodeType, 0, 0, [])
                {
                    IsTerminalBoss = true,
                    MinimumCombatEntryHpPermille = 0,
                },
            ],
            Steps =
            [
                new NetherRouteHorizonStepAudit(normal.NodeId, 20, 20, 900),
                new NetherRouteHorizonStepAudit(boss.NodeId, 20, 20, 900),
            ],
        };
        return new NetherRoutePlanner().Plan(
            routeSnapshot,
            new NetherRouteSafetyContext
            {
                StrategyMode = NetherStrategyMode.Equipment,
                ResearchIncomplete = false,
                VisibleMap = visible,
                HorizonEvaluationByFloorId = new Dictionary<long, NetherRouteHorizonSafetyEvaluation>
                {
                    [shop.NodeId] = ShopHorizon(shop.NodeId),
                    [normal.NodeId] = NormalHorizon,
                },
            }
        );
    }

    private static NetherPlannedAction ShopAction(NetherShopDecision decision) =>
        new(NetherActionKind.BuyShopItem)
        {
            ContentId = decision.ContentId,
            ContentAmount = decision.Amount,
            GoldCost = decision.GoldCost,
            ShopProcurementCommitment = decision.ProcurementCommitment,
        };

    private static NetherStrategyTypedSemanticProviderEvidence CreateShopProvider(long keyIdentity) =>
        new()
        {
            CanonicalRewardTiers =
            [new NetherCanonicalRewardTierProviderEvidence(9301, NetherCanonicalRewardTier.GoldRankFive, 91)],
            ShopKeyIdentities =
            [new NetherShopKeyProviderEvidence(1001, 166, 0, 1, keyIdentity)],
            EventBattleTiers =
            [
                new NetherEventBattleTierProviderEvidence(9601, NetherEventBattleTier.NormalBattle),
                new NetherEventBattleTierProviderEvidence(9701, NetherEventBattleTier.Boss),
            ],
        };

    private static NetherRuntimeManagedShopPopupCapture CreateManagedShopCapture(
        NetherSnapshot snapshot,
        bool includeUnknownSibling = false
    )
    {
        var contents = new List<NetherRawShopContent>
        {
            new(1001, 166, 0, 200, true, 1),
            new(1002, 31, 9301, 300, true, 1),
        };
        if (includeUnknownSibling)
            contents.Add(new NetherRawShopContent(1003, 999, 0, 1, true, 1));
        return new NetherRuntimeManagedShopPopupCapture(
            contents,
            [new NetherShopItemMaster(9301, 91, NetherRewardRarity.Red)]
        )
        {
            SnapshotFingerprint = snapshot.Fingerprint,
        };
    }

    private static void RegisterManagedShopPopup()
    {
        Type controllerType = ManagedShopControllerType.Value;
        MethodInfo setupPopupEvent = controllerType.GetMethod("SetupPopupEvent")!;
        object controller = Activator.CreateInstance(controllerType)!;
        NetherRuntimeBridge.ObservePatchedCall(
            setupPopupEvent,
            controller,
            [new object(), null!]
        );
    }

    private static void RegisterPopup()
    {
        MethodInfo setupPopupEvent = typeof(Project.Nether.NetherEventPopup.NetherEventPopupController)
            .GetMethod(nameof(Project.Nether.NetherEventPopup.NetherEventPopupController.SetupPopupEvent))!;
        NetherRuntimeBridge.ObservePatchedCall(
            setupPopupEvent,
            new Project.Nether.NetherEventPopup.NetherEventPopupController(),
            [new object(), null!]
        );
    }

    private static NetherRuntimeInteractivePreEntryInputsResult CaptureManagedPreEntry(
        NetherRuntimeBridge bridge,
        NetherSnapshot snapshot,
        NetherStrategyTypedSemanticProviderEvidence? provider
    )
    {
        if (provider != null)
        {
            bridge.RegisterTypedSemanticProviderFactory(current =>
                new NetherRuntimeTypedSemanticProviderScope(current.Fingerprint, provider)
            );
        }
        return bridge.CaptureManagedInteractivePreEntryFloor(
            snapshot,
            new NetherAutoClimbSettings { StrategyMode = NetherStrategyMode.Equipment },
            new Project.Nether.FloorSelection.ManagedFloorModel
            {
                MNetherMapFloorId = 900,
                ExtendId = 711,
                FloorType = (int)NetherFloorNodeType.Event,
            },
            new object[]
            {
                new Project.Nether.FloorSelection.ManagedMapFloorRow
                {
                    id = 900,
                    min_erosion_point = 0,
                    max_erosion_point = 1000,
                },
            },
            new object[]
            {
                new Project.Nether.FloorSelection.ManagedEventRow
                {
                    id = 711,
                    m_nether_map_floor_id = 900,
                    weight = 1,
                    type = 4,
                    m_nether_floor_event_part_id_1 = 712,
                },
            },
            new object[]
            {
                new Project.Nether.FloorSelection.ManagedEventPartRow
                {
                    id = 712,
                    target_type_1 = 8,
                    select_parameter_1 = 8201,
                },
            },
            Array.Empty<object>(),
            new object[]
            {
                new Project.Nether.FloorSelection.ManagedBattleRow
                {
                    id = 8201,
                    m_nether_map_floor_id = 900,
                    type = 9,
                    m_nether_battle_stage_id = 8202,
                    code_drop_ratio = 100,
                },
            },
            snapshot.CurrentNodeId,
            canCloseShop: false
        );
    }

    private static NetherRuntimeNativeEventPopupCapture Capture(NetherSnapshot snapshot) =>
        new(
            TargetCharacterId: 101,
            EventId: 711,
            DeclaredPartIds: [712, 0, 0, 0],
            Parts:
            [
                new NetherRuntimeNativeEventPart(
                    Id: 712,
                    TargetType1: 8,
                    SelectParameter1: 8201,
                    TargetType2: 0,
                    SelectParameter2: 0,
                    TargetType3: 0,
                    SelectParameter3: 0,
                    ContentType: 0,
                    ContentId: 0,
                    Amount: 0
                ),
            ],
            Battles:
            [new NetherRuntimeNativeBattle(8201, Type: 9, BattleStageId: 8202, CodeDropRatio: 100)],
            Items: []
        )
        {
            SnapshotFingerprint = snapshot.Fingerprint,
            FloorId = snapshot.CurrentFloorId,
            NodeId = snapshot.CurrentNodeId,
        };

    private static NetherRuntimeNativeEventPopupCapture CaptureReward(
        NetherSnapshot snapshot,
        int rawRarity
    ) => new(
        TargetCharacterId: 101,
        EventId: 711,
        DeclaredPartIds: [712, 0, 0, 0],
        Parts:
        [
            new NetherRuntimeNativeEventPart(
                Id: 712,
                TargetType1: 0,
                SelectParameter1: 0,
                TargetType2: 0,
                SelectParameter2: 0,
                TargetType3: 0,
                SelectParameter3: 0,
                ContentType: 30,
                ContentId: 8301,
                Amount: 1
            ),
        ],
        Battles: [],
        Items: [new NetherRuntimeNativeItem(8301, Type: 91, Rarity: rawRarity)]
    )
    {
        SnapshotFingerprint = snapshot.Fingerprint,
        FloorId = snapshot.CurrentFloorId,
        NodeId = snapshot.CurrentNodeId,
    };

    private static NetherSnapshot CreateSnapshot() => new()
    {
        Status = NetherSessionStatus.Play,
        CurrentFloorId = 900,
        CurrentNodeId = ((long)1 << 32) | 1,
        FloorLevel = 0,
        FloorIndex = 0,
        ErosionPoint = 20,
        NetherGold = 300,
        Characters = [new NetherCharacterState(101, 1000)],
        Floors =
        [
            new NetherFloorNode(900, 20, 0, NetherFloorNodeType.Event)
            {
                NodeId = ((long)1 << 32) | 1,
                IsUnlocked = true,
            },
        ],
    };

    private static NetherSnapshot CreateShopSnapshot(int gold, int treasureKeys) => new()
    {
        Status = NetherSessionStatus.Play,
        CurrentFloorId = 950,
        CurrentNodeId = ((long)1 << 32) | 1,
        FloorLevel = 95,
        FloorIndex = 95,
        NetherGold = gold,
        TreasureKeyCount = treasureKeys,
        ErosionPoint = 20,
        Characters = [new NetherCharacterState(101, 1000)],
        Floors =
        [
            new NetherFloorNode(950, 95, 0, NetherFloorNodeType.Shop)
            {
                NodeId = ((long)1 << 32) | 1,
                IsUnlocked = true,
            },
        ],
    };

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AutoNether.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("AutoNether repository root not found");
    }

    private static readonly Lazy<Type> ManagedShopControllerType = new(CreateManagedShopController);

    private static Type CreateManagedShopController()
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("Project"),
            AssemblyBuilderAccess.Run
        );
        ModuleBuilder module = assembly.DefineDynamicModule("ManagedShopPopupAdapter");
        TypeBuilder type = module.DefineType(
            "Project.Nether.NetherShopPopup.NetherShopPopupController",
            TypeAttributes.Public | TypeAttributes.Class
        );
        ConstructorBuilder constructor = type.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            Type.EmptyTypes
        );
        ILGenerator constructorIl = constructor.GetILGenerator();
        constructorIl.Emit(OpCodes.Ldarg_0);
        constructorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        constructorIl.Emit(OpCodes.Ret);
        MethodBuilder setup = type.DefineMethod(
            "SetupPopupEvent",
            MethodAttributes.Public,
            typeof(void),
            [typeof(object), typeof(object)]
        );
        setup.GetILGenerator().Emit(OpCodes.Ret);
        return type.CreateType()!;
    }

    private sealed class ManagedShopFloorDto
    {
        public long MNetherMapFloorId { get; init; }
        public long ExtendId { get; init; }
        public int FloorType { get; init; }
    }

    private sealed record PopupCapture(
        NetherSnapshot Snapshot,
        NetherRuntimeInteractivePreEntryInputsResult Interactive,
        NetherRuntimePopupResult Popup
    );

    private sealed record ManagedShopRouteCapture(
        NetherSnapshot Snapshot,
        NetherSnapshot RouteSnapshot,
        NetherRuntimeInteractivePreEntryInputsResult Interactive,
        NetherRuntimePopupResult Popup,
        NetherStrategyVisibleMapEvidence Visible
    );
}
