using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "FireballVolleyConfig", menuName = "Game/Content/Fireball Volley Config")]
    public sealed class FireballVolleyConfig : PerkBehaviourConfig
    {
        [SerializeField] private FireballVolleyLevel[] _levels = Array.Empty<FireballVolleyLevel>();
        [SerializeField, Min(0.02f)] private float _intervalSeconds = 0.12f;
        [SerializeField, Min(0f)] private float _spreadDegrees = 8f;

        public override int LevelCount => _levels.Length;

        public float IntervalSeconds => _intervalSeconds;

        public float SpreadDegrees => _spreadDegrees;

        public FireballVolleyLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(FireballVolleyConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' fireball volley '{name}'";

            for (int level = 1; level <= LevelCount; level++)
            {
                FireballVolleyLevel entry = Level(level);

                if (entry == null || entry.CooldownSeconds <= 0 || entry.ExtraFireballs <= 0)
                    throw new InvalidOperationException($"{label} level {level} must have positive values.");
            }
        }
    }

    [Serializable]
    public sealed class FireballVolleyLevel
    {
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 8f;
        [SerializeField, Min(1)] private int _extraFireballs = 2;

        public float CooldownSeconds => _cooldownSeconds;

        public int ExtraFireballs => _extraFireballs;
    }
}
