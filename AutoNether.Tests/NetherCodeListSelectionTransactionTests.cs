#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodeListSelectionTransactionTests
{
    [Fact]
    public void Mismatched_visible_tab_requests_activation_before_thumbnail_click()
    {
        var transaction = new NetherCodeListSelectionTransaction();

        NetherCodeListSelectionStep step = transaction.Advance(
            popupSequence: 12,
            targetTabIndex: 2,
            currentTabIndex: 0
        );

        Assert.Equal(NetherCodeListSelectionStepKind.RequestTabActivation, step.Kind);
    }

    [Fact]
    public void Repeated_mismatch_waits_instead_of_reissuing_or_clicking()
    {
        var transaction = new NetherCodeListSelectionTransaction();
        _ = transaction.Advance(12, targetTabIndex: 2, currentTabIndex: 0);

        NetherCodeListSelectionStep step = transaction.Advance(
            popupSequence: 12,
            targetTabIndex: 2,
            currentTabIndex: 0
        );

        Assert.Equal(NetherCodeListSelectionStepKind.AwaitTabActivation, step.Kind);
    }

    [Fact]
    public void Matching_visible_tab_allows_thumbnail_click()
    {
        var transaction = new NetherCodeListSelectionTransaction();

        NetherCodeListSelectionStep step = transaction.Advance(
            popupSequence: 12,
            targetTabIndex: 2,
            currentTabIndex: 2
        );

        Assert.Equal(NetherCodeListSelectionStepKind.SelectThumbnail, step.Kind);
    }

    [Fact]
    public void New_popup_sequence_gets_its_own_activation_request()
    {
        var transaction = new NetherCodeListSelectionTransaction();
        _ = transaction.Advance(12, targetTabIndex: 2, currentTabIndex: 0);

        NetherCodeListSelectionStep step = transaction.Advance(
            popupSequence: 13,
            targetTabIndex: 2,
            currentTabIndex: 0
        );

        Assert.Equal(NetherCodeListSelectionStepKind.RequestTabActivation, step.Kind);
    }

    [Fact]
    public void Exact_single_expected_selection_is_confirmable()
    {
        bool verified = NetherCodeListSelectionTransaction.TryVerifySelection(
            targetTabIndex: 2,
            currentTabIndex: 2,
            expectedCodeId: 30001,
            new[]
            {
                new NetherCodeListThumbnailSelection(30001, IsSelected: true),
                new NetherCodeListThumbnailSelection(30002, IsSelected: false),
            },
            out string error
        );

        Assert.True(verified, error);
    }

    [Fact]
    public void Stale_visible_tab_fails_closed_before_native_confirm()
    {
        bool verified = NetherCodeListSelectionTransaction.TryVerifySelection(
            targetTabIndex: 2,
            currentTabIndex: 0,
            expectedCodeId: 30001,
            new[] { new NetherCodeListThumbnailSelection(30001, IsSelected: true) },
            out string error
        );

        Assert.False(verified);
        Assert.Equal("code-list-visible-tab-mismatch:current_0:target_2", error);
    }

    [Fact]
    public void Empty_or_wrong_selection_fails_closed_before_native_first_safe()
    {
        Assert.False(NetherCodeListSelectionTransaction.TryVerifySelection(
            targetTabIndex: 2,
            currentTabIndex: 2,
            expectedCodeId: 30001,
            new[] { new NetherCodeListThumbnailSelection(30001, IsSelected: false) },
            out string emptyError
        ));
        Assert.Equal("code-list-selected-thumbnail-count:0", emptyError);

        Assert.False(NetherCodeListSelectionTransaction.TryVerifySelection(
            targetTabIndex: 2,
            currentTabIndex: 2,
            expectedCodeId: 30001,
            new[] { new NetherCodeListThumbnailSelection(30002, IsSelected: true) },
            out string wrongError
        ));
        Assert.Equal("code-list-selected-thumbnail-mismatch:selected_30002:expected_30001", wrongError);
    }

    [Fact]
    public void Multiple_selected_thumbnails_fail_closed()
    {
        bool verified = NetherCodeListSelectionTransaction.TryVerifySelection(
            targetTabIndex: 2,
            currentTabIndex: 2,
            expectedCodeId: 30001,
            new[]
            {
                new NetherCodeListThumbnailSelection(30001, IsSelected: true),
                new NetherCodeListThumbnailSelection(30002, IsSelected: true),
            },
            out string error
        );

        Assert.False(verified);
        Assert.Equal("code-list-selected-thumbnail-count:2", error);
    }
}
