using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

/// <summary>
/// Production repro for the live Recovery popup fault:
/// <c>audit=task ... floorId=161 terminal=Faulted
/// detail=owned-popup:owned-popup-policy:UnknownMasterData:recovery-complete-visible-branch-unavailable</c>.
///
/// Fresh native evidence (GameAssembly.dll f2ad9478…c75f4 / global-metadata.dat d7dffa62…5c27 /
/// Project.dll 033a5d1e…c75f4, Cpp2IL 2022.1.0-pre-release.21 diffable-cs):
/// <c>Project.Nether.NetherRecoverPopup.NetherRecoverPopupController</c> keeps only
/// <c>_netherDataStore</c>, <c>_mNetherEvents</c>, <c>_mNetherEventPartsArray</c> and
/// <c>_onConfirm</c>; its node identity arrives as a call argument of
/// <c>InitializeView(long mNetherMapFloorId, long extendId, Action&lt;NetherEventResultModel&gt;)</c>
/// and is never retained, so the live popup exposes no node id.
/// <c>Project.Master.NoaMessagePack.MNetherFloorEvents</c> is keyed by
/// <c>m_nether_map_floor_id</c>, so one event row is legitimately shared by several map nodes —
/// the observed run had event 354 (parts 102/402/700) on nodes 137438953474, 154618822657,
/// 154618822659 and 171798691844 at once.
/// </summary>
// Drives the NetherRuntimeBridge singleton; shares the serialized runtime collection.
[Collection("nether-managed-popup-runtime")]
public sealed class NetherRecoveryPopupProductionEvidenceTests
{
    private const long EventId = 354;
    private const long RestPartId = 102;
    private const long PurificationPartId = 402;
    private const long TransformPartId = 700;
    private const long CurrentNodeId = 150323855364;
    private const long CurrentFloorId = 149;
    private const long SelectedNodeId = 154618822659;
    private const long SelectedFloorId = 161;

    private static readonly long[] SiblingNodeIds =
    [
        137438953474,
        154618822657,
        SelectedNodeId,
        171798691844,
    ];

    [Fact]
    public void Recovery_popup_sharing_one_event_row_across_sibling_nodes_selects_the_proven_branch()
    {
        NetherSnapshot snapshot = Snapshot();

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            NativeRecoveryPopup() with
            {
                RouteOwnedNodeId = SelectedNodeId,
                RecoveryBranchSafetyByPartId = SelectedNodeProofs(),
            },
            Package(snapshot),
            InteractiveSiblingRecoveryNodes(snapshot),
            Settings()
        );

        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            snapshot,
            bound,
            Settings(),
            NoActiveErosion()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.Action.Kind);
        // Purification (-30 erosion) is the proven deterministic exit for the selected node.
        Assert.Equal(1, decision.Action.OptionNumber);
        Assert.Equal(PurificationPartId, decision.Action.EventPartId);
        // Only the route-selected option is justified by the route, and it now resolves against
        // the committed node instead of collapsing on the four-way sibling ambiguity.
        NetherEventOption purification = bound.Options.Single(option =>
            option.EventPartId == PurificationPartId
        );
        Assert.Equal(SelectedNodeId, purification.NodeId);
        Assert.Equal(SelectedFloorId, purification.FloorId);
        Assert.True(purification.HasRouteSafetyEvidence);
    }

    [Fact]
    public void Recovery_popup_without_a_bound_proof_for_its_own_parts_scores_locally_instead_of_faulting()
    {
        // Mirrors the post-Battle deferral NetherRecoveryBranchProofScope introduced on the
        // pre-entry side: the route bound proofs for an earlier row only, so this native row must
        // fall back to option-local scoring rather than fail closed and terminate the climb.
        NetherSnapshot snapshot = Snapshot();

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            NativeRecoveryPopup() with
            {
                RouteOwnedNodeId = SelectedNodeId,
                RecoveryBranchSafetyByPartId = UnrelatedProofs(),
            },
            Package(snapshot),
            InteractiveSiblingRecoveryNodes(snapshot, proofs: UnrelatedProofs()),
            Settings()
        );

        Assert.False(bound.RequireCompleteRecoveryBranchSafety);

        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            snapshot,
            bound,
            Settings(),
            NoActiveErosion()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.Action.Kind);
    }

    [Fact]
    public void Ownerless_recovery_popup_already_open_at_enable_uses_the_current_snapshot_node()
    {
        // Live repro: F12 is pressed while the native Recovery popup is already open. The popup
        // has no route-owned parent yet, but the authoritative snapshot is already positioned on
        // its exact current map node. Shared event rows make key-only matching ambiguous.
        NetherSnapshot snapshot = Snapshot() with
        {
            Status = NetherSessionStatus.Wait,
            CurrentFloorId = SelectedFloorId,
            CurrentNodeId = SelectedNodeId,
        };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            NativeRecoveryPopup() with
            {
                OwnerAction = NetherActionKind.None,
                HasRecoveredFloorEventTaskEvidence = true,
            },
            Package(snapshot),
            InteractiveSiblingRecoveryNodes(
                snapshot,
                new Dictionary<long, NetherRecoveryBranchSafetyEvidence>()
            ),
            Settings()
        );

        Assert.Equal(SelectedNodeId, bound.RouteOwnedNodeId);
        Assert.False(bound.RequireCompleteRecoveryBranchSafety);
        NetherEventOption purification = bound.Options.Single(option =>
            option.EventPartId == PurificationPartId
        );
        Assert.True(purification.HasRouteSafetyEvidence);

        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            snapshot,
            bound,
            Settings(),
            NoActiveErosion()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.Action.Kind);
        Assert.Equal(PurificationPartId, decision.Action.EventPartId);
    }

    /// <summary>
    /// Production repro of the live pause
    /// <c>popup-option:Recovery:0:1:354:402:1 ... detail=event-option-route-evidence-unavailable</c>.
    /// The visible map is produced by the real <see cref="NetherStrategyVisibleEvidenceMapper"/>
    /// over the four sibling Recovery nodes that shared event row 354, exactly as the live
    /// FloorSelection map did. The mapper emits one Event row per node, so the popup's key-only
    /// visible join sees four rows for (354, part) and cannot pick its own.
    /// </summary>
    [Fact]
    public void Ownerless_recovery_popup_resolves_its_own_visible_row_when_siblings_share_the_event_master_row()
    {
        NetherSnapshot snapshot = Snapshot() with
        {
            Status = NetherSessionStatus.Wait,
            CurrentFloorId = SelectedFloorId,
            CurrentNodeId = SelectedNodeId,
        };
        NetherStrategyEvidencePackage package = ProductionMappedPackage(snapshot);
        // The live map published the same event row on every sibling node.
        Assert.Equal(
            SiblingNodeIds.Length,
            package.VisibleMap.Value!.ContentRows.Count(row =>
                row.Kind == NetherStrategyVisibleContentKind.Event
                && row.EventId == EventId
                && row.EventPartId == PurificationPartId
            )
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            NativeRecoveryPopup() with
            {
                OwnerAction = NetherActionKind.None,
                HasRecoveredFloorEventTaskEvidence = true,
            },
            package,
            InteractiveSiblingRecoveryNodes(
                snapshot,
                new Dictionary<long, NetherRecoveryBranchSafetyEvidence>()
            ),
            Settings()
        );

        Assert.Equal(SelectedNodeId, bound.RouteOwnedNodeId);
        NetherEventOption purification = bound.Options.Single(option =>
            option.EventPartId == PurificationPartId
        );
        Assert.Equal(string.Empty, purification.UnknownReason);

        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            snapshot,
            bound,
            Settings(),
            NoActiveErosion()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.Action.Kind);
        Assert.Equal(PurificationPartId, decision.Action.EventPartId);
    }

    /// <summary>
    /// Without an owned node the shared visible row stays genuinely ambiguous and must fail closed.
    /// </summary>
    [Fact]
    public void Recovery_popup_without_an_owned_node_keeps_a_shared_visible_row_unknown()
    {
        NetherSnapshot snapshot = Snapshot() with
        {
            Status = NetherSessionStatus.Wait,
            CurrentFloorId = SelectedFloorId,
            CurrentNodeId = SelectedNodeId,
        };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            NativeRecoveryPopup() with { OwnerAction = NetherActionKind.None },
            ProductionMappedPackage(snapshot),
            InteractiveSiblingRecoveryNodes(
                snapshot,
                new Dictionary<long, NetherRecoveryBranchSafetyEvidence>()
            ),
            Settings()
        );

        Assert.Equal(0, bound.RouteOwnedNodeId);
        Assert.All(bound.Options, option => Assert.False(option.StrategyEvidence!.IsKnown));
    }

    /// <summary>
    /// Same shared-visible-row fault reached without a recovered owned node: only one sibling is
    /// pre-entry safe, so the option key resolves to exactly one projection, while the visible map
    /// still publishes the shared event row on all four siblings. The uniquely projected node is
    /// the popup's node, so its own visible row must be the evidence.
    /// </summary>
    [Fact]
    public void Recovery_popup_uses_the_visible_row_of_its_uniquely_projected_node()
    {
        NetherSnapshot snapshot = Snapshot() with
        {
            Status = NetherSessionStatus.Wait,
            CurrentFloorId = SelectedFloorId,
            CurrentNodeId = SelectedNodeId,
        };
        NetherRuntimeInteractivePreEntryInputsResult interactive = OnlySelectedSiblingIsSafe(
            InteractiveSiblingRecoveryNodes(
                snapshot,
                new Dictionary<long, NetherRecoveryBranchSafetyEvidence>()
            )
        );

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            NativeRecoveryPopup() with { OwnerAction = NetherActionKind.None },
            ProductionMappedPackage(snapshot),
            interactive,
            Settings()
        );

        Assert.Equal(0, bound.RouteOwnedNodeId);
        NetherEventOption purification = bound.Options.Single(option =>
            option.EventPartId == PurificationPartId
        );
        Assert.Equal(SelectedNodeId, purification.NodeId);
        Assert.Equal(string.Empty, purification.UnknownReason);
    }

    [Fact]
    public void Ownerless_recovery_popup_without_the_native_parent_evidence_does_not_borrow_snapshot_node()
    {
        NetherSnapshot snapshot = Snapshot() with
        {
            Status = NetherSessionStatus.Wait,
            CurrentFloorId = SelectedFloorId,
            CurrentNodeId = SelectedNodeId,
        };

        NetherRuntimePopupContext bound = NetherEventProductionEvidenceBinding.Bind(
            NativeRecoveryPopup() with
            {
                OwnerAction = NetherActionKind.None,
                RecoveryBranchSafetyByPartId = SelectedNodeProofs(),
            },
            Package(snapshot),
            InteractiveSiblingRecoveryNodes(snapshot),
            Settings()
        );

        // A snapshot alone must never adopt a stale or unrelated ownerless popup.
        Assert.Equal(0, bound.RouteOwnedNodeId);
    }

    [Fact]
    public void Real_runtime_bridge_carries_the_route_owned_node_and_recovery_proofs_into_the_recovery_popup()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherRuntimeBridge bridge = NetherRuntimeBridge.Instance;
        bridge.ClearRegistrations();
        try
        {
            // Production order: the FloorSelection scene registers first (that registration retires
            // the previous owner's route state), then the route plans and publishes its proofs.
            NetherRuntimeBridge.RegisterFloorSelection(
                new Project.Nether.FloorSelection.SubViewController()
            );
            bridge.BeginRouteReplan(snapshot.Fingerprint);
            bridge.CommitRouteOwnedEventProcurementCommitments(
                new NetherRouteBranchIdentity(
                    snapshot.Fingerprint,
                    CurrentNodeId,
                    SelectedNodeId,
                    CurrentNodeId + ">" + SelectedNodeId
                ),
                new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>()
            );
            bridge.BindRecoveryBranchSafetyProofs(SelectedNodeProofs());
            bridge.RegisterTypedSemanticProviderFactory(null);
            bridge.RegisterNativeEventPopupCaptureFactory((_, kind) =>
                kind == NetherRuntimePopupKind.Recovery ? NativeRecoveryCapture(snapshot) : null
            );
            RegisterRecoverPopup();

            NetherRuntimePopupResult result = bridge.TryGetActivePopup();

            Assert.True(result.Popup != null, result.Detail);
            NetherRuntimePopupContext popup = result.Popup!;
            Assert.Equal(NetherRuntimePopupKind.Recovery, popup.Kind);
            // The native capture carries no node identity, so the route's committed entry node is
            // the only thing that can correlate this popup with its pre-entry capture.
            Assert.Equal(0, popup.NodeId);
            Assert.Equal(SelectedNodeId, popup.RouteOwnedNodeId);
            Assert.Equal(
                new[] { RestPartId, PurificationPartId, TransformPartId }.OrderBy(id => id),
                popup.RecoveryBranchSafetyByPartId.Keys.OrderBy(id => id)
            );
            Assert.All(popup.Options, option => Assert.NotNull(option.RecoveryBranchSafety));
            Assert.True(popup.RequireCompleteRecoveryBranchSafety);
        }
        finally
        {
            bridge.ClearRegistrations();
        }
    }

    /// <summary>
    /// The live popup as the production mapper emits it: exact event/part identity, but no
    /// floor/node identity because the native controller does not retain it.
    /// </summary>
    private static NetherRuntimePopupContext NativeRecoveryPopup() => new()
    {
        Kind = NetherRuntimePopupKind.Recovery,
        RawFloorType = (int)NetherFloorNodeType.Recovery,
        OwnerAction = NetherActionKind.SelectFloor,
        RequireCompleteRecoveryBranchSafety = true,
        Options = RecoveryOptions(),
    };

    /// <summary>
    /// Native option order as the run recorded it: option 1 = Purification (part 402),
    /// option 2 = Rest (part 102), option 3 = Transform (part 700). Target types are the raw
    /// <c>MNetherFloorEventParts.target_type_1</c> values 4/1/7.
    /// </summary>
    private static NetherRuntimeNativeEventPopupCapture NativeRecoveryCapture(NetherSnapshot snapshot) =>
        new(
            TargetCharacterId: 0,
            EventId: EventId,
            DeclaredPartIds: [PurificationPartId, RestPartId, TransformPartId, 0],
            Parts:
            [
                NativePart(PurificationPartId, targetType: 4, parameter: 30),
                NativePart(RestPartId, targetType: 1, parameter: 300),
                NativePart(TransformPartId, targetType: 7, parameter: 0),
            ],
            Battles: [],
            Items: []
        )
        {
            SnapshotFingerprint = snapshot.Fingerprint,
        };

    private static NetherRuntimeNativeEventPart NativePart(long id, int targetType, long parameter) =>
        new(
            Id: id,
            TargetType1: targetType,
            SelectParameter1: parameter,
            TargetType2: 0,
            SelectParameter2: 0,
            TargetType3: 0,
            SelectParameter3: 0,
            ContentType: 0,
            ContentId: 0,
            Amount: 0
        );

    private static void RegisterRecoverPopup()
    {
        MethodInfo setupPopupEvent =
            typeof(Project.Nether.NetherRecoverPopup.NetherRecoverPopupController)
                .GetMethod("SetupPopupEvent")!;
        NetherRuntimeBridge.ObservePatchedCall(
            setupPopupEvent,
            new Project.Nether.NetherRecoverPopup.NetherRecoverPopupController(),
            [new object(), null!]
        );
    }

    private static IReadOnlyList<NetherEventOption> RecoveryOptions() =>
    [
        new NetherEventOption(1, [new NetherEffect(NetherEffectKind.ErosionHeal, 30)])
        {
            EventId = EventId,
            EventPartId = PurificationPartId,
            RequiresExactBinding = true,
        },
        new NetherEventOption(2, [new NetherEffect(NetherEffectKind.Heal, 300)])
        {
            EventId = EventId,
            EventPartId = RestPartId,
            RequiresExactBinding = true,
        },
        new NetherEventOption(3, [new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0)])
        {
            EventId = EventId,
            EventPartId = TransformPartId,
            RequiresExactBinding = true,
        },
    ];

    private static IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> SelectedNodeProofs() =>
        new Dictionary<long, NetherRecoveryBranchSafetyEvidence>
        {
            [RestPartId] = Proof(NetherRecoveryBranchKind.Rest, nextVisibleBranchSafe: false),
            [PurificationPartId] = Proof(NetherRecoveryBranchKind.Purification, nextVisibleBranchSafe: true),
            [TransformPartId] = Proof(NetherRecoveryBranchKind.Transform, nextVisibleBranchSafe: false),
        };

    private static IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> UnrelatedProofs() =>
        new Dictionary<long, NetherRecoveryBranchSafetyEvidence>
        {
            [RestPartId + 90_000] = Proof(NetherRecoveryBranchKind.Rest, nextVisibleBranchSafe: true),
        };

    private static NetherRecoveryBranchSafetyEvidence Proof(
        NetherRecoveryBranchKind kind,
        bool nextVisibleBranchSafe
    ) => new()
    {
        BranchKind = kind,
        IsKnown = true,
        IsCompleteVisibleBranch = true,
        IsNextVisibleBranchSafe = nextVisibleBranchSafe,
    };

    /// <summary>
    /// Four safe sibling Recovery nodes exposing the same event row, exactly as the live map did.
    /// Only <see cref="SelectedNodeId"/> is the route-committed entry.
    /// </summary>
    private static NetherRuntimeInteractivePreEntryInputsResult InteractiveSiblingRecoveryNodes(
        NetherSnapshot snapshot,
        IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence>? proofs = null
    )
    {
        IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> boundProofs =
            proofs ?? SelectedNodeProofs();
        var captures = new Dictionary<long, NetherRuntimeInteractivePreEntryCaptureResult>();
        foreach (long nodeId in SiblingNodeIds)
        {
            long floorId = nodeId == SelectedNodeId ? SelectedFloorId : SelectedFloorId + 1;
            Dictionary<NetherInteractiveEventOptionKey, NetherInteractiveOptionProjection> projections =
                RecoveryOptions().ToDictionary(
                    option => new NetherInteractiveEventOptionKey(
                        option.EventId,
                        option.EventPartId,
                        option.OptionNumber
                    ),
                    option => Projection(option, floorId, nodeId)
                );
            NetherInteractiveOptionProjection selected = projections[
                new NetherInteractiveEventOptionKey(EventId, PurificationPartId, 1)
            ];
            captures[nodeId] = new NetherRuntimeInteractivePreEntryCaptureResult
            {
                IsCaptured = true,
                Input = new NetherInteractiveFloorPreEntrySafetyInput(
                    NetherFloorNodeType.Recovery,
                    floorId,
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
                    FloorNodeId = nodeId,
                    RecoveryBranchSafetyByPartId = boundProofs,
                    RequireCompleteRecoveryBranchSafety = boundProofs.Count > 0,
                },
                Safety = NetherInteractiveFloorPreEntrySafetyResult.Safe(
                    new Dictionary<long, int> { [EventId] = selected.OptionNumber },
                    new Dictionary<long, NetherInteractiveOptionProjection> { [EventId] = selected },
                    optionProjections: projections
                ),
            };
        }
        return NetherRuntimeInteractivePreEntryInputsResult.Success(captures, snapshot.Fingerprint);
    }

    /// <summary>
    /// Keeps only <see cref="SelectedNodeId"/> pre-entry safe; the other siblings stay captured but
    /// unsafe, exactly as an erosion/HP-bound sibling floor does in production.
    /// </summary>
    private static NetherRuntimeInteractivePreEntryInputsResult OnlySelectedSiblingIsSafe(
        NetherRuntimeInteractivePreEntryInputsResult interactive
    ) => interactive with
    {
        ByFloorNodeId = interactive.ByFloorNodeId.ToDictionary(
            entry => entry.Key,
            entry => entry.Key == SelectedNodeId
                ? entry.Value
                : entry.Value with
                {
                    Safety = NetherInteractiveFloorPreEntrySafetyResult.Pause(
                        NetherPauseReason.UnsafeErosion,
                        "sibling-floor-erosion-bound"
                    ),
                }
        ),
    };

    private static NetherInteractiveOptionProjection Projection(
        NetherEventOption option,
        long floorId,
        long nodeId
    ) => new(
        option.OptionNumber,
        ErosionDelta: option.EventPartId == PurificationPartId ? -30 : 0,
        HpDelta: option.EventPartId == RestPartId ? 300 : 0,
        ExpectedEffects: option.Effects
    )
    {
        EventId = option.EventId,
        EventPartId = option.EventPartId,
        FloorId = floorId,
        NodeId = nodeId,
        HasRouteSafetyEvidence = true,
        RouteSafetyAllowed = true,
    };

    private static NetherStrategyEvidencePackage Package(NetherSnapshot snapshot)
    {
        NetherStrategyVisibleContentRow[] rows = RecoveryOptions()
            .Select(option => new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Event,
                SelectedNodeId,
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
                            IsKnown = true,
                            IsPresent = true,
                        }).ToArray()
                    ),
                ],
            })
            .ToArray();
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
            VisibleMap = NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Known(
                new NetherStrategyVisibleMapEvidence([], rows)
            ),
        };
    }

    /// <summary>
    /// Visible-map evidence produced by the real production mapper over the four sibling Recovery
    /// nodes of the live map, all resolving to the same <c>MNetherFloorEvents</c> row 354 through
    /// their native <c>ExtendId</c>.
    /// </summary>
    private static NetherStrategyEvidencePackage ProductionMappedPackage(NetherSnapshot snapshot)
    {
        NetherFloorNode[] floors = SiblingNodeIds
            .Select((nodeId, index) => new NetherFloorNode(
                SelectedFloorId,
                35,
                index,
                NetherFloorNodeType.Recovery
            )
            {
                NodeId = nodeId,
                IsUnlocked = true,
            })
            .ToArray();
        NetherStrategyVisibleEvidenceCaptureResult mapped = NetherStrategyVisibleEvidenceMapper.Map(
            new NetherStrategyVisibleEvidenceCaptureRequest(
                floors,
                [],
                [],
                // Native option order: part 402 -> option 1, 102 -> option 2, 700 -> option 3.
                [new NetherFloorEventMasterRow(
                    EventId,
                    SelectedFloorId,
                    1,
                    PurificationPartId,
                    RestPartId,
                    TransformPartId,
                    0
                )],
                [
                    // Raw MNetherFloorEventParts.target_type_1 values 4/1/7.
                    new NetherFloorEventPartMasterRow(PurificationPartId, 4, 30, 0, 0, 0, 0, 0, 0, 0),
                    new NetherFloorEventPartMasterRow(RestPartId, 1, 300, 0, 0, 0, 0, 0, 0, 0),
                    new NetherFloorEventPartMasterRow(TransformPartId, 7, 0, 0, 0, 0, 0, 0, 0, 0),
                ],
                []
            )
            {
                ExtendIdByNodeId = SiblingNodeIds.ToDictionary(nodeId => nodeId, _ => EventId),
            }
        );
        Assert.True(mapped.IsSuccess, mapped.Detail);
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
            VisibleMap = NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Known(
                mapped.Evidence!
            ),
        };
    }

    private static NetherSnapshot Snapshot() => new()
    {
        Status = NetherSessionStatus.Play,
        NetherId = 1,
        MapId = 1,
        CurrentFloorId = CurrentFloorId,
        CurrentNodeId = CurrentNodeId,
        ErosionPoint = 70,
        NetherGold = 160,
        Characters = [new NetherCharacterState(1300003, 1000)],
    };

    private static NetherAutoClimbSettings Settings() => new()
    {
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = 300,
        TreasureMode = NetherTreasureMode.KeyOnly,
        StrategyMode = NetherStrategyMode.Equipment,
    };

    private static NetherActiveCodeErosionProjection NoActiveErosion() => new()
    {
        ErosionProjectionKnown = true,
        CodeHash = "nether-codes:none",
        ErosionEffects = [],
    };
}
