#nullable enable

using System;

namespace AutoNether.Services;

/// <summary>
/// Exact generated callback descriptors for the one-ticket Continue branch. Both values of
/// Continue popup <c>_canBoost</c> enter the packaged <c>b__10_2</c> chain; an optional Skip popup
/// is declined before the true branch owns a fresh one-ticket Boost confirmation.
/// </summary>
internal static class NetherCheckpointContinueNativeBinding
{
    private const string UnitTypeName = "UniRx.Unit";
    private const string Il2CppActionTypeName = "Il2CppSystem.Action";
    private const string ContinueControllerTypeName =
        "Project.Nether.NetherContinueConfirmPopup.NetherContinueConfirmPopupController";
    private const string BoostControllerTypeName =
        "Project.Nether.NetherBoostConfirmPopup.NetherBoostConfirmPopupController";
    private const string BoostPopupTypeName =
        "Project.Nether.NetherBoostConfirmPopup.NetherBoostConfirmPopup";
    public const string SkipControllerTypeName =
        "Project.NetherSkipPopup.NetherSkipPopupController";
    public const string ReturnConfirmControllerTypeName = "Project.ConfirmButtonTwoPopupController";
    public const string ReturnConfirmPopupTypeName = "Project.ConfirmButtonTwoPopup";

    /// <summary>
    /// Closure that <c>OnTapReturn</c> stores as the confirmation's <c>_onClickOk</c>; its
    /// <c>__4__this</c> and <c>controller</c> prove which Continue popup owns the generic dialog.
    /// </summary>
    public const string ReturnConfirmClosureTypeName =
        ContinueControllerTypeName + "+__c__DisplayClass11_0";

    public static NetherNativeMethodDescriptor ContinueCallback { get; } = new(
        "<SetupPopupEvent>b__10_2",
        new[] { UnitTypeName, ContinueControllerTypeName },
        "System.Void"
    );

    public static NetherNativeMethodDescriptor BoostSetCount { get; } = new(
        "<SetupPopupEvent>b__7_2",
        new[] { "System.Int32", BoostControllerTypeName, BoostPopupTypeName },
        "System.Void"
    );

    public static NetherNativeMethodDescriptor BoostConfirm { get; } = new(
        "<SetupPopupEvent>b__7_1",
        new[] { UnitTypeName, BoostControllerTypeName, BoostPopupTypeName },
        "System.Void"
    );

    public static NetherCodePopupInteropMethodBinding ContinueCallbackInterop { get; } = new(
        "_SetupPopupEvent_b__10_2",
        "<SetupPopupEvent>b__10_2",
        new[] { UnitTypeName, ContinueControllerTypeName },
        "System.Void"
    ) { IsStatic = false };

    public static NetherCodePopupInteropMethodBinding FinishCallbackInterop { get; } = new(
        "_SetupPopupEvent_b__10_1",
        "<SetupPopupEvent>b__10_1",
        new[] { UnitTypeName, ContinueControllerTypeName },
        "System.Void"
    ) { IsStatic = false };

    /// <summary>OK button of the generic confirmation: <c>b__9_1(Unit, _onClickOk)</c> invokes the action.</summary>
    public static NetherCodePopupInteropMethodBinding ReturnConfirmOkCallbackInterop { get; } = new(
        "_SetupPopupEvent_b__9_1",
        "<SetupPopupEvent>b__9_1",
        new[] { UnitTypeName, Il2CppActionTypeName },
        "System.Void"
    ) { IsStatic = false };

    public static NetherCodePopupInteropMethodBinding SkipDeclineCallbackInterop { get; } = new(
        "_SetupPopupEvent_b__7_0",
        "<SetupPopupEvent>b__7_0",
        new[] { UnitTypeName, Il2CppActionTypeName },
        "System.Void"
    ) { IsStatic = false };

    public static NetherCodePopupInteropMethodBinding BoostSetCountInterop { get; } = new(
        "_SetupPopupEvent_b__7_2",
        "<SetupPopupEvent>b__7_2",
        new[] { "System.Int32", BoostControllerTypeName, BoostPopupTypeName },
        "System.Void"
    ) { IsStatic = false };

    public static NetherCodePopupInteropMethodBinding BoostConfirmInterop { get; } = new(
        "_SetupPopupEvent_b__7_1",
        "<SetupPopupEvent>b__7_1",
        new[] { UnitTypeName, BoostControllerTypeName, BoostPopupTypeName },
        "System.Void"
    ) { IsStatic = false };

    public const int ExactTicketCount = 1;

    public static bool SubmitContinue(NetherCheckpointNativeFlow flow, bool canBoost) =>
        flow != null && flow.SubmitContinue(canBoost);
}
