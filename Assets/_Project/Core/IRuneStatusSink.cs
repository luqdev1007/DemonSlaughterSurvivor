using UnityEngine;

namespace Game.Core
{
    public interface IRuneStatusSink
    {
        void Publish(string name, Color color, float remainingSeconds, float totalSeconds);

        void Clear();
    }
}
