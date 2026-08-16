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
    public void Treasure_popup_dispatches_verified_hp_payment_when_no_key_is_available()
    {
        NetherPopupDispatchDecision decision = NetherPopupDispatchPolicy.Decide(
            Snapshot(keys: 0, hp: 704),
            new NetherRuntimePopupContext
            {
                Kind = NetherRuntimePopupKind.Treasure,
                Options =
                [
                    new NetherEventOption(1, [new NetherEffect(NetherEffectKind.TreasureKeyUsed, 1)]),
                    new NetherEventOption(2,
                    [
                        new NetherEffect(NetherEffectKind.Damage, 300),
                        new NetherEffect(NetherEffectKind.ErosionHeal, 30),
                    ]),
                ],
            },
            Settings()
        );

        Assert.Equal(NetherPopupDispatchKind.NativeAction, decision.Kind);
        Assert.Equal(NetherActionKind.SelectEventOption, decision.Action.Kind);
        Assert.Equal(2, decision.Action.OptionNumber);
        Assert.Contains(decision.Action.ExpectedEffects, effect => effect.Kind == NetherEffectKind.Damage && effect.Amount == 300);
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

    private static NetherSnapshot Snapshot(int keys = 0, int hp = 1000) => new()
    {
        ErosionPoint = 20,
        NetherGold = 100,
        TreasureKeyCount = keys,
        Characters = [new NetherCharacterState(1, hp)],
    };

    private static NetherAutoClimbSettings Settings(NetherShopMode shop = NetherShopMode.Off) => new()
    {
        SoftErosionLimit = 90,
        MinimumCharacterHpPermille = 300,
        TreasureMode = NetherTreasureMode.KeyOnly,
        ShopMode = shop,
    };
}
