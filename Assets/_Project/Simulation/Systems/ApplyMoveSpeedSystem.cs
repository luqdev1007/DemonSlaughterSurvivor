using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyMoveSpeedSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<MoveIntent, MoveSpeed, Velocity>, Exc<Dashing, Pushed>> _filter = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<MoveSpeed> _speeds = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var intent = ref _intents.Value.Get(entity);
                ref var speed = ref _speeds.Value.Get(entity);
                ref var velocity = ref _velocities.Value.Get(entity);

                if (_specialAttacks.Value.Has(entity) && _specialAttacks.Value.Get(entity).LocksMovement)
                {
                    velocity.Value = Vector3.zero;

                    continue;
                }

                velocity.Value = intent.Value * speed.Value;
            }
        }
    }
}
