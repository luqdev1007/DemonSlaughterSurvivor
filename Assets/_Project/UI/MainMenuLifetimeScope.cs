using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    public sealed class MainMenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<DebugRunStarter>();
        }
    }
}
