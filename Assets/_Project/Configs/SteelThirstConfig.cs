using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "SteelThirstConfig", menuName = "Game/Content/Steel Thirst Config")]
    public sealed class SteelThirstConfig : PerkBehaviourConfig
    {
        [SerializeField, Min(1)] private int _minTargets = 3;
        [SerializeField, Min(0f)] private float _attackSpeedBonus = 0.5f;
        [SerializeField, Min(1)] private int _swings = 3;

        public override int LevelCount => 1;
        public int MinTargets => _minTargets;
        public float AttackSpeedBonus => _attackSpeedBonus;
        public int Swings => _swings;

        public override void Validate(PerkConfig perk)
        {
            if (_minTargets < 1 || _swings < 1 || _attackSpeedBonus <= 0f || float.IsNaN(_attackSpeedBonus))
                throw new InvalidOperationException(
                    $"{nameof(PerkConfig)} '{perk.Id}' steel thirst '{name}' needs at least one target and one swing and a positive bonus " +
                    $"(targets {_minTargets}, swings {_swings}, bonus {_attackSpeedBonus}).");
        }
    }
}
