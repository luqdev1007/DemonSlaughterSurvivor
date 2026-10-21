using Game.Configs;
using Game.Core;
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
        private const float SweepMinReach = 0.05f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Swing>> _swings = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<Swing> _swingPool = default;
        private readonly EcsPoolInject<Swinging> _swinging = default;
        private readonly EcsPoolInject<SpecialSwing> _special = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<Weapon> _weaponPool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<BodyHeight> _bodyHeights = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<SwingEndedEvent> _endedEvents = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Pushed> _pushes = default;
        private readonly EcsPoolInject<NotPushable> _notPushables = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

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

                SwingVariant variant = swing.Variant;
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

                if (_special.Value.Has(entity) == false && _swinging.Value.Has(weapon))
                    _swinging.Value.Del(weapon);

                ref SwingEndedEvent ended = ref _endedEvents.Value.Add(world.NewEntity());
                ended.Owner = swing.Owner;
                ended.EnemyHits = swing.EnemyHits;

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

            float bladeReach = reach * swing.Reach;
            bool sweeping = TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out SweepingStrikesConfig sweep, out int sweepLevel);
            SweepingStrikesLevel sweepEntry = sweeping ? sweep.Level(sweepLevel) : null;
            float sweepReach = sweeping && bladeReach > SweepMinReach ? swing.Reach * (bladeReach + sweepEntry.ExtraRadius) / bladeReach : 0f;
            float radius = bladeReach + (sweeping ? sweepEntry.ExtraRadius : 0f) + _motionBounds.Value.MaxEnemyBodyRadius;

            _grid.Value.Query(ownerPosition, radius, _motionBounds.Value.CandidateSlack(delta), _candidates);

            float damage = _weaponDamages.Value.Get(weapon).Value * swing.DamageScale;
            WeaponConfig weaponConfig = _weaponPool.Value.Has(weapon) ? _weaponPool.Value.Get(weapon).Config : null;
            float arcBonus = weaponConfig != null ? weaponConfig.ArcBonusDegrees : 0f;
            float pushSpeed = swing.PushSpeed;
            int pushTicks = swing.PushTicks;

            if (pushTicks <= 0 && weaponConfig != null && weaponConfig.HitPushSpeed > 0f && weaponConfig.HitPushSeconds > 0f)
            {
                pushSpeed = weaponConfig.HitPushSpeed;
                pushTicks = Mathf.Max(1, Mathf.RoundToInt(weaponConfig.HitPushSeconds / delta));
            }

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false && _breakables.Value.Has(candidate) == false)
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
                    variant.HalfAngleDegrees + arcBonus,
                    targetPosition,
                    _bodyRadii.Value.Get(candidate).Value,
                    _bodyHeights.Value.Get(candidate).Value,
                    swing.Reach);

                if (hit == false)
                {
                    if (sweepReach > 0f && SwingSweep.Hits(
                            _hands,
                            _tips,
                            count,
                            pose,
                            variant.HalfAngleDegrees + arcBonus,
                            targetPosition,
                            _bodyRadii.Value.Get(candidate).Value,
                            _bodyHeights.Value.Get(candidate).Value,
                            sweepReach))
                    {
                        RememberHit(ref swing, packed);
                        SweepHit(world, packed, owner, ownerPosition, damage * sweepEntry.DamageShare, sweep);
                        Push(candidate, ownerPosition, targetPosition, pose.Forward, pushSpeed, pushTicks);
                    }

                    continue;
                }

                RememberHit(ref swing, packed);

                if (_breakables.Value.Has(candidate) == false)
                    swing.EnemyHits++;

                int eventEntity = world.NewEntity();

                ref DamageEvent damageEvent = ref _damageEvents.Value.Add(eventEntity);
                damageEvent.Target = packed;
                damageEvent.Source = world.PackEntity(owner);
                damageEvent.SourcePosition = ownerPosition;
                damageEvent.Amount = damage;
                damageEvent.Kind = swing.Kind;
                damageEvent.IsSlash = true;
                damageEvent.SourcePerk = swing.SourcePerk;

                Push(candidate, ownerPosition, targetPosition, pose.Forward, pushSpeed, pushTicks);
            }
        }

        private void Push(int candidate, Vector3 ownerPosition, Vector3 targetPosition, Vector3 forward, float speed, int ticks)
        {
            if (ticks <= 0 || speed <= 0f || _velocities.Value.Has(candidate) == false || _notPushables.Value.Has(candidate))
                return;

            Vector3 direction = PushFromPoint.Direction(ownerPosition, targetPosition, forward);

            PushFromPoint.Apply(_pushes.Value, candidate, direction, speed, ticks);
        }

        private void SweepHit(EcsWorld world, EcsPackedEntity target, int owner, Vector3 ownerPosition, float amount, SweepingStrikesConfig sweep)
        {
            ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
            damageEvent.Target = target;
            damageEvent.Source = world.PackEntity(owner);
            damageEvent.SourcePosition = ownerPosition;
            damageEvent.Amount = amount;
            damageEvent.Kind = DamageKind.Perk;
            damageEvent.SourcePerk = sweep;
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
