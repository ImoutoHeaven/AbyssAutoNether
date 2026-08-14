#nullable enable

using System;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherCodeCancelTaskInvokerTests
{
    [Fact]
    public void Invoke_calls_exact_cancel_task_once_with_inherited_live_token()
    {
        FakeUtility.Reset();
        var token = new FakeCancellationToken(41);
        var controller = new FakeController(token);

        bool invoked = NetherCodeCancelTaskInvoker.TryInvoke(
            controller,
            typeof(FakeUtility),
            BindingFor<FakeController>(),
            out object? task,
            out string error
        );

        Assert.True(invoked, error);
        Assert.Same(FakeUtility.ReturnedTask, task);
        Assert.Equal(1, FakeUtility.CallCount);
        Assert.Same(controller, FakeUtility.Controller);
        Assert.Equal(token, FakeUtility.Token);
    }

    [Fact]
    public void Invoke_fails_before_native_call_when_cancellation_token_is_missing()
    {
        MissingTokenUtility.Reset();
        var controller = new MissingTokenController();

        bool invoked = NetherCodeCancelTaskInvoker.TryInvoke(
            controller,
            typeof(MissingTokenUtility),
            BindingFor<MissingTokenController>(nameof(MissingTokenUtility.Cancel)),
            out object? task,
            out string error
        );

        Assert.False(invoked);
        Assert.Null(task);
        Assert.Equal(0, MissingTokenUtility.CallCount);
        Assert.Contains("_cancellationToken", error, StringComparison.Ordinal);
    }

    private static NetherCodePopupInteropMethodBinding BindingFor<TController>(
        string methodName = nameof(FakeUtility.Cancel)
    ) => new(
        methodName,
        null,
        new[]
        {
            typeof(TController).FullName!,
            typeof(FakeCancellationToken).FullName!,
        },
        typeof(FakeUniTask).FullName!
    ) { IsStatic = true };

    private class FakeControllerBase
    {
        protected readonly FakeCancellationToken _cancellationToken;

        protected FakeControllerBase(FakeCancellationToken cancellationToken) =>
            _cancellationToken = cancellationToken;
    }

    private sealed class FakeController : FakeControllerBase
    {
        public FakeController(FakeCancellationToken cancellationToken) : base(cancellationToken)
        {
        }
    }

    private sealed class MissingTokenController
    {
    }

    private readonly record struct FakeCancellationToken(int Value);

    private sealed class FakeUniTask
    {
    }

    private static class FakeUtility
    {
        public static readonly FakeUniTask ReturnedTask = new();
        public static int CallCount { get; private set; }
        public static FakeController? Controller { get; private set; }
        public static FakeCancellationToken Token { get; private set; }

        public static FakeUniTask Cancel(FakeController controller, FakeCancellationToken token)
        {
            CallCount++;
            Controller = controller;
            Token = token;
            return ReturnedTask;
        }

        public static void Reset()
        {
            CallCount = 0;
            Controller = null;
            Token = default;
        }
    }

    private static class MissingTokenUtility
    {
        public static int CallCount { get; private set; }

        public static FakeUniTask Cancel(
            MissingTokenController controller,
            FakeCancellationToken token
        )
        {
            CallCount++;
            return new FakeUniTask();
        }

        public static void Reset() => CallCount = 0;
    }
}
