#nullable enable

namespace AutoNether.Services;

/// <summary>
/// Exact current-game contracts used to prove that the Treasure popup reached Open and that the
/// controller added its awaited SkipAndConfirmButton OnTap listener. Fresh Cpp2IL recovery shows
/// that UniRx adds this through the button UnityEvent runtime-call list; automation must not
/// reflectively invoke the popup's direct SkipOpenTreasureAnimationAsync entry point.
/// </summary>
internal static class NetherTreasureAnimationNativeBinding
{
    public const string PopupTypeName =
        "Project.Nether.NetherTreasurePopup.NetherTreasurePopup";
    public const string AnimatorExtensionsTypeName = "Project.AnimatorExtensions";
    public const string AnimatorMemberName = "_animator";
    public const string OpenAnimationMemberName = "OpenAnim";
    public const string AnimationHashMemberName = "Hash";
    public const string SkipAndConfirmButtonMemberName = "SkipAndConfirmButton";
    public const string NativeButtonTypeName = "Project.AppButton";
    public const string NativeButtonOnClickMemberName = "onClick";
    public const string UnityEventCallsMemberName = "m_Calls";
    public const string UnityEventRuntimeCallsMemberName = "m_RuntimeCalls";
    public const string CollectionCountMemberName = "Count";

    public static NetherNativeMethodDescriptor IsOpenAnimationDescriptor { get; } = new(
        "IsState",
        new[] { "UnityEngine.Animator", "System.Int32", "System.Int32" },
        "System.Boolean"
    );

    public static NetherNativeMethodDescriptor NativeButtonSubmitDescriptor { get; } = new(
        "OnSubmit",
        new[] { "UnityEngine.EventSystems.BaseEventData" },
        "System.Void"
    );
}
