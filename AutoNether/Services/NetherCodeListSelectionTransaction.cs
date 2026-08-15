#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

public enum NetherCodeListSelectionStepKind
{
    Invalid,
    RequestTabActivation,
    AwaitTabActivation,
    SelectThumbnail,
}

public readonly record struct NetherCodeListSelectionStep(
    NetherCodeListSelectionStepKind Kind,
    string Detail
);

public readonly record struct NetherCodeListThumbnailSelection(
    long CodeId,
    bool IsSelected
);

/// <summary>
/// Coordinates the player-visible tab transition before a code-list thumbnail is clicked.
/// </summary>
public sealed class NetherCodeListSelectionTransaction
{
    private long _popupSequence;
    private int _targetTabIndex = -1;
    private bool _activationRequested;

    public NetherCodeListSelectionStep Advance(
        long popupSequence,
        int targetTabIndex,
        int currentTabIndex
    )
    {
        if (popupSequence <= 0 || targetTabIndex < 0 || currentTabIndex < 0)
        {
            return new(
                NetherCodeListSelectionStepKind.Invalid,
                "invalid-code-list-tab-activation-coordinate"
            );
        }

        if (_popupSequence != popupSequence || _targetTabIndex != targetTabIndex)
        {
            _popupSequence = popupSequence;
            _targetTabIndex = targetTabIndex;
            _activationRequested = false;
        }

        if (currentTabIndex == targetTabIndex)
        {
            return new(
                NetherCodeListSelectionStepKind.SelectThumbnail,
                "code-list-target-tab-visible"
            );
        }

        if (!_activationRequested)
        {
            _activationRequested = true;
            return new(
                NetherCodeListSelectionStepKind.RequestTabActivation,
                "code-list-target-tab-activation-required"
            );
        }

        return new(
            NetherCodeListSelectionStepKind.AwaitTabActivation,
            "code-list-target-tab-activation-pending"
        );
    }

    public void Clear()
    {
        _popupSequence = 0;
        _targetTabIndex = -1;
        _activationRequested = false;
    }

    public static bool TryVerifySelection(
        int targetTabIndex,
        int currentTabIndex,
        long expectedCodeId,
        IReadOnlyList<NetherCodeListThumbnailSelection> models,
        out string error
    )
    {
        if (targetTabIndex < 0 || currentTabIndex < 0 || expectedCodeId <= 0 || models == null)
        {
            error = "invalid-code-list-selection-verification";
            return false;
        }
        if (currentTabIndex != targetTabIndex)
        {
            error = "code-list-visible-tab-mismatch:current_"
                + currentTabIndex
                + ":target_"
                + targetTabIndex;
            return false;
        }
        if (models.Any(model => model.CodeId <= 0))
        {
            error = "invalid-code-list-thumbnail-id";
            return false;
        }
        if (models.Select(model => model.CodeId).Distinct().Count() != models.Count)
        {
            error = "duplicate-code-list-thumbnail-id";
            return false;
        }

        NetherCodeListThumbnailSelection[] selected = models
            .Where(model => model.IsSelected)
            .ToArray();
        if (selected.Length != 1)
        {
            error = "code-list-selected-thumbnail-count:" + selected.Length;
            return false;
        }
        if (selected[0].CodeId != expectedCodeId)
        {
            error = "code-list-selected-thumbnail-mismatch:selected_"
                + selected[0].CodeId
                + ":expected_"
                + expectedCodeId;
            return false;
        }

        error = string.Empty;
        return true;
    }
}
