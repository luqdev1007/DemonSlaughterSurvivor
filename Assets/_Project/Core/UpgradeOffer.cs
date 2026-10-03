namespace Game.Core
{
    public readonly struct UpgradeOffer
    {
        public UpgradeOffer(string perkId, int nextLevel)
        {
            PerkId = perkId;
            NextLevel = nextLevel;
        }

        public string PerkId { get; }
        public int NextLevel { get; }
    }
}
