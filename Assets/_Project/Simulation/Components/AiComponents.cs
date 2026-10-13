using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct ChaseTarget { public EcsPackedEntity Value; }

    public struct Separation
    {
        public float Radius;
        public float Strength;
        public Vector3 Push;
    }
}
