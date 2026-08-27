namespace Game.Core
{
    public sealed class RunContext
    {
        public RunContext(string levelId, string characterId, RunMode mode, int seed)
        {
            LevelId = levelId;
            CharacterId = characterId;
            Mode = mode;
            Seed = seed;
        }

        public string LevelId { get; }
        public string CharacterId { get; }
        public RunMode Mode { get; }
        public int Seed { get; }
    }
}
