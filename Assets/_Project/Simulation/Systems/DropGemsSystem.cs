using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class DropGemsSystem : IEcsInitSystem, IEcsRunSystem
    {
        public const string RandomStreamId = "stream.gem-scatter";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Enemy, DiedEvent, GemDrop, Position>> _dying = default;

        private readonly EcsPoolInject<GemDrop> _drops = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Gem> _gems = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<GemScatter> _scatters = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private uint _random;

        public void Init(IEcsSystems systems)
        {
            _random = SimulationRandom.StreamState(_context.Value == null ? 0 : _context.Value.Seed, RandomStreamId);

            IReadOnlyList<GemConfig> gems = _content.Value.All<GemConfig>();

            for (int index = 0; index < gems.Count; index++)
                Validate(gems[index]);

            IReadOnlyList<EnemyConfig> enemies = _content.Value.All<EnemyConfig>();

            for (int index = 0; index < enemies.Count; index++)
            {
                if (enemies[index].Gem != null)
                    Validate(enemies[index].Gem);
            }
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int enemy in _dying.Value)
            {
                GemConfig config = _drops.Value.Get(enemy).Config;

                if (config == null)
                    continue;

                ref Position enemyPosition = ref _positions.Value.Get(enemy);

                int gem = world.NewEntity();

                _gems.Value.Add(gem).Experience = config.Experience;
                _positions.Value.Add(gem).Value = enemyPosition.Value;
                _velocities.Value.Add(gem);
                _notIndexed.Value.Add(gem);
                _views.Value.Add(gem).Value = _viewFactory.Value.Create(config.ViewPrefab, enemyPosition.Value);

                Scatter(gem, config, enemyPosition.Value);
            }
        }

        private void Scatter(int gem, GemConfig config, Vector3 origin)
        {
            float delta = _clock.Value == null ? 0f : _clock.Value.Delta;
            float seconds = Mathf.Lerp(config.ScatterMinSeconds, config.ScatterMaxSeconds, SimulationRandom.NextUnit(ref _random));
            float angle = SimulationRandom.NextUnit(ref _random) * Mathf.PI * 2f;
            float distance = Mathf.Lerp(config.ScatterMinDistance, config.ScatterMaxDistance, SimulationRandom.NextUnit(ref _random));
            float height = Mathf.Lerp(config.ScatterMinHeight, config.ScatterMaxHeight, SimulationRandom.NextUnit(ref _random));

            if (delta <= 0f || seconds <= 0f || distance <= 0f)
                return;

            ref GemScatter scatter = ref _scatters.Value.Add(gem);
            scatter.Start = new Vector3(origin.x, 0f, origin.z);
            scatter.End = scatter.Start + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            scatter.Height = height;
            scatter.ElapsedTicks = 0;
            scatter.TotalTicks = Mathf.Max(1, Mathf.RoundToInt(seconds / delta));
        }

        private static void Validate(GemConfig gem)
        {
            if (gem.ViewPrefab == null)
                throw new InvalidOperationException($"{nameof(GemConfig)} '{gem.Id}' has no view prefab.");

            if (gem.Experience <= 0)
                throw new InvalidOperationException(
                    $"{nameof(GemConfig)} '{gem.Id}' gives {gem.Experience} experience; a gem must give at least 1.");
        }
    }
}
