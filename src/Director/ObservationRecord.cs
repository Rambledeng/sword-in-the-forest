namespace SwordInTheForest.Director
{
    /// <summary>One observed attack with the success the belief predicted just before it.</summary>
    public readonly struct ObservationRecord
    {
        public ObservationRecord(ThreatEvent evidence, double predicted)
        {
            Evidence = evidence;
            Predicted = predicted;
        }

        public ThreatEvent Evidence { get; }
        public double Predicted { get; }
        public bool Success => Evidence.Success;
    }
}
