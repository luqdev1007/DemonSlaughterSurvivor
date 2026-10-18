using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickBackleapTimersSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<BackleapCooldown>> _cooldownFilter = default;
        private readonly EcsFilterInject<Inc<BackleapRequest>> _requestFilter = default;

        private readonly EcsPoolInject<BackleapCooldown> _cooldowns = default;
        private readonly EcsPoolInject<BackleapRequest> _requests = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<InputConfig> _input = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _cooldownFilter.Value)
            {
                ref BackleapCooldown cooldown = ref _cooldowns.Value.Get(entity);
                cooldown.Remaining -= delta;

                if (cooldown.Remaining <= 0f)
                    _cooldowns.Value.Del(entity);
            }

            foreach (int entity in _requestFilter.Value)
            {
                ref BackleapRequest request = ref _requests.Value.Get(entity);
                request.Age += delta;

                if (request.Age >= _input.Value.BufferSeconds)
                    _requests.Value.Del(entity);
            }
        }
    }
}
