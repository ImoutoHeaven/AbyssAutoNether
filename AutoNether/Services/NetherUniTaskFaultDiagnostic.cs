#nullable enable

using System;
using System.Reflection;

namespace AutoNether.Services;

internal delegate bool NetherUniTaskFaultMemberReader(
    object target,
    string name,
    out object? value
);

internal readonly record struct NetherUniTaskFaultDiagnostic(
    string ExceptionSummary,
    string Source,
    string Probe
);

/// <summary>
/// Read-only diagnostic walker for IL2CPP UniTask async builders.  Some generated async
/// methods leave builder.ex empty while their pooled runner stores the actual failure in
/// runnerPromise.core.error.exception.  This reader never invokes a task or continuation;
/// it only produces bounded evidence for a terminal fault log.
/// </summary>
internal static class NetherUniTaskFaultDiagnosticReader
{
    private const int MaximumTextLength = 240;

    public static NetherUniTaskFaultDiagnostic Read(object builder) =>
        Read(builder, TryReadMember);

    public static NetherUniTaskFaultDiagnostic Read(
        object builder,
        NetherUniTaskFaultMemberReader reader
    )
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        try
        {
            if (reader(builder, "ex", out object? builderException)
                && builderException != null
                && TryDescribeException(builderException, reader, out string directSummary, out _))
            {
                return new(
                    Bound(directSummary),
                    "builder.ex",
                    "builder.ex=present"
                );
            }

            string probe = "builder.ex=empty";
            if (!reader(builder, "runnerPromise", out object? runner) || runner == null)
                return new(string.Empty, "none", probe + ";runner=missing");
            probe += ";runner=" + TypeName(runner);

            if (!reader(runner, "core", out object? core) || core == null)
                return new(string.Empty, "none", Bound(probe + ";core=missing"));
            probe += ";core=" + TypeName(core);

            if (!reader(core, "error", out object? error) || error == null)
                return new(string.Empty, "none", Bound(probe + ";error=empty"));
            probe += ";error=" + TypeName(error);

            if (TryDescribeException(error, reader, out string summary, out string suffix))
            {
                return new(
                    Bound(summary),
                    "runner.core.error" + suffix,
                    Bound(probe)
                );
            }

            return new(string.Empty, "none", Bound(probe + ";exception=unreadable"));
        }
        catch (Exception exception)
        {
            return new(
                string.Empty,
                "probe-failed",
                Bound(exception.GetType().Name + ":" + exception.Message)
            );
        }
    }

    private static bool TryDescribeException(
        object candidate,
        NetherUniTaskFaultMemberReader reader,
        out string summary,
        out string sourceSuffix
    )
    {
        if (candidate is Exception managed)
        {
            summary = managed.GetType().FullName + ":" + managed.Message;
            sourceSuffix = string.Empty;
            return true;
        }

        if (reader(candidate, "exception", out object? held) && held != null)
        {
            if (TryDescribeExceptionObjectOrDispatch(held, reader, out summary, out string heldSuffix))
            {
                sourceSuffix = ".exception" + heldSuffix;
                return true;
            }
        }

        if (TryDescribeExceptionObjectOrDispatch(candidate, reader, out summary, out string suffix))
        {
            sourceSuffix = suffix;
            return true;
        }

        summary = string.Empty;
        sourceSuffix = string.Empty;
        return false;
    }

    private static bool TryDescribeExceptionObjectOrDispatch(
        object candidate,
        NetherUniTaskFaultMemberReader reader,
        out string summary,
        out string sourceSuffix
    )
    {
        if (candidate is Exception managed)
        {
            summary = managed.GetType().FullName + ":" + managed.Message;
            sourceSuffix = string.Empty;
            return true;
        }

        foreach (string name in new[] { "SourceException", "Exception", "_exception", "m_Exception" })
        {
            if (!reader(candidate, name, out object? nested) || nested == null)
                continue;
            if (nested is Exception nestedManaged)
            {
                summary = nestedManaged.GetType().FullName + ":" + nestedManaged.Message;
                sourceSuffix = "." + name;
                return true;
            }
            if (TryReadExceptionLike(nested, reader, out summary))
            {
                sourceSuffix = "." + name;
                return true;
            }
        }

        if (TryReadExceptionLike(candidate, reader, out summary))
        {
            sourceSuffix = string.Empty;
            return true;
        }

        sourceSuffix = string.Empty;
        return false;
    }

    private static bool TryReadExceptionLike(
        object candidate,
        NetherUniTaskFaultMemberReader reader,
        out string summary
    )
    {
        string type = candidate.GetType().FullName ?? candidate.GetType().Name;
        if (type.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) < 0)
        {
            summary = string.Empty;
            return false;
        }

        string message = reader(candidate, "Message", out object? rawMessage)
            && rawMessage != null
                ? rawMessage.ToString() ?? string.Empty
                : candidate.ToString() ?? string.Empty;
        summary = type + ":" + message;
        return true;
    }

    private static bool TryReadMember(object target, string name, out object? value)
    {
        value = null;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();
        PropertyInfo? property = type.GetProperty(name, flags);
        if (property != null && property.GetIndexParameters().Length == 0)
        {
            value = property.GetValue(target);
            return true;
        }
        FieldInfo? field = type.GetField(name, flags)
            ?? type.GetField("<" + name + ">k__BackingField", flags);
        if (field == null)
            return false;
        value = field.GetValue(target);
        return true;
    }

    private static string TypeName(object value) => value.GetType().Name;

    private static string Bound(string value) =>
        value.Length <= MaximumTextLength
            ? value
            : value.Substring(0, MaximumTextLength);
}
