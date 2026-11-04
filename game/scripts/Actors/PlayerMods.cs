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
}
