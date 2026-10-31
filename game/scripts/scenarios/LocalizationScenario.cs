using Godot;

namespace SwordInTheForest.Scenarios;

/// <summary>The title label shows its text in en, nl and zh_CN, and is altered by pseudolocalization.</summary>
public partial class LocalizationScenario : Scenario
{
    private static readonly (string Locale, string Expected)[] Cases =
    {
        ("en", "Sword in the Forest"),
        ("nl", "Zwaard in het Woud"),
        ("zh_CN", "林中剑"),
    };

    private Label? _label;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            var title = GD.Load<PackedScene>("res://scenes/Title.tscn").Instantiate();
            AddChild(title);
            _label = title.GetNode<Label>("%TitleLabel");
            Check(_label.Text == "ui.title.name", "title label must hold a key, not literal text");
            return;
        }

        // One locale per frame so the label re-translates in between.
        var i = frame - 1;
        if (i < Cases.Length)
        {
            var (locale, expected) = Cases[i];
            TranslationServer.SetLocale(locale);
            var shown = _label!.Atr(_label.Text);
            Check(shown == expected, $"{locale}: expected '{expected}', got '{shown}'");
            GD.Print($"  {locale}: {shown}");
        }
        else if (i == Cases.Length)
        {
            TranslationServer.SetLocale("en");
            TranslationServer.PseudolocalizationEnabled = true;
            TranslationServer.ReloadPseudolocalization();
            var shown = _label!.Atr(_label.Text);
            Check(shown != "Sword in the Forest" && shown != "ui.title.name", $"pseudolocalization had no effect: '{shown}'");
            GD.Print($"  pseudo: {shown}");
            TranslationServer.PseudolocalizationEnabled = false;
        }
    }
}
