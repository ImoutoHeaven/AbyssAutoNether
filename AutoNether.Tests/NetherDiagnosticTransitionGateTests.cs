using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherDiagnosticTransitionGateTests
{
    [Fact]
    public void Repeated_pending_signature_is_suppressed_but_transition_and_fault_remain_visible()
    {
        var gate = new NetherDiagnosticTransitionGate();

        Assert.True(gate.ShouldEmit("checkpoint", "AwaitingTerminal|Started|pending"));
        Assert.False(gate.ShouldEmit("checkpoint", "AwaitingTerminal|Started|pending"));
        Assert.True(gate.ShouldEmit("checkpoint", "AwaitingTerminal|UnknownOutcome|faulted"));
        Assert.False(gate.ShouldEmit("checkpoint", "AwaitingTerminal|UnknownOutcome|faulted"));
        Assert.True(gate.ShouldEmit("checkpoint", "Completed|Completed|terminal"));
    }

    [Fact]
    public void Channels_are_independent_and_reset_restores_first_observation()
    {
        var gate = new NetherDiagnosticTransitionGate();

        Assert.True(gate.ShouldEmit("native", "Pending"));
        Assert.True(gate.ShouldEmit("parent", "Pending"));
        Assert.False(gate.ShouldEmit("native", "Pending"));

        gate.Reset();

        Assert.True(gate.ShouldEmit("native", "Pending"));
    }
}
