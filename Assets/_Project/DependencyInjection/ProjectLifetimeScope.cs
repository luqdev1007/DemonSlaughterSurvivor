using DemonSlaughter.Core.EntryPoints.Bootstrap;
using DemonSlaughter.Core.Services;
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
        }
    }
}