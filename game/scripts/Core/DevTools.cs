using System;
using Godot;

namespace SwordInTheForest.Core;

/// <summary>
/// Whether the developer tools (F1 tuning, F2 director overlay, F3 developer menu) answer.
/// On in the editor and debug builds (so scenarios and screenshots keep them); off in a release
/// export unless the game is started with <c>-- --dev</c> or the environment has <c>SWORD_DEV=1</c>.
/// </summary>
public static class DevTools
{
    private static bool? _enabled;

    public static bool Enabled
    {
        get => _enabled ??= OS.IsDebugBuild()
                            || Array.IndexOf(OS.GetCmdlineUserArgs(), "--dev") >= 0
                            || OS.GetEnvironment("SWORD_DEV") == "1";
        // Tests only: act as a release build (false) or a debug one (true).
        set => _enabled = value;
    }
}
