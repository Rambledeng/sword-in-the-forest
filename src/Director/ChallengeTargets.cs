namespace SwordInTheForest.Director
{
    public static class ChallengeTargets
    {
        /// <summary>Target success rate: Relaxed 0.85, Standard 0.70, Hard 0.55.</summary>
        public static double Target(this Challenge c) => c switch
        {
            Challenge.Relaxed => 0.85,
            Challenge.Hard => 0.55,
            _ => 0.70,
        };
    }
}
