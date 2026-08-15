#nullable enable

using System.Reflection;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherNativeTaskStatusPollerTests
{
    [Fact]
    public void Native_status_exception_is_contained_as_one_terminal_result()
    {
        NetherNativeActionResult result = default;
        Exception? escaped = Record.Exception(() =>
        {
            result = NetherNativeTaskStatusPoller.Poll(() =>
                throw new TargetInvocationException(
                    new InvalidOperationException(
                        "Token version is not matched, can not await twice or get Status after await."
                    )
                )
            );
        });

        Assert.Null(escaped);
        Assert.Equal(NetherNativeActionResultKind.UnknownOutcome, result.Kind);
        Assert.Contains("InvalidOperationException", result.Detail, StringComparison.Ordinal);
        Assert.Contains("Token version is not matched", result.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_bridge_routes_native_status_getter_through_containing_boundary()
    {
        string bridge = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Services",
            "NetherRuntimeBridge.cs"
        ));
        int start = bridge.IndexOf(
            "private static NetherNativeActionResult PollResultTask(object task)",
            StringComparison.Ordinal
        );
        int end = bridge.IndexOf(
            "private bool TryCompleteBattleTask(",
            start,
            StringComparison.Ordinal
        );
        Assert.True(start >= 0 && end > start, "unable to bound runtime task poller");
        string method = bridge.Substring(start, end - start);

        Assert.Contains("NetherNativeTaskStatusPoller.Poll(", method, StringComparison.Ordinal);
        Assert.Contains("TryReadMember(task, \"Status\"", method, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AutoNether.Tests", "AutoNether.Tests.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
