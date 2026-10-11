using Game.Core;
using R3;
using System;

namespace Game.UI
{
    public sealed class RunResult : IRunResultSink, IDisposable
    {
        private readonly ReactiveProperty<bool> _victory = new ReactiveProperty<bool>();

        public ReadOnlyReactiveProperty<bool> Victory => _victory;

        public void ShowVictory()
        {
            _victory.Value = true;
        }

        public void Dispose()
        {
            _victory.Dispose();
        }
    }
}
