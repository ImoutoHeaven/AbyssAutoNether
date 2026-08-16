#nullable enable

using System;
using System.Collections.Generic;

namespace AutoNether.Services;

internal enum NetherMechanismQualitativePriority
{
    None = 0,
    FrontForceChainFallback,
    BackForceChainHigh,
}

/// <summary>
/// Native unit carried by one quantified mechanism result. Values are comparable only when this
/// tag is identical; it deliberately prevents mana energy, charge count, stacks, and erosion
/// parameter values from becoming one invented combat scalar.
/// </summary>
internal enum NetherMechanismQuantityKind
{
    None = 0,
    CrestRecipientPayoff,
    SharedManaEnergy,
    InitialSkillCharge,
    RecurringSkillCharge,
    GuaranteedStackPayoff,
    ErosionLinkedPayoff,
    CategoryThresholdPayoff,
}

internal readonly record struct NetherMechanismQuantityIdentity(
    NetherMechanismQuantityKind Kind,
    NetherStrategyBuffType BuffType,
    NetherStrategyBuffParameterReferenceKind ParameterReferenceKind
);

/// <summary>
/// Exact native quantity outcome for one matching character. Character and position remain part of
/// the identity so a same-domain Forward gain cannot numerically erase a Back loss.
/// </summary>
internal readonly record struct NetherMechanismRecipientQuantityIdentity(
    NetherMechanismQuantityIdentity QuantityIdentity,
    long CharacterId,
    NetherPartyPosition PartyPosition,
    NetherCombatMetricKind Metric
);

internal readonly record struct NetherMechanismRecipientQuantity(
    long CharacterId,
    NetherPartyPosition PartyPosition,
    NetherCombatMetricKind Metric,
    NetherMechanismQuantity Quantity
)
{
    public NetherMechanismRecipientQuantityIdentity Identity => new(
        Quantity.Identity,
        CharacterId,
        PartyPosition,
        Metric
    );
}

internal readonly record struct NetherMechanismQuantity(
    NetherMechanismQuantityKind Kind,
    decimal Value
)
{
    public NetherStrategyBuffType BuffType { get; init; }
    public NetherStrategyBuffParameterReferenceKind ParameterReferenceKind { get; init; }
    public NetherMechanismQuantityIdentity Identity => new(
        Kind,
        BuffType,
        ParameterReferenceKind
    );
}

internal readonly record struct NetherMechanismValue(
    NetherCombatValueEvidenceKind Kind,
    NetherMechanismQuantity Quantity,
    NetherMechanismQualitativePriority QualitativePriority,
    string Detail
)
{
    public IReadOnlyList<NetherMechanismRecipientQuantity> RecipientQuantities { get; init; } =
        Array.Empty<NetherMechanismRecipientQuantity>();

    public static NetherMechanismValue Missing(string detail) => new(
        NetherCombatValueEvidenceKind.Missing,
        default,
        NetherMechanismQualitativePriority.None,
        detail
    );

    public static NetherMechanismValue Quantified(
        NetherMechanismQuantityKind quantityKind,
        decimal value,
        string detail,
        NetherStrategyBuffType buffType = default,
        NetherStrategyBuffParameterReferenceKind parameterReferenceKind =
            NetherStrategyBuffParameterReferenceKind.Unknown
    ) => new(
        NetherCombatValueEvidenceKind.Quantified,
        new NetherMechanismQuantity(quantityKind, value)
        {
            BuffType = buffType,
            ParameterReferenceKind = parameterReferenceKind,
        },
        NetherMechanismQualitativePriority.None,
        detail
    );

    public static NetherMechanismValue ReachableUnquantified(string detail) => new(
        NetherCombatValueEvidenceKind.ReachableUnquantified,
        default,
        NetherMechanismQualitativePriority.None,
        detail
    );

    public static NetherMechanismValue Qualitative(
        NetherMechanismQualitativePriority priority,
        string detail
    ) => new(
        NetherCombatValueEvidenceKind.QualitativePriority,
        default,
        priority,
        detail
    );
}

internal sealed record NetherCrestPayoffRecipient(
    long CharacterId,
    NetherCrestIdentity CrestIdentity
)
{
    public bool ProviderPathKnown { get; init; }
    public bool ProviderReachable { get; init; }
    public bool ConsumerPathKnown { get; init; }
    public bool ConsumerReachable { get; init; }
}

internal sealed record NetherCrestPayoffInput(
    IReadOnlyList<NetherCrestPayoffRecipient> Recipients,
    decimal ValuePerRecipient
);

internal readonly record struct NetherSharedManaModifierStep(
    float InputEnergy,
    float OutputEnergy
);

internal sealed record NetherSharedManaInjectionInput(
    float CurrentSharedEnergy,
    float RawEnergyPerRecipient,
    int ScopeMatchCount,
    int AbilityChargeModifierPermille,
    IReadOnlyList<NetherSharedManaModifierStep> RegisteredModifierSteps
);

internal sealed record NetherSkillChargeRecipient(
    long CharacterId,
    float CurrentCharge,
    int MaxCharge
)
{
    public int ChargeEfficiencyPermille { get; init; } = 1000;
    public int PositiveModifierPermille { get; init; }
    public int NegativeModifierPermille { get; init; }
}

internal sealed record NetherInitialSkillChargeInput(
    int ChargePermille,
    IReadOnlyList<NetherSkillChargeRecipient> Recipients
);

internal readonly record struct NetherSkillChargeTimelineSegment(
    long CharacterId,
    float StartingCharge,
    int MaxCharge,
    float NativeBaseCharge,
    bool ResetAfterSegment
);

internal sealed record NetherRecurringSkillChargeInput(
    int ModifierPermille,
    IReadOnlyList<NetherSkillChargeTimelineSegment> Segments
);

internal sealed record NetherStackLinkedRecipient(long CharacterId)
{
    public bool LiveStackKnown { get; init; }
    public int LiveStackCount { get; init; }
    public bool GuaranteedLowerBoundKnown { get; init; }
    public int GuaranteedLowerBound { get; init; }
    /// <summary>Description-only metadata, retained to prove it is never treated as live state.</summary>
    public int DescribedMaximumStack { get; init; }
}

internal sealed record NetherStackLinkedPayoffInput(
    bool TriggerKnown,
    bool TriggerReachable,
    decimal ValuePerStack,
    IReadOnlyList<NetherStackLinkedRecipient> Recipients
);

internal readonly record struct NetherConfirmedCombatErosion(
    long FloorId,
    int ProjectedErosionPermille,
    bool IsExact
);

internal sealed record NetherErosionLinkedPayoffInput(
    int MinimumErosionPermille,
    int MaximumErosionPermille,
    int MinimumValue,
    int MaximumValue,
    IReadOnlyList<NetherConfirmedCombatErosion> ConfirmedCombats
)
{
    public NetherStrategyBuffType BuffType { get; init; }
    public NetherStrategyBuffParameterReferenceKind ParameterReferenceKind { get; init; }
}

internal readonly record struct NetherCategoryThresholdEffect(
    int RequiredCount,
    decimal ActiveValue
);

internal sealed record NetherCategoryThresholdInput(
    int BeforeEffectiveCount,
    int AfterEffectiveCount,
    IReadOnlyList<NetherCategoryThresholdEffect> Effects
);

internal sealed record NetherForceChainPayoffInput(
    bool CompletionTriggerKnown,
    bool CompletionMessageReachable,
    NetherCodeTargetRow TargetRow,
    bool NumericalEffectKnown
);

/// <summary>
/// Evidence-bounded public policy seam for native mechanics whose value is not an ordinary buff
/// percentage. Each method rejects only the relationship it needs and never uses displayed power.
/// </summary>
internal sealed class NetherMechanismSpecificValuation
{
    private readonly NetherCrestMechanismValuation _crest = new();
    private readonly NetherChargeMechanismValuation _charge = new();
    private readonly NetherStackMechanismValuation _stack = new();
    private readonly NetherErosionMechanismValuation _erosion = new();
    private readonly NetherCategoryMechanismValuation _category = new();
    private readonly NetherForceChainMechanismValuation _forceChain = new();

    public NetherMechanismValue EvaluateCrestPayoff(NetherCrestPayoffInput input) =>
        _crest.Evaluate(input);

    public NetherMechanismValue EvaluateSharedManaInjection(NetherSharedManaInjectionInput input) =>
        _charge.EvaluateSharedMana(input);

    public NetherMechanismValue EvaluateInitialSkillCharge(NetherInitialSkillChargeInput input) =>
        _charge.EvaluateInitialSkillCharge(input);

    public NetherMechanismValue EvaluateRecurringSkillCharge(NetherRecurringSkillChargeInput input) =>
        _charge.EvaluateRecurringSkillCharge(input);

    public NetherMechanismValue EvaluateStackLinkedPayoff(NetherStackLinkedPayoffInput input) =>
        _stack.Evaluate(input);

    public NetherMechanismValue EvaluateErosionLinkedPayoff(NetherErosionLinkedPayoffInput input) =>
        _erosion.Evaluate(input);

    public NetherMechanismValue EvaluateImmediateCategoryThreshold(
        NetherCategoryThresholdInput input
    ) => _category.Evaluate(input);

    public NetherMechanismValue EvaluateForceChainPayoff(NetherForceChainPayoffInput input) =>
        _forceChain.Evaluate(input);
}
