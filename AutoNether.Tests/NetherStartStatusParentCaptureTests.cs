#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherStartStatusParentCaptureTests
{
    [Fact]
    public void Popup_registered_between_state_machine_enter_and_exit_uses_the_matching_parent_task()
    {
        var capture = new NetherStartStatusParentCapture();
        var stateMachine = new object();
        var controller = new object();
        var parentTask = new object();

        Assert.True(capture.ObserveStateMachineEnter(stateMachine, controller));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.False(capture.IsReady(controller));

        Assert.True(capture.ObserveStateMachineExit(stateMachine, parentTask));
        Assert.True(capture.IsReady(controller));
        Assert.True(capture.TryGetParentTask(controller, out object? captured));
        Assert.Same(parentTask, captured);
    }

    [Fact]
    public void Attached_parent_cannot_be_replaced_by_an_unrelated_state_machine()
    {
        var capture = new NetherStartStatusParentCapture();
        var owner = new object();
        var intruder = new object();
        var controller = new object();
        var ownerTask = new object();

        Assert.True(capture.ObserveStateMachineEnter(owner, controller));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.False(capture.ObserveStateMachineEnter(intruder, controller));
        Assert.False(capture.ObserveStateMachineExit(intruder, new object()));
        Assert.True(capture.ObserveStateMachineExit(owner, ownerTask));

        Assert.True(capture.TryGetParentTask(controller, out object? captured));
        Assert.Same(ownerTask, captured);
    }

    [Fact]
    public void New_state_machine_replaces_an_unattached_stale_candidate()
    {
        var capture = new NetherStartStatusParentCapture();
        var stale = new object();
        var current = new object();
        var controller = new object();
        var currentTask = new object();

        Assert.True(capture.ObserveStateMachineEnter(stale, controller));
        Assert.True(capture.ObserveStateMachineEnter(current, controller));
        Assert.False(capture.ObserveStateMachineExit(stale, new object()));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.True(capture.ObserveStateMachineExit(current, currentTask));

        Assert.True(capture.TryGetParentTask(controller, out object? captured));
        Assert.Same(currentTask, captured);
    }

    [Fact]
    public void Clear_invalidates_state_machine_popup_and_parent_task_together()
    {
        var capture = new NetherStartStatusParentCapture();
        var stateMachine = new object();
        var controller = new object();

        Assert.True(capture.ObserveStateMachineEnter(stateMachine, controller));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.True(capture.ObserveStateMachineExit(stateMachine, new object()));

        capture.Clear();

        Assert.False(capture.IsReady(controller));
        Assert.False(capture.TryGetParentTask(controller, out _));
        Assert.False(capture.TryAttachPopup(controller));
    }

    [Fact]
    public void Same_native_runner_accepts_new_managed_wrappers_and_records_terminal_success()
    {
        var capture = new NetherStartStatusParentCapture();
        var initialWrapper = new object();
        var resumedWrapper = new object();
        var controller = new object();
        var initialTask = new object();
        var terminalTask = new object();

        Assert.True(capture.ObserveStateMachineEnter(initialWrapper, controller, runnerIdentity: 0));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.True(capture.ObserveStateMachineExit(
            initialWrapper,
            initialTask,
            runnerIdentity: 41,
            state: 0,
            taskStatus: "Pending"
        ));

        Assert.True(capture.ObserveStateMachineEnter(resumedWrapper, controller, runnerIdentity: 41));
        Assert.True(capture.ObserveStateMachineExit(
            resumedWrapper,
            terminalTask,
            runnerIdentity: 41,
            state: -2,
            taskStatus: "Succeeded"
        ));

        Assert.True(capture.TryGetParentObservation(
            controller,
            out NetherStartStatusParentObservation observation
        ));
        Assert.Equal(NetherStartStatusParentState.Completed, observation.State);
        Assert.Same(terminalTask, observation.Task);
    }

    [Fact]
    public void Different_native_runner_cannot_replace_an_attached_parent()
    {
        var capture = new NetherStartStatusParentCapture();
        var owner = new object();
        var unrelated = new object();
        var controller = new object();

        Assert.True(capture.ObserveStateMachineEnter(owner, controller, runnerIdentity: 0));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.True(capture.ObserveStateMachineExit(
            owner,
            new object(),
            runnerIdentity: 41,
            state: 0,
            taskStatus: "Pending"
        ));

        Assert.False(capture.ObserveStateMachineEnter(unrelated, controller, runnerIdentity: 99));
        Assert.False(capture.ObserveStateMachineExit(
            unrelated,
            new object(),
            runnerIdentity: 99,
            state: -2,
            taskStatus: "Succeeded"
        ));
        Assert.True(capture.TryGetParentObservation(
            controller,
            out NetherStartStatusParentObservation observation
        ));
        Assert.Equal(NetherStartStatusParentState.Pending, observation.State);
    }

    [Fact]
    public void Wrapper_without_stable_runner_identity_cannot_replace_attached_parent()
    {
        var capture = new NetherStartStatusParentCapture();
        var owner = new object();
        var ambiguous = new object();
        var controller = new object();

        Assert.True(capture.ObserveStateMachineEnter(owner, controller, runnerIdentity: 0));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.False(capture.ObserveStateMachineEnter(ambiguous, controller, runnerIdentity: 0));
    }

    [Fact]
    public void Public_wrapper_task_for_the_same_native_source_can_become_authoritative()
    {
        var capture = new NetherStartStatusParentCapture();
        var directMoveNext = new object();
        var controller = new object();
        var wrapperTask = new object();

        Assert.True(capture.ObserveStateMachineEnter(
            directMoveNext,
            controller,
            runnerIdentity: 41
        ));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.True(capture.ObservePublicTask(
            controller,
            wrapperTask,
            taskIdentity: 41,
            taskStatus: "Pending"
        ));
        Assert.True(capture.TryGetParentObservation(
            controller,
            out NetherStartStatusParentObservation observation
        ));
        Assert.Same(wrapperTask, observation.Task);
        Assert.Equal(NetherStartStatusParentState.Pending, observation.State);
    }

    [Theory]
    [InlineData("Faulted")]
    [InlineData("Canceled")]
    public void Terminal_task_failure_is_not_silently_reclassified(string taskStatus)
    {
        var capture = new NetherStartStatusParentCapture();
        var wrapper = new object();
        var controller = new object();

        Assert.True(capture.ObserveStateMachineEnter(wrapper, controller, runnerIdentity: 0));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.True(capture.ObserveStateMachineExit(
            wrapper,
            new object(),
            runnerIdentity: 7,
            state: -2,
            taskStatus: taskStatus
        ));

        Assert.True(capture.TryGetParentObservation(
            controller,
            out NetherStartStatusParentObservation observation
        ));
        Assert.Equal(NetherStartStatusParentState.Faulted, observation.State);
    }

    [Fact]
    public void Builder_exception_is_preserved_as_a_real_parent_fault()
    {
        var capture = new NetherStartStatusParentCapture();
        var wrapper = new object();
        var controller = new object();

        Assert.True(capture.ObserveStateMachineEnter(wrapper, controller, runnerIdentity: 0));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.True(capture.ObserveStateMachineExit(
            wrapper,
            new object(),
            runnerIdentity: 7,
            state: -2,
            taskStatus: "Faulted",
            builderException: "InvalidOperationException:boom"
        ));

        Assert.True(capture.TryGetParentObservation(
            controller,
            out NetherStartStatusParentObservation observation
        ));
        Assert.Equal(NetherStartStatusParentState.Faulted, observation.State);
        Assert.Contains("InvalidOperationException:boom", observation.Detail);
    }

    [Fact]
    public void Public_wrapper_for_a_different_native_source_cannot_supersede_the_owner()
    {
        var capture = new NetherStartStatusParentCapture();
        var directEntry = new object();
        var publicContinuation = new object();
        var controller = new object();
        var continuationTask = new object();

        Assert.True(capture.ObserveStateMachineEnter(directEntry, controller));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.True(capture.ObserveStateMachineExit(
            directEntry,
            new object(),
            runnerIdentity: 11,
            state: -2,
            taskStatus: "Faulted",
            builderException: "InvalidOperationException:direct-entry-abandoned"
        ));

        Assert.False(capture.ObservePublicTask(
            controller,
            continuationTask,
            taskIdentity: 22,
            taskStatus: "Succeeded"
        ));
        Assert.True(capture.TryGetParentObservation(
            controller,
            out NetherStartStatusParentObservation observation
        ));
        Assert.Equal(NetherStartStatusParentState.Faulted, observation.State);
        Assert.NotSame(continuationTask, observation.Task);
    }

    [Fact]
    public void Captured_success_is_not_downgraded_by_a_recycled_task_source()
    {
        var capture = new NetherStartStatusParentCapture();
        var firstWrapper = new object();
        var recycledWrapper = new object();
        var controller = new object();
        var successfulTask = new object();

        Assert.True(capture.ObserveStateMachineEnter(
            firstWrapper,
            controller,
            runnerIdentity: 77
        ));
        Assert.True(capture.TryAttachPopup(controller));
        Assert.True(capture.ObserveStateMachineExit(
            firstWrapper,
            successfulTask,
            runnerIdentity: 77,
            state: -2,
            taskStatus: "Succeeded"
        ));

        Assert.True(capture.ObserveStateMachineEnter(
            recycledWrapper,
            controller,
            runnerIdentity: 77
        ));
        Assert.True(capture.ObserveStateMachineExit(
            recycledWrapper,
            new object(),
            runnerIdentity: 77,
            state: -2,
            taskStatus: "Faulted",
            builderException: "InvalidOperationException:source-already-consumed"
        ));

        Assert.True(capture.TryGetParentObservation(
            controller,
            out NetherStartStatusParentObservation observation
        ));
        Assert.Equal(NetherStartStatusParentState.Completed, observation.State);
        Assert.Same(successfulTask, observation.Task);
    }
}
