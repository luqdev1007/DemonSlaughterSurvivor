using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Configs.Enemies;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay.Characters;
using DemonSlaughter.Gameplay.ECS;
using DemonSlaughter.Gameplay.Enemies;
using UnityEngine;
using VContainer.Unity;

namespace DemonSlaughter.Gameplay
{
    public sealed class GameplayEntryPoint : IStartable
    {
        private readonly HeroFactory _characterFactory;
        private readonly EnemyFactory _enemyFactory;

        private readonly HeroConfig _berserkConfig;
        private readonly EnemyConfig _enemyConfig;

        private readonly Vector3 _spawnPoint;
        private readonly Transform[] _enemySpawnPoints;

        private readonly EcsPipeline _pipeline;
        private readonly EcsRunner _ecsRunner;

        private readonly IGameplayReadySignal _readySignal;

        public GameplayEntryPoint(
            HeroFactory characterFactory,
            EnemyFactory enemyFactory,
            EcsPipeline pipeline,
            EcsRunner ecsRunner,
            IGameplayReadySignal readySignal,
            HeroConfig berserkConfig,
            EnemyConfig enemyConfig,
            Vector3 spawnPoint,
            Transform[] enemySpawnPoints
            )
        {
            _characterFactory = characterFactory;
            _enemyFactory = enemyFactory; 
            _pipeline = pipeline;
            _ecsRunner = ecsRunner;
            _readySignal = readySignal;
            _berserkConfig = berserkConfig;
            _enemyConfig = enemyConfig;   
            _spawnPoint = spawnPoint;
            _enemySpawnPoints = enemySpawnPoints;
        }

        public void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            await _characterFactory.CreatePlayerAsync(_berserkConfig, _spawnPoint);

            // enemies
            foreach (var spawnPoint in _enemySpawnPoints)
            {
                await _enemyFactory.CreateAsync(_enemyConfig, spawnPoint.position);
            }

            _ecsRunner.Initialize(_pipeline);

            Debug.Log("ECS initialized, player spawned");

            _readySignal.Fire();
        }
    }
}