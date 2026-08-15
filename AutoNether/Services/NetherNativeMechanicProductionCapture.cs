#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Absf;
using Il2CppInterop.Runtime.InteropTypes;
using Project.Master;
using Project.Master.NoaMessagePack;

namespace AutoNether.Services;

/// <summary>
/// Compile-time native adapter for owned-code mechanic evidence.  RuntimeBridge supplies only the
/// current authoritative code portfolio and master store; all native ability/trigger/buff traversal,
/// typed component-local unknowns, and row assembly remain local to this module.
/// </summary>
internal static class NetherNativeMechanicProductionCapture
{
    public static bool TryCapture(
        IReadOnlyList<NetherCodeState> codes,
        MasterDataStore masterDataStore,
        out IReadOnlyList<NetherStrategyNativeMechanic>? mechanics,
        out string error
    )
    {
        mechanics = null;
        MNetherCodes[]? rows = masterDataStore.GetCache<MNetherCodes>();
        if (rows == null)
        {
            error = "missing-strategy-m-nether-codes-cache";
            return false;
        }
        var rowById = rows
            .Where(row => row != null && row.id > 0)
            .GroupBy(row => row.id)
            .ToDictionary(group => group.Key, group => group.First());
        Project.NetherCodeAbilityAssetDataStore? netherAbilityStore =
            Engine.Get<Project.NetherCodeAbilityAssetDataStore>();
        Project.AbilityAssetDataStore? commonAbilityStore = Engine.Get<Project.AbilityAssetDataStore>();
        TryReadStrategyBuffMap(
            Engine.Get<Project.Ingame.BuffTypeStrategies>(),
            out IReadOnlyDictionary<int, Project.Ingame.IBuffStrategy> strategyByBuffType,
            out string buffMapError
        );

        var capturedByCodeId = new Dictionary<long, NetherStrategyNativeMechanic>();
        foreach (NetherCodeState code in codes
            .Where(code => code != null && code.PossessionAmount > 0)
            .GroupBy(code => code.CodeId)
            .Select(group => group.First())
            .OrderBy(code => code.CodeId))
        {
            if (!rowById.TryGetValue(code.CodeId, out MNetherCodes? row))
                continue;
            NetherCodeMasterEffectType sourceEffectType = MapStrategySourceEffectType(row.effect_type);
            Project.IAbilityEffectData? ability = row.effect_type switch
            {
                (int)NetherCodeMasterEffectType.NetherAbility =>
                    netherAbilityStore?.GetAbilityEffectAsset(row.effect_parameter_1),
                (int)NetherCodeMasterEffectType.CommonAbility =>
                    commonAbilityStore?.GetAbilityEffectAsset(row.effect_parameter_1),
                _ => null,
            };
            bool expectsAbility = row.effect_type is
                (int)NetherCodeMasterEffectType.NetherAbility or
                (int)NetherCodeMasterEffectType.CommonAbility;
            bool known = sourceEffectType != NetherCodeMasterEffectType.Unknown
                && (!expectsAbility || ability != null);
            string unknown = sourceEffectType == NetherCodeMasterEffectType.Unknown
                ? "unsupported-nether-code-effect-type:" + row.id + ":" + row.effect_type
                : known
                    ? string.Empty
                    : "ability-effect-asset-unavailable:" + row.id;
            IReadOnlyList<NetherStrategyTriggerEvidence> triggers = expectsAbility
                ? new[]
                {
                    UnknownStrategyTrigger("ability-effect-asset-unavailable:" + row.id),
                }
                : new[]
                {
                    new NetherStrategyTriggerEvidence(NetherStrategyTriggerKind.NativeRunState)
                    {
                        ParametersKnown = true,
                        NativeTypeIdentity = "Project.Master.NoaMessagePack.MNetherCodes",
                        ControlRelationships = NetherStrategyTriggerControlEvidence.KnownNotApplicable(),
                    },
                };
            NetherStrategyTargetEvidence target = expectsAbility
                ? UnknownStrategyTarget("ability-effect-asset-unavailable:" + row.id)
                : new NetherStrategyTargetEvidence(NetherStrategyTargetKind.NetherRun)
                {
                    ParametersKnown = true,
                    NativeTypeIdentity = "Project.Master.NoaMessagePack.MNetherCodes",
                };
            NetherStrategyAbilityEffectEvidence abilityEffect = expectsAbility
                ? UnknownStrategyAbilityEffect("ability-effect-asset-unavailable:" + row.id)
                : new NetherStrategyAbilityEffectEvidence(
                    NetherStrategyAbilityEffectKind.NativeCodeEffect
                )
                {
                    NativeTypeIdentity = "Project.Master.NoaMessagePack.MNetherCodes",
                };
            IReadOnlyList<NetherStrategyBuffEvidence> typedBuffStrategies =
                Array.Empty<NetherStrategyBuffEvidence>();
            int duration = 0;
            bool durationKnown = false;
            int cap = 0;
            bool capKnown = false;
            if (ability != null)
            {
                if (!TryMapStrategyTriggers(
                        ability.Situations,
                        row.id,
                        out triggers,
                        out string triggerError
                    ))
                {
                    known = false;
                    unknown = triggerError;
                }
                target = MapStrategyTarget(ability.Target);
                if (!target.IsKnown)
                {
                    known = false;
                    unknown = target.UnknownReason;
                }
                NetherStrategyTriggerEvidence durationTrigger = triggers.FirstOrDefault(
                    trigger => trigger.Kind == NetherStrategyTriggerKind.Duration
                        && trigger.ParametersKnown
                );
                if (durationTrigger.IsKnown)
                {
                    duration = durationTrigger.Parameter1;
                    durationKnown = true;
                }
                NetherStrategyTriggerEvidence actionCount = triggers.FirstOrDefault(
                    trigger => trigger.Kind == NetherStrategyTriggerKind.ActionCount
                        && trigger.ParametersKnown
                );
                if (actionCount.IsKnown)
                {
                    cap = actionCount.Parameter1;
                    capKnown = true;
                }
                object? effect = row.effect_parameter_2 is > 0 and <= int.MaxValue
                    ? ability.GetAbilityEffect(checked((int)row.effect_parameter_2), 0)
                    : null;
                if (effect == null)
                {
                    known = false;
                    unknown = "ability-effect-level-unavailable:" + row.id;
                    abilityEffect = UnknownStrategyAbilityEffect(unknown);
                }
                else
                {
                    abilityEffect = MapStrategyAbilityEffect(effect);
                    if (!abilityEffect.IsKnown)
                    {
                        known = false;
                        unknown = abilityEffect.UnknownReason;
                    }
                    if (!TryReadStrategyBuffTypes(effect, out IReadOnlyList<int> buffTypes, out string buffTypeError))
                    {
                        known = false;
                        unknown = "buff-type-evidence:" + row.id + ":" + buffTypeError;
                    }
                    else
                    {
                        typedBuffStrategies = buffTypes.Select(buffType =>
                            MapStrategyBuff(
                                row.id,
                                buffType,
                                strategyByBuffType,
                                buffMapError
                            )
                        ).ToArray();
                        NetherStrategyBuffEvidence? unknownBuff = typedBuffStrategies.FirstOrDefault(
                            strategy => !strategy.IsKnown
                        );
                        if (unknownBuff != null)
                        {
                            known = false;
                            unknown = unknownBuff.UnknownReason;
                        }
                    }
                }
            }
            capturedByCodeId[row.id] = new NetherStrategyNativeMechanic(
                row.id,
                sourceEffectType,
                triggers,
                target
            )
            {
                Duration = duration,
                DurationKnown = durationKnown,
                Cap = cap,
                CapKnown = capKnown,
                AbilityEffect = abilityEffect,
                BuffStrategies = typedBuffStrategies,
                MasterEffectParameter1 = row.effect_parameter_1,
                MasterEffectParameter2 = row.effect_parameter_2,
                MasterEffectParameter3 = row.effect_parameter_3,
                IsKnown = known,
                UnknownReason = unknown,
            };
        }
        mechanics = NetherStrategyNativeMechanicAssembler.AssembleOwnedCodes(
            codes,
            capturedByCodeId
        );
        error = string.Empty;
        return true;
    }

    private static NetherCodeMasterEffectType MapStrategySourceEffectType(int raw) => raw switch
    {
        (int)NetherCodeMasterEffectType.NetherAbility => NetherCodeMasterEffectType.NetherAbility,
        (int)NetherCodeMasterEffectType.CommonAbility => NetherCodeMasterEffectType.CommonAbility,
        (int)NetherCodeMasterEffectType.ErosionAdditionUp => NetherCodeMasterEffectType.ErosionAdditionUp,
        (int)NetherCodeMasterEffectType.ErosionAdditionDown => NetherCodeMasterEffectType.ErosionAdditionDown,
        (int)NetherCodeMasterEffectType.ErosionRateUp => NetherCodeMasterEffectType.ErosionRateUp,
        (int)NetherCodeMasterEffectType.ErosionRateDown => NetherCodeMasterEffectType.ErosionRateDown,
        _ => NetherCodeMasterEffectType.Unknown,
    };

    private static bool TryMapStrategyTriggers(
        object? rawSituations,
        long mechanicId,
        out IReadOnlyList<NetherStrategyTriggerEvidence> triggers,
        out string error
    )
    {
        if (!NetherRuntimeEnumerableReader.TryRead(rawSituations, out List<object> values, out string detail))
        {
            error = "ability-situations-enumeration:" + mechanicId + ":" + detail;
            triggers = new[] { UnknownStrategyTrigger(error) };
            return false;
        }
        if (values.Count == 0)
        {
            error = "ability-situations-empty:" + mechanicId;
            triggers = new[] { UnknownStrategyTrigger(error) };
            return false;
        }
        NetherStrategyTriggerCaptureResult captured =
            NetherStrategyNativeMechanicCaptureMapper.MapTriggers(
                mechanicId,
                values.Select(MapStrategyTrigger).ToArray()
            );
        if (!captured.IsKnown)
        {
            error = captured.UnknownReason;
            triggers = captured.Triggers;
            return false;
        }
        triggers = captured.Triggers;
        error = string.Empty;
        return true;
    }

    private static NetherStrategyTriggerEvidence MapStrategyTrigger(object source)
    {
        string identity = RuntimeTypeIdentifier(source);
        if (source is not Project.BattleSituations.BattleSituationBase native)
            return UnknownStrategyTrigger("unsupported-ability-situation-type:" + identity);
        NetherStrategyNativeTriggerCapture subtype = source switch
        {
            Project.BattleSituations.BattleSituationAboveErosion value =>
                KnownTriggerCapture(NetherStrategyTriggerKind.AboveErosion, identity, value.Percent),
            Project.BattleSituations.BattleSituationActionCount value =>
                KnownTriggerCapture(NetherStrategyTriggerKind.ActionCount, identity, value.Count, (int)value.SkillTypeFlag),
            Project.BattleSituations.BattleSituationActivateForceChain => KnownTriggerCapture(NetherStrategyTriggerKind.ActivateForceChain, identity),
            Project.BattleSituations.BattleSituationAvoidance => KnownTriggerCapture(NetherStrategyTriggerKind.Avoidance, identity),
            Project.BattleSituations.BattleSituationBattleFinish => KnownTriggerCapture(NetherStrategyTriggerKind.BattleFinish, identity),
            Project.BattleSituations.BattleSituationBelowErosion value =>
                KnownTriggerCapture(NetherStrategyTriggerKind.BelowErosion, identity, value.Percent),
            Project.BattleSituations.BattleSituationBelowHp value =>
                KnownTriggerCapture(NetherStrategyTriggerKind.BelowHp, identity, value.HpThreshold),
            Project.BattleSituations.BattleSituationBuiltIn => KnownTriggerCapture(NetherStrategyTriggerKind.BuiltIn, identity),
            Project.BattleSituations.BattleSituationCritical => KnownTriggerCapture(NetherStrategyTriggerKind.Critical, identity),
            Project.BattleSituations.BattleSituationDeadEnemy => KnownTriggerCapture(NetherStrategyTriggerKind.DeadEnemy, identity),
            Project.BattleSituations.BattleSituationDestroyEnemy => KnownTriggerCapture(NetherStrategyTriggerKind.DestroyEnemy, identity),
            Project.BattleSituations.BattleSituationDuration value =>
                KnownTriggerCapture(NetherStrategyTriggerKind.Duration, identity, value.MilliSec),
            Project.BattleSituations.BattleSituationExceedHp value =>
                KnownTriggerCapture(NetherStrategyTriggerKind.ExceedHp, identity, value.HpThreshold),
            Project.BattleSituations.BattleSituationGameStart => KnownTriggerCapture(NetherStrategyTriggerKind.GameStart, identity),
            Project.BattleSituations.BattleSituationGiveDamage => KnownTriggerCapture(NetherStrategyTriggerKind.GiveDamage, identity),
            Project.BattleSituations.BattleSituationGiveRecovery => KnownTriggerCapture(NetherStrategyTriggerKind.GiveRecovery, identity),
            Project.BattleSituations.BattleSituationImmediateExecution => KnownTriggerCapture(NetherStrategyTriggerKind.ImmediateExecution, identity),
            Project.BattleSituations.BattleSituationOreMining => KnownTriggerCapture(NetherStrategyTriggerKind.OreMining, identity),
            Project.BattleSituations.BattleSituationOtherAllyActivateActionSkill => KnownTriggerCapture(NetherStrategyTriggerKind.OtherAllyActivateActionSkill, identity),
            Project.BattleSituations.BattleSituationReceiveAbnormal => KnownTriggerCapture(NetherStrategyTriggerKind.ReceiveAbnormal, identity),
            Project.BattleSituations.BattleSituationReceiveBuff value =>
                KnownTriggerCapture(NetherStrategyTriggerKind.ReceiveBuff, identity, (int)value.BuffType, value.StackCount),
            Project.BattleSituations.BattleSituationReceiveDamage => KnownTriggerCapture(NetherStrategyTriggerKind.ReceiveDamage, identity),
            Project.BattleSituations.BattleSituationReceiveRecovery => KnownTriggerCapture(NetherStrategyTriggerKind.ReceiveRecovery, identity),
            Project.BattleSituations.BattleSituationServantSummonExist => KnownTriggerCapture(NetherStrategyTriggerKind.ServantSummonExist, identity),
            Project.BattleSituations.BattleSituationServantSummonLeave => KnownTriggerCapture(NetherStrategyTriggerKind.ServantSummonLeave, identity),
            Project.BattleSituations.BattleSituationSpendBuff value =>
                KnownTriggerCapture(NetherStrategyTriggerKind.SpendBuff, identity, (int)value.BuffType),
            Project.BattleSituations.BattleSituationStartBattle => KnownTriggerCapture(NetherStrategyTriggerKind.StartBattle, identity),
            _ => new NetherStrategyNativeTriggerCapture(NetherStrategyTriggerKind.Unknown, identity)
            {
                UnknownReason = "unsupported-ability-situation-type:" + identity,
            },
        };
        return NetherStrategyNativeMechanicCaptureMapper.MapTrigger(
            CaptureStrategyTriggerControl(native, subtype)
        );
    }

    private static NetherStrategyNativeTriggerCapture KnownTriggerCapture(
        NetherStrategyTriggerKind kind,
        string identity,
        int parameter1 = 0,
        int parameter2 = 0,
        int parameter3 = 0
    ) => new(kind, identity)
    {
        Parameter1 = parameter1,
        Parameter2 = parameter2,
        Parameter3 = parameter3,
        ParametersKnown = true,
    };

    private static NetherStrategyNativeTriggerCapture CaptureStrategyTriggerControl(
        Project.BattleSituations.BattleSituationBase source,
        NetherStrategyNativeTriggerCapture capture
    )
    {
        NetherStrategyTriggerProbabilityType probabilityType = source._probabilityType switch
        {
            Project.BattleSituations.BattleSituationBase.ProbabilityType.Fixed =>
                NetherStrategyTriggerProbabilityType.Fixed,
            Project.BattleSituations.BattleSituationBase.ProbabilityType.AbilityLevel =>
                NetherStrategyTriggerProbabilityType.AbilityLevel,
            _ => NetherStrategyTriggerProbabilityType.Unknown,
        };
        IReadOnlyList<int> levelProbability = Array.Empty<int>();
        string unknown = probabilityType == NetherStrategyTriggerProbabilityType.Unknown
            ? "unsupported-trigger-probability-type:" + (int)source._probabilityType
            : string.Empty;
        if (probabilityType == NetherStrategyTriggerProbabilityType.AbilityLevel)
        {
            Project.BattleSituations.BattleSituationBase.LevelBasedProbability? levels =
                source._levelBasedProbability;
            if (levels == null)
                unknown = "level-based-trigger-probability-unavailable:" + capture.NativeTypeIdentity;
            else
            {
                levelProbability =
                [
                    levels._level1,
                    levels._level2,
                    levels._level3,
                    levels._level4,
                    levels._level5,
                    levels._level6,
                    levels._level7,
                    levels._level8,
                    levels._level9,
                    levels._level10,
                ];
            }
        }

        NetherStrategyExecuteCountLimitEvidence? limit =
            MapStrategyExecuteCountLimit(source._executeCountLimit, out string limitError);
        if (unknown.Length == 0 && limitError.Length > 0)
            unknown = limitError;
        IReadOnlyList<NetherStrategySituationCostEvidence> costs =
            MapStrategySituationCosts(source._situationCost, out string costError);
        if (unknown.Length == 0 && costError.Length > 0)
            unknown = costError;
        return capture with
        {
            ProbabilityType = probabilityType,
            FixedProbabilityPermille = source._probabilityPerMille,
            LevelProbabilityPermille = levelProbability,
            ExecuteCountLimit = limit,
            SituationCosts = costs,
            ControlRelationshipsKnown = unknown.Length == 0,
            ControlUnknownReason = unknown,
        };
    }

    private static NetherStrategyExecuteCountLimitEvidence MapStrategyExecuteCountLimit(
        Project.BattleSituations.SituationLimits.ExecuteCountLimit? wrapper,
        out string error
    )
    {
        error = string.Empty;
        if (wrapper?._executeCountLimit == null)
        {
            return new NetherStrategyExecuteCountLimitEvidence(
                NetherStrategyExecuteCountLimitKind.None,
                string.Empty,
                0,
                0,
                Array.Empty<int>()
            );
        }
        Project.BattleSituations.SituationLimits.IExecuteCountLimitLogicFactory factory =
            wrapper._executeCountLimit;
        Project.BattleSituations.SituationLimits.ExecuteCountLimitBattleParameter? battle =
            factory.TryCast<Project.BattleSituations.SituationLimits.ExecuteCountLimitBattleParameter>();
        if (battle != null)
        {
            return MapStrategyExecuteCountLimitParameter(
                NetherStrategyExecuteCountLimitKind.Battle,
                battle._valueType,
                battle._countLimit,
                battle._levelBased,
                RuntimeTypeIdentifier(factory),
                out error
            );
        }
        Project.BattleSituations.SituationLimits.ExecuteCountLimitQuestParameter? quest =
            factory.TryCast<Project.BattleSituations.SituationLimits.ExecuteCountLimitQuestParameter>();
        if (quest != null)
        {
            return MapStrategyExecuteCountLimitParameter(
                NetherStrategyExecuteCountLimitKind.Quest,
                quest._valueType,
                quest._countLimit,
                quest._levelBased,
                RuntimeTypeIdentifier(factory),
                out error
            );
        }
        error = "unsupported-trigger-execute-count-limit:" + RuntimeTypeIdentifier(factory);
        return new NetherStrategyExecuteCountLimitEvidence(
            NetherStrategyExecuteCountLimitKind.Unknown,
            RuntimeTypeIdentifier(factory),
            0,
            0,
            Array.Empty<int>()
        )
        {
            IsKnown = false,
            UnknownReason = error,
        };
    }

    private static NetherStrategyExecuteCountLimitEvidence MapStrategyExecuteCountLimitParameter(
        NetherStrategyExecuteCountLimitKind kind,
        Project.BattleSituations.SituationLimits.ExecuteCountLimit.ValueType valueType,
        Project.BattleSituations.SituationLimits.LevelBasedExecuteCountLimit.LevelParameter? fixedValue,
        Project.BattleSituations.SituationLimits.LevelBasedExecuteCountLimit? levelBased,
        string identity,
        out string error
    )
    {
        int rawValueType = (int)valueType;
        int fixedCount = fixedValue?._countLimit ?? 0;
        IReadOnlyList<int> levels = Array.Empty<int>();
        error = string.Empty;
        if (valueType == Project.BattleSituations.SituationLimits.ExecuteCountLimit.ValueType.Fixed)
        {
            if (fixedValue == null)
                error = "fixed-trigger-execute-count-limit-unavailable:" + identity;
        }
        else if (valueType == Project.BattleSituations.SituationLimits.ExecuteCountLimit.ValueType.AbilityLevel)
        {
            if (levelBased == null
                || levelBased._level1 == null || levelBased._level2 == null
                || levelBased._level3 == null || levelBased._level4 == null
                || levelBased._level5 == null || levelBased._level6 == null
                || levelBased._level7 == null || levelBased._level8 == null
                || levelBased._level9 == null || levelBased._level10 == null)
            {
                error = "level-trigger-execute-count-limit-unavailable:" + identity;
            }
            else
            {
                levels =
                [
                    levelBased._level1._countLimit,
                    levelBased._level2._countLimit,
                    levelBased._level3._countLimit,
                    levelBased._level4._countLimit,
                    levelBased._level5._countLimit,
                    levelBased._level6._countLimit,
                    levelBased._level7._countLimit,
                    levelBased._level8._countLimit,
                    levelBased._level9._countLimit,
                    levelBased._level10._countLimit,
                ];
            }
        }
        else
            error = "unsupported-trigger-execute-count-value-type:" + rawValueType;
        return new NetherStrategyExecuteCountLimitEvidence(
            kind,
            identity,
            rawValueType,
            fixedCount,
            levels
        )
        {
            IsKnown = error.Length == 0,
            UnknownReason = error,
        };
    }

    private static IReadOnlyList<NetherStrategySituationCostEvidence> MapStrategySituationCosts(
        Project.BattleSituations.SituationCosts.SituationCost? wrapper,
        out string error
    )
    {
        error = string.Empty;
        if (wrapper?.SituationCosts == null)
            return Array.Empty<NetherStrategySituationCostEvidence>();
        if (!NetherRuntimeEnumerableReader.TryRead(
                wrapper.SituationCosts,
                out List<object> values,
                out string detail
            ))
        {
            error = "trigger-situation-cost-enumeration:" + detail;
            return Array.Empty<NetherStrategySituationCostEvidence>();
        }
        var costs = new List<NetherStrategySituationCostEvidence>(values.Count);
        foreach (object raw in values)
        {
            if (raw is not Il2CppObjectBase nativeRaw)
            {
                error = "invalid-trigger-situation-cost-runtime-value:" + RuntimeTypeIdentifier(raw);
                return costs;
            }
            Project.BattleSituations.SituationCosts.SituationCostParameterBuffStack? fixedCost =
                nativeRaw.TryCast<Project.BattleSituations.SituationCosts.SituationCostParameterBuffStack>();
            if (fixedCost != null)
            {
                costs.Add(new NetherStrategySituationCostEvidence(
                    NetherStrategySituationCostKind.BuffStack,
                    RuntimeTypeIdentifier(raw),
                    (int)fixedCost._buffType,
                    fixedCost._stack,
                    Array.Empty<int>()
                ));
                continue;
            }
            Project.BattleSituations.SituationCosts.SituationCostParameterBuffStackPerLevel? perLevel =
                nativeRaw.TryCast<Project.BattleSituations.SituationCosts.SituationCostParameterBuffStackPerLevel>();
            if (perLevel != null
                && perLevel._level1 != null && perLevel._level2 != null
                && perLevel._level3 != null && perLevel._level4 != null
                && perLevel._level5 != null && perLevel._level6 != null
                && perLevel._level7 != null && perLevel._level8 != null
                && perLevel._level9 != null && perLevel._level10 != null)
            {
                Project.BattleSituations.SituationCosts.SituationCostParameterBuffStack[] levels =
                [
                    perLevel._level1,
                    perLevel._level2,
                    perLevel._level3,
                    perLevel._level4,
                    perLevel._level5,
                    perLevel._level6,
                    perLevel._level7,
                    perLevel._level8,
                    perLevel._level9,
                    perLevel._level10,
                ];
                costs.Add(new NetherStrategySituationCostEvidence(
                    NetherStrategySituationCostKind.BuffStackPerLevel,
                    RuntimeTypeIdentifier(raw),
                    0,
                    0,
                    levels.Select(level => level._stack).ToArray()
                )
                {
                    LevelBuffTypes = levels.Select(level => (int)level._buffType).ToArray(),
                });
                continue;
            }
            error = perLevel != null
                ? "level-trigger-situation-cost-unavailable:" + RuntimeTypeIdentifier(raw)
                : "unsupported-trigger-situation-cost:" + RuntimeTypeIdentifier(raw);
            costs.Add(new NetherStrategySituationCostEvidence(
                NetherStrategySituationCostKind.Unknown,
                RuntimeTypeIdentifier(raw),
                0,
                0,
                Array.Empty<int>()
            )
            {
                IsKnown = false,
                UnknownReason = error,
            });
            return costs;
        }
        return costs;
    }

    private static NetherStrategyTriggerEvidence UnknownStrategyTrigger(string reason) =>
        new(NetherStrategyTriggerKind.Unknown)
        {
            ParametersKnown = false,
            UnknownReason = reason,
            NativeTypeIdentity = reason.StartsWith("unsupported-ability-situation-type:", StringComparison.Ordinal)
                ? reason["unsupported-ability-situation-type:".Length..]
                : string.Empty,
            ControlRelationships = NetherStrategyTriggerControlEvidence.Unknown(reason),
        };

    private static NetherStrategyTargetEvidence MapStrategyTarget(object? source)
    {
        if (source == null)
            return UnknownStrategyTarget("ability-target-unavailable");
        string identity = RuntimeTypeIdentifier(source);
        NetherStrategyTargetKind kind = source switch
        {
            Project.AbilityTarget.AbilityTargetAction => NetherStrategyTargetKind.Action,
            Project.AbilityTarget.AbilityTargetFriend => NetherStrategyTargetKind.Friend,
            Project.AbilityTarget.AbilityTargetOpponent => NetherStrategyTargetKind.Opponent,
            Project.AbilityTarget.AbilityTargetSelf => NetherStrategyTargetKind.Self,
            Project.AbilityTarget.AbilityTargetSucceedAttack => NetherStrategyTargetKind.SucceedAttack,
            Project.AbilityTarget.AbilityTargetSucceedRecover => NetherStrategyTargetKind.SucceedRecover,
            Project.AbilityTarget.AbilityTargetTemplate => NetherStrategyTargetKind.Template,
            _ => NetherStrategyTargetKind.Unknown,
        };
        if (kind == NetherStrategyTargetKind.Unknown)
            return UnknownStrategyTarget("unsupported-ability-target-type:" + identity);
        if (source is Project.AbilityTarget.AbilityTargetGroupBase group)
        {
            return new NetherStrategyTargetEvidence(kind)
            {
                ElementTypeFlags = (int)group._elementTypeFlag,
                PartyPositionFlags = (int)group._partyPositionFlag,
                UnionTypeFlags = (int)group._unionTypeFlag,
                SearchType = (int)group._searchType,
                RandomCount = group._randomNum,
                ParametersKnown = true,
                NativeTypeIdentity = identity,
            };
        }
        return new NetherStrategyTargetEvidence(kind)
        {
            ParametersKnown = true,
            NativeTypeIdentity = identity,
        };
    }

    private static NetherStrategyTargetEvidence UnknownStrategyTarget(string reason) =>
        new(NetherStrategyTargetKind.Unknown)
        {
            UnknownReason = reason,
            NativeTypeIdentity = reason.StartsWith("unsupported-ability-target-type:", StringComparison.Ordinal)
                ? reason["unsupported-ability-target-type:".Length..]
                : string.Empty,
        };

    private static NetherStrategyAbilityEffectEvidence MapStrategyAbilityEffect(object source)
    {
        string identity = RuntimeTypeIdentifier(source);
        NetherStrategyNativeAbilityEffectCapture capture = source switch
        {
            Project.AbilityEffect.AbilityEffectAbnormalApply value =>
                MapStrategyAbnormalApply(value, identity),
            Project.AbilityEffect.AbilityEffectAbnormalRecovery value =>
                new NetherStrategyNativeAbilityEffectCapture(
                    NetherStrategyAbilityEffectKind.AbnormalRecovery,
                    identity
                )
                {
                    AbnormalType = (int)value.AbnormalType,
                    AbnormalLevel = value.Level,
                    ParametersKnown = true,
                },
            Project.AbilityEffect.AbilityEffectActionPatternChange =>
                UnsupportedStrategyEffectParameters(
                    NetherStrategyAbilityEffectKind.ActionPatternChange,
                    identity
                ),
            Project.AbilityEffect.AbilityEffectAppendSkill =>
                UnsupportedStrategyEffectParameters(
                    NetherStrategyAbilityEffectKind.AppendSkill,
                    identity
                ),
            Project.AbilityEffect.AbilityEffectChargeMana value =>
                new NetherStrategyNativeAbilityEffectCapture(
                    NetherStrategyAbilityEffectKind.ChargeMana,
                    identity
                )
                {
                    ManaEnergy = value.Energy,
                    ParametersKnown = true,
                },
            Project.AbilityEffect.AbilityEffectErosionLinkedBuff value =>
                MapStrategyErosionLinkedBuff(value, identity),
            Project.AbilityEffect.AbilityEffectHpLinkedBuff value =>
                MapStrategyHpLinkedBuff(value, identity),
            Project.AbilityEffect.AbilityEffectParameterBuff value =>
                MapStrategyParameterBuff(value, identity),
            Project.AbilityEffect.AbilityEffectPassiveBuff value =>
                MapStrategyPassiveBuff(value, identity),
            Project.AbilityEffect.AbilityEffectRecoverHp value =>
                new NetherStrategyNativeAbilityEffectCapture(
                    NetherStrategyAbilityEffectKind.RecoverHp,
                    identity
                )
                {
                    RecoverHpHealType = (int)value.HealType,
                    RecoverHpFixedValue = value.FixedValue,
                    RecoverHpStatusSourceType = (int)value.StatusSourceType,
                    RecoverHpRatePermille = value.RatePerMille,
                    RecoverHpMaxHeal = value.MaxHeal,
                    ParametersKnown = true,
                },
            Project.AbilityEffect.AbilityEffectSkillCharge value =>
                new NetherStrategyNativeAbilityEffectCapture(
                    NetherStrategyAbilityEffectKind.SkillCharge,
                    identity
                )
                {
                    SkillChargePermille = value.ChargePermille,
                    ParametersKnown = true,
                },
            Project.AbilityEffect.AbilityEffectStackLinkedBuff value =>
                MapStrategyStackLinkedBuff(value, identity),
            Project.AbilityEffect.AbilityEffectStageFieldManaGainDown value =>
                new NetherStrategyNativeAbilityEffectCapture(
                    NetherStrategyAbilityEffectKind.StageFieldManaGainDown,
                    identity
                )
                {
                    StageFieldReductionPermille = value.ReductionPermille,
                    StageFieldManaGainSourceFlags = (int)value.TargetSources,
                    ParametersKnown = true,
                },
            Project.AbilityEffect.AbilityEffectSummonParameterAdditionRate value =>
                new NetherStrategyNativeAbilityEffectCapture(
                    NetherStrategyAbilityEffectKind.SummonParameterAdditionRate,
                    identity
                )
                {
                    SummonParameterAdditionRatePermille = value.GetParameterAdditionRatePermille(),
                    ParametersKnown = true,
                },
            Project.AbilityEffect.AbilityEffectTemplate =>
                new NetherStrategyNativeAbilityEffectCapture(
                    NetherStrategyAbilityEffectKind.Template,
                    identity
                )
                {
                    ParametersKnown = true,
                },
            _ => new NetherStrategyNativeAbilityEffectCapture(
                NetherStrategyAbilityEffectKind.Unknown,
                identity
            )
            {
                ParameterUnknownReason = "unsupported-ability-effect-type:" + identity,
            },
        };
        return NetherStrategyNativeMechanicCaptureMapper.MapAbilityEffect(capture);
    }

    private static NetherStrategyNativeAbilityEffectCapture MapStrategyAbnormalApply(
        Project.AbilityEffect.AbilityEffectAbnormalApply value,
        string identity
    )
    {
        Project.ValuePermille? probability = value.ApplyProbability;
        return new NetherStrategyNativeAbilityEffectCapture(
            NetherStrategyAbilityEffectKind.AbnormalApply,
            identity
        )
        {
            AbnormalType = (int)value.AbnormalType,
            AbnormalApplyProbabilityPermille = probability?.Value ?? 0,
            AbnormalDurationSeconds = value.Duration,
            ParametersKnown = probability != null,
            ParameterUnknownReason = probability == null
                ? "abnormal-apply-probability-unavailable:" + identity
                : string.Empty,
        };
    }

    private static NetherStrategyNativeAbilityEffectCapture MapStrategyErosionLinkedBuff(
        Project.AbilityEffect.AbilityEffectErosionLinkedBuff value,
        string identity
    )
    {
        if (value.Min?.Effect == null || value.Max?.Effect == null)
        {
            return UnknownStrategyEffectParameters(
                NetherStrategyAbilityEffectKind.ErosionLinkedBuff,
                identity,
                "erosion-linked-buff-parameter-unavailable"
            );
        }
        NetherStrategyNativeBuffParameterCapture min = MapStrategyBuffParameter(value.Min.Effect);
        NetherStrategyNativeBuffParameterCapture max = MapStrategyBuffParameter(value.Max.Effect);
        string error = !min.IsKnown ? min.UnknownReason : !max.IsKnown ? max.UnknownReason : string.Empty;
        return new NetherStrategyNativeAbilityEffectCapture(
            NetherStrategyAbilityEffectKind.ErosionLinkedBuff,
            identity
        )
        {
            MinLinkedBuff = new NetherStrategyLinkedBuffThresholdCapture(value.Min.PerMille, min),
            MaxLinkedBuff = new NetherStrategyLinkedBuffThresholdCapture(value.Max.PerMille, max),
            ParametersKnown = error.Length == 0,
            ParameterUnknownReason = error,
        };
    }

    private static NetherStrategyNativeAbilityEffectCapture MapStrategyHpLinkedBuff(
        Project.AbilityEffect.AbilityEffectHpLinkedBuff value,
        string identity
    )
    {
        if (value.Min?.Effect == null || value.Max?.Effect == null)
        {
            return UnknownStrategyEffectParameters(
                NetherStrategyAbilityEffectKind.HpLinkedBuff,
                identity,
                "hp-linked-buff-parameter-unavailable"
            );
        }
        NetherStrategyNativeBuffParameterCapture min = MapStrategyBuffParameter(value.Min.Effect);
        NetherStrategyNativeBuffParameterCapture max = MapStrategyBuffParameter(value.Max.Effect);
        string error = !min.IsKnown ? min.UnknownReason : !max.IsKnown ? max.UnknownReason : string.Empty;
        return new NetherStrategyNativeAbilityEffectCapture(
            NetherStrategyAbilityEffectKind.HpLinkedBuff,
            identity
        )
        {
            MinLinkedBuff = new NetherStrategyLinkedBuffThresholdCapture(value.Min.PerMille, min),
            MaxLinkedBuff = new NetherStrategyLinkedBuffThresholdCapture(value.Max.PerMille, max),
            ParametersKnown = error.Length == 0,
            ParameterUnknownReason = error,
        };
    }

    private static NetherStrategyNativeAbilityEffectCapture MapStrategyParameterBuff(
        Project.AbilityEffect.AbilityEffectParameterBuff value,
        string identity
    )
    {
        bool mapped = TryMapStrategyBuffParameters(
            value.Buffs,
            out IReadOnlyList<NetherStrategyNativeBuffParameterCapture> parameters,
            out string error
        );
        return new NetherStrategyNativeAbilityEffectCapture(
            NetherStrategyAbilityEffectKind.ParameterBuff,
            identity
        )
        {
            BuffParameters = parameters,
            EndSituationCondition = value.EndSituation == null
                ? 0
                : (int)value.EndSituation.situation,
            EndSituationValue = value.EndSituation?.value ?? 0,
            EndSituationKnown = value.EndSituation != null,
            ParametersKnown = mapped && value.EndSituation != null,
            ParameterUnknownReason = !mapped
                ? error
                : value.EndSituation == null
                    ? "parameter-buff-end-situation-unavailable:" + identity
                    : string.Empty,
        };
    }

    private static NetherStrategyNativeAbilityEffectCapture MapStrategyPassiveBuff(
        Project.AbilityEffect.AbilityEffectPassiveBuff value,
        string identity
    )
    {
        bool parametersMapped = TryMapStrategyBuffParameters(
            value.Buffs,
            out IReadOnlyList<NetherStrategyNativeBuffParameterCapture> parameters,
            out string parameterError
        );
        bool conditionsMapped = TryMapStrategyBuffConditions(
            value.Conditions,
            out IReadOnlyList<NetherStrategyBuffConditionEvidence> conditions,
            out string conditionError
        );
        return new NetherStrategyNativeAbilityEffectCapture(
            NetherStrategyAbilityEffectKind.PassiveBuff,
            identity
        )
        {
            BuffParameters = parameters,
            Conditions = conditions,
            ParametersKnown = parametersMapped && conditionsMapped,
            ParameterUnknownReason = !parametersMapped ? parameterError : conditionError,
        };
    }

    private static NetherStrategyNativeAbilityEffectCapture MapStrategyStackLinkedBuff(
        Project.AbilityEffect.AbilityEffectStackLinkedBuff value,
        string identity
    )
    {
        bool parametersMapped = TryMapStrategyBuffParameters(
            value.Buffs,
            out IReadOnlyList<NetherStrategyNativeBuffParameterCapture> parameters,
            out string parameterError
        );
        bool conditionsMapped = TryMapStrategyBuffConditions(
            value.Conditions,
            out IReadOnlyList<NetherStrategyBuffConditionEvidence> conditions,
            out string conditionError
        );
        int linkedBuffType = (int)value.LinkedBuffType;
        string linkedError = linkedBuffType > 0
            ? string.Empty
            : "invalid-stack-linked-buff-type:" + linkedBuffType;
        return new NetherStrategyNativeAbilityEffectCapture(
            NetherStrategyAbilityEffectKind.StackLinkedBuff,
            identity
        )
        {
            BuffParameters = parameters,
            Conditions = conditions,
            LinkedBuffType = new NetherStrategyBuffType(linkedBuffType),
            LinkedBuffTypeKnown = linkedError.Length == 0,
            ParametersKnown = parametersMapped && conditionsMapped && linkedError.Length == 0,
            ParameterUnknownReason = !parametersMapped
                ? parameterError
                : !conditionsMapped
                    ? conditionError
                    : linkedError,
        };
    }

    private static NetherStrategyNativeAbilityEffectCapture UnsupportedStrategyEffectParameters(
        NetherStrategyAbilityEffectKind kind,
        string identity
    ) => UnknownStrategyEffectParameters(
        kind,
        identity,
        "unsupported-ability-effect-parameter-relationship:" + identity
    );

    private static NetherStrategyNativeAbilityEffectCapture UnknownStrategyEffectParameters(
        NetherStrategyAbilityEffectKind kind,
        string identity,
        string reason
    ) => new(kind, identity)
    {
        ParameterUnknownReason = reason,
    };

    private static bool TryMapStrategyBuffParameters(
        object? source,
        out IReadOnlyList<NetherStrategyNativeBuffParameterCapture> parameters,
        out string error
    )
    {
        if (!NetherRuntimeEnumerableReader.TryRead(source, out List<object> values, out string detail))
        {
            parameters = Array.Empty<NetherStrategyNativeBuffParameterCapture>();
            error = "buff-parameter-enumeration:" + detail;
            return false;
        }
        var mapped = new List<NetherStrategyNativeBuffParameterCapture>(values.Count);
        foreach (object raw in values)
        {
            if (raw is not Project.Ingame.BuffParameterByType parameter)
            {
                parameters = mapped;
                error = "invalid-buff-parameter-entry:" + RuntimeTypeIdentifier(raw);
                return false;
            }
            NetherStrategyNativeBuffParameterCapture capture = MapStrategyBuffParameter(parameter);
            mapped.Add(capture);
            if (!capture.IsKnown)
            {
                parameters = mapped;
                error = capture.UnknownReason;
                return false;
            }
        }
        parameters = mapped;
        error = string.Empty;
        return true;
    }

    private static NetherStrategyNativeBuffParameterCapture MapStrategyBuffParameter(
        Project.Ingame.BuffParameterByType parameter
    )
    {
        int buffType = (int)parameter.buffType;
        if (buffType <= 0)
        {
            return UnknownStrategyBuffParameter(
                buffType,
                "invalid-native-buff-type:" + buffType
            );
        }
        if (!TryMapStrategyBuffTargetFilter(
                parameter.BuffTargetFilter,
                out NetherStrategyBuffTargetFilterEvidence? filter,
                out string filterError
            ))
        {
            return UnknownStrategyBuffParameter(buffType, filterError);
        }
        NetherStrategyBuffParameterReferenceEvidence reference =
            MapStrategyBuffParameterReference(parameter.parameterReference, buffType);
        if (!reference.IsKnown)
            return UnknownStrategyBuffParameter(buffType, reference.UnknownReason, filter, reference);
        return new NetherStrategyNativeBuffParameterCapture(
            new NetherStrategyBuffType(buffType),
            filter,
            reference
        );
    }

    private static NetherStrategyNativeBuffParameterCapture UnknownStrategyBuffParameter(
        int buffType,
        string reason,
        NetherStrategyBuffTargetFilterEvidence? filter = null,
        NetherStrategyBuffParameterReferenceEvidence reference = default
    ) => new(new NetherStrategyBuffType(buffType), filter, reference)
    {
        IsKnown = false,
        UnknownReason = reason,
    };

    private static bool TryMapStrategyBuffTargetFilter(
        Project.Ingame.BuffTargetFilter? filter,
        out NetherStrategyBuffTargetFilterEvidence? evidence,
        out string error
    )
    {
        if (filter == null)
        {
            evidence = null;
            error = string.Empty;
            return true;
        }
        var required = new List<NetherStrategyBuffType>();
        if (filter._buffs != null)
        {
            if (!NetherRuntimeEnumerableReader.TryRead(
                    filter._buffs,
                    out List<object> rawBuffs,
                    out string detail
                ))
            {
                evidence = null;
                error = "buff-target-filter-required-buffs-enumeration:" + detail;
                return false;
            }
            foreach (object raw in rawBuffs)
            {
                if (!TryConvertInt32(raw, out int value) || value <= 0)
                {
                    evidence = null;
                    error = "invalid-buff-target-filter-required-buff";
                    return false;
                }
                required.Add(new NetherStrategyBuffType(value));
            }
        }
        evidence = new NetherStrategyBuffTargetFilterEvidence(
            filter._ignoreDeadUnit,
            (int)filter._elementTypeFlag,
            (int)filter._elementWeakTypeFlag,
            (int)filter._partyPositionFlag,
            (int)filter._unionTypeFlag,
            (int)filter._jobGroupFlag,
            (int)filter._jobSpeciesFlag,
            (int)filter._charaSizeFlag,
            required
        );
        error = string.Empty;
        return true;
    }

    private static NetherStrategyBuffParameterReferenceEvidence MapStrategyBuffParameterReference(
        Project.Ingame.IBuffParameterReference? source,
        int buffType
    )
    {
        if (source == null)
        {
            return UnknownStrategyBuffParameterReference(
                string.Empty,
                "buff-parameter-reference-unavailable:" + buffType
            );
        }
        string identity = RuntimeTypeIdentifier(source);
        Project.Ingame.RatePermilleBuffParameterReferenceBase? rate =
            source.TryCast<Project.Ingame.RatePermilleBuffParameterReferenceBase>();
        if (rate != null)
        {
            return KnownStrategyBuffParameterReference(
                NetherStrategyBuffParameterReferenceKind.RatePermille,
                identity,
                (int)rate.valueType,
                rate.value,
                rate.limit
            );
        }
        Project.Ingame.FixedPermilleBuffParameterReferenceBase? fixedPermille =
            source.TryCast<Project.Ingame.FixedPermilleBuffParameterReferenceBase>();
        if (fixedPermille != null)
        {
            return KnownStrategyBuffParameterReference(
                NetherStrategyBuffParameterReferenceKind.FixedPermille,
                identity,
                (int)fixedPermille.valueType,
                fixedPermille.value,
                fixedPermille.limit
            );
        }
        Project.Ingame.FixedBuffParameterReferenceBase? fixedValue =
            source.TryCast<Project.Ingame.FixedBuffParameterReferenceBase>();
        if (fixedValue != null)
        {
            return KnownStrategyBuffParameterReference(
                NetherStrategyBuffParameterReferenceKind.FixedValue,
                identity,
                0,
                fixedValue.value,
                0
            );
        }
        Project.Ingame.AbnormalBuffParameterReferenceBase? abnormal =
            source.TryCast<Project.Ingame.AbnormalBuffParameterReferenceBase>();
        if (abnormal != null)
        {
            return abnormal.Probability == null
                ? UnknownStrategyBuffParameterReference(
                    identity,
                    "abnormal-buff-parameter-probability-unavailable:" + buffType
                )
                : KnownStrategyBuffParameterReference(
                    NetherStrategyBuffParameterReferenceKind.AbnormalProbabilityPermille,
                    identity,
                    0,
                    abnormal.Probability.Value,
                    0
                );
        }
        return UnknownStrategyBuffParameterReference(
            identity,
            "unsupported-buff-parameter-reference:" + buffType + ":" + identity
        );
    }

    private static NetherStrategyBuffParameterReferenceEvidence KnownStrategyBuffParameterReference(
        NetherStrategyBuffParameterReferenceKind kind,
        string identity,
        int valueType,
        int value,
        int limit
    ) => new(kind, identity)
    {
        ValueType = valueType,
        Value = value,
        Limit = limit,
        ValuesKnown = true,
    };

    private static NetherStrategyBuffParameterReferenceEvidence UnknownStrategyBuffParameterReference(
        string identity,
        string reason
    ) => new(NetherStrategyBuffParameterReferenceKind.Unknown, identity)
    {
        UnknownReason = reason,
    };

    private static bool TryMapStrategyBuffConditions(
        object? source,
        out IReadOnlyList<NetherStrategyBuffConditionEvidence> conditions,
        out string error
    )
    {
        if (!NetherRuntimeEnumerableReader.TryRead(source, out List<object> values, out string detail))
        {
            conditions = Array.Empty<NetherStrategyBuffConditionEvidence>();
            error = "buff-condition-enumeration:" + detail;
            return false;
        }
        var mapped = new List<NetherStrategyBuffConditionEvidence>(values.Count);
        foreach (object value in values)
        {
            string identity = RuntimeTypeIdentifier(value);
            NetherStrategyBuffConditionEvidence condition = value switch
            {
                Project.Ingame.BuffEnableConditions.ConditionParameterHpBelowOrEqual typed =>
                    new(NetherStrategyBuffConditionKind.HpBelowOrEqual, identity)
                    {
                        HpThresholdPermille = typed.HpThreshold.Value,
                    },
                Project.Ingame.BuffEnableConditions.ConditionParameterHpAboveOrEqual typed =>
                    new(NetherStrategyBuffConditionKind.HpAboveOrEqual, identity)
                    {
                        HpThresholdPermille = typed.HpThreshold.Value,
                    },
                Project.Ingame.BuffEnableConditions.ConditionParameterHpFull =>
                    new(NetherStrategyBuffConditionKind.HpFull, identity),
                Project.Ingame.BuffEnableConditions.ConditionParameterHasBuff typed =>
                    new(NetherStrategyBuffConditionKind.HasBuff, identity)
                    {
                        RequiredBuffType = new NetherStrategyBuffType((int)typed.BuffType),
                        RequiredBuffStack = typed.Stack,
                    },
                _ => new NetherStrategyBuffConditionEvidence(
                    NetherStrategyBuffConditionKind.Unknown,
                    identity
                )
                {
                    UnknownReason = "unsupported-buff-condition-parameter:" + identity,
                },
            };
            mapped.Add(condition);
            if (!condition.IsKnown)
            {
                conditions = mapped;
                error = condition.UnknownReason;
                return false;
            }
        }
        conditions = mapped;
        error = string.Empty;
        return true;
    }

    private static NetherStrategyAbilityEffectEvidence UnknownStrategyAbilityEffect(string reason) =>
        new(NetherStrategyAbilityEffectKind.Unknown)
        {
            UnknownReason = reason,
            NativeTypeIdentity = reason.StartsWith("unsupported-ability-effect-type:", StringComparison.Ordinal)
                ? reason["unsupported-ability-effect-type:".Length..]
                : string.Empty,
        };

    private static bool TryReadStrategyBuffMap(
        Project.Ingame.BuffTypeStrategies? store,
        out IReadOnlyDictionary<int, Project.Ingame.IBuffStrategy> strategies,
        out string error
    )
    {
        var mapped = new Dictionary<int, Project.Ingame.IBuffStrategy>();
        strategies = mapped;
        if (store?._strategies == null)
        {
            error = "buff-strategy-map-unavailable";
            return false;
        }
        if (!NetherRuntimeEnumerableReader.TryRead(
                store._strategies,
                out List<object> entries,
                out string enumerationDetail
            ))
        {
            error = "buff-strategy-map-enumeration:" + enumerationDetail;
            return false;
        }
        foreach (object entry in entries)
        {
            if (!TryReadMember(entry, "Key", out object? rawKey)
                || rawKey == null
                || !TryConvertInt32(rawKey, out int key)
                || !TryReadMember(entry, "Value", out object? rawStrategy)
                || rawStrategy is not Project.Ingame.IBuffStrategy strategy)
            {
                error = "invalid-buff-strategy-map-entry";
                return false;
            }
            if (!mapped.TryAdd(key, strategy))
            {
                error = "duplicate-buff-strategy-map-entry:" + key;
                return false;
            }
        }
        error = string.Empty;
        return true;
    }

    private static NetherStrategyBuffEvidence MapStrategyBuff(
        long mechanicId,
        int buffType,
        IReadOnlyDictionary<int, Project.Ingame.IBuffStrategy> strategies,
        string mapError
    )
    {
        if (!strategies.TryGetValue(buffType, out Project.Ingame.IBuffStrategy? strategy))
        {
            string reason = string.IsNullOrEmpty(mapError)
                ? "buff-strategy-unavailable:" + mechanicId + ":" + buffType
                : "buff-strategy-unavailable:" + mechanicId + ":" + buffType + ":" + mapError;
            return new NetherStrategyBuffEvidence(
                new NetherStrategyBuffType(buffType),
                NetherStrategyBuffEffectKind.Unknown,
                NetherStrategyStatusPriorityKind.Unknown,
                NetherStrategyBuffCoexistenceKind.Unknown
            )
            {
                IsKnown = false,
                UnknownReason = reason,
            };
        }

        NetherStrategyBuffEffectKind effectKind = (int)strategy.BuffEffectType switch
        {
            0 => NetherStrategyBuffEffectKind.Buff,
            1 => NetherStrategyBuffEffectKind.DeBuff,
            2 => NetherStrategyBuffEffectKind.Unique,
            _ => NetherStrategyBuffEffectKind.Unknown,
        };
        NetherStrategyStatusPriorityKind priority = (int)strategy.StatusPriorityType switch
        {
            -1 => NetherStrategyStatusPriorityKind.Invalid,
            0 => NetherStrategyStatusPriorityKind.Crest,
            1 => NetherStrategyStatusPriorityKind.Abnormal,
            2 => NetherStrategyStatusPriorityKind.Unique,
            3 => NetherStrategyStatusPriorityKind.Debuff,
            4 => NetherStrategyStatusPriorityKind.Buff,
            _ => NetherStrategyStatusPriorityKind.Unknown,
        };
        NetherStrategyBuffCoexistenceKind coexistence = (int)strategy.Coexistence switch
        {
            0 => NetherStrategyBuffCoexistenceKind.Allow,
            1 => NetherStrategyBuffCoexistenceKind.HigherValue,
            2 => NetherStrategyBuffCoexistenceKind.LongerRemainTime,
            3 => NetherStrategyBuffCoexistenceKind.Latest,
            4 => NetherStrategyBuffCoexistenceKind.Oldest,
            5 => NetherStrategyBuffCoexistenceKind.Stack,
            6 => NetherStrategyBuffCoexistenceKind.ExclusiveCrest,
            _ => NetherStrategyBuffCoexistenceKind.Unknown,
        };
        var additional = new List<NetherStrategyBuffType>();
        string additionalError = string.Empty;
        if (!NetherRuntimeEnumerableReader.TryRead(
                strategy.AdditionalMatchedQueryTypes,
                out List<object> rawAdditional,
                out string enumerationDetail
            ))
        {
            additionalError = "buff-additional-match-enumeration:"
                + mechanicId + ":" + buffType + ":" + enumerationDetail;
        }
        else
        {
            foreach (object raw in rawAdditional)
            {
                if (!TryConvertInt32(raw, out int value) || value <= 0)
                {
                    additionalError = "invalid-buff-additional-match:" + mechanicId + ":" + buffType;
                    break;
                }
                additional.Add(new NetherStrategyBuffType(value));
            }
        }
        bool known = effectKind != NetherStrategyBuffEffectKind.Unknown
            && priority != NetherStrategyStatusPriorityKind.Unknown
            && coexistence != NetherStrategyBuffCoexistenceKind.Unknown
            && additionalError.Length == 0;
        string unknown = known
            ? string.Empty
            : additionalError.Length > 0
                ? additionalError
                : "unknown-buff-strategy-enum:" + mechanicId + ":" + buffType;
        return new NetherStrategyBuffEvidence(
            new NetherStrategyBuffType(buffType),
            effectKind,
            priority,
            coexistence
        )
        {
            AdditionalMatchedTypes = additional,
            IsKnown = known,
            UnknownReason = unknown,
        };
    }

    private static bool TryReadStrategyBuffTypes(
        object effect,
        out IReadOnlyList<int> buffTypes,
        out string error
    )
    {
        var values = new HashSet<int>();
        object? collection = effect switch
        {
            Project.AbilityEffect.AbilityEffectParameterBuff typed => typed.Buffs,
            Project.AbilityEffect.AbilityEffectPassiveBuff typed => typed.Buffs,
            Project.AbilityEffect.AbilityEffectStackLinkedBuff typed => typed.Buffs,
            _ => null,
        };
        if (collection != null)
        {
            if (!NetherRuntimeEnumerableReader.TryRead(collection, out List<object> entries, out string detail))
            {
                buffTypes = Array.Empty<int>();
                error = "buff-parameter-enumeration:" + detail;
                return false;
            }
            foreach (object entry in entries)
            {
                if (entry is not Project.Ingame.BuffParameterByType parameter
                    || (int)parameter.buffType <= 0)
                {
                    buffTypes = Array.Empty<int>();
                    error = "invalid-buff-parameter-entry";
                    return false;
                }
                values.Add((int)parameter.buffType);
            }
        }
        if (effect is Project.AbilityEffect.AbilityEffectErosionLinkedBuff erosion)
        {
            if (erosion.Min?.Effect == null || erosion.Max?.Effect == null)
            {
                buffTypes = Array.Empty<int>();
                error = "erosion-linked-buff-parameter-unavailable";
                return false;
            }
            values.Add((int)erosion.Min.Effect.buffType);
            values.Add((int)erosion.Max.Effect.buffType);
        }
        if (effect is Project.AbilityEffect.AbilityEffectHpLinkedBuff hp)
        {
            if (hp.Min?.Effect == null || hp.Max?.Effect == null)
            {
                buffTypes = Array.Empty<int>();
                error = "hp-linked-buff-parameter-unavailable";
                return false;
            }
            values.Add((int)hp.Min.Effect.buffType);
            values.Add((int)hp.Max.Effect.buffType);
        }
        if (values.Any(value => value <= 0))
        {
            buffTypes = Array.Empty<int>();
            error = "invalid-native-buff-type";
            return false;
        }
        buffTypes = values.OrderBy(value => value).ToArray();
        error = string.Empty;
        return true;
    }

    private static string RuntimeTypeIdentifier(object? value) =>
        value?.GetType().FullName ?? "null";

    private static bool TryReadMember(object target, string name, out object? value)
    {
        value = null;
        Type type = target.GetType();
        PropertyInfo? property = type.GetProperty(name, InstanceFlags);
        if (property != null && property.GetIndexParameters().Length == 0)
        {
            value = property.GetValue(target);
            return true;
        }
        MethodInfo? getter = type.GetMethod("get_" + name, InstanceFlags, null, Type.EmptyTypes, null);
        if (getter != null)
        {
            value = getter.Invoke(target, Array.Empty<object>());
            return true;
        }
        FieldInfo? field = type.GetField(name, InstanceFlags)
            ?? type.GetField("<" + name + ">k__BackingField", InstanceFlags);
        if (field != null)
        {
            value = field.GetValue(target);
            return true;
        }
        return false;
    }

    private static bool TryConvertInt32(object raw, out int value)
    {
        value = 0;
        try
        {
            value = Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static readonly BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}
