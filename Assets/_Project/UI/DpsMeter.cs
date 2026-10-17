using Game.Core;
using UnityEngine;

namespace Game.UI
{
    public sealed class DpsMeter : IDamageDealtSink
    {
        public const float BucketSeconds = 0.5f;
        public const float MaxWindowSeconds = 120f;

        private readonly float[] _buckets = new float[Mathf.CeilToInt(MaxWindowSeconds / BucketSeconds) + 1];

        private float _windowSeconds = 15f;
        private float _time;
        private long _bucket;

        public float WindowSeconds => _windowSeconds;

        public void SetWindow(float seconds)
        {
            _windowSeconds = Mathf.Clamp(seconds, BucketSeconds, MaxWindowSeconds);
        }

        public void Advance(float simulationTime)
        {
            long bucket = (long)(simulationTime / BucketSeconds);

            if (bucket > _bucket)
            {
                long steps = bucket - _bucket;

                if (steps >= _buckets.Length)
                    System.Array.Clear(_buckets, 0, _buckets.Length);
                else
                {
                    for (long step = 1; step <= steps; step++)
                        _buckets[(_bucket + step) % _buckets.Length] = 0f;
                }

                _bucket = bucket;
            }

            _time = simulationTime;
        }

        public void Record(float amount)
        {
            _buckets[_bucket % _buckets.Length] += amount;
        }

        public float Dps()
        {
            int count = Mathf.Min(Mathf.CeilToInt(_windowSeconds / BucketSeconds), _buckets.Length);
            float sum = 0f;

            for (int index = 0; index < count; index++)
            {
                long bucket = _bucket - index;

                if (bucket < 0)
                    break;

                sum += _buckets[bucket % _buckets.Length];
            }

            float span = Mathf.Min(_windowSeconds, Mathf.Max(_time, BucketSeconds));

            return sum / span;
        }
    }
}
