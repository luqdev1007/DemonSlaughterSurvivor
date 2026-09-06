using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ReadDashInputSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, DashStats>> _filter = default;

        private readonly EcsPoolInject<DashRequest> _requests = default;
        private readonly EcsPoolInject<Dashing> _dashing = default;

        private readonly EcsCustomInject<IInputService> _input = default;
        private readonly EcsCustomInject<CharacterConfig> _config;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Dashing dashing = ref _dashing.Value.Add(entity);
                dashing.Remaining = _config.Value.DashDuration;
                dashing.Speed = _config.Value.DashDistance / _config.Value.DashDuration;

                _requests.Value.Del(entity);
            }
        }
    }

    public sealed class StartDashSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<DashRequest, DashStats, MoveIntent, Facing>, Exc<Dashing, DashCooldown>> _ready = default;

        private readonly EcsPoolInject<Dashing> _dashing = default;
        private readonly EcsPoolInject<DashRequest> _requests = default;


        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _ready.Value)
            {
                ref Dashing dashing = ref _dashing.Value.Add(entity);

                _requests.Value.Del(entity);
            }
        }
    }

    public sealed class AdvanceDashSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Dashing>> _filter = default;

        private readonly EcsPoolInject<Dashing> _dashing = default;
        private readonly EcsPoolInject<DashCooldown> _cooldown = default;

        private readonly EcsCustomInject<CharacterConfig> _config = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Dashing dashing = ref _dashing.Value.Get(entity);

                dashing.Remaining -= _clock.Value.Delta;

                if (dashing.Remaining > 0f)
                    continue;

                ref DashCooldown cooldown = ref _cooldown.Value.Add(entity);

                cooldown.Remaining = _config.Value.DashCooldown;

                _dashing.Value.Del(entity);
            }
        }
    }

    public sealed class TickDashCooldownSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            
        }
    }

    public sealed class ApplyDashVelocitySystem
    {

    }

    public sealed class ExpireDashRequestSystem
    {

    }
}
