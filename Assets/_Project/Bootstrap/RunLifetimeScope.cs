using Game.Simulation.Services;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class RunLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SimulationClock>(Lifetime.Scoped);

            builder.RegisterEntryPoint<RunEntryPoint>(Lifetime.Scoped);
        }
    }
}
