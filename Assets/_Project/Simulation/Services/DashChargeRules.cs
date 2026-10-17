using Game.Simulation.Components;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class DashChargeRules
    {
        public const string SlowSourceId = "dash.charge";

        public static float Strength(in DashStats stats, int chargeTicks, float delta)
        {
            float held = chargeTicks * delta;

            if (held <= stats.TapSeconds)
                return 0f;

            float span = Mathf.Max(1e-4f, stats.MaxChargeSeconds - stats.TapSeconds);
            float linear = Mathf.Clamp01((held - stats.TapSeconds) / span);

            return stats.ChargeCurve != null ? Mathf.Clamp01(stats.ChargeCurve.Evaluate(linear)) : linear;
        }

        public static float DamageMultiplier(in Dashing dashing)
        {
            return dashing.DamageMultiplier > 0f ? dashing.DamageMultiplier : 1f;
        }

        public static float Scale(float atFullCharge, float strength)
        {
            return Mathf.Lerp(1f, atFullCharge, strength);
        }
    }
}
