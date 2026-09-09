using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;

namespace Game.Simulation.Systems
{
    public sealed class AcquireChaseTargetSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Enemy, ChaseTarget>> _seekers = default;
        private readonly EcsFilterInject<Inc<Player, Position>> _candidates = default;

        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _seekers.Value)
            {
                ref ChaseTarget chaseTarget = ref _chaseTargets.Value.Get(entity);

                if (chaseTarget.Value.Unpack(_world.Value, out int _))
                    continue;

                foreach (var candidate in _candidates.Value)
                {
                    chaseTarget.Value = _world.Value.PackEntity(candidate);
                    break;
                }
            }
        }
    }
}
