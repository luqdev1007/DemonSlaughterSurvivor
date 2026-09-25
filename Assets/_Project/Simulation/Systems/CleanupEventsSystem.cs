using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class CleanupEventsSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent>> _damageEvents = default;
        private readonly EcsFilterInject<Inc<DiedEvent>> _diedEvents = default;

        private readonly EcsPoolInject<DiedEvent> _diedEventPool = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _damageEvents.Value)
                world.DelEntity(entity);

            foreach (int entity in _diedEvents.Value)
                _diedEventPool.Value.Del(entity);
        }
    }
}
