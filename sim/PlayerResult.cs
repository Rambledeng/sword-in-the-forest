using SwordInTheForest.Director;

namespace SwordInTheForest.Sim;

/// <summary>What happened to one simulated player.</summary>
public sealed class PlayerResult
{
    public PlayerResult(PlayerParams truth, double[] medianAfter, int[] leads, bool[] success, int calibrationAttacks)
    {
        Truth = truth;
        MedianAfter = medianAfter;
        Leads = leads;
        Success = success;
        CalibrationAttacks = calibrationAttacks;
    }

    public PlayerParams Truth { get; }
    /// <summary>MedianAfter[k] = the estimated median after k + 1 attacks.</summary>
    public double[] MedianAfter { get; }
    public int[] Leads { get; }
    public bool[] Success { get; }
    public int CalibrationAttacks { get; }

    public double ErrorAfter(int attacks) => Math.Abs(MedianAfter[attacks - 1] - Truth.MedianMs);

    /// <summary>Success rate over the attacks after calibration (the run).</summary>
    public double RunSuccessRate
    {
        get
        {
            var n = Success.Length - CalibrationAttacks;
            if (n <= 0) return double.NaN;
            var s = 0;
            for (var i = CalibrationAttacks; i < Success.Length; i++) if (Success[i]) s++;
            return (double)s / n;
        }
    }
}
