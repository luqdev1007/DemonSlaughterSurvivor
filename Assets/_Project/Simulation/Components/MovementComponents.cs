using UnityEngine;

namespace Game.Simulation.Components
{
    public struct MoveIntent { public Vector3 Value; }
    public struct Velocity { public Vector3 Value; }
    public struct PreviousPosition { public Vector3 Value; }
    public struct MoveSpeed { public float Value; public float Base; }
    public struct TurnSpeed { public float Value; }
}
