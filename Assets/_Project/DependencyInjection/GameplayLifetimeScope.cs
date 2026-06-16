using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay;
using DemonSlaughter.Gameplay.Characters;
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
            builder.Register<CharacterFactory>(Lifetime.Singleton);
            builder.Register<IAssetProvider, AddressableAssetProvider>(Lifetime.Singleton);

            builder.RegisterEntryPoint<GameplayEntryPoint>()
                   .WithParameter(_berserkCharacterConfig)
                   .WithParameter(_spawnPoint.position);
        }
    }
}