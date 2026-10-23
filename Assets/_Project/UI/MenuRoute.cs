using Game.Core;

namespace Game.UI
{
    public sealed class MenuRoute : IMenuRoute
    {
        private bool _upgradeRequested;

        public void RequestUpgrade()
        {
            _upgradeRequested = true;
        }

        public bool ConsumeUpgradeRequest()
        {
            bool requested = _upgradeRequested;
            _upgradeRequested = false;

            return requested;
        }
    }
}
