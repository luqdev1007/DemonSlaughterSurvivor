using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "FieryWhirlConfig", menuName = "Game/Content/Fiery Whirl Config")]
    public sealed class FieryWhirlConfig : PerkBehaviourConfig
    {
        [SerializeField, Min(1)] private int _levelCount = 1;
        [SerializeField, Range(0.05f, 1f)] private float _sizeScale = 0.5f;
        [SerializeField, Range(0.05f, 1f)] private float _damageScale = 0.5f;
        [SerializeField, Min(1)] private int _pillarCount = 24;

        public override int LevelCount => _levelCount;

        public float SizeScale => _sizeScale;

        public float DamageScale => _damageScale;

        public int PillarCount => _pillarCount;

        public override void Validate(PerkConfig perk)
        {
            bool whirl = false;
            bool flame = false;

            for (int index = 0; index < perk.Requires.Count; index++)
            {
                PerkConfig parent = perk.Requires[index];

                if (parent == null)
                    continue;

                whirl |= parent.Behaviour is CounterStrikeConfig;
                flame |= parent.Behaviour is FlameSwingConfig;
            }

            if (whirl == false || flame == false)
                throw new InvalidOperationException(
                    $"{nameof(PerkConfig)} '{perk.Id}' fiery whirl '{name}' releases a small flame swing wave from the blade whirl, so it must require both a " +
                    $"{nameof(CounterStrikeConfig)} perk and a {nameof(FlameSwingConfig)} perk.");
        }
    }
}
