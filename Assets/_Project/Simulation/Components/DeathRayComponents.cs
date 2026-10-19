using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct Channeling { }

    public struct DeathRayCooldown { public int RemainingTicks; }

    public struct DeathRay
    {
        public DeathRayConfig Config;
        public EcsPackedEntity Owner;
        public Vector3 Direction;
        public bool Firing;
        public int RemainingTicks;
        public int TotalTicks;
        public int TickCountdown;
        public float InitialDamage;
        public float TickDamage;
        public int CooldownTicks;
    }
}
