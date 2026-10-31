namespace SwordInTheForest.Combat
{
    public readonly struct Resolution
    {
        public Resolution(Response response, Outcome outcome)
        {
            Response = response;
            Outcome = outcome;
        }

        public Response Response { get; }
        public Outcome Outcome { get; }

        /// <summary>The player takes the attack's damage.</summary>
        public bool PlayerHit => Outcome == Outcome.Hit;

        public override string ToString() => $"{Response}/{Outcome}";
    }
}
