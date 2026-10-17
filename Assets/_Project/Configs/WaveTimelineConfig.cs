using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class WeightedEnemy
    {
        [SerializeField] private EnemyConfig _enemy;
        [SerializeField] private float _weight = 1f;

        public EnemyConfig Enemy => _enemy;

        public float Weight => _weight;
    }

    [Serializable]
    public sealed class GuaranteedSpawn
    {
        [SerializeField] private float _offsetSeconds;
        [SerializeField] private EnemyConfig _enemy;
        [SerializeField] private int _count = 1;

        public float OffsetSeconds => _offsetSeconds;

        public EnemyConfig Enemy => _enemy;

        public int Count => _count;
    }

    [Serializable]
    public sealed class Wave
    {
        [SerializeField] private float _durationSeconds = 60f;
        [SerializeField] private float _spawnRatePerSecond;
        [SerializeField] private int _liveCap = 200;
        [SerializeField] private int _liveFloor;
        [SerializeField] private float _refillRatePerSecond;
        [SerializeField] private WeightedEnemy[] _enemies = Array.Empty<WeightedEnemy>();
        [SerializeField] private GuaranteedSpawn[] _guaranteed = Array.Empty<GuaranteedSpawn>();

        public float DurationSeconds => _durationSeconds;

        public float SpawnRatePerSecond => _spawnRatePerSecond;

        public int LiveCap => _liveCap;

        public int LiveFloor => _liveFloor;

        public float RefillRatePerSecond => _refillRatePerSecond;

        public IReadOnlyList<WeightedEnemy> Enemies => _enemies;

        public IReadOnlyList<GuaranteedSpawn> Guaranteed => _guaranteed;
    }

    [CreateAssetMenu(fileName = "WaveTimeline", menuName = "Game/Content/Wave Timeline")]
    public sealed class WaveTimelineConfig : ContentConfig
    {
        [SerializeField] private float _spawnRadius = 18f;
        [SerializeField] private Wave[] _waves = Array.Empty<Wave>();
        [SerializeField, Min(0)] private int _prewarmCapPerEnemy = 100;

        [Header("Elites")]
        [SerializeField, Range(0f, 1f)] private float _eliteChance;
        [SerializeField] private EliteAffixConfig[] _eliteAffixes = Array.Empty<EliteAffixConfig>();

        [Header("Items")]
        [SerializeField] private ItemLootConfig _itemLoot;

        public float SpawnRadius => _spawnRadius;

        public float EliteChance => _eliteChance;

        public ItemLootConfig ItemLoot => _itemLoot;

        public IReadOnlyList<EliteAffixConfig> EliteAffixes => _eliteAffixes ?? Array.Empty<EliteAffixConfig>();

        public float MaxEliteSpeedFactor
        {
            get
            {
                float max = 1f;

                foreach (EliteAffixConfig affix in EliteAffixes)
                {
                    if (affix != null)
                        max = Mathf.Max(max, affix.MoveSpeedFactor);
                }

                return max;
            }
        }

        public float MaxEliteScale
        {
            get
            {
                float max = 1f;

                foreach (EliteAffixConfig affix in EliteAffixes)
                {
                    if (affix != null)
                        max = Mathf.Max(max, affix.Scale);
                }

                return max;
            }
        }

        public int PrewarmCapPerEnemy => _prewarmCapPerEnemy;

        public IReadOnlyList<Wave> Waves => _waves;
    }
}
