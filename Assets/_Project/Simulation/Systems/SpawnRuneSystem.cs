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
    public sealed class SpawnRuneSystem : IEcsInitSystem, IEcsRunSystem
    {
        public const string RandomStreamId = "stream.runes";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Rune>> _runes = default;
        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<Rune> _runePool = default;
        private readonly EcsPoolInject<ActiveRune> _active = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<IAnnouncementSink> _announcements = default;

        private RuneTableConfig _table;
        private uint _random;
        private float _nextTime;

        public void Init(IEcsSystems systems)
        {
            _table = _level.Value.Waves == null ? null : _level.Value.Waves.Runes;

            if (_table == null)
                return;

            _table.Validate($"{nameof(LevelConfig)} '{_level.Value.Id}'");
            _random = SimulationRandom.StreamState(_context.Value == null ? 0 : _context.Value.Seed, RandomStreamId);
            _nextTime = _table.FirstDelaySeconds;
        }

        public void Run(IEcsSystems systems)
        {
            if (_table == null)
                return;

            float now = _clock.Value.Elapsed;

            foreach (int hero in _heroes.Value)
            {
                if (_runes.Value.GetEntitiesCount() > 0 || _active.Value.Has(hero))
                    return;

                if (float.IsPositiveInfinity(_nextTime))
                    _nextTime = now + Mathf.Lerp(_table.MinIntervalSeconds, _table.MaxIntervalSeconds, SimulationRandom.NextUnit(ref _random));

                if (now < _nextTime)
                    return;

                Spawn(_positions.Value.Get(hero).Value);
                _nextTime = float.PositiveInfinity;

                return;
            }
        }

        private void Spawn(Vector3 hero)
        {
            RuneConfig rune = Pick();
            float angle = SimulationRandom.NextUnit(ref _random) * Mathf.PI * 2f;
            float distance = Mathf.Lerp(_table.MinDistanceFromHero, _table.MaxDistanceFromHero, SimulationRandom.NextUnit(ref _random));

            Vector3 position = new Vector3(hero.x + Mathf.Cos(angle) * distance, 0f, hero.z + Mathf.Sin(angle) * distance);

            float limit = _level.Value.ArenaRadius - _table.ArenaEdgeMargin;

            if (limit > 0f && position.magnitude > limit)
                position *= limit / position.magnitude;

            EcsWorld world = _world.Value;
            int entity = world.NewEntity();

            _runePool.Value.Add(entity).Config = rune;
            _positions.Value.Add(entity).Value = position;
            _notIndexed.Value.Add(entity);
            _views.Value.Add(entity).Value = _viewFactory.Value.Create(rune.ViewPrefab, position);

            Announce(rune);
        }

        private void Announce(RuneConfig rune)
        {
            if (_announcements.Value == null || string.IsNullOrEmpty(_table.SpawnAnnouncement) || string.IsNullOrEmpty(rune.DisplayName))
                return;

            string name = char.ToLowerInvariant(rune.DisplayName[0]) + rune.DisplayName.Substring(1);

            _announcements.Value.Announce(string.Format(_table.SpawnAnnouncement, name));
        }

        private RuneConfig Pick()
        {
            IReadOnlyList<WeightedRune> runes = _table.Runes;
            float total = 0f;

            for (int index = 0; index < runes.Count; index++)
                total += runes[index].Weight;

            float pick = SimulationRandom.NextUnit(ref _random) * total;

            for (int index = 0; index < runes.Count; index++)
            {
                pick -= runes[index].Weight;

                if (pick < 0f)
                    return runes[index].Rune;
            }

            return runes[runes.Count - 1].Rune;
        }
    }
}
