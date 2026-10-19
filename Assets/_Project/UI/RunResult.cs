using Game.Core;
using R3;
using System;

namespace Game.UI
{
    public sealed class RunResult : IRunResultSink, IRunResultInput, IDisposable
    {
        private readonly ReactiveProperty<bool> _victory = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _summary = new ReactiveProperty<bool>();

        private bool _exitRequested;

        public ReadOnlyReactiveProperty<bool> Victory => _victory;

        public ReadOnlyReactiveProperty<bool> Summary => _summary;

        public float RunSeconds { get; private set; }

        public void ShowVictory()
        {
            _victory.Value = true;
        }

        public void ShowSummary(float runSeconds)
        {
            RunSeconds = runSeconds;
            _exitRequested = false;

            _summary.Value = true;
        }

        public void RequestExit()
        {
            if (_summary.Value == false)
                return;

            _exitRequested = true;
        }

        public bool ConsumeExitRequested()
        {
            if (_exitRequested == false)
                return false;

            _exitRequested = false;

            return true;
        }

        public void Dispose()
        {
            _victory.Dispose();
            _summary.Dispose();
        }
    }
}
