using System;
using System.Linq;
using Godot;
using SwordInTheForest.Combat;
using SwordInTheForest.Director;

namespace SwordInTheForest.Core;

/// <summary>
/// Autoload "Director": owns the adaptive director, feeds it every finished attack, and keeps the
/// belief (user://director.sav) and the settings (user://settings.cfg) across runs. Scenario and
/// screenshot runs start from the prior with adaptive timing off and never touch the user files.
/// </summary>
public partial class DirectorService : Node
{
    public const string SavePath = "user://director.sav";
    public const string SettingsPath = "user://settings.cfg";

    private double[] _prior = Array.Empty<double>();

    public static DirectorService? Instance { get; private set; }

    public AdaptiveLeadTimes Leads { get; private set; } = null!;
    public ReactionDirector Director => Leads.Director;
    /// <summary>False in scenario and screenshot runs: nothing is loaded or saved.</summary>
    public bool Persist { get; private set; }

    /// <summary>Raised after every observation (the overlay and HUD redraw on their own; scenarios listen).</summary>
    public event Action? Observed;

    public override void _EnterTree()
    {
        Instance = this;
        var grid = DirectorGrid.Default;
        _prior = Prior.FromPopulation(grid, PopulationModel.Default);
        Leads = new AdaptiveLeadTimes(ReactionDirector.FromPrior(grid, _prior));
        var args = OS.GetCmdlineUserArgs();
        Persist = !args.Contains("--scenario") && !args.Contains("--screenshot");
        UserSettings.Persist = Persist;
        if (Persist)
        {
            UserSettings.Load(SettingsPath, applyLocale: !args.Contains("--locale"));
            LoadSettings();
            LoadBelief();
        }
        else
        {
            Leads.Adaptive = false;
        }
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Feeds one finished attack (its telemetry line is complete) to the director.</summary>
    public void Observe(ThreatWindow threat)
    {
        var record = TelemetryRecord.From(threat, "", 0);
        if (!Leads.Observe(record, Tunables.Player.GuardArcDeg)) return;
        if (Persist) SaveBelief();
        Observed?.Invoke();
    }

    /// <summary>Wandering souls fallen across every run (仇), kept with the settings.</summary>
    public int SoulsFallen { get; private set; }

    public void AddSoulFallen()
    {
        SoulsFallen++;
        SaveSettings();
    }

    public void SetAdaptive(bool on)
    {
        Leads.Adaptive = on;
        SaveSettings();
    }

    public void SetChallenge(Challenge c)
    {
        Leads.Challenge = c;
        SaveSettings();
    }

    /// <summary>Forgets the player: back to the population prior.</summary>
    public void ResetBelief()
    {
        Leads.Director = ReactionDirector.FromPrior(Director.Grid, _prior);
        if (Persist) SaveBelief();
        Observed?.Invoke();
    }

    private void LoadSettings()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) != Error.Ok) return;
        Leads.Adaptive = (bool)cfg.GetValue("director", "adaptive", true);
        var c = (int)cfg.GetValue("director", "challenge", (int)Challenge.Standard);
        Leads.Challenge = Enum.IsDefined(typeof(Challenge), c) ? (Challenge)c : Challenge.Standard;
        SoulsFallen = (int)cfg.GetValue("records", "souls_fallen", 0);
    }

    private void SaveSettings()
    {
        if (!Persist) return;
        var cfg = new ConfigFile();
        cfg.Load(SettingsPath);
        cfg.SetValue("director", "adaptive", Leads.Adaptive);
        cfg.SetValue("director", "challenge", (int)Leads.Challenge);
        cfg.SetValue("records", "souls_fallen", SoulsFallen);
        cfg.Save(SettingsPath);
    }

    private void LoadBelief()
    {
        if (!FileAccess.FileExists(SavePath)) return;
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (f == null) return;
        var loaded = DirectorSave.FromText(Director.Grid, f.GetAsText());
        if (loaded != null) Leads.Director = loaded;
        else GD.PushWarning($"Director: {SavePath} does not match this grid; starting from the prior.");
    }

    private void SaveBelief()
    {
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PushWarning($"Director: cannot write {SavePath}: {FileAccess.GetOpenError()}");
            return;
        }
        f.StoreString(DirectorSave.ToText(Director));
    }
}
