#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Owns the generated HandleStartEventByStatusAsync state-machine task started for one exact
/// checkpoint action. The public method's boxed UniTask is only a wrapper observation: IL2CPP can
/// expose it as a source-less, already-succeeded value while the generated runner is still
/// awaiting Continue/Finish work, so it is never authoritative for an initiated checkpoint.
/// </summary>
internal sealed class NetherCheckpointStartStatusParentCapture
{
    private readonly NetherStartStatusParentCapture _generatedParent = new();
    private object? _controller;
    private long _ownerGeneration;

    public bool Begin(object controller, long ownerGeneration)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (ownerGeneration < 1 || _ownerGeneration > 0)
            return false;

        _generatedParent.Clear();
        _controller = controller;
        _ownerGeneration = ownerGeneration;
        return true;
    }

    public bool IsActiveFor(object? controller, long ownerGeneration) =>
        controller != null
        && ownerGeneration > 0
        && ownerGeneration == _ownerGeneration
        && ReferenceEquals(controller, _controller);

    public bool ObserveStateMachineEnter(
        object stateMachine,
        object controller,
        long ownerGeneration,
        long runnerIdentity = 0
    ) => IsActiveFor(controller, ownerGeneration)
        && _generatedParent.ObserveStateMachineEnter(
            stateMachine,
            controller,
            runnerIdentity
        );

    public bool ObserveStateMachineExit(
        object stateMachine,
        object parentTask,
        object controller,
        long ownerGeneration,
        long runnerIdentity = 0,
        int state = int.MinValue,
        string taskStatus = "",
        string builderException = ""
    ) => IsActiveFor(controller, ownerGeneration)
        && _generatedParent.ObserveStateMachineExit(
            stateMachine,
            parentTask,
            runnerIdentity,
            state,
            taskStatus,
            builderException
        );

    public bool TryAttachPopup(object controller, long ownerGeneration) =>
        IsActiveFor(controller, ownerGeneration)
        && _generatedParent.TryAttachPopup(controller);

    /// <summary>
    /// Deliberately observation-only. An initiated checkpoint is correlated before the public
    /// invocation, so only its generated builder task may settle the parent.
    /// </summary>
    public bool ObservePublicTask(
        object controller,
        long ownerGeneration,
        object parentTask,
        long taskIdentity,
        string taskStatus
    )
    {
        ArgumentNullException.ThrowIfNull(parentTask);
        _ = controller;
        _ = ownerGeneration;
        _ = taskIdentity;
        _ = taskStatus;
        return false;
    }

    public bool TryGetParentObservation(
        object controller,
        long ownerGeneration,
        out NetherStartStatusParentObservation observation
    )
    {
        if (!IsActiveFor(controller, ownerGeneration))
        {
            observation = default;
            return false;
        }
        return _generatedParent.TryGetParentObservation(controller, out observation);
    }

    public bool HasCandidateFor(object controller, long ownerGeneration) =>
        IsActiveFor(controller, ownerGeneration)
        && _generatedParent.HasCandidateFor(controller);

    /// <summary>
    /// Native Finish changes to Result and then awaits work with the FloorSelection owner's
    /// GetCancellationTokenOnDestroy token. Destroying that exact owner therefore terminates the
    /// generated parent as Canceled after the Result transition has already been submitted.
    /// No other cancellation or fault is converted to success.
    /// </summary>
    public static NetherStartStatusParentObservation ResolveFinishObservation(
        NetherStartStatusParentObservation observation,
        bool exactOwnerTerminated
    ) => exactOwnerTerminated
        && observation.State == NetherStartStatusParentState.Faulted
        && string.Equals(
            observation.Detail,
            "native-start-status-terminal-canceled",
            StringComparison.Ordinal
        )
            ? observation with
            {
                State = NetherStartStatusParentState.Completed,
                Detail = "native-finish-parent-canceled-after-owner-transition",
            }
            : observation;

    public void Clear()
    {
        _generatedParent.Clear();
        _controller = null;
        _ownerGeneration = 0;
    }
}
