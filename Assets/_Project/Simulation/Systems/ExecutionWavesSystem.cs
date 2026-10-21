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

                float previous = wave.Radius;

                wave.Radius = Mathf.Min(wave.Config.MaxRadius, previous + wave.Config.Speed * delta);

                if (wave.Owner.Unpack(world, out int owner))
                    Strike(world, ref wave, owner, previous, delta);

                Scale(entity, wave.Radius);

                if (wave.Radius >= wave.Config.MaxRadius)
                    Retire(world, entity);
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
                Vector3 direction = Quaternion.AngleAxis(first + step * index, Vector3.up) * forward;
                int entity = world.NewEntity();

                ref ForceWave wave = ref _wavePool.Value.Add(entity);
                wave.Owner = world.PackEntity(owner);
                wave.Config = config;
                wave.Origin = origin;
                wave.Direction = direction;
                wave.Radius = config.StartRadius;
                wave.Damage = damage;

                _positions.Value.Add(entity).Value = origin;
                _facings.Value.Add(entity).Value = direction;
                _notIndexed.Value.Add(entity);
                _views.Value.Add(entity).Value = _viewFactory.Value.Create(config.WavePrefab, origin);

                Scale(entity, wave.Radius);
            }
        }

        private void Strike(EcsWorld world, ref ForceWave wave, int owner, float previous, float delta)
        {
            ExecutionWavesConfig config = wave.Config;
            float halfThickness = config.Thickness * 0.5f;
            float maxBody = _motionBounds.Value.MaxEnemyBodyRadius;
            float halfAngle = config.WaveHalfAngleDegrees * Mathf.Deg2Rad;
            int pushTicks = Mathf.Max(1, Mathf.RoundToInt(config.PushSeconds / delta));
            bool pushes = config.PushSpeed > 0f && config.PushSeconds > 0f;

            _grid.Value.Query(wave.Origin, wave.Radius + halfThickness + maxBody, _motionBounds.Value.CandidateSlack(delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false && _breakables.Value.Has(candidate) == false)
                    continue;

                if (_dead.Value.Has(candidate) || _bodyRadii.Value.Has(candidate) == false)
                    continue;

                Vector3 offset = _positions.Value.Get(candidate).Value - wave.Origin;
                offset.y = 0f;

                float distance = offset.magnitude;
                float body = _bodyRadii.Value.Get(candidate).Value;

                if (distance > wave.Radius + halfThickness + body || distance < previous - halfThickness - body)
                    continue;

                if (distance > body && Vector3.Dot(offset / distance, wave.Direction) < Mathf.Cos(Mathf.Min(Mathf.PI, halfAngle + body / distance)))
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

                Vector3 direction = PushFromPoint.Direction(wave.Origin, wave.Origin + offset, wave.Direction);

                PushFromPoint.Apply(_pushes.Value, candidate, direction, config.PushSpeed, pushTicks);
                _pushes.Value.Get(candidate).ArcHeight = config.PushArcHeight;
            }
        }

        private void Scale(int entity, float radius)
        {
            if (_views.Value.Has(entity) && _views.Value.Get(entity).Value is IAreaView area)
                area.SetAreaScale(Mathf.Max(0.01f, radius));
        }

        private void Retire(EcsWorld world, int entity)
        {
            if (_views.Value.Has(entity))
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value != null)
                    _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }

            world.DelEntity(entity);
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
