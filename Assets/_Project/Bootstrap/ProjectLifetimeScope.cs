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
            if (_contentDatabase == null)
                throw new InvalidOperationException(
                    $"{nameof(ContentDatabase)} is not assigned on the {nameof(ProjectLifetimeScope)} prefab.");

            if (_scenesConfig == null)
                throw new InvalidOperationException(
                    $"{nameof(ScenesConfig)} is not assigned on the {nameof(ProjectLifetimeScope)} prefab.");

            builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();

            builder.Register<SeedSource>(Lifetime.Singleton).As<ISeedSource>();

            builder.Register<RunLauncher>(Lifetime.Singleton).As<IRunLauncher>();

            builder.Register<InputService>(Lifetime.Singleton).As<IInputService>();

            builder.RegisterInstance(new ContentRegistry(_contentDatabase.Entries)).As<IContentRegistry>();

            builder.RegisterInstance(_scenesConfig);

            builder.RegisterEntryPoint<BootstrapEntryPoint>();
        }
    }
}
