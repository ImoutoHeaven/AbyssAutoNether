using System.Collections.Generic;
using AutoNether.Services;
using UnityEngine;

namespace AutoNether;

public sealed class Hotkey : MonoBehaviour
{
    private const float DebounceInterval = 0.15f;
    private readonly Dictionary<KeyCode, float> _lastPressTime = new();

    private void Update()
    {
        bool keyDown = Input.GetKeyDown(KeyCode.F12);
        bool accepted = keyDown && CanTrigger(KeyCode.F12);
        if (keyDown)
            NetherAutoClimbController.ObserveHotkeyInput(accepted);

        ConfigAutoReload.Update(Time.unscaledTime);
        NetherAutoClimbController.Update();

        if (accepted)
            NetherAutoClimbController.ToggleFromHotkey();
    }

    private bool CanTrigger(KeyCode key)
    {
        float now = Time.unscaledTime;
        if (_lastPressTime.TryGetValue(key, out float last) && now - last < DebounceInterval)
            return false;

        _lastPressTime[key] = now;
        return true;
    }
}
