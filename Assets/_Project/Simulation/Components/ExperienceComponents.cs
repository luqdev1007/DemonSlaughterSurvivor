using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct PickupRadius { public float Value; public float Base; }

    public struct Experience { public int Level; public int Current; }

    public struct GemDrop { public GemConfig Config; }

    public struct Gem { public int Experience; }

    public struct GemFlight
    {
        public Vector3 Start;
        public int TotalTicks;
        public int ElapsedTicks;
        public EcsPackedEntity Target;
    }
}
