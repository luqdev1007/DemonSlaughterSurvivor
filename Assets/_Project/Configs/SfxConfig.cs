using System;
using UnityEngine;
using UnityEngine.Serialization;

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
            [SerializeField] private float _delaySeconds;
            [SerializeField] private int _maxConcurrent = 5;
            [SerializeField] private int _maxPerFrame = 2;

            public AudioClip[] Clips => _clips;

            public float Volume => _volume;

            public float MinPitch => _minPitch;

            public float MaxPitch => _maxPitch;

            public float DelaySeconds => _delaySeconds;

            public int MaxConcurrent => _maxConcurrent;

            public int MaxPerFrame => _maxPerFrame;
        }

        [Serializable]
        public sealed class AttackCue
        {
            [SerializeField] private string _animatorTrigger;
            [SerializeField] private Cue _cue = new Cue();

            public string AnimatorTrigger => _animatorTrigger;

            public Cue Cue => _cue;
        }

        [Header("Voices")]
        [SerializeField] private int _voiceCount = 16;
        [SerializeField, Range(0f, 1f)] private float _spatialBlend = 0.5f;
        [SerializeField] private float _minDistance = 20f;
        [SerializeField] private float _maxDistance = 50f;

        [Header("Cues")]
        [FormerlySerializedAs("_enemyHit")]
        [SerializeField] private Cue _swordHit = new Cue();
        [SerializeField] private Cue _woodHit = new Cue();
        [SerializeField] private Cue _enemyDeath = new Cue();
        [SerializeField] private Cue _gemPickup = new Cue();
        [SerializeField] private Cue _dash = new Cue();
        [SerializeField] private Cue _heroDeath = new Cue();
        [SerializeField] private AttackCue[] _attacks = Array.Empty<AttackCue>();

        public int VoiceCount => _voiceCount;

        public float SpatialBlend => _spatialBlend;

        public float MinDistance => _minDistance;

        public float MaxDistance => _maxDistance;

        public Cue SwordHit => _swordHit;

        public Cue WoodHit => _woodHit;

        public Cue EnemyDeath => _enemyDeath;

        public Cue GemPickup => _gemPickup;

        public Cue Dash => _dash;

        public Cue HeroDeath => _heroDeath;

        public AttackCue[] Attacks => _attacks;
    }
}
