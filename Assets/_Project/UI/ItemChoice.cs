using Game.Core;
using R3;
using System;
using System.Collections.Generic;

namespace Game.UI
{
    public sealed class ItemChoice : IItemChoiceSink, IDisposable
    {
        private readonly List<ItemOfferData> _offers = new List<ItemOfferData>();
        private readonly Subject<Unit> _changed = new Subject<Unit>();

        public bool IsOpen { get; private set; }

        public IReadOnlyList<ItemOfferData> Offers => _offers;

        public Observable<Unit> Changed => _changed;

        public void Show(IReadOnlyList<ItemOfferData> offers)
        {
            _offers.Clear();

            for (int index = 0; index < offers.Count; index++)
                _offers.Add(offers[index]);

            IsOpen = true;
            _changed.OnNext(Unit.Default);
        }

        public void Hide()
        {
            _offers.Clear();
            IsOpen = false;
            _changed.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _changed.Dispose();
        }
    }
}
