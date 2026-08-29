using Game.Core;
using Game.Simulation.Services;
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

            builder.Register<ViewFactory>(Lifetime.Scoped).As<IViewFactory>();

            builder.RegisterEntryPoint<RunEntryPoint>(Lifetime.Scoped);
        }
    }
}
