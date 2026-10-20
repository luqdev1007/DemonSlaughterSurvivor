using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyRapidFireSystem : IEcsRunSystem
    {
        public const string ModifierSource = "perk.rapid-fire";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<RapidFire> _states = default;
        private readonly EcsPoolInject<Duality> _dualities = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out RapidFireConfig config, out int level) == false)
                    continue;

                if (_states.Value.Has(hero) == false)
                    _states.Value.Add(hero);

                ref RapidFire state = ref _states.Value.Get(hero);
                RapidFireLevel entry = config.Level(level);

                if (state.CooldownTicks > 0)
                    state.CooldownTicks--;

                if (state.CooldownTicks <= 0)
                {
                    float interval = entry.IntervalSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero);

                    state.ActiveTicks = Mathf.Max(1, Mathf.RoundToInt(entry.DurationSeconds / delta));
                    state.CooldownTicks = Mathf.Max(state.ActiveTicks, Mathf.RoundToInt(interval / delta));
                }

                float desired = 0f;

                if (state.ActiveTicks > 0)
                {
                    state.ActiveTicks--;
                    desired = entry.AttackSpeed;

                    if (IsDemonic(hero, config))
                        desired += entry.DemonicAttackSpeed;
                }

                if (Mathf.Approximately(desired, state.Applied))
                    continue;

                state.Applied = desired;

                if (desired > 0f)
                    _statModifiers.Value.Add(hero, StatId.AttackSpeed, StatOp.More, desired, ModifierSource);
                else
                    _statModifiers.Value.RemoveBySource(hero, ModifierSource);
            }
        }

        private bool IsDemonic(int hero, RapidFireConfig config)
        {
            return _dualities.Value.Has(hero) && _dualities.Value.Get(hero).Demonic > config.DemonicThreshold;
        }
    }
}
