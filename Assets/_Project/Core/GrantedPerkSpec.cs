namespace Game.Core
{
    public readonly struct GrantedPerkSpec
    {
        public GrantedPerkSpec(string perkId, int level)
        {
            PerkId = perkId;
            Level = level;
        }

        public string PerkId { get; }
        public int Level { get; }
    }
}
