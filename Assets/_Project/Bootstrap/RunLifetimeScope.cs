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

            builder.Register<DebugGoreInput>(Lifetime.Scoped).As<IDebugGoreInput>();

            builder.Register<GoreDirector>(Lifetime.Scoped);

            builder.Register<CombatAudio>(Lifetime.Scoped);

            builder.Register<ViewPool>(Lifetime.Scoped).As<IViewFactory>().As<IViewPrewarm>().As<ICorpseKickSink>();

            builder.Register<CameraService>(Lifetime.Scoped).AsSelf().As<ICameraService>();

            builder.Register<CursorAim>(Lifetime.Scoped).As<IAimInput>();

            builder.Register<PlayerVitals>(Lifetime.Scoped).AsSelf().As<IPlayerVitalsSink>();

            builder.Register<AbilityPanel>(Lifetime.Scoped).AsSelf().As<IAbilityPanelSink>();

            builder.Register<DamageNumbers>(Lifetime.Scoped).AsSelf().As<IDamageNumberSink>();

            builder.Register<Announcements>(Lifetime.Scoped).AsSelf().As<IAnnouncementSink>();

            builder.Register<RuneStatus>(Lifetime.Scoped).AsSelf().As<IRuneStatusSink>();

            builder.Register<DpsMeter>(Lifetime.Scoped).AsSelf().As<IDamageDealtSink>();

            builder.Register<DpsToggleInput>(Lifetime.Scoped).As<IDpsToggleInput>();

            builder.Register<BossVitals>(Lifetime.Scoped).AsSelf().As<IBossVitalsSink>();

            builder.Register<RunResult>(Lifetime.Scoped).AsSelf().As<IRunResultSink>();

            builder.Register<UpgradeChoice>(Lifetime.Scoped).AsSelf().As<IUpgradeChoiceSink>();

            builder.Register<ItemChoice>(Lifetime.Scoped).AsSelf().As<IItemChoiceSink>();

            builder.Register<PlayerExperience>(Lifetime.Scoped).AsSelf().As<IExperienceSink>();

            builder.Register<PauseMenu>(Lifetime.Scoped).AsSelf().As<IPauseMenuSink>().As<IPauseMenuInput>();

            builder.Register<UpgradeChoiceInput>(Lifetime.Scoped).As<IUpgradeChoiceInput>().As<IUpgradeChoiceSubmit>();

            builder.Register<DebugLevelUpInput>(Lifetime.Scoped).As<IDebugLevelUpInput>();

            builder.Register<DebugVictoryInput>(Lifetime.Scoped).As<IDebugVictoryInput>();

            builder.RegisterEntryPoint<HudPresenter>(Lifetime.Scoped);

            builder.RegisterEntryPoint<RunEntryPoint>(Lifetime.Scoped);
        }
    }
}
