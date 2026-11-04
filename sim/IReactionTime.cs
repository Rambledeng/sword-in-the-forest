namespace SwordInTheForest.Sim;

/// <summary>Where a simulated player's reaction times come from (before input delay and frame snapping).</summary>
public interface IReactionTime
{
    /// <summary>The true median reaction time (ms): what the director's estimate is scored against.</summary>
    double MedianMs { get; }

    double SampleMs(Random rng);
}
