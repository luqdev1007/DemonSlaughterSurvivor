using Game.Simulation.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class PushFromPoint
    {
        private const float CoincidenceEpsilonSquared = 1e-8f;

        public static Vector3 Direction(Vector3 origin, Vector3 target, Vector3 fallback)
        {
            float x = target.x - origin.x;
            float z = target.z - origin.z;
            float squared = x * x + z * z;

            if (squared > CoincidenceEpsilonSquared)
            {
                float length = Mathf.Sqrt(squared);

                return new Vector3(x / length, 0f, z / length);
            }

            Vector3 flat = new Vector3(fallback.x, 0f, fallback.z);

            if (flat.sqrMagnitude > CoincidenceEpsilonSquared)
                return flat.normalized;

            return Vector3.forward;
        }

        public static void Apply(EcsPool<Pushed> pushes, int target, Vector3 direction, float speed, int ticks)
        {
            ref Pushed pushed = ref pushes.Has(target) ? ref pushes.Get(target) : ref pushes.Add(target);

            pushed.Velocity = direction * speed;
            pushed.RemainingTicks = ticks;
            pushed.TotalTicks = ticks;
        }
    }
}
