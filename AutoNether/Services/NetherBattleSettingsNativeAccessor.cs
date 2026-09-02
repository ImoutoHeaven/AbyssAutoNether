#nullable enable

using System;
using System.Linq;
using System.Reflection;

namespace AutoNether.Services;

/// <summary>
/// Exact packaged-game binding for exploration/Nether battle settings.  The game exposes this
/// surface through Project.Ingame.IIngameUserSettings and supplies it to BottomRightView via
/// InitializeTimeScaleButtons/ApplyUserSettings; no property-name fallback or guessed component
/// lookup is used.
/// </summary>
internal sealed class NetherBattleSettingsNativeAccessor : INetherBattleSettingsNative
{
    private const string SettingsInterfaceTypeName = "Project.Ingame.IIngameUserSettings";
    private readonly object _settings;
    private readonly MethodInfo _getAuto;
    private readonly MethodInfo _setAuto;
    private readonly MethodInfo _getSpeed;
    private readonly MethodInfo _setSpeed;
    private readonly Type _speedType;

    private NetherBattleSettingsNativeAccessor(
        object settings,
        MethodInfo getAuto,
        MethodInfo setAuto,
        MethodInfo getSpeed,
        MethodInfo setSpeed,
        Type speedType
    )
    {
        _settings = settings;
        _getAuto = getAuto;
        _setAuto = setAuto;
        _getSpeed = getSpeed;
        _setSpeed = setSpeed;
        _speedType = speedType;
    }

    public static bool TryCreate(object settings, out NetherBattleSettingsNativeAccessor? accessor, out string error)
    {
        accessor = null;
        error = string.Empty;
        if (settings == null)
        {
            error = "missing-native-ingame-user-settings";
            return false;
        }

        Type concrete = settings.GetType();
        // IL2CPP interop projects a native interface as a CLR wrapper class, so an argument
        // declared IIngameUserSettings normally arrives with that exact concrete FullName and
        // an empty CLR GetInterfaces() result.  Accept that exact wrapper as well as any future
        // generated concrete type which genuinely exposes the same CLR contract; the four
        // accessor methods below still have to match exactly before mutation is possible.
        bool isExpectedWrapper = string.Equals(
            concrete.FullName,
            SettingsInterfaceTypeName,
            StringComparison.Ordinal
        );
        bool implementsExpectedContract = concrete.GetInterfaces().Any(type =>
            string.Equals(type.FullName, SettingsInterfaceTypeName, StringComparison.Ordinal)
        );
        if (!isExpectedWrapper && !implementsExpectedContract)
        {
            error = "unexpected-native-settings-interface:" + concrete.FullName;
            return false;
        }

        if (!NetherLifecycleInteropBindings.TryResolveExactMethod(
                concrete,
                NetherNativeBindingCatalog.BattleSettingsGetAuto.Method,
                Flags,
                out error,
                out MethodInfo? getAuto
            )
            || !NetherLifecycleInteropBindings.TryResolveExactMethod(
                concrete,
                NetherNativeBindingCatalog.BattleSettingsSetAuto.Method,
                Flags,
                out error,
                out MethodInfo? setAuto
            )
            || !NetherLifecycleInteropBindings.TryResolveExactMethod(
                concrete,
                NetherNativeBindingCatalog.BattleSettingsGetSpeed.Method,
                Flags,
                out error,
                out MethodInfo? getSpeed
            )
            || !NetherLifecycleInteropBindings.TryResolveExactMethod(
                concrete,
                NetherNativeBindingCatalog.BattleSettingsSetSpeed.Method,
                Flags,
                out error,
                out MethodInfo? setSpeed
            ))
        {
            return false;
        }

        accessor = new NetherBattleSettingsNativeAccessor(
            settings,
            getAuto!,
            setAuto!,
            getSpeed!,
            setSpeed!,
            getSpeed!.ReturnType
        );
        return true;
    }

    public bool TryRead(out bool autoEnabled, out int speed, out string error)
    {
        autoEnabled = false;
        speed = 0;
        error = string.Empty;
        try
        {
            object? rawAuto = _getAuto.Invoke(_settings, Array.Empty<object>());
            object? rawSpeed = _getSpeed.Invoke(_settings, Array.Empty<object>());
            if (rawAuto == null || rawSpeed == null)
            {
                error = "native-settings-read-null";
                return false;
            }
            autoEnabled = Convert.ToBoolean(rawAuto);
            speed = Convert.ToInt32(rawSpeed);
            if (speed is < 0 or > 3)
            {
                error = "native-settings-speed-out-of-range:" + speed;
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ":" + ex.Message;
            return false;
        }
    }

    public bool TryForceAutoAndHighestSpeed(out string error) => TryWrite(true, 3, out error);

    public bool TryWrite(bool autoEnabled, int speed, out string error)
    {
        error = string.Empty;
        if (speed is < 0 or > 3)
        {
            error = "invalid-native-settings-speed:" + speed;
            return false;
        }
        try
        {
            _setAuto.Invoke(_settings, new object[] { autoEnabled });
            object speedValue = Enum.ToObject(_speedType, speed);
            _setSpeed.Invoke(_settings, new[] { speedValue });
            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ":" + ex.Message;
            return false;
        }
    }

    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}

/// <summary>Owns the exact BottomRightView registration lifetime for the settings accessor.</summary>
internal static class NetherBattleSettingsNativeRegistry
{
    private static readonly object Gate = new();
    private static object? _owner;
    private static NetherBattleSettingsNativeAccessor? _accessor;

    public static void Register(object owner, object settings)
    {
        string error = string.Empty;
        if (owner == null || !NetherBattleSettingsNativeAccessor.TryCreate(settings, out NetherBattleSettingsNativeAccessor? next, out error))
        {
            Logger.Error("[F12][AutoNether] native battle settings accessor unavailable: " + error);
            return;
        }

        lock (Gate)
        {
            if (_accessor != null)
                NetherBattleSettingsLease.UnregisterNativeAccessor(_accessor);
            _owner = owner;
            _accessor = next;
            NetherBattleSettingsLease.RegisterNativeAccessor(next!);
        }
        // Recovery belongs to the controller lifecycle, not accessor registration itself.  The
        // callback runs after the exact native object is stored and can therefore defer/retry
        // with the persisted lease phase as its authority.
        NetherAutoClimbController.OnBattleSettingsAccessorRegistered();
    }

    public static void Unregister(object owner)
    {
        if (owner == null)
            return;
        bool unregistered = false;
        lock (Gate)
        {
            if (!ReferenceEquals(_owner, owner) || _accessor == null)
                return;
            NetherBattleSettingsLease.UnregisterNativeAccessor(_accessor);
            _accessor = null;
            _owner = null;
            unregistered = true;
        }
        if (unregistered)
            NetherAutoClimbController.OnBattleSettingsAccessorUnregistered();
    }

    /// <summary>
    /// Prefix half of BottomRightView.OnDestroy. Owner identity prevents a stale view from
    /// restoring a lease owned by a newer battle view.
    /// </summary>
    public static void PrepareForUnregister(object owner)
    {
        if (owner == null)
            return;
        lock (Gate)
        {
            if (!ReferenceEquals(_owner, owner) || _accessor == null)
                return;
        }
        NetherAutoClimbController.OnBattleSettingsAccessorDestroying();
    }
}
