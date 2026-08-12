#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodePopupReadinessTests
{
    [Fact]
    public void Registered_popup_with_offer_ids_but_without_initialized_model_is_not_ready()
    {
        NetherCodePopupReadinessResult result = NetherCodePopupReadiness.Evaluate(
            offerIdsReadable: true,
            offerIdCount: 3,
            modelReadable: true,
            hasModel: false
        );

        Assert.False(result.IsReady);
        Assert.Equal("code-offer-model-not-ready", result.Detail);
    }

    [Fact]
    public void Initialized_model_and_nonempty_offer_ids_are_ready()
    {
        NetherCodePopupReadinessResult result = NetherCodePopupReadiness.Evaluate(
            offerIdsReadable: true,
            offerIdCount: 3,
            modelReadable: true,
            hasModel: true
        );

        Assert.True(result.IsReady);
        Assert.Equal(string.Empty, result.Detail);
    }

    [Theory]
    [InlineData(false, 0, "code-offer-ids-member-unavailable")]
    [InlineData(true, 0, "code-offer-ids-not-ready")]
    public void Missing_or_empty_offer_ids_are_not_ready(
        bool offerIdsReadable,
        int offerIdCount,
        string expectedDetail
    )
    {
        NetherCodePopupReadinessResult result = NetherCodePopupReadiness.Evaluate(
            offerIdsReadable,
            offerIdCount,
            modelReadable: true,
            hasModel: true
        );

        Assert.False(result.IsReady);
        Assert.Equal(expectedDetail, result.Detail);
    }

    [Fact]
    public void Missing_model_member_is_a_binding_failure_not_a_ready_popup()
    {
        NetherCodePopupReadinessResult result = NetherCodePopupReadiness.Evaluate(
            offerIdsReadable: true,
            offerIdCount: 3,
            modelReadable: false,
            hasModel: false
        );

        Assert.False(result.IsReady);
        Assert.Equal("code-offer-model-member-unavailable", result.Detail);
    }
}
