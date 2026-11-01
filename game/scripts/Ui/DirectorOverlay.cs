using System;
using System.Linq;
using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Director;

namespace SwordInTheForest.Ui;

/// <summary>
/// F2: what the director believes. A developer tool like the F1 panel, so its text is not localized.
/// Heat map: the belief over the median (x, 150–900 ms, log scale) and the spread s (y, low at the
/// top), with the 80% interval and the estimate below it. Reliability plot: predicted against
/// observed success, in five bins of prediction. Backspace (while open) resets the belief.
/// </summary>
public partial class DirectorOverlay : CanvasLayer
{
    private const float PanelW = 236;
    private const float PanelH = 152;
    private const float CellW = 3;
    private const float CellH = 3;
    private const int Recent = 20;
    private Control _canvas = null!;
    private double[] _heat = Array.Empty<double>();

    public bool IsOpen => _canvas.Visible;

    public override void _Ready()
    {
        Layer = 15;
        ProcessMode = ProcessModeEnum.Always;
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        _canvas.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _canvas.Draw += DrawOverlay;
        AddChild(_canvas);
    }

    public void Toggle() => _canvas.Visible = !_canvas.Visible;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("debug_overlay"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
        else if (IsOpen && e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Backspace })
        {
            DirectorService.Instance?.ResetBelief();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (IsOpen) _canvas.QueueRedraw();
    }

    private void DrawOverlay()
    {
        var service = DirectorService.Instance;
        if (service == null) return;
        var d = service.Director;
        var leads = service.Leads;
        var grid = d.Grid;
        var est = d.Estimate();
        var font = _canvas.GetThemeDefaultFont();
        var fs = _canvas.GetThemeDefaultFontSize();
        var x0 = Mathf.Round(_canvas.Size.X - PanelW - 4);
        var y = 16f;
        var mapH = grid.SpreadCount * CellH;
        _canvas.DrawRect(new Rect2(x0, y, PanelW, PanelH), new Color(0.02f, 0.03f, 0.04f, 0.86f));
        _canvas.DrawRect(new Rect2(x0, y, PanelW, PanelH), new Color(0.5f, 0.6f, 0.65f, 0.6f), false);
        var x = x0 + 4;
        var dim = new Color(0.7f, 0.75f, 0.8f);
        var line = (float)fs + 1;

        void Text(string s, Color? c = null, float dx = 0) =>
            _canvas.DrawString(font, new Vector2(x + dx, y + fs - 2), s, HorizontalAlignment.Left, -1, fs, c ?? Colors.White);

        var mode = leads.Mode == DirectorMode.Calibrate
            ? $"calibrate {d.Observations}/{leads.CalibrationAttacks}"
            : $"control, n {d.Observations}";
        Text($"F2 director · {mode}");
        y += line;
        Text($"{leads.Challenge} {leads.Challenge.Target():P0} · adaptive {(leads.Adaptive ? "on" : "OFF")}", leads.Adaptive ? dim : new Color(1f, 0.6f, 0.4f));
        y += line + 2;

        // Heat map of the belief over (median, spread).
        if (_heat.Length != grid.SliceSize) _heat = new double[grid.SliceSize];
        d.MedianSpreadMarginal(_heat);
        var max = _heat.Max();
        for (var si = 0; si < grid.SpreadCount; si++)
            for (var mi = 0; mi < grid.MedianCount; mi++)
            {
                var v = (float)Math.Sqrt(_heat[si * grid.MedianCount + mi] / max);
                if (v < 0.02f) continue;
                var c = new Color(0.1f, 0.2f, 0.5f).Lerp(new Color(1f, 0.85f, 0.3f), v);
                _canvas.DrawRect(new Rect2(x + mi * CellW, y + si * CellH, CellW, CellH), new Color(c, Mathf.Min(1, 0.3f + v)));
            }
        var mapW = grid.MedianCount * CellW;
        _canvas.DrawRect(new Rect2(x - 1, y - 1, mapW + 2, mapH + 2), new Color(1, 1, 1, 0.25f), false);
        float Px(double ms) => x + (float)((Math.Log(ms) - grid.LogMedians[0]) / grid.LogStep + 0.5) * CellW;
        foreach (var tick in new[] { 200, 300, 500, 800 })
            _canvas.DrawRect(new Rect2(Px(tick), y + mapH + 1, 1, 2), new Color(1, 1, 1, 0.5f));
        // The 80% interval and the estimate.
        var iy = y + mapH + 4;
        _canvas.DrawRect(new Rect2(Px(est.LowMs), iy, Mathf.Max(1, Px(est.HighMs) - Px(est.LowMs)), 2), new Color(0.5f, 0.9f, 1f));
        _canvas.DrawRect(new Rect2(Px(est.MedianMs) - 0.5f, y - 1, 1, mapH + 7), new Color(1, 1, 1, 0.9f));
        DrawReliability(new Vector2(x + mapW + 8, y), mapH + 6, d);
        y += mapH + 8;

        Text($"median {est.MedianMs:0} ms  80%: {est.LowMs:0}–{est.HighMs:0}");
        y += line;
        Text($"s {est.Spread:0.00}  lapse {est.Lapse:P0}  attune {est.Attunement:P0}", dim);
        y += line;
        var lead = leads.LastLeadMs > 0 ? $"lead {leads.LastLeadMs} ms ({leads.LastAttack}) pred {leads.LastPredicted:P0}" : "lead: no attack yet";
        Text(lead);
        y += line;
        var recent = d.History.Skip(Math.Max(0, d.History.Count - Recent)).ToList();
        if (recent.Count > 0)
            Text($"last {recent.Count}: predicted {recent.Average(r => r.Predicted):P0}  observed {recent.Average(r => r.Success ? 1.0 : 0.0):P0}", dim);
        else
            Text("predicted vs observed: no data", dim);
        y += line;
        DrawStrip(recent, x, y);
        y += 8;
        Text("Backspace: forget the player", new Color(1, 1, 1, 0.45f));
    }

    /// <summary>Predicted (x) against observed (y) success over the session, five bins.</summary>
    private void DrawReliability(Vector2 at, float size, ReactionDirector d)
    {
        _canvas.DrawRect(new Rect2(at, new Vector2(size, size)), new Color(1, 1, 1, 0.25f), false);
        _canvas.DrawLine(at + new Vector2(0, size), at + new Vector2(size, 0), new Color(1, 1, 1, 0.2f));
        for (var b = 0; b < 5; b++)
        {
            var bin = d.History.Where(r => Math.Min(4, (int)(r.Predicted * 5)) == b).ToList();
            if (bin.Count == 0) continue;
            var px = (float)bin.Average(r => r.Predicted) * size;
            var py = (float)(1 - bin.Average(r => r.Success ? 1.0 : 0.0)) * size;
            var r = Mathf.Clamp(1 + bin.Count / 6f, 1, 3);
            _canvas.DrawRect(new Rect2(at.X + px - r / 2, at.Y + py - r / 2, r, r), new Color(1f, 0.85f, 0.3f));
        }
    }

    /// <summary>Recent attacks, oldest first: green success, red miss, grey lapse; the bar is the prediction.</summary>
    private void DrawStrip(System.Collections.Generic.List<ObservationRecord> recent, float x, float y)
    {
        for (var i = 0; i < recent.Count; i++)
        {
            var r = recent[i];
            var c = r.Evidence.Kind == EvidenceKind.Lapse ? new Color(0.6f, 0.6f, 0.6f)
                : r.Success ? new Color(0.4f, 0.9f, 0.4f) : new Color(1f, 0.35f, 0.3f);
            var bx = x + i * 6;
            _canvas.DrawRect(new Rect2(bx, y + 1, 5, 4), c);
            _canvas.DrawRect(new Rect2(bx, y + 5 - (float)r.Predicted * 5, 5, 1), Colors.White);
        }
    }
}
