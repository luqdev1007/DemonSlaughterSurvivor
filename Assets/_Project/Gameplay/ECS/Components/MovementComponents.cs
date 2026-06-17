using UnityEngine;

namespace DemonSlaughter.Gameplay.ECS.Components
{
    public struct MoveInputComponent
    {
        public Vector2 Value;
    }

    public struct MovementComponent
    {
        public float Speed;
    }

    public struct TransformRefComponent
    {
        public Transform Value;
    }
}