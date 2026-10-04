using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceSwingSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int SegmentCapacity = 64;
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Swing>> _swings = default;

        private readonly EcsPoolInject<Swing> _swingPool = default;
        private readonly EcsPoolInject<Swinging> _swinging = default;
        private readonly EcsPoolInject<Weapon> _weapons = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<BodyHeight> _bodyHeights = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private Vector3[] _hands;
        private Vector3[] _tips;
        private List<int> _candidates;

        public void Init(IEcsSystems systems)
        {
            _hands = new Vector3[SegmentCapacity];
            _tips = new Vector3[SegmentCapacity];
            _candidates = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CandidateCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int entity in _swings.Value)
            {
                ref Swing swing = ref _swingPool.Value.Get(entity);

                if (swing.Weapon.Unpack(world, out int weapon) == false || swing.Owner.Unpack(world, out int owner) == false)
                {
                    world.DelEntity(entity);

                    continue;
                }

                WeaponConfig config = _weapons.Value.Get(weapon).Config;
                SwingVariant variant = config.Variant(swing.Variant);
                SwingBake bake = variant.Bake;

                Vector3 ownerPosition = _positions.Value.Get(owner).Value;
                SwingPose pose = new SwingPose(ownerPosition, _facings.Value.Get(owner).Value);

                float from = swing.ClipTime;
                bool lastTick = swing.RemainingTicks <= 1;
                float to = lastTick ? bake.ClipLength : Math.Min(from + delta * swing.PlaybackSpeed, bake.ClipLength);

                int count = SwingSweep.BuildSegments(bake, from, to, variant.WindowStart, variant.WindowEnd, swing.PreviousHand, swing.PreviousTip, pose, _hands, _tips);

                if (count > 0)
                    Strike(world, ref swing, owner, ownerPosition, weapon, variant, pose, count, delta);

                bake.SampleAt(to, out Vector3 hand, out Vector3 tip);

                swing.PreviousHand = pose.ToWorld(hand);
                swing.PreviousTip = pose.ToWorld(tip);
                swing.ClipTime = to;
                swing.RemainingTicks--;

                if (lastTick == false)
                    continue;

                if (_swinging.Value.Has(weapon))
                    _swinging.Value.Del(weapon);

                world.DelEntity(entity);
            }
        }

        private void Strike(EcsWorld world, ref Swing swing, int owner, Vector3 ownerPosition, int weapon, SwingVariant variant, in SwingPose pose, int count, float delta)
        {
            float reach = 0f;

            for (int index = 0; index < count; index++)
            {
                reach = Math.Max(reach, FlatDistance(_hands[index], ownerPosition));
                reach = Math.Max(reach, FlatDistance(_tips[index], ownerPosition));
            }

            float radius = reach + _motionBounds.Value.MaxEnemyBodyRadius;

            _grid.Value.Query(ownerPosition, radius, _motionBounds.Value.CandidateSlack(delta), _candidates);

            float damage = _weaponDamages.Value.Get(weapon).Value;

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false)
                    continue;

                if (_bodyRadii.Value.Has(candidate) == false || _bodyHeights.Value.Has(candidate) == false)
                    continue;

                EcsPackedEntity packed = world.PackEntity(candidate);

                if (AlreadyHit(ref swing, packed))
                    continue;

                Vector3 targetPosition = _positions.Value.Get(candidate).Value;

                bool hit = SwingSweep.Hits(
                    _hands,
                    _tips,
                    count,
                    pose,
                    variant.HalfAngleDegrees,
                    targetPosition,
                    _bodyRadii.Value.Get(candidate).Value,
                    _bodyHeights.Value.Get(candidate).Value);

                if (hit == false)
                    continue;

                RememberHit(ref swing, packed);

                int eventEntity = world.NewEntity();

                ref DamageEvent damageEvent = ref _damageEvents.Value.Add(eventEntity);
                damageEvent.Target = packed;
                damageEvent.Source = world.PackEntity(owner);
                damageEvent.SourcePosition = ownerPosition;
                damageEvent.Amount = damage;
                damageEvent.Kind = DamageKind.Weapon;
            }
        }

        private static bool AlreadyHit(ref Swing swing, in EcsPackedEntity candidate)
        {
            for (int index = 0; index < swing.HitCount; index++)
            {
                if (swing.Hits[index].EqualsTo(candidate))
                    return true;
            }

            return false;
        }

        private static void RememberHit(ref Swing swing, in EcsPackedEntity candidate)
        {
            if (swing.HitCount == swing.Hits.Length)
                Array.Resize(ref swing.Hits, swing.Hits.Length * 2);

            swing.Hits[swing.HitCount] = candidate;
            swing.HitCount++;
        }

        private static float FlatDistance(Vector3 point, Vector3 origin)
        {
            float x = point.x - origin.x;
            float z = point.z - origin.z;

            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
