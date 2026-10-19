using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ElementalWeaknessConfig", menuName = "Game/Content/Elemental Weakness Config")]
    public sealed class ElementalWeaknessConfig : PerkBehaviourConfig
    {
        [SerializeField] private ElementalWeaknessLevel[] _levels = Array.Empty<ElementalWeaknessLevel>();
        [SerializeField, Min(1f)] private float _range = 15f;
        [SerializeField, Min(0.1f)] private float _markSeconds = 6f;
        [SerializeField, Min(0.5f)] private float _explosionRadius = 3f;
        [SerializeField, Min(0.05f)] private float _explosionFxSeconds = 1.2f;
        [SerializeField, Min(0f)] private float _markHeight = 2.3f;

        [Header("View")]
        [SerializeField] private GameObject _fireMarkPrefab;
        [SerializeField] private GameObject _iceMarkPrefab;
        [SerializeField] private GameObject _arcaneMarkPrefab;
        [SerializeField] private GameObject _fireExplosionPrefab;
        [SerializeField] private GameObject _iceExplosionPrefab;
        [SerializeField] private GameObject _arcaneExplosionPrefab;

        public override int LevelCount => _levels.Length;

        public float Range => _range;

        public float MarkSeconds => _markSeconds;

        public float ExplosionRadius => _explosionRadius;

        public float ExplosionFxSeconds => _explosionFxSeconds;

        public float MarkHeight => _markHeight;

        public GameObject FireMarkPrefab => _fireMarkPrefab;

        public GameObject IceMarkPrefab => _iceMarkPrefab;

        public GameObject ArcaneMarkPrefab => _arcaneMarkPrefab;

        public GameObject FireExplosionPrefab => _fireExplosionPrefab;

        public GameObject IceExplosionPrefab => _iceExplosionPrefab;

        public GameObject ArcaneExplosionPrefab => _arcaneExplosionPrefab;

        public ElementalWeaknessLevel Level(int level)
        {
            if (level < 1 || level > LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"{nameof(ElementalWeaknessConfig)} '{name}' has levels 1..{LevelCount}.");

            return _levels[level - 1];
        }

        public override void Validate(PerkConfig perk)
        {
            string label = $"{nameof(PerkConfig)} '{perk.Id}' elemental weakness '{name}'";

            if (_fireMarkPrefab == null || _iceMarkPrefab == null || _arcaneMarkPrefab == null || _fireExplosionPrefab == null || _iceExplosionPrefab == null || _arcaneExplosionPrefab == null)
                throw new InvalidOperationException($"{label} is missing a view prefab.");

            for (int level = 1; level <= LevelCount; level++)
            {
                ElementalWeaknessLevel entry = Level(level);

                if (entry == null || entry.IntervalSeconds <= 0 || entry.MarksPerPulse <= 0 || entry.DamageBonus <= 0 || entry.ExplosionDamage <= 0)
                    throw new InvalidOperationException($"{label} level {level} must have positive values.");
            }
        }
    }

    [Serializable]
    public sealed class ElementalWeaknessLevel
    {
        [SerializeField, Min(0.1f)] private float _intervalSeconds = 3f;
        [SerializeField, Min(1)] private int _marksPerPulse = 1;
        [SerializeField, Min(0.01f)] private float _damageBonus = 0.5f;
        [SerializeField, Min(0.01f)] private float _explosionDamage = 30f;

        public float IntervalSeconds => _intervalSeconds;

        public int MarksPerPulse => _marksPerPulse;

        public float DamageBonus => _damageBonus;

        public float ExplosionDamage => _explosionDamage;
    }
}
