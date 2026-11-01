namespace SwordInTheForest.Combat
{
    /// <summary>The player's situation on the impact frame.</summary>
    public readonly struct ImpactState
    {
        public ImpactState(bool inReach, bool invulnerable, bool guarding, bool guardCovers = true, int angleDeg = 0)
        {
            InReach = inReach;
            Invulnerable = invulnerable;
            Guarding = guarding;
            GuardCovers = guardCovers;
            AngleDeg = angleDeg;
        }

        /// <summary>The player is inside the attack's hit area.</summary>
        public bool InReach { get; }
        /// <summary>The player is inside a roll's invulnerability frames.</summary>
        public bool Invulnerable { get; }
        /// <summary>The player is holding a working guard (not posture-broken).</summary>
        public bool Guarding { get; }
        /// <summary>The attacker is inside the guard's front arc, so a guard or deflect can catch it.</summary>
        public bool GuardCovers { get; }
        /// <summary>Angle between the player's facing and the direction to the attacker, 0–180°.</summary>
        public int AngleDeg { get; }
    }
}
