using Game.Core;
using System;

namespace Game.Simulation.Services
{
    public struct StatTotals
    {
        public float Flat;
        public float Increased;
        public float More;

        public static StatTotals Neutral => new StatTotals { Flat = 0f, Increased = 0f, More = 1f };
    }

    public static class StatFormula
    {
        public static void Accumulate(ref StatTotals totals, StatOp op, float value)
        {
            switch (op)
            {
                case StatOp.Flat:
                    totals.Flat += value;
                    return;
                case StatOp.Increased:
                    totals.Increased += value;
                    return;
                case StatOp.More:
                    totals.More *= 1f + value;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(op),
                        op,
                        "Unknown stat operation. A modifier with an unknown operation would be dropped silently, " +
                        "so the stat would look unmodified while its source believes it applied.");
            }
        }

        public static float Evaluate(float baseValue, in StatTotals totals)
        {
            return (baseValue + totals.Flat) * (1f + totals.Increased) * totals.More;
        }
    }
}
