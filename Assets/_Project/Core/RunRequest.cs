namespace Game.Core
{
    public readonly struct RunRequest
    {
        public RunRequest(string levelId, string characterId, RunMode mode)
        {
            LevelId = levelId;
            CharacterId = characterId;
            Mode = mode;
        }

        public string LevelId { get; }
        public string CharacterId { get; }
        public RunMode Mode { get; }
    }
}
