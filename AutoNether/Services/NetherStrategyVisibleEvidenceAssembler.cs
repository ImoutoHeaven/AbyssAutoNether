#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Inputs already copied by compile-time native adapters.  The module binds those rows to the
/// current interactive capture and popup without exposing either lifecycle join to RuntimeBridge.
/// </summary>
internal sealed record NetherStrategyVisibleEvidenceAssemblyRequest(
    NetherSnapshot Snapshot,
    NetherRuntimeInteractivePreEntryInputsResult Interactive,
    NetherRuntimePopupResult ActivePopup,
    NetherStrategyVisibleEvidenceCaptureRequest CapturedMasters
);

internal static class NetherStrategyVisibleEvidenceAssembler
{
    public static NetherStrategyVisibleEvidenceCaptureResult Assemble(
        NetherStrategyVisibleEvidenceAssemblyRequest? request
    )
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (request.Snapshot == null
            || request.Interactive == null
            || request.CapturedMasters == null)
        {
            return NetherStrategyVisibleEvidenceCaptureResult.Failure(
                "invalid-visible-evidence-assembly-contract"
            );
        }
        if (!request.Interactive.IsSuccess)
        {
            return NetherStrategyVisibleEvidenceCaptureResult.Failure(
                "strategy-visible-interactive:" + request.Interactive.Detail
            );
        }

        var extendIdByNodeId = request.Interactive.ByFloorNodeId
            .Where(entry => entry.Value?.Input != null)
            .ToDictionary(entry => entry.Key, entry => entry.Value.Input!.FloorExtendId);
        var shopByNodeId = new Dictionary<long, NetherStrategyShopInventoryCapture>();
        NetherFloorNode? currentShop = request.Snapshot.Floors.FirstOrDefault(floor =>
            floor != null
            && floor.NodeId == request.Snapshot.CurrentNodeId
            && floor.NodeType == NetherFloorNodeType.Shop);
        if (currentShop != null)
        {
            if (request.ActivePopup.IsSuccess
                && request.ActivePopup.Popup is NetherRuntimePopupContext shopPopup
                && shopPopup.Kind == NetherRuntimePopupKind.Shop)
            {
                shopByNodeId[currentShop.NodeId] = new NetherStrategyShopInventoryCapture(
                    true,
                    shopPopup.ShopContents,
                    string.Empty
                );
            }
            else
            {
                shopByNodeId[currentShop.NodeId] = new NetherStrategyShopInventoryCapture(
                    false,
                    Array.Empty<NetherShopContent>(),
                    "shop-inventory-active-popup-unavailable:" + request.ActivePopup.Detail
                );
            }
        }

        NetherStrategyVisibleEvidenceCaptureRequest mapped = request.CapturedMasters with
        {
            ExtendIdByNodeId = extendIdByNodeId,
            ShopInventoryByNodeId = shopByNodeId,
        };
        return NetherStrategyVisibleEvidenceMapper.Map(mapped);
    }
}
