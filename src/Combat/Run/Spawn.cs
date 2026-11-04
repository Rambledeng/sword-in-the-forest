namespace SwordInTheForest.Run
{
    /// <summary>One enemy in a planned room: its kind, tile and colour variant.</summary>
    public readonly struct Spawn
    {
        public Spawn(EnemyKind kind, TilePos at, int variant)
        {
            Kind = kind;
            At = at;
            Variant = variant;
        }

        public EnemyKind Kind { get; }
        public TilePos At { get; }
        public int Variant { get; }
    }
}
