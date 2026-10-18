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
    public sealed class AdvanceArrowsSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Projectile, Arrow, Position>> _arrows = default;

        private readonly EcsPoolInject<Projectile> _projectiles = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        private List<int> _candidates;

        public void Init(IEcsSystems systems)
        {
            _candidates = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CandidateCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int arrow in _arrows.Value)
            {
                ref Projectile projectile = ref _projectiles.Value.Get(arrow);

                Vector3 from = projectile.LastPosition;
                Vector3 to = _positions.Value.Get(arrow).Value;

                if (TryHit(world, in projectile, from, to, out int target))
                {
                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
                    damageEvent.Target = world.PackEntity(target);
                    damageEvent.Source = projectile.Source;
                    damageEvent.SourcePosition = to;
                    damageEvent.Amount = projectile.Damage;
                    damageEvent.Kind = DamageKind.Weapon;

                    Remove(world, arrow);

                    continue;
                }

                if (projectile.RemainingTicks <= 0)
                {
                    Remove(world, arrow);

                    continue;
                }

                projectile.RemainingTicks--;
                projectile.LastPosition = to;
            }
        }

        private bool TryHit(EcsWorld world, in Projectile projectile, Vector3 from, Vector3 to, out int hit)
        {
            hit = -1;

            Vector3 middle = (from + to) * 0.5f;
            float halfLength = Vector3.Distance(from, to) * 0.5f;
            float radius = halfLength + projectile.Radius + _motionBounds.Value.MaxEnemyBodyRadius;

            _grid.Value.Query(middle, radius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            float earliest = float.MaxValue;

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if ((_enemies.Value.Has(candidate) == false && _breakables.Value.Has(candidate) == false) || _dead.Value.Has(candidate))
                    continue;

                if (_healths.Value.Has(candidate) == false || _bodyRadii.Value.Has(candidate) == false)
                    continue;

                Vector3 bodyTo = _positions.Value.Get(candidate).Value;
                Vector3 bodyFrom = _previousPositions.Value.Has(candidate) ? _previousPositions.Value.Get(candidate).Value : bodyTo;
                float contact = projectile.Radius + _bodyRadii.Value.Get(candidate).Value;

                if (RangedTarget.SweptHit(from, to, bodyFrom, bodyTo, contact, out float time) == false)
                    continue;

                if (time >= earliest)
                    continue;

                earliest = time;
                hit = candidate;
            }

            return hit >= 0;
        }

        private void Remove(EcsWorld world, int arrow)
        {
            if (_views.Value.Has(arrow))
            {
                ref View view = ref _views.Value.Get(arrow);

                if (view.Value != null)
                    _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }

            world.DelEntity(arrow);
        }
    }
}
