using UnityEngine;

namespace Game.Configs
{
    public abstract class PerkBehaviourConfig : ScriptableObject
    {
        public abstract int LevelCount { get; }

        public abstract void Validate(PerkConfig perk);
    }
}
