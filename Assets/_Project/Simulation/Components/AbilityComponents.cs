using Game.Core;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct DashRequest { public float Age; }

    public struct DashCooldown { public float Remaining; }

    public struct Dashing
    {
        public int RemainingTicks;
        public int TotalTicks;
        public Vector3 Direction;
        public float Speed;
        public float AccelerationPower;
    }

    public struct DashStats
    {
        public float Distance;
        public float Duration;
        public float Cooldown;
        public DashDirection Direction;
        public float AccelerationPower;
        public float InvulnerabilitySeconds;
        public float PushSpeed;
        public float PushSeconds;
    }
}
