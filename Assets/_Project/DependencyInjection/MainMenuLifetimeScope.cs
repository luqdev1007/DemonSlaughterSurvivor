using DemonSlaughter.UI.MainMenu;
using VContainer;
using VContainer.Unity;

namespace DemonSlaughter.DependencyInjection
{
    public sealed class MainMenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MainMenuView>();
            builder.Register<MainMenuPresenter>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MainMenuPresenter>();
        }
    }
}