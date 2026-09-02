#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoNether.Patches;
using HarmonyLib;

namespace AutoNether.Services;

internal sealed record NetherNativeCompatibilityReport(
    int CheckedMethodCount,
    int CheckedGeneratedMethodCount,
    IReadOnlyList<string> Failures
)
{
    public bool IsCompatible => Failures.Count == 0;
}

internal sealed record NetherGeneratedInteropContract(
    string TypeName,
    NetherCodePopupInteropMethodBinding Method
);

/// <summary>
/// The single inventory of game methods invoked or patched by AutoNether.  Runtime callers use
/// these same descriptors, so startup validation and later invocation cannot silently diverge.
/// </summary>
internal static class NetherNativeBindingCatalog
{
    private const string UniTask = "Cysharp.Threading.Tasks.UniTask";
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public const string FloorControllerType = "Project.Nether.FloorSelection.SubViewController";
    public const string EventControllerType =
        "Project.Nether.NetherEventPopup.NetherEventPopupController";
    public const string EventPopupType = "Project.Nether.NetherEventPopup.NetherEventPopup";
    public const string RecoveryControllerType =
        "Project.Nether.NetherRecoverPopup.NetherRecoverPopupController";
    public const string RecoveryPopupType = "Project.Nether.NetherRecoverPopup.NetherRecoverPopup";
    public const string TreasureControllerType =
        "Project.Nether.NetherTreasurePopup.NetherTreasurePopupController";
    public const string TreasurePopupType =
        "Project.Nether.NetherTreasurePopup.NetherTreasurePopup";
    public const string ShopControllerType =
        "Project.Nether.NetherShopPopup.NetherShopPopupController";
    public const string ShopPopupType = "Project.Nether.NetherShopPopup.NetherShopPopup";
    public const string CodeSelectControllerType =
        "Project.Nether.AbyssCodeSelectPopup.AbyssCodeSelectPopupController";
    public const string CodeSelectPopupType =
        "Project.Nether.AbyssCodeSelectPopup.AbyssCodeSelectPopup";
    public const string CodeListControllerType =
        "Project.Nether.NetherAbyssCodeListPopup.AbyssCodeListPopupController";
    public const string ReturnControllerType =
        "Project.Nether.NetherReturnItemSelectionPopup.NetherReturnItemSelectionPopupController";
    public const string ReturnPopupType =
        "Project.Nether.NetherReturnItemSelectionPopup.NetherReturnItemSelectionPopup";
    public const string ReturnScrollControllerType =
        "Project.Nether.NetherReturnItemSelectionPopup.NetherReturnableItemScrollViewController";
    public const string NetherUtilityType = "Project.Nether.NetherUtility";

    public static NetherInteropPatchBinding BattleStartTask { get; } = Instance(
        "Project.Ingame.Exploration.ExplorationQuestPreserveAPIService",
        "Project_Ingame_Exploration_IExplorationQuestAPIService_StartQuestAsync",
        new[] { "Il2CppSystem.Threading.CancellationToken" },
        "Cysharp.Threading.Tasks.UniTask<Project.Api.BattleSessionStatusResponseEntity>"
    );

    public static NetherInteropPatchBinding BattleTerminal { get; } = Static(
        "Project.BattleResult.BattleResultUtility",
        "CreateBattleResultModel",
        new[]
        {
            "Project.Ingame.BattleResultType",
            "Absf.ISceneTransitionParam",
            "Project.BattleResult.Top.BattleClearRecordBase",
            "Project.Api.IFinishQuestResponseEntity",
            "Il2CppSystem.Threading.CancellationToken",
        },
        "Project.BattleResult.IBattleResultModel"
    );

    public static NetherInteropPatchBinding ResultTask { get; } = Instance(
        "Project.NetherTop.Result.SubViewController",
        "CreateNetherResultModelAsync",
        new[]
        {
            "System.Boolean",
            "Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Project.NetherTop.Result.NetherResultPartyCharacterModel>",
        },
        UniTask
    );

    public static NetherInteropPatchBinding BattleResultViewInitialization { get; } = Instance(
        NetherBattleResultNextNativeBinding.ControllerTypeName,
        NetherBattleResultNextNativeBinding.InitializeViewDescriptor
    );

    public static NetherInteropPatchBinding FloorEventSequence { get; } = Instance(
        NetherFloorEventSequenceNativeBinding.ControllerTypeName,
        NetherFloorEventSequenceNativeBinding.SequenceDescriptor
    );

    public static NetherInteropPatchBinding StartRun { get; } = Static(
        NetherUtilityType,
        "TransitionNetherFloorSelectionSceneFromPartyAsync",
        new[]
        {
            "System.Int32",
            "System.Int32",
            "System.Int32",
            "Il2CppSystem.Threading.CancellationToken",
        },
        UniTask
    );

    public static NetherInteropPatchBinding ReadOnlySync { get; } = Instance(
        NetherReadOnlyReconcileNativeBinding.DataStoreTypeName,
        NetherReadOnlyReconcileNativeBinding.SyncDescriptor
    );

    public static NetherInteropPatchBinding FloorClick { get; } = Instance(
        FloorControllerType,
        "OnFloorClickedEventAsync",
        new[] { "System.Int32", "System.Int32" },
        UniTask
    );

    public static NetherInteropPatchBinding EventSelect { get; } = Instance(
        EventControllerType,
        "OnPanelSelected",
        new[] { EventPopupType, "System.Int32" },
        "System.Void"
    );
    public static NetherInteropPatchBinding EventExecute { get; } = Instance(
        EventControllerType,
        "ExecuteEvent",
        new[] { EventPopupType },
        "System.Void"
    );
    public static NetherInteropPatchBinding RecoverySelect { get; } = Instance(
        RecoveryControllerType,
        "OnPanelSelected",
        new[] { RecoveryPopupType, "System.Int32" },
        "System.Void"
    );
    public static NetherInteropPatchBinding RecoveryExecute { get; } = Instance(
        RecoveryControllerType,
        "ExecuteEvent",
        new[] { RecoveryPopupType },
        "System.Void"
    );
    public static NetherInteropPatchBinding TreasureSelect { get; } = Instance(
        TreasureControllerType,
        "OnPanelSelected",
        new[] { TreasurePopupType, "System.Int32" },
        "System.Void"
    );
    public static NetherInteropPatchBinding TreasureConfirm { get; } = Instance(
        TreasureControllerType,
        "OnConfirm",
        new[] { TreasurePopupType },
        "System.Void"
    );

    public static NetherInteropPatchBinding ShopPurchase { get; } = Instance(
        ShopControllerType,
        "OnPurchaseContentAsync",
        new[] { ShopPopupType, "System.Int32" },
        UniTask
    );

    public static NetherInteropPatchBinding ReturnSelect { get; } = Instance(
        ReturnScrollControllerType,
        "OnThumbnailClicked",
        new[] { "System.Int32" },
        "System.Void"
    );
    public static NetherInteropPatchBinding ReturnConfirm { get; } = Instance(
        ReturnControllerType,
        "OnConfirmAsync",
        new[] { ReturnPopupType },
        UniTask
    );

    public static NetherInteropPatchBinding TreasureSkip { get; } = Instance(
        TreasurePopupType,
        "SkipOpenTreasureAnimationAsync",
        new[] { "Il2CppSystem.Threading.CancellationToken" },
        UniTask
    );
    public static NetherInteropPatchBinding AppButtonSubmit { get; } = Instance(
        "Project.AppButton",
        "OnSubmit",
        new[] { "UnityEngine.EventSystems.BaseEventData" },
        "System.Void"
    );
    public static NetherInteropPatchBinding TabGroupUpdate { get; } = Instance(
        "Project.Outgame.UIParts.TabGroupView",
        "UpdateTabState",
        new[] { "System.Int32" },
        "System.Void"
    );

    public static NetherInteropPatchBinding BattleSettingsGetAuto { get; } = Instance(
        "Project.Ingame.IIngameUserSettings",
        "get_IsAuto",
        Array.Empty<string>(),
        "System.Boolean"
    );
    public static NetherInteropPatchBinding BattleSettingsSetAuto { get; } = Instance(
        "Project.Ingame.IIngameUserSettings",
        "set_IsAuto",
        new[] { "System.Boolean" },
        "System.Void"
    );
    public static NetherInteropPatchBinding BattleSettingsGetSpeed { get; } = Instance(
        "Project.Ingame.IIngameUserSettings",
        "get_Speed",
        Array.Empty<string>(),
        "Project.GameSpeedType"
    );
    public static NetherInteropPatchBinding BattleSettingsSetSpeed { get; } = Instance(
        "Project.Ingame.IIngameUserSettings",
        "set_Speed",
        new[] { "Project.GameSpeedType" },
        "System.Void"
    );

    public static NetherInteropPatchBinding CodeListThumbnail { get; } = Instance(
        CodeListControllerType,
        "OnClickThumbnail",
        new[] { "System.Int32" },
        "System.Void"
    );
    public static NetherInteropPatchBinding CodeListReplace { get; } = Instance(
        CodeListControllerType,
        "OnClickReplace",
        Array.Empty<string>(),
        "System.Void"
    );
    public static NetherInteropPatchBinding CodeListChangeTab { get; } = Instance(
        CodeListControllerType,
        "OnChangeTab",
        new[] { "System.Int32" },
        "System.Void"
    );
    public static NetherInteropPatchBinding CodeListChange { get; } = Instance(
        CodeListControllerType,
        "OnClickChange",
        Array.Empty<string>(),
        "System.Void"
    );
    public static NetherInteropPatchBinding CodeReroll { get; } = Instance(
        CodeSelectControllerType,
        "RerollAsync",
        new[] { CodeSelectPopupType },
        UniTask
    );

    // Standard IL2CPP delegate contracts are not game-version bindings, but keeping their
    // descriptors here prevents the runtime bridge from creating uncataloged reflection calls.
    public static NetherNativeMethodDescriptor NoArgumentDelegateInvoke { get; } = new(
        "Invoke",
        Array.Empty<string>(),
        "System.Void"
    ) { IsStatic = false };
    public static NetherNativeMethodDescriptor BooleanDelegateInvoke { get; } = new(
        "Invoke",
        new[] { "System.Boolean" },
        "System.Void"
    ) { IsStatic = false };

    public static IReadOnlyList<NetherInteropPatchBinding> RequiredMethods { get; } =
        NetherLifecycleInteropBindings.All
            .Concat(new[]
            {
                NetherLifecycleInteropBindings.StartStatusTask,
                NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext,
                NetherLifecycleInteropBindings.CodeListInitializationTask,
                BattleStartTask,
                BattleTerminal,
                ResultTask,
                BattleResultViewInitialization,
                FloorEventSequence,
                StartRun,
                ReadOnlySync,
                FloorClick,
                EventSelect,
                EventExecute,
                RecoverySelect,
                RecoveryExecute,
                TreasureSelect,
                TreasureConfirm,
                ShopPurchase,
                ReturnSelect,
                ReturnConfirm,
                TreasureSkip,
                AppButtonSubmit,
                TabGroupUpdate,
                BattleSettingsGetAuto,
                BattleSettingsSetAuto,
                BattleSettingsGetSpeed,
                BattleSettingsSetSpeed,
                CodeListThumbnail,
                CodeListReplace,
                CodeListChangeTab,
                CodeListChange,
                CodeReroll,
            })
            .GroupBy(BindingIdentity, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();

    public static IReadOnlyList<NetherGeneratedInteropContract> RequiredGeneratedMethods { get; } =
        new[]
        {
            Generated(ShopControllerType, NetherLifecycleInteropBindings.ShopCloseCallback),
            Generated(
                "Project.Nether.NetherShopConfirmPopup.NetherShopConfirmPopupController",
                NetherLifecycleInteropBindings.ShopPurchaseConfirmCallback
            ),
            Generated(
                "Project.Nether.NetherFloorEventHintBox.NetherFloorEventHintBoxPopupController",
                NetherLifecycleInteropBindings.FloorEventHintDismissCallback
            ),
            Generated(
                "Project.Nether.ErosionPointNotificationPopupController",
                NetherLifecycleInteropBindings.ErosionPointNotificationConfirmCallback
            ),
            Generated(
                NetherBattleResultNextNativeBinding.ControllerTypeName,
                NetherBattleResultNextNativeBinding.NextCallbackInterop
            ),
            Generated(
                CodeSelectControllerType,
                NetherCodePopupNativeBinding.ConfirmCallbackBinding(CodeSelectControllerType)
            ),
            Generated(
                CodeSelectControllerType,
                NetherCodePopupNativeBinding.CancelCallbackBinding(CodeSelectControllerType)
            ),
            Generated(
                CodeSelectControllerType,
                NetherCodePopupNativeBinding.DetailCallbackBinding(CodeSelectControllerType)
            ),
            Generated(
                NetherUtilityType,
                NetherCodePopupNativeBinding.ConfirmTaskBinding(CodeSelectControllerType)
            ),
            Generated(
                NetherUtilityType,
                NetherCodePopupNativeBinding.CancelTaskBinding(CodeSelectControllerType)
            ),
            Generated(
                "Project.Nether.AbyssCodeChangePopup.AbyssCodeChangePopupController",
                NetherCodeTransformNativeBinding.ConfirmCallbackBinding
            ),
            Generated(
                "Project.Nether.AbyssCodeChangeCompletePopup.AbyssCodeChangeCompletePopupController",
                NetherCodeTransformNativeBinding.CompleteCloseCallbackBinding
            ),
            Generated(
                NetherUtilityType,
                NetherCodeTransformNativeBinding.TransformTaskBinding(CodeListControllerType)
            ),
            Generated(
                "Project.Nether.NetherContinueConfirmPopup.NetherContinueConfirmPopupController",
                NetherCheckpointContinueNativeBinding.ContinueCallbackInterop
            ),
            Generated(
                "Project.Nether.NetherContinueConfirmPopup.NetherContinueConfirmPopupController",
                NetherCheckpointContinueNativeBinding.FinishCallbackInterop
            ),
            Generated(
                "Project.Nether.NetherBoostConfirmPopup.NetherBoostConfirmPopupController",
                NetherCheckpointContinueNativeBinding.BoostSetCountInterop
            ),
            Generated(
                "Project.Nether.NetherBoostConfirmPopup.NetherBoostConfirmPopupController",
                NetherCheckpointContinueNativeBinding.BoostConfirmInterop
            ),
        };

    public static MethodInfo ResolveRequiredMethod(NetherInteropPatchBinding binding)
    {
        if (NetherLifecycleInteropBindings.TryResolve(
                AppDomain.CurrentDomain.GetAssemblies(),
                binding,
                out string error,
                out MethodInfo? method
            ))
        {
            return method!;
        }

        throw new MissingMethodException(error);
    }

    private static NetherGeneratedInteropContract Generated(
        string typeName,
        NetherCodePopupInteropMethodBinding method
    ) => new(typeName, method);

    private static NetherInteropPatchBinding Instance(
        string typeName,
        string methodName,
        IReadOnlyList<string> parameterTypeNames,
        string returnTypeName
    ) => Instance(
        typeName,
        new NetherNativeMethodDescriptor(methodName, parameterTypeNames, returnTypeName)
        {
            IsStatic = false,
        }
    );

    private static NetherInteropPatchBinding Instance(
        string typeName,
        NetherNativeMethodDescriptor method
    ) => new(typeName, method with { IsStatic = false }, InstanceFlags);

    private static NetherInteropPatchBinding Static(
        string typeName,
        string methodName,
        IReadOnlyList<string> parameterTypeNames,
        string returnTypeName
    ) => new(
        typeName,
        new NetherNativeMethodDescriptor(methodName, parameterTypeNames, returnTypeName)
        {
            IsStatic = true,
        },
        StaticFlags
    );

    private static string BindingIdentity(NetherInteropPatchBinding binding) =>
        binding.TypeName
        + "|"
        + binding.Method.Name
        + "|"
        + string.Join(",", binding.Method.ParameterTypeNames)
        + "|"
        + binding.Method.ReturnTypeName
        + "|"
        + (binding.Method.IsStatic ?? (binding.Flags & BindingFlags.Static) != 0);
}

/// <summary>Fail-closed compatibility gate executed before any AutoNether initialization.</summary>
internal static class NetherNativeCompatibilityPreflight
{
    public static NetherNativeCompatibilityReport Validate(IEnumerable<Assembly> assemblies)
    {
        Assembly[] loaded = assemblies?.Where(assembly => assembly != null).Distinct().ToArray()
            ?? Array.Empty<Assembly>();
        var failures = new List<string>();

        foreach (NetherInteropPatchBinding binding in NetherNativeBindingCatalog.RequiredMethods)
        {
            try
            {
                if (!NetherLifecycleInteropBindings.TryResolve(
                        loaded,
                        binding,
                        out string error,
                        out _
                    ))
                {
                    failures.Add(binding.TypeName + "." + binding.Method.Name + " => " + error);
                }
            }
            catch (Exception ex)
            {
                failures.Add(
                    binding.TypeName
                    + "."
                    + binding.Method.Name
                    + " => precheck-exception:"
                    + ex.GetType().Name
                    + ":"
                    + ex.Message
                );
            }
        }

        foreach (NetherGeneratedInteropContract contract in NetherNativeBindingCatalog.RequiredGeneratedMethods)
        {
            try
            {
                Type? type = NetherLifecycleInteropBindings.ResolveType(loaded, contract.TypeName);
                if (type == null)
                {
                    failures.Add(
                        contract.TypeName + "." + contract.Method.ManagedName + " => missing-type"
                    );
                    continue;
                }

                bool resolved;
                string error;
                if (contract.Method.IsStatic)
                {
                    resolved = NetherCodePopupInteropResolver.TryResolveStaticMethodExactIdentity(
                        type,
                        contract.Method,
                        out error,
                        out _
                    );
                }
                else
                {
                    resolved = NetherCodePopupInteropResolver.TryResolveGeneratedCallbackTarget(
                        type,
                        contract.Method,
                        out error,
                        out _,
                        out _
                    );
                }
                if (!resolved)
                    failures.Add(contract.TypeName + "." + contract.Method.ManagedName + " => " + error);
            }
            catch (Exception ex)
            {
                failures.Add(
                    contract.TypeName
                    + "."
                    + contract.Method.ManagedName
                    + " => precheck-exception:"
                    + ex.GetType().Name
                    + ":"
                    + ex.Message
                );
            }
        }

        ValidatePatchCatalog(failures);
        return new NetherNativeCompatibilityReport(
            NetherNativeBindingCatalog.RequiredMethods.Count,
            NetherNativeBindingCatalog.RequiredGeneratedMethods.Count,
            failures
        );
    }

    private static void ValidatePatchCatalog(List<string> failures)
    {
        Type[] catalog = PatchManager.PatchTypes.ToArray();
        foreach (Type duplicate in catalog.GroupBy(type => type).Where(group => group.Count() != 1).Select(group => group.Key))
            failures.Add("duplicate-patch-type:" + duplicate.FullName);
        foreach (Type patchType in catalog.Where(type =>
                     type.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length == 0))
        {
            failures.Add("invalid-patch-type:" + patchType.FullName);
        }
    }
}
