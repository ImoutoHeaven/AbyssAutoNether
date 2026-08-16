using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherPopupDispatchPolicyTests
{
    [Fact]
    public void Code_offer_is_dispatched_to_code_flow_not_the_owned_code_list()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext { Kind = NetherRuntimePopupKind.CodeOffer },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.Code, decision.Kind);
    }

    [Fact]
    public void Raw_floor_event_type_four_remains_event_and_selects_its_safe_option()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Event,
                RawFloorType = 4,
                TargetCharacterId = 101,
                Options = [new NetherEventOption(1, [new NetherEffect(NetherEffectKind.Item, 1)])],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.Action.Kind);
        Assert.Equal(1, decision.Action.OptionNumber);
        Assert.Equal(101, decision.Action.TargetCharacterId);
        Assert.Single(decision.Action.ExpectedEffects);
        Assert.Equal(NetherEffectKind.Item, decision.Action.ExpectedEffects[0].Kind);
    }

    [Fact]
    public void Recovery_and_treasure_use_their_distinct_policies()
    {
        NetherPopupDispatchDecision recovery = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options = [new NetherEventOption(1, [new NetherEffect(NetherEffectKind.NetherGoldUsed, 0)])],
            },
            Settings()
        );
        NetherPopupDispatchDecision treasure = NetherPopupDispatchPolicy.Decide(
            Snapshot(keys: 1),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Treasure,
                Options = [new NetherEventOption(1, [new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1), new NetherEffect(NetherEffectKind.Item, 1)])],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, recovery.Kind);
        Assert.Equal(NetherPopupDispatchKind.NativeAction, treasure.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, recovery.Action.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, treasure.Action.Kind);
    }

    [Fact]
    public void Recovery_projection_applies_active_category_erosion_relief()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options = [new NetherEventOption(2, [new NetherEffect(NetherEffectKind.Heal, 300)])],
            },
            Settings(),
            new NetherActiveCodeErosionProjection
            {
                ErosionProjectionKnown = true,
                CodeHash = "nether-codes:safe-category-threshold",
                ErosionEffects =
                [
                    new NetherCodeEffect(30000, NetherCodeEffectKind.ErosionAdditionDown, 5),
                ],
            }
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.True(decision.HasEffectProjection);
        Assert.Equal(15, decision.ProjectedErosion);
        Assert.True(decision.Action.HasExpectedErosionDelta);
        Assert.Equal(-5, decision.Action.ExpectedErosionDelta);
    }

    [Fact]
    public void Recovery_transform_real_popup_flow_requires_zero_value_rest_and_purification_then_commits_hard_excluded_code()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly.dll 573fa800...e1fb:
        // NetherRecoveryFloorEventFlow opens NetherRecoverPopupController from floor type 5;
        // InitializeView resolves exactly three MNetherFloorEvents option parts and target_type=7
        // opens the separate AbyssCodeListPopupType.Change flow. The transform removal therefore
        // has to be committed while the exact Recovery options are still visible.
        NetherSnapshot snapshot = Snapshot() with
        {
            ErosionPoint = 0,
            CodeCapacity = 25,
            Codes =
            [
                new NetherCodeState(9001, NetherCodeFamily.Risk, 1)
                {
                    Category = NetherCodeCategory.Risk,
                    Rarity = 3,
                    Power = 999_999,
                },
            ],
        };
        NetherAutoClimbSettings settings = Settings() with
        {
            StrategyMode = NetherStrategyMode.Equipment,
            EquipmentRecoveryCodeTransformEnabled = true,
        };
        var hardEvidence = new NetherCodeTransformHardExclusionEvidence
        {
            IsKnown = true,
            HardExcludedCodes =
            [
                new NetherCodeTransformHardExclusion(
                    9001,
                    NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment
                ),
            ],
        };

        NetherPopupDispatchDecision recovery = NetherPopupDispatchPolicy.Decide(
            snapshot,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options =
                [
                    new NetherEventOption(1, [new NetherEffect(NetherEffectKind.Heal, 300)]),
                    new NetherEventOption(2, [new NetherEffect(NetherEffectKind.ErosionHeal, 30)]),
                    new NetherEventOption(3, [new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0)]),
                ],
            },
            settings,
            NoActiveErosion(),
            hardEvidence
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, recovery.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, recovery.Action.Kind);
        Assert.Equal(3, recovery.Action.OptionNumber);
        Assert.Equal(9001, recovery.Action.CodeId);

        NetherPopupDispatchDecision transform = NetherPopupDispatchPolicy.Decide(
            snapshot,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.CodeTransform,
                CodeTransformCommitment = new NetherCodeTransformCommitment(9001),
            },
            settings,
            NoActiveErosion(),
            hardEvidence
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, transform.Kind);
        Assert.Equal(NetherActionKind.TransformCode, transform.Action.Kind);
        Assert.Equal(9001, transform.Action.ReplaceCodeId);
    }

    [Theory]
    [InlineData((int)NetherStrategyMode.Research, true, 0, 1000)]
    [InlineData((int)NetherStrategyMode.Equipment, false, 0, 1000)]
    [InlineData((int)NetherStrategyMode.Equipment, true, 20, 1000)]
    [InlineData((int)NetherStrategyMode.Equipment, true, 0, 700)]
    public void Recovery_transform_real_popup_flow_rejects_mode_opt_in_and_nonzero_deterministic_value(
        int rawMode,
        bool optIn,
        int erosion,
        int hp
    )
    {
        NetherSnapshot snapshot = Snapshot() with
        {
            ErosionPoint = erosion,
            CodeCapacity = 25,
            Characters = [new NetherCharacterState(1, hp)],
            Codes = [new NetherCodeState(9001, NetherCodeFamily.Risk, 1)],
        };
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            snapshot,
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Recovery,
                Options =
                [
                    new NetherEventOption(1, [new NetherEffect(NetherEffectKind.Heal, 300)]),
                    new NetherEventOption(2, [new NetherEffect(NetherEffectKind.ErosionHeal, 30)]),
                    new NetherEventOption(3, [new NetherEffect(NetherEffectKind.AbyssCodeTransform, 0)]),
                ],
            },
            Settings() with
            {
                StrategyMode = (NetherStrategyMode)rawMode,
                EquipmentRecoveryCodeTransformEnabled = optIn,
            },
            NoActiveErosion(),
            new NetherCodeTransformHardExclusionEvidence
            {
                IsKnown = true,
                HardExcludedCodes =
                [
                    new NetherCodeTransformHardExclusion(
                        9001,
                        NetherCodeTransformHardExclusionReason.AdverseErosionAdjustment
                    ),
                ],
            }
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.Action.Kind);
        Assert.NotEqual(3, decision.Action.OptionNumber);
        Assert.Equal(0, decision.Action.CodeId);
    }

    [Fact]
    public void Shop_off_leaves_through_native_close_callback()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(),
            new NetherRuntimePopupContext { Kind = NetherRuntimePopupKind.Shop, ShopContents = [] },
            Settings(shop: NetherShopMode.Off)
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.LeaveShop, decision.Action.Kind);
    }

    private static NetherSnapshot Snapshot(int keys = 0) => new()
    {
        ErosionPoint = 20,
        NetherGold = 100,
        TreasureKeyCount = keys,
        Characters = [new NetherCharacterState(1, 1000)],
    };

    private static NetherAutoClimbSettings Settings(NetherShopMode shop = NetherShopMode.Off) => new()
    {
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = 300,
        TreasureMode = NetherTreasureMode.KeyOnly,
        ShopMode = shop,
    };

    private static NetherActiveCodeErosionProjection NoActiveErosion() => new()
    {
        ErosionProjectionKnown = true,
        CodeHash = "nether-codes:none",
        ErosionEffects = [],
    };
}
