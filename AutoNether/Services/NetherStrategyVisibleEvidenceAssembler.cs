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
        if (!TryResolveTypedSemanticProvider(
                request.Interactive,
                request.CapturedMasters.TypedSemanticProvider,
                out NetherStrategyTypedSemanticProviderEvidence? typedSemanticProvider,
                out string providerError
            ))
        {
            return NetherStrategyVisibleEvidenceCaptureResult.Failure(providerError);
        }

        var extendIdByNodeId = request.Interactive.ByFloorNodeId
            .Where(entry => entry.Value?.Input != null)
            .ToDictionary(entry => entry.Key, entry => entry.Value.Input!.FloorExtendId);
        var shopByNodeId = request.CapturedMasters.ShopInventoryByNodeId == null
            ? new Dictionary<long, NetherStrategyShopInventoryCapture>()
            : request.CapturedMasters.ShopInventoryByNodeId.ToDictionary(
                entry => entry.Key,
                entry => entry.Value
            );
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
            TypedSemanticProvider = typedSemanticProvider,
            ExtendIdByNodeId = extendIdByNodeId,
            ShopInventoryByNodeId = shopByNodeId,
        };
        return NetherStrategyVisibleEvidenceMapper.Map(mapped);
    }

    private static bool TryResolveTypedSemanticProvider(
        NetherRuntimeInteractivePreEntryInputsResult interactive,
        NetherStrategyTypedSemanticProviderEvidence? capturedProvider,
        out NetherStrategyTypedSemanticProviderEvidence? provider,
        out string error
    )
    {
        provider = capturedProvider;
        if (interactive.TypedSemanticProvider != null)
        {
            if (provider == null)
            {
                provider = interactive.TypedSemanticProvider;
            }
            else if (!Equivalent(provider, interactive.TypedSemanticProvider))
            {
                provider = null;
                error = "ambiguous-runtime-semantic-provider-evidence";
                return false;
            }
        }
        foreach (NetherRuntimeInteractivePreEntryCaptureResult entry in
            interactive.ByFloorNodeId?.Values ?? Array.Empty<NetherRuntimeInteractivePreEntryCaptureResult>())
        {
            NetherStrategyTypedSemanticProviderEvidence? candidate = entry?.Input?.TypedSemanticProvider;
            if (candidate == null)
                continue;
            if (provider == null)
            {
                provider = candidate;
                continue;
            }
            if (!Equivalent(provider, candidate))
            {
                provider = null;
                error = "ambiguous-runtime-semantic-provider-evidence";
                return false;
            }
        }
        error = string.Empty;
        return true;
    }

    private static bool Equivalent(
        NetherStrategyTypedSemanticProviderEvidence left,
        NetherStrategyTypedSemanticProviderEvidence right
    ) =>
        (left.CanonicalRewardTiers ?? Array.Empty<NetherCanonicalRewardTierProviderEvidence>())
            .SequenceEqual(right.CanonicalRewardTiers ?? Array.Empty<NetherCanonicalRewardTierProviderEvidence>())
        && (left.EventBattleTiers ?? Array.Empty<NetherEventBattleTierProviderEvidence>())
            .SequenceEqual(right.EventBattleTiers ?? Array.Empty<NetherEventBattleTierProviderEvidence>())
        && (left.EventBattleRouteSafety ?? Array.Empty<NetherEventBattleRouteSafetyProviderEvidence>())
            .SequenceEqual(right.EventBattleRouteSafety ?? Array.Empty<NetherEventBattleRouteSafetyProviderEvidence>())
        && (left.ShopKeyIdentities ?? Array.Empty<NetherShopKeyProviderEvidence>())
            .SequenceEqual(right.ShopKeyIdentities ?? Array.Empty<NetherShopKeyProviderEvidence>());
}
