using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "PiercingVolleyConfig", menuName = "Game/Content/Piercing Volley Config")]
    public sealed class PiercingVolleyConfig : PerkBehaviourConfig
    {
        [SerializeField, Min(1)] private int _levelCount = 1;

        public override int LevelCount => _levelCount;

        public override void Validate(PerkConfig perk)
        {
            bool power = false;
            bool multishot = false;

            for (int index = 0; index < perk.Requires.Count; index++)
            {
                PerkConfig parent = perk.Requires[index];

                if (parent == null)
                    continue;

                power |= parent.Behaviour is PowerShotConfig;
                multishot |= parent.Behaviour is MultishotConfig;
            }

            if (power == false || multishot == false)
                throw new InvalidOperationException(
                    $"{nameof(PerkConfig)} '{perk.Id}' piercing volley '{name}' empowers a multishot volley, so it must require both a " +
                    $"{nameof(PowerShotConfig)} perk and a {nameof(MultishotConfig)} perk.");
        }
    }
}
