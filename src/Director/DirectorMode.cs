namespace SwordInTheForest.Director
{
    public enum DirectorMode
    {
        /// <summary>Pick the lead time expected to teach the most about the player's median (Psi method).</summary>
        Calibrate,
        /// <summary>Pick the shortest lead time whose predicted success reaches the target.</summary>
        Control,
    }
}
