using Game.Core;
using R3;
using System;

namespace Game.UI
{
    public sealed class UpgradeChoice : IUpgradeChoiceSink, IDisposable
    {
        private readonly UpgradeOffer[] _offers = new UpgradeOffer[3];
        private readonly Subject<Unit> _changed = new Subject<Unit>();

        public bool IsOpen { get; private set; }

        public int Count { get; private set; }

        public Observable<Unit> Changed => _changed;

        public UpgradeOffer Offer(int index)
        {
            return _offers[index];
        }

        public void Show(UpgradeOffer first, UpgradeOffer second, UpgradeOffer third, int count)
        {
            _offers[0] = first;
            _offers[1] = second;
            _offers[2] = third;

            Count = count;
            IsOpen = true;

            _changed.OnNext(Unit.Default);
        }

        public void Hide()
        {
            Count = 0;
            IsOpen = false;

            _changed.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _changed.Dispose();
        }
    }
}
