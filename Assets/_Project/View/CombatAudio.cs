using Game.Configs;
using System;
using UnityEngine;

namespace Game.View
{
    public sealed class CombatAudio : IDisposable
    {
        private sealed class CueState
        {
            public SfxConfig.Cue Cue;
            public int Frame = -1;
            public int PlayedThisFrame;
            public int LastClip = -1;
        }

        private readonly System.Random _random = new System.Random();
        private readonly GameObject _root;
        private readonly AudioSource[] _voices;
        private readonly CueState[] _voiceCues;
        private readonly CueState _hit;
        private readonly CueState _death;

        private bool _disposed;

        public CombatAudio(LevelConfig level)
        {
            FeedbackConfig feedback = level.Feedback;

            if (feedback == null || feedback.Sfx == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{level.Id}' has no {nameof(SfxConfig)} in its {nameof(FeedbackConfig)}.");

            SfxConfig config = feedback.Sfx;

            Validate(config, config.EnemyHit, nameof(SfxConfig.EnemyHit));
            Validate(config, config.EnemyDeath, nameof(SfxConfig.EnemyDeath));

            if (config.VoiceCount <= 0 || config.MinDistance <= 0f || config.MaxDistance < config.MinDistance)
                throw new InvalidOperationException(
                    $"{nameof(SfxConfig)} '{config.name}' needs at least one voice and 0 < min distance <= max distance.");

            _hit = new CueState { Cue = config.EnemyHit };
            _death = new CueState { Cue = config.EnemyDeath };

            _root = new GameObject("Combat Audio");
            _voices = new AudioSource[config.VoiceCount];
            _voiceCues = new CueState[config.VoiceCount];

            for (int index = 0; index < _voices.Length; index++)
            {
                GameObject voice = new GameObject("Voice " + index);
                voice.transform.SetParent(_root.transform, false);

                AudioSource source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = config.SpatialBlend;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = config.MinDistance;
                source.maxDistance = config.MaxDistance;
                source.dopplerLevel = 0f;

                _voices[index] = source;
            }
        }

        public int VoiceCount => _voices.Length;

        public void PlayEnemyHit(Vector3 position)
        {
            Play(_hit, position);
        }

        public void PlayEnemyDeath(Vector3 position)
        {
            Play(_death, position);
        }

        private void Play(CueState state, Vector3 position)
        {
            if (_disposed)
                return;

            SfxConfig.Cue cue = state.Cue;

            if (cue.Clips.Length == 0)
                return;

            int frame = Time.frameCount;

            if (state.Frame != frame)
            {
                state.Frame = frame;
                state.PlayedThisFrame = 0;
            }

            if (state.PlayedThisFrame >= cue.MaxPerFrame)
                return;

            int free = -1;
            int playing = 0;

            for (int index = 0; index < _voices.Length; index++)
            {
                if (_voices[index].isPlaying == false)
                {
                    if (free < 0)
                        free = index;

                    continue;
                }

                if (_voiceCues[index] == state)
                    playing++;
            }

            if (free < 0 || playing >= cue.MaxConcurrent)
                return;

            AudioClip clip = cue.Clips[PickClip(state)];

            if (clip == null)
                return;

            AudioSource source = _voices[free];
            source.transform.position = position;
            source.clip = clip;
            source.volume = cue.Volume;
            source.pitch = Mathf.Lerp(cue.MinPitch, cue.MaxPitch, (float)_random.NextDouble());
            source.Play();

            _voiceCues[free] = state;
            state.PlayedThisFrame++;
        }

        private int PickClip(CueState state)
        {
            int count = state.Cue.Clips.Length;

            if (count == 1)
                return 0;

            int pick = state.LastClip < 0 ? _random.Next(count) : _random.Next(count - 1);

            if (state.LastClip >= 0 && pick >= state.LastClip)
                pick++;

            state.LastClip = pick;

            return pick;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        private static void Validate(SfxConfig config, SfxConfig.Cue cue, string name)
        {
            if (cue == null)
                throw new InvalidOperationException($"{nameof(SfxConfig)} '{config.name}' has no {name} cue.");

            if (cue.MinPitch <= 0f || cue.MaxPitch < cue.MinPitch)
                throw new InvalidOperationException(
                    $"{nameof(SfxConfig)} '{config.name}': {name} needs 0 < min pitch <= max pitch.");

            if (cue.MaxConcurrent < 0 || cue.MaxPerFrame < 0)
                throw new InvalidOperationException(
                    $"{nameof(SfxConfig)} '{config.name}': {name} has a negative limit. Use 0 to switch the cue off.");
        }
    }
}
