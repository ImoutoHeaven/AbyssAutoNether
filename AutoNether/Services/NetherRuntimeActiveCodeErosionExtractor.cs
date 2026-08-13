#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace AutoNether.Services;

/// <summary>
/// Reflection-only adapter for the exact live code path: possession <c>NetherCodeData</c>
/// supplies <c>MNetherCodeId</c>/<c>Amount</c>, while <c>MNetherCodes</c> and
/// <c>MNetherCodeCategorySkills</c> supply the exact category thresholds and effect fields.
/// </summary>
internal sealed class NetherRuntimeActiveCodeErosionExtractor
{
    private static readonly BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly NetherActiveCodeErosionProjectionMapper _mapper = new();

    public NetherActiveCodeErosionProjection Extract(object? rawPossessionCodes, object? rawMasterRows)
        => ExtractCore(rawPossessionCodes, rawMasterRows, null, 0, includeCategorySkills: false);

    public NetherActiveCodeErosionProjection Extract(
        object? rawPossessionCodes,
        object? rawMasterRows,
        object? rawCategorySkillRows,
        long activeNetherId
    ) => ExtractCore(
        rawPossessionCodes,
        rawMasterRows,
        rawCategorySkillRows,
        activeNetherId,
        includeCategorySkills: true
    );

    private NetherActiveCodeErosionProjection ExtractCore(
        object? rawPossessionCodes,
        object? rawMasterRows,
        object? rawCategorySkillRows,
        long activeNetherId,
        bool includeCategorySkills
    )
    {
        if (rawPossessionCodes == null)
            return NetherActiveCodeErosionProjectionMapper.Unknown("missing-possession-nether-codes");
        if (!NetherRuntimeEnumerableReader.TryRead(rawPossessionCodes, out List<object> rawPossessions, out string possessionError))
        {
            return NetherActiveCodeErosionProjectionMapper.Unknown(
                "invalid-possession-nether-code-collection:" + possessionError
            );
        }

        var possessions = new List<NetherPossessionCodeErosionInput>(rawPossessions!.Count);
        foreach (object rawPossession in rawPossessions)
        {
            if (!TryReadInt64(rawPossession, "MNetherCodeId", out long codeId)
                || !TryReadInt64(rawPossession, "Amount", out long amount))
            {
                return NetherActiveCodeErosionProjectionMapper.Unknown("missing-possession-nether-code-member");
            }
            possessions.Add(new NetherPossessionCodeErosionInput(codeId, amount));
        }

        if (rawMasterRows == null)
            return _mapper.Map(possessions, null);
        if (!NetherRuntimeEnumerableReader.TryRead(rawMasterRows, out List<object> rawMasters, out string masterError))
        {
            return NetherActiveCodeErosionProjectionMapper.Unknown(
                "invalid-m-nether-code-collection:" + masterError
            );
        }

        var masters = new List<NetherCodeErosionMasterInput>(rawMasters!.Count);
        foreach (object rawMaster in rawMasters)
        {
            if (!TryReadInt64(rawMaster, "id", out long codeId)
                || !TryReadInt32(rawMaster, "effect_type", out int effectType)
                || !TryReadInt64(rawMaster, "effect_parameter_1", out long parameter1)
                || !TryReadInt64(rawMaster, "effect_parameter_2", out long parameter2)
                || !TryReadInt64(rawMaster, "effect_parameter_3", out long parameter3))
            {
                return NetherActiveCodeErosionProjectionMapper.Unknown("missing-m-nether-code-effect-member");
            }
            long netherId = 0;
            int category = 0;
            if (includeCategorySkills
                && (!TryReadInt64(rawMaster, "m_nether_id", out netherId)
                    || !TryReadInt32(rawMaster, "category", out category)))
            {
                return NetherActiveCodeErosionProjectionMapper.Unknown("missing-m-nether-code-category-member");
            }
            masters.Add(new NetherCodeErosionMasterInput(
                codeId,
                effectType,
                parameter1,
                parameter2,
                parameter3
            )
            {
                NetherId = netherId,
                Category = category,
            });
        }

        if (!includeCategorySkills)
            return _mapper.Map(possessions, masters);
        if (rawCategorySkillRows == null)
            return _mapper.Map(possessions, masters, null, activeNetherId);
        if (!NetherRuntimeEnumerableReader.TryRead(
                rawCategorySkillRows,
                out List<object> rawCategorySkills,
                out string categorySkillError
            ))
        {
            return NetherActiveCodeErosionProjectionMapper.Unknown(
                "invalid-m-nether-code-category-skill-collection:" + categorySkillError
            );
        }

        var categorySkills = new List<NetherCodeCategoryErosionMasterInput>(rawCategorySkills.Count);
        foreach (object rawSkill in rawCategorySkills)
        {
            if (!TryReadInt64(rawSkill, "id", out long skillId)
                || !TryReadInt64(rawSkill, "m_nether_id", out long netherId)
                || !TryReadInt32(rawSkill, "counter", out int counter)
                || !TryReadInt32(rawSkill, "category", out int category)
                || !TryReadInt32(rawSkill, "effect_type", out int effectType)
                || !TryReadInt64(rawSkill, "effect_parameter_1", out long parameter1)
                || !TryReadInt64(rawSkill, "effect_parameter_2", out long parameter2)
                || !TryReadInt64(rawSkill, "effect_parameter_3", out long parameter3))
            {
                return NetherActiveCodeErosionProjectionMapper.Unknown(
                    "missing-m-nether-code-category-skill-member"
                );
            }
            categorySkills.Add(new NetherCodeCategoryErosionMasterInput(
                skillId,
                netherId,
                counter,
                category,
                effectType,
                parameter1,
                parameter2,
                parameter3
            ));
        }

        return _mapper.Map(possessions, masters, categorySkills, activeNetherId);
    }

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
        if (field == null)
            return false;
        value = field.GetValue(target);
        return true;
    }

    private static bool TryReadInt64(object target, string name, out long value)
    {
        value = 0;
        if (!TryReadMember(target, name, out object? raw) || raw == null)
            return false;
        try
        {
            value = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryReadInt32(object target, string name, out int value)
    {
        value = 0;
        if (!TryReadInt64(target, name, out long raw) || raw is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)raw;
        return true;
    }
}
