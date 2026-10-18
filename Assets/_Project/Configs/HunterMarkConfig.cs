using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "HunterMarkConfig", menuName = "Game/Content/Hunter Mark Config")]
    public sealed class HunterMarkConfig : PerkBehaviourConfig
    {
        [SerializeField] private HunterMarkLevel[] _levels = Array.Empty<HunterMarkLevel>();
        [SerializeField, Min(0.5f)] private float _radius = 7f;

        [Header("View")]
        [SerializeField] private GameObject _markPrefab;

        public override int LevelCount => _levels.Length;

        public float Radius => _radius;

        public GameObject MarkPrefab => _markPrefab;

        public HunterMarkLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(HunterMarkConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' hunter mark '{name}'";

            if (_markPrefab == null)
                throw new InvalidOperationException($"{label} has no mark prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                HunterMarkLevel entry = Level(level);

                if (entry == null || entry.Targets < 1 || entry.DurationSeconds <= 0f || entry.DamageTakenBonus < 0f || entry.CritChanceBonus < 0f)
                    throw new InvalidOperationException(
                        $"{label} level {level} must mark at least one enemy for a positive time with non-negative bonuses.");
            }
        }
    }

    [Serializable]
    public sealed class HunterMarkLevel
    {
        [SerializeField, Min(1)] private int _targets = 3;
        [SerializeField, Min(0.1f)] private float _durationSeconds = 4f;
        [SerializeField, Min(0f)] private float _damageTakenBonus = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _critChanceBonus = 0.1f;

        public int Targets => _targets;

        public float DurationSeconds => _durationSeconds;

        public float DamageTakenBonus => _damageTakenBonus;

        public float CritChanceBonus => _critChanceBonus;
    }
}
