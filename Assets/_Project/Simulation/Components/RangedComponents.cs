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

    public struct Arrow : IEcsAutoReset<Arrow>
    {
        public const int InitialHitCapacity = 16;

        public bool Piercing;
        public float CritChanceBonus;
        public EcsPackedEntity[] Hits;
        public int HitCount;

        public void AutoReset(ref Arrow c)
        {
            if (c.Hits == null)
                c.Hits = new EcsPackedEntity[InitialHitCapacity];

            c.Piercing = false;
            c.CritChanceBonus = 0f;
            c.HitCount = 0;
        }
    }

    public struct Projectile
    {
        public EcsPackedEntity Source;
        public Vector3 LastPosition;
        public float Damage;
        public float Radius;
        public int RemainingTicks;
    }
}
