using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ElementalWeaknessSystem : IEcsRunSystem
    {
        public const string StreamId = "stream.elemental-weakness";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<ElementMark>> _marked = default;
        private readonly EcsFilterInject<Inc<Enemy, Position>, Exc<Dead, ElementMark>> _candidates = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<ElementalWeaknessState> _states = default;
        private readonly EcsPoolInject<ElementMark> _marks = default;
        private readonly EcsPoolInject<StatusFx> _statusFx = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<RunContext> _context = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int entity in _marked.Value)
            {
                if (--_marks.Value.Get(entity).RemainingTicks <= 0)
                    _marks.Value.Del(entity);
            }

            foreach (int hero in _heroes.Value)
            {
                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out ElementalWeaknessConfig config, out int level) == false)
                    continue;

                if (_states.Value.Has(hero) == false)
                {
                    ref ElementalWeaknessState fresh = ref _states.Value.Add(hero);
                    fresh.Random = SimulationRandom.StreamState(_context.Value.Seed, StreamId);
                }

                ref ElementalWeaknessState state = ref _states.Value.Get(hero);

                if (--state.Countdown > 0)
                    continue;

                ElementalWeaknessLevel entry = config.Level(level);
                state.Countdown = Mathf.Max(1, Mathf.RoundToInt(entry.IntervalSeconds / delta));

                Vector3 origin = _positions.Value.Get(hero).Value;

                for (int mark = 0; mark < entry.MarksPerPulse; mark++)
                {
                    if (TryPick(origin, config.Range, ref state.Random, out int enemy) == false)
                        break;

                    Mark(world, hero, enemy, config, entry, ref state.Random, delta);
                }
            }
        }

        private bool TryPick(Vector3 origin, float range, ref uint random, out int picked)
        {
            picked = -1;
            int seen = 0;

            foreach (int enemy in _candidates.Value)
            {
                Vector3 offset = _positions.Value.Get(enemy).Value - origin;

                if (offset.x * offset.x + offset.z * offset.z > range * range)
                    continue;

                seen++;

                if (SimulationRandom.NextUnit(ref random) * seen < 1f)
                    picked = enemy;
            }

            return picked >= 0;
        }

        private void Mark(EcsWorld world, int hero, int enemy, ElementalWeaknessConfig config, ElementalWeaknessLevel entry, ref uint random, float delta)
        {
            DamageElement element = (DamageElement)(1 + Mathf.Min(2, (int)(SimulationRandom.NextUnit(ref random) * 3f)));

            ref ElementMark mark = ref _marks.Value.Add(enemy);
            mark.Config = config;
            mark.Source = world.PackEntity(hero);
            mark.Element = element;
            mark.DamageBonus = entry.DamageBonus;
            mark.ExplosionDamage = entry.ExplosionDamage;
            mark.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.MarkSeconds / delta));
            mark.HasFx = true;

            Vector3 at = _positions.Value.Get(enemy).Value;
            int fx = world.NewEntity();

            ref StatusFx status = ref _statusFx.Value.Add(fx);
            status.Owner = world.PackEntity(enemy);
            status.Kind = StatusKind.Marked;
            status.Height = config.MarkHeight;

            _positions.Value.Add(fx).Value = at;
            _facings.Value.Add(fx).Value = Vector3.forward;
            _notIndexed.Value.Add(fx);

            IView view = _viewFactory.Value.Create(MarkPrefab(config, element), at);
            _views.Value.Add(fx).Value = view;

            if (view == null)
                return;

            view.SetLift(config.MarkHeight);
            view.SetLift(config.MarkHeight);
        }

        public static GameObject MarkPrefab(ElementalWeaknessConfig config, DamageElement element)
        {
            return element == DamageElement.Fire ? config.FireMarkPrefab : element == DamageElement.Ice ? config.IceMarkPrefab : config.ArcaneMarkPrefab;
        }

        public static GameObject ExplosionPrefab(ElementalWeaknessConfig config, DamageElement element)
        {
            return element == DamageElement.Fire ? config.FireExplosionPrefab : element == DamageElement.Ice ? config.IceExplosionPrefab : config.ArcaneExplosionPrefab;
        }
    }
}
