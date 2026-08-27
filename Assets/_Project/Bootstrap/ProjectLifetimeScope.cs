using Game.Configs;
using Game.Core;
using Game.Services;
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private ContentDatabase _contentDatabase;
        [SerializeField] private ScenesConfig _scenesConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            // Both references live only in the root prefab, so they can be cleared silently.
            // Fail here, loudly, instead of resolving into a null three screens later.
            if (_contentDatabase == null)
                throw new InvalidOperationException(
                    $"{nameof(ContentDatabase)} is not assigned on the {nameof(ProjectLifetimeScope)} prefab.");

            if (_scenesConfig == null)
                throw new InvalidOperationException(
                    $"{nameof(ScenesConfig)} is not assigned on the {nameof(ProjectLifetimeScope)} prefab.");

            builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();

            builder.Register<SeedSource>(Lifetime.Singleton).As<ISeedSource>();

            builder.Register<RunLauncher>(Lifetime.Singleton).As<IRunLauncher>();

            // Built by hand, not by the container: the registry must validate the whole
            // database while the project scope is being built, not on the first Get.
            builder.RegisterInstance(new ContentRegistry(_contentDatabase.Entries)).As<IContentRegistry>();

            builder.RegisterInstance(_scenesConfig);

            builder.RegisterEntryPoint<BootstrapEntryPoint>();
        }
    }
}
