using Godot;
using SwordInTheForest.Run;

namespace SwordInTheForest.Ui;

/// <summary>Names, descriptions (with run.tres numbers) and icons for the starter perks.</summary>
public static class PerkText
{
    public static string Key(StarterPerk p) => p switch
    {
        StarterPerk.Hale => "hale",
        StarterPerk.Sturdy => "sturdy",
        StarterPerk.EvenBreath => "even_breath",
        StarterPerk.SpareSip => "spare_sip",
        _ => "steady_hand",
    };

    public static string Name(StarterPerk p) => TranslationServer.Translate($"upgrade.perk.{Key(p)}.name");

    public static string Describe(StarterPerk p)
    {
        var r = Core.Tunables.Run;
        static string Pct(float factor) => Mathf.RoundToInt((factor - 1f) * 100f).ToString();
        return TranslationServer.Translate($"upgrade.perk.{Key(p)}.desc").ToString()
            .Replace("{health}", r.PerkHealth.ToString())
            .Replace("{posture}", Mathf.RoundToInt(r.PerkPosture).ToString())
            .Replace("{pct}", Pct(p == StarterPerk.EvenBreath ? r.PerkBreathFactor : r.PerkDeflectFactor))
            .Replace("{sips}", r.PerkSips.ToString());
    }

    /// <summary>The icon of the upgrade the perk echoes (no new art).</summary>
    public static Texture2D Icon(StarterPerk p) => PixelIcons.For(StarterPerks.Echoes(p));
}
