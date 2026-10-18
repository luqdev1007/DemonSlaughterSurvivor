using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PunishLeapLandingSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent>> _events = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;
        private readonly EcsFilterInject<Inc<PunishingLeapCooldown>> _cooldowns = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<PunishingLeapCooldown> _cooldownPool = default;
        private readonly EcsPoolInject<ExecutionCooldown> _executionCooldowns = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private readonly List<Candidate> _candidates = new List<Candidate>();

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _cooldowns.Value)
            {
                ref PunishingLeapCooldown cooldown = ref _cooldownPool.Value.Get(entity);

                if (cooldown.RemainingTicks > 0)
                    cooldown.RemainingTicks--;
            }

            EcsWorld world = _world.Value;

            if (Collect(world, out int hero) == false)
                return;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out PunishingLeapConfig punish, out int level) == false)
                return;

            if (_cooldownPool.Value.Has(hero) == false)
                _cooldownPool.Value.Add(hero);

            ref PunishingLeapCooldown own = ref _cooldownPool.Value.Get(hero);

            if (own.RemainingTicks > 0)
                return;

            if (_executionCooldowns.Value.Has(hero) && _executionCooldowns.Value.Get(hero).RemainingTicks > 0)
                return;

            int finished = 0;
            int weakest = 0;

            for (int index = 0; index < _candidates.Count; index++)
            {
                Candidate candidate = _candidates[index];

                if (candidate.Fraction < _candidates[weakest].Fraction)
                    weakest = index;

                if (candidate.Fraction > punish.HealthThreshold)
                    continue;

                Finish(world, punish, candidate);
                finished++;
            }

            if (finished == 0)
                Finish(world, punish, _candidates[weakest]);

            float seconds = punish.Level(level).CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero);

            own.RemainingTicks = Mathf.Max(0, Mathf.RoundToInt(seconds / _clock.Value.Delta));
        }

        private bool Collect(EcsWorld world, out int hero)
        {
            _candidates.Clear();
            hero = -1;

            foreach (int entity in _events.Value)
            {
                ref DamageEvent hit = ref _damageEvents.Value.Get(entity);

                if (hit.SourcePerk is HeroicLeapConfig == false)
                    continue;

                if (hit.Source.Unpack(world, out int source) == false || hit.Target.Unpack(world, out int target) == false)
                    continue;

                if (_enemies.Value.Has(target) == false || _bosses.Value.Has(target) || _dead.Value.Has(target))
                    continue;

                if (_healths.Value.Has(target) == false || _maxHealths.Value.Has(target) == false)
                    continue;

                if (hero >= 0 && source != hero)
                    continue;

                if (Contains(target))
                    continue;

                float current = _healths.Value.Get(target).Current;
                float left = current - hit.Amount;

                if (left <= 0f)
                    continue;

                hero = source;

                _candidates.Add(new Candidate
                {
                    Target = hit.Target,
                    Source = hit.Source,
                    SourcePosition = hit.SourcePosition,
                    TargetEntity = target,
                    Current = current,
                    Fraction = left / Mathf.Max(1e-4f, _maxHealths.Value.Get(target).Value)
                });
            }

            return _candidates.Count > 0;
        }

        private bool Contains(int target)
        {
            for (int index = 0; index < _candidates.Count; index++)
            {
                if (_candidates[index].TargetEntity == target)
                    return true;
            }

            return false;
        }

        private void Finish(EcsWorld world, PunishingLeapConfig punish, in Candidate candidate)
        {
            ref DamageEvent finish = ref _damageEvents.Value.Add(world.NewEntity());
            finish.Target = candidate.Target;
            finish.Source = candidate.Source;
            finish.SourcePosition = candidate.SourcePosition;
            finish.Amount = candidate.Current;
            finish.Kind = DamageKind.Perk;
            finish.IsSlash = true;
            finish.SourcePerk = punish;

            SpawnFx(world, punish, _positions.Value.Get(candidate.TargetEntity).Value);
        }

        private void SpawnFx(EcsWorld world, PunishingLeapConfig punish, Vector3 position)
        {
            if (punish.ImpactFxPrefab == null || punish.ImpactFxSeconds <= 0f)
                return;

            int fx = world.NewEntity();

            _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(punish.ImpactFxSeconds / _clock.Value.Delta));
            _positions.Value.Add(fx).Value = position;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = _viewFactory.Value.Create(punish.ImpactFxPrefab, position);
        }

        private struct Candidate
        {
            public EcsPackedEntity Target;
            public EcsPackedEntity Source;
            public Vector3 SourcePosition;
            public int TargetEntity;
            public float Current;
            public float Fraction;
        }
    }
}
