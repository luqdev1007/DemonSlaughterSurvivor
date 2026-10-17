using System.Collections.Generic;

namespace Game.Core
{
    public interface IItemChoiceSink
    {
        void Show(IReadOnlyList<ItemOfferData> offers);

        void Hide();
    }
}
