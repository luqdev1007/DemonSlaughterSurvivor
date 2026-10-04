using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SpawnFireTrailSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int PointCapacity = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Dashing, Position, PreviousPosition>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<FireSegment>> _segments = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<Dashing> _dashes = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<FireTrail> _trails = default;
        private readonly EcsPoolInject<FireSegment> _segmentPool = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private Vector3[] _points;

        public void Init(IEcsSystems systems)
        {
            _points = new Vector3[PointCapacity];
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            int live = _segments.Value.GetEntitiesCount();

            foreach (int hero in _heroes.Value)
            {
                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out FlamingDashConfig config, out int level) == false)
                    continue;

                if (_trails.Value.Has(hero) == false)
                    _trails.Value.Add(hero);

                ref FireTrail trail = ref _trails.Value.Get(hero);
                ref Dashing dashing = ref _dashes.Value.Get(hero);

                if (dashing.RemainingTicks == dashing.TotalTicks)
                    trail.Carried = 0f;

                int count = TrailPlacement.Place(_previousPositions.Value.Get(hero).Value, _positions.Value.Get(hero).Value, config.SegmentSpacing, ref trail.Carried, _points);

                FlamingDashLevel entry = config.Level(level);
                int lifetime = Mathf.Max(1, Mathf.RoundToInt(entry.LifetimeSeconds / _clock.Value.Delta));

                for (int index = 0; index < count; index++)
                {
                    if (live >= config.MaxLiveSegments)
                        break;

                    Spawn(world, hero, config, entry, lifetime, _points[index]);
                    live++;
                }
            }
        }

        private void Spawn(EcsWorld world, int hero, FlamingDashConfig config, FlamingDashLevel entry, int lifetime, Vector3 point)
        {
            int entity = world.NewEntity();

            ref FireSegment segment = ref _segmentPool.Value.Add(entity);
            segment.Owner = world.PackEntity(hero);
            segment.RemainingTicks = lifetime;
            segment.DamageScale = entry.DamageShare;

            _positions.Value.Add(entity).Value = point;
            _views.Value.Add(entity).Value = _viewFactory.Value.Create(config.SegmentPrefab, point);
        }
    }
}
