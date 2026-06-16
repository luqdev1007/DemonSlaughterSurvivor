using DemonSlaughter.Core.EntryPoints.Bootstrap;
using DemonSlaughter.Core.Save;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Core.StateMachine;
using DemonSlaughter.Core.StateMachine.States;
using DemonSlaughter.Infrastructure.Save;
using DemonSlaughter.Infrastructure.Services;
using VContainer;
using VContainer.Unity;

namespace DemonSlaughter.DependencyInjection
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);

            builder.RegisterEntryPoint<GameEntryPoint>();

            builder.Register<GameStateMachine>(Lifetime.Singleton);

            builder.Register<BootstrapState>(Lifetime.Singleton);

            builder.Register<MainMenuState>(Lifetime.Singleton);

            builder.Register<GameState>(Lifetime.Singleton);

            builder.Register<ISaveService, JsonSaveService>(Lifetime.Singleton);
        }
    }
}