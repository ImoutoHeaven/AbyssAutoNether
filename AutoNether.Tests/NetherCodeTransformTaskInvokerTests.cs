#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodeTransformTaskInvokerTests
{
    [Fact]
    public void Invoke_calls_exact_transform_task_once_with_live_closure_state()
    {
        FakeUtility.Reset();
        var netherModel = new FakeNetherModel();
        var resultModel = new FakeEventResultModel();
        var completed = new FakeBoolReactiveProperty();
        var token = new FakeCancellationToken(73);
        var controller = new FakeListController();
        var closure = new FakeUtility.DisplayClosure(controller, netherModel, resultModel, completed, token);
        controller.Bind(closure.OnClickChange);

        bool invoked = NetherCodeTransformTaskInvoker.TryInvoke(
            controller,
            typeof(FakeUtility),
            30012,
            BindingFor<FakeListController>(),
            out object? task,
            out string error
        );

        Assert.True(invoked, error);
        Assert.Same(FakeUtility.ReturnedTask, task);
        Assert.Equal(1, FakeUtility.CallCount);
        Assert.Same(controller, FakeUtility.Controller);
        Assert.Same(netherModel, FakeUtility.NetherModel);
        Assert.Same(resultModel, FakeUtility.ResultModel);
        Assert.Equal(30012, FakeUtility.BeforeCodeId);
        Assert.Same(completed, FakeUtility.CompletedProperty);
        Assert.Equal(token, FakeUtility.Token);
    }

    [Fact]
    public void Invoke_fails_before_native_call_when_closure_controller_mismatches_or_action_is_null()
    {
        FakeUtility.Reset();
        var uninitialized = new FakeListController();

        Assert.False(NetherCodeTransformTaskInvoker.TryInvoke(
            uninitialized,
            typeof(FakeUtility),
            30012,
            BindingFor<FakeListController>(),
            out object? nullActionTask,
            out string nullActionError
        ));
        Assert.Null(nullActionTask);
        Assert.Equal(0, FakeUtility.CallCount);
        Assert.Contains("null-change-action", nullActionError, StringComparison.Ordinal);

        var otherController = new FakeListController();
        var closure = new FakeUtility.DisplayClosure(
            otherController,
            new FakeNetherModel(),
            new FakeEventResultModel(),
            new FakeBoolReactiveProperty(),
            new FakeCancellationToken(11)
        );
        var activeController = new FakeListController();
        activeController.Bind(closure.OnClickChange);

        Assert.False(NetherCodeTransformTaskInvoker.TryInvoke(
            activeController,
            typeof(FakeUtility),
            30012,
            BindingFor<FakeListController>(),
            out object? mismatchTask,
            out string mismatchError
        ));
        Assert.Null(mismatchTask);
        Assert.Equal(0, FakeUtility.CallCount);
        Assert.Contains("closure-controller-mismatch", mismatchError, StringComparison.Ordinal);
    }

    [Fact]
    public void Packaged_project_assembly_resolves_transform_task_and_closure_members_uniquely()
    {
        if (!Directory.Exists("/game/BepInEx/interop"))
            return;

        using var packaged = PackagedProjectAssembly.Load();
        Type controllerType = packaged.RequireType(NetherNativeBindingCatalog.CodeListControllerType);
        Type utilityType = packaged.RequireType(NetherNativeBindingCatalog.NetherUtilityType);

        bool resolved = NetherCodeTransformTaskInvoker.TryResolve(
            controllerType,
            utilityType,
            NetherCodeTransformNativeBinding.TransformTaskBinding(NetherNativeBindingCatalog.CodeListControllerType),
            out string error,
            out MethodInfo? taskMethod,
            out MemberInfo? changeActionMember,
            out PropertyInfo? targetProperty,
            out Type? closureType,
            out MemberInfo? closureControllerMember,
            out MemberInfo? netherModelMember,
            out MemberInfo? resultModelMember,
            out MemberInfo? completedPropertyMember,
            out MemberInfo? cancellationTokenMember
        );

        Assert.True(resolved, error);
        Assert.NotNull(taskMethod);
        Assert.Equal("Cysharp.Threading.Tasks.UniTask", taskMethod!.ReturnType.FullName);
        Assert.Equal("_onChangeButtonClicked", changeActionMember!.Name);
        Assert.Equal("Target", targetProperty!.Name);
        Assert.Equal("Il2CppSystem.Object", closureType!.BaseType?.FullName);
        Assert.Equal("controller", closureControllerMember!.Name);
        Assert.Equal("netherModel", netherModelMember!.Name);
        Assert.Equal("resultModel", resultModelMember!.Name);
        Assert.Equal("isCompletedRP", completedPropertyMember!.Name);
        Assert.Equal("ct", cancellationTokenMember!.Name);
    }

    private static NetherCodePopupInteropMethodBinding BindingFor<TController>() => new(
        "transform-shape",
        null,
        new[]
        {
            typeof(TController).FullName!,
            typeof(FakeNetherModel).FullName!,
            typeof(FakeEventResultModel).FullName!,
            "System.Int64",
            typeof(FakeBoolReactiveProperty).FullName!,
            typeof(FakeCancellationToken).FullName!,
        },
        typeof(FakeUniTask).FullName!
    ) { IsStatic = true, SignatureOnly = true };

    private sealed class FakeListController
    {
        private Action<long>? _onChangeButtonClicked;

        public void Bind(Action<long> callback) => _onChangeButtonClicked = callback;
    }

    private sealed class FakeNetherModel;

    private sealed class FakeEventResultModel;

    private sealed class FakeBoolReactiveProperty;

    private readonly record struct FakeCancellationToken(int Value);

    private sealed class FakeUniTask;

    private static class FakeUtility
    {
        public sealed class DisplayClosure
        {
            public FakeListController controller { get; }
            public FakeNetherModel netherModel { get; }
            public FakeEventResultModel resultModel { get; }
            public FakeBoolReactiveProperty isCompletedRP { get; }
            public FakeCancellationToken ct { get; }

            public DisplayClosure(
                FakeListController controller,
                FakeNetherModel netherModel,
                FakeEventResultModel resultModel,
                FakeBoolReactiveProperty isCompletedRP,
                FakeCancellationToken ct
            )
            {
                this.controller = controller;
                this.netherModel = netherModel;
                this.resultModel = resultModel;
                this.isCompletedRP = isCompletedRP;
                this.ct = ct;
            }

            public void OnClickChange(long beforeCodeId)
            {
            }
        }

        public static readonly FakeUniTask ReturnedTask = new();
        public static int CallCount { get; private set; }
        public static FakeListController? Controller { get; private set; }
        public static FakeNetherModel? NetherModel { get; private set; }
        public static FakeEventResultModel? ResultModel { get; private set; }
        public static long BeforeCodeId { get; private set; }
        public static FakeBoolReactiveProperty? CompletedProperty { get; private set; }
        public static FakeCancellationToken Token { get; private set; }

        public static FakeUniTask Transform(
            FakeListController controller,
            FakeNetherModel netherModel,
            FakeEventResultModel resultModel,
            long beforeMNetherCodeId,
            FakeBoolReactiveProperty isCompletedRP,
            FakeCancellationToken ct
        )
        {
            CallCount++;
            Controller = controller;
            NetherModel = netherModel;
            ResultModel = resultModel;
            BeforeCodeId = beforeMNetherCodeId;
            CompletedProperty = isCompletedRP;
            Token = ct;
            return ReturnedTask;
        }

        public static void Reset()
        {
            CallCount = 0;
            Controller = null;
            NetherModel = null;
            ResultModel = null;
            BeforeCodeId = 0;
            CompletedProperty = null;
            Token = default;
        }
    }

    private sealed class PackagedProjectAssembly : IDisposable
    {
        private readonly AssemblyLoadContext _context;

        private PackagedProjectAssembly(AssemblyLoadContext context) => _context = context;

        public Assembly Assembly => _context.Assemblies.Single(assembly => assembly.GetName().Name == "Project");

        public Type RequireType(string name) => Assembly.GetType(name, throwOnError: true)!;

        public static PackagedProjectAssembly Load()
        {
            const string interopDirectory = "/game/BepInEx/interop";
            const string coreDirectory = "/game/BepInEx/core";

            var context = new AssemblyLoadContext("transform-invoker", isCollectible: true);
            context.Resolving += (_, name) =>
            {
                string candidate = Path.Combine(interopDirectory, name.Name + ".dll");
                if (File.Exists(candidate))
                    return context.LoadFromAssemblyPath(candidate);
                candidate = Path.Combine(coreDirectory, name.Name + ".dll");
                return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
            };
            context.LoadFromAssemblyPath(Path.Combine(interopDirectory, "Project.dll"));
            return new PackagedProjectAssembly(context);
        }

        public void Dispose() => _context.Unload();
    }
}
