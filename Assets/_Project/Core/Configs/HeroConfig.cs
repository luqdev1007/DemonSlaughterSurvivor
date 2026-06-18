using UnityEngine;

namespace DemonSlaughter.Core.Configs
{
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Configs/Characters/HeroConfig")]
    public sealed class HeroConfig : ScriptableObject
    {
        [field: SerializeField] public string AddressableAddress { get; private set; }
        [field: SerializeField] public float MoveSpeed { get; private set; } = 5f;

        [Header("Combat")]
        [field: SerializeField] public float AttackCooldown { get; private set; } = 0.8f;
        [field: SerializeField] public float AttackDamage { get; private set; } = 25f;
        [field: SerializeField] public float DetectionRadius { get; private set; } = 3f;
        [field: SerializeField] public float DetectionAngle { get; private set; } = 120f;
        [field: SerializeField] public float HitboxRadius { get; private set; } = 0.4f;
        [field: SerializeField] public string SwordBoneName { get; private set; } = "Sword";

        [field: SerializeField]
        public string[] AttackAnimationTriggers { get; private set; }
            = { "Attack1", "Attack2", "Attack3" };
    }
}