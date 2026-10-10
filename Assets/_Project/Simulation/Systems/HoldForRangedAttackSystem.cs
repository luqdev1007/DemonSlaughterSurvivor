using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class HoldForRangedAttackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<RangedAttack, ChaseTarget, Position, MoveIntent>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<RangedAttack> _attacks = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _filter.Value)
            {
                ref RangedAttack attack = ref _attacks.Value.Get(entity);

                attack.IsHolding = attack.WindupTicks > 0 || IsTargetInRange(world, entity, attack.Config.Range);

                if (attack.IsHolding == false)
                    continue;

                _intents.Value.Get(entity).Value = Vector3.zero;
            }
        }

        private bool IsTargetInRange(EcsWorld world, int entity, float range)
        {
            if (RangedTarget.TryFindLiving(world, _chaseTargets.Value.Get(entity).Value, _positions.Value, _dead.Value, out Vector3 target) == false)
                return false;

            return RangedTarget.IsWithin(_positions.Value.Get(entity).Value, target, range);
        }
    }
}
