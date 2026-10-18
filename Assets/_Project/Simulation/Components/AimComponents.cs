using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct TorsoAim
    {
        public TorsoAimConfig Config;
        public Vector3 Direction;
        public Vector3 Desired;
        public EcsPackedEntity Target;
        public bool HasTarget;
        public bool Manual;
        public bool Backpedal;
        public int TurnSide;
    }
}
