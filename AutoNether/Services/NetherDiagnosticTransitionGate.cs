#nullable enable

using System;
using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// Suppresses only an identical consecutive poll observation on the same diagnostic channel.
/// Stage changes and terminal/fault details remain visible, while a long native Pending state
/// cannot fill LogOutput.log with one line per Unity update.
/// </summary>
internal sealed class NetherDiagnosticTransitionGate
{
    private readonly Dictionary<string, string> _lastByChannel = new(StringComparer.Ordinal);

    public bool ShouldEmit(string channel, string signature)
    {
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("A diagnostic channel is required.", nameof(channel));
        signature ??= string.Empty;
        if (_lastByChannel.TryGetValue(channel, out string? previous)
            && string.Equals(previous, signature, StringComparison.Ordinal))
        {
            return false;
        }

        _lastByChannel[channel] = signature;
        return true;
    }

    public void Reset() => _lastByChannel.Clear();
}
