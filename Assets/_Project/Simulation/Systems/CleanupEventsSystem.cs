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
        private readonly EcsFilterInject<Inc<SwingStarted>> _swingStarts = default;
        private readonly EcsFilterInject<Inc<LevelUpEvent>> _levelUps = default;
        private readonly EcsFilterInject<Inc<LeapStarted>> _leapStarts = default;

        private readonly EcsPoolInject<DiedEvent> _diedEventPool = default;
        private readonly EcsPoolInject<SwingStarted> _swingStartPool = default;
        private readonly EcsPoolInject<LevelUpEvent> _levelUpPool = default;
        private readonly EcsPoolInject<LeapStarted> _leapStartPool = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _damageEvents.Value)
                world.DelEntity(entity);

            foreach (int entity in _diedEvents.Value)
                _diedEventPool.Value.Del(entity);

            foreach (int entity in _swingStarts.Value)
                _swingStartPool.Value.Del(entity);

            foreach (int entity in _levelUps.Value)
                _levelUpPool.Value.Del(entity);

            foreach (int entity in _leapStarts.Value)
                _leapStartPool.Value.Del(entity);
        }
    }
}
