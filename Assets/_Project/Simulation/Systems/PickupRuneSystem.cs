using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PickupRuneSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Rune, Position>> _runes = default;
        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead, ActiveRune>> _heroes = default;

        private readonly EcsPoolInject<Rune> _runePool = default;
        private readonly EcsPoolInject<ActiveRune> _active = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<IAnnouncementSink> _announcements = default;

        public void Run(IEcsSystems systems)
        {
            RuneTableConfig table = _level.Value.Waves == null ? null : _level.Value.Waves.Runes;

            if (table == null)
                return;

            EcsWorld world = _world.Value;
            float radiusSqr = table.PickupRadius * table.PickupRadius;

            foreach (int hero in _heroes.Value)
            {
                Vector3 heroPosition = _positions.Value.Get(hero).Value;

                foreach (int rune in _runes.Value)
                {
                    Vector3 position = _positions.Value.Get(rune).Value;
                    float dx = position.x - heroPosition.x;
                    float dz = position.z - heroPosition.z;

                    if (dx * dx + dz * dz > radiusSqr)
                        continue;

                    Apply(hero, _runePool.Value.Get(rune).Config);
                    Remove(world, rune);

                    break;
                }
            }
        }

        private void Apply(int hero, RuneConfig config)
        {
            float delta = _clock.Value.Delta;
            IReadOnlyList<StatModifierEntry> modifiers = config.Modifiers;

            for (int index = 0; index < modifiers.Count; index++)
                _statModifiers.Value.Add(hero, modifiers[index].Stat, modifiers[index].Op, modifiers[index].Value, config.ModifierSource);

            ref ActiveRune active = ref _active.Value.Add(hero);
            active.Config = config;
            active.TotalTicks = Mathf.Max(1, Mathf.RoundToInt(config.DurationSeconds / delta));
            active.RemainingTicks = active.TotalTicks;
            active.PulseTicks = Mathf.Max(1, Mathf.RoundToInt(config.RegenIntervalSeconds / delta));

            if (_announcements.Value != null && string.IsNullOrEmpty(config.DisplayName) == false)
                _announcements.Value.Announce(config.DisplayName);
        }

        private void Remove(EcsWorld world, int rune)
        {
            if (_views.Value.Has(rune))
            {
                ref View view = ref _views.Value.Get(rune);

                if (view.Value != null)
                    _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }

            world.DelEntity(rune);
        }
    }
}
