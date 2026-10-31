#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherGeneratedSequenceObservationTests
{
    [Fact]
    public void Entry_then_completion_is_the_shared_terminal_contract()
    {
        var registry = new NetherGeneratedSequenceObservationRegistry();
        object controller = new();

        Assert.Equal(
            NetherGeneratedSequencePhase.None,
            registry.Current(NetherGeneratedSequenceFamily.Transform).Phase
        );

        long token = registry.Arm(NetherGeneratedSequenceFamily.Transform, controller, 9001, 40024);
        Assert.True(registry.ObserveEntry(
            NetherGeneratedSequenceFamily.Transform,
            controller,
            9001,
            40024,
            out var started
        ));
        Assert.Equal(token, started.Token);
        Assert.Equal(NetherGeneratedSequencePhase.Started, started.Phase);

        Assert.True(registry.ObserveTerminal(
            NetherGeneratedSequenceFamily.Transform,
            controller,
            9001,
            40024,
            NetherGeneratedSequencePhase.Completed,
            "state-machine-completed",
            out var completed
        ));
        Assert.Equal(NetherGeneratedSequencePhase.Completed, completed.Phase);
        Assert.True(registry.Current(NetherGeneratedSequenceFamily.Transform).IsTerminal);
    }

    [Fact]
    public void Completion_is_never_downgraded_by_a_later_fault()
    {
        var registry = new NetherGeneratedSequenceObservationRegistry();
        object controller = new();
        registry.Arm(NetherGeneratedSequenceFamily.Confirm, controller, 7, 12);
        registry.ObserveEntry(NetherGeneratedSequenceFamily.Confirm, controller, 7, 12, out _);
        registry.ObserveTerminal(
            NetherGeneratedSequenceFamily.Confirm,
            controller,
            7,
            12,
            NetherGeneratedSequencePhase.Completed,
            "done",
            out _
        );

        Assert.False(registry.ObserveTerminal(
            NetherGeneratedSequenceFamily.Confirm,
            controller,
            7,
            12,
            NetherGeneratedSequencePhase.Faulted,
            "late",
            out var observation
        ));
        Assert.Equal(NetherGeneratedSequencePhase.Completed, observation.Phase);
    }

    [Fact]
    public void Unarmed_different_controller_and_foreign_runner_observations_are_ignored()
    {
        var registry = new NetherGeneratedSequenceObservationRegistry();
        object armedController = new();
        object otherController = new();

        Assert.False(registry.ObserveEntry(NetherGeneratedSequenceFamily.Cancel, armedController, 0, 0, out _));

        registry.Arm(NetherGeneratedSequenceFamily.Cancel, armedController, 3, 0);
        Assert.False(registry.ObserveEntry(NetherGeneratedSequenceFamily.Cancel, otherController, 4, 0, out _));

        // A terminal that never passed the entry check cannot satisfy an armed slot.
        Assert.False(registry.ObserveTerminal(
            NetherGeneratedSequenceFamily.Cancel,
            armedController,
            3,
            0,
            NetherGeneratedSequencePhase.Completed,
            "terminal-without-entry",
            out _
        ));
        Assert.Equal(
            NetherGeneratedSequencePhase.None,
            registry.Current(NetherGeneratedSequenceFamily.Cancel).Phase
        );

        Assert.True(registry.ObserveEntry(NetherGeneratedSequenceFamily.Cancel, armedController, 3, 0, out _));
        Assert.True(registry.ObserveTerminal(
            NetherGeneratedSequenceFamily.Cancel,
            armedController,
            3,
            0,
            NetherGeneratedSequencePhase.Completed,
            "entered-then-completed",
            out _
        ));
    }

    [Fact]
    public void Re_arming_starts_a_new_token_and_ignores_the_previous_terminal()
    {
        var registry = new NetherGeneratedSequenceObservationRegistry();
        object controller = new();
        long first = registry.Arm(NetherGeneratedSequenceFamily.Transform, controller, 55, 40024);
        registry.ObserveEntry(NetherGeneratedSequenceFamily.Transform, controller, 55, 40024, out _);
        registry.ObserveTerminal(
            NetherGeneratedSequenceFamily.Transform,
            controller,
            55,
            40024,
            NetherGeneratedSequencePhase.Completed,
            "first",
            out _
        );

        long second = registry.Arm(NetherGeneratedSequenceFamily.Transform, controller, 55, 40025);
        Assert.NotEqual(first, second);
        Assert.Equal(
            NetherGeneratedSequencePhase.None,
            registry.Current(NetherGeneratedSequenceFamily.Transform).Phase
        );
        Assert.True(registry.ObserveEntry(NetherGeneratedSequenceFamily.Transform, controller, 55, 40025, out _));
    }

    [Fact]
    public void Code_id_mismatch_and_resume_after_entry_are_ignored()
    {
        var registry = new NetherGeneratedSequenceObservationRegistry();
        object controller = new();
        registry.Arm(NetherGeneratedSequenceFamily.Transform, controller, 55, 40024);

        Assert.False(registry.ObserveEntry(NetherGeneratedSequenceFamily.Transform, controller, 55, 40025, out _));
        Assert.True(registry.ObserveEntry(NetherGeneratedSequenceFamily.Transform, controller, 55, 40024, out _));
        Assert.False(registry.ObserveEntry(NetherGeneratedSequenceFamily.Transform, controller, 55, 40024, out _));
    }

    [Fact]
    public void Faulted_terminal_state_is_reported_and_cleared_by_clear()
    {
        var registry = new NetherGeneratedSequenceObservationRegistry();
        object controller = new();
        registry.Arm(NetherGeneratedSequenceFamily.Transform, controller, 1, 40024);
        registry.ObserveEntry(NetherGeneratedSequenceFamily.Transform, controller, 1, 40024, out _);
        Assert.True(registry.ObserveTerminal(
            NetherGeneratedSequenceFamily.Transform,
            controller,
            1,
            40024,
            NetherGeneratedSequencePhase.Faulted,
            "state-machine-faulted",
            out var faulted
        ));
        Assert.Equal(NetherGeneratedSequencePhase.Faulted, faulted.Phase);

        registry.Clear(NetherGeneratedSequenceFamily.Transform);
        Assert.Equal(
            NetherGeneratedSequencePhase.None,
            registry.Current(NetherGeneratedSequenceFamily.Transform).Phase
        );
        Assert.Equal(0, registry.Current(NetherGeneratedSequenceFamily.Transform).Token);
    }
}
