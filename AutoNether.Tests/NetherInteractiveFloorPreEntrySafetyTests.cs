using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherInteractiveFloorPreEntrySafetyTests
{
    [Fact]
    public void Zero_extend_id_uses_the_first_floor_event_row_like_the_native_resolver()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events:
            [
                Event(100, 1001) with { Weight = 0 },
                Event(101, 1002),
            ],
            parts:
            [
                Part(1001, targetType1: (int)NetherEffectKind.Heal, parameter1: 1),
                Part(1002, targetType1: (int)NetherEffectKind.Damage, parameter1: 500),
            ]
        ));

        Assert.True(result.IsSafe);
        Assert.Single(result.SafeOptionNumberByEventId);
        Assert.Equal(1, result.SafeOptionNumberByEventId[100]);
    }

    [Fact]
    public void Positive_extend_id_uses_the_exact_event_row_without_generation_filters()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events:
            [
                Event(101, 1002),
                Event(100, 1001) with { MapFloorMasterId = 901, Weight = 0 },
            ],
            parts:
            [
                Part(1001, targetType1: (int)NetherEffectKind.Heal, parameter1: 1),
                Part(1002, targetType1: (int)NetherEffectKind.Damage, parameter1: 500),
            ],
            hp: 500,
            floorExtendId: 100
        ));

        Assert.True(result.IsSafe, result.PauseReason + ":" + result.Detail);
        Assert.Single(result.SafeOptionNumberByEventId);
        Assert.Equal(1, result.SafeOptionNumberByEventId[100]);
    }

    [Fact]
    public void Recovery_accepts_a_completely_neutral_master_option_as_the_safe_exit()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Recovery,
            events: [Event(100, 1001)],
            parts: [Part(1001, targetType1: (int)NetherEffectKind.NetherGoldUsed, parameter1: 0)]
        ));

        Assert.True(result.IsSafe);
        Assert.Equal(1, result.SafeOptionNumberByEventId[100]);
    }

    [Fact]
    public void Recovery_all_safe_tie_preserves_known_safe_loser_through_pre_entry_projection()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Recovery,
            events: [Event(100, 1001, 1002, 1003)],
            parts:
            [
                Part(1001, targetType1: (int)NetherEffectKind.Heal, parameter1: 100),
                Part(1002, targetType1: (int)NetherEffectKind.ErosionHeal, parameter1: 10),
                Part(1003, targetType1: 7, parameter1: 0),
            ],
            erosion: 0,
            hp: 1000,
            recoveryProofs: CompleteRecoveryProofs(1001, 1002, 1003),
            requireCompleteRecoveryBranchSafety: true
        ));

        Assert.True(result.IsSafe, result.PauseReason + ":" + result.Detail);
        Assert.Equal(1, result.SafeOptionNumberByEventId[100]);
        NetherInteractiveOptionAudit selected = Assert.Single(
            result.OptionAudits,
            audit => audit.Key.EventPartId == 1001
        );
        Assert.True(selected.IsKnown);
        Assert.True(selected.IsSelected);
        Assert.Equal(NetherInteractiveOptionHardGate.None, selected.FirstFailingHardGate);
        Assert.Equal(NetherInteractiveOptionSelectionTier.Recovery, selected.SelectionTier);
        Assert.Equal(NetherStrategyUnknownReasonCode.None, selected.UnknownReasonCode);
        Assert.Equal("selected-by-complete-branch-proof", selected.ComparisonRationale);

        NetherInteractiveOptionAudit loser = Assert.Single(
            result.OptionAudits,
            audit => audit.Key.EventPartId == 1002
        );
        Assert.True(loser.IsKnown);
        Assert.False(loser.IsSelected);
        Assert.Equal(NetherInteractiveOptionHardGate.None, loser.FirstFailingHardGate);
        Assert.Equal(NetherInteractiveOptionSelectionTier.Recovery, loser.SelectionTier);
        Assert.Equal(NetherStrategyUnknownReasonCode.None, loser.UnknownReasonCode);
        Assert.Equal(
            "eligible-safe-but-not-selected-by-deterministic-recovery-tie-break",
            loser.ComparisonRationale
        );

        NetherInteractiveOptionAudit transform = Assert.Single(
            result.OptionAudits,
            audit => audit.Key.EventPartId == 1003
        );
        Assert.True(transform.IsKnown);
        Assert.False(transform.IsSelected);
        Assert.Equal(
            NetherInteractiveOptionHardGate.RecoveryTransformPolicy,
            transform.FirstFailingHardGate
        );
        Assert.Equal(NetherInteractiveOptionSelectionTier.None, transform.SelectionTier);
        Assert.Equal(NetherStrategyUnknownReasonCode.None, transform.UnknownReasonCode);
        Assert.Equal("deterministic-recovery-choice-has-value", transform.Detail);
        Assert.Equal(
            "excluded:recovery-transform-policy=deterministic-recovery-choice-has-value",
            transform.ComparisonRationale
        );
    }

    [Fact]
    public void Map_generation_erosion_range_is_not_an_interactive_action_cost()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Recovery,
            events: [Event(100, 1001)],
            parts: [Part(1001, targetType1: (int)NetherEffectKind.NetherGoldUsed, parameter1: 0)],
            erosion: 0,
            mapMinimumErosion: 0,
            mapMaximumErosion: 100
        ));

        Assert.True(result.IsSafe, result.PauseReason + ":" + result.Detail);
        Assert.Equal(0, result.WorstCaseProjection!.Value.ErosionDelta);
    }

    [Fact]
    public void Ordinary_event_hp_cost_is_safe_while_every_living_character_remains_above_zero()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts: [Part(1001, targetType1: (int)NetherEffectKind.Damage, parameter1: 201)],
            hp: 500
        ));

        Assert.True(result.IsSafe, result.PauseReason + ":" + result.Detail);
        Assert.Equal(-201, result.WorstCaseProjection!.Value.HpDelta);
    }

    [Fact]
    public void Ordinary_event_hp_cost_is_rejected_when_any_living_character_reaches_zero()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts: [Part(1001, targetType1: (int)NetherEffectKind.Damage, parameter1: 500)],
            hp: 500
        ));

        Assert.False(result.IsSafe);
        Assert.Equal(NetherPauseReason.UnsafeHp, result.PauseReason);
    }

    [Fact]
    public void Erosion_at_the_soft_limit_is_not_a_safe_exit()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts: [Part(1001, targetType1: (int)NetherEffectKind.Erosion, parameter1: 70)],
            erosion: 20
        ));

        Assert.False(result.IsSafe);
        Assert.Equal(NetherPauseReason.UnsafeErosion, result.PauseReason);
    }

    [Fact]
    public void Battle_trigger_option_requires_a_nonbattle_safe_fallback_in_the_same_possible_row()
    {
        NetherInteractiveFloorPreEntrySafetyResult onlyBattle = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts: [Part(1001, targetType1: (int)NetherEffectKind.Battle, parameter1: 0)]
        ));
        NetherInteractiveFloorPreEntrySafetyResult fallback = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001, 1002)],
            parts:
            [
                Part(1001, targetType1: (int)NetherEffectKind.Battle, parameter1: 0),
                Part(1002, targetType1: (int)NetherEffectKind.NetherGoldUsed, parameter1: 0),
            ]
        ));

        Assert.False(onlyBattle.IsSafe);
        Assert.Equal(NetherPauseReason.NoSafeRoute, onlyBattle.PauseReason);
        Assert.True(fallback.IsSafe);
        Assert.Equal(2, fallback.SafeOptionNumberByEventId[100]);
    }

    [Theory]
    [InlineData(160, (int)NetherEffectKind.AbyssCodeOffer)]
    [InlineData(165, (int)NetherEffectKind.NetherGoldGain)]
    [InlineData(166, (int)NetherEffectKind.TreasureKeyGain)]
    public void Native_resource_content_allows_zero_content_id(int contentType, int expectedKind)
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts:
            [
                Part(
                    1001,
                    targetType1: 0,
                    parameter1: 0,
                    contentType: contentType,
                    contentId: 0,
                    amount: 30
                ),
            ]
        ));

        Assert.True(result.IsSafe, result.PauseReason + ":" + result.Detail);
        Assert.Equal(1, result.SafeOptionNumberByEventId[100]);
        NetherEffect effect = Assert.Single(result.SafeOptionProjectionByEventId[100].ExpectedEffects);
        Assert.Equal((NetherEffectKind)expectedKind, effect.Kind);
        Assert.Equal(0, effect.ContentId);
        Assert.Equal(30, effect.Amount);
    }

    [Theory]
    [InlineData(165)]
    [InlineData(166)]
    public void Negative_resource_content_id_is_unknown_before_floor_entry(int contentType)
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts:
            [
                Part(
                    1001,
                    targetType1: 0,
                    parameter1: 0,
                    contentType: contentType,
                    contentId: -1,
                    amount: 1
                ),
            ]
        ));

        Assert.False(result.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, result.PauseReason);
        Assert.Contains("unsupported-event-content-type:" + contentType, result.Detail);
    }

    [Fact]
    public void Out_of_domain_item_rarity_is_unknown_before_floor_entry()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(
            Input(
                NetherFloorNodeType.Event,
                events: [Event(100, 1001)],
                parts: [Part(1001, targetType1: 0, parameter1: 0, contentType: 30, contentId: 701, amount: 1)]
            ) with
            {
                ItemRows = [new NetherStrategyItemMasterRow(701, 91, 999, 1, 99)],
            }
        );

        Assert.False(result.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, result.PauseReason);
        Assert.Contains("event-item-master-row-unavailable", result.Detail);
    }

    [Fact]
    public void Nonzero_code_offer_content_id_remains_unknown_instead_of_using_a_runtime_only_identity()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts: [Part(
                1001,
                targetType1: 0,
                parameter1: 0,
                contentType: 160,
                contentId: 999,
                amount: 1
            )]
        ));

        Assert.False(result.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, result.PauseReason);
        Assert.Contains("unsupported-event-content-type:160", result.Detail);
    }

    [Fact]
    public void Nonzero_target_type_seven_parameter_is_unknown_and_does_not_open_code_transform_flow()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts: [Part(1001, targetType1: 7, parameter1: 999)]
        ));

        Assert.False(result.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, result.PauseReason);
        Assert.Contains("unsupported-event-target", result.Detail);
    }

    [Fact]
    public void Native_code_transform_requires_prevalidated_opt_in_while_code_offer_remains_exact()
    {
        NetherInteractiveFloorPreEntrySafetyResult transform = Evaluate(Input(
            NetherFloorNodeType.Recovery,
            events: [Event(354, 700)],
            parts: [Part(700, targetType1: 7, parameter1: 0)],
            codes: [Code(40024, NetherCodeFamily.Risk)]
        ));
        NetherInteractiveFloorPreEntrySafetyResult offer = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(355, 701)],
            parts: [Part(701, targetType1: 0, parameter1: 0, contentType: 160, contentId: 0, amount: 1)]
        ));

        Assert.False(transform.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, transform.PauseReason);
        Assert.Contains("code-transform-hard-exclusions-not-captured", transform.Detail);

        Assert.True(offer.IsSafe, offer.PauseReason + ":" + offer.Detail);
        NetherEffect offerEffect = Assert.Single(offer.SafeOptionProjectionByEventId[355].ExpectedEffects);
        Assert.Equal(NetherEffectKind.AbyssCodeOffer, offerEffect.Kind);
        Assert.Equal(0, offerEffect.ContentId);
    }

    [Fact]
    public void Transform_option_without_any_current_code_fails_before_floor_click()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts: [Part(1001, targetType1: 7, parameter1: 0)],
            codes: []
        ));

        Assert.False(result.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, result.PauseReason);
        Assert.Contains("invalid-code-transform-portfolio", result.Detail);
    }

    [Fact]
    public void Three_targets_plus_native_content_are_all_retained()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts:
            [
                Part(
                    1001,
                    targetType1: (int)NetherEffectKind.Heal,
                    parameter1: 10,
                    targetType2: (int)NetherEffectKind.ErosionHeal,
                    parameter2: 5,
                    targetType3: (int)NetherEffectKind.NetherGoldUsed,
                    parameter3: 0,
                    contentType: 160,
                    contentId: 0,
                    amount: 1
                ),
            ]
        ));

        Assert.True(result.IsSafe, result.PauseReason + ":" + result.Detail);
        Assert.Equal(4, result.SafeOptionProjectionByEventId[100].ExpectedEffects.Count);
    }

    [Fact]
    public void Missing_sibling_part_is_option_local_when_a_known_option_is_safe()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001, 9999)],
            parts: [Part(1001, targetType1: (int)NetherEffectKind.Heal, parameter1: 1)]
        ));

        Assert.True(result.IsSafe, result.PauseReason + ":" + result.Detail);
        Assert.Equal(1, result.SafeOptionNumberByEventId[100]);
        Assert.Contains(
            result.OptionProjectionByKey,
            entry => entry.Key.EventPartId == 9999 && !entry.Value.IsKnown
        );
    }

    [Fact]
    public void Malformed_event_part_is_option_local_when_a_sibling_option_is_exactly_safe()
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001, 1002)],
            parts:
            [
                Part(1001, targetType1: (int)NetherEffectKind.Heal, parameter1: 1),
                Part(1002, targetType1: 99, parameter1: 0),
            ]
        ));

        Assert.True(result.IsSafe, result.PauseReason + ":" + result.Detail);
        Assert.Equal(1, result.SafeOptionNumberByEventId[100]);
        Assert.Contains(
            result.OptionProjectionByKey,
            entry => entry.Key.EventId == 100
                && entry.Key.EventPartId == 1001
                && entry.Value.IsKnown
        );
        Assert.Contains(
            result.OptionProjectionByKey,
            entry => entry.Key.EventId == 100
                && entry.Key.EventPartId == 1002
                && !entry.Value.IsKnown
        );
    }

    [Fact]
    public void Shop_off_requires_an_observable_close_and_safe_exact_floor_bounds()
    {
        NetherInteractiveFloorPreEntrySafetyResult canClose = Evaluate(Input(
            NetherFloorNodeType.Shop,
            canCloseShop: true
        ));
        NetherInteractiveFloorPreEntrySafetyResult cannotClose = Evaluate(Input(
            NetherFloorNodeType.Shop,
            canCloseShop: false
        ));

        Assert.True(canClose.IsSafe);
        Assert.False(cannotClose.IsSafe);
        Assert.Equal(NetherPauseReason.BindingUnavailable, cannotClose.PauseReason);
    }

    [Fact]
    public void Treasure_prefers_the_exact_key_option_and_rejects_unproved_hp_fallback()
    {
        NetherInteractiveFloorPreEntrySafetyResult key = Evaluate(Input(
            NetherFloorNodeType.Treasure,
            events: [Event(100, 1001, 1002)],
            parts:
            [
                Part(1001, targetType1: (int)NetherEffectKind.TreasureKeyUsed, parameter1: 1),
                Part(1002, targetType1: (int)NetherEffectKind.Damage, parameter1: 80),
            ],
            keys: 1
        ));
        NetherInteractiveFloorPreEntrySafetyResult noKey = Evaluate(Input(
            NetherFloorNodeType.Treasure,
            events: [Event(100, 1001, 1002)],
            parts:
            [
                Part(1001, targetType1: (int)NetherEffectKind.TreasureKeyUsed, parameter1: 1),
                Part(1002, targetType1: (int)NetherEffectKind.Damage, parameter1: 200),
            ],
            keys: 0
        ));

        Assert.True(key.IsSafe);
        Assert.Equal(1, key.SafeOptionNumberByEventId[100]);
        Assert.False(noKey.IsSafe);
        Assert.Equal(NetherPauseReason.NoSafeRoute, noKey.PauseReason);
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(0, true)]
    public void Treasure_hp_fallback_requires_prevalidated_rank5_or_only_terminal_route(
        int exactTreasureRank,
        bool onlyTerminalRoute
    )
    {
        NetherInteractiveFloorPreEntrySafetyResult result = Evaluate(Input(
            NetherFloorNodeType.Treasure,
            events: [Event(100, 1001, 1002)],
            parts:
            [
                Part(1001, targetType1: (int)NetherEffectKind.TreasureKeyUsed, parameter1: 1),
                Part(1002, targetType1: (int)NetherEffectKind.Damage, parameter1: 80),
            ],
            keys: 0,
            eligibility:
            [
                TreasureEligibility(100, 1002) with
                {
                    ExactTreasureRank = exactTreasureRank,
                    IsOnlyTerminalReachingRoute = onlyTerminalRoute,
                },
            ]
        ));

        Assert.True(result.IsSafe, result.PauseReason + ":" + result.Detail);
        Assert.Equal(2, result.SafeOptionNumberByEventId[100]);
        Assert.True(result.SafeOptionProjectionByEventId[100].AllowsPartialActiveDeaths);
    }

    [Fact]
    public void Missing_or_duplicate_master_data_is_never_a_safe_interactive_exit()
    {
        NetherInteractiveFloorPreEntrySafetyResult missingPart = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 9999)],
            parts: []
        ));
        NetherInteractiveFloorPreEntrySafetyResult duplicatePart = Evaluate(Input(
            NetherFloorNodeType.Event,
            events: [Event(100, 1001)],
            parts:
            [
                Part(1001, targetType1: (int)NetherEffectKind.Heal, parameter1: 1),
                Part(1001, targetType1: (int)NetherEffectKind.Heal, parameter1: 1),
            ]
        ));

        Assert.False(missingPart.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, missingPart.PauseReason);
        Assert.False(duplicatePart.IsSafe);
        Assert.Equal(NetherPauseReason.UnknownMasterData, duplicatePart.PauseReason);
    }

    private static NetherInteractiveFloorPreEntrySafetyResult Evaluate(NetherInteractiveFloorPreEntrySafetyInput input) =>
        new NetherInteractiveFloorPreEntrySafety().Evaluate(input);

    private static NetherInteractiveFloorPreEntrySafetyInput Input(
        NetherFloorNodeType kind,
        IReadOnlyList<NetherFloorEventMasterRow>? events = null,
        IReadOnlyList<NetherFloorEventPartMasterRow>? parts = null,
        int erosion = 20,
        int hp = 500,
        int gold = 100,
        int keys = 1,
        bool canCloseShop = true,
        int mapMinimumErosion = 0,
        int mapMaximumErosion = 10,
        long floorExtendId = 0,
        IReadOnlyList<NetherCodeState>? codes = null,
        IReadOnlyList<NetherInteractivePartialDeathEligibility>? eligibility = null,
        IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence>? recoveryProofs = null,
        bool requireCompleteRecoveryBranchSafety = false
    ) => new(
        FloorKind: kind,
        FloorMasterId: 900,
        MapFloorRows: [new NetherFloorMasterBoundsRow(900, mapMinimumErosion, mapMaximumErosion)],
        EventRows: events ?? [],
        EventPartRows: parts ?? [],
        CurrentErosion: erosion,
        ActiveHpPermille: [hp],
        CurrentNetherGold: gold,
        CurrentTreasureKeys: keys,
        Settings: new NetherAutoClimbSettings
        {
            SoftErosionLimit = 90,
            MinimumCharacterHpPermille = 300,
            ShopMode = NetherShopMode.Off,
            TreasureMode = NetherTreasureMode.KeyOnly,
        }
    )
    {
        CanCloseShop = canCloseShop,
        FloorExtendId = floorExtendId,
        CurrentCodes = codes ?? [Code(40024, NetherCodeFamily.Risk)],
        CodeCapacity = 5,
        PartialDeathEligibility = eligibility ?? [],
        RecoveryBranchSafetyByPartId = recoveryProofs
            ?? new Dictionary<long, NetherRecoveryBranchSafetyEvidence>(),
        RequireCompleteRecoveryBranchSafety = requireCompleteRecoveryBranchSafety,
    };

    private static IReadOnlyDictionary<long, NetherRecoveryBranchSafetyEvidence> CompleteRecoveryProofs(
        long restPartId,
        long purificationPartId,
        long transformPartId
    )
    {
        NetherCodeTransformEligibilityEvidence transformEligibility = new()
        {
            IsKnown = true,
            EquipmentOptInEnabled = true,
            IsRecovery = true,
            DeterministicRecoveryChoicesHaveZeroValue = false,
            HardExcludedCodes = [],
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

    private static NetherInteractivePartialDeathEligibility TreasureEligibility(long eventId, long partId) => new(
        NetherInteractivePartialDeathObjectiveKind.TreasureHpPayment,
        eventId,
        partId,
        ObjectiveNodeId: 999
    )
    {
        IsKnown = true,
        ObjectiveReachable = true,
    };

    private static NetherCodeState Code(long id, NetherCodeFamily family) => new(id, family, 1)
    {
        IsKnown = true,
        Category = family switch
        {
            NetherCodeFamily.Rush => NetherCodeCategory.Rush,
            NetherCodeFamily.Impact => NetherCodeCategory.Impact,
            NetherCodeFamily.Safe => NetherCodeCategory.Safe,
            NetherCodeFamily.Risk => NetherCodeCategory.Risk,
            _ => NetherCodeCategory.Unknown,
        },
        Rarity = 1,
        Power = 0,
        PossessionAmount = 1,
        MasterEffectType = NetherCodeMasterEffectType.NetherAbility,
        AbilityAssetId = 100006,
        EffectParameter1 = 100006,
        EffectParameter2 = 1,
    };

    private static NetherFloorEventMasterRow Event(long eventId, params long[] partIds) => new(
        EventId: eventId,
        MapFloorMasterId: 900,
        Weight: 1,
        PartId1: partIds.ElementAtOrDefault(0),
        PartId2: partIds.ElementAtOrDefault(1),
        PartId3: partIds.ElementAtOrDefault(2),
        PartId4: partIds.ElementAtOrDefault(3)
    );

    private static NetherFloorEventPartMasterRow Part(
        long partId,
        int targetType1,
        long parameter1,
        int targetType2 = 0,
        long parameter2 = 0,
        int targetType3 = 0,
        long parameter3 = 0,
        int contentType = 0,
        long contentId = 0,
        long amount = 0
    ) => new(
        PartId: partId,
        TargetType1: targetType1,
        SelectParameter1: parameter1,
        TargetType2: targetType2,
        SelectParameter2: parameter2,
        TargetType3: targetType3,
        SelectParameter3: parameter3,
        ContentType: contentType,
        ContentId: contentId,
        Amount: amount
    );
}
