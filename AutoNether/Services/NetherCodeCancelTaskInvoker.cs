#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoNether.Services;

/// <summary>
/// Starts the game's complete code-offer cancel/keep UniTask directly and returns the exact
/// boxed task to its owner.  The packaged b__12_0 callback invokes _onCancel, whose native
/// closure starts this same task with Forget(); native-to-native calls do not pass through a
/// managed Harmony wrapper, so observing that callback cannot provide a reliable task boundary.
/// </summary>
internal static class NetherCodeCancelTaskInvoker
{
    private const string CancellationTokenFieldName = "_cancellationToken";
    private const BindingFlags DeclaredInstanceFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    public static bool TryResolve(
        Type controllerType,
        Type utilityType,
        NetherCodePopupInteropMethodBinding binding,
        out string error,
        out MethodInfo? taskMethod,
        out MemberInfo? cancellationTokenMember
    )
    {
        taskMethod = null;
        cancellationTokenMember = null;
        if (controllerType == null || utilityType == null || binding == null)
        {
            error = "binding-unavailable:code-cancel:null-input";
            return false;
        }

        if (!NetherCodePopupInteropResolver.TryResolveStaticMethod(
                utilityType,
                binding,
                out error,
                out taskMethod
            ))
        {
            return false;
        }

        ParameterInfo[] parameters = taskMethod!.GetParameters();
        if (parameters.Length != 2 || parameters[0].ParameterType != controllerType)
        {
            error = "binding-unavailable:code-cancel:unexpected-task-signature";
            taskMethod = null;
            return false;
        }

        if (!TryResolveExactMember(
                controllerType,
                CancellationTokenFieldName,
                parameters[1].ParameterType,
                out error,
                out cancellationTokenMember
            ))
        {
            taskMethod = null;
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryInvoke(
        object controller,
        Type utilityType,
        NetherCodePopupInteropMethodBinding binding,
        out object? task,
        out string error
    )
    {
        task = null;
        if (controller == null)
        {
            error = "binding-unavailable:code-cancel:invalid-input";
            return false;
        }

        if (!TryResolve(
                controller.GetType(),
                utilityType,
                binding,
                out error,
                out MethodInfo? taskMethod,
                out MemberInfo? cancellationTokenMember
            ))
        {
            return false;
        }

        try
        {
            object? cancellationToken = ReadValue(cancellationTokenMember!, controller);
            if (cancellationToken == null)
            {
                error = "binding-unavailable:code-cancel:null-cancellation-token";
                return false;
            }

            task = taskMethod!.Invoke(null, new[] { controller, cancellationToken });
            if (task == null)
            {
                error = "binding-unavailable:code-cancel:null-task";
                return false;
            }

            error = string.Empty;
            return true;
        }
        catch (TargetInvocationException ex)
        {
            task = null;
            Exception cause = ex.InnerException ?? ex;
            error = "native-code-cancel-task-exception:"
                + cause.GetType().Name
                + ":"
                + cause.Message;
            return false;
        }
        catch (Exception ex)
        {
            task = null;
            error = "native-code-cancel-task-exception:"
                + ex.GetType().Name
                + ":"
                + ex.Message;
            return false;
        }
    }

    private static bool TryResolveExactMember(
        Type controllerType,
        string memberName,
        Type expectedMemberType,
        out string error,
        out MemberInfo? member
    )
    {
        var candidates = new List<MemberInfo>();
        for (Type? current = controllerType; current != null; current = current.BaseType)
        {
            foreach (FieldInfo candidate in current.GetFields(DeclaredInstanceFlags))
            {
                if (string.Equals(candidate.Name, memberName, StringComparison.Ordinal))
                    candidates.Add(candidate);
            }
            foreach (PropertyInfo candidate in current.GetProperties(DeclaredInstanceFlags))
            {
                if (string.Equals(candidate.Name, memberName, StringComparison.Ordinal)
                    && candidate.GetMethod != null
                    && !candidate.GetMethod.IsStatic
                    && candidate.GetIndexParameters().Length == 0)
                {
                    candidates.Add(candidate);
                }
            }
        }

        if (candidates.Count != 1)
        {
            member = null;
            error = "binding-unavailable:code-cancel:"
                + memberName
                + ":"
                + (candidates.Count == 0 ? "no-exact" : "ambiguous")
                + ":available="
                + DescribeInstanceMembers(controllerType);
            return false;
        }

        Type actualType = candidates[0] switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property => property.PropertyType,
            _ => typeof(void),
        };
        if (actualType != expectedMemberType)
        {
            member = null;
            error = "binding-unavailable:code-cancel:"
                + memberName
                + ":type-mismatch:"
                + (actualType.FullName ?? actualType.Name);
            return false;
        }

        member = candidates[0];
        error = string.Empty;
        return true;
    }

    private static object? ReadValue(MemberInfo member, object target) => member switch
    {
        FieldInfo field => field.GetValue(target),
        PropertyInfo property => property.GetValue(target),
        _ => null,
    };

    private static string DescribeInstanceMembers(Type controllerType)
    {
        var members = new List<string>();
        for (Type? current = controllerType; current != null && members.Count < 32; current = current.BaseType)
        {
            foreach (FieldInfo field in current.GetFields(DeclaredInstanceFlags))
            {
                if (members.Count >= 32)
                    break;
                members.Add(
                    (current.FullName ?? current.Name)
                    + "."
                    + field.Name
                    + ":"
                    + (field.FieldType.FullName ?? field.FieldType.Name)
                );
            }
            foreach (PropertyInfo property in current.GetProperties(DeclaredInstanceFlags))
            {
                if (members.Count >= 32)
                    break;
                members.Add(
                    (current.FullName ?? current.Name)
                    + "."
                    + property.Name
                    + ":"
                    + (property.PropertyType.FullName ?? property.PropertyType.Name)
                    + "(property)"
                );
            }
        }
        return members.Count == 0 ? "none" : string.Join("|", members);
    }
}
