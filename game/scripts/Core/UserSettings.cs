using System;
using Godot;

namespace SwordInTheForest.Core;

/// <summary>
/// The player's own settings, kept in user://settings.cfg beside the director's: language and the
/// master, music and effects volumes, whether the first ending was reached and Detail mode. First launch follows the operating system's language
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
        cfg.SetValue("progress", "ending_seen", EndingSeen);
        cfg.SetValue("ui", "detail_mode", DetailMode);
        cfg.Save(DirectorService.SettingsPath);
    }
}
