#nullable enable

using System;
using System.Reflection;

namespace AutoNether.Services;

internal readonly record struct NetherNativeTaskStatusRead(bool IsAvailable, object? Value)
{
    public static NetherNativeTaskStatusRead Available(object? value) => new(true, value);

    public static NetherNativeTaskStatusRead Missing() => new(false, null);
}

/// <summary>
/// Converts the reflective Status boundary of a native UniTask into the bridge's terminal result
/// vocabulary. The reader delegate keeps Il2Cpp reflection outside the pure behavioral seam.
/// </summary>
internal static class NetherNativeTaskStatusPoller
{
    public static NetherNativeActionResult Poll(Func<NetherNativeTaskStatusRead> readStatus)
    {
        ArgumentNullException.ThrowIfNull(readStatus);

        try
        {
            NetherNativeTaskStatusRead read = readStatus();
            if (!read.IsAvailable || read.Value == null)
                return NetherNativeActionResult.BindingUnavailable("missing-result-task-status");

            string status = read.Value.ToString() ?? string.Empty;
            if (string.Equals(status, "Pending", StringComparison.Ordinal))
                return NetherNativeActionResult.Started("awaiting-native-result");
            if (string.Equals(status, "Succeeded", StringComparison.Ordinal))
                return NetherNativeActionResult.Completed("native-result-succeeded");
            if (string.Equals(status, "Canceled", StringComparison.Ordinal))
                return NetherNativeActionResult.UnknownOutcome("native-result-canceled");
            if (string.Equals(status, "Faulted", StringComparison.Ordinal))
                return NetherNativeActionResult.UnknownOutcome("native-result-faulted");
            return NetherNativeActionResult.UnknownOutcome("unknown-native-result-status:" + status);
        }
        catch (Exception ex)
        {
            Exception root = ex;
            while (root is TargetInvocationException { InnerException: not null } invocation)
                root = invocation.InnerException!;
            return NetherNativeActionResult.UnknownOutcome(
                "native-result-status-exception:" + root.GetType().Name + ":" + root.Message
            );
        }
    }
}
