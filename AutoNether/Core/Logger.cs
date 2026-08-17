#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BepInEx.Logging;

namespace AutoNether;

/// <summary>
/// 统一日志封装，避免各处直接引用 Plugin.Log。
/// </summary>
public static class Logger
{
    private static readonly ConcurrentQueue<string> CapturedWhenUnbound = new();
    private static ManualLogSource? _boundLog;

    internal static IReadOnlyCollection<string> Messages => CapturedWhenUnbound.ToArray();

    internal static void Reset()
    {
        while (CapturedWhenUnbound.TryDequeue(out _)) { }
    }

    internal static void Bind(ManualLogSource log) => _boundLog = log;

    internal static void Unbind(ManualLogSource log)
    {
        if (ReferenceEquals(_boundLog, log))
            _boundLog = null;
    }

    public static void Info(string msg) => Emit(msg, static (log, value) => log.LogInfo(value));

    public static void Warn(string msg) => Emit(msg, static (log, value) => log.LogWarning(value));

    public static void Error(string msg) => Emit(msg, static (log, value) => log.LogError(value));

    private static void Emit(
        string message,
        Action<BepInEx.Logging.ManualLogSource, string> write
    )
    {
        if (_boundLog != null)
        {
            write(_boundLog, message);
            return;
        }

        CapturedWhenUnbound.Enqueue(message);
    }
}
