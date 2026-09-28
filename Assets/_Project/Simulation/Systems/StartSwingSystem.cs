using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartSwingSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Weapon, WeaponReady, WeaponCooldown, AttackSpeed, SwingRandom, OwnerLink>, Exc<Swinging>> _weapons = default;

        private readonly EcsPoolInject<Weapon> _weaponPool = default;
        private readonly EcsPoolInject<WeaponReady> _readies = default;
        private readonly EcsPoolInject<WeaponCooldown> _cooldowns = default;
        private readonly EcsPoolInject<AttackSpeed> _attackSpeeds = default;
        private readonly EcsPoolInject<SwingRandom> _randoms = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<Swinging> _swinging = default;
        private readonly EcsPoolInject<Swing> _swings = default;
        private readonly EcsPoolInject<SwingStarted> _started = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<MoveSpeed> _speeds = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Dashing> _dashing = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private List<int> _candidates;

        public void Init(IEcsSystems systems)
        {
            _candidates = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CandidateCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int weapon in _weapons.Value)
            {
                ref WeaponReady ready = ref _readies.Value.Get(weapon);

                if (ready.Remaining > 0f)
                    continue;

                ref OwnerLink link = ref _ownerLinks.Value.Get(weapon);

                if (link.Owner.Unpack(world, out int owner) == false)
                    continue;

                if (_dead.Value.Has(owner) || _dashing.Value.Has(owner))
                    continue;

                if (_positions.Value.Has(owner) == false || _facings.Value.Has(owner) == false)
                    continue;

                WeaponConfig config = _weaponPool.Value.Get(weapon).Config;
                SwingPose pose = new SwingPose(_positions.Value.Get(owner).Value, _facings.Value.Get(owner).Value);
                float attackSpeed = _attackSpeeds.Value.Get(weapon).Value;

                if (HasTarget(config, attackSpeed, pose) == false)
                    continue;

                ref SwingRandom random = ref _randoms.Value.Get(weapon);

                int variant = PickVariant(config, ref random.State);

                StartSwing(world, weapon, owner, config, variant, attackSpeed, pose);

                ready.Remaining = _cooldowns.Value.Get(weapon).Value / attackSpeed;
            }
        }

        private bool HasTarget(WeaponConfig config, float attackSpeed, in SwingPose pose)
        {
            SwingCoverage coverage = config.TriggerCoverage;
            float lead = ResolveShortestWindup(config, attackSpeed);

            float radius = coverage.MaxDistance + SwingCoverage.DistanceStep * 0.5f + _motionBounds.Value.MaxEnemyMoveSpeed * lead;

            _grid.Value.Query(pose.Position, radius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false)
                    continue;

                Vector3 position = _positions.Value.Get(candidate).Value;

                if (Covers(coverage, pose, position))
                    return true;

                if (_intents.Value.Has(candidate) == false || _speeds.Value.Has(candidate) == false)
                    continue;

                Vector3 walk = _intents.Value.Get(candidate).Value * _speeds.Value.Get(candidate).Value;

                if (Covers(coverage, pose, position + walk * lead))
                    return true;
            }

            return false;
        }

        private static bool Covers(SwingCoverage coverage, in SwingPose pose, Vector3 position)
        {
            Vector3 local = pose.ToLocal(position);

            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float distance = Mathf.Sqrt(local.x * local.x + local.z * local.z);

            return coverage.Covers(angle, distance);
        }

        private static float ResolveShortestWindup(WeaponConfig config, float attackSpeed)
        {
            float shortest = float.MaxValue;

            for (int index = 0; index < config.VariantCount; index++)
            {
                SwingVariant variant = config.Variant(index);

                shortest = Mathf.Min(shortest, variant.WindowStart / (variant.PlaybackSpeed * attackSpeed));
            }

            return shortest;
        }

        private static int PickVariant(WeaponConfig config, ref uint state)
        {
            float roll = SimulationRandom.NextUnit(ref state);

            int variant = (int)(roll * config.VariantCount);

            if (variant >= config.VariantCount)
                variant = config.VariantCount - 1;

            return variant;
        }

        private void StartSwing(EcsWorld world, int weapon, int owner, WeaponConfig config, int variant, float attackSpeed, in SwingPose pose)
        {
            SwingVariant chosen = config.Variant(variant);
            SwingBake bake = chosen.Bake;

            float delta = _clock.Value.Delta;
            float requestedSpeed = chosen.PlaybackSpeed * attackSpeed;
            int ticks = Mathf.Max(1, Mathf.RoundToInt(bake.ClipLength / requestedSpeed / delta));

            int entity = world.NewEntity();

            ref Swing swing = ref _swings.Value.Add(entity);
            swing.Weapon = world.PackEntity(weapon);
            swing.Owner = world.PackEntity(owner);
            swing.Variant = variant;
            swing.ClipTime = 0f;
            swing.PlaybackSpeed = bake.ClipLength / (ticks * delta);
            swing.RemainingTicks = ticks;

            bake.SampleAt(0f, out Vector3 hand, out Vector3 tip);

            swing.PreviousHand = pose.ToWorld(hand);
            swing.PreviousTip = pose.ToWorld(tip);

            _started.Value.Add(entity);
            _swinging.Value.Add(weapon);
        }
    }
}
