using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyPushSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Pushed, Velocity>> _filter = default;

        private readonly EcsPoolInject<Pushed> _pushes = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Pushed pushed = ref _pushes.Value.Get(entity);
                ref Velocity velocity = ref _velocities.Value.Get(entity);

                pushed.RemainingTicks -= 1;

                if (pushed.RemainingTicks <= 0)
                {
                    velocity.Value = Vector3.zero;

                    _pushes.Value.Del(entity);

                    continue;
                }

                velocity.Value = pushed.Velocity * (pushed.RemainingTicks / (float)pushed.TotalTicks);
            }
        }
    }
}
