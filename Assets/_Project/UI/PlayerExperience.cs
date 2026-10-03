using Game.Core;
using R3;
using System;

namespace Game.UI
{
    public sealed class PlayerExperience : IExperienceSink, IDisposable
    {
        private readonly Subject<Unit> _changed = new Subject<Unit>();

        public int Level { get; private set; }

        public int Current { get; private set; }

        public int Required { get; private set; }

        public float Fraction => Required > 0 ? (float)Current / Required : 0f;

        public Observable<Unit> Changed => _changed;

        public void Publish(int level, int current, int required)
        {
            if (level == Level && current == Current && required == Required)
                return;

            Level = level;
            Current = current;
            Required = required;

            _changed.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _changed.Dispose();
        }
    }
}
