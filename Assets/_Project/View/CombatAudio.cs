using Game.Configs;
using System;
using UnityEngine;

namespace Game.View
{
    public sealed class CombatAudio : IDisposable
    {
        private sealed class CueState
        {
            public string Trigger;
            public SfxConfig.Cue Cue;
            public int Frame = -1;
            public int PlayedThisFrame;
            public int LastClip = -1;
        }

        private readonly System.Random _random = new System.Random();
        private readonly GameObject _root;
        private readonly AudioSource[] _voices;
        private readonly CueState[] _voiceCues;
        private readonly float[] _busyUntil;
        private readonly CueState _swordHit;
        private readonly CueState _woodHit;
        private readonly CueState _enemyDeath;
        private readonly CueState _gemPickup;
        private readonly CueState _dash;
        private readonly CueState _heroDeath;
        private readonly CueState[] _attacks;

        private bool _disposed;

        public CombatAudio(LevelConfig level)
        {
            FeedbackConfig feedback = level.Feedback;

            if (feedback == null || feedback.Sfx == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{level.Id}' has no {nameof(SfxConfig)} in its {nameof(FeedbackConfig)}.");

            SfxConfig config = feedback.Sfx;

            if (config.VoiceCount <= 0 || config.MinDistance <= 0f || config.MaxDistance < config.MinDistance)
                throw new InvalidOperationException(
                    $"{nameof(SfxConfig)} '{config.name}' needs at least one voice and 0 < min distance <= max distance.");

            _swordHit = CreateState(config, config.SwordHit, nameof(SfxConfig.SwordHit), null);
            _woodHit = CreateState(config, config.WoodHit, nameof(SfxConfig.WoodHit), null);
            _enemyDeath = CreateState(config, config.EnemyDeath, nameof(SfxConfig.EnemyDeath), null);
            _gemPickup = CreateState(config, config.GemPickup, nameof(SfxConfig.GemPickup), null);
            _dash = CreateState(config, config.Dash, nameof(SfxConfig.Dash), null);
            _heroDeath = CreateState(config, config.HeroDeath, nameof(SfxConfig.HeroDeath), null);

            SfxConfig.AttackCue[] attacks = config.Attacks ?? Array.Empty<SfxConfig.AttackCue>();
            _attacks = new CueState[attacks.Length];

            for (int index = 0; index < attacks.Length; index++)
            {
                SfxConfig.AttackCue attack = attacks[index];

                if (attack == null || string.IsNullOrEmpty(attack.AnimatorTrigger))
                    throw new InvalidOperationException(
                        $"{nameof(SfxConfig)} '{config.name}': attack cue {index} has no animator trigger.");

                _attacks[index] = CreateState(config, attack.Cue, $"attack cue {index} ({attack.AnimatorTrigger})", attack.AnimatorTrigger);
            }

            _root = new GameObject("Combat Audio");
            _voices = new AudioSource[config.VoiceCount];
            _voiceCues = new CueState[config.VoiceCount];
            _busyUntil = new float[config.VoiceCount];

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

        public void PlaySwordHit(Vector3 position)
        {
            Play(_swordHit, position);
        }

        public void PlayWoodHit(Vector3 position)
        {
            Play(_woodHit, position);
        }

        public void PlayEnemyDeath(Vector3 position)
        {
            Play(_enemyDeath, position);
        }

        public void PlayGemPickup(Vector3 position)
        {
            Play(_gemPickup, position);
        }

        public void PlayDash(Vector3 position)
        {
            Play(_dash, position);
        }

        public void PlayHeroDeath(Vector3 position)
        {
            Play(_heroDeath, position);
        }

        public void PlayAttack(string trigger, Vector3 position)
        {
            for (int index = 0; index < _attacks.Length; index++)
            {
                if (string.Equals(_attacks[index].Trigger, trigger, StringComparison.Ordinal))
                    Play(_attacks[index], position);
            }
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

            float now = Time.unscaledTime;
            int free = -1;
            int playing = 0;

            for (int index = 0; index < _voices.Length; index++)
            {
                if (_busyUntil[index] <= now)
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

            float pitch = Mathf.Lerp(cue.MinPitch, cue.MaxPitch, (float)_random.NextDouble());

            AudioSource source = _voices[free];
            source.transform.position = position;
            source.clip = clip;
            source.volume = cue.Volume;
            source.pitch = pitch;

            if (cue.DelaySeconds > 0f)
                source.PlayDelayed(cue.DelaySeconds);
            else
                source.Play();

            _voiceCues[free] = state;
            _busyUntil[free] = now + cue.DelaySeconds + clip.length / pitch;
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
            UnityEngine.Debug.Log($"[RunEndTrace] CombatAudio.Dispose, frame {UnityEngine.Time.frameCount}");

            if (_disposed)
                return;

            _disposed = true;

            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        private static CueState CreateState(SfxConfig config, SfxConfig.Cue cue, string name, string trigger)
        {
            if (cue == null)
                throw new InvalidOperationException($"{nameof(SfxConfig)} '{config.name}' has no {name} cue.");

            if (cue.MinPitch <= 0f || cue.MaxPitch < cue.MinPitch)
                throw new InvalidOperationException(
                    $"{nameof(SfxConfig)} '{config.name}': {name} needs 0 < min pitch <= max pitch.");

            if (cue.MaxConcurrent < 0 || cue.MaxPerFrame < 0 || cue.DelaySeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(SfxConfig)} '{config.name}': {name} has a negative limit or delay. Use 0 limits to switch the cue off.");

            return new CueState { Cue = cue, Trigger = trigger };
        }
    }
}
