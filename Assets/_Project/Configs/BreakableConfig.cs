using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "BreakableConfig", menuName = "Game/Content/Breakable Config")]
    public sealed class BreakableConfig : ContentConfig
    {
        [SerializeField] private GameObject _viewPrefab;
        [SerializeField] private float _maxHealth = 40f;
        [SerializeField] private float _bodyRadius = 0.4f;
        [SerializeField] private float _bodyHeight = 1f;

        [Header("Drop")]
        [SerializeField] private HealOrbConfig _healOrb;
        [SerializeField, Range(0f, 1f)] private float _healOrbChance = 0.7f;

        public GameObject ViewPrefab => _viewPrefab;

        public float MaxHealth => _maxHealth;

        public float BodyRadius => _bodyRadius;

        public float BodyHeight => _bodyHeight;

        public HealOrbConfig HealOrb => _healOrb;

        public float HealOrbChance => _healOrbChance;
    }
}
