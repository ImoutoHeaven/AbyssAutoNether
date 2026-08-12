#nullable enable

using System;

namespace AutoNether.Services;

internal enum NetherStartStatusParentState
{
    None,
    Pending,
    Completed,
    Faulted,
}

internal readonly record struct NetherStartStatusParentObservation(
    NetherStartStatusParentState State,
    object? Task,
    string Detail
);

/// <summary>
/// Correlates the generated HandleStartEventByStatusAsync state machine with the code popup
/// it owns. IL2CPP may expose a different managed wrapper for the same native state-machine
/// runner on each MoveNext; the runner identity, not CLR reference identity, owns the flow.
/// Terminal state is recorded inside the MoveNext postfix before the pooled UniTask source can
/// be consumed and recycled by the game's original caller.
/// </summary>
internal sealed class NetherStartStatusParentCapture
{
    private object? _stateMachine;
    private object? _controller;
    private object? _parentTask;
    private long _runnerIdentity;
    private bool _popupAttached;
    private NetherStartStatusParentState _parentState;
    private string _detail = string.Empty;

    public bool ObserveStateMachineEnter(
        object stateMachine,
        object controller,
        long runnerIdentity = 0
    )
    {
        ArgumentNullException.ThrowIfNull(stateMachine);
        ArgumentNullException.ThrowIfNull(controller);

        if (ReferenceEquals(_stateMachine, stateMachine))
        {
            if (!ReferenceEquals(_controller, controller))
                return false;
            if (_popupAttached
                && _runnerIdentity > 0
                && runnerIdentity > 0
                && runnerIdentity != _runnerIdentity)
            {
                return false;
            }
            if (runnerIdentity > 0)
                _runnerIdentity = runnerIdentity;
            return true;
        }

        bool sameNativeRunner = _popupAttached
            && _runnerIdentity > 0
            && runnerIdentity == _runnerIdentity
            && ReferenceEquals(_controller, controller);
        if (sameNativeRunner)
        {
            _stateMachine = stateMachine;
            return true;
        }
        if (_popupAttached)
            return false;

        _stateMachine = stateMachine;
        _controller = controller;
        _parentTask = null;
        _runnerIdentity = runnerIdentity;
        _popupAttached = false;
        _parentState = NetherStartStatusParentState.None;
        _detail = string.Empty;
        return true;
    }

    public bool TryAttachPopup(object controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (_stateMachine == null || !ReferenceEquals(_controller, controller))
            return false;
        _popupAttached = true;
        return true;
    }

    public bool ObservePublicTask(
        object controller,
        object parentTask,
        long taskIdentity,
        string taskStatus
    )
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(parentTask);
        if (_controller != null && !ReferenceEquals(_controller, controller))
            return false;
        if (_popupAttached
            && (_runnerIdentity <= 0 || taskIdentity <= 0 || taskIdentity != _runnerIdentity))
        {
            return false;
        }

        _controller = controller;
        if (taskIdentity > 0)
            _runnerIdentity = taskIdentity;
        if (_parentState == NetherStartStatusParentState.Completed)
            return true;

        _parentTask = parentTask;
        if (string.Equals(taskStatus, "Succeeded", StringComparison.Ordinal))
        {
            _parentState = NetherStartStatusParentState.Completed;
            _detail = "native-start-status-wrapper-terminal";
        }
        else if (string.Equals(taskStatus, "Faulted", StringComparison.Ordinal)
            || string.Equals(taskStatus, "Canceled", StringComparison.Ordinal))
        {
            _parentState = NetherStartStatusParentState.Faulted;
            _detail = "native-start-status-wrapper-" + taskStatus.ToLowerInvariant();
        }
        else
        {
            _parentState = NetherStartStatusParentState.Pending;
            _detail = "native-start-status-wrapper-pending:" + taskStatus;
        }
        return true;
    }

    public bool ObserveStateMachineExit(
        object stateMachine,
        object parentTask,
        long runnerIdentity = 0,
        int state = int.MinValue,
        string taskStatus = "",
        string builderException = ""
    )
    {
        ArgumentNullException.ThrowIfNull(stateMachine);
        ArgumentNullException.ThrowIfNull(parentTask);
        if (!ReferenceEquals(_stateMachine, stateMachine))
            return false;
        if (_popupAttached
            && _runnerIdentity > 0
            && runnerIdentity > 0
            && runnerIdentity != _runnerIdentity)
        {
            return false;
        }

        // SetResult is terminal for this exact native runner.  A later managed wrapper can
        // still expose the already-consumed pooled UniTask source as Faulted; never let that
        // stale observation overwrite the success captured in the MoveNext postfix.
        if (_parentState == NetherStartStatusParentState.Completed)
            return true;

        _parentTask = parentTask;
        if (runnerIdentity > 0)
            _runnerIdentity = runnerIdentity;

        if (!string.IsNullOrEmpty(builderException))
        {
            _parentState = NetherStartStatusParentState.Faulted;
            _detail = "native-start-status-exception:" + builderException;
        }
        else if (state == -2 && string.Equals(taskStatus, "Succeeded", StringComparison.Ordinal))
        {
            // The generated state machine reached SetResult. A later Status read can report
            // Faulted after its pooled source was consumed; the terminal state wins.
            _parentState = NetherStartStatusParentState.Completed;
            _detail = "native-start-status-terminal:" + taskStatus;
        }
        else if (state == -2 && string.Equals(taskStatus, "Faulted", StringComparison.Ordinal))
        {
            _parentState = NetherStartStatusParentState.Faulted;
            _detail = "native-start-status-terminal-faulted";
        }
        else if (state == -2 && string.Equals(taskStatus, "Canceled", StringComparison.Ordinal))
        {
            _parentState = NetherStartStatusParentState.Faulted;
            _detail = "native-start-status-terminal-canceled";
        }
        else
        {
            _parentState = NetherStartStatusParentState.Pending;
            _detail = "native-start-status-pending:" + taskStatus;
        }
        return true;
    }

    public bool IsReady(object? currentController) =>
        currentController != null
        && _popupAttached
        && _parentTask != null
        && ReferenceEquals(_controller, currentController);

    public bool TryGetParentTask(object? currentController, out object? parentTask)
    {
        parentTask = IsReady(currentController) ? _parentTask : null;
        return parentTask != null;
    }

    public bool TryGetObservedParentTask(object? currentController, out object? parentTask)
    {
        parentTask = HasCandidateFor(currentController) ? _parentTask : null;
        return parentTask != null;
    }

    public bool TryGetParentObservation(
        object? currentController,
        out NetherStartStatusParentObservation observation
    )
    {
        if (!IsReady(currentController))
        {
            observation = default;
            return false;
        }

        observation = new NetherStartStatusParentObservation(
            _parentState,
            _parentTask,
            _detail
        );
        return true;
    }

    public bool HasCandidateFor(object? currentController) =>
        currentController != null
        && _stateMachine != null
        && ReferenceEquals(_controller, currentController);

    public bool PopupAttached => _popupAttached;

    public void Clear()
    {
        _stateMachine = null;
        _controller = null;
        _parentTask = null;
        _runnerIdentity = 0;
        _popupAttached = false;
        _parentState = NetherStartStatusParentState.None;
        _detail = string.Empty;
    }
}
