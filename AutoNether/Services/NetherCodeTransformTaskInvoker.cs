#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes;

namespace AutoNether.Services;

/// <summary>
/// Starts the game's complete target_type=7 code-conversion UniTask directly from the live
/// Change-list closure state and returns its boxed UniTask to the owner.  OnClickChange invokes
/// _onChangeButtonClicked, whose native closure inlines this same sequence and calls Forget();
/// Harmony-detouring the value-type state machine instead would box its struct receiver without
/// write-back and corrupt multi-await state transitions.
/// </summary>
internal static class NetherCodeTransformTaskInvoker
{
    private const string ErrorScope = "code-transform";
    private const string ChangeActionFieldName = "_onChangeButtonClicked";
    private const string ClosureControllerFieldName = "controller";
    private const string NetherModelFieldName = "netherModel";
    private const string ResultModelFieldName = "resultModel";
    private const string CompletedPropertyFieldName = "isCompletedRP";
    private const string CancellationTokenFieldName = "ct";
    private const BindingFlags NestedFlags = BindingFlags.Public | BindingFlags.NonPublic;

    public static bool TryResolve(
        Type controllerType,
        Type utilityType,
        NetherCodePopupInteropMethodBinding binding,
        out string error,
        out MethodInfo? taskMethod,
        out MemberInfo? changeActionMember,
        out PropertyInfo? delegateTargetProperty,
        out Type? closureType,
        out MemberInfo? closureControllerMember,
        out MemberInfo? netherModelMember,
        out MemberInfo? resultModelMember,
        out MemberInfo? completedPropertyMember,
        out MemberInfo? cancellationTokenMember
    )
    {
        taskMethod = null;
        changeActionMember = null;
        delegateTargetProperty = null;
        closureType = null;
        closureControllerMember = null;
        netherModelMember = null;
        resultModelMember = null;
        completedPropertyMember = null;
        cancellationTokenMember = null;

        if (controllerType == null || utilityType == null || binding == null)
        {
            error = "binding-unavailable:code-transform:null-input";
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
        if (parameters.Length != 6
            || parameters[0].ParameterType != controllerType
            || parameters[3].ParameterType != typeof(long))
        {
            error = "binding-unavailable:code-transform:unexpected-task-signature";
            taskMethod = null;
            return false;
        }

        if (!TryResolveChangeActionMember(
                controllerType,
                out error,
                out changeActionMember,
                out delegateTargetProperty
            ))
        {
            taskMethod = null;
            return false;
        }

        if (!TryResolveClosureMembers(
                utilityType,
                parameters,
                out error,
                out closureType,
                out closureControllerMember,
                out netherModelMember,
                out resultModelMember,
                out completedPropertyMember,
                out cancellationTokenMember
            ))
        {
            taskMethod = null;
            changeActionMember = null;
            delegateTargetProperty = null;
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryInvoke(
        object controller,
        Type utilityType,
        long beforeCodeId,
        NetherCodePopupInteropMethodBinding binding,
        out object? task,
        out string error
    )
    {
        task = null;
        if (controller == null || beforeCodeId <= 0)
        {
            error = "binding-unavailable:code-transform:invalid-input";
            return false;
        }

        if (!TryResolve(
                controller.GetType(),
                utilityType,
                binding,
                out error,
                out MethodInfo? taskMethod,
                out MemberInfo? changeActionMember,
                out PropertyInfo? delegateTargetProperty,
                out Type? closureType,
                out MemberInfo? closureControllerMember,
                out MemberInfo? netherModelMember,
                out MemberInfo? resultModelMember,
                out MemberInfo? completedPropertyMember,
                out MemberInfo? cancellationTokenMember
            ))
        {
            return false;
        }

        try
        {
            object? changeAction = NetherCodePopupInteropResolver.ReadMemberValue(changeActionMember!, controller);
            if (changeAction == null)
            {
                error = "binding-unavailable:code-transform:null-change-action";
                return false;
            }

            object? rawTarget = delegateTargetProperty!.GetValue(changeAction);
            object? closure = CoerceClosureTarget(rawTarget, closureType!);
            if (closure == null)
            {
                error = "binding-unavailable:code-transform:closure-target-mismatch";
                return false;
            }

            object? closureController = NetherCodePopupInteropResolver.ReadMemberValue(closureControllerMember!, closure);
            if (!IsSameInstance(closureController, controller))
            {
                error = "binding-unavailable:code-transform:closure-controller-mismatch";
                return false;
            }

            object? netherModel = NetherCodePopupInteropResolver.ReadMemberValue(netherModelMember!, closure);
            object? resultModel = NetherCodePopupInteropResolver.ReadMemberValue(resultModelMember!, closure);
            object? isCompletedRP = NetherCodePopupInteropResolver.ReadMemberValue(completedPropertyMember!, closure);
            object? cancellationToken = NetherCodePopupInteropResolver.ReadMemberValue(cancellationTokenMember!, closure);
            if (netherModel == null)
            {
                error = "binding-unavailable:code-transform:null-nether-model";
                return false;
            }
            if (resultModel == null)
            {
                error = "binding-unavailable:code-transform:null-result-model";
                return false;
            }
            if (isCompletedRP == null)
            {
                error = "binding-unavailable:code-transform:null-completed-property";
                return false;
            }
            if (cancellationToken == null)
            {
                error = "binding-unavailable:code-transform:null-cancellation-token";
                return false;
            }

            task = taskMethod!.Invoke(
                null,
                new[]
                {
                    controller,
                    netherModel,
                    resultModel,
                    (object)beforeCodeId,
                    isCompletedRP,
                    cancellationToken,
                }
            );
            if (task == null)
            {
                error = "binding-unavailable:code-transform:null-task";
                return false;
            }

            error = string.Empty;
            return true;
        }
        catch (TargetInvocationException ex)
        {
            task = null;
            Exception cause = ex.InnerException ?? ex;
            error = "native-code-transform-task-exception:"
                + cause.GetType().Name
                + ":"
                + cause.Message;
            return false;
        }
        catch (Exception ex)
        {
            task = null;
            error = "native-code-transform-task-exception:"
                + ex.GetType().Name
                + ":"
                + ex.Message;
            return false;
        }
    }

    private static bool TryResolveChangeActionMember(
        Type controllerType,
        out string error,
        out MemberInfo? member,
        out PropertyInfo? targetProperty
    )
    {
        member = null;
        targetProperty = null;
        if (!NetherCodePopupInteropResolver.TryFindSingleMember(
                controllerType,
                ChangeActionFieldName,
                ErrorScope,
                out error,
                out MemberInfo? candidate,
                out Type actualType
            ))
        {
            return false;
        }

        if (!actualType.IsGenericType
            || !string.Equals(actualType.Name, "Action`1", StringComparison.Ordinal)
            || actualType.GetGenericArguments().Length != 1
            || actualType.GetGenericArguments()[0] != typeof(long))
        {
            error = "binding-unavailable:code-transform:"
                + ChangeActionFieldName
                + ":type-mismatch:"
                + (actualType.FullName ?? actualType.Name);
            return false;
        }

        PropertyInfo? target = actualType.GetProperty(
            "Target",
            BindingFlags.Public | BindingFlags.Instance
        );
        if (target?.GetMethod == null || target.GetIndexParameters().Length != 0)
        {
            error = "binding-unavailable:code-transform:"
                + ChangeActionFieldName
                + ":missing-delegate-target";
            return false;
        }

        member = candidate;
        targetProperty = target;
        error = string.Empty;
        return true;
    }

    private static bool TryResolveClosureMembers(
        Type utilityType,
        ParameterInfo[] parameters,
        out string error,
        out Type? closureType,
        out MemberInfo? controllerMember,
        out MemberInfo? netherModelMember,
        out MemberInfo? resultModelMember,
        out MemberInfo? completedPropertyMember,
        out MemberInfo? cancellationTokenMember
    )
    {
        closureType = null;
        controllerMember = null;
        netherModelMember = null;
        resultModelMember = null;
        completedPropertyMember = null;
        cancellationTokenMember = null;

        var matches = new List<(
            Type Type,
            MemberInfo Controller,
            MemberInfo NetherModel,
            MemberInfo ResultModel,
            MemberInfo CompletedProperty,
            MemberInfo CancellationToken
        )>();

        foreach (Type candidate in utilityType.GetNestedTypes(NestedFlags))
        {
            if (candidate.IsValueType)
                continue;
            string? baseName = candidate.BaseType?.FullName;
            if (!string.Equals(baseName, "Il2CppSystem.Object", StringComparison.Ordinal)
                && !string.Equals(baseName, "System.Object", StringComparison.Ordinal))
            {
                continue;
            }

            if (NetherCodePopupInteropResolver.TryResolveExactMember(candidate, ClosureControllerFieldName, parameters[0].ParameterType, ErrorScope, out _, out MemberInfo? cMember)
                && NetherCodePopupInteropResolver.TryResolveExactMember(candidate, NetherModelFieldName, parameters[1].ParameterType, ErrorScope, out _, out MemberInfo? nMember)
                && NetherCodePopupInteropResolver.TryResolveExactMember(candidate, ResultModelFieldName, parameters[2].ParameterType, ErrorScope, out _, out MemberInfo? rMember)
                && NetherCodePopupInteropResolver.TryResolveExactMember(candidate, CompletedPropertyFieldName, parameters[4].ParameterType, ErrorScope, out _, out MemberInfo? pMember)
                && NetherCodePopupInteropResolver.TryResolveExactMember(candidate, CancellationTokenFieldName, parameters[5].ParameterType, ErrorScope, out _, out MemberInfo? tMember))
            {
                matches.Add((candidate, cMember!, nMember!, rMember!, pMember!, tMember!));
            }
        }

        if (matches.Count != 1)
        {
            error = "binding-unavailable:code-transform:closure:"
                + (matches.Count == 0 ? "no-exact" : "ambiguous:" + string.Join("|", matches.Select(m => m.Type.Name).OrderBy(n => n, StringComparer.Ordinal)));
            return false;
        }

        (closureType, controllerMember, netherModelMember, resultModelMember, completedPropertyMember, cancellationTokenMember) = matches[0];
        error = string.Empty;
        return true;
    }

    private static object? CoerceClosureTarget(object? rawTarget, Type closureType)
    {
        if (rawTarget == null)
            return null;
        if (closureType.IsInstanceOfType(rawTarget))
            return rawTarget;
        if (rawTarget is Il2CppObjectBase && typeof(Il2CppObjectBase).IsAssignableFrom(closureType))
        {
            MethodInfo? tryCast = typeof(Il2CppObjectBase).GetMethod(
                nameof(Il2CppObjectBase.TryCast),
                Type.EmptyTypes
            );
            return tryCast?.MakeGenericMethod(closureType).Invoke(rawTarget, null);
        }
        return null;
    }

    private static bool IsSameInstance(object? left, object? right) =>
        ReferenceEquals(left, right)
        || (left is Il2CppObjectBase a
            && right is Il2CppObjectBase b
            && a.Pointer != IntPtr.Zero
            && a.Pointer == b.Pointer);
}
