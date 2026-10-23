using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "KnightBannerConfig", menuName = "Game/Content/Knight Banner Config")]
    public sealed class KnightBannerConfig : PerkBehaviourConfig
    {
        [SerializeField, Min(1)] private int _levels = 5;
        [SerializeField] private string[] _tags = { "Charge", "Physical" };

        [Header("Cooldown")]
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 12f;
        [SerializeField, Min(0f)] private float _cooldownReductionPerLevel = 1f;
        [SerializeField, Min(0.1f)] private float _minCooldownSeconds = 1f;

        [Header("Ram")]
        [SerializeField, Min(0f)] private float _damageShare = 3f;
        [SerializeField, Min(0f)] private float _damageSharePerLevel = 0.4f;
        [SerializeField, Min(0f)] private float _hitReach = 0.8f;
        [SerializeField, Min(0f)] private float _pushSpeed = 16f;
        [SerializeField, Min(0f)] private float _pushSeconds = 0.35f;

        public override int LevelCount => _levels;
        public override float MaxPushSpeed => _pushSpeed;
        public override IReadOnlyList<string> Tags => _tags ?? Array.Empty<string>();
        public float HitReach => _hitReach;
        public float PushSpeed => _pushSpeed;
        public float PushSeconds => _pushSeconds;

        public override float CooldownSeconds(int level)
        {
            return Mathf.Max(_minCooldownSeconds, _cooldownSeconds - _cooldownReductionPerLevel * (level - 1));
        }

        public override float WeaponDamageShare(int level)
        {
            return _damageShare + _damageSharePerLevel * (level - 1);
        }

        public override void Validate(PerkConfig perk)
        {
            if (_levels < 1 || _damageShare <= 0f || float.IsNaN(_damageShare) || _pushSeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(PerkConfig)} '{perk.Id}' knight banner '{name}' needs at least one level, a positive damage share and a non-negative push " +
                    $"(levels {_levels}, share {_damageShare}, push {_pushSeconds} s).");
        }
    }
}
