using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PublishAbilityPanelSystem : IEcsRunSystem
    {
        public const string DashId = "ability.dash";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<DashCooldown> _dashCooldowns = default;
        private readonly EcsPoolInject<DashStats> _dashStats = default;
        private readonly EcsPoolInject<BackleapStats> _backleapStats = default;
        private readonly EcsPoolInject<BackleapCooldown> _backleapCooldowns = default;
        private readonly EcsPoolInject<BlinkStats> _blinkStats = default;
        private readonly EcsPoolInject<BlinkCooldown> _blinkCooldowns = default;
        private readonly EcsPoolInject<ExecutionCooldown> _executionCooldowns = default;
        private readonly EcsPoolInject<HeroicLeapCooldown> _leapCooldowns = default;
        private readonly EcsPoolInject<AgileDashCooldown> _agileCooldowns = default;
        private readonly EcsPoolInject<CounterStrikeCooldown> _counterCooldowns = default;
        private readonly EcsPoolInject<SlicingDashCooldown> _slicingCooldowns = default;
        private readonly EcsPoolInject<PunishingLeapCooldown> _punishCooldowns = default;
        private readonly EcsPoolInject<PowerShotCooldown> _powerShotCooldowns = default;
        private readonly EcsPoolInject<ArrowRainCooldown> _arrowRainCooldowns = default;
        private readonly EcsPoolInject<LightningArrowCooldown> _lightningCooldowns = default;

        private readonly EcsCustomInject<IAbilityPanelSink> _sink = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            if (_sink.Value == null)
                return;

            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                if (_dashStats.Value.Has(hero))
                    _sink.Value.Publish(DashId, _dashCooldowns.Value.Has(hero) ? Mathf.Max(0f, _dashCooldowns.Value.Get(hero).Remaining) : 0f);

                if (_backleapStats.Value.Has(hero))
                    _sink.Value.Publish(_backleapStats.Value.Get(hero).Config.Id, _backleapCooldowns.Value.Has(hero) ? Mathf.Max(0f, _backleapCooldowns.Value.Get(hero).Remaining) : 0f);

                if (_blinkStats.Value.Has(hero))
                    _sink.Value.Publish(_blinkStats.Value.Get(hero).Config.Id, _blinkCooldowns.Value.Has(hero) ? Mathf.Max(0f, _blinkCooldowns.Value.Get(hero).Remaining) : 0f);

                foreach (int entity in _taken.Value)
                {
                    ref TakenPerk taken = ref _takenPool.Value.Get(entity);

                    if (taken.Owner.Unpack(world, out int owner) == false || owner != hero)
                        continue;

                    PerkConfig perk = _content.Value.Get<PerkConfig>(taken.PerkId);

                    if (HasCooldown(perk.Behaviour) == false)
                        continue;

                    _sink.Value.Publish(taken.PerkId, ResolveRemainingTicks(hero, perk.Behaviour) * delta);
                }
            }
        }

        private static bool HasCooldown(PerkBehaviourConfig behaviour)
        {
            return behaviour is ExecutionConfig
                || behaviour is HeroicLeapConfig
                || behaviour is AgileDashConfig
                || behaviour is CounterStrikeConfig
                || behaviour is SlicingDashConfig
                || behaviour is PunishingLeapConfig
                || behaviour is PowerShotConfig
                || behaviour is ArrowRainConfig
                || behaviour is LightningArrowConfig;
        }

        private int ResolveRemainingTicks(int hero, PerkBehaviourConfig behaviour)
        {
            if (behaviour is ExecutionConfig && _executionCooldowns.Value.Has(hero))
                return Mathf.Max(0, _executionCooldowns.Value.Get(hero).RemainingTicks);

            if (behaviour is PowerShotConfig && _powerShotCooldowns.Value.Has(hero))
                return Mathf.Max(0, _powerShotCooldowns.Value.Get(hero).RemainingTicks);

            if (behaviour is LightningArrowConfig && _lightningCooldowns.Value.Has(hero))
                return Mathf.Max(0, _lightningCooldowns.Value.Get(hero).RemainingTicks);

            if (behaviour is ArrowRainConfig && _arrowRainCooldowns.Value.Has(hero))
                return Mathf.Max(0, _arrowRainCooldowns.Value.Get(hero).RemainingTicks);

            if (behaviour is HeroicLeapConfig && _leapCooldowns.Value.Has(hero))
                return Mathf.Max(0, _leapCooldowns.Value.Get(hero).RemainingTicks);

            if (behaviour is AgileDashConfig && _agileCooldowns.Value.Has(hero))
                return Mathf.Max(0, _agileCooldowns.Value.Get(hero).RemainingTicks);

            if (behaviour is CounterStrikeConfig && _counterCooldowns.Value.Has(hero))
                return Mathf.Max(0, _counterCooldowns.Value.Get(hero).RemainingTicks);

            if (behaviour is SlicingDashConfig && _slicingCooldowns.Value.Has(hero))
                return Mathf.Max(0, _slicingCooldowns.Value.Get(hero).RemainingTicks);

            if (behaviour is PunishingLeapConfig && _punishCooldowns.Value.Has(hero))
                return Mathf.Max(0, _punishCooldowns.Value.Get(hero).RemainingTicks);

            return 0;
        }
    }
}
