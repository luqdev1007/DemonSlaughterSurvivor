using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class DebugDamageSystem : IEcsRunSystem
    {
        private const float Damage = 20f;
        private const float Radius = 3f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead>> _sources = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, Health>, Exc<Dead>> _targets = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

        private readonly EcsCustomInject<IDebugDamageInput> _input = default;

        public void Run(IEcsSystems systems)
        {
            if (_input.Value.ConsumePressed() == false)
                return;

            EcsWorld world = _world.Value;

            foreach (int source in _sources.Value)
            {
                ref Position sourcePosition = ref _positions.Value.Get(source);

                foreach (int target in _targets.Value)
                {
                    ref Position targetPosition = ref _positions.Value.Get(target);

                    float deltaX = targetPosition.Value.x - sourcePosition.Value.x;
                    float deltaZ = targetPosition.Value.z - sourcePosition.Value.z;

                    if (deltaX * deltaX + deltaZ * deltaZ > Radius * Radius)
                        continue;

                    int entity = world.NewEntity();

                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(entity);
                    damageEvent.Target = world.PackEntity(target);
                    damageEvent.Source = world.PackEntity(source);
                    damageEvent.Amount = Damage;
                }

                return;
            }
        }
    }
}
