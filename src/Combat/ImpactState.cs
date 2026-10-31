namespace SwordInTheForest.Combat
{
    /// <summary>The player's situation on the impact frame.</summary>
    public readonly struct ImpactState
    {
        public ImpactState(bool inReach, bool invulnerable, bool guarding)
        {
            InReach = inReach;
            Invulnerable = invulnerable;
            Guarding = guarding;
        }

        /// <summary>The player is inside the attack's hit area.</summary>
        public bool InReach { get; }
        /// <summary>The player is inside a roll's invulnerability frames.</summary>
        public bool Invulnerable { get; }
        /// <summary>The player is holding a working guard (not posture-broken).</summary>
        public bool Guarding { get; }
    }
}
