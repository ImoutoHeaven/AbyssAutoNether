#nullable enable

using System.Collections.Generic;
using System.Linq;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherStrategySpecReviewFixTests
{
    [Fact]
    public void Current_native_visible_capture_never_requests_residual_treasure_cache()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Services",
            "NetherRuntimeBridge.cs"
        ));
        int start = source.IndexOf(
            "private static bool TryMapStrategyVisibleEvidence(",
            StringComparison.Ordinal
        );
        int end = source.IndexOf("private static bool TryMapCharacters(", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        string capture = source[start..end];

        Assert.DoesNotContain("MNetherFloorTreasures", capture, StringComparison.Ordinal);
        Assert.Contains("UsesCurrentNativeTreasureEventAuthority = true", capture, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_capture_uses_the_native_ingame_buff_strategy_constructor_not_engine_registration()
    {
        // Fresh game evidence (Project.dll SHA-256
        // 033a5d1e92df1f90d15b4f33312fb935327fd2baa87811b7860b227d6c1c75f4):
        // BuffTypeStrategies implements IIngameServiceRegister, and its native constructor builds
        // the complete strategy map (including BuffType 90 and 120). BuffParameterByTypeExtension
        // also constructs this type directly as its null fallback. Engine.Get is the wrong registry.
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Services",
            "NetherNativeMechanicProductionCapture.cs"
        ));

        Assert.Contains(
            "new Project.Ingame.BuffTypeStrategies()",
            source,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "Engine.Get<Project.Ingame.BuffTypeStrategies>()",
            source,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Production_capture_reads_native_buff_query_targets_through_the_indexed_store_api()
    {
        // Fresh game evidence (Project.dll SHA-256
        // 033a5d1e92df1f90d15b4f33312fb935327fd2baa87811b7860b227d6c1c75f4):
        // BuffQuery delegates its query-target count/index reads to BuffTypeStrategies. The store
        // lazily builds the correctly oriented query-type -> held-type map. Directly enumerating an
        // IBuffStrategy's IL2CPP AdditionalMatchedQueryTypes array both reverses that relationship
        // and fails in production with get-enumerator-unavailable.
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Services",
            "NetherNativeMechanicProductionCapture.cs"
        ));

        Assert.Contains(
            "store!.GetQueryTargetTypeCount(queryBuffType)",
            source,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "store.GetQueryTargetTypeAt(queryBuffType, index)",
            source,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "strategy.AdditionalMatchedQueryTypes",
            source,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Native_mechanic_retains_same_popup_scope_coverage_for_self_target_resolution()
    {
        // Fresh game evidence for this exact build: AbilityTargetSelf.Logic.TargetResolve invokes
        // its callback with self. NetherCodeAbilityController installs each Code ability only on
        // units accepted by the Code's native Scope, while GetBuffTargetCount counts that same Scope
        // over NetherPartyModel.GetValidCharacterModels. The mechanic must retain that same-popup
        // count so Self can become All only when it equals the authoritative party size.
        Type mechanic = typeof(NetherStrategyNativeMechanic);

        Assert.Equal(
            typeof(bool),
            mechanic.GetProperty("PartyCoverageKnown")?.PropertyType
        );
        Assert.Equal(
            typeof(int),
            mechanic.GetProperty("PartyCoverage")?.PropertyType
        );

        string capture = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Services",
            "NetherNativeMechanicProductionCapture.cs"
        ));
        Assert.Contains(
            "PartyCoverageKnown = code.PartyCoverageKnown",
            capture,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "PartyCoverage = code.PartyCoverage",
            capture,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Route_audit_is_complete_for_every_frontier_candidate_and_records_comparison_data()
    {
        NetherFloorNode current = Node(1, 1, NetherFloorNodeType.Recovery);
        NetherFloorNode locked = Node(2, 2, NetherFloorNodeType.Recovery, 1) with
        {
            IsUnlocked = false,
        };
        NetherFloorNode first = Node(3, 2, NetherFloorNodeType.Recovery, 1) with
        {
            FloorIndex = 2,
        };
        NetherFloorNode second = Node(4, 2, NetherFloorNodeType.Recovery, 1) with
        {
            FloorIndex = 3,
        };
        NetherFloorNode boss = Node(5, 3, NetherFloorNodeType.Boss, 3, 4);
        NetherSnapshot snapshot = new()
        {
            Status = NetherSessionStatus.Play,
            CurrentFloorId = 1,
            CurrentNodeId = 1,
            Floors = [current, locked, first, second, boss],
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            snapshot,
            new NetherRouteSafetyContext
            {
                AllowLegacyComparatorCompatibility = true,
                EventProcurementCommitments = new Dictionary<NetherInteractiveEventOptionKey, NetherEventProcurementBudget>
                {
                    [new NetherInteractiveEventOptionKey(10, 20, 1)] = new(200, 1),
                },
            }
        );

        NetherRouteCandidateAudit[] audits = plan.Audit
            .Where(audit => audit.IsCandidate)
            .OrderBy(audit => audit.FloorId)
            .ToArray();
        Assert.Equal(3, audits.Length);
        Assert.Equal(3, plan.SelectionEvidence!.CandidateAudits.Count);
        Assert.Contains(audits, audit => audit.FloorId == 2 && audit.FirstFailingHardGate == NetherRouteCandidateHardGate.Locked);
        Assert.Single(audits, audit => audit.IsSelected);
        Assert.All(audits, audit =>
        {
            Assert.NotEmpty(audit.SemanticVectorUnknownReason);
            Assert.NotEmpty(audit.TieBreakOrder);
            Assert.NotEmpty(audit.ComparisonRationale);
            Assert.Equal(1, audit.ProcurementCommitmentCount);
        });
    }

    [Theory]
    [InlineData("party:active-party-hp-unavailable", (int)NetherStrategyUnknownReasonCode.PartyEvidenceUnavailable)]
    [InlineData("master-data:event-part-row-unavailable", (int)NetherStrategyUnknownReasonCode.MasterDataUnavailable)]
    [InlineData("inventory:shop-content-row-unavailable", (int)NetherStrategyUnknownReasonCode.InventoryEvidenceUnavailable)]
    [InlineData("transaction:event-commitment-mismatch", (int)NetherStrategyUnknownReasonCode.TransactionEvidenceUnavailable)]
    [InlineData("invalid-run-boundary-settings", (int)NetherStrategyUnknownReasonCode.ConfigurationUnknown)]
    [InlineData("unsupported-trigger-probability-type:7", (int)NetherStrategyUnknownReasonCode.TriggerEvidenceUnavailable)]
    [InlineData("buff-strategy-map-unavailable", (int)NetherStrategyUnknownReasonCode.BuffStrategyEvidenceUnavailable)]
    public void Route_unknown_reason_mapping_keeps_component_type(
        string detail,
        int expected
    )
    {
        Assert.Equal((NetherStrategyUnknownReasonCode)expected, NetherStrategyUnknownReasonCodes.FromDetail(detail));
    }

    [Fact]
    public void Unknown_frontier_node_is_rejected_locally_while_a_known_sibling_remains_selectable()
    {
        NetherSnapshot snapshot = new()
        {
            Status = NetherSessionStatus.Play,
            CurrentFloorId = 1,
            CurrentNodeId = 1,
            ErosionPoint = 20,
            Floors =
            [
                Node(1, 1, NetherFloorNodeType.Recovery),
                Node(2, 2, NetherFloorNodeType.Unknown, 1),
                Node(3, 2, NetherFloorNodeType.Recovery, 1),
                Node(4, 3, NetherFloorNodeType.Boss, 2, 3),
            ],
        };

        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            snapshot,
            new NetherRouteSafetyContext
            {
                AllowLegacyComparatorCompatibility = true,
                KnownNodeByFloorId = new Dictionary<long, bool>
                {
                    [2] = false,
                    [3] = true,
                },
                UnknownReasonCodeByFloorId = new Dictionary<long, NetherStrategyUnknownReasonCode>
                {
                    [2] = NetherStrategyUnknownReasonCode.MasterDataUnavailable,
                },
                UnknownDetailByFloorId = new Dictionary<long, string>
                {
                    [2] = "master-data:unknown-frontier-node",
                },
                HardSafeByFloorId = new Dictionary<long, bool> { [3] = true },
                HpSafeByFloorId = new Dictionary<long, bool> { [3] = true },
                MinimumWorstCaseErosionToTerminal = new Dictionary<long, int> { [3] = 1 },
            }
        );

        Assert.Equal(3, plan.SelectedNode!.NodeId);
        NetherRouteCandidateAudit unknown = Assert.Single(plan.Audit, audit => audit.FloorId == 2);
        Assert.Equal(NetherStrategyUnknownReasonCode.MasterDataUnavailable, unknown.UnknownReasonCode);
        Assert.Equal(NetherRouteCandidateHardGate.NativeNodeSemantics, unknown.FirstFailingHardGate);
        Assert.DoesNotContain(plan.Audit, audit => audit.Reason == "not-evaluated-after-unknown-frontier");
    }

    [Fact]
    public void Route_context_unknown_preserves_the_typed_party_reason_at_the_route_audit_seam()
    {
        NetherSnapshot snapshot = new()
        {
            Status = NetherSessionStatus.Play,
            CurrentFloorId = 1,
            CurrentNodeId = 1,
            Floors =
            [
                Node(1, 1, NetherFloorNodeType.Recovery),
                Node(2, 2, NetherFloorNodeType.Battle, 1),
                Node(3, 3, NetherFloorNodeType.Boss, 2),
            ],
        };
        NetherRoutePlan plan = new NetherRoutePlanner().Plan(
            snapshot,
            new NetherRouteSafetyContext
            {
                AllowLegacyComparatorCompatibility = true,
                KnownNodeByFloorId = new Dictionary<long, bool> { [2] = false },
                UnknownDetailByFloorId = new Dictionary<long, string>
                {
                    [2] = "party:active-party-hp-unavailable|master-data:known",
                },
                UnknownReasonCodeByFloorId = new Dictionary<long, NetherStrategyUnknownReasonCode>
                {
                    [2] = NetherStrategyUnknownReasonCode.PartyEvidenceUnavailable,
                },
            }
        );

        NetherRouteCandidateAudit audit = Assert.Single(plan.Audit, item => item.IsCandidate);
        Assert.Equal(NetherRouteCandidateHardGate.NativeNodeSemantics, audit.FirstFailingHardGate);
        Assert.Equal(NetherStrategyUnknownReasonCode.PartyEvidenceUnavailable, audit.UnknownReasonCode);
    }

    [Fact]
    public void Event_policy_emits_one_typed_audit_for_each_option_including_excluded_options()
    {
        NetherEventOption safe = Option(1, new NetherEffect(NetherEffectKind.Heal, 1)) with
        {
            EventId = 100,
            EventPartId = 1001,
        };
        NetherEventOption lethal = Option(2, new NetherEffect(NetherEffectKind.Damage, 500)) with
        {
            EventId = 100,
            EventPartId = 1002,
        };

        NetherEventDecision decision = new NetherEventPolicy().DecideEvent(
            Snapshot(hp: 500),
            [safe, lethal],
            Settings()
        );

        Assert.Equal(NetherEventDecisionKind.Select, decision.Kind);
        Assert.Equal(2, decision.OptionAudits.Count);
        Assert.Single(decision.OptionAudits, audit => audit.IsSelected);
        NetherEventOptionAudit rejected = Assert.Single(
            decision.OptionAudits,
            audit => audit.OptionNumber == 2
        );
        Assert.NotEqual(NetherEventOptionHardGate.None, rejected.FirstFailingHardGate);
        Assert.Equal(NetherStrategyUnknownReasonCode.None, rejected.UnknownReasonCode);
        Assert.NotEmpty(rejected.ComparisonRationale);
    }

    [Fact]
    public void Interactive_and_shop_evaluations_emit_one_typed_audit_per_option()
    {
        NetherInteractiveFloorPreEntrySafetyResult interactive = new NetherInteractiveFloorPreEntrySafety().Evaluate(
            new NetherInteractiveFloorPreEntrySafetyInput(
                NetherFloorNodeType.Event,
                900,
                [new NetherFloorMasterBoundsRow(900, 0, 10)],
                [new NetherFloorEventMasterRow(100, 900, 1, 1001, 1002, 0, 0)],
                [
                    new NetherFloorEventPartMasterRow(1001, (int)NetherEffectKind.Heal, 1, 0, 0, 0, 0, 0, 0, 0),
                    new NetherFloorEventPartMasterRow(1002, 99, 0, 0, 0, 0, 0, 0, 0, 0),
                ],
                20,
                [500],
                100,
                1,
                Settings()
            )
            {
                FloorNodeId = 901,
                CanCloseShop = true,
                CurrentCodes = [new NetherCodeState(40024, NetherCodeFamily.Risk, 1)],
                CodeCapacity = 5,
            }
        );

        Assert.Equal(2, interactive.OptionAudits.Count);
        Assert.Single(interactive.OptionAudits, audit => audit.IsSelected);
        Assert.Contains(
            interactive.OptionAudits,
            audit => audit.OptionNumber == 2
                && audit.FirstFailingHardGate != NetherInteractiveOptionHardGate.None
                && audit.UnknownReasonCode != NetherStrategyUnknownReasonCode.None
        );

        NetherShopDecision shop = new NetherEventPolicy().DecideShop(
            Snapshot(gold: 300, floorLevel: 91),
            [
                new NetherShopContent(1, 1, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true),
                new NetherShopContent(2, 2, 91, NetherRewardRarity.Gold, 300, usesNetherGold: true)
                {
                    CanonicalRewardTier = NetherCanonicalRewardTier.GoldRankFive,
                },
            ],
            Settings(shopMode: NetherShopMode.EquipmentBags)
        );

        Assert.Equal(NetherShopDecisionKind.Buy, shop.Kind);
        Assert.Equal(2, shop.OptionAudits.Count);
        Assert.Single(shop.OptionAudits, audit => audit.IsSelected);
        Assert.Contains(
            shop.OptionAudits,
            audit => audit.ContentId == 1
                && audit.FirstFailingHardGate != NetherShopOptionHardGate.None
        );
    }

    [Fact]
    public void Recovery_and_treasure_evaluations_emit_typed_audits_for_every_option()
    {
        NetherEventPolicy policy = new();
        NetherEventDecision recovery = policy.DecideRecovery(
            Snapshot(hp: 500),
            [
                Option(1, new NetherEffect(NetherEffectKind.Heal, 50)),
                Option(2, new NetherEffect(NetherEffectKind.Item, 1)),
            ],
            Settings()
        );

        Assert.Equal(2, recovery.OptionAudits.Count);
        Assert.All(recovery.OptionAudits, audit =>
        {
            Assert.NotEqual(NetherEventOptionHardGate.None, audit.FirstFailingHardGate);
            Assert.Equal(
                NetherStrategyUnknownReasonCode.RecoveryBranchSafetyUnavailable,
                audit.UnknownReasonCode
            );
            Assert.NotEmpty(audit.ComparisonRationale);
        });

        NetherEventDecision treasure = policy.DecideTreasure(
            Snapshot(keys: 1),
            [
                Option(1, new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1)),
                Option(2, new NetherEffect(NetherEffectKind.Damage, 1)),
            ],
            Settings()
        );

        Assert.Equal(2, treasure.OptionAudits.Count);
        Assert.Single(treasure.OptionAudits, audit => audit.IsSelected && audit.OptionNumber == 1);
        NetherEventOptionAudit excluded = Assert.Single(
            treasure.OptionAudits,
            audit => audit.OptionNumber == 2
        );
        Assert.Equal(NetherEventOptionHardGate.TreasurePaymentShape, excluded.FirstFailingHardGate);
        Assert.Equal(NetherStrategyUnknownReasonCode.None, excluded.UnknownReasonCode);
        Assert.NotEmpty(excluded.ComparisonRationale);
    }

    private static NetherFloorNode Node(long id, int level, NetherFloorNodeType type, params long[] previousIds) => new(id, level, (int)id, type)
    {
        IsUnlocked = true,
        PreviousFloorIds = previousIds,
    };

    private static NetherEventOption Option(int number, params NetherEffect[] effects) => new(number, effects);

    private static NetherAutoClimbSettings Settings(NetherShopMode shopMode = NetherShopMode.Off) => new()
    {
        ShopMode = shopMode,
        TreasureMode = NetherTreasureMode.KeyOnly,
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = 300,
    };

    private static NetherSnapshot Snapshot(
        int hp = 500,
        int gold = 100,
        int floorLevel = 1,
        int keys = 1
    ) => new()
    {
        Status = NetherSessionStatus.Play,
        FloorLevel = floorLevel,
        ErosionPoint = 20,
        NetherGold = gold,
        TreasureKeyCount = keys,
        Characters = [new NetherCharacterState(1, hp)],
    };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AutoNether.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate AutoNether repository root.");
    }
}
