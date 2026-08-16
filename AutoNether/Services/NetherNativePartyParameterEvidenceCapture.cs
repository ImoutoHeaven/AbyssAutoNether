#nullable enable

using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using Project.Common;
using Project.Master;
using Project.Nether;
using Project.Outgame;

namespace AutoNether.Services;

/// <summary>
/// Compile-time native adapter for the exact inputs consumed by
/// <see cref="NetherPartyCharacterModel.CalculateUnitParameter"/>. It mirrors the current
/// <c>NetherPartyCharacterParametersCalculator.CalculateUnitParametersMap</c> input assembly but
/// exports only immutable typed numbers; Unity/IL2CPP owners never cross the strategy boundary.
/// </summary>
internal static class NetherNativePartyParameterEvidenceCapture
{
    private static readonly ParameterType[] SupportedParameters =
    {
        ParameterType.Hp,
        ParameterType.Defence,
    };

    public static bool TryCapture(
        NetherPartyModel party,
        out IReadOnlyDictionary<int, IReadOnlyList<NetherStrategyParameterCalculationEvidence>> rows,
        out string error
    )
    {
        rows = new Dictionary<int, IReadOnlyList<NetherStrategyParameterCalculationEvidence>>();
        error = string.Empty;
        if (party?.CharacterModels == null)
        {
            error = "native-party-parameter-owner-unavailable";
            return false;
        }

        try
        {
            var partyMembers =
                new Il2CppSystem.Collections.Generic.List<NetherPartyCharacterModel>();
            foreach (NetherPartyCharacterModel member in party.CharacterModels)
                partyMembers.Add(member);
            CharacterParameters? support =
                NetherPartyCharacterParametersCalculator.CalculateSupportBuffParameters(
                    partyMembers.Cast<Il2CppSystem.Collections.Generic.IEnumerable<
                        NetherPartyCharacterModel>>()
                );
            if (support == null)
            {
                error = "native-party-support-parameters-unavailable";
                return false;
            }

            var captured = new Dictionary<int, IReadOnlyList<NetherStrategyParameterCalculationEvidence>>();
            foreach (NetherPartyCharacterModel member in partyMembers)
            {
                if (member == null || member.PartyIndex < 0)
                {
                    error = "invalid-native-party-parameter-member";
                    return false;
                }

                var friendEffects =
                    new Il2CppSystem.Collections.Generic.List<AbilityEffectModel>();
                foreach (NetherPartyCharacterModel other in partyMembers)
                {
                    if (other == null || other.PartyIndex == member.PartyIndex)
                        continue;
                    if (other.GeneralAbilityEffectModels == null)
                    {
                        error = "native-friend-general-ability-effects-unavailable:"
                            + member.PartyIndex;
                        return false;
                    }
                    foreach (AbilityEffectModel effect in other.GeneralAbilityEffectModels)
                    {
                        if (effect == null)
                        {
                            error = "invalid-native-friend-general-ability-effect:"
                                + member.PartyIndex;
                            return false;
                        }
                        friendEffects.Add(effect);
                    }
                }

                Il2CppSystem.ValueTuple<CharacterParameters, CharacterParameters> allTarget =
                    AbilityParameters.AccumulateBuiltInFriendAbilityParametersForSelf(
                        friendEffects.Cast<Il2CppSystem.Collections.Generic.IEnumerable<
                            AbilityEffectModel>>(),
                        member
                    );
                if (allTarget.Item1 == null || allTarget.Item2 == null)
                {
                    error = "native-all-target-parameters-unavailable:" + member.PartyIndex;
                    return false;
                }

                var memberRows = new List<NetherStrategyParameterCalculationEvidence>(
                    SupportedParameters.Length
                );
                foreach (ParameterType parameterType in SupportedParameters)
                {
                    int equipment = checked(
                        member.WeaponBasicParameters.GetParameter(parameterType)
                        + member.ArmorBasicParameters.GetParameter(parameterType)
                        + member.AccessoryBasicParameters.GetParameter(parameterType)
                    );
                    if (ParameterTypeExtensions.IsPermilleStoredParameter(parameterType))
                        equipment = Project.NumericsUtility.PercentToPerMille(equipment);
                    equipment = checked(
                        equipment
                        + member.EquipmentAbilityAdditionParameters.GetParameter(parameterType)
                    );

                    memberRows.Add(new NetherStrategyParameterCalculationEvidence(
                        Map(parameterType),
                        checked(
                            member.BasicParameters.GetParameter(parameterType)
                            + member.BondParameters.GetParameter(parameterType)
                        ),
                        member.CharacterAbilityAdditionParameters.GetParameter(parameterType),
                        equipment,
                        allTarget.Item1.GetParameter(parameterType),
                        member.CharacterAbilityMultiplicationParameters.GetParameter(parameterType),
                        allTarget.Item2.GetParameter(parameterType),
                        member.EquipmentAbilityMultiplicationParameters.GetParameter(parameterType),
                        member.BuildingMultiplicationParameters.GetParameter(parameterType),
                        member.PartyPosition == Project.Common.CharacterPartyPosition.Assist
                            ? 0
                            : support.GetParameter(parameterType)
                    ));
                }
                if (!captured.TryAdd(member.PartyIndex, memberRows.ToArray()))
                {
                    error = "duplicate-native-party-parameter-index:" + member.PartyIndex;
                    return false;
                }
            }
            if (captured.Count == 0)
            {
                error = "native-party-parameter-members-empty";
                return false;
            }
            rows = captured;
            return true;
        }
        catch (Exception ex)
        {
            error = "native-party-parameter-capture:"
                + ex.GetType().Name + ":" + ex.Message;
            return false;
        }
    }

    private static NetherCharacterParameterKind Map(ParameterType parameterType) => parameterType switch
    {
        ParameterType.Hp => NetherCharacterParameterKind.Hp,
        ParameterType.Defence => NetherCharacterParameterKind.Defence,
        _ => NetherCharacterParameterKind.None,
    };
}
