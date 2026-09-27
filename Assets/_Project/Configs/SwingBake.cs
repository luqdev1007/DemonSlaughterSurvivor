using System;
using UnityEngine;

namespace Game.Configs
{
    public sealed class SwingBake : ScriptableObject
    {
        [SerializeField] private AnimationClip _clip;
        [SerializeField] private float _sampleRate;
        [SerializeField] private float _clipLength;
        [SerializeField] private uint _clipHash;
        [SerializeField] private Vector3[] _hands = Array.Empty<Vector3>();
        [SerializeField] private Vector3[] _tips = Array.Empty<Vector3>();

        public AnimationClip Clip => _clip;

        public float SampleRate => _sampleRate;

        public float ClipLength => _clipLength;

        public uint ClipHash => _clipHash;

        public int SampleCount => _hands.Length;

        public void SampleAt(float clipTime, out Vector3 hand, out Vector3 tip)
        {
            int last = _hands.Length - 1;

            if (last < 0)
                throw new InvalidOperationException($"{nameof(SwingBake)} '{name}' has no samples; bake it before use.");

            float position = clipTime * _sampleRate;

            if (position <= 0f)
            {
                hand = _hands[0];
                tip = _tips[0];

                return;
            }

            if (position >= last)
            {
                hand = _hands[last];
                tip = _tips[last];

                return;
            }

            int index = (int)position;
            float fraction = position - index;

            hand = Vector3.LerpUnclamped(_hands[index], _hands[index + 1], fraction);
            tip = Vector3.LerpUnclamped(_tips[index], _tips[index + 1], fraction);
        }

#if UNITY_EDITOR
        public void Overwrite(AnimationClip clip, float sampleRate, float clipLength, uint clipHash, Vector3[] hands, Vector3[] tips)
        {
            _clip = clip;
            _sampleRate = sampleRate;
            _clipLength = clipLength;
            _clipHash = clipHash;
            _hands = hands;
            _tips = tips;
        }
#endif
    }
}
