namespace SwordInTheForest.Actors;

/// <summary>What the run's sword-intent upgrades add to the player's base tuning.</summary>
public sealed class PlayerMods
{
    public int HealthBonus { get; set; }
    public float PostureBonus { get; set; }
    public float PostureDrainFactor { get; set; } = 1f;
    public int DamageBonus { get; set; }
    public int SipsBonus { get; set; }
    /// <summary>Multiplies the posture damage your deflects deal.</summary>
    public float DeflectFactor { get; set; } = 1f;
    public int FinisherHeal { get; set; }
    /// <summary>瞬步 is taken.</summary>
    public bool FlashStep { get; set; }
    /// <summary>Multiplies its cooldown (each repeat take shortens it).</summary>
    public float FlashStepCooldownFactor { get; set; } = 1f;
    /// <summary>余威: posture each finisher deals to the other enemies within reach.</summary>
    public float DreadPosture { get; set; }
    /// <summary>止水: multiplies the posture your own deflects cost you.</summary>
    public float DeflectSelfPostureFactor { get; set; } = 1f;
}
