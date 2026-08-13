#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AutoNether.Services;

/// <summary>
/// Resolves the exact scroll owned by a Return popup after that popup has been initialized.
/// The native popup controller calls the scroll's implementation directly, so the public
/// <c>InitializeView</c> IL2CPP wrapper is not guaranteed to pass through Harmony.
/// </summary>
internal readonly record struct NetherCheckpointReturnScrollResolution(
    object? Controller,
    int ContentCount,
    int SelectionLimit,
    string Detail
)
{
    public bool IsReady => Controller != null && ContentCount > 0 && SelectionLimit > 0
        && Detail == "nested-return-scroll-ready";
}

internal static class NetherCheckpointReturnScrollResolver
{
    private static readonly BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static NetherCheckpointReturnScrollResolution Resolve(
        object? popup,
        string expectedControllerTypeName
    )
    {
        if (popup == null)
            return new(null, 0, 0, "missing-return-popup-instance");
        if (string.IsNullOrWhiteSpace(expectedControllerTypeName))
            return new(null, 0, 0, "missing-return-scroll-type-contract");

        try
        {
            if (!TryReadMember(popup, "ReturnableItemScrollViewController", out object? controller)
                || controller == null)
            {
                return new(null, 0, 0, "missing-return-popup-scroll-controller");
            }

            string actualTypeName = controller.GetType().FullName ?? controller.GetType().Name;
            if (!string.Equals(actualTypeName, expectedControllerTypeName, StringComparison.Ordinal))
            {
                return new(
                    null,
                    0,
                    0,
                    "return-scroll-type-mismatch:" + actualTypeName
                );
            }

            if (!TryReadMember(controller, "_contentModelList", out object? contentModels)
                || contentModels == null
                || !TryCount(contentModels, out int contentCount))
            {
                return new(null, 0, 0, "missing-return-scroll-content-model-list");
            }
            if (contentCount <= 0)
                return new(null, 0, 0, "return-scroll-content-not-ready");

            if (!TryReadMember(controller, "_maxSelectedCount", out object? rawLimit)
                || rawLimit == null
                || !TryConvertInt32(rawLimit, out int selectionLimit))
            {
                return new(null, contentCount, 0, "missing-return-scroll-selection-limit");
            }
            if (selectionLimit <= 0)
                return new(null, contentCount, selectionLimit, "return-scroll-selection-limit-not-ready");

            return new(
                controller,
                contentCount,
                selectionLimit,
                "nested-return-scroll-ready"
            );
        }
        catch (TargetInvocationException ex)
        {
            Exception cause = ex.InnerException ?? ex;
            return new(null, 0, 0, "return-scroll-resolution-exception:" + cause.GetType().Name);
        }
        catch (Exception ex)
        {
            return new(null, 0, 0, "return-scroll-resolution-exception:" + ex.GetType().Name);
        }
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

    private static bool TryCount(object collection, out int count)
    {
        count = 0;
        if (collection is ICollection nonGeneric)
        {
            count = nonGeneric.Count;
            return true;
        }
        if (TryReadMember(collection, "Count", out object? rawCount)
            && rawCount != null
            && TryConvertInt32(rawCount, out count))
        {
            return count >= 0;
        }
        if (!NetherRuntimeEnumerableReader.TryRead(collection, out List<object> values, out _))
            return false;
        count = values.Count;
        return true;
    }

    private static bool TryConvertInt32(object raw, out int value)
    {
        value = 0;
        try
        {
            value = Convert.ToInt32(raw, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
