using Game.Core;
using UnityEngine;

namespace Game.UI
{
    public sealed class DpsMeter : IDamageDealtSink
    {
        public const float BucketSeconds = 0.5f;
        public const float MaxWindowSeconds = 120f;
        public const float BestWindowSeconds = 5f;

        private readonly float[] _buckets = new float[Mathf.CeilToInt(MaxWindowSeconds / BucketSeconds) + 1];

        private float _windowSeconds = 15f;
        private float _time;
        private long _bucket;
        private float _total;
        private float _biggest;
        private float _bestDps;

        public float WindowSeconds => _windowSeconds;

        public float Total => _total;

        public float Biggest => _biggest;

        public float BestDps => _bestDps > 0f ? _bestDps : _total / Mathf.Max(_time, BestWindowSeconds);

        public void SetWindow(float seconds)
        {
            _windowSeconds = Mathf.Clamp(seconds, BucketSeconds, MaxWindowSeconds);
        }

        public void Advance(float simulationTime)
        {
            long bucket = (long)(simulationTime / BucketSeconds);

            if (bucket > _bucket)
            {
                TrackBest();

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

            _total += amount;
            _biggest = Mathf.Max(_biggest, amount);
        }

        public float Dps()
        {
            float span = Mathf.Min(_windowSeconds, Mathf.Max(_time, BucketSeconds));

            return Sum(Mathf.CeilToInt(_windowSeconds / BucketSeconds)) / span;
        }

        private void TrackBest()
        {
            int count = Mathf.CeilToInt(BestWindowSeconds / BucketSeconds);

            if (_bucket + 1 < count)
                return;

            _bestDps = Mathf.Max(_bestDps, Sum(count) / BestWindowSeconds);
        }

        private float Sum(int buckets)
        {
            int count = Mathf.Min(buckets, _buckets.Length);
            float sum = 0f;

            for (int index = 0; index < count; index++)
            {
                long bucket = _bucket - index;

                if (bucket < 0)
                    break;

                sum += _buckets[bucket % _buckets.Length];
            }

            return sum;
        }
    }
}
