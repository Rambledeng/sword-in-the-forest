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

    // Held in static fields: Godot's resource cache is weak, so if only a collectable C# wrapper
    // referenced a resource, a GC would free it and the next load would drop live F1 edits.
    private static PlayerTuning? _player;
    private static EnemyTuning? _saberBandit;
    private static FeelTuning? _feel;
    private static CombatTuning? _combat;
    private static readonly Dictionary<string, AttackData> Attacks = new();

    public static PlayerTuning Player => _player ??= GD.Load<PlayerTuning>($"{Dir}/player.tres");
    public static EnemyTuning SaberBandit => _saberBandit ??= GD.Load<EnemyTuning>($"{Dir}/saber_bandit.tres");
    public static FeelTuning Feel => _feel ??= GD.Load<FeelTuning>($"{Dir}/feel.tres");
    public static CombatTuning Combat => _combat ??= GD.Load<CombatTuning>($"{Dir}/combat.tres");

    public static AttackData Attack(string id)
    {
        if (!Attacks.TryGetValue(id, out var a)) Attacks[id] = a = GD.Load<AttackData>($"{Dir}/attacks/{id}.tres");
        return a;
    }

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
            if (name.EndsWith(".tres")) yield return Attack(name.TrimSuffix(".tres"));
        }
    }
}
