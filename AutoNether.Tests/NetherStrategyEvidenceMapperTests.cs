#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherStrategyEvidenceMapperTests
{
    [Fact]
    public void Mapper_copies_complete_party_code_research_mechanic_and_visible_evidence_immutably()
    {
        var parameters = new[] { new NetherStrategyNamedValue("Attack", 1234) };
        var characterEffects = new[] { new NetherStrategyAbilityEffect(7001, 2, 3, 40) };
        var party = new[]
        {
            new NetherStrategyPartyMember(
                11,
                1,
                NetherPartyPosition.Back,
                3,
                NetherCrestIdentity.Impact,
                875,
                true,
                90,
                5
            )
            {
                NativeParameters = parameters,
                CharacterAbilityEffects = characterEffects,
                EquipmentAbilityEffects = new[] { new NetherStrategyAbilityEffect(7002, 1, 4, 20) },
                GeneralAbilityEffects = new[] { new NetherStrategyAbilityEffect(7003, 1, 5, 10) },
            },
        };
        var codes = new[]
        {
            Code(101, NetherCodeFamily.Rush, 1),
            Code(102, NetherCodeFamily.Rush, 1),
            Code(201, NetherCodeFamily.Impact, 1),
            Code(301, NetherCodeFamily.Safe, 0),
        };
        var research = new[]
        {
            new NetherStrategyResearchFamilyState(NetherCodeFamily.Rush, 12, 4, 15)
            {
                SettlementAcquiredCodeCount = 2,
            },
            new NetherStrategyResearchFamilyState(NetherCodeFamily.Impact, 8, 0, 15),
            new NetherStrategyResearchFamilyState(NetherCodeFamily.Safe, 3, 0, 15),
            new NetherStrategyResearchFamilyState(NetherCodeFamily.Risk, 2, 0, 15),
        };
        var mechanics = new[]
        {
            new NetherStrategyNativeMechanic(
                7001,
                NetherCodeMasterEffectType.NetherAbility,
                [KnownTrigger(NetherStrategyTriggerKind.StartBattle)],
                new NetherStrategyTargetEvidence(NetherStrategyTargetKind.Friend)
                {
                    ParametersKnown = true,
                    NativeTypeIdentity = "Project.AbilityTargets.AbilityTargetAllAllies",
                }
            )
            {
                Duration = 15,
                DurationKnown = true,
                Cap = 3,
                CapKnown = true,
                AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                    NetherStrategyAbilityEffectKind.ParameterBuff
                ),
                BuffStrategies =
                [
                    new NetherStrategyBuffEvidence(
                        new NetherStrategyBuffType(10),
                        NetherStrategyBuffEffectKind.Buff,
                        NetherStrategyStatusPriorityKind.Buff,
                        NetherStrategyBuffCoexistenceKind.HigherValue
                    ),
                ],
            },
        };
        var visibleRows = new[]
        {
            new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Battle,
                9001,
                5001,
                6001
            ) { Rank = 5, Amount = 1 },
        };
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(Identity(snapshot), snapshot)
            {
                Party = party,
                OwnedCodes = new NetherStrategyOwnedCodeEvidence(codes, 25, 2)
                {
                    CategorySkills = new[]
                    {
                        new NetherStrategyCategorySkill(8001, 5, NetherCodeFamily.Rush, 1, 9001, 1, 0),
                    },
                },
                Research = research,
                NativeMechanics = mechanics,
                VisibleMap = new NetherStrategyVisibleMapEvidence(snapshot.Floors, visibleRows),
            }
        );

        Assert.True(mapped.IsMapped, mapped.Detail);
        NetherStrategyEvidencePackage package = mapped.Package!;
        Assert.True(package.Party.IsKnown);
        Assert.Equal(NetherCrestIdentity.Impact, package.Party.Value!.Members[0].Crest);
        Assert.Equal(1234, package.Party.Value.Members[0].NativeParameters[0].Value);
        Assert.Equal(7001, package.Party.Value.Members[0].CharacterAbilityEffects[0].EffectId);
        Assert.True(package.OwnedCodes.IsKnown);
        Assert.Equal(new long[] { 101, 102, 201 }, package.OwnedCodes.Value!.Codes.Select(code => code.CodeId));
        NetherStrategyFamilyCount rush = Assert.Single(
            package.OwnedCodes.Value.FamilyCounts,
            count => count.Family == NetherCodeFamily.Rush
        );
        Assert.Equal(2, rush.OwnedCount);
        Assert.Equal(1, rush.OpposingCount);
        Assert.Equal(1, rush.EffectiveCount);
        Assert.True(package.Research.IsKnown);
        Assert.Equal(4, package.Research.Value!.Families.Count);
        Assert.True(package.NativeMechanics.IsKnown);
        Assert.Equal(
            NetherStrategyBuffCoexistenceKind.HigherValue,
            package.NativeMechanics.Value!.Mechanics[0].BuffStrategies[0].Coexistence
        );
        Assert.True(package.VisibleMap.IsKnown);
        Assert.Equal(NetherStrategyVisibleContentKind.Battle, package.VisibleMap.Value!.ContentRows[0].Kind);

        parameters[0] = new NetherStrategyNamedValue("Attack", 9999);
        characterEffects[0] = new NetherStrategyAbilityEffect(9999, 1, 1, 1);
        codes[0] = Code(9999, NetherCodeFamily.Risk, 1);
        research[0] = new NetherStrategyResearchFamilyState(NetherCodeFamily.Rush, 99, 99, 99);
        mechanics[0] = mechanics[0] with
        {
            BuffStrategies =
            [
                mechanics[0].BuffStrategies[0] with
                {
                    Coexistence = NetherStrategyBuffCoexistenceKind.Latest,
                },
            ],
        };
        visibleRows[0] = visibleRows[0] with { Rank = 1 };

        Assert.Equal(1234, package.Party.Value.Members[0].NativeParameters[0].Value);
        Assert.Equal(7001, package.Party.Value.Members[0].CharacterAbilityEffects[0].EffectId);
        Assert.Equal(101, package.OwnedCodes.Value.Codes[0].CodeId);
        Assert.Equal(12, package.Research.Value.Families[0].WalletPoints);
        Assert.Equal(
            NetherStrategyBuffCoexistenceKind.HigherValue,
            package.NativeMechanics.Value.Mechanics[0].BuffStrategies[0].Coexistence
        );
        Assert.Equal(5, package.VisibleMap.Value.ContentRows[0].Rank);
    }

    [Fact]
    public void Battle_result_code_owner_maps_current_evidence_without_a_floor_scene_entered_event()
    {
        // Fresh current-game Cpp2IL evidence (Project.dll 033a5d1e...c75f4,
        // GameAssembly.dll f2ad9478...767) shows
        // AbyssCodeSelectPopupController.InitializeView(NetherPartyModel, long[], Action<long>,
        // Action) stores the live party model, while FloorSelection transitions to the Result
        // scene before Result's later floor-selection return. A result-owned offer therefore has
        // a positive result owner generation but no current FloorSelection.OnEntered proof.
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(
                new NetherStrategyEvidenceIdentity(
                    RuntimeGeneration: 6,
                    ControllerOwnerGeneration: 4,
                    EnteredSubsceneGeneration: 0,
                    SnapshotFingerprint: snapshot.Fingerprint
                ),
                snapshot
            )
        );

        Assert.True(mapped.IsMapped, mapped.Detail);
        Assert.Equal(6, mapped.Package!.Identity.RuntimeGeneration);
        Assert.Equal(4, mapped.Package.Identity.ControllerOwnerGeneration);
        Assert.Equal(0, mapped.Package.Identity.EnteredSubsceneGeneration);
    }

    [Fact]
    public void Unknown_party_is_local_and_display_diagnostics_cannot_substitute_for_mechanics()
    {
        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(Identity(snapshot), snapshot)
            {
                Party = null,
                OwnedCodes = new NetherStrategyOwnedCodeEvidence(
                    new[] { Code(101, NetherCodeFamily.Rush, 1) },
                    25,
                    1
                ),
                NativeMechanics = null,
                DisplayDiagnostics = new[]
                {
                    new NetherStrategyDisplayDiagnostic(101, 999999, 7),
                },
            }
        );

        Assert.True(mapped.IsMapped, mapped.Detail);
        Assert.False(mapped.Package!.Party.IsKnown);
        Assert.Equal("party-profile-unavailable", mapped.Package.Party.UnknownReason);
        Assert.True(mapped.Package.OwnedCodes.IsKnown);
        Assert.False(mapped.Package.NativeMechanics.IsKnown);
        Assert.Equal("native-mechanics-unavailable", mapped.Package.NativeMechanics.UnknownReason);
        Assert.Single(mapped.Package.DisplayDiagnostics);
    }

    [Fact]
    public void Runtime_capture_error_survives_exactly_in_only_its_dependent_component()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300: NetherPartyCharacterModel exposes
        // BasicParameters/BondParameters/... as CharacterParameters and the three exact
        // AbilityEffectModel arrays. A failed member read is therefore component evidence, not a
        // reason to erase the authoritative server snapshot or a separately captured code set.
        NetherSnapshot snapshot = Snapshot();
        const string exactRuntimeError =
            "missing-strategy-character-parameters:11:EquipmentAbilityMultiplicationParameters";

        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(Identity(snapshot), snapshot)
            {
                Party = null,
                PartyUnknownReason = exactRuntimeError,
                OwnedCodes = new NetherStrategyOwnedCodeEvidence(
                    Array.Empty<NetherCodeState>(),
                    25,
                    1
                ),
            }
        );

        Assert.True(mapped.IsMapped, mapped.Detail);
        Assert.NotNull(mapped.Package!.Server);
        Assert.False(mapped.Package.Party.IsKnown);
        Assert.Equal(exactRuntimeError, mapped.Package.Party.UnknownReason);
        Assert.True(mapped.Package.OwnedCodes.IsKnown, mapped.Package.OwnedCodes.UnknownReason);
    }

    [Fact]
    public void Mapper_preserves_typed_native_trigger_target_effect_and_buff_coexistence()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300: IAbilityEffectData exposes typed
        // BattleSituationBase/AbilityTargetBase/AbilityEffectBase objects; IBuffStrategy exposes
        // BuffType, BuffEffectType, StatusPriorityType and BuffCoexistenceType (HigherValue=1).
        NetherSnapshot snapshot = Snapshot();
        var mechanic = new NetherStrategyNativeMechanic(
            7001,
            NetherCodeMasterEffectType.NetherAbility,
            [KnownTrigger(NetherStrategyTriggerKind.StartBattle)],
            new NetherStrategyTargetEvidence(NetherStrategyTargetKind.Friend)
            {
                ParametersKnown = true,
                NativeTypeIdentity = "Project.AbilityTargets.AbilityTargetAllAllies",
            }
        )
        {
            AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                NetherStrategyAbilityEffectKind.ParameterBuff
            ),
            BuffStrategies =
            [
                new NetherStrategyBuffEvidence(
                    new NetherStrategyBuffType(10),
                    NetherStrategyBuffEffectKind.Buff,
                    NetherStrategyStatusPriorityKind.Buff,
                    NetherStrategyBuffCoexistenceKind.HigherValue
                ),
            ],
            MasterEffectParameter1 = 9001,
            MasterEffectParameter2 = 1,
        };

        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(Identity(snapshot), snapshot)
            {
                NativeMechanics = [mechanic],
            }
        );

        NetherStrategyNativeMechanic copied = Assert.Single(
            Assert.IsType<NetherStrategyNativeMechanicsEvidence>(
                mapped.Package!.NativeMechanics.Value
            ).Mechanics
        );
        Assert.Equal(NetherStrategyTriggerKind.StartBattle, Assert.Single(copied.Triggers).Kind);
        Assert.Equal(NetherStrategyTargetKind.Friend, copied.Target.Kind);
        Assert.Equal(NetherStrategyAbilityEffectKind.ParameterBuff, copied.AbilityEffect.Kind);
        Assert.Equal(
            NetherStrategyBuffCoexistenceKind.HigherValue,
            Assert.Single(copied.BuffStrategies).Coexistence
        );
    }

    [Fact]
    public void Production_trigger_capture_accepts_an_all_known_native_trigger_list()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300: IAbilityEffectData.Situations is the
        // authoritative list of BattleSituationBase instances. BattleSituationStartBattle and
        // BattleSituationDuration.MilliSec are stable typed native members; an all-known list must
        // not be confused with default(NetherStrategyTriggerEvidence), whose Kind is Unknown.
        NetherStrategyTriggerCaptureResult captured =
            NetherStrategyNativeMechanicCaptureMapper.MapTriggers(
                7001,
                [
                    new NetherStrategyTriggerEvidence(NetherStrategyTriggerKind.StartBattle)
                    {
                        ParametersKnown = true,
                        NativeTypeIdentity =
                            "Project.BattleSituations.BattleSituationStartBattle",
                        ControlRelationships = NetherStrategyTriggerControlEvidence.KnownFixed(1000),
                    },
                    new NetherStrategyTriggerEvidence(NetherStrategyTriggerKind.Duration)
                    {
                        Parameter1 = 15000,
                        ParametersKnown = true,
                        NativeTypeIdentity =
                            "Project.BattleSituations.BattleSituationDuration",
                        ControlRelationships = NetherStrategyTriggerControlEvidence.KnownFixed(1000),
                    },
                ]
            );

        Assert.True(captured.IsKnown, captured.UnknownReason);
        Assert.Equal(2, captured.Triggers.Count);
        Assert.Equal(15000, captured.Triggers[1].Parameter1);
    }

    [Fact]
    public void Production_trigger_capture_preserves_the_exact_unknown_native_subtype_error()
    {
        const string exact =
            "unsupported-ability-situation-type:Project.Future.BattleSituationMoonPhase";
        NetherStrategyTriggerCaptureResult captured =
            NetherStrategyNativeMechanicCaptureMapper.MapTriggers(
                7002,
                [
                    KnownTrigger(NetherStrategyTriggerKind.StartBattle),
                    new NetherStrategyTriggerEvidence(NetherStrategyTriggerKind.Unknown)
                    {
                        NativeTypeIdentity = "Project.Future.BattleSituationMoonPhase",
                        UnknownReason = exact,
                    },
                ]
            );

        Assert.False(captured.IsKnown);
        Assert.Equal(exact, captured.UnknownReason);
        Assert.Equal(exact, captured.Triggers[1].UnknownReason);
    }

    [Fact]
    public void Production_effect_capture_preserves_native_threshold_filter_reference_and_charge_values()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300: AbilityEffectErosionLinkedBuff
        // exposes Min/Max Param.PerMille and Param.Effect; BuffParameterByType exposes buffType,
        // BuffTargetFilter and parameterReference; RatePermilleBuffParameterReferenceBase exposes
        // valueType/value/limit. ChargeMana.Energy and SkillCharge.ChargePermille are exact native
        // scalar values. Deliberately distinct literals catch collapsed/name-inferred mappings.
        var requiredBuffs = new[] { new NetherStrategyBuffType(71), new NetherStrategyBuffType(72) };
        var parameter = new NetherStrategyNativeBuffParameterCapture(
            new NetherStrategyBuffType(401),
            new NetherStrategyBuffTargetFilterEvidence(
                IgnoreDeadUnit: true,
                ElementTypeFlags: 3,
                ElementWeakTypeFlags: 5,
                PartyPositionFlags: NetherPartyPositionFlags.Forward,
                UnionTypeFlags: 7,
                JobGroupFlags: 11,
                JobSpeciesFlags: 13,
                CharacterSizeFlags: 17,
                RequiredBuffTypes: requiredBuffs
            ),
            new NetherStrategyBuffParameterReferenceEvidence(
                NetherStrategyBuffParameterReferenceKind.RatePermille,
                "Project.Ingame.AttackUpPermilleBuffParameterReference"
            )
            {
                ValueType = 2,
                Value = 123,
                Limit = 456,
                ValuesKnown = true,
            }
        );
        var erosionCapture = new NetherStrategyNativeAbilityEffectCapture(
            NetherStrategyAbilityEffectKind.ErosionLinkedBuff,
            "Project.AbilityEffect.AbilityEffectErosionLinkedBuff"
        )
        {
            MinLinkedBuff = new NetherStrategyLinkedBuffThresholdCapture(350, parameter),
            MaxLinkedBuff = new NetherStrategyLinkedBuffThresholdCapture(700, parameter),
            ParametersKnown = true,
        };

        NetherStrategyAbilityEffectEvidence erosion =
            NetherStrategyNativeMechanicCaptureMapper.MapAbilityEffect(erosionCapture);
        NetherStrategyAbilityEffectEvidence mana =
            NetherStrategyNativeMechanicCaptureMapper.MapAbilityEffect(
                new NetherStrategyNativeAbilityEffectCapture(
                    NetherStrategyAbilityEffectKind.ChargeMana,
                    "Project.AbilityEffect.AbilityEffectChargeMana"
                )
                {
                    ManaEnergy = 12.5f,
                    ParametersKnown = true,
                }
            );
        NetherStrategyAbilityEffectEvidence skillCharge =
            NetherStrategyNativeMechanicCaptureMapper.MapAbilityEffect(
                new NetherStrategyNativeAbilityEffectCapture(
                    NetherStrategyAbilityEffectKind.SkillCharge,
                    "Project.AbilityEffect.AbilityEffectSkillCharge"
                )
                {
                    SkillChargePermille = 875,
                    ParametersKnown = true,
                }
            );

        Assert.True(erosion.ParametersKnown, erosion.ParameterUnknownReason);
        Assert.Equal(350, erosion.MinLinkedBuff!.PerMille);
        Assert.Equal(700, erosion.MaxLinkedBuff!.PerMille);
        NetherStrategyBuffParameterEvidence mapped = erosion.MinLinkedBuff.BuffParameter;
        Assert.Equal(401, mapped.BuffType.Value);
        Assert.Equal(13, mapped.TargetFilter!.JobSpeciesFlags);
        Assert.Equal(new[] { 71, 72 }, mapped.TargetFilter.RequiredBuffTypes.Select(value => value.Value));
        Assert.Equal(NetherStrategyBuffParameterReferenceKind.RatePermille, mapped.ParameterReference.Kind);
        Assert.Equal(2, mapped.ParameterReference.ValueType);
        Assert.Equal(123, mapped.ParameterReference.Value);
        Assert.Equal(456, mapped.ParameterReference.Limit);
        Assert.Equal(12.5f, mana.ManaEnergy);
        Assert.Equal(875, skillCharge.SkillChargePermille);

        requiredBuffs[0] = new NetherStrategyBuffType(999);
        Assert.Equal(71, mapped.TargetFilter.RequiredBuffTypes[0].Value);
    }

    [Fact]
    public void Production_trigger_capture_preserves_exact_native_base_probability_limit_and_cost_relationships()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300: BattleSituationBase owns
        // _probabilityType/_probabilityPerMille/_levelBasedProbability,
        // _executeCountLimit and _situationCost. GetProbabilityPerMille(level),
        // ExecuteCountLimit.Create(register, level), and SituationCost.SituationCosts consume
        // these exact relationships; subtype-only evidence is incomplete.
        NetherStrategyTriggerEvidence trigger = NetherStrategyNativeMechanicCaptureMapper.MapTrigger(
            new NetherStrategyNativeTriggerCapture(
                NetherStrategyTriggerKind.Duration,
                "Project.BattleSituations.BattleSituationDuration"
            )
            {
                Parameter1 = 4567,
                ParametersKnown = true,
                ProbabilityType = NetherStrategyTriggerProbabilityType.AbilityLevel,
                FixedProbabilityPermille = 111,
                LevelProbabilityPermille = [101, 202, 303, 404, 505, 606, 707, 808, 909, 999],
                ExecuteCountLimit = new NetherStrategyExecuteCountLimitEvidence(
                    NetherStrategyExecuteCountLimitKind.Battle,
                    "Project.BattleSituations.SituationLimits.ExecuteCountLimitBattleParameter",
                    RawValueType: 1,
                    FixedCountLimit: 17,
                    LevelCountLimits: [11, 12, 13, 14, 15, 16, 17, 18, 19, 20]
                ),
                SituationCosts =
                [
                    new NetherStrategySituationCostEvidence(
                        NetherStrategySituationCostKind.BuffStack,
                        "Project.BattleSituations.SituationCosts.SituationCostParameterBuffStack",
                        BuffType: 37,
                        FixedStack: 3,
                        LevelStacks: []
                    ),
                    new NetherStrategySituationCostEvidence(
                        NetherStrategySituationCostKind.BuffStackPerLevel,
                        "Project.BattleSituations.SituationCosts.SituationCostParameterBuffStackPerLevel",
                        BuffType: 0,
                        FixedStack: 0,
                        LevelStacks: [21, 22, 23, 24, 25, 26, 27, 28, 29, 30]
                    )
                    {
                        LevelBuffTypes = [41, 42, 43, 44, 45, 46, 47, 48, 49, 50],
                    },
                ],
                ControlRelationshipsKnown = true,
            }
        );

        Assert.True(trigger.IsKnown, trigger.UnknownReason);
        Assert.Equal(4567, trigger.Parameter1);
        Assert.True(trigger.ControlRelationships.IsKnown, trigger.ControlRelationships.UnknownReason);
        Assert.Equal(NetherStrategyTriggerProbabilityType.AbilityLevel, trigger.ControlRelationships.ProbabilityType);
        Assert.Equal(111, trigger.ControlRelationships.FixedProbabilityPermille);
        Assert.Equal(707, trigger.ControlRelationships.LevelProbabilityPermille[6]);
        Assert.Equal(17, trigger.ControlRelationships.ExecuteCountLimit!.FixedCountLimit);
        Assert.Equal(20, trigger.ControlRelationships.ExecuteCountLimit.LevelCountLimits[9]);
        Assert.Equal(37, trigger.ControlRelationships.SituationCosts[0].BuffType);
        Assert.Equal(50, trigger.ControlRelationships.SituationCosts[1].LevelBuffTypes[9]);
        Assert.Equal(30, trigger.ControlRelationships.SituationCosts[1].LevelStacks[9]);
    }

    [Fact]
    public void Owned_mechanic_assembly_keeps_a_missing_master_row_component_local()
    {
        NetherStrategyNativeMechanic known = KnownMechanic(101);
        IReadOnlyList<NetherStrategyNativeMechanic> mechanics =
            NetherStrategyNativeMechanicAssembler.AssembleOwnedCodes(
                [
                    new NetherCodeState(101, NetherCodeFamily.Impact, 1),
                    new NetherCodeState(202, NetherCodeFamily.Safe, 1),
                ],
                new Dictionary<long, NetherStrategyNativeMechanic> { [101] = known }
            );

        Assert.Equal(2, mechanics.Count);
        Assert.True(mechanics[0].IsKnown);
        Assert.Equal(101, mechanics[0].MechanicId);
        Assert.False(mechanics[1].IsKnown);
        Assert.Equal(202, mechanics[1].MechanicId);
        Assert.Equal("missing-strategy-m-nether-code:202", mechanics[1].UnknownReason);

        NetherSnapshot snapshot = Snapshot();
        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(
                new NetherStrategyEvidenceIdentity(7, 7, 7, snapshot.Fingerprint),
                snapshot
            )
            {
                NativeMechanics = mechanics,
            }
        );
        Assert.True(mapped.IsMapped, mapped.Detail);
        Assert.True(mapped.Package!.NativeMechanics.IsKnown);
        Assert.Equal(2, mapped.Package.NativeMechanics.Value!.Mechanics.Count);
    }

    [Fact]
    public void Mapper_accepts_idless_resource_rows_and_preserves_component_local_unknowns()
    {
        // Fresh Project.dll SHA-256 53806a5b...1300: MNetherFloorEventParts.content_type
        // 160 (code offer), 165 (NetherGold), and 166 (treasure key) may carry content_id=0.
        NetherSnapshot snapshot = Snapshot();
        var research = new[]
        {
            ProjectedUnknown(NetherCodeFamily.Rush, 12, 2),
            ProjectedUnknown(NetherCodeFamily.Impact, 8, 0),
            ProjectedUnknown(NetherCodeFamily.Safe, 3, 0),
            ProjectedUnknown(NetherCodeFamily.Risk, 2, 0),
        };
        var mechanics = new[]
        {
            new NetherStrategyNativeMechanic(
                7101,
                NetherCodeMasterEffectType.NetherAbility,
                [new NetherStrategyTriggerEvidence(NetherStrategyTriggerKind.Unknown)
                {
                    UnknownReason = "ability-effect-asset-unavailable:7101",
                }],
                new NetherStrategyTargetEvidence(NetherStrategyTargetKind.Unknown)
                {
                    UnknownReason = "ability-effect-asset-unavailable:7101",
                }
            )
            {
                IsKnown = false,
                UnknownReason = "ability-effect-asset-unavailable:7101",
            },
        };
        var resources = new[]
        {
            new NetherStrategyVisibleContentRow(
                NetherStrategyVisibleContentKind.Resource,
                9001,
                6002,
                0
            )
            {
                Amount = 1,
                RawValues = new[] { new NetherStrategyNamedValue("ContentType", 160) },
            },
        };

        NetherStrategyEvidenceMapResult mapped = NetherStrategyEvidenceMapper.Map(
            new NetherStrategyEvidenceMapRequest(Identity(snapshot), snapshot)
            {
                Research = research,
                NativeMechanics = mechanics,
                VisibleMap = new NetherStrategyVisibleMapEvidence(snapshot.Floors, resources),
            }
        );

        Assert.True(mapped.IsMapped, mapped.Detail);
        Assert.True(mapped.Package!.Research.IsKnown);
        Assert.False(mapped.Package.Research.Value!.Families[0].IsProjectedNormalSettlementKnown);
        Assert.True(mapped.Package.NativeMechanics.IsKnown);
        Assert.False(mapped.Package.NativeMechanics.Value!.Mechanics[0].IsKnown);
        Assert.True(mapped.Package.VisibleMap.IsKnown, mapped.Package.VisibleMap.UnknownReason);
        NetherStrategyVisibleContentRow resource = Assert.Single(mapped.Package.VisibleMap.Value!.ContentRows);
        Assert.Equal(NetherStrategyVisibleContentKind.Resource, resource.Kind);
        Assert.Equal(0, resource.ContentId);
    }

    private static NetherCodeState Code(long id, NetherCodeFamily family, int amount) =>
        new(id, family, 1)
        {
            Category = (NetherCodeCategory)family,
            PossessionAmount = amount,
            MasterEffectType = NetherCodeMasterEffectType.NetherAbility,
            AbilityAssetId = id + 1000,
            EffectParameter1 = id + 1000,
            EffectParameter2 = 1,
        };

    private static NetherStrategyNativeMechanic KnownMechanic(long id) => new(
        id,
        NetherCodeMasterEffectType.NetherAbility,
        [new NetherStrategyTriggerEvidence(NetherStrategyTriggerKind.StartBattle)
        {
            ParametersKnown = true,
            NativeTypeIdentity = "Project.BattleSituations.BattleSituationStartBattle",
            ControlRelationships = NetherStrategyTriggerControlEvidence.KnownFixed(1000),
        }],
        new NetherStrategyTargetEvidence(NetherStrategyTargetKind.Friend)
        {
            ParametersKnown = true,
            NativeTypeIdentity = "Project.AbilityTargets.AbilityTargetAllAllies",
        }
    )
    {
        AbilityEffect = new NetherStrategyAbilityEffectEvidence(
            NetherStrategyAbilityEffectKind.Template
        )
        {
            NativeTypeIdentity = "Project.AbilityEffect.AbilityEffectTemplate",
        },
    };

    private static NetherStrategyTriggerEvidence KnownTrigger(
        NetherStrategyTriggerKind kind
    ) => new(kind)
    {
        ParametersKnown = true,
        NativeTypeIdentity = "Project.BattleSituations." + kind,
        ControlRelationships = NetherStrategyTriggerControlEvidence.KnownFixed(1000),
    };

    private static NetherStrategyResearchFamilyState ProjectedUnknown(
        NetherCodeFamily family,
        int wallet,
        int acquiredCount
    ) => new(family, wallet, 0, 15)
    {
        SettlementAcquiredCodeCount = acquiredCount,
        IsProjectedNormalSettlementKnown = false,
        ProjectionUnknownReason = "normal-result-nether-code-points-not-server-authoritative-before-settlement",
    };

    private static NetherSnapshot Snapshot() => new()
    {
        Status = NetherSessionStatus.Play,
        NetherId = 101,
        MapId = 202,
        CurrentFloorId = 5001,
        CurrentNodeId = 9001,
        FloorLevel = 20,
        FloorIndex = 0,
        MasterMaxFloorLevel = 130,
        RecoveryFloorLevel = 80,
        AuthoritativeBossFloorLevels = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 },
        ErosionPoint = 25,
        TicketCount = 3,
        TreasureKeyCount = 1,
        NetherGold = 250,
        CodeReloadCount = 2,
        CodeCapacity = 25,
        Floors = new[]
        {
            new NetherFloorNode(5001, 20, 0, NetherFloorNodeType.Battle)
            {
                NodeId = 9001,
                ApiFloorIndex = 0,
                IsUnlocked = true,
            },
        },
        CharacterHpHash = "party",
        CodeHash = "codes",
        MapHash = "map",
    };

    private static NetherStrategyEvidenceIdentity Identity(NetherSnapshot snapshot) =>
        new(8, 8, 8, snapshot.Fingerprint);
}
