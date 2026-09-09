using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ChaseTargetSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<ChaseTarget, Position, MoveIntent>> _filter = default;

        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref ChaseTarget target = ref _chaseTargets.Value.Get(entity);
                ref MoveIntent intent = ref _intents.Value.Get(entity);

                if (target.Value.Unpack(_world.Value, out int targetEntity) == false)
                {
                    intent.Value = Vector3.zero;
                    continue;
                }

                if (_positions.Value.Has(targetEntity) == false)
                {
                    intent.Value = Vector3.zero;
                    continue;
                }

                ref Position self = ref _positions.Value.Get(entity);
                ref Position targetPosition = ref _positions.Value.Get(targetEntity);

                Vector3 delta = targetPosition.Value - self.Value;
                delta.y = 0f;

                intent.Value = delta.sqrMagnitude <= Mathf.Epsilon ? Vector3.zero : delta.normalized;
            }
        }
    }
}
