using UnityEngine;

namespace DemonSlaughter.Gameplay.ECS.Components
{
    public struct AttackRequestComponent { }

    public struct AttackStateComponent
    {
        public float Cooldown;     
        public int ComboIndex;      
        public bool IsAttacking;      
    }

    public struct WeaponComponent
    {
        public Transform SwordBone;   
        public float HitboxRadius;    
        public float Damage;
    }

    public struct AttackDetectorComponent
    {
        public float DetectionRadius;
        public float DetectionAngle;
    }

    public struct DamageReceivedComponent
    {
        public float Value;
        public int AttackerEntity;
    }

    public struct HealthComponent
    {
        public float Current;
        public float Max;
    }

    public struct DamageComponent
    {
        public float Value;
    }

    public struct AttackCooldownComponent
    {
        public float Remaining;
    }
}