using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class DecayRageSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, UltimateCharge, RageState>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<UltimateCharge> _charges = default;
        private readonly EcsPoolInject<RageState> _states = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

        private float _delaySeconds;
        private float _decayPerSecond;

        private float _resolvedDelta;
        private int _delayTicks;
        private float _decayPerTick;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);
            RageConfig rage = character.Rage;

            if (rage == null)
                throw new InvalidOperationException(
                    $"{nameof(CharacterConfig)} '{character.Id}' has no {nameof(RageConfig)} assigned, " +
                    $"but {nameof(DecayRageSystem)} is registered and reads its decay numbers.");

            _delaySeconds = rage.DecayDelaySeconds;
            _decayPerSecond = rage.DecayPerSecond;
            _resolvedDelta = 0f;
        }

        public void Run(IEcsSystems systems)
        {
            ResolveTicks(_clock.Value.Delta);

            foreach (int hero in _heroes.Value)
            {
                ref RageState state = ref _states.Value.Get(hero);

                if (state.TicksSinceCombat < _delayTicks)
                {
                    state.TicksSinceCombat++;
                    continue;
                }

                ref UltimateCharge charge = ref _charges.Value.Get(hero);

                if (charge.Value >= charge.Max)
                    continue;

                charge.Value = Mathf.Max(0f, charge.Value - _decayPerTick);
            }
        }

        private void ResolveTicks(float delta)
        {
            if (delta == _resolvedDelta)
                return;

            _resolvedDelta = delta;
            _delayTicks = Mathf.Max(0, Mathf.RoundToInt(_delaySeconds / delta));
            _decayPerTick = _decayPerSecond * delta;
        }
    }
}
