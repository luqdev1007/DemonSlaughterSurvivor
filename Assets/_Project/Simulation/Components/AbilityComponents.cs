using Game.Configs;
using Game.Core;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct DashRequest { public float Age; public float Strength; }

    public struct DashCharge { public int Ticks; }

    public struct DashEndedEvent
    {
        public Vector3 Start;
        public Vector3 End;
        public Vector3 Direction;
        public float Strength;
        public float DamageMultiplier;
    }

    public struct DashCooldown { public float Remaining; }

    public struct BlinkStats { public BlinkConfig Config; }

    public struct BlinkRequest { public float Age; }

    public struct BlinkCooldown { public float Remaining; }

    public struct AbilityCooldownScale { public float Value; public float Base; }

    public struct SlicingDashRoll
    {
        public int Level;
        public EcsPackedEntity Strike;
        public bool Cued;
    }

    public struct UpperSlashCue
    {
        public string Trigger;
        public float PlaybackSpeed;
    }

    public struct SlicingDashCooldown { public int RemainingTicks; }

    public struct AgileDashCooldown { public int RemainingTicks; }

    public struct DashFinisher
    {
        public EcsPackedEntity Owner;
        public int RemainingTicks;
        public string Trigger;
        public float PlaybackSpeed;
    }

    public struct DashFinisherStarted { }

    public struct Dashing
    {
        public int RemainingTicks;
        public int TotalTicks;
        public Vector3 Direction;
        public float Speed;
        public float AccelerationPower;
        public float Strength;
        public float DamageMultiplier;
        public Vector3 Start;
    }

    public struct UltimateCharge { public float Value; public float Max; }

    public struct RageState { public int TicksSinceCombat; }

    public struct BerserkMode { public int RemainingTicks; public int TotalTicks; }

    public struct PoweringUp { public int RemainingTicks; public int TotalTicks; }

    public struct DashStats
    {
        public float Distance;
        public float Duration;
        public float Cooldown;
        public float CooldownBase;
        public DashDirection Direction;
        public float AccelerationPower;
        public float InvulnerabilitySeconds;
        public float PushSpeed;
        public float PushSeconds;
        public float PushArcHeight;
        public float PushReach;
        public float PushEdgeScale;
        public float PushForwardTiltDegrees;
        public float PushSpreadDegrees;
        public bool ChargeEnabled;
        public float TapSeconds;
        public float MaxChargeSeconds;
        public AnimationCurve ChargeCurve;
        public float DistanceAtFullCharge;
        public float SpeedAtFullCharge;
        public float DamageAtFullCharge;
        public float ChargeMoveSpeedMore;
        public bool ChargeRunAnimation;
    }
}
