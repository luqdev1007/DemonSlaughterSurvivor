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
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<MoveSpeed> _speeds = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Dashing> _dashing = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;
        private readonly EcsPoolInject<PoweringUp> _poweringUps = default;
        private readonly EcsPoolInject<InputState> _inputStates = default;

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

                if (_dead.Value.Has(owner) || _dashing.Value.Has(owner) || _specialAttacks.Value.Has(owner) || _poweringUps.Value.Has(owner))
                    continue;

                if (_positions.Value.Has(owner) == false || _facings.Value.Has(owner) == false)
                    continue;

                WeaponConfig config = _weaponPool.Value.Get(weapon).Config;
                SwingPose pose = new SwingPose(_positions.Value.Get(owner).Value, _facings.Value.Get(owner).Value);
                float attackSpeed = _attackSpeeds.Value.Get(weapon).Value;

                ref SwingRandom random = ref _randoms.Value.Get(weapon);
                int variant;

                if (HasTarget(config, attackSpeed, pose, out Vector3 target))
                {
                    variant = PickVariant(config, pose, target, ref random.State);
                }
                else if (TryPredict(owner, config, ref pose, out variant) == false)
                {
                    continue;
                }

                StartSwing(world, weapon, owner, config, variant, attackSpeed, pose);

                ready.Remaining = _cooldowns.Value.Get(weapon).Value / attackSpeed;
            }
        }

        private bool HasTarget(WeaponConfig config, float attackSpeed, in SwingPose pose, out Vector3 target)
        {
            target = default;

            SwingCoverage coverage = config.TriggerCoverage;
            float lead = ResolveShortestWindup(config, attackSpeed);

            float radius = coverage.MaxDistance + SwingCoverage.DistanceStep * 0.5f + _motionBounds.Value.MaxEnemyMoveSpeed * lead;

            _grid.Value.Query(pose.Position, radius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false && _breakables.Value.Has(candidate) == false)
                    continue;

                Vector3 position = _positions.Value.Get(candidate).Value;

                if (Covers(coverage, pose, position))
                {
                    target = position;

                    return true;
                }

                if (_intents.Value.Has(candidate) == false || _speeds.Value.Has(candidate) == false)
                    continue;

                Vector3 walk = _intents.Value.Get(candidate).Value * _speeds.Value.Get(candidate).Value;

                if (Covers(coverage, pose, position + walk * lead))
                {
                    target = position + walk * lead;

                    return true;
                }
            }

            return false;
        }

        private bool TryPredict(int owner, WeaponConfig config, ref SwingPose pose, out int variant)
        {
            variant = -1;

            if (_inputStates.Value.Has(owner) == false)
                return false;

            ref InputState state = ref _inputStates.Value.Get(owner);

            if (state.PredictiveLead == 0f)
                return false;

            Vector3 facing = Quaternion.Euler(0f, state.PredictiveLead, 0f) * _facings.Value.Get(owner).Value;
            SwingPose predicted = new SwingPose(pose.Position, facing);
            float stretch = Mathf.Max(1f, state.PredictiveStretch);
            SwingCoverage coverage = config.TriggerCoverage;

            _grid.Value.Query(pose.Position, coverage.MaxDistance * stretch, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false && _breakables.Value.Has(candidate) == false)
                    continue;

                Vector3 local = predicted.ToLocal(_positions.Value.Get(candidate).Value);
                float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                float distance = Mathf.Sqrt(local.x * local.x + local.z * local.z) / stretch;

                if (coverage.Covers(angle, distance) == false)
                    continue;

                variant = FindVariant(config, state.PredictiveSwing);

                if (variant < 0)
                    variant = 0;

                pose = predicted;
                _facings.Value.Get(owner).Value = facing;

                return true;
            }

            return false;
        }

        private static int FindVariant(WeaponConfig config, string trigger)
        {
            for (int index = 0; index < config.VariantCount; index++)
            {
                if (config.Variant(index).AnimatorTrigger == trigger)
                    return index;
            }

            return -1;
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

        private static int PickVariant(WeaponConfig config, in SwingPose pose, Vector3 target, ref uint state)
        {
            float roll = SimulationRandom.NextUnit(ref state);
            float total = 0f;
            int last = 0;

            for (int index = 0; index < config.VariantCount; index++)
            {
                if (IsEligible(config.Variant(index), pose, target))
                    total += config.Variant(index).Weight;
            }

            float pick = roll * total;
            float cumulative = 0f;

            for (int index = 0; index < config.VariantCount; index++)
            {
                SwingVariant variant = config.Variant(index);

                if (IsEligible(variant, pose, target) == false)
                    continue;

                cumulative += variant.Weight;
                last = index;

                if (pick < cumulative)
                    return index;
            }

            return last;
        }

        private static bool IsEligible(SwingVariant variant, in SwingPose pose, Vector3 target)
        {
            return variant.DefinesTrigger || Covers(variant.Coverage, pose, target);
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
            swing.Variant = chosen;
            swing.DamageScale = 1f;
            swing.Kind = DamageKind.Weapon;
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
