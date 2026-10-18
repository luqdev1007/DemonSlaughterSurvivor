using Game.Configs;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct Height
    {
        public float Value;
        public float Clearance;
    }

    public struct Airborne { }

    public enum BackleapPhase
    {
        Leap = 1,
        Hover = 2,
        Fall = 3,
        Hop = 4,
        Dive = 5
    }

    public struct BackleapStats
    {
        public BackleapConfig Config;
        public bool Held;
    }

    public struct BackleapRequest { public float Age; }

    public struct BackleapCooldown { public float Remaining; }

    public struct Backleaping
    {
        public BackleapPhase Phase;
        public int Ticks;
        public int PhaseTicks;
        public int HoverTicks;
        public Vector3 Direction;
        public float Speed;
        public float FallFrom;
    }
}
