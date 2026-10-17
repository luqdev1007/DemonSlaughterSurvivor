using UnityEngine;

namespace Game.Simulation.Components
{
    public struct InputState
    {
        public Vector3 AimPoint;
        public bool HasAim;
        public bool StandHeld;
        public bool MoveHeld;
        public bool AimTurning;
        public float AimAngularSpeed;
        public Vector3 LastAimDirection;
        public float PredictiveLead;
        public float PredictiveStretch;
        public string PredictiveSwing;
    }

    public struct Aim
    {
        public float DeadZone;
        public float TurnSpeed;
    }
}
