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

        public float Distance => _distance;
        public float Duration => _duration;
        public float Cooldown => _cooldown;
        public DashDirection Direction => _direction;
    }
}
