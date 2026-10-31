#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoNether.Services;

/// <summary>
/// One generated code sequence in <c>Project.Nether.NetherUtility</c>.  A sequence is a
/// local-function UniTask factory plus the compiler-generated state machine that owns it.
/// IL2CPP inlines the factory body into the reachable popup lambda, so the factory has no call
/// sites and cannot be observed.  The state machine survives that inlining: its
/// <c>MoveNext</c> stays a real call from both the lambda and the dead factory body.
/// </summary>
internal enum NetherGeneratedSequenceFamily
{
    Transform,
    Confirm,
    Cancel,
}

/// <summary>
/// Version-characterized identity of one generated sequence.  The state machine type is
/// resolved by the SOURCE local-function name it embeds in its <c>ObfuscatedNameAttribute</c>
/// (<c>&lt;Outer&gt;g__&lt;SourceName&gt;|ordinal</c>), never by the generated managed type name,
/// which the interop package obfuscates independently, and never by the factory's
/// <c>Method_Internal_..._PDM_0</c> name.
/// </summary>
internal sealed record NetherGeneratedSequenceContract(
    NetherGeneratedSequenceFamily Family,
    string SourceMethodName,
    string ControllerTypeName,
    string? CodeIdFieldName,
    Func<string, NetherCodePopupInteropMethodBinding> FactoryBinding
)
{
    public string FamilyName => Family.ToString().ToLowerInvariant();

    /// <summary>Local-function marker embedded in the state machine's original type name.</summary>
    public string SourceMarker => ">g__" + SourceMethodName + "|";
}

internal sealed record NetherGeneratedSequenceTarget(
    NetherGeneratedSequenceContract Contract,
    Type StateMachineType,
    MethodInfo MoveNext,
    MethodInfo Factory
);

/// <summary>
/// Shape and source-name resolution for the three generated code sequences.  Every rule is
/// strict and unique: a renamed, duplicated, or reshaped sequence fails closed with a precise
/// message instead of silently degrading to a later 600-pump flow timeout.
/// </summary>
internal static class NetherGeneratedSequenceResolver
{
    public const string UtilityTypeName = "Project.Nether.NetherUtility";
    public const string StateFieldName = "__1__state";
    public const string BuilderFieldName = "__t__builder";
    public const string ControllerFieldName = "controller";
    public const string MoveNextName = "MoveNext";

    private const string AsyncBuilderTypeName =
        "Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder";
    private const string ValueTypeBaseName = "Il2CppSystem.ValueType";
    private const string CoreValueTypeBaseName = "System.ValueType";
    private const BindingFlags DeclaredInstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private const BindingFlags NestedFlags = BindingFlags.Public | BindingFlags.NonPublic;

    public static IReadOnlyList<NetherGeneratedSequenceContract> Contracts { get; } = new[]
    {
        new NetherGeneratedSequenceContract(
            NetherGeneratedSequenceFamily.Transform,
            "HandleChangeAsync",
            NetherNativeBindingCatalog.CodeListControllerType,
            "beforeMNetherCodeId",
            NetherCodeTransformNativeBinding.TransformTaskBinding
        ),
        new NetherGeneratedSequenceContract(
            NetherGeneratedSequenceFamily.Confirm,
            "HandleConfirmSequenceAsync",
            NetherNativeBindingCatalog.CodeSelectControllerType,
            "addMNetherCodeId",
            NetherCodePopupNativeBinding.ConfirmTaskBinding
        ),
        new NetherGeneratedSequenceContract(
            NetherGeneratedSequenceFamily.Cancel,
            "HandleCancelSequenceAsync",
            NetherNativeBindingCatalog.CodeSelectControllerType,
            null,
            NetherCodePopupNativeBinding.CancelTaskBinding
        ),
    };

    public static NetherGeneratedSequenceContract For(NetherGeneratedSequenceFamily family) =>
        Contracts.Single(contract => contract.Family == family);

    public static bool TryResolve(
        Type? utilityType,
        NetherGeneratedSequenceContract contract,
        out NetherGeneratedSequenceTarget? target,
        out string error
    )
    {
        target = null;
        if (contract == null)
        {
            error = "binding-unavailable:generated-sequence:null-contract";
            return false;
        }
        if (utilityType == null)
        {
            error = "binding-unavailable:" + contract.FamilyName + ":missing-type:" + UtilityTypeName;
            return false;
        }
        if (!TryResolveStateMachine(utilityType, contract, out Type? stateMachine, out error))
            return false;
        if (!TryResolveMoveNext(stateMachine!, contract, out MethodInfo? moveNext, out error))
            return false;
        if (!NetherCodePopupInteropResolver.TryResolveStaticMethod(
                utilityType,
                contract.FactoryBinding(contract.ControllerTypeName),
                out string factoryError,
                out MethodInfo? factory
            ))
        {
            error = "binding-unavailable:" + contract.FamilyName + ":factory:" + factoryError;
            return false;
        }

        target = new NetherGeneratedSequenceTarget(contract, stateMachine!, moveNext!, factory!);
        error = string.Empty;
        return true;
    }

    public static bool TryResolveAll(
        Type? utilityType,
        out IReadOnlyList<NetherGeneratedSequenceTarget> targets,
        out IReadOnlyList<string> failures
    )
    {
        var resolved = new List<NetherGeneratedSequenceTarget>();
        var errors = new List<string>();
        foreach (NetherGeneratedSequenceContract contract in Contracts)
        {
            if (TryResolve(utilityType, contract, out NetherGeneratedSequenceTarget? target, out string error))
                resolved.Add(target!);
            else
                errors.Add("generated-sequence:" + contract.FamilyName + " => " + error);
        }
        targets = resolved;
        failures = errors;
        return errors.Count == 0;
    }

    public static bool TryResolveStateMachine(
        Type utilityType,
        NetherGeneratedSequenceContract contract,
        out Type? stateMachine,
        out string error
    )
    {
        stateMachine = null;
        Type[] matches = utilityType
            .GetNestedTypes(NestedFlags)
            .Where(type => MatchesSourceMarker(type, contract.SourceMarker))
            .ToArray();
        if (matches.Length == 0)
        {
            error = "binding-unavailable:"
                + contract.FamilyName
                + ":state-machine:no-source-method:"
                + contract.SourceMarker;
            return false;
        }
        if (matches.Length > 1)
        {
            error = "binding-unavailable:"
                + contract.FamilyName
                + ":state-machine:ambiguous-source-method:"
                + string.Join("|", matches.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal));
            return false;
        }

        Type candidate = matches[0];
        if (!HasInt32Member(candidate, StateFieldName))
        {
            error = "binding-unavailable:"
                + contract.FamilyName
                + ":state-machine:missing-state-field:"
                + candidate.Name;
            return false;
        }
        if (!TryReadDeclaredMemberType(candidate, BuilderFieldName, out Type? builderType)
            || builderType == null
            || !string.Equals(builderType.FullName, AsyncBuilderTypeName, StringComparison.Ordinal))
        {
            error = "binding-unavailable:"
                + contract.FamilyName
                + ":state-machine:missing-builder-field:"
                + candidate.Name;
            return false;
        }
        string? baseName = candidate.BaseType?.FullName;
        if (!string.Equals(baseName, ValueTypeBaseName, StringComparison.Ordinal)
            && !string.Equals(baseName, CoreValueTypeBaseName, StringComparison.Ordinal))
        {
            error = "binding-unavailable:"
                + contract.FamilyName
                + ":state-machine:not-value-type:"
                + candidate.Name
                + ":"
                + (baseName ?? "none");
            return false;
        }

        stateMachine = candidate;
        error = string.Empty;
        return true;
    }

    public static bool TryResolveMoveNext(
        Type stateMachine,
        NetherGeneratedSequenceContract contract,
        out MethodInfo? moveNext,
        out string error
    )
    {
        moveNext = null;
        MethodInfo[] candidates = stateMachine
            .GetMethods(DeclaredInstanceFlags)
            .Where(method => string.Equals(method.Name, MoveNextName, StringComparison.Ordinal))
            .ToArray();
        if (candidates.Length != 1)
        {
            error = "binding-unavailable:"
                + contract.FamilyName
                + ":move-next:"
                + (candidates.Length == 0 ? "missing" : "ambiguous")
                + ":"
                + stateMachine.Name;
            return false;
        }

        MethodInfo candidate = candidates[0];
        if (candidate.ReturnType != typeof(void) || candidate.GetParameters().Length != 0)
        {
            error = "binding-unavailable:"
                + contract.FamilyName
                + ":move-next:unexpected-shape:"
                + stateMachine.Name;
            return false;
        }

        moveNext = candidate;
        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Reads the original type name from the interop package's <c>ObfuscatedNameAttribute</c>.
    /// The generated managed name is not stable across packages, so only this value identifies
    /// the source local function.
    /// </summary>
    public static string? OriginalName(MemberInfo member) =>
        member.CustomAttributes
            .Where(attribute =>
                string.Equals(attribute.AttributeType.Name, "ObfuscatedNameAttribute", StringComparison.Ordinal))
            .SelectMany(attribute => attribute.ConstructorArguments)
            .Select(argument => argument.Value as string)
            .FirstOrDefault(value => !string.IsNullOrEmpty(value));

    private static bool MatchesSourceMarker(Type type, string marker)
    {
        string? original = OriginalName(type);
        return original != null && original.Contains(marker, StringComparison.Ordinal);
    }

    private static bool HasInt32Member(Type type, string name) =>
        TryReadDeclaredMemberType(type, name, out Type? memberType)
        && memberType == typeof(int);

    private static bool TryReadDeclaredMemberType(Type type, string name, out Type? memberType)
    {
        memberType = null;
        PropertyInfo? property = type.GetProperty(name, DeclaredInstanceFlags);
        if (property != null && property.GetIndexParameters().Length == 0)
        {
            memberType = property.PropertyType;
            return true;
        }
        FieldInfo? field = type.GetField(name, DeclaredInstanceFlags)
            ?? type.GetField("<" + name + ">k__BackingField", DeclaredInstanceFlags);
        if (field != null)
        {
            memberType = field.FieldType;
            return true;
        }
        return false;
    }
}
