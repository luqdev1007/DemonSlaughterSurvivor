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
    public sealed class ExecutionWavesSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<ExecutionFinishedEvent>> _finished = default;
        private readonly EcsFilterInject<Inc<ForceWave>> _waves = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink, WeaponDamage>> _weapons = default;

        private readonly EcsPoolInject<ExecutionFinishedEvent> _finishedPool = default;
        private readonly EcsPoolInject<ForceWave> _wavePool = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Pushed> _pushes = default;
        private readonly EcsPoolInject<NotPushable> _notPushables = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<LeapImpactFx> _bursts = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
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
            float delta = _clock.Value.Delta;

            foreach (int entity in _finished.Value)
            {
                if (_finishedPool.Value.Get(entity).Owner.Unpack(world, out int owner) == false || _dead.Value.Has(owner))
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out ExecutionWavesConfig config, out int level) == false)
                    continue;

                if (TryFindWeaponDamage(world, owner, out float weaponDamage) == false)
                    continue;

                Release(world, owner, config, weaponDamage * config.Level(level).DamageShare);
            }

            foreach (int entity in _waves.Value)
            {
                ref ForceWave wave = ref _wavePool.Value.Get(entity);

                float previous = wave.Travelled;

                wave.Travelled = Mathf.Min(wave.Config.Range, previous + wave.Config.Speed * delta);

                if (wave.Owner.Unpack(world, out int owner))
                    Strike(world, ref wave, owner, previous, delta);

                Burst(world, ref wave, delta);

                if (wave.Travelled >= wave.Config.Range)
                    world.DelEntity(entity);
            }
        }

        private bool TryFindWeaponDamage(EcsWorld world, int hero, out float damage)
        {
            foreach (int entity in _weapons.Value)
            {
                if (_ownerLinks.Value.Get(entity).Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                damage = _weaponDamages.Value.Get(entity).Value;

                return true;
            }

            damage = 0f;

            return false;
        }

        private void Release(EcsWorld world, int owner, ExecutionWavesConfig config, float damage)
        {
            Vector3 origin = _positions.Value.Get(owner).Value;
            Vector3 forward = PushFromPoint.Direction(Vector3.zero, _facings.Value.Get(owner).Value, Vector3.forward);
            float step = config.ConeDegrees / config.WaveCount;
            float first = -config.ConeDegrees * 0.5f + step * 0.5f;

            for (int index = 0; index < config.WaveCount; index++)
            {
                ref ForceWave wave = ref _wavePool.Value.Add(world.NewEntity());
                wave.Owner = world.PackEntity(owner);
                wave.Config = config;
                wave.Origin = origin;
                wave.Direction = Quaternion.AngleAxis(first + step * index, Vector3.up) * forward;
                wave.Travelled = config.StartDistance;
                wave.NextBurst = config.StartDistance;
                wave.Damage = damage;
            }
        }

        private void Strike(EcsWorld world, ref ForceWave wave, int owner, float previous, float delta)
        {
            ExecutionWavesConfig config = wave.Config;
            int pushTicks = Mathf.Max(1, Mathf.RoundToInt(config.PushSeconds / delta));
            bool pushes = config.PushSpeed > 0f && config.PushSeconds > 0f;
            Vector3 from = wave.Origin + wave.Direction * previous;
            Vector3 to = wave.Origin + wave.Direction * wave.Travelled;
            float halfStep = (wave.Travelled - previous) * 0.5f;

            _grid.Value.Query((from + to) * 0.5f, halfStep + config.HalfWidth + _motionBounds.Value.MaxEnemyBodyRadius, _motionBounds.Value.CandidateSlack(delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false && _breakables.Value.Has(candidate) == false)
                    continue;

                if (_dead.Value.Has(candidate) || _bodyRadii.Value.Has(candidate) == false)
                    continue;

                Vector3 offset = _positions.Value.Get(candidate).Value - wave.Origin;
                offset.y = 0f;

                float along = Vector3.Dot(offset, wave.Direction);
                float body = _bodyRadii.Value.Get(candidate).Value;
                Vector3 lateral = offset - wave.Direction * along;
                float closest = Mathf.Clamp(along, previous, wave.Travelled);
                Vector3 gap = offset - wave.Direction * closest;
                float reach = config.HalfWidth + body;

                if (gap.x * gap.x + gap.z * gap.z > reach * reach)
                    continue;

                EcsPackedEntity packed = world.PackEntity(candidate);

                if (AlreadyHit(ref wave, packed))
                    continue;

                RememberHit(ref wave, packed);

                ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
                damageEvent.Target = packed;
                damageEvent.Source = world.PackEntity(owner);
                damageEvent.SourcePosition = wave.Origin;
                damageEvent.Amount = wave.Damage;
                damageEvent.Kind = DamageKind.Perk;
                damageEvent.IsSlash = true;
                damageEvent.SourcePerk = config;

                if (pushes == false || _velocities.Value.Has(candidate) == false || _notPushables.Value.Has(candidate))
                    continue;

                Vector3 direction = PushFromPoint.Direction(Vector3.zero, wave.Direction + lateral.normalized * config.PushSideShare, wave.Direction);

                PushFromPoint.Apply(_pushes.Value, candidate, direction, config.PushSpeed, pushTicks);
                _pushes.Value.Get(candidate).ArcHeight = config.PushArcHeight;
            }
        }

        private void Burst(EcsWorld world, ref ForceWave wave, float delta)
        {
            ExecutionWavesConfig config = wave.Config;
            int ticks = Mathf.Max(1, Mathf.RoundToInt(config.BurstSeconds / delta));

            while (wave.NextBurst <= wave.Travelled + 1e-4f)
            {
                Vector3 at = wave.Origin + wave.Direction * wave.NextBurst;
                int entity = world.NewEntity();

                _bursts.Value.Add(entity).RemainingTicks = ticks;
                _positions.Value.Add(entity).Value = at;
                _notIndexed.Value.Add(entity);
                _views.Value.Add(entity).Value = _viewFactory.Value.Create(config.BurstPrefab, at);

                wave.NextBurst += config.BurstSpacing;
            }
        }

        private static bool AlreadyHit(ref ForceWave wave, in EcsPackedEntity candidate)
        {
            for (int index = 0; index < wave.HitCount; index++)
            {
                if (wave.Hits[index].EqualsTo(candidate))
                    return true;
            }

            return false;
        }

        private static void RememberHit(ref ForceWave wave, in EcsPackedEntity candidate)
        {
            if (wave.HitCount == wave.Hits.Length)
                Array.Resize(ref wave.Hits, wave.Hits.Length * 2);

            wave.Hits[wave.HitCount] = candidate;
            wave.HitCount++;
        }
    }
}
