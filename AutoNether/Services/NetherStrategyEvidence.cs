#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Immutable identity shared by every strategy component.  The three generation values are
/// intentionally explicit: a controller can be current before the matching FloorSelection
/// SubScene.OnEntered evidence has been observed, and neither fact proves the server snapshot.
/// </summary>
internal readonly record struct NetherStrategyEvidenceIdentity(
    long RuntimeGeneration,
    long ControllerOwnerGeneration,
    long EnteredSubsceneGeneration,
    NetherSnapshotFingerprint SnapshotFingerprint
);

/// <summary>
/// The strategy package is populated component-by-component by the mapper below.  Keeping the
/// identity at the package boundary lets existing policy callers migrate incrementally without
/// weakening the already stable controller transaction gate.
/// </summary>
internal sealed record NetherStrategyEvidencePackage
{
    public NetherStrategyEvidenceIdentity Identity { get; init; }
    public NetherStrategyServerEvidence? Server { get; init; }
    public NetherStrategyEvidenceComponent<NetherStrategyPartyProfile> Party { get; init; } =
        NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Unknown("party-profile-unavailable");
    public NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence> OwnedCodes { get; init; } =
        NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence>.Unknown("owned-code-evidence-unavailable");
    public NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence> Research { get; init; } =
        NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence>.Unknown("research-evidence-unavailable");
    public NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence> NativeMechanics { get; init; } =
        NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>.Unknown("native-mechanics-unavailable");
    public NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence> VisibleMap { get; init; } =
        NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Unknown("visible-map-evidence-unavailable");
    /// <summary>Audit-only. These values never alter any component's known/unknown state.</summary>
    public IReadOnlyList<NetherStrategyDisplayDiagnostic> DisplayDiagnostics { get; init; } =
        Array.Empty<NetherStrategyDisplayDiagnostic>();
}

internal sealed record NetherStrategyEvidenceComponent<T> where T : class
{
    public bool IsKnown { get; init; }
    public T? Value { get; init; }
    public string UnknownReason { get; init; } = string.Empty;

    public static NetherStrategyEvidenceComponent<T> Known(T value) => new()
    {
        IsKnown = true,
        Value = value ?? throw new ArgumentNullException(nameof(value)),
    };

    public static NetherStrategyEvidenceComponent<T> Unknown(string reason) => new()
    {
        UnknownReason = string.IsNullOrWhiteSpace(reason) ? "unknown-evidence" : reason,
    };
}

internal sealed record NetherStrategyServerEvidence
{
    public NetherSessionStatus Status { get; init; }
    public long NetherId { get; init; }
    public long MapId { get; init; }
    public long CurrentFloorId { get; init; }
    public long CurrentNodeId { get; init; }
    public int FloorLevel { get; init; }
    public int FloorIndex { get; init; }
    public int ErosionPoint { get; init; }
    public int TicketCount { get; init; }
    public int SignalCount { get; init; }
    public int TreasureKeyCount { get; init; }
    public int NetherGold { get; init; }
    public int CodeCapacity { get; init; }
    public int CodeReloadCount { get; init; }
    public int RecoveryFloorLevel { get; init; }
    public int MasterMaxFloorLevel { get; init; }
    public IReadOnlyList<int> BossFloorLevels { get; init; } = Array.Empty<int>();
}

internal readonly record struct NetherStrategyNamedValue(string Name, long Value);

/// <summary>
/// Exact values exposed by the current NetherPartyCharacterModel.  Raw enum values are retained
/// so update-tolerance never invents a renamed position, element, or ManaType.
/// </summary>
internal sealed record NetherStrategyPartyMember(
    long CharacterId,
    int PartyIndex,
    int PartyPosition,
    int ElementType,
    int ManaType,
    int HpPermille,
    bool IsAlive,
    int Level,
    int LimitBreakCount
)
{
    public IReadOnlyList<NetherStrategyNamedValue> NativeParameters { get; init; } =
        Array.Empty<NetherStrategyNamedValue>();
    public IReadOnlyList<NetherStrategyAbilityEffect> CharacterAbilityEffects { get; init; } =
        Array.Empty<NetherStrategyAbilityEffect>();
    public IReadOnlyList<NetherStrategyAbilityEffect> EquipmentAbilityEffects { get; init; } =
        Array.Empty<NetherStrategyAbilityEffect>();
    public IReadOnlyList<NetherStrategyAbilityEffect> GeneralAbilityEffects { get; init; } =
        Array.Empty<NetherStrategyAbilityEffect>();
}

internal readonly record struct NetherStrategyAbilityEffect(
    long EffectId,
    int Level,
    int RawAbilityType,
    int Value
)
{
    public int AwakeningLevel { get; init; }
}

internal sealed record NetherStrategyPartyProfile(
    IReadOnlyList<NetherStrategyPartyMember> Members
);

internal sealed record NetherStrategyCategorySkill(
    long SkillId,
    int Counter,
    NetherCodeFamily Family,
    int RawEffectType,
    long EffectParameter1,
    long EffectParameter2,
    long EffectParameter3
);

internal sealed record NetherStrategyDecodedCodeEffect(
    long CodeId,
    NetherCodeMasterEffectType EffectType,
    long EffectParameter1,
    long EffectParameter2,
    long EffectParameter3,
    long AbilityAssetId,
    bool IsKnown,
    string UnknownReason
);

internal readonly record struct NetherStrategyFamilyCount(
    NetherCodeFamily Family,
    int OwnedCount,
    int OpposingCount,
    int EffectiveCount
);

internal sealed record NetherStrategyOwnedCodeEvidence(
    IReadOnlyList<NetherCodeState> Codes,
    int Capacity,
    int Rerolls
)
{
    public IReadOnlyList<NetherStrategyDecodedCodeEffect> DecodedEffects { get; init; } =
        Array.Empty<NetherStrategyDecodedCodeEffect>();
    public IReadOnlyList<NetherStrategyCategorySkill> CategorySkills { get; init; } =
        Array.Empty<NetherStrategyCategorySkill>();
    public IReadOnlyList<NetherStrategyFamilyCount> FamilyCounts { get; init; } =
        Array.Empty<NetherStrategyFamilyCount>();
}

/// <summary>
/// Wallet values map to NetherPointData's four Sphere*Point properties.  The technology rate is
/// retained in its raw native unit (NetherPointData.SpherePointRatio), never guessed as additive.
/// </summary>
internal readonly record struct NetherStrategyResearchFamilyState(
    NetherCodeFamily Family,
    int WalletPoints,
    int ProjectedNormalSettlementPoints,
    int TechnologyResearchRate
)
{
    /// <summary>
    /// Exact family count consumed by the native normal-result calculation. The current result
    /// endpoint exposes its authoritative projection only as NetherCodePointEntity at settlement,
    /// so a live run retains this input even when the final point result is not yet knowable.
    /// </summary>
    public int SettlementAcquiredCodeCount { get; init; }
    public bool IsProjectedNormalSettlementKnown { get; init; } = true;
    public string ProjectionUnknownReason { get; init; } = string.Empty;
}

internal sealed record NetherStrategyResearchEvidence(
    IReadOnlyList<NetherStrategyResearchFamilyState> Families
);

internal enum NetherStrategyTriggerKind
{
    Unknown = 0,
    NativeRunState,
    AboveErosion,
    ActionCount,
    ActivateForceChain,
    Avoidance,
    BattleFinish,
    BelowErosion,
    BelowHp,
    BuiltIn,
    Critical,
    DeadEnemy,
    DestroyEnemy,
    Duration,
    ExceedHp,
    GameStart,
    GiveDamage,
    GiveRecovery,
    ImmediateExecution,
    OreMining,
    OtherAllyActivateActionSkill,
    ReceiveAbnormal,
    ReceiveBuff,
    ReceiveDamage,
    ReceiveRecovery,
    ServantSummonExist,
    ServantSummonLeave,
    SpendBuff,
    StartBattle,
}

internal enum NetherStrategyTriggerProbabilityType
{
    Unknown = 0,
    Fixed,
    AbilityLevel,
    NotApplicable,
}

internal enum NetherStrategyExecuteCountLimitKind
{
    Unknown = 0,
    None,
    Battle,
    Quest,
}

internal enum NetherStrategySituationCostKind
{
    Unknown = 0,
    BuffStack,
    BuffStackPerLevel,
}

internal sealed record NetherStrategyExecuteCountLimitEvidence(
    NetherStrategyExecuteCountLimitKind Kind,
    string NativeTypeIdentity,
    int RawValueType,
    int FixedCountLimit,
    IReadOnlyList<int> LevelCountLimits
)
{
    public bool IsKnown { get; init; } = Kind != NetherStrategyExecuteCountLimitKind.Unknown;
    public string UnknownReason { get; init; } = string.Empty;
}

internal sealed record NetherStrategySituationCostEvidence(
    NetherStrategySituationCostKind Kind,
    string NativeTypeIdentity,
    int BuffType,
    int FixedStack,
    IReadOnlyList<int> LevelStacks
)
{
    public IReadOnlyList<int> LevelBuffTypes { get; init; } = Array.Empty<int>();
    public bool IsKnown { get; init; } = Kind != NetherStrategySituationCostKind.Unknown;
    public string UnknownReason { get; init; } = string.Empty;
}

/// <summary>
/// Exact control relationships inherited by every current BattleSituationBase.  The native
/// GetProbabilityPerMille(level), CreateSituationLimits(...), ExecuteCountLimit.Create(...), and
/// SituationCost.SituationCosts flows consume these values before the subtype condition runs.
/// </summary>
internal sealed record NetherStrategyTriggerControlEvidence
{
    public bool IsKnown { get; init; }
    public NetherStrategyTriggerProbabilityType ProbabilityType { get; init; }
    public int FixedProbabilityPermille { get; init; }
    public IReadOnlyList<int> LevelProbabilityPermille { get; init; } = Array.Empty<int>();
    public NetherStrategyExecuteCountLimitEvidence? ExecuteCountLimit { get; init; }
    public IReadOnlyList<NetherStrategySituationCostEvidence> SituationCosts { get; init; } =
        Array.Empty<NetherStrategySituationCostEvidence>();
    public string UnknownReason { get; init; } = string.Empty;

    public static NetherStrategyTriggerControlEvidence KnownFixed(int probabilityPermille) => new()
    {
        IsKnown = true,
        ProbabilityType = NetherStrategyTriggerProbabilityType.Fixed,
        FixedProbabilityPermille = probabilityPermille,
        ExecuteCountLimit = new NetherStrategyExecuteCountLimitEvidence(
            NetherStrategyExecuteCountLimitKind.None,
            string.Empty,
            0,
            0,
            Array.Empty<int>()
        ),
    };

    public static NetherStrategyTriggerControlEvidence KnownNotApplicable() => new()
    {
        IsKnown = true,
        ProbabilityType = NetherStrategyTriggerProbabilityType.NotApplicable,
        ExecuteCountLimit = new NetherStrategyExecuteCountLimitEvidence(
            NetherStrategyExecuteCountLimitKind.None,
            string.Empty,
            0,
            0,
            Array.Empty<int>()
        ),
    };

    public static NetherStrategyTriggerControlEvidence Unknown(string reason) => new()
    {
        UnknownReason = string.IsNullOrWhiteSpace(reason)
            ? "trigger-control-relationships-unavailable"
            : reason,
    };
}

internal readonly record struct NetherStrategyTriggerEvidence(NetherStrategyTriggerKind Kind)
{
    public bool IsKnown => Kind != NetherStrategyTriggerKind.Unknown
        && ParametersKnown
        && ControlRelationships.IsKnown;
    public int Parameter1 { get; init; }
    public int Parameter2 { get; init; }
    public int Parameter3 { get; init; }
    public bool ParametersKnown { get; init; }
    public string NativeTypeIdentity { get; init; } = string.Empty;
    public string UnknownReason { get; init; } = string.Empty;
    public NetherStrategyTriggerControlEvidence ControlRelationships { get; init; } =
        NetherStrategyTriggerControlEvidence.Unknown("trigger-control-relationships-not-captured");
}

internal sealed record NetherStrategyNativeTriggerCapture(
    NetherStrategyTriggerKind Kind,
    string NativeTypeIdentity
)
{
    public int Parameter1 { get; init; }
    public int Parameter2 { get; init; }
    public int Parameter3 { get; init; }
    public bool ParametersKnown { get; init; }
    public string UnknownReason { get; init; } = string.Empty;
    public NetherStrategyTriggerProbabilityType ProbabilityType { get; init; }
    public int FixedProbabilityPermille { get; init; }
    public IReadOnlyList<int> LevelProbabilityPermille { get; init; } = Array.Empty<int>();
    public NetherStrategyExecuteCountLimitEvidence? ExecuteCountLimit { get; init; }
    public IReadOnlyList<NetherStrategySituationCostEvidence> SituationCosts { get; init; } =
        Array.Empty<NetherStrategySituationCostEvidence>();
    public bool ControlRelationshipsKnown { get; init; }
    public string ControlUnknownReason { get; init; } = string.Empty;
}

/// <summary>
/// Result of validating the exact typed trigger values captured from
/// <c>IAbilityEffectData.Situations</c>. This public production-mapper seam deliberately avoids
/// sentinel searches on a value type: <c>default(NetherStrategyTriggerEvidence)</c> is Unknown and
/// is not evidence that an all-known native list contained an unknown trigger.
/// </summary>
internal sealed record NetherStrategyTriggerCaptureResult(
    IReadOnlyList<NetherStrategyTriggerEvidence> Triggers,
    bool IsKnown,
    string UnknownReason
);

internal static class NetherStrategyNativeMechanicCaptureMapper
{
    public static NetherStrategyTriggerEvidence MapTrigger(
        NetherStrategyNativeTriggerCapture capture
    )
    {
        if (capture == null)
            throw new ArgumentNullException(nameof(capture));
        NetherStrategyTriggerControlEvidence control = new()
        {
            IsKnown = capture.ControlRelationshipsKnown,
            ProbabilityType = capture.ProbabilityType,
            FixedProbabilityPermille = capture.FixedProbabilityPermille,
            LevelProbabilityPermille = capture.LevelProbabilityPermille.ToArray(),
            ExecuteCountLimit = capture.ExecuteCountLimit == null
                ? null
                : capture.ExecuteCountLimit with
                {
                    LevelCountLimits = capture.ExecuteCountLimit.LevelCountLimits.ToArray(),
                },
            SituationCosts = capture.SituationCosts.Select(cost => cost with
            {
                LevelBuffTypes = cost.LevelBuffTypes.ToArray(),
                LevelStacks = cost.LevelStacks.ToArray(),
            }).ToArray(),
            UnknownReason = capture.ControlRelationshipsKnown
                ? string.Empty
                : string.IsNullOrWhiteSpace(capture.ControlUnknownReason)
                    ? "trigger-control-relationships-unavailable:" + capture.NativeTypeIdentity
                    : capture.ControlUnknownReason,
        };
        string unknown = capture.Kind == NetherStrategyTriggerKind.Unknown
            || !capture.ParametersKnown
            || !control.IsKnown
                ? !string.IsNullOrWhiteSpace(capture.UnknownReason)
                    ? capture.UnknownReason
                    : control.UnknownReason
                : string.Empty;
        return new NetherStrategyTriggerEvidence(capture.Kind)
        {
            Parameter1 = capture.Parameter1,
            Parameter2 = capture.Parameter2,
            Parameter3 = capture.Parameter3,
            ParametersKnown = capture.ParametersKnown,
            NativeTypeIdentity = capture.NativeTypeIdentity,
            UnknownReason = unknown,
            ControlRelationships = control,
        };
    }

    public static NetherStrategyTriggerCaptureResult MapTriggers(
        long mechanicId,
        IReadOnlyList<NetherStrategyTriggerEvidence>? captured
    )
    {
        if (captured == null)
        {
            return new NetherStrategyTriggerCaptureResult(
                Array.Empty<NetherStrategyTriggerEvidence>(),
                false,
                "ability-situations-unavailable:" + mechanicId
            );
        }
        if (captured.Count == 0)
        {
            return new NetherStrategyTriggerCaptureResult(
                Array.Empty<NetherStrategyTriggerEvidence>(),
                false,
                "ability-situations-empty:" + mechanicId
            );
        }

        NetherStrategyTriggerEvidence[] copy = captured.ToArray();
        foreach (NetherStrategyTriggerEvidence trigger in copy)
        {
            if (!trigger.IsKnown)
            {
                return new NetherStrategyTriggerCaptureResult(
                    copy,
                    false,
                    string.IsNullOrWhiteSpace(trigger.UnknownReason)
                        ? "unknown-ability-situation:" + mechanicId
                        : trigger.UnknownReason
                );
            }
        }

        return new NetherStrategyTriggerCaptureResult(copy, true, string.Empty);
    }

    public static NetherStrategyAbilityEffectEvidence MapAbilityEffect(
        NetherStrategyNativeAbilityEffectCapture capture
    )
    {
        if (capture == null)
            throw new ArgumentNullException(nameof(capture));

        NetherStrategyLinkedBuffThresholdEvidence? min = MapThreshold(capture.MinLinkedBuff);
        NetherStrategyLinkedBuffThresholdEvidence? max = MapThreshold(capture.MaxLinkedBuff);
        NetherStrategyBuffParameterEvidence[] parameters = capture.BuffParameters
            .Select(MapBuffParameter)
            .ToArray();
        return new NetherStrategyAbilityEffectEvidence(capture.Kind)
        {
            NativeTypeIdentity = capture.NativeTypeIdentity,
            UnknownReason = capture.Kind == NetherStrategyAbilityEffectKind.Unknown
                || !capture.ParametersKnown
                    ? capture.ParameterUnknownReason
                    : string.Empty,
            ParametersKnown = capture.ParametersKnown,
            ParameterUnknownReason = capture.ParameterUnknownReason,
            ManaEnergy = capture.ManaEnergy,
            SkillChargePermille = capture.SkillChargePermille,
            MinLinkedBuff = min,
            MaxLinkedBuff = max,
            BuffParameters = parameters,
            Conditions = capture.Conditions.ToArray(),
            EndSituationCondition = capture.EndSituationCondition,
            EndSituationValue = capture.EndSituationValue,
            EndSituationKnown = capture.EndSituationKnown,
            LinkedBuffType = capture.LinkedBuffType,
            LinkedBuffTypeKnown = capture.LinkedBuffTypeKnown,
            RecoverHpHealType = capture.RecoverHpHealType,
            RecoverHpFixedValue = capture.RecoverHpFixedValue,
            RecoverHpStatusSourceType = capture.RecoverHpStatusSourceType,
            RecoverHpRatePermille = capture.RecoverHpRatePermille,
            RecoverHpMaxHeal = capture.RecoverHpMaxHeal,
            AbnormalType = capture.AbnormalType,
            AbnormalLevel = capture.AbnormalLevel,
            AbnormalApplyProbabilityPermille = capture.AbnormalApplyProbabilityPermille,
            AbnormalDurationSeconds = capture.AbnormalDurationSeconds,
            StageFieldReductionPermille = capture.StageFieldReductionPermille,
            StageFieldManaGainSourceFlags = capture.StageFieldManaGainSourceFlags,
            SummonParameterAdditionRatePermille = capture.SummonParameterAdditionRatePermille,
        };
    }

    private static NetherStrategyLinkedBuffThresholdEvidence? MapThreshold(
        NetherStrategyLinkedBuffThresholdCapture? capture
    ) => capture == null
        ? null
        : new NetherStrategyLinkedBuffThresholdEvidence(
            capture.PerMille,
            MapBuffParameter(capture.BuffParameter)
        );

    private static NetherStrategyBuffParameterEvidence MapBuffParameter(
        NetherStrategyNativeBuffParameterCapture capture
    ) => new(
        capture.BuffType,
        capture.TargetFilter == null
            ? null
            : capture.TargetFilter with
            {
                RequiredBuffTypes = capture.TargetFilter.RequiredBuffTypes.ToArray(),
            },
        capture.ParameterReference
    )
    {
        IsKnown = capture.IsKnown,
        UnknownReason = capture.UnknownReason,
    };
}

internal enum NetherStrategyTargetKind
{
    Unknown = 0,
    NetherRun,
    Action,
    Friend,
    Opponent,
    Self,
    SucceedAttack,
    SucceedRecover,
    Template,
}

internal readonly record struct NetherStrategyTargetEvidence(NetherStrategyTargetKind Kind)
{
    public bool IsKnown => Kind != NetherStrategyTargetKind.Unknown;
    public int ElementTypeFlags { get; init; }
    public int PartyPositionFlags { get; init; }
    public int UnionTypeFlags { get; init; }
    public int SearchType { get; init; }
    public int RandomCount { get; init; }
    public bool ParametersKnown { get; init; }
    public string NativeTypeIdentity { get; init; } = string.Empty;
    public string UnknownReason { get; init; } = string.Empty;
}

internal enum NetherStrategyAbilityEffectKind
{
    Unknown = 0,
    NativeCodeEffect,
    AbnormalApply,
    AbnormalRecovery,
    ActionPatternChange,
    AppendSkill,
    ChargeMana,
    ErosionLinkedBuff,
    HpLinkedBuff,
    ParameterBuff,
    PassiveBuff,
    RecoverHp,
    SkillCharge,
    StackLinkedBuff,
    StageFieldManaGainDown,
    SummonParameterAdditionRate,
    Template,
}

internal enum NetherStrategyBuffParameterReferenceKind
{
    Unknown = 0,
    RatePermille,
    FixedPermille,
    FixedValue,
    AbnormalProbabilityPermille,
}

/// <summary>
/// Exact stable fields of the current native BuffTargetFilter. Raw flag values are retained so a
/// future enum member cannot be silently assigned an invented semantic name.
/// </summary>
internal sealed record NetherStrategyBuffTargetFilterEvidence(
    bool IgnoreDeadUnit,
    int ElementTypeFlags,
    int ElementWeakTypeFlags,
    int PartyPositionFlags,
    int UnionTypeFlags,
    int JobGroupFlags,
    int JobSpeciesFlags,
    int CharacterSizeFlags,
    IReadOnlyList<NetherStrategyBuffType> RequiredBuffTypes
);

internal readonly record struct NetherStrategyBuffParameterReferenceEvidence(
    NetherStrategyBuffParameterReferenceKind Kind,
    string NativeTypeIdentity
)
{
    public bool IsKnown => Kind != NetherStrategyBuffParameterReferenceKind.Unknown && ValuesKnown;
    public int ValueType { get; init; }
    public int Value { get; init; }
    public int Limit { get; init; }
    public bool ValuesKnown { get; init; }
    public string UnknownReason { get; init; } = string.Empty;
}

internal sealed record NetherStrategyNativeBuffParameterCapture(
    NetherStrategyBuffType BuffType,
    NetherStrategyBuffTargetFilterEvidence? TargetFilter,
    NetherStrategyBuffParameterReferenceEvidence ParameterReference
)
{
    public bool IsKnown { get; init; } = true;
    public string UnknownReason { get; init; } = string.Empty;
}

internal sealed record NetherStrategyBuffParameterEvidence(
    NetherStrategyBuffType BuffType,
    NetherStrategyBuffTargetFilterEvidence? TargetFilter,
    NetherStrategyBuffParameterReferenceEvidence ParameterReference
)
{
    public bool IsKnown { get; init; } = true;
    public string UnknownReason { get; init; } = string.Empty;
}

internal sealed record NetherStrategyLinkedBuffThresholdCapture(
    int PerMille,
    NetherStrategyNativeBuffParameterCapture BuffParameter
);

internal sealed record NetherStrategyLinkedBuffThresholdEvidence(
    int PerMille,
    NetherStrategyBuffParameterEvidence BuffParameter
);

internal enum NetherStrategyBuffConditionKind
{
    Unknown = 0,
    HpBelowOrEqual,
    HpAboveOrEqual,
    HpFull,
    HasBuff,
}

internal readonly record struct NetherStrategyBuffConditionEvidence(
    NetherStrategyBuffConditionKind Kind,
    string NativeTypeIdentity
)
{
    public bool IsKnown => Kind != NetherStrategyBuffConditionKind.Unknown;
    public int HpThresholdPermille { get; init; }
    public NetherStrategyBuffType RequiredBuffType { get; init; }
    public int RequiredBuffStack { get; init; }
    public string UnknownReason { get; init; } = string.Empty;
}

/// <summary>
/// Typed boundary DTO populated directly from current Project ability-effect classes. It contains
/// only stable, compile-time native members; unsupported subtypes are captured as Unknown with the
/// exact runtime identity/reason rather than inferred from type names.
/// </summary>
internal sealed record NetherStrategyNativeAbilityEffectCapture(
    NetherStrategyAbilityEffectKind Kind,
    string NativeTypeIdentity
)
{
    public bool ParametersKnown { get; init; }
    public string ParameterUnknownReason { get; init; } = string.Empty;
    public float ManaEnergy { get; init; }
    public int SkillChargePermille { get; init; }
    public NetherStrategyLinkedBuffThresholdCapture? MinLinkedBuff { get; init; }
    public NetherStrategyLinkedBuffThresholdCapture? MaxLinkedBuff { get; init; }
    public IReadOnlyList<NetherStrategyNativeBuffParameterCapture> BuffParameters { get; init; } =
        Array.Empty<NetherStrategyNativeBuffParameterCapture>();
    public IReadOnlyList<NetherStrategyBuffConditionEvidence> Conditions { get; init; } =
        Array.Empty<NetherStrategyBuffConditionEvidence>();
    public int EndSituationCondition { get; init; }
    public int EndSituationValue { get; init; }
    public bool EndSituationKnown { get; init; }
    public NetherStrategyBuffType LinkedBuffType { get; init; }
    public bool LinkedBuffTypeKnown { get; init; }
    public int RecoverHpHealType { get; init; }
    public int RecoverHpFixedValue { get; init; }
    public int RecoverHpStatusSourceType { get; init; }
    public int RecoverHpRatePermille { get; init; }
    public int RecoverHpMaxHeal { get; init; }
    public int AbnormalType { get; init; }
    public int AbnormalLevel { get; init; }
    public int AbnormalApplyProbabilityPermille { get; init; }
    public float AbnormalDurationSeconds { get; init; }
    public int StageFieldReductionPermille { get; init; }
    public int StageFieldManaGainSourceFlags { get; init; }
    public int SummonParameterAdditionRatePermille { get; init; }
}

internal readonly record struct NetherStrategyAbilityEffectEvidence(
    NetherStrategyAbilityEffectKind Kind
)
{
    public bool IsKnown => Kind != NetherStrategyAbilityEffectKind.Unknown && ParametersKnown;
    public string NativeTypeIdentity { get; init; } = string.Empty;
    public string UnknownReason { get; init; } = string.Empty;
    public bool ParametersKnown { get; init; } = true;
    public string ParameterUnknownReason { get; init; } = string.Empty;
    public float ManaEnergy { get; init; }
    public int SkillChargePermille { get; init; }
    public NetherStrategyLinkedBuffThresholdEvidence? MinLinkedBuff { get; init; }
    public NetherStrategyLinkedBuffThresholdEvidence? MaxLinkedBuff { get; init; }
    public IReadOnlyList<NetherStrategyBuffParameterEvidence> BuffParameters { get; init; } =
        Array.Empty<NetherStrategyBuffParameterEvidence>();
    public IReadOnlyList<NetherStrategyBuffConditionEvidence> Conditions { get; init; } =
        Array.Empty<NetherStrategyBuffConditionEvidence>();
    public int EndSituationCondition { get; init; }
    public int EndSituationValue { get; init; }
    public bool EndSituationKnown { get; init; }
    public NetherStrategyBuffType LinkedBuffType { get; init; }
    public bool LinkedBuffTypeKnown { get; init; }
    public int RecoverHpHealType { get; init; }
    public int RecoverHpFixedValue { get; init; }
    public int RecoverHpStatusSourceType { get; init; }
    public int RecoverHpRatePermille { get; init; }
    public int RecoverHpMaxHeal { get; init; }
    public int AbnormalType { get; init; }
    public int AbnormalLevel { get; init; }
    public int AbnormalApplyProbabilityPermille { get; init; }
    public float AbnormalDurationSeconds { get; init; }
    public int StageFieldReductionPermille { get; init; }
    public int StageFieldManaGainSourceFlags { get; init; }
    public int SummonParameterAdditionRatePermille { get; init; }
}

/// <summary>Exact raw Project.BuffType value, kept typed without inventing future enum members.</summary>
internal readonly record struct NetherStrategyBuffType(int Value)
{
    public bool IsKnown => Value > 0;
}

internal enum NetherStrategyBuffEffectKind
{
    Unknown = -1,
    Buff = 0,
    DeBuff = 1,
    Unique = 2,
}

internal enum NetherStrategyStatusPriorityKind
{
    Unknown = int.MinValue,
    Invalid = -1,
    Crest = 0,
    Abnormal = 1,
    Unique = 2,
    Debuff = 3,
    Buff = 4,
}

internal enum NetherStrategyBuffCoexistenceKind
{
    Unknown = -1,
    Allow = 0,
    HigherValue = 1,
    LongerRemainTime = 2,
    Latest = 3,
    Oldest = 4,
    Stack = 5,
    ExclusiveCrest = 6,
}

internal sealed record NetherStrategyBuffEvidence(
    NetherStrategyBuffType BuffType,
    NetherStrategyBuffEffectKind EffectKind,
    NetherStrategyStatusPriorityKind StatusPriority,
    NetherStrategyBuffCoexistenceKind Coexistence
)
{
    public IReadOnlyList<NetherStrategyBuffType> AdditionalMatchedTypes { get; init; } =
        Array.Empty<NetherStrategyBuffType>();
    public bool IsKnown { get; init; } = true;
    public string UnknownReason { get; init; } = string.Empty;
}

/// <summary>
/// Typed copy of one current native Code mechanic. Polymorphic trigger/target/effect classes are
/// mapped to tagged domain values; an unrecognized future subtype remains typed Unknown with its
/// exact runtime identity in the component reason. Native master parameters retain their fixed
/// field positions instead of becoming name/value bags.
/// </summary>
internal sealed record NetherStrategyNativeMechanic(
    long MechanicId,
    NetherCodeMasterEffectType SourceEffectType,
    IReadOnlyList<NetherStrategyTriggerEvidence> Triggers,
    NetherStrategyTargetEvidence Target
)
{
    public NetherStrategyAbilityEffectEvidence AbilityEffect { get; init; } =
        new(NetherStrategyAbilityEffectKind.Unknown);
    public IReadOnlyList<NetherStrategyBuffEvidence> BuffStrategies { get; init; } =
        Array.Empty<NetherStrategyBuffEvidence>();
    public long MasterEffectParameter1 { get; init; }
    public long MasterEffectParameter2 { get; init; }
    public long MasterEffectParameter3 { get; init; }
    public int Duration { get; init; }
    public bool DurationKnown { get; init; }
    public int Cap { get; init; }
    public bool CapKnown { get; init; }
    public bool IsKnown { get; init; } = true;
    public string UnknownReason { get; init; } = string.Empty;
}

internal sealed record NetherStrategyNativeMechanicsEvidence(
    IReadOnlyList<NetherStrategyNativeMechanic> Mechanics
);

/// <summary>
/// Joins the authoritative positive owned-Code set to independently captured native mechanics.
/// A missing MNetherCodes row is a row-local typed unknown, never a reason to discard mechanics
/// already captured for other owned Codes.
/// </summary>
internal static class NetherStrategyNativeMechanicAssembler
{
    public static IReadOnlyList<NetherStrategyNativeMechanic> AssembleOwnedCodes(
        IReadOnlyList<NetherCodeState> ownedCodes,
        IReadOnlyDictionary<long, NetherStrategyNativeMechanic> capturedByCodeId
    )
    {
        if (ownedCodes == null)
            throw new ArgumentNullException(nameof(ownedCodes));
        if (capturedByCodeId == null)
            throw new ArgumentNullException(nameof(capturedByCodeId));

        var assembled = new List<NetherStrategyNativeMechanic>();
        foreach (NetherCodeState code in ownedCodes
            .Where(code => code != null && code.PossessionAmount > 0)
            .GroupBy(code => code.CodeId)
            .Select(group => group.First())
            .OrderBy(code => code.CodeId))
        {
            if (capturedByCodeId.TryGetValue(code.CodeId, out NetherStrategyNativeMechanic? mechanic))
            {
                assembled.Add(mechanic);
                continue;
            }
            string reason = "missing-strategy-m-nether-code:" + code.CodeId;
            assembled.Add(new NetherStrategyNativeMechanic(
                code.CodeId,
                NetherCodeMasterEffectType.Unknown,
                [new NetherStrategyTriggerEvidence(NetherStrategyTriggerKind.Unknown)
                {
                    UnknownReason = reason,
                    ControlRelationships = NetherStrategyTriggerControlEvidence.Unknown(reason),
                }],
                new NetherStrategyTargetEvidence(NetherStrategyTargetKind.Unknown)
                {
                    UnknownReason = reason,
                }
            )
            {
                AbilityEffect = new NetherStrategyAbilityEffectEvidence(
                    NetherStrategyAbilityEffectKind.Unknown
                )
                {
                    ParametersKnown = false,
                    ParameterUnknownReason = reason,
                    UnknownReason = reason,
                },
                IsKnown = false,
                UnknownReason = reason,
            });
        }
        return assembled.ToArray();
    }
}

internal enum NetherStrategyVisibleContentKind
{
    Unknown = 0,
    Event = 1,
    Battle = 2,
    Item = 3,
    Treasure = 4,
    ShopInventory = 5,
    Resource = 6,
    Boss = 7,
}

internal enum NetherStrategyVisibleEventEffectSource
{
    Unknown = 0,
    Target1,
    Target2,
    Target3,
    Content,
}

internal sealed record NetherStrategyVisibleEventEffectEvidence(
    NetherStrategyVisibleEventEffectSource Source,
    int RawType,
    long RawParameter
)
{
    public long ContentId { get; init; }
    public long Amount { get; init; }
    public NetherEffectKind EffectKind { get; init; }
    public bool IsPresent { get; init; }
    public bool IsKnown { get; init; }
    public string UnknownReason { get; init; } = string.Empty;
}

internal sealed record NetherStrategyVisibleEventOptionEvidence(
    int OptionNumber,
    long EventPartId,
    IReadOnlyList<NetherStrategyVisibleEventEffectEvidence> Effects
);

internal sealed record NetherStrategyVisibleContentRow(
    NetherStrategyVisibleContentKind Kind,
    long NodeId,
    long MasterRowId,
    long ContentId
)
{
    public int Amount { get; init; }
    public int Cost { get; init; }
    public int Rank { get; init; }
    public int Weight { get; init; }
    public long MapFloorMasterId { get; init; }
    public long EventId { get; init; }
    public long EventPartId { get; init; }
    public int ContentType { get; init; }
    public int BattleType { get; init; }
    public long BattleStageId { get; init; }
    public int CodeDropRatio { get; init; }
    public long ItemType { get; init; }
    public int ItemRarity { get; init; }
    public int ItemValue { get; init; }
    public int ItemPossessionLimit { get; init; }
    public bool IsKnown { get; init; } = true;
    public string UnknownReason { get; init; } = string.Empty;
    public IReadOnlyList<NetherStrategyNamedValue> RawValues { get; init; } =
        Array.Empty<NetherStrategyNamedValue>();
    public IReadOnlyList<NetherStrategyVisibleEventOptionEvidence> EventOptions { get; init; } =
        Array.Empty<NetherStrategyVisibleEventOptionEvidence>();
}

internal sealed record NetherStrategyVisibleMapEvidence(
    IReadOnlyList<NetherFloorNode> Floors,
    IReadOnlyList<NetherStrategyVisibleContentRow> ContentRows
);

internal readonly record struct NetherStrategyDisplayDiagnostic(
    long CodeId,
    int DisplayedPower,
    int DisplayedTargetCoverage
);

internal sealed record NetherStrategyEvidenceMapRequest(
    NetherStrategyEvidenceIdentity Identity,
    NetherSnapshot? Snapshot
)
{
    public IReadOnlyList<NetherStrategyPartyMember>? Party { get; init; }
    public string PartyUnknownReason { get; init; } = string.Empty;
    public NetherStrategyOwnedCodeEvidence? OwnedCodes { get; init; }
    public string OwnedCodesUnknownReason { get; init; } = string.Empty;
    public IReadOnlyList<NetherStrategyResearchFamilyState>? Research { get; init; }
    public string ResearchUnknownReason { get; init; } = string.Empty;
    public IReadOnlyList<NetherStrategyNativeMechanic>? NativeMechanics { get; init; }
    public string NativeMechanicsUnknownReason { get; init; } = string.Empty;
    public NetherStrategyVisibleMapEvidence? VisibleMap { get; init; }
    public string VisibleMapUnknownReason { get; init; } = string.Empty;
    public IReadOnlyList<NetherStrategyDisplayDiagnostic>? DisplayDiagnostics { get; init; }
}

internal sealed record NetherStrategyEvidenceMapResult
{
    public NetherStrategyEvidencePackage? Package { get; init; }
    public string Detail { get; init; } = string.Empty;
    public bool IsMapped => Package != null && Detail.Length == 0;

    public static NetherStrategyEvidenceMapResult Success(NetherStrategyEvidencePackage package) =>
        new() { Package = package };

    public static NetherStrategyEvidenceMapResult Failure(string detail) =>
        new() { Detail = detail };
}

/// <summary>
/// Deep-copy mapper for the public policy seam.  The authoritative server identity is the only
/// package-wide requirement; malformed optional evidence invalidates just that component.
/// </summary>
internal static class NetherStrategyEvidenceMapper
{
    private static readonly NetherCodeFamily[] Families =
    {
        NetherCodeFamily.Rush,
        NetherCodeFamily.Impact,
        NetherCodeFamily.Safe,
        NetherCodeFamily.Risk,
    };

    public static NetherStrategyEvidenceMapResult Map(NetherStrategyEvidenceMapRequest? request)
    {
        if (request?.Snapshot == null)
            return NetherStrategyEvidenceMapResult.Failure("authoritative-strategy-snapshot-unavailable");

        NetherSnapshot snapshot = request.Snapshot;
        NetherStrategyEvidenceIdentity identity = request.Identity;
        if (identity.RuntimeGeneration <= 0
            || identity.ControllerOwnerGeneration != identity.RuntimeGeneration
            || identity.EnteredSubsceneGeneration != identity.RuntimeGeneration)
        {
            return NetherStrategyEvidenceMapResult.Failure("invalid-strategy-evidence-owner-binding");
        }
        if (identity.SnapshotFingerprint != snapshot.Fingerprint)
            return NetherStrategyEvidenceMapResult.Failure("strategy-evidence-snapshot-binding-mismatch");
        if (snapshot.NetherId <= 0 || snapshot.MapId <= 0
            || snapshot.MasterMaxFloorLevel < 1
            || snapshot.AuthoritativeBossFloorLevels == null
            || snapshot.AuthoritativeBossFloorLevels.Count == 0)
        {
            return NetherStrategyEvidenceMapResult.Failure("invalid-authoritative-strategy-snapshot");
        }

        var package = new NetherStrategyEvidencePackage
        {
            Identity = identity,
            Server = CopyServer(snapshot),
            Party = MapParty(request.Party, request.PartyUnknownReason),
            OwnedCodes = MapCodes(request.OwnedCodes, request.OwnedCodesUnknownReason),
            Research = MapResearch(request.Research, request.ResearchUnknownReason),
            NativeMechanics = MapMechanics(
                request.NativeMechanics,
                request.NativeMechanicsUnknownReason
            ),
            VisibleMap = MapVisible(request.VisibleMap, request.VisibleMapUnknownReason),
            DisplayDiagnostics = CopyDiagnostics(request.DisplayDiagnostics),
        };
        return NetherStrategyEvidenceMapResult.Success(package);
    }

    private static NetherStrategyServerEvidence CopyServer(NetherSnapshot snapshot) => new()
    {
        Status = snapshot.Status,
        NetherId = snapshot.NetherId,
        MapId = snapshot.MapId,
        CurrentFloorId = snapshot.CurrentFloorId,
        CurrentNodeId = snapshot.CurrentNodeId,
        FloorLevel = snapshot.FloorLevel,
        FloorIndex = snapshot.FloorIndex,
        ErosionPoint = snapshot.ErosionPoint,
        TicketCount = snapshot.TicketCount,
        SignalCount = snapshot.SignalCount,
        TreasureKeyCount = snapshot.TreasureKeyCount,
        NetherGold = snapshot.NetherGold,
        CodeCapacity = snapshot.CodeCapacity,
        CodeReloadCount = snapshot.CodeReloadCount,
        RecoveryFloorLevel = snapshot.RecoveryFloorLevel,
        MasterMaxFloorLevel = snapshot.MasterMaxFloorLevel,
        BossFloorLevels = ReadOnly(snapshot.AuthoritativeBossFloorLevels.ToArray()),
    };

    private static NetherStrategyEvidenceComponent<NetherStrategyPartyProfile> MapParty(
        IReadOnlyList<NetherStrategyPartyMember>? source,
        string sourceUnknownReason
    )
    {
        if (source == null)
            return NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Unknown(
                ExactOrFallback(sourceUnknownReason, "party-profile-unavailable")
            );
        if (source.Count == 0)
            return NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Unknown("party-profile-empty");

        var members = new List<NetherStrategyPartyMember>(source.Count);
        var characterIds = new HashSet<long>();
        var partyIndexes = new HashSet<int>();
        foreach (NetherStrategyPartyMember? member in source)
        {
            if (member == null || member.CharacterId <= 0 || member.PartyIndex < 0
                || member.PartyPosition < 0 || member.ElementType < 0 || member.ManaType < 0
                || member.HpPermille is < 0 or > 1000 || member.Level < 1
                || member.LimitBreakCount < 0 || !characterIds.Add(member.CharacterId)
                || !partyIndexes.Add(member.PartyIndex))
            {
                return NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Unknown(
                    "invalid-party-profile-member"
                );
            }
            if (!TryCopyNamed(member.NativeParameters, out IReadOnlyList<NetherStrategyNamedValue>? parameters)
                || !TryCopyEffects(member.CharacterAbilityEffects, out IReadOnlyList<NetherStrategyAbilityEffect>? character)
                || !TryCopyEffects(member.EquipmentAbilityEffects, out IReadOnlyList<NetherStrategyAbilityEffect>? equipment)
                || !TryCopyEffects(member.GeneralAbilityEffects, out IReadOnlyList<NetherStrategyAbilityEffect>? general))
            {
                return NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Unknown(
                    "invalid-party-profile-native-input"
                );
            }
            members.Add(member with
            {
                NativeParameters = parameters!,
                CharacterAbilityEffects = character!,
                EquipmentAbilityEffects = equipment!,
                GeneralAbilityEffects = general!,
            });
        }
        return NetherStrategyEvidenceComponent<NetherStrategyPartyProfile>.Known(
            new NetherStrategyPartyProfile(ReadOnly(members.ToArray()))
        );
    }

    private static NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence> MapCodes(
        NetherStrategyOwnedCodeEvidence? source,
        string sourceUnknownReason
    )
    {
        if (source == null)
            return NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence>.Unknown(
                ExactOrFallback(sourceUnknownReason, "owned-code-evidence-unavailable")
            );
        if (source.Codes == null || source.CategorySkills == null
            || source.Capacity < 1 || source.Rerolls < 0)
        {
            return NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence>.Unknown("invalid-owned-code-contract");
        }

        NetherCodeState[] positive = source.Codes
            .Where(code => code != null && code.PossessionAmount > 0)
            .Select(code => code with { })
            .OrderBy(code => code.CodeId)
            .ToArray();
        if (positive.Any(code => code.CodeId <= 0 || code.Family == NetherCodeFamily.Unknown)
            || positive.GroupBy(code => code.CodeId).Any(group => group.Count() != 1)
            || positive.Length > source.Capacity)
        {
            return NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence>.Unknown("invalid-positive-owned-code-set");
        }

        var skills = new List<NetherStrategyCategorySkill>(source.CategorySkills.Count);
        foreach (NetherStrategyCategorySkill? skill in source.CategorySkills)
        {
            if (skill == null || skill.SkillId <= 0 || skill.Counter < 1
                || skill.Family == NetherCodeFamily.Unknown)
            {
                return NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence>.Unknown("invalid-code-category-skill-row");
            }
            skills.Add(skill with { });
        }
        if (skills.GroupBy(skill => skill.SkillId).Any(group => group.Count() != 1))
            return NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence>.Unknown("duplicate-code-category-skill-row");

        NetherStrategyDecodedCodeEffect[] effects = positive.Select(code =>
            new NetherStrategyDecodedCodeEffect(
                code.CodeId,
                code.MasterEffectType,
                code.EffectParameter1,
                code.EffectParameter2,
                code.EffectParameter3,
                code.AbilityAssetId,
                code.EffectSemanticsKnown,
                code.EffectSemanticsKnown ? string.Empty : "code-effect-semantics-unknown:" + code.CodeId
            )
        ).ToArray();
        NetherStrategyFamilyCount[] counts = Families.Select(family =>
        {
            int owned = positive.Count(code => code.Family == family);
            int opposing = positive.Count(code => code.Family == Opposing(family));
            return new NetherStrategyFamilyCount(family, owned, opposing, Math.Max(0, owned - opposing));
        }).ToArray();

        return NetherStrategyEvidenceComponent<NetherStrategyOwnedCodeEvidence>.Known(
            new NetherStrategyOwnedCodeEvidence(ReadOnly(positive), source.Capacity, source.Rerolls)
            {
                DecodedEffects = ReadOnly(effects),
                CategorySkills = ReadOnly(skills.ToArray()),
                FamilyCounts = ReadOnly(counts),
            }
        );
    }

    private static NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence> MapResearch(
        IReadOnlyList<NetherStrategyResearchFamilyState>? source,
        string sourceUnknownReason
    )
    {
        if (source == null)
            return NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence>.Unknown(
                ExactOrFallback(sourceUnknownReason, "research-evidence-unavailable")
            );
        NetherStrategyResearchFamilyState[] copied = source.ToArray();
        if (copied.Length != Families.Length
            || copied.Any(row => !Families.Contains(row.Family)
                || row.WalletPoints < 0
                || row.ProjectedNormalSettlementPoints < 0
                || row.TechnologyResearchRate < 0
                || row.SettlementAcquiredCodeCount < 0
                || !row.IsProjectedNormalSettlementKnown
                    && string.IsNullOrWhiteSpace(row.ProjectionUnknownReason))
            || copied.GroupBy(row => row.Family).Any(group => group.Count() != 1))
        {
            return NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence>.Unknown("invalid-research-family-evidence");
        }
        copied = copied.OrderBy(row => (int)row.Family).ToArray();
        return NetherStrategyEvidenceComponent<NetherStrategyResearchEvidence>.Known(
            new NetherStrategyResearchEvidence(ReadOnly(copied))
        );
    }

    private static NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence> MapMechanics(
        IReadOnlyList<NetherStrategyNativeMechanic>? source,
        string sourceUnknownReason
    )
    {
        if (source == null)
            return NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>.Unknown(
                ExactOrFallback(sourceUnknownReason, "native-mechanics-unavailable")
            );
        var copied = new List<NetherStrategyNativeMechanic>(source.Count);
        foreach (NetherStrategyNativeMechanic? mechanic in source)
        {
            NetherStrategyAbilityEffectEvidence abilityEffectSource = mechanic?.AbilityEffect
                ?? new NetherStrategyAbilityEffectEvidence(NetherStrategyAbilityEffectKind.Unknown);
            if (mechanic is { IsKnown: false }
                && !abilityEffectSource.IsKnown
                && string.IsNullOrWhiteSpace(abilityEffectSource.UnknownReason)
                && !string.IsNullOrWhiteSpace(mechanic.UnknownReason))
            {
                // A runtime row can be component-locally unknown before an effect subtype exists.
                // Retain the exact row error on that smallest nested component instead of
                // collapsing the complete mechanics collection into a generic invalid row.
                abilityEffectSource = abilityEffectSource with
                {
                    ParametersKnown = false,
                    ParameterUnknownReason = mechanic.UnknownReason,
                    UnknownReason = mechanic.UnknownReason,
                };
            }
            if (mechanic == null || mechanic.MechanicId <= 0
                || mechanic.Triggers == null
                || mechanic.BuffStrategies == null
                || mechanic.Duration < 0 || mechanic.Cap < 0
                || !TryCopyAbilityEffect(
                    abilityEffectSource,
                    out NetherStrategyAbilityEffectEvidence abilityEffect
                )
                || !TryCopyBuffStrategies(
                    mechanic.BuffStrategies,
                    out IReadOnlyList<NetherStrategyBuffEvidence>? buffStrategies
                )
                || mechanic.IsKnown
                    && (mechanic.SourceEffectType == NetherCodeMasterEffectType.Unknown
                        || mechanic.Triggers.Count == 0
                        || mechanic.Triggers.Any(trigger => !trigger.IsKnown)
                        || !mechanic.Target.IsKnown
                        || !mechanic.AbilityEffect.IsKnown)
                || !mechanic.IsKnown && string.IsNullOrWhiteSpace(mechanic.UnknownReason))
            {
                return NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>.Unknown("invalid-native-mechanic-row");
            }
            copied.Add(mechanic with
            {
                Triggers = ReadOnly(mechanic.Triggers.Select(CopyTrigger).ToArray()),
                AbilityEffect = abilityEffect,
                BuffStrategies = buffStrategies!,
            });
        }
        if (copied.GroupBy(row => row.MechanicId).Any(group => group.Count() != 1))
            return NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>.Unknown("duplicate-native-mechanic-row");
        return NetherStrategyEvidenceComponent<NetherStrategyNativeMechanicsEvidence>.Known(
            new NetherStrategyNativeMechanicsEvidence(ReadOnly(copied.ToArray()))
        );
    }

    private static NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence> MapVisible(
        NetherStrategyVisibleMapEvidence? source,
        string sourceUnknownReason
    )
    {
        if (source == null)
            return NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Unknown(
                ExactOrFallback(sourceUnknownReason, "visible-map-evidence-unavailable")
            );
        if (source.Floors == null || source.ContentRows == null)
            return NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Unknown("invalid-visible-map-contract");

        NetherFloorNode[] floors = source.Floors
            .Where(floor => floor != null && !floor.IsHidden && floor.IsUnlocked)
            .Select(floor => floor with
            {
                PreviousFloorIds = ReadOnly((floor.PreviousFloorIds ?? Array.Empty<long>()).ToArray()),
            })
            .OrderBy(floor => floor.FloorLevel)
            .ThenBy(floor => floor.ApiFloorIndex)
            .ToArray();
        if (floors.Any(floor => floor.NodeId <= 0 || floor.FloorId <= 0)
            || floors.GroupBy(floor => floor.NodeId).Any(group => group.Count() != 1))
        {
            return NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Unknown("invalid-visible-floor-row");
        }
        var nodeIds = new HashSet<long>(floors.Select(floor => floor.NodeId));
        var rows = new List<NetherStrategyVisibleContentRow>();
        foreach (NetherStrategyVisibleContentRow? row in source.ContentRows)
        {
            if (row == null || row.Kind == NetherStrategyVisibleContentKind.Unknown
                || row.NodeId <= 0 || row.MasterRowId < 0
                || row.MasterRowId == 0 && row.IsKnown
                || row.ContentId < 0
                || row.ContentId == 0
                    && row.IsKnown
                    && row.Kind is not (
                        NetherStrategyVisibleContentKind.Resource
                        or NetherStrategyVisibleContentKind.ShopInventory
                    )
                || !nodeIds.Contains(row.NodeId)
                || row.Amount < 0 || row.Cost < 0 || row.Rank < 0 || row.Weight < 0
                || !TryCopyNamed(row.RawValues, out IReadOnlyList<NetherStrategyNamedValue>? rawValues)
                || !TryCopyEventOptions(
                    row.EventOptions,
                    out IReadOnlyList<NetherStrategyVisibleEventOptionEvidence>? eventOptions
                )
                || !row.IsKnown && string.IsNullOrWhiteSpace(row.UnknownReason))
            {
                return NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Unknown("invalid-visible-content-row");
            }
            rows.Add(row with
            {
                RawValues = rawValues!,
                EventOptions = eventOptions!,
            });
        }
        return NetherStrategyEvidenceComponent<NetherStrategyVisibleMapEvidence>.Known(
            new NetherStrategyVisibleMapEvidence(ReadOnly(floors), ReadOnly(rows.ToArray()))
        );
    }

    private static IReadOnlyList<NetherStrategyDisplayDiagnostic> CopyDiagnostics(
        IReadOnlyList<NetherStrategyDisplayDiagnostic>? source
    ) => ReadOnly((source ?? Array.Empty<NetherStrategyDisplayDiagnostic>())
        .Where(row => row.CodeId > 0)
        .ToArray());

    private static string ExactOrFallback(string exact, string fallback) =>
        string.IsNullOrWhiteSpace(exact) ? fallback : exact;

    private static NetherStrategyTriggerEvidence CopyTrigger(
        NetherStrategyTriggerEvidence trigger
    ) => trigger with
    {
        ControlRelationships = trigger.ControlRelationships with
        {
            LevelProbabilityPermille = trigger.ControlRelationships.LevelProbabilityPermille.ToArray(),
            ExecuteCountLimit = trigger.ControlRelationships.ExecuteCountLimit == null
                ? null
                : trigger.ControlRelationships.ExecuteCountLimit with
                {
                    LevelCountLimits = trigger.ControlRelationships.ExecuteCountLimit.LevelCountLimits.ToArray(),
                },
            SituationCosts = trigger.ControlRelationships.SituationCosts.Select(cost => cost with
            {
                LevelBuffTypes = cost.LevelBuffTypes.ToArray(),
                LevelStacks = cost.LevelStacks.ToArray(),
            }).ToArray(),
        },
    };

    private static bool TryCopyEventOptions(
        IReadOnlyList<NetherStrategyVisibleEventOptionEvidence>? source,
        out IReadOnlyList<NetherStrategyVisibleEventOptionEvidence>? copied
    )
    {
        copied = null;
        if (source == null)
            return false;
        var options = new List<NetherStrategyVisibleEventOptionEvidence>(source.Count);
        foreach (NetherStrategyVisibleEventOptionEvidence? option in source)
        {
            if (option == null || option.OptionNumber < 1 || option.EventPartId <= 0
                || option.Effects == null || option.Effects.Count != 4)
            {
                return false;
            }
            NetherStrategyVisibleEventEffectEvidence[] effects = option.Effects.ToArray();
            if (effects.Any(effect => effect == null
                    || effect.Source == NetherStrategyVisibleEventEffectSource.Unknown
                    || !effect.IsKnown && string.IsNullOrWhiteSpace(effect.UnknownReason)))
            {
                return false;
            }
            options.Add(option with { Effects = effects });
        }
        copied = options.ToArray();
        return true;
    }

    private static bool TryCopyNamed(
        IReadOnlyList<NetherStrategyNamedValue>? source,
        out IReadOnlyList<NetherStrategyNamedValue>? copied
    )
    {
        copied = null;
        if (source == null)
            return false;
        NetherStrategyNamedValue[] values = source.ToArray();
        if (values.Any(value => string.IsNullOrWhiteSpace(value.Name))
            || values.GroupBy(value => value.Name, StringComparer.Ordinal).Any(group => group.Count() != 1))
            return false;
        copied = ReadOnly(values);
        return true;
    }

    private static bool TryCopyEffects(
        IReadOnlyList<NetherStrategyAbilityEffect>? source,
        out IReadOnlyList<NetherStrategyAbilityEffect>? copied
    )
    {
        copied = null;
        if (source == null)
            return false;
        NetherStrategyAbilityEffect[] values = source.ToArray();
        if (values.Any(value => value.EffectId <= 0 || value.Level < 0)
            || values.GroupBy(value => value.EffectId).Any(group => group.Count() != 1))
            return false;
        copied = ReadOnly(values);
        return true;
    }

    private static bool TryCopyBuffStrategies(
        IReadOnlyList<NetherStrategyBuffEvidence>? source,
        out IReadOnlyList<NetherStrategyBuffEvidence>? copied
    )
    {
        copied = null;
        if (source == null)
            return false;
        var seen = new HashSet<int>();
        var values = new List<NetherStrategyBuffEvidence>(source.Count);
        foreach (NetherStrategyBuffEvidence? strategy in source)
        {
            if (strategy == null
                || !strategy.BuffType.IsKnown
                || !seen.Add(strategy.BuffType.Value)
                || strategy.AdditionalMatchedTypes == null
                || strategy.AdditionalMatchedTypes.Any(type => !type.IsKnown)
                || strategy.AdditionalMatchedTypes
                    .GroupBy(type => type.Value)
                    .Any(group => group.Count() != 1)
                || strategy.IsKnown
                    && (strategy.EffectKind == NetherStrategyBuffEffectKind.Unknown
                        || strategy.StatusPriority == NetherStrategyStatusPriorityKind.Unknown
                        || strategy.Coexistence == NetherStrategyBuffCoexistenceKind.Unknown)
                || !strategy.IsKnown && string.IsNullOrWhiteSpace(strategy.UnknownReason))
            {
                return false;
            }
            values.Add(strategy with
            {
                AdditionalMatchedTypes = ReadOnly(strategy.AdditionalMatchedTypes.ToArray()),
            });
        }
        copied = ReadOnly(values.ToArray());
        return true;
    }

    private static bool TryCopyAbilityEffect(
        NetherStrategyAbilityEffectEvidence source,
        out NetherStrategyAbilityEffectEvidence copied
    )
    {
        copied = source;
        if (source.BuffParameters == null
            || source.Conditions == null
            || !source.IsKnown && string.IsNullOrWhiteSpace(source.UnknownReason))
        {
            return false;
        }
        var parameters = new List<NetherStrategyBuffParameterEvidence>(source.BuffParameters.Count);
        foreach (NetherStrategyBuffParameterEvidence? parameter in source.BuffParameters)
        {
            if (parameter == null || !TryCopyBuffParameter(parameter, out NetherStrategyBuffParameterEvidence? clone))
                return false;
            parameters.Add(clone!);
        }
        NetherStrategyBuffConditionEvidence[] conditions = source.Conditions.ToArray();
        if (conditions.Any(condition => !condition.IsKnown && string.IsNullOrWhiteSpace(condition.UnknownReason)))
            return false;

        NetherStrategyLinkedBuffThresholdEvidence? min = null;
        if (source.MinLinkedBuff != null)
        {
            if (!TryCopyBuffParameter(source.MinLinkedBuff.BuffParameter, out NetherStrategyBuffParameterEvidence? parameter))
                return false;
            min = new NetherStrategyLinkedBuffThresholdEvidence(source.MinLinkedBuff.PerMille, parameter!);
        }
        NetherStrategyLinkedBuffThresholdEvidence? max = null;
        if (source.MaxLinkedBuff != null)
        {
            if (!TryCopyBuffParameter(source.MaxLinkedBuff.BuffParameter, out NetherStrategyBuffParameterEvidence? parameter))
                return false;
            max = new NetherStrategyLinkedBuffThresholdEvidence(source.MaxLinkedBuff.PerMille, parameter!);
        }
        copied = source with
        {
            BuffParameters = ReadOnly(parameters.ToArray()),
            Conditions = ReadOnly(conditions),
            MinLinkedBuff = min,
            MaxLinkedBuff = max,
        };
        return true;
    }

    private static bool TryCopyBuffParameter(
        NetherStrategyBuffParameterEvidence source,
        out NetherStrategyBuffParameterEvidence? copied
    )
    {
        copied = null;
        if (!source.BuffType.IsKnown
            || source.IsKnown && !source.ParameterReference.IsKnown
            || !source.IsKnown && string.IsNullOrWhiteSpace(source.UnknownReason))
        {
            return false;
        }
        NetherStrategyBuffTargetFilterEvidence? filter = source.TargetFilter;
        if (filter != null)
        {
            if (filter.RequiredBuffTypes == null
                || filter.RequiredBuffTypes.Any(type => !type.IsKnown))
            {
                return false;
            }
            filter = filter with
            {
                RequiredBuffTypes = ReadOnly(filter.RequiredBuffTypes.ToArray()),
            };
        }
        copied = source with { TargetFilter = filter };
        return true;
    }

    private static NetherCodeFamily Opposing(NetherCodeFamily family) => family switch
    {
        NetherCodeFamily.Rush => NetherCodeFamily.Impact,
        NetherCodeFamily.Impact => NetherCodeFamily.Rush,
        NetherCodeFamily.Safe => NetherCodeFamily.Risk,
        NetherCodeFamily.Risk => NetherCodeFamily.Safe,
        _ => NetherCodeFamily.Unknown,
    };

    private static IReadOnlyList<T> ReadOnly<T>(T[] values) => Array.AsReadOnly(values);
}

internal readonly record struct NetherStrategyEvidenceAcceptanceDecision(
    bool IsAccepted,
    string Detail
);

/// <summary>Pure controller-acceptance seam; it performs no reflection or native action.</summary>
internal static class NetherStrategyEvidenceAcceptance
{
    public static NetherStrategyEvidenceAcceptanceDecision Evaluate(
        NetherStrategyEvidencePackage? package,
        long currentRuntimeGeneration,
        long currentControllerOwnerGeneration,
        long currentEnteredSubsceneGeneration,
        NetherSnapshotFingerprint currentAuthoritativeSnapshot
    )
    {
        if (package == null)
            return Reject("strategy-evidence-package-unavailable");

        NetherStrategyEvidenceIdentity identity = package.Identity;
        if (identity.RuntimeGeneration <= 0
            || identity.RuntimeGeneration != currentRuntimeGeneration)
        {
            return Reject("strategy-evidence-runtime-generation-mismatch");
        }
        if (identity.ControllerOwnerGeneration <= 0
            || identity.ControllerOwnerGeneration != currentControllerOwnerGeneration)
        {
            return Reject("strategy-evidence-controller-owner-mismatch");
        }
        if (identity.EnteredSubsceneGeneration <= 0
            || identity.EnteredSubsceneGeneration != currentEnteredSubsceneGeneration)
        {
            return Reject("strategy-evidence-entered-subscene-mismatch");
        }
        if (identity.SnapshotFingerprint != currentAuthoritativeSnapshot)
            return Reject("strategy-evidence-authoritative-snapshot-mismatch");

        return new NetherStrategyEvidenceAcceptanceDecision(true, string.Empty);
    }

    private static NetherStrategyEvidenceAcceptanceDecision Reject(string detail) =>
        new(false, detail);
}
