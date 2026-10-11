using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "SfxConfig", menuName = "Game/Sfx Config")]
    public sealed class SfxConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Cue
        {
            [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
            [SerializeField, Range(0f, 1f)] private float _volume = 0.7f;
            [SerializeField] private float _minPitch = 0.9f;
            [SerializeField] private float _maxPitch = 1.1f;
            [SerializeField] private int _maxConcurrent = 5;
            [SerializeField] private int _maxPerFrame = 2;

            public AudioClip[] Clips => _clips;

            public float Volume => _volume;

            public float MinPitch => _minPitch;

            public float MaxPitch => _maxPitch;

            public int MaxConcurrent => _maxConcurrent;

            public int MaxPerFrame => _maxPerFrame;
        }

        [Header("Voices")]
        [SerializeField] private int _voiceCount = 12;
        [SerializeField, Range(0f, 1f)] private float _spatialBlend = 0.5f;
        [SerializeField] private float _minDistance = 20f;
        [SerializeField] private float _maxDistance = 50f;

        [Header("Cues")]
        [SerializeField] private Cue _enemyHit = new Cue();
        [SerializeField] private Cue _enemyDeath = new Cue();

        public int VoiceCount => _voiceCount;

        public float SpatialBlend => _spatialBlend;

        public float MinDistance => _minDistance;

        public float MaxDistance => _maxDistance;

        public Cue EnemyHit => _enemyHit;

        public Cue EnemyDeath => _enemyDeath;
    }
}
