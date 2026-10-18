using Game.Core;
using UnityEngine;

namespace Game.UI
{
    public sealed class RuneStatus : IRuneStatusSink
    {
        public bool Active { get; private set; }

        public string Name { get; private set; }

        public Color Color { get; private set; }

        public float Remaining { get; private set; }

        public float Total { get; private set; }

        public void Publish(string name, Color color, float remainingSeconds, float totalSeconds)
        {
            Active = true;
            Name = name;
            Color = color;
            Remaining = remainingSeconds;
            Total = totalSeconds;
        }

        public void Clear()
        {
            Active = false;
        }
    }
}
