#nullable enable

using System;
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
    private const string ErrorScope = "code-cancel";
    private const string CancellationTokenFieldName = "_cancellationToken";

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

        if (!NetherCodePopupInteropResolver.TryResolveExactMember(
                controllerType,
                CancellationTokenFieldName,
                parameters[1].ParameterType,
                ErrorScope,
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
            object? cancellationToken = NetherCodePopupInteropResolver.ReadMemberValue(cancellationTokenMember!, controller);
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
}
