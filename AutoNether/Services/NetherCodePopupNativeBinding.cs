#nullable enable

namespace AutoNether.Services;

/// <summary>
/// Version-characterized generated callbacks for the normal Abyss code offer popup.  ISIL for
/// the packaged client proves b__12_0 invokes _onCancel while b__12_2 invokes _onConfirm; keep
/// the identifiers here so tests can distinguish the two otherwise adjacent lambdas.  These are
/// popup button callbacks the plugin clicks, not sequence factories: the confirm and cancel
/// UniTask factories are resolved by shape only, and their state machines by source name.
/// </summary>
internal static class NetherCodePopupNativeBinding
{
    private const string UnitTypeName = "UniRx.Unit";
    private const string PopupTypeName =
        "Project.Nether.AbyssCodeSelectPopup.AbyssCodeSelectPopup";
    private const string PartyModelTypeName = "Project.Nether.NetherPartyModel";
    private const string Il2CppCancellationTokenTypeName = "Il2CppSystem.Threading.CancellationToken";
    private const string UniTaskTypeName = "Cysharp.Threading.Tasks.UniTask";

    // These are the exact managed member names in the current packaged BepInEx interop.  The
    // cpp2il source names are retained only as exact ObfuscatedNameAttribute contracts, never
    // as a fuzzy or arity-only fallback.
    public const string CancelCallback = "_SetupPopupEvent_b__12_0";
    public const string DetailCallback = "_SetupPopupEvent_b__12_3";
    public const string ConfirmCallback = "_SetupPopupEvent_b__12_2";
    public const string CancelCallbackObfuscatedName = "<SetupPopupEvent>b__12_0";
    public const string DetailCallbackObfuscatedName = "<SetupPopupEvent>b__12_3";
    public const string ConfirmCallbackObfuscatedName = "<SetupPopupEvent>b__12_2";

    // The generated sequence factories embed an unstable interop suffix in their managed names.
    // They are identified only by their complete static signature; the state machines they
    // start are resolved by source local-function name (NetherGeneratedSequenceResolver).
    public const string ConfirmTaskShape = "code-confirm-generated-sequence";
    public const string CancelTaskShape = "code-cancel-generated-sequence";

    public static NetherNativeMethodDescriptor ConfirmDescriptor(string controllerTypeName) => new(
        ConfirmCallback,
        new[] { UnitTypeName, controllerTypeName },
        "System.Void"
    ) { IsStatic = false };

    public static NetherNativeMethodDescriptor CancelDescriptor(string controllerTypeName) => new(
        CancelCallback,
        new[] { UnitTypeName, controllerTypeName },
        "System.Void"
    ) { IsStatic = false };

    public static NetherCodePopupInteropMethodBinding ConfirmCallbackBinding(string controllerTypeName) => new(
        ConfirmCallback,
        ConfirmCallbackObfuscatedName,
        new[] { UnitTypeName, controllerTypeName },
        "System.Void"
    ) { IsStatic = false };

    public static NetherCodePopupInteropMethodBinding CancelCallbackBinding(string controllerTypeName) => new(
        CancelCallback,
        CancelCallbackObfuscatedName,
        new[] { UnitTypeName, controllerTypeName },
        "System.Void"
    ) { IsStatic = false };

    public static NetherCodePopupInteropMethodBinding DetailCallbackBinding(string controllerTypeName) => new(
        DetailCallback,
        DetailCallbackObfuscatedName,
        new[] { "System.Int32", controllerTypeName, PopupTypeName },
        "System.Void"
    ) { IsStatic = false };

    public static NetherCodePopupInteropMethodBinding ConfirmTaskBinding(string controllerTypeName) => new(
        ConfirmTaskShape,
        null,
        new[]
        {
            controllerTypeName,
            "System.Int64",
            PartyModelTypeName,
            "System.Boolean",
            Il2CppCancellationTokenTypeName,
        },
        UniTaskTypeName
    ) { IsStatic = true, SignatureOnly = true };

    public static NetherCodePopupInteropMethodBinding CancelTaskBinding(string controllerTypeName) => new(
        CancelTaskShape,
        null,
        new[] { controllerTypeName, Il2CppCancellationTokenTypeName },
        UniTaskTypeName
    ) { IsStatic = true, SignatureOnly = true };

    /// <summary>
    /// The cancel callback itself returns void because the packaged closure calls Forget.
    /// AutoNether invokes this exact static generated UniTask factory directly and retains its
    /// return value; its ISIL awaits RequestNetherFixCodeAsync(0, 0), so popup disappearance is
    /// never treated as a completed Keep action.
    /// </summary>
    public static NetherNativeMethodDescriptor CancelSequenceDescriptor(string controllerTypeName) => new(
        CancelTaskShape,
        new[] { controllerTypeName, Il2CppCancellationTokenTypeName },
        UniTaskTypeName
    ) { IsStatic = true };
}
