using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyDashVelocitySystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Dashing, Velocity>> _filter = default;

        private readonly EcsPoolInject<Dashing> _dashes = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Dashing dashing = ref _dashes.Value.Get(entity);
                ref Velocity velocity = ref _velocities.Value.Get(entity);

                velocity.Value = dashing.Direction * (dashing.Speed * ResolveSpeedFactor(dashing));
            }
        }

        private static float ResolveSpeedFactor(in Dashing dashing)
        {
            int total = dashing.TotalTicks;

            if (total <= 0)
                return 1f;

            int elapsed = Mathf.Clamp(total - dashing.RemainingTicks, 0, total - 1);

            float from = Mathf.Pow(elapsed / (float)total, dashing.AccelerationPower);
            float to = Mathf.Pow((elapsed + 1) / (float)total, dashing.AccelerationPower);

            return (to - from) * total;
        }
    }
}
