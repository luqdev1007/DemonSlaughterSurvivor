using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "SwordTrailConfig", menuName = "Game/Sword Trail Config")]
    public sealed class SwordTrailConfig : ScriptableObject
    {
        [SerializeField] private Material _material;
        [SerializeField] private float _fadeSeconds = 0.15f;
        [SerializeField] private float _minTipSpeed = 3f;
        [SerializeField] private int _maxSamples = 64;
        [SerializeField] private Gradient _color = new Gradient();
        [SerializeField] private Gradient _berserkColor = new Gradient();
        [SerializeField] private string[] _attackStates = Array.Empty<string>();

        public Material Material => _material;

        public float FadeSeconds => _fadeSeconds;

        public float MinTipSpeed => _minTipSpeed;

        public int MaxSamples => _maxSamples;

        public Gradient Color => _color;

        public Gradient BerserkColor => _berserkColor;

        public string[] AttackStates => _attackStates;
    }
}
