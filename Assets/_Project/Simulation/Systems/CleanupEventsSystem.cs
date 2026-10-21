using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class CleanupEventsSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent>> _damageEvents = default;
        private readonly EcsFilterInject<Inc<HealEvent>> _healEvents = default;
        private readonly EcsFilterInject<Inc<SwingEndedEvent>> _swingEndedEvents = default;
        private readonly EcsFilterInject<Inc<LeapLandedEvent>> _landedEvents = default;
        private readonly EcsFilterInject<Inc<AreaStrikeEvent>> _areaStrikeEvents = default;
        private readonly EcsFilterInject<Inc<ExecutionFinishedEvent>> _executionFinishedEvents = default;
        private readonly EcsFilterInject<Inc<DiedEvent>> _diedEvents = default;
        private readonly EcsFilterInject<Inc<SwingStarted>> _swingStarts = default;
        private readonly EcsFilterInject<Inc<LevelUpEvent>> _levelUps = default;
        private readonly EcsFilterInject<Inc<LeapStarted>> _leapStarts = default;
        private readonly EcsFilterInject<Inc<ExecutionStarted>> _executionStarts = default;
        private readonly EcsFilterInject<Inc<FlameSwingStarted>> _flameSwingStarts = default;
        private readonly EcsFilterInject<Inc<RangedWindupStarted>> _windupStarts = default;
        private readonly EcsFilterInject<Inc<CastInterruptedEvent>> _castInterrupts = default;
        private readonly EcsFilterInject<Inc<DashEndedEvent>> _dashEnds = default;
        private readonly EcsFilterInject<Inc<DashFinisherStarted>> _finisherStarts = default;
        private readonly EcsFilterInject<Inc<UpperSlashCue>> _upperSlashCues = default;

        private readonly EcsPoolInject<DiedEvent> _diedEventPool = default;
        private readonly EcsPoolInject<SwingStarted> _swingStartPool = default;
        private readonly EcsPoolInject<LevelUpEvent> _levelUpPool = default;
        private readonly EcsPoolInject<LeapStarted> _leapStartPool = default;
        private readonly EcsPoolInject<ExecutionStarted> _executionStartPool = default;
        private readonly EcsPoolInject<FlameSwingStarted> _flameSwingStartPool = default;
        private readonly EcsPoolInject<RangedWindupStarted> _windupStartPool = default;
        private readonly EcsPoolInject<CastInterruptedEvent> _castInterruptPool = default;
        private readonly EcsPoolInject<DashEndedEvent> _dashEndPool = default;
        private readonly EcsPoolInject<DashFinisherStarted> _finisherStartPool = default;
        private readonly EcsPoolInject<UpperSlashCue> _upperSlashCuePool = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _damageEvents.Value)
                world.DelEntity(entity);

            foreach (int entity in _healEvents.Value)
                world.DelEntity(entity);

            foreach (int entity in _swingEndedEvents.Value)
                world.DelEntity(entity);

            foreach (int entity in _landedEvents.Value)
                world.DelEntity(entity);

            foreach (int entity in _areaStrikeEvents.Value)
                world.DelEntity(entity);

            foreach (int entity in _executionFinishedEvents.Value)
                world.DelEntity(entity);

            foreach (int entity in _diedEvents.Value)
                _diedEventPool.Value.Del(entity);

            foreach (int entity in _swingStarts.Value)
                _swingStartPool.Value.Del(entity);

            foreach (int entity in _levelUps.Value)
                _levelUpPool.Value.Del(entity);

            foreach (int entity in _leapStarts.Value)
                _leapStartPool.Value.Del(entity);

            foreach (int entity in _executionStarts.Value)
                _executionStartPool.Value.Del(entity);

            foreach (int entity in _flameSwingStarts.Value)
                _flameSwingStartPool.Value.Del(entity);

            foreach (int entity in _windupStarts.Value)
                _windupStartPool.Value.Del(entity);

            foreach (int entity in _castInterrupts.Value)
                _castInterruptPool.Value.Del(entity);

            foreach (int entity in _dashEnds.Value)
                _dashEndPool.Value.Del(entity);

            foreach (int entity in _finisherStarts.Value)
                _finisherStartPool.Value.Del(entity);

            foreach (int entity in _upperSlashCues.Value)
                _upperSlashCuePool.Value.Del(entity);
        }
    }
}
