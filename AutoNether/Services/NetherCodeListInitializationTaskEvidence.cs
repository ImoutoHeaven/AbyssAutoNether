#nullable enable

using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// Correlates an Abyss code-list popup registration with the exact UniTask returned by that
/// controller's InitializePopupAsync call. SetupPopupEvent can run before the async initializer
/// has populated its private model dictionary, so registration alone is not readiness evidence.
/// </summary>
internal sealed class NetherCodeListInitializationTaskEvidence
{
    private const int MaximumEntries = 8;
    private readonly List<Entry> _entries = new();

    public bool ObserveTask(object? controller, object? popup, object? task)
    {
        if (controller == null || popup == null || task == null)
            return false;

        Entry? entry = Find(controller, popup);
        if (entry == null)
        {
            entry = new Entry(controller, popup);
            Add(entry);
        }
        entry.Task = task;
        return entry.HasRegistration;
    }

    public bool ObserveRegistration(
        object? controller,
        object? popup,
        NetherActionKind ownerAction,
        long ownerGeneration,
        long sequence
    )
    {
        if (controller == null
            || popup == null
            || ownerAction == NetherActionKind.None
            || ownerGeneration < 1
            || sequence < 1)
        {
            return false;
        }

        Entry? entry = Find(controller, popup);
        if (entry == null)
        {
            entry = new Entry(controller, popup);
            Add(entry);
        }
        else if (entry.HasRegistration
            && (entry.OwnerAction != ownerAction
                || entry.OwnerGeneration != ownerGeneration
                || entry.Sequence != sequence))
        {
            // A reused native popup/controller pair is a new initialization boundary. Never
            // let the completed task from its previous ownership generation unlock the new one.
            entry.Task = null;
        }

        entry.OwnerAction = ownerAction;
        entry.OwnerGeneration = ownerGeneration;
        entry.Sequence = sequence;
        entry.HasRegistration = true;
        return entry.Task != null;
    }

    public bool TryGetTask(
        object? controller,
        object? popup,
        NetherActionKind ownerAction,
        long ownerGeneration,
        long sequence,
        out object? task
    )
    {
        Entry? entry = controller == null || popup == null ? null : Find(controller, popup);
        if (entry == null
            || !entry.HasRegistration
            || entry.Task == null
            || entry.OwnerAction != ownerAction
            || entry.OwnerGeneration != ownerGeneration
            || entry.Sequence != sequence)
        {
            task = null;
            return false;
        }

        task = entry.Task;
        return true;
    }

    public bool InvalidatePopup(object? popup)
    {
        if (popup == null)
            return false;

        int removed = _entries.RemoveAll(entry => ReferenceEquals(entry.Popup, popup));
        return removed > 0;
    }

    public void Reset() => _entries.Clear();

    private Entry? Find(object controller, object popup) =>
        _entries.Find(entry =>
            ReferenceEquals(entry.Controller, controller) && ReferenceEquals(entry.Popup, popup));

    private void Add(Entry entry)
    {
        _entries.Add(entry);
        if (_entries.Count > MaximumEntries)
            _entries.RemoveAt(0);
    }

    private sealed class Entry
    {
        public Entry(object controller, object popup)
        {
            Controller = controller;
            Popup = popup;
        }

        public object Controller { get; }
        public object Popup { get; }
        public object? Task { get; set; }
        public NetherActionKind OwnerAction { get; set; }
        public long OwnerGeneration { get; set; }
        public long Sequence { get; set; }
        public bool HasRegistration { get; set; }
    }
}
