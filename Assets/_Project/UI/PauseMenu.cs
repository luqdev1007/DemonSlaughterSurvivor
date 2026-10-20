using Game.Core;
using R3;
using System;

namespace Game.UI
{
    public sealed class PauseMenu : IPauseMenuSink, IPauseMenuInput, IDisposable
    {
        private readonly Subject<Unit> _changed = new Subject<Unit>();

        private bool _resumeRequested;
        private bool _exitRequested;

        public bool IsOpen { get; private set; }

        public Observable<Unit> Changed => _changed;

        public void Show()
        {
            IsOpen = true;
            _resumeRequested = false;
            _exitRequested = false;

            _changed.OnNext(Unit.Default);
        }

        public void Hide()
        {
            IsOpen = false;
            _resumeRequested = false;
            _exitRequested = false;

            _changed.OnNext(Unit.Default);
        }

        public void RequestResume()
        {
            if (IsOpen == false)
                return;

            _resumeRequested = true;
        }

        public void RequestExit()
        {
            if (IsOpen == false)
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

        public bool ConsumeResumeRequested()
        {
            if (_resumeRequested == false)
                return false;

            _resumeRequested = false;

            return true;
        }

        public void Dispose()
        {
            _changed.Dispose();
        }
    }
}
