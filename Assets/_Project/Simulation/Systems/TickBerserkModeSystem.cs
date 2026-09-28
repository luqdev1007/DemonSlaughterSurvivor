using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickBerserkModeSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<BerserkMode>> _filter = default;

        private readonly EcsPoolInject<BerserkMode> _modes = default;
        private readonly EcsPoolInject<UltimateCharge> _charges = default;
        private readonly EcsPoolInject<RageState> _rageStates = default;

        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int hero in _filter.Value)
            {
                ref BerserkMode mode = ref _modes.Value.Get(hero);

                mode.RemainingTicks -= 1;

                if (mode.RemainingTicks > 0)
                    continue;

                _statModifiers.Value.RemoveBySource(hero, StartBerserkSystem.SourceId);

                if (_charges.Value.Has(hero))
                {
                    ref UltimateCharge charge = ref _charges.Value.Get(hero);
                    charge.Value = 0f;
                }

                if (_rageStates.Value.Has(hero))
                {
                    ref RageState state = ref _rageStates.Value.Get(hero);
                    state.TicksSinceCombat = 0;
                }

                _modes.Value.Del(hero);
            }
        }
    }
}
