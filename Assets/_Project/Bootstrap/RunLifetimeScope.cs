using Game.Core;
using Game.Services;
using Game.Simulation.Services;
using Game.UI;
using Game.View;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class RunLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SimulationClock>(Lifetime.Scoped);

            builder.Register<RunOutcome>(Lifetime.Scoped);

            builder.Register<DebugDamageInput>(Lifetime.Scoped).As<IDebugDamageInput>();

            builder.Register<ViewPool>(Lifetime.Scoped).As<IViewFactory>();

            builder.Register<CameraService>(Lifetime.Scoped).As<ICameraService>();

            builder.Register<PlayerVitals>(Lifetime.Scoped).AsSelf().As<IPlayerVitalsSink>();

            builder.RegisterEntryPoint<HudPresenter>(Lifetime.Scoped);

            builder.RegisterEntryPoint<RunEntryPoint>(Lifetime.Scoped);
        }
    }
}
