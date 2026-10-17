using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PunishLeapLandingSystem : IEcsRunSystem
    {
        public const string RandomStreamId = "stream.punishing-leap";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent>> _events = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<PunishingLeapRandom> _randoms = default;
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
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _events.Value)
            {
                DamageEvent hit = _damageEvents.Value.Get(entity);

                if (hit.SourcePerk is HeroicLeapConfig == false)
                    continue;

                if (hit.Source.Unpack(world, out int hero) == false || hit.Target.Unpack(world, out int target) == false)
                    continue;

                if (_enemies.Value.Has(target) == false || _bosses.Value.Has(target) || _dead.Value.Has(target))
                    continue;

                if (_healths.Value.Has(target) == false || _maxHealths.Value.Has(target) == false)
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out PunishingLeapConfig punish, out int level) == false)
                    continue;

                float current = _healths.Value.Get(target).Current;
                float left = current - hit.Amount;

                if (left <= 0f || left > punish.HealthThreshold * _maxHealths.Value.Get(target).Value)
                    continue;

                if (SimulationRandom.NextUnit(ref ResolveRandom(hero).State) >= punish.Level(level).Chance)
                    continue;

                Vector3 position = _positions.Value.Get(target).Value;

                ref DamageEvent finish = ref _damageEvents.Value.Add(world.NewEntity());
                finish.Target = hit.Target;
                finish.Source = hit.Source;
                finish.SourcePosition = hit.SourcePosition;
                finish.Amount = current;
                finish.Kind = DamageKind.Perk;
                finish.IsSlash = true;
                finish.SourcePerk = punish;

                SpawnFx(world, punish, position);
            }
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

        private ref PunishingLeapRandom ResolveRandom(int hero)
        {
            if (_randoms.Value.Has(hero))
                return ref _randoms.Value.Get(hero);

            ref PunishingLeapRandom random = ref _randoms.Value.Add(hero);
            random.State = SimulationRandom.StreamState(_context.Value.Seed, RandomStreamId);

            return ref random;
        }
    }
}
