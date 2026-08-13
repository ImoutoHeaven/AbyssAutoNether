#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherUniTaskFaultDiagnosticTests
{
    [Fact]
    public void Read_PrefersBuilderException_WhenPresent()
    {
        FakeBuilder builder = new()
        {
            ex = new InvalidOperationException("builder exploded"),
            runnerPromise = new FakeRunner(),
        };

        NetherUniTaskFaultDiagnostic diagnostic = NetherUniTaskFaultDiagnosticReader.Read(builder);

        Assert.Equal("builder.ex", diagnostic.Source);
        Assert.Contains("InvalidOperationException", diagnostic.ExceptionSummary);
        Assert.Contains("builder exploded", diagnostic.ExceptionSummary);
        Assert.Contains("builder.ex=present", diagnostic.Probe);
    }

    [Fact]
    public void Read_ExtractsRunnerCompletionCoreException_WhenBuilderExceptionIsEmpty()
    {
        FakeBuilder builder = new()
        {
            runnerPromise = new FakeRunner
            {
                core = new FakeCore
                {
                    error = new FakeExceptionHolder
                    {
                        exception = new FakeExceptionDispatchInfo
                        {
                            SourceException = new InvalidOperationException("runner exploded"),
                        },
                    },
                },
            },
        };

        NetherUniTaskFaultDiagnostic diagnostic = NetherUniTaskFaultDiagnosticReader.Read(builder);

        Assert.Equal("runner.core.error.exception.SourceException", diagnostic.Source);
        Assert.Contains("InvalidOperationException", diagnostic.ExceptionSummary);
        Assert.Contains("runner exploded", diagnostic.ExceptionSummary);
        Assert.Contains("runner=FakeRunner", diagnostic.Probe);
        Assert.Contains("core=FakeCore", diagnostic.Probe);
        Assert.Contains("error=FakeExceptionHolder", diagnostic.Probe);
    }

    [Fact]
    public void Read_ReportsTraversedStructure_WhenNoExceptionCanBeRecovered()
    {
        FakeBuilder builder = new()
        {
            runnerPromise = new FakeRunner
            {
                core = new FakeCore(),
            },
        };

        NetherUniTaskFaultDiagnostic diagnostic = NetherUniTaskFaultDiagnosticReader.Read(builder);

        Assert.Equal(string.Empty, diagnostic.ExceptionSummary);
        Assert.Equal("none", diagnostic.Source);
        Assert.Contains("runner=FakeRunner", diagnostic.Probe);
        Assert.Contains("core=FakeCore", diagnostic.Probe);
        Assert.Contains("error=empty", diagnostic.Probe);
    }

    private sealed class FakeBuilder
    {
        public object? ex;
        public object? runnerPromise;
    }

    private sealed class FakeRunner
    {
        public object? core;
    }

    private sealed class FakeCore
    {
        public object? error;
    }

    private sealed class FakeExceptionHolder
    {
        public object? exception;
    }

    private sealed class FakeExceptionDispatchInfo
    {
        public Exception? SourceException { get; init; }
    }
}
