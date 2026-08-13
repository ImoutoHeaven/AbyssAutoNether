#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCheckpointReturnScrollResolverTests
{
    [Fact]
    public void Populated_nested_scroll_is_ready_without_a_separate_initialize_hook()
    {
        var scroll = new FakeScroll(new object[] { new(), new() }, maxSelectedCount: 1);
        var popup = new FakePopup(scroll);

        NetherCheckpointReturnScrollResolution result =
            NetherCheckpointReturnScrollResolver.Resolve(popup, typeof(FakeScroll).FullName!);

        Assert.True(result.IsReady, result.Detail);
        Assert.Same(scroll, result.Controller);
        Assert.Equal(2, result.ContentCount);
        Assert.Equal(1, result.SelectionLimit);
        Assert.Equal("nested-return-scroll-ready", result.Detail);
    }

    [Fact]
    public void Empty_or_not_yet_populated_nested_scroll_remains_waiting()
    {
        var popup = new FakePopup(new FakeScroll(Array.Empty<object>(), maxSelectedCount: 1));

        NetherCheckpointReturnScrollResolution result =
            NetherCheckpointReturnScrollResolver.Resolve(popup, typeof(FakeScroll).FullName!);

        Assert.False(result.IsReady);
        Assert.Equal("return-scroll-content-not-ready", result.Detail);
    }

    [Fact]
    public void Wrong_nested_controller_type_is_rejected()
    {
        var popup = new WrongPopup(new object());

        NetherCheckpointReturnScrollResolution result =
            NetherCheckpointReturnScrollResolver.Resolve(popup, typeof(FakeScroll).FullName!);

        Assert.False(result.IsReady);
        Assert.StartsWith("return-scroll-type-mismatch:", result.Detail);
    }

    private sealed class FakePopup
    {
        public FakePopup(FakeScroll scroll) => ReturnableItemScrollViewController = scroll;

        public FakeScroll ReturnableItemScrollViewController { get; }
    }

    private sealed class WrongPopup
    {
        public WrongPopup(object scroll) => ReturnableItemScrollViewController = scroll;

        public object ReturnableItemScrollViewController { get; }
    }

    private sealed class FakeScroll
    {
#pragma warning disable CS0414
        private readonly IReadOnlyList<object> _contentModelList;
        private readonly int _maxSelectedCount;
#pragma warning restore CS0414

        public FakeScroll(IReadOnlyList<object> contentModelList, int maxSelectedCount)
        {
            _contentModelList = contentModelList;
            _maxSelectedCount = maxSelectedCount;
        }
    }
}
