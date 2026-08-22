using Game.Core;
using Game.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameObject _loadingScreenPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();

            builder.Register<RunLauncher>(Lifetime.Singleton).As<IRunLauncher>();

            builder.RegisterEntryPoint<BootstrapEntryPoint>();
        }
    }
}
