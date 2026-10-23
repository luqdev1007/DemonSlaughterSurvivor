using Game.Core;
using R3;
using System;

namespace Game.UI
{
    public sealed class RunResult : IRunResultSink, IRunResultInput, IDisposable
    {
        private readonly ReactiveProperty<bool> _victory = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _runEnd = new ReactiveProperty<bool>();

        private bool _exitRequested;
        private RunExitChoice _exitChoice;

        public ReadOnlyReactiveProperty<bool> Victory => _victory;

        public ReadOnlyReactiveProperty<bool> RunEnd => _runEnd;

        public RunReport Report { get; private set; }

        public RunRewards Rewards { get; private set; }

        public void ShowVictory()
        {
            _victory.Value = true;
        }

        public void ShowRunEnd(RunReport report, RunRewards rewards)
        {
            Report = report;
            Rewards = rewards;
            _exitRequested = false;

            _runEnd.Value = true;
        }

        public void RequestExit(RunExitChoice choice)
        {
            if (_runEnd.Value == false)
                return;

            _exitChoice = choice;
            _exitRequested = true;
        }

        public bool TryConsumeExit(out RunExitChoice choice)
        {
            choice = _exitChoice;

            if (_exitRequested == false)
                return false;

            _exitRequested = false;

            return true;
        }

        public void Dispose()
        {
            _victory.Dispose();
            _runEnd.Dispose();
        }
    }
}
