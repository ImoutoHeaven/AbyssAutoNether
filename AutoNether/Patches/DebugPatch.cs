#if DEBUG
using HarmonyLib;

namespace AutoNether.Patches;

/// <summary>
/// 调试补丁
/// </summary>
[HarmonyPatch]
public static class DebugPatch { }
#endif
