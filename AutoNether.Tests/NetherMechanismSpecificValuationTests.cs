using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherMechanismSpecificValuationTests
{
    [Fact]
    public void Crest_payoff_requires_explicit_provider_and_consumer_paths_not_mana_type_alone()
    {
        // Fresh Project.dll 53806a5b...1300 / GameAssembly 573fa800...c1fb:
        // ManaType identifies Passion/Impact, while no inspected ManaType member grants a crest or
        // proves cadence. A payoff is reachable only through separately captured provider and
        // consumer relationships for the actual AbilityTarget recipients.
        NetherCrestPayoffInput manaTypeOnly = new(
            [new NetherCrestPayoffRecipient(101, NetherCrestIdentity.Passion)],
            ValuePerRecipient: 300
        );
        NetherCrestPayoffInput provenPath = manaTypeOnly with
        {
            Recipients =
            [
                new NetherCrestPayoffRecipient(101, NetherCrestIdentity.Passion)
                {
                    ProviderPathKnown = true,
                    ProviderReachable = true,
                    ConsumerPathKnown = true,
                    ConsumerReachable = true,
                },
            ],
        };
        var valuation = new NetherMechanismSpecificValuation();

        NetherMechanismValue missing = valuation.EvaluateCrestPayoff(manaTypeOnly);
        NetherMechanismValue quantified = valuation.EvaluateCrestPayoff(provenPath);

        Assert.Equal(NetherCombatValueEvidenceKind.Missing, missing.Kind);
        Assert.Contains("provider", missing.Detail);
        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, quantified.Kind);
        Assert.Equal(300m, quantified.Quantity.Value);
    }

    [Fact]
    public void Shared_mana_uses_scope_modifier_chain_and_remaining_capacity_of_one_pool()
    {
        // Fresh ManaGemEnergy declares Min=0 and Max=10. AbilityChargeMana applies the native
        // charge-rate modifier before spawning energy, and ManaGemEnergy.ModifyChargeAmount folds
        // the registered IManaGemEnergyChargeModifier chain. The pool clamps only after additions.
        NetherSharedManaInjectionInput input = new(
            CurrentSharedEnergy: 7,
            RawEnergyPerRecipient: 1,
            ScopeMatchCount: 2,
            AbilityChargeModifierPermille: 500,
            RegisteredModifierSteps:
            [
                new NetherSharedManaModifierStep(InputEnergy: 1.5f, OutputEnergy: 2f),
            ]
        );
        var valuation = new NetherMechanismSpecificValuation();

        NetherMechanismValue value = valuation.EvaluateSharedManaInjection(input);
        NetherMechanismValue saturated = valuation.EvaluateSharedManaInjection(
            input with { CurrentSharedEnergy = 10 }
        );

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        Assert.Equal(3m, value.Quantity.Value);
        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, saturated.Kind);
        Assert.Equal(0m, saturated.Quantity.Value);
    }

    [Fact]
    public void Shared_mana_ignores_nonpositive_native_charge_rate_sum_and_never_injects_negative_energy()
    {
        // Fresh Cpp2IL ISIL from Project.dll 53806a5b...1300 / GameAssembly
        // 573fa800...c1fb: AbilityChargeMana.ExecuteInternal branches around PerMilleToFloat and
        // multiplication when SumIfEnableBuffs(BuffType 190, group 63) is <= 0.
        NetherSharedManaInjectionInput input = new(
            CurrentSharedEnergy: 0,
            RawEnergyPerRecipient: 2,
            ScopeMatchCount: 1,
            AbilityChargeModifierPermille: -1500,
            RegisteredModifierSteps: []
        );

        NetherMechanismValue value = new NetherMechanismSpecificValuation()
            .EvaluateSharedManaInjection(input);

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        Assert.Equal(2m, value.Quantity.Value);
    }

    [Fact]
    public void Initial_skill_charge_clips_independently_at_each_recipient_ready_threshold()
    {
        // Fresh AbilitySkillCharge.ExecuteInternal floors MaxChargeCount * ChargePermille for each
        // target, then ActionSkillCharge.AddChargeCount applies that recipient's efficiency and
        // positive/negative charge modifiers before ReadyToActionSkill compares to MaxChargeCount.
        NetherInitialSkillChargeInput input = new(
            ChargePermille: 500,
            Recipients:
            [
                new NetherSkillChargeRecipient(201, CurrentCharge: 90, MaxCharge: 100),
                new NetherSkillChargeRecipient(202, CurrentCharge: 20, MaxCharge: 100),
            ]
        );

        NetherMechanismValue value = new NetherMechanismSpecificValuation()
            .EvaluateInitialSkillCharge(input);

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        Assert.Equal(60m, value.Quantity.Value);
    }

    [Fact]
    public void Skill_charge_efficiency_retains_recurring_value_after_a_full_gauge_resets()
    {
        // Fresh ActionSkillCharge.ResetChargeCount writes the live count to zero, while the
        // AddChargeCount modifier path remains registered for later charge events. The first
        // segment caps in both cases; only the confirmed post-reset segment has marginal value.
        NetherRecurringSkillChargeInput input = new(
            ModifierPermille: 200,
            Segments:
            [
                new NetherSkillChargeTimelineSegment(
                    CharacterId: 301,
                    StartingCharge: 90,
                    MaxCharge: 100,
                    NativeBaseCharge: 20,
                    ResetAfterSegment: true
                ),
                new NetherSkillChargeTimelineSegment(
                    CharacterId: 301,
                    StartingCharge: 0,
                    MaxCharge: 100,
                    NativeBaseCharge: 40,
                    ResetAfterSegment: false
                ),
            ]
        );

        NetherMechanismValue value = new NetherMechanismSpecificValuation()
            .EvaluateRecurringSkillCharge(input);

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        Assert.Equal(8m, value.Quantity.Value);
    }

    [Fact]
    public void Stack_linked_value_requires_guaranteed_future_lower_bound_not_instant_live_count()
    {
        // Fresh Cpp2IL for AbilityStackLinkedBuff.GetStackCount reads only the instantaneous
        // StackBuffBase reactive count. It proves neither a future Boss grant/consume timeline nor
        // a lower bound, and the description's maximum is likewise not live evidence.
        NetherStackLinkedPayoffInput input = new(
            TriggerKnown: true,
            TriggerReachable: true,
            ValuePerStack: 10,
            Recipients:
            [
                new NetherStackLinkedRecipient(402)
                {
                    GuaranteedLowerBoundKnown = true,
                    GuaranteedLowerBound = 1,
                    DescribedMaximumStack = 99,
                },
            ]
        );
        NetherStackLinkedPayoffInput instantaneousOnly = input with
        {
            Recipients =
            [
                new NetherStackLinkedRecipient(403)
                {
                    LiveStackKnown = true,
                    LiveStackCount = 9,
                    DescribedMaximumStack = 99,
                },
            ],
        };

        NetherMechanismValue quantified = new NetherMechanismSpecificValuation()
            .EvaluateStackLinkedPayoff(input);
        NetherMechanismValue unquantified = new NetherMechanismSpecificValuation()
            .EvaluateStackLinkedPayoff(instantaneousOnly);

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, quantified.Kind);
        Assert.Equal(10m, quantified.Quantity.Value);
        Assert.Equal(NetherCombatValueEvidenceKind.ReachableUnquantified, unquantified.Kind);
    }

    [Fact]
    public void Erosion_linked_value_interpolates_each_confirmed_combat_not_description_maximum()
    {
        // Fresh AbilityErosionLinkedBuff initializes from the current Nether erosion ratio, converts
        // it to permille, then linearly interpolates Param Min/Max values on every ratio change.
        NetherErosionLinkedPayoffInput input = new(
            MinimumErosionPermille: 0,
            MaximumErosionPermille: 1000,
            MinimumValue: 0,
            MaximumValue: 200,
            ConfirmedCombats:
            [
                new NetherConfirmedCombatErosion(501, ProjectedErosionPermille: 250, IsExact: true),
                new NetherConfirmedCombatErosion(502, ProjectedErosionPermille: 750, IsExact: true),
            ]
        )
        {
            BuffType = new NetherStrategyBuffType((int)NetherKnownBuffType.AttackUp1),
            ParameterReferenceKind = NetherStrategyBuffParameterReferenceKind.RatePermille,
        };

        NetherMechanismValue value = new NetherMechanismSpecificValuation()
            .EvaluateErosionLinkedPayoff(input);

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        Assert.Equal(200m, value.Quantity.Value);
    }

    [Fact]
    public void Erosion_linked_midpoint_uses_native_math_round_to_even()
    {
        // Fresh Cpp2IL: AbilityErosionLinkedBuff.OnChangedHpLinkedIncrease/Decrease converts the
        // interpolated Single to Double and calls one-argument System.Math.Round(double).
        NetherErosionLinkedPayoffInput input = new(
            MinimumErosionPermille: 0,
            MaximumErosionPermille: 1000,
            MinimumValue: 0,
            MaximumValue: 5,
            ConfirmedCombats:
            [
                new NetherConfirmedCombatErosion(503, ProjectedErosionPermille: 500, IsExact: true),
            ]
        )
        {
            BuffType = new NetherStrategyBuffType((int)NetherKnownBuffType.AttackUp1),
            ParameterReferenceKind = NetherStrategyBuffParameterReferenceKind.RatePermille,
        };

        NetherMechanismValue value = new NetherMechanismSpecificValuation()
            .EvaluateErosionLinkedPayoff(input);

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        Assert.Equal(2m, value.Quantity.Value);
    }

    [Fact]
    public void Category_value_counts_only_immediate_authoritative_threshold_crossings()
    {
        // Fresh GetCategoryCount is the clamped paired-family count. Threshold effects are active
        // only at their current required count; distance from three to the future count five has no
        // option value.
        NetherCategoryThresholdInput crossing = new(
            BeforeEffectiveCount: 4,
            AfterEffectiveCount: 5,
            Effects:
            [
                new NetherCategoryThresholdEffect(RequiredCount: 5, ActiveValue: 100),
                new NetherCategoryThresholdEffect(RequiredCount: 10, ActiveValue: 500),
            ]
        );

        NetherMechanismValue crossed = new NetherMechanismSpecificValuation()
            .EvaluateImmediateCategoryThreshold(crossing);
        NetherMechanismValue merelyCloser = new NetherMechanismSpecificValuation()
            .EvaluateImmediateCategoryThreshold(
                crossing with { BeforeEffectiveCount = 3, AfterEffectiveCount = 4 }
            );

        Assert.Equal(100m, crossed.Quantity.Value);
        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, merelyCloser.Kind);
        Assert.Equal(0m, merelyCloser.Quantity.Value);
    }

    [Fact]
    public void Force_chain_uses_completion_message_reachability_and_keeps_row_priority_qualitative()
    {
        // Fresh BattleTriggerActivateForceChain completes only from ForceChainFinishedMessage.
        // That message proves reachability but contains no future cadence, so the approved row
        // preference remains qualitative rather than an invented per-second number.
        NetherForceChainPayoffInput back = new(
            CompletionTriggerKnown: true,
            CompletionMessageReachable: true,
            TargetRow: NetherCodeTargetRow.Back,
            NumericalEffectKnown: true
        );
        NetherForceChainPayoffInput front = back with { TargetRow = NetherCodeTargetRow.Forward };
        NetherForceChainPayoffInput missing = back with { CompletionTriggerKnown = false };

        NetherMechanismSpecificValuation valuation = new();
        NetherMechanismValue backValue = valuation.EvaluateForceChainPayoff(back);
        NetherMechanismValue frontValue = valuation.EvaluateForceChainPayoff(front);
        NetherMechanismValue missingValue = valuation.EvaluateForceChainPayoff(missing);

        Assert.Equal(NetherCombatValueEvidenceKind.QualitativePriority, backValue.Kind);
        Assert.Equal(NetherMechanismQualitativePriority.BackForceChainHigh, backValue.QualitativePriority);
        Assert.Equal(NetherCombatValueEvidenceKind.QualitativePriority, frontValue.Kind);
        Assert.Equal(NetherMechanismQualitativePriority.FrontForceChainFallback, frontValue.QualitativePriority);
        Assert.Equal(NetherCombatValueEvidenceKind.Missing, missingValue.Kind);
    }

    [Fact]
    public void Saturated_charge_is_quantified_zero_not_a_missing_or_banned_mechanism()
    {
        NetherInitialSkillChargeInput input = new(
            ChargePermille: 500,
            Recipients: [new NetherSkillChargeRecipient(601, CurrentCharge: 100, MaxCharge: 100)]
        );

        NetherMechanismValue value = new NetherMechanismSpecificValuation()
            .EvaluateInitialSkillCharge(input);

        Assert.Equal(NetherCombatValueEvidenceKind.Quantified, value.Kind);
        Assert.Equal(0m, value.Quantity.Value);
    }
}
