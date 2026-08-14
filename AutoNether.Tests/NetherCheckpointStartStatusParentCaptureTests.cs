#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCheckpointStartStatusParentCaptureTests
{
    [Fact]
    public void Source_less_public_wrapper_cannot_complete_an_initiated_checkpoint_parent()
    {
        var capture = new NetherCheckpointStartStatusParentCapture();
        var controller = new object();
        var initialStateMachine = new object();
        var resumedStateMachine = new object();
        var generatedPendingTask = new object();
        var generatedTerminalTask = new object();
        var publicWrapperTask = new object();

        Assert.True(capture.Begin(controller, ownerGeneration: 23));
        Assert.True(capture.ObserveStateMachineEnter(
            initialStateMachine,
            controller,
            ownerGeneration: 23,
            runnerIdentity: 41
        ));
        Assert.True(capture.TryAttachPopup(controller, ownerGeneration: 23));
        Assert.True(capture.ObserveStateMachineExit(
            initialStateMachine,
            generatedPendingTask,
            controller,
            ownerGeneration: 23,
            runnerIdentity: 41,
            state: 0,
            taskStatus: "Pending"
        ));

        Assert.False(capture.ObservePublicTask(
            controller,
            ownerGeneration: 23,
            publicWrapperTask,
            taskIdentity: 0,
            taskStatus: "Succeeded"
        ));
        Assert.True(capture.TryGetParentObservation(
            controller,
            ownerGeneration: 23,
            out NetherStartStatusParentObservation pending
        ));
        Assert.Equal(NetherStartStatusParentState.Pending, pending.State);
        Assert.Same(generatedPendingTask, pending.Task);

        Assert.True(capture.ObserveStateMachineEnter(
            resumedStateMachine,
            controller,
            ownerGeneration: 23,
            runnerIdentity: 41
        ));
        Assert.True(capture.ObserveStateMachineExit(
            resumedStateMachine,
            generatedTerminalTask,
            controller,
            ownerGeneration: 23,
            runnerIdentity: 41,
            state: -2,
            taskStatus: "Succeeded"
        ));
        Assert.True(capture.TryGetParentObservation(
            controller,
            ownerGeneration: 23,
            out NetherStartStatusParentObservation terminal
        ));
        Assert.Equal(NetherStartStatusParentState.Completed, terminal.State);
        Assert.Same(generatedTerminalTask, terminal.Task);
    }

    [Fact]
    public void Controller_and_generation_are_both_required_for_checkpoint_evidence()
    {
        var capture = new NetherCheckpointStartStatusParentCapture();
        var controller = new object();
        var otherController = new object();
        var stateMachine = new object();

        Assert.True(capture.Begin(controller, ownerGeneration: 7));
        Assert.False(capture.ObserveStateMachineEnter(
            stateMachine,
            otherController,
            ownerGeneration: 7,
            runnerIdentity: 11
        ));
        Assert.False(capture.ObserveStateMachineEnter(
            stateMachine,
            controller,
            ownerGeneration: 8,
            runnerIdentity: 11
        ));
        Assert.False(capture.TryAttachPopup(otherController, ownerGeneration: 7));
        Assert.False(capture.TryAttachPopup(controller, ownerGeneration: 8));
    }

    [Fact]
    public void Finish_cancellation_is_terminal_success_only_after_the_exact_owner_was_destroyed()
    {
        var capture = new NetherCheckpointStartStatusParentCapture();
        var controller = new object();
        var initialStateMachine = new object();
        var resumedStateMachine = new object();

        Assert.True(capture.Begin(controller, ownerGeneration: 31));
        Assert.True(capture.ObserveStateMachineEnter(
            initialStateMachine,
            controller,
            ownerGeneration: 31
        ));
        Assert.True(capture.TryAttachPopup(controller, ownerGeneration: 31));
        Assert.True(capture.ObserveStateMachineExit(
            initialStateMachine,
            new object(),
            controller,
            ownerGeneration: 31,
            runnerIdentity: 71,
            state: 3,
            taskStatus: "Pending"
        ));

        Assert.True(capture.ObserveStateMachineEnter(
            resumedStateMachine,
            controller,
            ownerGeneration: 31,
            runnerIdentity: 71
        ));
        Assert.True(capture.ObserveStateMachineExit(
            resumedStateMachine,
            new object(),
            controller,
            ownerGeneration: 31,
            runnerIdentity: 71,
            state: -2,
            taskStatus: "Canceled"
        ));
        Assert.True(capture.TryGetParentObservation(
            controller,
            ownerGeneration: 31,
            out NetherStartStatusParentObservation canceled
        ));
        NetherStartStatusParentObservation canceledBeforeTeardown =
            NetherCheckpointStartStatusParentCapture.ResolveFinishObservation(
                canceled,
                exactOwnerTerminated: false
            );
        Assert.Equal(NetherStartStatusParentState.Faulted, canceledBeforeTeardown.State);

        NetherStartStatusParentObservation transitioned =
            NetherCheckpointStartStatusParentCapture.ResolveFinishObservation(
                canceled,
                exactOwnerTerminated: true
            );
        Assert.Equal(NetherStartStatusParentState.Completed, transitioned.State);
        Assert.Equal(
            "native-finish-parent-canceled-after-owner-transition",
            transitioned.Detail
        );
    }
}
