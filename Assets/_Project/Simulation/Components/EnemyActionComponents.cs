using Game.Configs;

namespace Game.Simulation.Components
{
    public enum EnemyActionPhase
    {
        None = 0,
        ChargeWindup = 1,
        ChargeLock = 2,
        ChargeRush = 3,
        ChargeRecover = 4,
        SpinRaise = 5,
        Spinning = 6,
        SpinRecover = 7
    }

    public struct Boss { public BossConfig Config; }

    public struct BossRandom { public uint State; }

    public struct EnemyAction
    {
        public EnemyActionPhase Phase;
        public int PhaseTicks;
        public int PhaseTotalTicks;
        public int GlobalCooldownTicks;
    }

    public struct PendingVictory { public int RemainingTicks; }
}
