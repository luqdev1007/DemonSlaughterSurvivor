using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;

namespace Game.Simulation.Systems
{
    public sealed class DetectContactDamageSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int NonEnemyCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position, BodyRadius, Health>, Exc<Dead>> _targets = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<ContactDamage> _contactDamages = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

        private List<int> _contacts;

        private float _queryRadius;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);

            WaveTimelineConfig waves = _level.Value.Waves;

            float widestEnemy = WaveTimelineValidator.ResolveMaxBodyRadius(waves);

            _queryRadius = character.BodyRadius + widestEnemy;

            float cellSize = _level.Value.SpatialCellSize;

            if (_queryRadius > cellSize)
                throw new InvalidOperationException(
                    $"The contact damage query radius is {_queryRadius} " +
                    $"({nameof(CharacterConfig)} '{character.Id}' {nameof(CharacterConfig.BodyRadius)} {character.BodyRadius} " +
                    $"plus the widest {nameof(EnemyConfig.BodyRadius)} {widestEnemy} in {nameof(WaveTimelineConfig)} " +
                    $"'{(waves == null ? "<none>" : waves.Id)}'), which is greater than " +
                    $"{nameof(LevelConfig)}.{nameof(LevelConfig.SpatialCellSize)} {cellSize}. " +
                    "The spatial index walks one ring of cells around the center and rejects a query radius wider than one cell, " +
                    "so an enemy standing against the player would deal no damage at all. " +
                    $"Either raise {nameof(LevelConfig)}.{nameof(LevelConfig.SpatialCellSize)} in the level config, " +
                    "or widen the walk in SpatialGrid.Query to ceil(radius / cellSize) rings, " +
                    $"or lower the {nameof(CharacterConfig.BodyRadius)} of the character or of the enemies in the timeline.");

            _contacts = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(waves) + NonEnemyCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            SpatialGrid grid = _grid.Value;

            EcsWorld world = _world.Value;

            foreach (int target in _targets.Value)
            {
                ref Position targetPosition = ref _positions.Value.Get(target);
                ref BodyRadius targetRadius = ref _bodyRadii.Value.Get(target);

                grid.Query(targetPosition.Value, _queryRadius, _contacts);

                for (int index = 0; index < _contacts.Count; index++)
                {
                    int source = _contacts[index];

                    if (_contactDamages.Value.Has(source) == false)
                        continue;

                    if (_bodyRadii.Value.Has(source) == false)
                        continue;

                    ref ContactDamage damage = ref _contactDamages.Value.Get(source);

                    if (damage.Value <= 0f)
                        continue;

                    ref Position sourcePosition = ref _positions.Value.Get(source);
                    ref BodyRadius sourceRadius = ref _bodyRadii.Value.Get(source);

                    float contact = targetRadius.Value + sourceRadius.Value;

                    if (contact <= 0f)
                        continue;

                    float deltaX = sourcePosition.Value.x - targetPosition.Value.x;
                    float deltaZ = sourcePosition.Value.z - targetPosition.Value.z;

                    if (deltaX * deltaX + deltaZ * deltaZ >= contact * contact)
                        continue;

                    int entity = world.NewEntity();

                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(entity);
                    damageEvent.Target = world.PackEntity(target);
                    damageEvent.Source = world.PackEntity(source);
                    damageEvent.Amount = damage.Value;
                }
            }
        }
    }
}
