using Game.Core;

namespace Game.UI
{
    public sealed class Announcements : IAnnouncementSink
    {
        private string _pending;

        public void Announce(string text)
        {
            _pending = text;
        }

        public bool TryTake(out string text)
        {
            text = _pending;
            _pending = null;

            return string.IsNullOrEmpty(text) == false;
        }
    }
}
