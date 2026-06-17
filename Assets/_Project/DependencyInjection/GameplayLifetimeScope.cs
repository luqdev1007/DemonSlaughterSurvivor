using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay;
using DemonSlaughter.Gameplay.Characters;
using DemonSlaughter.Gameplay.ECS;
using DemonSlaughter.Infrastructure.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DemonSlaughter.DependencyInjection
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private CharacterConfig _berserkCharacterConfig;
        [SerializeField] private Transform _spawnPoint;

        protected override void Configure(IContainerBuilder builder)
        {
            // input
            var inputActions = new PlayerInputActions();
            inputActions.Enable();
            builder.RegisterInstance(inputActions);

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