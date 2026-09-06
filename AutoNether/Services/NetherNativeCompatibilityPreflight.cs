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
    int CheckedMemberCount,
    int CheckedCompiledReferenceCount,
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
/// Versioned method and member contracts required by AutoNether's native adapters.
/// </summary>
internal static class NetherNativeBindingCatalog
{
    private const string UniTask = "Cysharp.Threading.Tasks.UniTask";
    private const string NetherModel = "Project.Nether.FloorSelection.NetherModel";
    private const string FloorModel = "Project.Nether.FloorSelection.NetherFloorModel";
    private const string PartyModel = "Project.Nether.NetherPartyModel";
    private const string PartyCharacter = "Project.Nether.NetherPartyCharacterModel";
    private const string ReferenceArray = "Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray";
    private const string StructArray = "Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray";
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
                NetherCheckpointContinueNativeBinding.SkipControllerTypeName,
                NetherCheckpointContinueNativeBinding.SkipDeclineCallbackInterop
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

    public static IReadOnlyList<(string TypeName, string Path, string ValueType)> RequiredMembers { get; } =
        new[]
        {
            (NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext.TypeName, "__4__this", FloorControllerType),
            (NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext.TypeName, "__t__builder", "Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder"),
            (NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext.TypeName, "__t__builder.Task", UniTask),
            (NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext.TypeName, "__1__state", "System.Int32"),
            (NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext.TypeName, "__t__builder.runnerPromise", "Cysharp.Threading.Tasks.CompilerServices.IStateMachineRunnerPromise"),
            (NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext.TypeName, "__t__builder.ex", "Il2CppSystem.Exception"),
            (NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext.TypeName, "__t__builder.Task.Status", "Cysharp.Threading.Tasks.UniTaskStatus"),
            (NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext.TypeName, "__t__builder.Task.source", "Cysharp.Threading.Tasks.IUniTaskSource"),
            ("Project.Nether.FloorSelection.SubScene", "_subViewController", FloorControllerType),
            (FloorControllerType, "_netherModel", NetherModel),
            (NetherModel, "MNetherId", "System.Int64"),
            (NetherModel, "MNetherMapId", "System.Int64"),
            (NetherModel, "StatusType", "Project.User.NetherStatusType"),
            (NetherModel, "ErosionPoint", "System.Int32"),
            (NetherModel, "NetherGold", "System.Int32"),
            (NetherModel, "TreasureKey", "System.Int32"),
            (NetherModel, "CurrentFloorModel", FloorModel),
            (NetherModel, "MapModel", "Project.Nether.FloorSelection.NetherMapModel"),
            (NetherModel, "MapModel.FloorModelListPerFloorLevel", "Il2CppSystem.Collections.Generic.Dictionary<System.Int32,Il2CppSystem.Collections.Generic.List<" + FloorModel + ">>"),
            (NetherModel, "PartyModel", PartyModel),
            (PartyModel, "CharacterModels", ReferenceArray + "<" + PartyCharacter + ">"),
            (FloorModel, "MNetherMapFloorId", "System.Int64"),
            (FloorModel, "ExtendId", "System.Int64"),
            (FloorModel, "FloorLevel", "System.Int32"),
            (FloorModel, "FloorIndex", "System.Int32"),
            (FloorModel, "ApiFloorIndex", "System.Int32"),
            (FloorModel, "FloorType", "Project.Master.NetherFloorType"),
            (FloorModel, "IsSecretFloor", "System.Boolean"),
            (FloorModel, "IsUnlocked", "System.Boolean"),
            (FloorModel, "MNetherMapFloorPrevIds", StructArray + "<System.Int64>"),
            (PartyCharacter, "MCharacterId", "System.Int64"),
            (PartyCharacter, "PartyIndex", "System.Int32"),
            (PartyCharacter, "PartyPosition", "Project.Common.CharacterPartyPosition"),
            (PartyCharacter, "ElementType", "Project.ElementType"),
            (PartyCharacter, "ManaType", "Project.Master.ManaType"),
            (PartyCharacter, "HpRatio", "System.Single"),
            (PartyCharacter, "IsAlive", "System.Boolean"),
            (PartyCharacter, "Level", "System.Int32"),
            (PartyCharacter, "LimitBreakCount", "System.Int32"),
            (PartyCharacter, "BasicParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "BondParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "BuildingMultiplicationParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "CharacterAbilityAdditionParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "CharacterAbilityMultiplicationParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "EquipmentAbilityAdditionParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "EquipmentAbilityMultiplicationParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "WeaponBasicParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "ArmorBasicParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "AccessoryBasicParameters", "Project.Common.CharacterParameters"),
            (PartyCharacter, "CharacterAbilityEffectModels", ReferenceArray + "<Project.Outgame.AbilityEffectModel>"),
            (PartyCharacter, "EquipmentAbilityEffectModels", ReferenceArray + "<Project.Outgame.AbilityEffectModel>"),
            (PartyCharacter, "GeneralAbilityEffectModels", ReferenceArray + "<Project.Outgame.AbilityEffectModel>"),
            ("Project.Common.CharacterParameters", "Table", "Il2CppSystem.Collections.Generic.Dictionary<Project.Master.ParameterType,System.Int32>"),
            ("Project.Outgame.AbilityEffectModel", "Effect", "Project.IAbilityEffectData"),
            ("Project.Outgame.AbilityEffectModel", "Effect.ID", "System.Int64"),
            ("Project.Outgame.AbilityEffectModel", "Level", "System.Int32"),
            ("Project.Outgame.AbilityEffectModel", "AwakeningLevel", "System.Int32"),
            ("Project.Outgame.AbilityEffectModel", "Type", "Project.AbilityType"),
            ("Project.Outgame.AbilityEffectModel", "Value", "System.Int32"),
            (CodeSelectControllerType, "_mIds", StructArray + "<System.Int64>"),
            (CodeSelectControllerType, "_model", "Project.Nether.AbyssCodeSelectPopup.AbyssCodeSelectPopupModel"),
            (CodeSelectControllerType, "_partyModel", PartyModel),
            (CodeSelectControllerType, "_cancellationToken", "Il2CppSystem.Threading.CancellationToken"),
            (CodeListControllerType, "_popupType", "Project.Nether.NetherAbyssCodeListPopup.AbyssCodeListPopupType"),
            (CodeListControllerType, "_replaceMId", "System.Int64"),
            (CodeListControllerType, "_modelDictionary", "Il2CppSystem.Collections.Generic.Dictionary<System.Int32,Il2CppSystem.Collections.Generic.List<Project.Nether.AbyssCodeThumbnailModel>>"),
            (CodeListControllerType, "TabIndexes", "Il2CppSystem.Collections.Generic.Dictionary<System.Int32,System.Int32>"),
            ("Project.Nether.NetherAbyssCodeListPopup.AbyssCodeListPopup", "_tabGroupView", "Project.Outgame.UIParts.TabGroupView"),
            ("Project.Nether.NetherAbyssCodeListPopup.AbyssCodeListPopup", "_tabGroupView.CurrentIndex", "System.Int32"),
            ("Project.Nether.AbyssCodeThumbnailModel", "MNetherCodeId", "System.Int64"),
            ("Project.Nether.AbyssCodeThumbnailModel", "NetherCodeCategoryType", "Project.NetherCodeCategoryType"),
            ("Project.Nether.AbyssCodeThumbnailModel", "IsSelected", "System.Boolean"),
            ("Project.Nether.AbyssCodeReplacePopup.AbyssCodeReplacePopupController", "_beforeMNetherCodeId", "System.Int64"),
            ("Project.Nether.AbyssCodeReplacePopup.AbyssCodeReplacePopupController", "_afterMNetherCodeId", "System.Int64"),
            ("Project.Nether.AbyssCodeReplacePopup.AbyssCodeReplacePopupController", "_onCompleted", "Il2CppSystem.Action<System.Boolean>"),
            ("Project.Nether.AbyssCodeReplaceCompletePopup.AbyssCodeReplaceCompletePopupController", "_beforeMNetherCodeId", "System.Int64"),
            ("Project.Nether.AbyssCodeReplaceCompletePopup.AbyssCodeReplaceCompletePopupController", "_afterMNetherCodeId", "System.Int64"),
            ("Project.Nether.AbyssCodeChangePopup.AbyssCodeChangePopupController", "_mNetherCodeId", "System.Int64"),
            ("Project.Nether.AbyssCodeChangePopup.AbyssCodeChangePopupController", "_onCompleted", "Il2CppSystem.Action<System.Boolean>"),
            ("Project.Nether.AbyssCodeChangeCompletePopup.AbyssCodeChangeCompletePopupController", "_beforeMNetherCodeId", "System.Int64"),
            ("Project.Nether.AbyssCodeChangeCompletePopup.AbyssCodeChangeCompletePopupController", "_afterMNetherCodeId", "System.Int64"),
            (EventControllerType, "_mCharacterId", "System.Int64"),
            (EventControllerType, "_mNetherEvents", "Project.Master.NoaMessagePack.MNetherFloorEvents"),
            (EventControllerType, "_mNetherEventPartsArray", ReferenceArray + "<Project.Master.NoaMessagePack.MNetherFloorEventParts>"),
            (RecoveryControllerType, "_mNetherEvents", "Project.Master.NoaMessagePack.MNetherFloorEvents"),
            (RecoveryControllerType, "_mNetherEventPartsArray", ReferenceArray + "<Project.Master.NoaMessagePack.MNetherFloorEventParts>"),
            (TreasureControllerType, "_mNetherEvents", "Project.Master.NoaMessagePack.MNetherFloorEvents"),
            (TreasureControllerType, "_mNetherEventPartsArray", ReferenceArray + "<Project.Master.NoaMessagePack.MNetherFloorEventParts>"),
            (TreasurePopupType, "SkipAndConfirmButton", "Project.AppButton"),
            (ShopControllerType, "_mNetherFloorShopContentsArray", ReferenceArray + "<Project.Master.NoaMessagePack.MNetherFloorShopContents>"),
            (ReturnPopupType, "ReturnableItemScrollViewController", ReturnScrollControllerType),
            (ReturnScrollControllerType, "_contentModelList", "Il2CppSystem.Collections.Generic.List<Project.Outgame.UI.ContentModel>"),
            (ReturnScrollControllerType, "_maxSelectedCount", "System.Int32"),
            (ReturnControllerType, "_maxSelectedCount", "System.Int32"),
            ("Project.Nether.NetherContinueConfirmPopup.NetherContinueConfirmPopupController", "_canBoost", "System.Boolean"),
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
    public static NetherNativeCompatibilityReport Validate(IEnumerable<Assembly> assemblies) =>
        ValidateCore(assemblies, typeof(Plugin).Assembly, inspectGeneratedInstances: true);

    // Offline assembly characterization cannot initialize the game's native singleton objects.
    public static NetherNativeCompatibilityReport ValidateMetadata(IEnumerable<Assembly> assemblies, Assembly? plugin = null) =>
        ValidateCore(assemblies, plugin ?? typeof(Plugin).Assembly, inspectGeneratedInstances: false);

    private static NetherNativeCompatibilityReport ValidateCore(
        IEnumerable<Assembly> assemblies,
        Assembly plugin,
        bool inspectGeneratedInstances
    )
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
                        out MethodInfo? method
                    ))
                {
                    failures.Add(binding.TypeName + "." + binding.Method.Name + " => " + error);
                }
                else
                    ValidateTaskStatus(method!, failures);
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
                MethodInfo? method;
                if (contract.Method.IsStatic)
                {
                    resolved = NetherCodePopupInteropResolver.TryResolveStaticMethodExactIdentity(
                        type,
                        contract.Method,
                        out error,
                        out method
                    );
                }
                else if (inspectGeneratedInstances)
                {
                    resolved = NetherCodePopupInteropResolver.TryResolveGeneratedCallback(
                        type,
                        contract.Method,
                        out error,
                        out _,
                        out method
                    );
                }
                else
                {
                    resolved = NetherCodePopupInteropResolver.TryResolveGeneratedCallbackTarget(
                        type,
                        contract.Method,
                        out error,
                        out _,
                        out method
                    );
                }
                if (!resolved)
                    failures.Add(contract.TypeName + "." + contract.Method.ManagedName + " => " + error);
                else
                    ValidateTaskStatus(method!, failures);
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

        ValidateMembers(loaded, failures);
        ValidateCodeConfirmationMembers(loaded, failures);
        ValidatePatchCatalog(failures);
        int compiledReferences = NetherCompiledInteropPreflight.Validate(plugin, failures);
        return new NetherNativeCompatibilityReport(
            NetherNativeBindingCatalog.RequiredMethods.Count,
            NetherNativeBindingCatalog.RequiredGeneratedMethods.Count,
            NetherNativeBindingCatalog.RequiredMembers.Count,
            compiledReferences,
            failures
        );
    }

    private static void ValidateTaskStatus(MethodInfo method, List<string> failures)
    {
        string returnType = NetherLifecycleInteropBindings.TypeName(method.ReturnType);
        if (returnType != "Cysharp.Threading.Tasks.UniTask"
            && !returnType.StartsWith("Cysharp.Threading.Tasks.UniTask<", StringComparison.Ordinal))
            return;

        var status = new NetherNativeMethodDescriptor(
            "get_Status", Array.Empty<string>(), "Cysharp.Threading.Tasks.UniTaskStatus"
        ) { IsStatic = false };
        if (!NetherLifecycleInteropBindings.TryResolveExactMethod(
                method.ReturnType,
                status,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                out string error,
                out _
            ))
        {
            failures.Add(method.DeclaringType?.FullName + "." + method.Name + ".return.Status => " + error);
        }
    }

    private static void ValidateMembers(Assembly[] loaded, List<string> failures)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var binding in NetherNativeBindingCatalog.RequiredMembers)
        {
            try
            {
                Type? type = NetherLifecycleInteropBindings.ResolveType(loaded, binding.TypeName);
                foreach (string name in binding.Path.Split('.'))
                {
                    if (type == null)
                        break;
                    PropertyInfo? property = type.GetProperty(name, flags);
                    if (property != null && property.GetMethod != null && property.GetIndexParameters().Length == 0)
                        type = property.PropertyType;
                    else
                        type = type.GetMethod("get_" + name, flags, null, Type.EmptyTypes, null)?.ReturnType
                            ?? type.GetField(name, flags)?.FieldType
                            ?? type.GetField("<" + name + ">k__BackingField", flags)?.FieldType;
                }
                if (type == null || !string.Equals(NetherLifecycleInteropBindings.TypeName(type), binding.ValueType, StringComparison.Ordinal))
                    failures.Add(binding.TypeName + "." + binding.Path + " => missing-or-incompatible-member:" + binding.ValueType);
            }
            catch (Exception ex)
            {
                failures.Add(binding.TypeName + "." + binding.Path + " => precheck-exception:" + ex.GetType().Name + ":" + ex.Message);
            }
        }
    }

    private static void ValidateCodeConfirmationMembers(Assembly[] loaded, List<string> failures)
    {
        try
        {
            Type? controller = NetherLifecycleInteropBindings.ResolveType(
                loaded, NetherNativeBindingCatalog.CodeSelectControllerType
            );
            Type? utility = NetherLifecycleInteropBindings.ResolveType(
                loaded, NetherNativeBindingCatalog.NetherUtilityType
            );
            if (!NetherCodeConfirmTaskInvoker.TryResolve(
                    controller!,
                    utility!,
                    NetherCodePopupNativeBinding.ConfirmTaskBinding(NetherNativeBindingCatalog.CodeSelectControllerType),
                    out string error,
                    out _,
                    out _,
                    out _
                ))
                failures.Add(error);
        }
        catch (Exception ex)
        {
            failures.Add("code-confirm-members => precheck-exception:" + ex.GetType().Name + ":" + ex.Message);
        }
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
