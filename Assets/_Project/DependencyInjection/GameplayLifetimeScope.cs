using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Configs.Enemies;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay;
using DemonSlaughter.Gameplay.Camera;
using DemonSlaughter.Gameplay.Characters;
using DemonSlaughter.Gameplay.ECS;
using DemonSlaughter.Gameplay.Enemies;
using DemonSlaughter.Infrastructure.Services;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DemonSlaughter.DependencyInjection
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private HeroConfig _berserkCharacterConfig;
        [SerializeField] private EnemyConfig _enemyTrollConfig;

        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform[] _enemySpawnPoints;

        [SerializeField] private CinemachineCamera _virtualCamera;
        [SerializeField] private CameraOcclusionHandler _occlusionHandler;

        protected override void Configure(IContainerBuilder builder)
        {
            // input
            var inputActions = new PlayerInputActions();
            inputActions.Enable();
            builder.RegisterInstance(inputActions);

            // camera
            builder.RegisterInstance(_virtualCamera);
            builder.RegisterInstance(_occlusionHandler);

            // ECS
            builder.Register<EcsPipeline>(Lifetime.Singleton)
                   .WithParameter(_berserkCharacterConfig.AttackAnimationTriggers);

            builder.RegisterComponentInNewPrefab(
                CreateEcsRunner(), Lifetime.Singleton);

            // Factories
            builder.Register<IAssetProvider, AddressableAssetProvider>(Lifetime.Singleton);
            builder.Register<HeroFactory>(Lifetime.Singleton);
            builder.Register<EnemyFactory>(Lifetime.Singleton); 

            // Entry point
            builder.RegisterEntryPoint<GameplayEntryPoint>()
                   .WithParameter(_berserkCharacterConfig)
                   .WithParameter(_enemyTrollConfig)
                   .WithParameter(_spawnPoint.position)
                   .WithParameter(_enemySpawnPoints)
                   ;
        }

        private EcsRunner CreateEcsRunner()
        {
            var go = new GameObject("EcsRunner");
            return go.AddComponent<EcsRunner>();
        }
    }
}