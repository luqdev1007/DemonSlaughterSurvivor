using Game.Configs;
using Game.Core;
using Game.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private ContentDatabase _contentDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();

            builder.Register<SeedSource>(Lifetime.Singleton).As<ISeedSource>();

            builder.Register<RunLauncher>(Lifetime.Singleton).As<IRunLauncher>();

            ContentRegistry registry = new ContentRegistry(_contentDatabase.Entries);
            builder.RegisterInstance(registry).As<IContentRegistry>();

            builder.RegisterEntryPoint<BootstrapEntryPoint>();
        }
    }
}
