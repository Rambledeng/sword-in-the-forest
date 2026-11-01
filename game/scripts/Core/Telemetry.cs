using System;
using System.Collections.Generic;
using Godot;
using SwordInTheForest.Combat;

namespace SwordInTheForest.Core;

/// <summary>
/// Autoload "Telemetry": one JSON line per attack aimed at the player, appended to
/// user://telemetry/&lt;run&gt;.jsonl. Nothing is ever sent anywhere.
/// </summary>
public partial class Telemetry : Node
{
    public const string Folder = "user://telemetry";
    private FileAccess? _file;
    private bool _created;

    public static Telemetry Instance { get; private set; } = null!;

    public string RunId { get; private set; } = "";
    public double RunStartMs { get; private set; }
    public string FilePath => $"{Folder}/{RunId}.jsonl";
    /// <summary>Lines written this run, newest last (kept for scenarios and the F2 overlay).</summary>
    public List<string> Lines { get; } = new();

    public override void _EnterTree() => Instance = this;

    public override void _Ready() => StartRun();

    /// <summary>Starts a new run id; later lines go to a new file.</summary>
    public void StartRun()
    {
        _file?.Close();
        _file = null;
        _created = false;
        Lines.Clear();
        RunId = "r" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        RunStartMs = GameClock.Instance?.NowMs ?? 0;
    }

    public void Write(ThreatWindow threat)
    {
        var line = TelemetryJson.Format(TelemetryRecord.From(threat, RunId, RunStartMs));
        Lines.Add(line);
        if (_file == null)
        {
            DirAccess.MakeDirRecursiveAbsolute(Folder);
            // Append if this run's file exists already (e.g. a line flushed after _ExitTree at quit).
            _file = _created
                ? FileAccess.Open(FilePath, FileAccess.ModeFlags.ReadWrite)
                : FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
            if (_file == null)
            {
                GD.PushWarning($"Telemetry: cannot open {FilePath}: {FileAccess.GetOpenError()}");
                return;
            }
            _created = true;
            _file.SeekEnd();
        }
        _file.StoreLine(line);
        _file.Flush();
    }

    public override void _ExitTree()
    {
        _file?.Close();
        _file = null;
    }
}
