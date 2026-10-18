using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct RangedAttack
    {
        public RangedAttackConfig Config;
        public int CooldownTicks;
        public int WindupTicks;
        public float PlaybackSpeed;
        public bool IsHolding;
    }

    public struct RangedWindupStarted { }

    public struct CastInterruptedEvent { }

    public struct Staggered
    {
        public int RemainingTicks;
    }

    public struct Arrow { }

    public struct Projectile
    {
        public EcsPackedEntity Source;
        public Vector3 LastPosition;
        public float Damage;
        public float Radius;
        public int RemainingTicks;
    }
}
