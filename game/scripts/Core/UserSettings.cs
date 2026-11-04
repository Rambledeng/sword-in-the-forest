using System;
using Godot;

namespace SwordInTheForest.Core;

/// <summary>
/// The player's own settings, kept in user://settings.cfg beside the director's: language and the
/// master, music and effects volumes, the display mode and window size, whether the first ending was reached and Detail mode. First launch follows the operating system's language
/// (Dutch, Chinese, otherwise English). Nothing is read or written in scenario and screenshot runs.
/// </summary>
public static class UserSettings
{
    public static readonly string[] Locales = { "en", "nl", "zh_CN" };

    /// <summary>Each language's name in itself, for the picker (never translated).</summary>
    public static string LocaleName(string locale) => locale switch
    {
        "nl" => "Nederlands",
        "zh_CN" => "简体中文",
        _ => "English",
    };

    public static bool Persist { get; set; }
    public static float Master { get; private set; } = 1f;
    public static float Music { get; private set; } = 1f;
    public static float Effects { get; private set; } = 1f;
    public static string Locale => Normalize(TranslationServer.GetLocale());
    /// <summary>The boss has been beaten once (the first ending): Detail mode can be turned on.</summary>
    public static bool EndingSeen { get; private set; }
    /// <summary>The player's choice; only counts once <see cref="EndingSeen"/>.</summary>
    public static bool DetailMode { get; private set; }
    /// <summary>Show merit, karma, reaction times and the attunement meter.</summary>
    public static bool ShowDetails => EndingSeen && DetailMode;

    /// <summary>Windowed, borderless fullscreen or fullscreen.</summary>
    public static DisplayMode Display { get; private set; } = DisplayMode.Windowed;
    /// <summary>The windowed size as a multiple of the 480×270 viewport (1 to <see cref="MaxScale"/>).</summary>
    public static int Scale { get; private set; } = 3;
    public const int BaseWidth = 480, BaseHeight = 270;

    public static event Action? Changed;

    /// <summary>One of <see cref="Locales"/> for any locale code.</summary>
    public static string Normalize(string locale)
    {
        if (locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh_CN";
        if (locale.StartsWith("nl", StringComparison.OrdinalIgnoreCase)) return "nl";
        return "en";
    }

    public static void Load(string path, bool applyLocale)
    {
        var cfg = new ConfigFile();
        var loaded = cfg.Load(path) == Error.Ok;
        Master = (float)(double)cfg.GetValue("audio", "master", 1.0);
        Music = (float)(double)cfg.GetValue("audio", "music", 1.0);
        Effects = (float)(double)cfg.GetValue("audio", "effects", 1.0);
        EndingSeen = (bool)cfg.GetValue("progress", "ending_seen", false);
        DetailMode = (bool)cfg.GetValue("ui", "detail_mode", false);
        var mode = (int)cfg.GetValue("display", "mode", (int)DisplayMode.Windowed);
        Display = Enum.IsDefined(typeof(DisplayMode), mode) ? (DisplayMode)mode : DisplayMode.Windowed;
        Scale = Mathf.Clamp((int)cfg.GetValue("display", "scale", DefaultScale()), 1, MaxScale());
        ApplyDisplay();
        if (applyLocale)
        {
            var saved = loaded ? (string)cfg.GetValue("ui", "locale", "") : "";
            TranslationServer.SetLocale(saved != "" ? Normalize(saved) : Normalize(OS.GetLocaleLanguage()));
        }
        ApplyAudio();
    }

    public static void SetLocale(string locale)
    {
        TranslationServer.SetLocale(Normalize(locale));
        Save();
    }

    /// <summary>The next language in <see cref="Locales"/> (step +1 or -1).</summary>
    public static void CycleLocale(int step)
    {
        var i = Array.IndexOf(Locales, Locale);
        SetLocale(Locales[(i + step + Locales.Length) % Locales.Length]);
    }

    public static void SetVolumes(float? master = null, float? music = null, float? effects = null)
    {
        Master = Mathf.Clamp(master ?? Master, 0f, 1f);
        Music = Mathf.Clamp(music ?? Music, 0f, 1f);
        Effects = Mathf.Clamp(effects ?? Effects, 0f, 1f);
        ApplyAudio();
        Save();
    }

    /// <summary>The first ending: unlocks Detail mode (saved).</summary>
    public static void MarkEndingSeen()
    {
        if (EndingSeen) return;
        EndingSeen = true;
        Save();
    }

    public static void SetDetailMode(bool on)
    {
        DetailMode = on && EndingSeen;
        Save();
    }

    /// <summary>Scenarios set the hidden-values state directly (nothing is saved).</summary>
    public static void SetProgressForTest(bool endingSeen, bool detailMode)
    {
        EndingSeen = endingSeen;
        DetailMode = detailMode && endingSeen;
        Changed?.Invoke();
    }

    /// <summary>The largest multiple of 480×270 whose window fits the screen (4 when there is no screen).</summary>
    public static int MaxScale()
    {
        if (Headless) return 4;
        var usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen()).Size;
        // Leave room for the title bar in windowed mode.
        var k = Mathf.Min(usable.X / BaseWidth, (usable.Y - 32) / BaseHeight);
        return Mathf.Max(1, k);
    }

    /// <summary>The project's own window size (1440×810, ×3) if it fits, else the largest that does.</summary>
    private static int DefaultScale() => Mathf.Min(3, MaxScale());

    public static void SetDisplay(DisplayMode mode)
    {
        Display = mode;
        ApplyDisplay();
        Save();
    }

    /// <summary>The next display mode (step +1 or -1).</summary>
    public static void CycleDisplay(int step) => SetDisplay((DisplayMode)Mathf.PosMod((int)Display + step, 3));

    /// <summary>The next window size that fits the screen (step +1 or -1, wrapping).</summary>
    public static void CycleScale(int step)
    {
        var max = MaxScale();
        Scale = Mathf.PosMod(Mathf.Clamp(Scale, 1, max) - 1 + step, max) + 1;
        ApplyDisplay();
        Save();
    }

    private static bool Headless => DisplayServer.GetName() == "headless";

    /// <summary>Sets the window. Scenario and screenshot runs leave the window alone.</summary>
    private static void ApplyDisplay()
    {
        if (!Persist || Headless) return;
        switch (Display)
        {
            case DisplayMode.Borderless:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                break;
            case DisplayMode.Fullscreen:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
                break;
            default:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                var size = new Vector2I(BaseWidth, BaseHeight) * Mathf.Clamp(Scale, 1, MaxScale());
                DisplayServer.WindowSetSize(size);
                var screen = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
                DisplayServer.WindowSetPosition(screen.Position + (screen.Size - size) / 2);
                break;
        }
    }

    private static void ApplyAudio() => AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(Mathf.Max(0.0001f, Master)));

    private static void Save()
    {
        Changed?.Invoke();
        if (!Persist) return;
        var cfg = new ConfigFile();
        cfg.Load(DirectorService.SettingsPath);
        cfg.SetValue("ui", "locale", Locale);
        cfg.SetValue("audio", "master", (double)Master);
        cfg.SetValue("audio", "music", (double)Music);
        cfg.SetValue("audio", "effects", (double)Effects);
        cfg.SetValue("display", "mode", (int)Display);
        cfg.SetValue("display", "scale", Scale);
        cfg.SetValue("progress", "ending_seen", EndingSeen);
        cfg.SetValue("ui", "detail_mode", DetailMode);
        cfg.Save(DirectorService.SettingsPath);
    }
}
