#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherLifecycleInteropBindingsTests
{
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "AutoNether")))
                return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("repository root not found");
    }

    [Fact]
    public void Packaged_battle_hud_uses_the_mandatory_token_free_settings_initialization_seam()
    {
        using var packaged = PackagedProjectAssembly.Load();
        NetherInteropPatchBinding binding = Assert.Single(
            NetherLifecycleInteropBindings.All,
            candidate => candidate.TypeName == "Project.Ingame.BottomRightView"
                && candidate.Method.Name == "InitializeTimeScaleButtons"
        );

        Assert.True(
            NetherLifecycleInteropBindings.TryResolve(
                new[] { packaged.Assembly },
                binding,
                out string error,
                out MethodInfo? method
            ),
            error
        );
        Assert.Equal(
            new[] { "Project.Ingame.IIngameUserSettings" },
            method!.GetParameters().Select(parameter => parameter.ParameterType.FullName).ToArray()
        );
        Assert.DoesNotContain(
            NetherLifecycleInteropBindings.All,
            candidate => candidate.TypeName == "Project.Ingame.BottomRightView"
                && candidate.Method.ParameterTypeNames.Contains("Il2CppSystem.Threading.CancellationToken")
        );
    }

    [Fact]
    public void Packaged_ingame_settings_wrapper_is_accepted_by_the_exact_native_accessor()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type settingsType = packaged.RequireType("Project.Ingame.IIngameUserSettings");
        object settings = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(settingsType);
        // This is a metadata-only characterizer.  The generated wrapper inherits the native
        // Il2CppObjectBase finalizer, but this deliberately uninitialized instance owns no
        // native pointer.  Do not let a later GC call into IL2CPP after the isolated packaged
        // assembly load context has been unloaded.
        GC.SuppressFinalize(settings);

        Assert.False(settingsType.IsInterface);
        Assert.True(
            NetherBattleSettingsNativeAccessor.TryCreate(settings, out var accessor, out string error),
            error
        );
        Assert.NotNull(accessor);
    }

    [Fact]
    public void Packaged_battle_result_model_is_a_post_request_nether_terminal_seam()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type utility = packaged.RequireType("Project.BattleResult.BattleResultUtility");
        MethodInfo method = Assert.Single(
            utility.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
            candidate => candidate.Name == "CreateBattleResultModel"
        );

        Assert.Equal("Project.BattleResult.IBattleResultModel", method.ReturnType.FullName);
        Assert.Equal(
            new[]
            {
                "Project.Ingame.BattleResultType",
                "Absf.ISceneTransitionParam",
                "Project.BattleResult.Top.BattleClearRecordBase",
                "Project.Api.IFinishQuestResponseEntity",
                "Il2CppSystem.Threading.CancellationToken",
            },
            method.GetParameters().Select(parameter => parameter.ParameterType.FullName).ToArray()
        );
    }

    [Fact]
    public void Packaged_nether_battle_result_exposes_exact_ready_task_and_next_callback()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type controller = packaged.RequireType(NetherBattleResultNextNativeBinding.ControllerTypeName);

        Assert.True(
            NetherLifecycleInteropBindings.TryResolveExactMethod(
                controller,
                NetherBattleResultNextNativeBinding.InitializeViewDescriptor,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                out string initializeError,
                out MethodInfo? initialize
            ),
            initializeError
        );
        Assert.NotNull(initialize);

        Assert.True(
            NetherCodePopupInteropResolver.TryResolveGeneratedCallbackTarget(
                controller,
                NetherBattleResultNextNativeBinding.NextCallbackInterop,
                out string nextError,
                out MemberInfo? singleton,
                out MethodInfo? next
            ),
            nextError
        );
        Assert.NotNull(singleton);
        Assert.Equal("_InitializeViewAsync_b__21_1", next!.Name);
    }

    [Fact]
    public void Packaged_floor_selection_exposes_exact_current_floor_event_sequence_task()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type controller = packaged.RequireType(NetherFloorEventSequenceNativeBinding.ControllerTypeName);

        Assert.True(
            NetherLifecycleInteropBindings.TryResolveExactMethod(
                controller,
                NetherFloorEventSequenceNativeBinding.SequenceDescriptor,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                out string error,
                out MethodInfo? method
            ),
            error
        );
        Assert.NotNull(method);
        Assert.Equal("ExecuteCurrentFloorEventSequenceAsync", method!.Name);
        Assert.Empty(method.GetParameters());
        Assert.Equal("Cysharp.Threading.Tasks.UniTask", method.ReturnType.FullName);
    }

    [Fact]
    public void Packaged_event_update_protocol_has_no_character_target_and_returns_party_statuses()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type request = packaged.RequireType("Project.Api.NetherUpdateEventRequestEntity");
        Type response = packaged.RequireType("Project.Api.NetherUpdateEventResponseEntity");

        string[] requestFields = request
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "ApiName",
                "Method",
                "floor_index",
                "floor_level",
                "m_nether_code_id",
                "m_nether_id",
                "m_nether_map_id",
                "select_number",
            },
            requestFields
        );
        Assert.Contains(
            response.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
            property => property.Name == "t_nether_characters"
        );
    }

    [Fact]
    public void Packaged_floor_selection_exposes_exact_start_status_parent_task()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type controller = packaged.RequireType(
            NetherLifecycleInteropBindings.StartStatusTask.TypeName
        );

        Assert.True(
            NetherLifecycleInteropBindings.TryResolveExactMethod(
                controller,
                NetherLifecycleInteropBindings.StartStatusTask.Method,
                NetherLifecycleInteropBindings.StartStatusTask.Flags,
                out string error,
                out MethodInfo? method
            ),
            error
        );
        Assert.NotNull(method);
        Assert.Equal("HandleStartEventByStatusAsync", method!.Name);
        Assert.Equal(new[] { "System.Boolean" }, method.GetParameters()
            .Select(parameter => parameter.ParameterType.FullName)
            .ToArray());
        Assert.Equal("Cysharp.Threading.Tasks.UniTask", method.ReturnType.FullName);
    }

    [Fact]
    public void Patch_manager_registers_both_start_status_entry_paths()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "AutoNether",
            "Patches",
            "PatchManager.cs"
        ));

        Assert.Contains(
            "CreateAndPatchAll(typeof(NetherAutoClimbStartStatusLifecyclePatch))",
            source,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "CreateAndPatchAll(typeof(NetherAutoClimbStartStatusTaskPatch))",
            source,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Packaged_floor_selection_exposes_the_actual_start_status_state_machine_seam()
    {
        using var packaged = PackagedProjectAssembly.Load();
        NetherInteropPatchBinding binding = NetherLifecycleInteropBindings.StartStatusStateMachineMoveNext;
        Type stateMachine = packaged.RequireType(binding.TypeName);

        Assert.True(
            NetherLifecycleInteropBindings.TryResolveExactMethod(
                stateMachine,
                binding.Method,
                binding.Flags,
                out string error,
                out MethodInfo? moveNext
            ),
            error
        );
        Assert.NotNull(moveNext);
        Assert.Empty(moveNext!.GetParameters());
        Assert.Equal(typeof(void), moveNext.ReturnType);

        PropertyInfo controller = Assert.Single(
            stateMachine.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name == "__4__this"
        );
        PropertyInfo builder = Assert.Single(
            stateMachine.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name == "__t__builder"
        );
        PropertyInfo parentTask = Assert.Single(
            builder.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name == "Task"
        );
        PropertyInfo runnerPromise = Assert.Single(
            builder.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name == "runnerPromise"
        );
        PropertyInfo builderException = Assert.Single(
            builder.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name == "ex"
        );
        PropertyInfo taskSource = Assert.Single(
            parentTask.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name == "source"
        );

        Assert.Equal(NetherLifecycleInteropBindings.StartStatusTask.TypeName, controller.PropertyType.FullName);
        Assert.Equal("Cysharp.Threading.Tasks.UniTask", parentTask.PropertyType.FullName);
        Assert.Equal(
            "Cysharp.Threading.Tasks.CompilerServices.IStateMachineRunnerPromise",
            runnerPromise.PropertyType.FullName
        );
        Assert.Equal("Cysharp.Threading.Tasks.IUniTaskSource", taskSource.PropertyType.FullName);
        Assert.Contains("Exception", builderException.PropertyType.FullName ?? string.Empty);
    }

    [Fact]
    public void Packaged_preserve_service_exposes_exact_start_clear_and_close_task_seams()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type preserve = packaged.RequireType(
            "Project.Ingame.Exploration.ExplorationQuestPreserveAPIService"
        );
        MethodInfo[] methods = preserve.GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        AssertTaskSeam(
            methods,
            "Project_Ingame_Exploration_IExplorationQuestAPIService_StartQuestAsync",
            "Project.Api.BattleSessionStatusResponseEntity",
            "Il2CppSystem.Threading.CancellationToken"
        );
        AssertTaskSeam(
            methods,
            "Project_Ingame_Exploration_IExplorationQuestAPIService_ClearQuestAsync",
            "Project.Api.IFinishQuestResponseEntity",
            "Project.Ingame.Exploration.ExplorationBattleEndRecord",
            "Il2CppSystem.Threading.CancellationToken",
            "System.Boolean"
        );
        AssertTaskSeam(
            methods,
            "Project_Ingame_Exploration_IExplorationQuestAPIService_CloseQuestAsync",
            "Project.Api.IFinishQuestResponseEntity",
            "Project.Ingame.Exploration.ExplorationBattleEndRecord",
            "Il2CppSystem.Threading.CancellationToken"
        );
    }

    [Fact]
    public void Packaged_project_resolves_every_F12_lifecycle_binding_without_a_global_type_scan()
    {
        using var packaged = PackagedProjectAssembly.Load();
        NetherInteropPatchBinding[] bindings = NetherLifecycleInteropBindings.All
            .Where(binding => binding.TypeName.StartsWith("Project.", StringComparison.Ordinal))
            .ToArray();
        var failures = new List<string>();
        var resolvedNames = new List<string>();

        foreach (NetherInteropPatchBinding binding in bindings)
        {
            if (!NetherLifecycleInteropBindings.TryResolve(
                    new[] { packaged.Assembly },
                    binding,
                    out string error,
                    out MethodInfo? method
                ))
            {
                failures.Add(binding.TypeName + "." + binding.Method.Name + " => " + error);
                continue;
            }

            resolvedNames.Add(method!.Name);
        }

        Assert.Equal(28, bindings.Length);
        Assert.Empty(failures);
        Assert.Contains("Project_ISubService_Terminate", resolvedNames);
        Assert.Equal(17, bindings.Count(binding => binding.Method.Name == "SetupPopupEvent"));
        Assert.Equal(
            new[] { "OnEntered", "OnInitializeAsync", "OnRefreshAsync" },
            bindings
                .Where(binding => binding.TypeName == "Project.Nether.FloorSelection.SubScene")
                .Select(binding => binding.Method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray()
        );
    }

    [Fact]
    public void Packaged_content_acquired_popup_exposes_the_exact_confirm_close_contract()
    {
        using var packaged = PackagedProjectAssembly.Load();
        NetherInteropPatchBinding binding = Assert.Single(
            NetherLifecycleInteropBindings.All,
            candidate => candidate.TypeName
                == "Project.Nether.NetherContentAcquiredPopup.NetherContentAcquiredPopupController"
        );

        Assert.True(
            NetherLifecycleInteropBindings.TryResolve(
                new[] { packaged.Assembly },
                binding,
                out string error,
                out MethodInfo? method
            ),
            error
        );
        Assert.NotNull(method);
        Assert.Equal("SetupPopupEvent", method!.Name);
        Assert.Equal(
            new[]
            {
                "Project.Nether.NetherContentAcquiredPopup.NetherContentAcquiredPopup",
                "Il2CppSystem.Action",
            },
            method.GetParameters().Select(parameter => parameter.ParameterType.FullName).ToArray()
        );
    }

    [Fact]
    public void Packaged_code_received_popup_exposes_the_exact_confirm_close_contract()
    {
        using var packaged = PackagedProjectAssembly.Load();
        NetherInteropPatchBinding binding = Assert.Single(
            NetherLifecycleInteropBindings.All,
            candidate => candidate.TypeName
                == "Project.Nether.AbyssCodeReceivedPopup.AbyssCodeReceivedPopupController"
        );

        Assert.True(
            NetherLifecycleInteropBindings.TryResolve(
                new[] { packaged.Assembly },
                binding,
                out string error,
                out MethodInfo? method
            ),
            error
        );
        Assert.NotNull(method);
        Assert.Equal("SetupPopupEvent", method!.Name);
        Assert.Equal(
            new[]
            {
                "Project.Nether.AbyssCodeReceivedPopup.AbyssCodeReceivedPopup",
                "Il2CppSystem.Action",
            },
            method.GetParameters().Select(parameter => parameter.ParameterType.FullName).ToArray()
        );

    }

    [Fact]
    public void Packaged_code_list_popup_exposes_exact_asynchronous_initialization_task()
    {
        using var packaged = PackagedProjectAssembly.Load();
        NetherInteropPatchBinding binding = NetherLifecycleInteropBindings.CodeListInitializationTask;
        Type controller = packaged.RequireType(binding.TypeName);
        string actual = string.Join("|", controller.GetMethods(binding.Flags)
            .Where(method => method.Name == binding.Method.Name)
            .Select(method => method.ReturnType.FullName
                + "("
                + string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName))
                + ")"));

        Assert.True(
            NetherLifecycleInteropBindings.TryResolve(
                new[] { packaged.Assembly },
                binding,
                out string error,
                out MethodInfo? method
            ),
            error + ";actual=" + actual
        );
        Assert.NotNull(method);
    }

    [Fact]
    public void Packaged_code_list_exposes_category_to_tab_and_bucket_model_coordinates()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type controller = packaged.RequireType(
            "Project.Nether.NetherAbyssCodeListPopup.AbyssCodeListPopupController"
        );
        Type thumbnail = packaged.RequireType("Project.Nether.AbyssCodeThumbnailModel");
        const BindingFlags flags = BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        PropertyInfo tabIndexes = Assert.Single(
            controller.GetProperties(flags),
            property => property.Name == "TabIndexes"
        );
        Assert.Equal(
            new[] { "System.Int32", "System.Int32" },
            tabIndexes.PropertyType.GetGenericArguments()
                .Select(argument => argument.FullName)
                .ToArray()
        );
        PropertyInfo modelDictionary = Assert.Single(
            controller.GetProperties(flags),
            property => property.Name == "_modelDictionary"
        );
        Assert.Equal(
            "System.Int32",
            modelDictionary.PropertyType.GetGenericArguments()[0].FullName
        );
        PropertyInfo category = Assert.Single(
            thumbnail.GetProperties(flags),
            property => property.Name == "NetherCodeCategoryType"
        );
        Assert.Equal("Project.NetherCodeCategoryType", category.PropertyType.FullName);
    }


    [Fact]
    public void Packaged_floor_event_hint_box_exposes_the_exact_native_dismiss_contract()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type controller = packaged.RequireType(
            "Project.Nether.NetherFloorEventHintBox.NetherFloorEventHintBoxPopupController"
        );
        NetherInteropPatchBinding binding = Assert.Single(
            NetherLifecycleInteropBindings.All,
            candidate => candidate.TypeName == controller.FullName
        );

        Assert.True(
            NetherLifecycleInteropBindings.TryResolve(
                new[] { packaged.Assembly },
                binding,
                out string setupError,
                out MethodInfo? setup
            ),
            setupError
        );
        Assert.Equal(
            new[]
            {
                "Project.Nether.NetherFloorEventHintBox.NetherFloorEventHintBoxPopup",
                "Il2CppSystem.Action",
            },
            setup!.GetParameters().Select(parameter => parameter.ParameterType.FullName).ToArray()
        );

        Assert.True(
            NetherCodePopupInteropResolver.TryResolveGeneratedCallbackTarget(
                controller,
                NetherLifecycleInteropBindings.FloorEventHintDismissCallback,
                out string callbackError,
                out MemberInfo? singleton,
                out MethodInfo? callback
            ),
            callbackError
        );
        Assert.NotNull(singleton);
        Assert.Equal("_SetupPopupEvent_b__3_0", callback!.Name);
    }

    [Fact]
    public void Packaged_popup_close_parameter_is_Il2Cpp_Action_not_System_Action()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type controller = packaged.RequireType(
            "Project.Nether.NetherContinueConfirmPopup.NetherContinueConfirmPopupController"
        );
        MethodInfo setup = Assert.Single(controller.GetMethods(), method => method.Name == "SetupPopupEvent");

        Assert.Equal("Il2CppSystem.Action", setup.GetParameters()[1].ParameterType.FullName);
    }

    [Fact]
    public void Packaged_read_only_nether_sync_resolves_by_exact_interop_signature()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type dataStore = packaged.RequireType(NetherReadOnlyReconcileNativeBinding.DataStoreTypeName);
        MethodInfo[] candidates = dataStore.GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        ).Where(method => method.Name == NetherReadOnlyReconcileNativeBinding.SyncDescriptor.Name).ToArray();
        string actual = string.Join("|", candidates.Select(method =>
            method.ReturnType.FullName
            + "("
            + string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName))
            + ")"
        ));

        Assert.True(
            NetherLifecycleInteropBindings.TryResolveExactMethod(
                dataStore,
                NetherReadOnlyReconcileNativeBinding.SyncDescriptor,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                out string error,
                out MethodInfo? resolved
            ),
            error + ";actual=" + actual
        );
        Assert.NotNull(resolved);
    }

    [Fact]
    public void Packaged_checkpoint_generated_callbacks_resolve_by_sanitized_name_and_exact_signature()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type continueController = packaged.RequireType(
            "Project.Nether.NetherContinueConfirmPopup.NetherContinueConfirmPopupController"
        );
        Type boostController = packaged.RequireType(
            "Project.Nether.NetherBoostConfirmPopup.NetherBoostConfirmPopupController"
        );
        var cases = new[]
        {
            (continueController, NetherCheckpointContinueNativeBinding.ContinueCallbackInterop, "_SetupPopupEvent_b__8_2"),
            (continueController, NetherCheckpointContinueNativeBinding.FinishCallbackInterop, "_SetupPopupEvent_b__8_1"),
            (boostController, NetherCheckpointContinueNativeBinding.BoostSetCountInterop, "_SetupPopupEvent_b__7_2"),
            (boostController, NetherCheckpointContinueNativeBinding.BoostConfirmInterop, "_SetupPopupEvent_b__7_1"),
        };

        foreach ((Type controller, NetherCodePopupInteropMethodBinding binding, string expectedName) in cases)
        {
            Assert.True(
                NetherCodePopupInteropResolver.TryResolveGeneratedCallbackTarget(
                    controller,
                    binding,
                    out string error,
                    out MemberInfo? singleton,
                    out MethodInfo? callback
                ),
                error
            );
            Assert.NotNull(singleton);
            Assert.Equal(expectedName, callback!.Name);
        }
    }

    [Fact]
    public void Packaged_shop_close_generated_callback_resolves_without_cpp2il_holder_names()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type controller = packaged.RequireType(
            "Project.Nether.NetherShopPopup.NetherShopPopupController"
        );

        Assert.True(
            NetherCodePopupInteropResolver.TryResolveGeneratedCallbackTarget(
                controller,
                NetherLifecycleInteropBindings.ShopCloseCallback,
                out string error,
                out MemberInfo? singleton,
                out MethodInfo? callback
            ),
            error
        );
        Assert.NotNull(singleton);
        Assert.Equal("_SetupPopupEvent_b__16_0", callback!.Name);
    }

    [Fact]
    public void Packaged_shop_purchase_confirm_popup_and_exact_confirm_callback_resolve()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type controller = packaged.RequireType(
            "Project.Nether.NetherShopConfirmPopup.NetherShopConfirmPopupController"
        );

        NetherInteropPatchBinding setup = Assert.Single(
            NetherLifecycleInteropBindings.All,
            binding => binding.TypeName == controller.FullName
                && binding.Method.Name == "SetupPopupEvent"
        );
        Assert.Equal(
            new[]
            {
                "Project.Nether.NetherShopConfirmPopup.NetherShopConfirmPopup",
                "Il2CppSystem.Action",
            },
            setup.Method.ParameterTypeNames
        );
        Assert.True(
            NetherCodePopupInteropResolver.TryResolveGeneratedCallbackTarget(
                controller,
                NetherLifecycleInteropBindings.ShopPurchaseConfirmCallback,
                out string error,
                out MemberInfo? singleton,
                out MethodInfo? callback
            ),
            error
        );
        Assert.NotNull(singleton);
        Assert.Equal("_SetupPopupEvent_b__5_1", callback!.Name);
    }

    [Fact]
    public void Packaged_return_popup_exposes_its_exact_nested_scroll_controller()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type popup = packaged.RequireType(
            "Project.Nether.NetherReturnItemSelectionPopup.NetherReturnItemSelectionPopup"
        );
        PropertyInfo scroll = Assert.Single(
            popup.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name == "ReturnableItemScrollViewController"
        );

        Assert.Equal(
            "Project.Nether.NetherReturnItemSelectionPopup.NetherReturnableItemScrollViewController",
            scroll.PropertyType.FullName
        );
        Type scrollType = scroll.PropertyType;
        Assert.Contains(
            scrollType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            member => member.Name == "_contentModelList"
        );
        Assert.Contains(
            scrollType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            member => member.Name == "_maxSelectedCount"
        );
    }

    [Fact]
    public void Packaged_code_transform_callbacks_and_generated_task_have_exact_interop_bindings()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type confirmController = packaged.RequireType(
            "Project.Nether.AbyssCodeChangePopup.AbyssCodeChangePopupController"
        );
        Type completeController = packaged.RequireType(
            "Project.Nether.AbyssCodeChangeCompletePopup.AbyssCodeChangeCompletePopupController"
        );
        Type listController = packaged.RequireType(
            "Project.Nether.NetherAbyssCodeListPopup.AbyssCodeListPopupController"
        );
        Type utility = packaged.RequireType("Project.Nether.NetherUtility");

        Assert.True(NetherCodePopupInteropResolver.TryResolveGeneratedCallbackTarget(
            confirmController,
            NetherCodeTransformNativeBinding.ConfirmCallbackBinding,
            out string confirmError,
            out MemberInfo? confirmSingleton,
            out MethodInfo? confirm
        ), confirmError);
        Assert.True(NetherCodePopupInteropResolver.TryResolveGeneratedCallbackTarget(
            completeController,
            NetherCodeTransformNativeBinding.CompleteCloseCallbackBinding,
            out string closeError,
            out MemberInfo? closeSingleton,
            out MethodInfo? close
        ), closeError);
        bool taskResolved = NetherCodePopupInteropResolver.TryResolveStaticMethod(
            utility,
            NetherCodeTransformNativeBinding.TransformTaskBinding(listController.FullName!),
            out string taskError,
            out MethodInfo? task
        );
        Assert.True(taskResolved, taskError + ":" + string.Join("|", utility.GetMethods(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
        ).Where(method => method.Name == NetherCodeTransformNativeBinding.TransformTask).Select(method =>
            method.ReturnType.FullName + "(" + string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName)) + ")"
        )));

        Assert.NotNull(confirmSingleton);
        Assert.NotNull(closeSingleton);
        Assert.Equal("_SetupPopupEvent_b__6_1", confirm!.Name);
        Assert.Equal("_SetupPopupEvent_b__7_0", close!.Name);
        Assert.Equal(
            "Method_Internal_Static_UniTask_AbyssCodeListPopupController_NetherModel_NetherEventResultModel_Int64_BoolReactiveProperty_CancellationToken_PDM_0",
            task!.Name
        );

        MemberInfo completed = Assert.Single(
            confirmController.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            member => member.Name == "_onCompleted"
        );
        Type completedType = completed switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new Xunit.Sdk.XunitException("_onCompleted is not a field/property"),
        };
        Assert.True(completedType.IsGenericType);
        Assert.Equal("Il2CppSystem.Action`1", completedType.GetGenericTypeDefinition().FullName);
        Assert.Equal(typeof(bool), Assert.Single(completedType.GetGenericArguments()));
    }

    [Fact]
    public void Packaged_code_replacement_popups_expose_the_native_confirm_and_complete_lifecycles()
    {
        using var packaged = PackagedProjectAssembly.Load();
        const string confirmController =
            "Project.Nether.AbyssCodeReplacePopup.AbyssCodeReplacePopupController";
        const string completeController =
            "Project.Nether.AbyssCodeReplaceCompletePopup.AbyssCodeReplaceCompletePopupController";

        NetherInteropPatchBinding[] bindings = NetherLifecycleInteropBindings.All
            .Where(candidate => candidate.TypeName is confirmController or completeController)
            .ToArray();
        Assert.Equal(2, bindings.Length);

        foreach (NetherInteropPatchBinding binding in bindings)
        {
            Assert.Equal("SetupPopupEvent", binding.Method.Name);
            Assert.True(
                NetherLifecycleInteropBindings.TryResolve(
                    new[] { packaged.Assembly },
                    binding,
                    out string error,
                    out MethodInfo? method
                ),
                error
            );
            Assert.Equal("Il2CppSystem.Action", method!.GetParameters()[1].ParameterType.FullName);
        }

        Type confirm = packaged.RequireType(confirmController);
        Assert.Contains(
            confirm.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            member => member.Name == "_beforeMNetherCodeId"
        );
        Assert.Contains(
            confirm.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            member => member.Name == "_afterMNetherCodeId"
        );
        MemberInfo completed = Assert.Single(
            confirm.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            member => member.Name == "_onCompleted"
        );
        Type completedType = completed switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new Xunit.Sdk.XunitException("_onCompleted is not a field/property"),
        };
        Assert.Equal("Il2CppSystem.Action`1", completedType.GetGenericTypeDefinition().FullName);
        Assert.Equal(typeof(bool), Assert.Single(completedType.GetGenericArguments()));
    }

    [Fact]
    public void Packaged_floor_scene_exposes_the_initialized_floor_controller_property()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type scene = packaged.RequireType("Project.Nether.FloorSelection.SubScene");
        PropertyInfo controller = Assert.Single(
            scene.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name == "_subViewController"
        );

        Assert.Equal(
            "Project.Nether.FloorSelection.SubViewController",
            controller.PropertyType.FullName
        );
    }

    [Fact]
    public void Packaged_event_popup_exposes_the_exact_target_character_member()
    {
        using var packaged = PackagedProjectAssembly.Load();
        Type controller = packaged.RequireType(
            "Project.Nether.NetherEventPopup.NetherEventPopupController"
        );
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MemberInfo? target = (MemberInfo?)controller.GetProperty("_mCharacterId", flags)
            ?? controller.GetField("_mCharacterId", flags);

        Assert.NotNull(target);
        Type targetType = target switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new Xunit.Sdk.XunitException("_mCharacterId is not a field/property"),
        };
        Assert.Equal(typeof(long), targetType);
    }

    private static void AssertTaskSeam(
        IEnumerable<MethodInfo> methods,
        string name,
        string resultType,
        params string[] parameterTypes
    )
    {
        MethodInfo method = Assert.Single(methods, candidate => candidate.Name == name);
        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal("Cysharp.Threading.Tasks.UniTask`1", method.ReturnType.GetGenericTypeDefinition().FullName);
        Assert.Equal(resultType, Assert.Single(method.ReturnType.GetGenericArguments()).FullName);
        Assert.Equal(
            parameterTypes,
            method.GetParameters().Select(parameter => parameter.ParameterType.FullName).ToArray()
        );
    }

    private sealed class PackagedProjectAssembly : IDisposable
    {
        private readonly AssemblyLoadContext _context;

        private PackagedProjectAssembly(AssemblyLoadContext context, Assembly assembly)
        {
            _context = context;
            Assembly = assembly;
        }

        public Assembly Assembly { get; }

        public static PackagedProjectAssembly Load()
        {
            const string interopDirectory = "/game/BepInEx/interop";
            const string coreDirectory = "/game/BepInEx/core";
            const string projectPath = interopDirectory + "/Project.dll";
            Assert.True(File.Exists(projectPath), "packaged Project.dll must be mounted read-only at /game");

            var context = new AssemblyLoadContext("lifecycle-packaged-project", isCollectible: true);
            context.Resolving += (_, name) =>
            {
                string candidate = Path.Combine(interopDirectory, name.Name + ".dll");
                if (File.Exists(candidate))
                    return context.LoadFromAssemblyPath(candidate);
                candidate = Path.Combine(coreDirectory, name.Name + ".dll");
                return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
            };
            return new PackagedProjectAssembly(context, context.LoadFromAssemblyPath(projectPath));
        }

        public Type RequireType(string name) => Assembly.GetType(name, throwOnError: false)
            ?? throw new Xunit.Sdk.XunitException("missing packaged type: " + name);

        public void Dispose() => _context.Unload();
    }
}
