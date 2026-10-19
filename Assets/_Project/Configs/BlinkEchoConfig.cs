using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "BlinkEchoConfig", menuName = "Game/Content/Blink Echo Config")]
    public sealed class BlinkEchoConfig : PerkBehaviourConfig
    {
        [SerializeField] private BlinkEchoLevel[] _levels = Array.Empty<BlinkEchoLevel>();

        public override int LevelCount => _levels.Length;

        public BlinkEchoLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(BlinkEchoConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' blink echo '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                BlinkEchoLevel entry = Level(level);

                if (entry == null || entry.WindowSeconds <= 0)
                    throw new InvalidOperationException($"{label} level {level} must have positive values.");
            }
        }
    }

    [Serializable]
    public sealed class BlinkEchoLevel
    {
        [SerializeField, Min(0.05f)] private float _windowSeconds = 0.5f;

        public float WindowSeconds => _windowSeconds;
    }
}
