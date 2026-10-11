using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickPowerUpSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<PoweringUp>> _filter = default;

        private readonly EcsPoolInject<PoweringUp> _poweringUps = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int hero in _filter.Value)
            {
                ref PoweringUp powerUp = ref _poweringUps.Value.Get(hero);

                powerUp.RemainingTicks -= 1;

                if (powerUp.RemainingTicks > 0)
                    continue;

                _poweringUps.Value.Del(hero);
            }
        }
    }
}
