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
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

        private List<int> _contacts;

        private float _queryRadius;
        private float _maxOverlapCorrection;
        private float _maxEnemyMoveSpeed;
        private float _pushSpeed;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);

            WaveTimelineConfig waves = _level.Value.Waves;

            float widestEnemy = WaveTimelineValidator.ResolveMaxBodyRadius(waves);

            _queryRadius = character.BodyRadius + widestEnemy;

            _maxOverlapCorrection = character.BodyRadius + widestEnemy;
            _maxEnemyMoveSpeed = WaveTimelineValidator.ResolveMaxMoveSpeed(waves);
            _pushSpeed = character.Dash == null ? 0f : character.Dash.PushSpeed;

            _contacts = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(waves) + NonEnemyCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            SpatialGrid grid = _grid.Value;

            EcsWorld world = _world.Value;

            float candidateSlack = ResolveCandidateSlack();

            foreach (int target in _targets.Value)
            {
                ref Position targetPosition = ref _positions.Value.Get(target);
                ref BodyRadius targetRadius = ref _bodyRadii.Value.Get(target);

                grid.Query(targetPosition.Value, _queryRadius, candidateSlack, _contacts);

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
                    damageEvent.SourcePosition = sourcePosition.Value;
                    damageEvent.Amount = damage.Value;
                }
            }
        }

        private float ResolveCandidateSlack()
        {
            float delta = _clock.Value.Delta;

            float walkedAndCorrected = _maxEnemyMoveSpeed * delta + _maxOverlapCorrection;
            float pushed = _pushSpeed * delta;

            return Math.Max(walkedAndCorrected, pushed);
        }
    }
}
