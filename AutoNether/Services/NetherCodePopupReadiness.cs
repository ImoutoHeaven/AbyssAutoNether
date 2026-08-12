#nullable enable

namespace AutoNether.Services;

internal readonly record struct NetherCodePopupReadinessResult(bool IsReady, string Detail);

internal static class NetherCodePopupReadiness
{
    public static NetherCodePopupReadinessResult Evaluate(
        bool offerIdsReadable,
        int offerIdCount,
        bool modelReadable,
        bool hasModel
    )
    {
        if (!offerIdsReadable)
        {
            return new NetherCodePopupReadinessResult(
                IsReady: false,
                Detail: "code-offer-ids-member-unavailable"
            );
        }
        if (offerIdCount <= 0)
        {
            return new NetherCodePopupReadinessResult(
                IsReady: false,
                Detail: "code-offer-ids-not-ready"
            );
        }
        if (!modelReadable)
        {
            return new NetherCodePopupReadinessResult(
                IsReady: false,
                Detail: "code-offer-model-member-unavailable"
            );
        }
        if (!hasModel)
        {
            return new NetherCodePopupReadinessResult(
                IsReady: false,
                Detail: "code-offer-model-not-ready"
            );
        }
        return new NetherCodePopupReadinessResult(IsReady: true, Detail: string.Empty);
    }
}
