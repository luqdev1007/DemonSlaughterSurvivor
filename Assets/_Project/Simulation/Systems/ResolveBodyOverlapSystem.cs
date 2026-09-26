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
    public sealed class ResolveBodyOverlapSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int NonEnemyCapacityReserve = 16;
        private const float CoincidenceEpsilonSquared = 1e-6f;
        private const float TieBreakAngleScale = 6.2831855f / 4294967296f;
        private const float CandidateSlack = 0f;

        private readonly EcsFilterInject<Inc<Player, Position, BodyRadius>> _players = default;

        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Pushed> _pushes = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

        private List<int> _contacts;

        private float _queryRadius;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);

            if (character.BodyRadius < 0f)
                throw new InvalidOperationException(
                    $"{nameof(CharacterConfig)} '{character.Id}' has a negative {nameof(CharacterConfig.BodyRadius)} " +
                    $"of {character.BodyRadius}. " +
                    "The contact distance is the sum of the two body radii, so a negative radius shrinks it below the " +
                    "real gap and no enemy ever reports an overlap: the crowd walks through the player without any message. " +
                    $"Set {nameof(CharacterConfig.BodyRadius)} to zero to disable the contact radius on purpose.");

            WaveTimelineConfig waves = _level.Value.Waves;

            float widestEnemy = WaveTimelineValidator.ResolveMaxBodyRadius(waves);

            _queryRadius = character.BodyRadius + widestEnemy;

            _contacts = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(waves) + NonEnemyCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            SpatialGrid grid = _grid.Value;

            float delta = _clock.Value.Delta;

            foreach (int player in _players.Value)
            {
                ref Position playerPosition = ref _positions.Value.Get(player);
                ref BodyRadius playerRadius = ref _bodyRadii.Value.Get(player);

                grid.Query(playerPosition.Value, _queryRadius, CandidateSlack, _contacts);

                for (int index = 0; index < _contacts.Count; index++)
                {
                    int enemy = _contacts[index];

                    if (_enemies.Value.Has(enemy) == false)
                        continue;

                    if (_bodyRadii.Value.Has(enemy) == false)
                        continue;

                    if (_velocities.Value.Has(enemy) == false)
                        continue;

                    if (_pushes.Value.Has(enemy))
                        continue;

                    ref Position enemyPosition = ref _positions.Value.Get(enemy);
                    ref BodyRadius enemyRadius = ref _bodyRadii.Value.Get(enemy);

                    float contact = playerRadius.Value + enemyRadius.Value;

                    if (contact <= 0f)
                        continue;

                    float deltaX = enemyPosition.Value.x - playerPosition.Value.x;
                    float deltaZ = enemyPosition.Value.z - playerPosition.Value.z;

                    float squared = deltaX * deltaX + deltaZ * deltaZ;

                    if (squared >= contact * contact)
                        continue;

                    Vector3 fromPlayerToEnemy;
                    float overlap;

                    if (squared <= CoincidenceEpsilonSquared)
                    {
                        float angle = ResolveTieBreakAngle(player, enemy);

                        fromPlayerToEnemy = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                        overlap = contact;
                    }
                    else
                    {
                        float distance = Mathf.Sqrt(squared);

                        fromPlayerToEnemy = new Vector3(deltaX / distance, 0f, deltaZ / distance);
                        overlap = contact - distance;
                    }

                    ref Velocity velocity = ref _velocities.Value.Get(enemy);

                    velocity.Value = Resolve(velocity.Value, fromPlayerToEnemy, overlap, delta);
                }
            }
        }

        private Vector3 Resolve(Vector3 enemyVelocity, Vector3 fromPlayerToEnemy, float overlap, float deltaTime)
        {
            return enemyVelocity + fromPlayerToEnemy * (overlap / deltaTime);
        }

        private static float ResolveTieBreakAngle(int player, int enemy)
        {
            int low = player < enemy ? player : enemy;
            int high = player < enemy ? enemy : player;

            uint hash = unchecked((uint)low * 2654435761u) ^ unchecked((uint)high * 2246822519u);

            hash ^= hash >> 15;
            hash = unchecked(hash * 2246822519u);
            hash ^= hash >> 13;
            hash = unchecked(hash * 3266489917u);
            hash ^= hash >> 16;

            return hash * TieBreakAngleScale;
        }
    }
}
