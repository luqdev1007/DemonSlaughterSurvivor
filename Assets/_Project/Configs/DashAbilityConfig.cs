using Game.Core;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "DashAbilityConfig", menuName = "Game/Content/Dash Ability Config")]
    public sealed class DashAbilityConfig : ContentConfig
    {
        [SerializeField] private float _distance;
        [SerializeField] private float _duration;
        [SerializeField] private float _cooldown;
        [SerializeField] private DashDirection _direction;

        [Header("Invulnerability")]
        [SerializeField] private float _invulnerabilitySeconds = 0.3f;

        [Header("Push")]
        [SerializeField] private float _pushSpeed = 5f;
        [SerializeField] private float _pushSeconds = 0.6f;

        public float Distance => _distance;
        public float Duration => _duration;
        public float Cooldown => _cooldown;
        public DashDirection Direction => _direction;

        public float InvulnerabilitySeconds => _invulnerabilitySeconds;

        public float PushSpeed => _pushSpeed;

        public float PushSeconds => _pushSeconds;
    }
}
