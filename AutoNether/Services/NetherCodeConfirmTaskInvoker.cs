#nullable enable

using System;
using System.Reflection;

namespace AutoNether.Services;

/// <summary>
/// Starts the game's complete code-confirmation UniTask directly from the live popup state.
/// Calling the generated Receive lambda through reflection is unsafe here: that native lambda
/// synchronously enters the same task through a Harmony DMD and nests IL2CPP runtime invokes.
/// The task itself is authoritative (replacement UI plus fix-code request), so retaining its
/// boxed return value preserves both the mutation and its completion boundary.
/// </summary>
internal static class NetherCodeConfirmTaskInvoker
{
    private const string ErrorScope = "code-confirm";
    private const string PartyModelFieldName = "_partyModel";
    private const string CancellationTokenFieldName = "_cancellationToken";

    public static bool TryResolve(
        Type controllerType,
        Type utilityType,
        NetherCodePopupInteropMethodBinding binding,
        out string error,
        out MethodInfo? taskMethod,
        out MemberInfo? partyModelMember,
        out MemberInfo? cancellationTokenMember
    )
    {
        taskMethod = null;
        partyModelMember = null;
        cancellationTokenMember = null;
        if (controllerType == null || utilityType == null || binding == null)
        {
            error = "binding-unavailable:code-confirm:null-input";
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
        if (parameters.Length != 5
            || parameters[0].ParameterType != controllerType
            || parameters[1].ParameterType != typeof(long)
            || parameters[3].ParameterType != typeof(bool))
        {
            error = "binding-unavailable:code-confirm:unexpected-task-signature";
            taskMethod = null;
            return false;
        }

        if (!NetherCodePopupInteropResolver.TryResolveExactMember(
                controllerType,
                PartyModelFieldName,
                parameters[2].ParameterType,
                ErrorScope,
                out error,
                out partyModelMember
            )
            || !NetherCodePopupInteropResolver.TryResolveExactMember(
                controllerType,
                CancellationTokenFieldName,
                parameters[4].ParameterType,
                ErrorScope,
                out error,
                out cancellationTokenMember
            ))
        {
            taskMethod = null;
            partyModelMember = null;
            cancellationTokenMember = null;
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryInvoke(
        object controller,
        Type utilityType,
        long selectedCodeId,
        NetherCodePopupInteropMethodBinding binding,
        out object? task,
        out string error
    )
    {
        task = null;
        if (controller == null || selectedCodeId <= 0)
        {
            error = "binding-unavailable:code-confirm:invalid-input";
            return false;
        }

        if (!TryResolve(
                controller.GetType(),
                utilityType,
                binding,
                out error,
                out MethodInfo? taskMethod,
                out MemberInfo? partyModelMember,
                out MemberInfo? cancellationTokenMember
            ))
        {
            return false;
        }

        try
        {
            object? partyModel = NetherCodePopupInteropResolver.ReadMemberValue(partyModelMember!, controller);
            object? cancellationToken = NetherCodePopupInteropResolver.ReadMemberValue(cancellationTokenMember!, controller);
            if (partyModel == null)
            {
                error = "binding-unavailable:code-confirm:null-party-model";
                return false;
            }
            if (cancellationToken == null)
            {
                error = "binding-unavailable:code-confirm:null-cancellation-token";
                return false;
            }

            task = taskMethod!.Invoke(
                null,
                new[] { controller, (object)selectedCodeId, partyModel, true, cancellationToken }
            );
            if (task == null)
            {
                error = "binding-unavailable:code-confirm:null-task";
                return false;
            }

            error = string.Empty;
            return true;
        }
        catch (TargetInvocationException ex)
        {
            task = null;
            Exception cause = ex.InnerException ?? ex;
            error = "native-code-confirm-task-exception:"
                + cause.GetType().Name
                + ":"
                + cause.Message;
            return false;
        }
        catch (Exception ex)
        {
            task = null;
            error = "native-code-confirm-task-exception:"
                + ex.GetType().Name
                + ":"
                + ex.Message;
            return false;
        }
    }
}
