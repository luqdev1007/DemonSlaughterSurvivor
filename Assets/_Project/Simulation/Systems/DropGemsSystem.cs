using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;

namespace Game.Simulation.Systems
{
    public sealed class DropGemsSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Enemy, DiedEvent, GemDrop, Position>> _dying = default;

        private readonly EcsPoolInject<GemDrop> _drops = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Gem> _gems = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Init(IEcsSystems systems)
        {
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
            }
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
