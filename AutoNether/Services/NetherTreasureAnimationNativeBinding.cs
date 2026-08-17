#nullable enable

namespace AutoNether.Services;

/// <summary>
/// Exact current-game contracts used to prove that the Treasure popup has entered its native
/// Open animation before SkipOpenTreasureAnimationAsync may be invoked. Fresh Cpp2IL recovery
/// shows that HandleEventConfirmedAsync first awaits RequestNetherUpdateEventAsync and only then
/// starts PlayOpenTreasureAnimationSequenceAsync; dispatching OnConfirm is therefore not itself
/// animation-readiness evidence.
/// </summary>
internal static class NetherTreasureAnimationNativeBinding
{
    public const string PopupTypeName =
        "Project.Nether.NetherTreasurePopup.NetherTreasurePopup";
    public const string AnimatorExtensionsTypeName = "Project.AnimatorExtensions";
    public const string AnimatorMemberName = "_animator";
    public const string OpenAnimationMemberName = "OpenAnim";
    public const string AnimationHashMemberName = "Hash";

    public static NetherNativeMethodDescriptor IsOpenAnimationDescriptor { get; } = new(
        "IsState",
        new[] { "UnityEngine.Animator", "System.Int32", "System.Int32" },
        "System.Boolean"
    );

    public static NetherNativeMethodDescriptor SkipAnimationDescriptor { get; } = new(
        "SkipOpenTreasureAnimationAsync",
        new[] { "Il2CppSystem.Threading.CancellationToken" },
        "Cysharp.Threading.Tasks.UniTask"
    );
}
