using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct PickupRadius { public float Value; public float Base; }

    public struct Experience { public int Level; public int Current; public float LuckCarry; }

    public struct LevelGrowthApplied { public int Level; }

    public struct GemDrop { public GemConfig Config; }

    public struct Gem { public int Experience; }

    public struct HealOrbDrop
    {
        public HealOrbConfig Config;
        public float Chance;
    }

    public struct HealOrb { public float Fraction; }

    public struct GemScatter
    {
        public Vector3 Start;
        public Vector3 End;
        public float Height;
        public int ElapsedTicks;
        public int TotalTicks;
    }

    public struct GemFlight
    {
        public float Side;
        public Vector3 Start;
        public int TotalTicks;
        public int ElapsedTicks;
        public EcsPackedEntity Target;
    }
}
