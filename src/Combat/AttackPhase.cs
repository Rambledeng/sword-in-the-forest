namespace SwordInTheForest.Combat
{
    public enum AttackPhase
    {
        /// <summary>Before the glint. Guarding now is anticipation: it can only block.</summary>
        Windup,
        /// <summary>From the glint up to the impact: the response window.</summary>
        Lead,
        /// <summary>After the impact: the player's opening.</summary>
        Recovery,
        Done,
    }
}
