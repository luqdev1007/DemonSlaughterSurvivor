using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay;
using DemonSlaughter.Gameplay.Camera;
using DemonSlaughter.Gameplay.Characters;
using DemonSlaughter.Gameplay.ECS;
using DemonSlaughter.Infrastructure.Services;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DemonSlaughter.DependencyInjection
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private CharacterConfig _berserkCharacterConfig;
        [SerializeField] private Transform _spawnPoint;
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
            builder.Register<EcsPipeline>(Lifetime.Singleton);
            builder.RegisterComponentInNewPrefab(
                CreateEcsRunner(), Lifetime.Singleton);

            // Character
            builder.Register<IAssetProvider, AddressableAssetProvider>(Lifetime.Singleton);
            builder.Register<CharacterFactory>(Lifetime.Singleton);

            // Entry point
            builder.RegisterEntryPoint<GameplayEntryPoint>()
                   .WithParameter(_berserkCharacterConfig)
                   .WithParameter(_spawnPoint.position);
        }

        private EcsRunner CreateEcsRunner()
        {
            var go = new GameObject("EcsRunner");
            return go.AddComponent<EcsRunner>();
        }
    }
}