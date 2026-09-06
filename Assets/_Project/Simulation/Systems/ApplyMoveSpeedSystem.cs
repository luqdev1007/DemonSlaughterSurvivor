using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ApplyMoveSpeedSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<MoveIntent, MoveSpeed, Velocity>, Exc<Dashing>> _filter = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<MoveSpeed> _speeds = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var intent = ref _intents.Value.Get(entity);
                ref var speed = ref _speeds.Value.Get(entity);
                ref var velocity = ref _velocities.Value.Get(entity);

                velocity.Value = intent.Value * speed.Value;
            }
        }
    }
}
