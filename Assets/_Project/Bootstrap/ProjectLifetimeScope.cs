using Game.Core;
using Game.Services;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();

            builder.Register<SeedSource>(Lifetime.Singleton).As<ISeedSource>();

            builder.Register<RunLauncher>(Lifetime.Singleton).As<IRunLauncher>();

            builder.RegisterEntryPoint<BootstrapEntryPoint>();
        }
    }
}
