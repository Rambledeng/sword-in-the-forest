using System.Collections.Generic;
using Godot;
using SwordInTheForest.Tuning;

namespace SwordInTheForest.Core;

/// <summary>
/// The shared tuning resources. GD.Load caches, so every node sees the same instances and the
/// F1 panel's edits apply live.
/// </summary>
public static class Tunables
{
    public const string Dir = "res://tuning";

    public static PlayerTuning Player => GD.Load<PlayerTuning>($"{Dir}/player.tres");
    public static EnemyTuning SaberBandit => GD.Load<EnemyTuning>($"{Dir}/saber_bandit.tres");
    public static FeelTuning Feel => GD.Load<FeelTuning>($"{Dir}/feel.tres");
    public static CombatTuning Combat => GD.Load<CombatTuning>($"{Dir}/combat.tres");

    public static AttackData Attack(string id) => GD.Load<AttackData>($"{Dir}/attacks/{id}.tres");

    /// <summary>Every tuning resource, for the F1 panel.</summary>
    public static IEnumerable<Resource> All()
    {
        yield return Player;
        yield return SaberBandit;
        yield return Feel;
        yield return Combat;
        foreach (var file in DirAccess.GetFilesAt($"{Dir}/attacks"))
        {
            var name = file.TrimSuffix(".remap");
            if (name.EndsWith(".tres")) yield return GD.Load<AttackData>($"{Dir}/attacks/{name}");
        }
    }
}
