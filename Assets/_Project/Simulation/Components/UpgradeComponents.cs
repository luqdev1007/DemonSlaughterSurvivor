using Leopotam.EcsLite;

namespace Game.Simulation.Components
{
    public struct TakenPerk
    {
        public EcsPackedEntity Owner;
        public string PerkId;
        public int Level;
    }

    public struct UpgradeRandom { public uint State; }

    public struct CounterStrikeRandom { public uint State; }

    public struct HeroicLeapCooldown { public int RemainingTicks; }

    public struct PendingLevelUps { public int Count; }

    public struct PendingChoice
    {
        public string First;
        public string Second;
        public string Third;
        public int Count;
    }

    public struct LevelUpEvent { public int Count; }
}
