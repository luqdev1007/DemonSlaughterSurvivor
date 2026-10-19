using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct MeteorCooldown { public int RemainingTicks; }

    public struct MeteorCast
    {
        public MeteorConfig Config;
        public EcsPackedEntity Owner;
        public float Damage;
        public Vector3 Target;
        public int RemainingTicks;
    }

    public struct Meteor
    {
        public MeteorConfig Config;
        public EcsPackedEntity Owner;
        public float Damage;
        public Vector3 From;
        public Vector3 To;
        public int RemainingTicks;
        public int TotalTicks;
    }
}
