using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class InterruptCastSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<RangedAttack, Pushed, Interruptible>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<RangedAttack> _attacks = default;
        private readonly EcsPoolInject<Staggered> _staggered = default;
        private readonly EcsPoolInject<CastInterruptedEvent> _interrupted = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref RangedAttack attack = ref _attacks.Value.Get(entity);

                if (attack.WindupTicks <= 0)
                    continue;

                attack.WindupTicks = 0;
                attack.IsHolding = false;

                ref Staggered staggered = ref _staggered.Value.Has(entity) ? ref _staggered.Value.Get(entity) : ref _staggered.Value.Add(entity);
                staggered.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(attack.Config.StaggerSeconds / delta));

                if (_interrupted.Value.Has(entity) == false)
                    _interrupted.Value.Add(entity);
            }
        }
    }
}
