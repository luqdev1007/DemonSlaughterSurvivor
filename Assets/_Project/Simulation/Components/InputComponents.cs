using UnityEngine;

namespace Game.Simulation.Components
{
    public struct InputState
    {
        public Vector3 AimPoint;
        public bool HasAim;
        public bool StandHeld;
    }

    public struct Aim
    {
        public float DeadZone;
        public float TurnSpeed;
    }
}
