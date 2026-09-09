using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SpawnEnemiesSystem : IEcsInitSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<MoveSpeed> _speeds = default;
        private readonly EcsPoolInject<TurnSpeed> _turnSpeeds = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Init(IEcsSystems systems)
        {
            LevelConfig level = _level.Value;
            EnemyConfig enemy = level.StartingEnemy;

            if (enemy == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{level.Id}' has no starting enemy assigned.");

            if (enemy.ViewPrefab == null)
                throw new InvalidOperationException(
                    $"{nameof(EnemyConfig)} '{enemy.Id}' has no view prefab assigned.");

            int count = level.StartingEnemyCount;

            for (int index = 0; index < count; index++)
            {
                Vector3 spawnPosition = ResolveSpawnPosition(index, count);

                int entity = _world.Value.NewEntity();

                _enemies.Value.Add(entity);

                ref Position position = ref _positions.Value.Add(entity);
                position.Value = spawnPosition;

                ref Facing facing = ref _facings.Value.Add(entity);
                facing.Value = Vector3.forward;

                ref MoveSpeed speed = ref _speeds.Value.Add(entity);
                speed.Value = enemy.MoveSpeed;

                ref TurnSpeed turnSpeed = ref _turnSpeeds.Value.Add(entity);
                turnSpeed.Value = enemy.TurnSpeed;

                _intents.Value.Add(entity);
                _velocities.Value.Add(entity);
                _chaseTargets.Value.Add(entity);

                ref View view = ref _views.Value.Add(entity);
                view.Value = _viewFactory.Value.Create(enemy.ViewPrefab, spawnPosition);
            }
        }

        private Vector3 ResolveSpawnPosition(int index, int count)
        {
            Vector3 center = Vector3.zero;
            float spawnRange = _level.Value.EnemySpawnRadius;

            float fullTurn = Mathf.PI * 2f;
            float angle = fullTurn * index / count;

            float x = Mathf.Cos(angle) * spawnRange;
            float z = Mathf.Sin(angle) * spawnRange;

            return center + new Vector3(x, 0f, z);
        }
    }
}
