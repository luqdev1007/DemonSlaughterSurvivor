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

    public struct CounterStrikeCooldown { public int RemainingTicks; }

    public struct PunishingLeapCooldown { public int RemainingTicks; }

    public struct ExecutionThirstActive { }

    public struct LowHealthFury { public int Step; }

    public struct CounterStrikeArmed
    {
        public int RemainingTicks;
        public int Level;
    }

    public struct HeroicLeapCooldown { public int RemainingTicks; }

    public struct PowerShotCooldown { public int RemainingTicks; }

    public struct ExecutionCooldown { public int RemainingTicks; }

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
