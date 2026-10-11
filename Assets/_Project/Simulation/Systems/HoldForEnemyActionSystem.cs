using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class HoldForEnemyActionSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemyAction, ChaseTarget, Position, MoveIntent, BodyRadius>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _filter.Value)
            {
                if (HoldsPosition(_actions.Value.Get(entity).Phase) || IsInContact(world, entity))
                    _intents.Value.Get(entity).Value = Vector3.zero;
            }
        }

        public static bool HoldsPosition(EnemyActionPhase phase)
        {
            return phase != EnemyActionPhase.None && phase != EnemyActionPhase.Spinning;
        }

        private bool IsInContact(EcsWorld world, int entity)
        {
            if (_chaseTargets.Value.Get(entity).Value.Unpack(world, out int target) == false)
                return false;

            if (_positions.Value.Has(target) == false || _bodyRadii.Value.Has(target) == false || _dead.Value.Has(target))
                return false;

            float contact = _bodyRadii.Value.Get(entity).Value + _bodyRadii.Value.Get(target).Value;

            Vector3 self = _positions.Value.Get(entity).Value;
            Vector3 other = _positions.Value.Get(target).Value;

            float deltaX = other.x - self.x;
            float deltaZ = other.z - self.z;

            return deltaX * deltaX + deltaZ * deltaZ <= contact * contact;
        }
    }
}
