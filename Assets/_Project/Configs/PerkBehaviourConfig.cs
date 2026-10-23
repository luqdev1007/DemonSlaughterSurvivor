using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public abstract class PerkBehaviourConfig : ScriptableObject
    {
        public abstract int LevelCount { get; }

        public virtual float MaxPushSpeed => 0f;

        public virtual IReadOnlyList<string> Tags => Array.Empty<string>();

        public virtual float WeaponDamageShare(int level) => 0f;

        public virtual float CooldownSeconds(int level) => 0f;

        public abstract void Validate(PerkConfig perk);
    }
}
