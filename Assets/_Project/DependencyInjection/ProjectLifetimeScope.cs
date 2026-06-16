using DemonSlaughter.Core.Bootstrap;
using DemonSlaughter.Core.EntryPoints.Bootstrap;
using DemonSlaughter.Core.Loading;
using DemonSlaughter.Core.Save;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Core.StateMachine;
using DemonSlaughter.Core.StateMachine.States;
using DemonSlaughter.Infrastructure;
using DemonSlaughter.Infrastructure.Save;
using DemonSlaughter.Infrastructure.Services;
using DemonSlaughter.UI.Loading;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DemonSlaughter.DependencyInjection
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private LoadingScreenView _loadingScreenPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            // Loading screen
            builder.RegisterComponentInNewPrefab(_loadingScreenPrefab, Lifetime.Singleton)
                   .As<ILoadingScreen>();

            // Services
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
            builder.Register<ISaveService, JsonSaveService>(Lifetime.Singleton);
            builder.Register<ILevelRunner, LevelRunner>(Lifetime.Singleton);

            // State machine
            builder.Register<IStateFactory, VContainerStateFactory>(Lifetime.Singleton);
            builder.Register<GameStateMachine>(Lifetime.Singleton);

            // States
            builder.Register<BootstrapState>(Lifetime.Singleton);
            builder.Register<MainMenuState>(Lifetime.Singleton);
            builder.Register<GameState>(Lifetime.Singleton);

            // Entry point
            builder.RegisterEntryPoint<GameEntryPoint>();

            // Misc
            builder.Register<GameplayReadySignal>(Lifetime.Singleton);
            builder.Register<IGameplayReadySignal>(c =>
                c.Resolve<GameplayReadySignal>(), Lifetime.Singleton);
        }
    }
}