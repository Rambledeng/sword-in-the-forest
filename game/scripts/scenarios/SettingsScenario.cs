using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// The language picker on the title switches the language at once (en → nl → zh_CN → en), the
/// settings panel opens from the title and from the pause menu, its volume changes apply, and the
/// display mode and window size cycle.
/// </summary>
public partial class SettingsScenario : Scenario
{
    private Title _title = null!;
    private RunScene _run = null!;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        switch (frame)
        {
            case 0:
                _title = GD.Load<PackedScene>("res://scenes/Title.tscn").Instantiate<Title>();
                _title.ChangeScene = false;
                AddChild(_title);
                return;
            case 2:
                Check(UserSettings.Locale == "en", $"started in {UserSettings.Locale}");
                foreach (var u in System.Enum.GetValues<Upgrade>())
                {
                    var text = Ui.UpgradeScreen.Describe(u, Tr($"upgrade.{Ui.UpgradeScreen.Key(u)}.desc"));
                    Check(!text.Contains('{'), $"unfilled upgrade text: {text}");
                    var icon = Ui.PixelIcons.For(u);
                    Check(icon != null && icon.GetWidth() >= 12 && icon.GetHeight() >= 12, $"{u} has no icon");
                }
                Check(Ui.UpgradeScreen.Describe(Upgrade.IronDeflect, Tr("upgrade.iron_deflect.desc")).Contains(Mathf.RoundToInt((Tunables.Run.IronDeflectFactor - 1) * 100) + "%"), "Iron Deflect does not show its tuned number");
                _title.LanguageButton.EmitSignal(BaseButton.SignalName.Pressed);
                Check(UserSettings.Locale == "nl" && Tr("ui.title.start") == "Beginnen", "the picker did not switch to Dutch");
                Check(_title.LanguageButton.Text.Contains("Nederlands"), "the picker does not name Dutch");
                _title.LanguageButton.EmitSignal(BaseButton.SignalName.Pressed);
                Check(UserSettings.Locale == "zh_CN" && Tr("ui.title.start") == "开始", "the picker did not switch to Chinese");
                _title.LanguageButton.EmitSignal(BaseButton.SignalName.Pressed);
                Check(UserSettings.Locale == "en", "the picker did not come back to English");
                _title.Settings.Open();
                return;
            case 4:
                Check(_title.Settings.IsOpen, "settings did not open from the title");
                Check(!_title.StartRequested, "opening settings started the game");
                // Display: windowed → borderless → fullscreen → windowed; the window size cycles
                // through the multiples of 480×270 that fit, and only applies to a window.
                var panel = _title.Settings;
                Check(UserSettings.Display == DisplayMode.Windowed && !panel.ResolutionButton.Disabled, "does not start windowed with a window size");
                var start = UserSettings.Scale;
                for (var i = 0; i < UserSettings.MaxScale(); i++)
                {
                    panel.ResolutionButton.EmitSignal(BaseButton.SignalName.Pressed);
                    Check(UserSettings.Scale >= 1 && UserSettings.Scale <= UserSettings.MaxScale(), $"window size ×{UserSettings.Scale} does not fit");
                    Check(panel.ResolutionButton.Text.Contains($"{480 * UserSettings.Scale}×{270 * UserSettings.Scale}"), $"window size reads {panel.ResolutionButton.Text}");
                }
                Check(UserSettings.Scale == start, "the window size did not wrap around");
                panel.DisplayButton.EmitSignal(BaseButton.SignalName.Pressed);
                Check(UserSettings.Display == DisplayMode.Borderless && panel.ResolutionButton.Disabled, "borderless did not take over the window size");
                Check(panel.DisplayButton.Text.Contains(Tr("ui.settings.borderless")), "the display button does not say borderless");
                panel.DisplayButton.EmitSignal(BaseButton.SignalName.Pressed);
                Check(UserSettings.Display == DisplayMode.Fullscreen, "no fullscreen after borderless");
                panel.DisplayButton.EmitSignal(BaseButton.SignalName.Pressed);
                Check(UserSettings.Display == DisplayMode.Windowed && !panel.ResolutionButton.Disabled, "did not come back to windowed");
                UserSettings.SetVolumes(master: 0.5f, music: 0.25f, effects: 0.75f);
                Check(Mathf.IsEqualApprox(AudioServer.GetBusVolumeDb(0), Mathf.LinearToDb(0.5f)), "master volume not applied");
                Input.ParseInputEvent(new InputEventAction { Action = "ui_cancel", Pressed = true });
                return;
            case 6:
                Check(!_title.Settings.IsOpen, "Esc did not close the settings");
                UserSettings.SetVolumes(1, 1, 1);
                _title.QueueFree();
                _run = new RunScene { Seed = 7, StartStep = 1, PlayMusic = false, ChangeScenes = false };
                AddChild(_run);
                return;
            case 8:
                _run.Pause.Toggle(true);
                _run.Pause.Settings.Open();
                return;
            case 10:
                Check(_run.Pause.Settings.IsOpen && GetTree().Paused, "settings did not open from the pause menu");
                _run.Pause.Settings.Close();
                _run.Pause.Toggle(false);
                Check(!GetTree().Paused, "still paused");
                Pass();
                return;
        }
    }
}
