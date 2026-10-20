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
    public sealed class AdvanceFlameWaveSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<FlameWave>> _waves = default;
        private readonly EcsFilterInject<Inc<FlameWavePillar>> _pillars = default;

        private readonly EcsPoolInject<FlameWave> _wavePool = default;
        private readonly EcsPoolInject<FlameWavePillar> _pillarPool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

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

            foreach (int entity in _waves.Value)
            {
                ref FlameWave wave = ref _wavePool.Value.Get(entity);

                float previous = wave.Radius;

                wave.Radius = Mathf.Min(wave.MaxRadius, previous + wave.Config.Speed * delta);

                if (wave.Owner.Unpack(world, out int owner))
                    Burn(world, ref wave, owner, previous, delta);

                if (wave.Radius >= wave.MaxRadius)
                    world.DelEntity(entity);
            }

            foreach (int entity in _pillars.Value)
            {
                ref FlameWavePillar pillar = ref _pillarPool.Value.Get(entity);

                if (pillar.Wave.Unpack(world, out int waveEntity) == false)
                {
                    Retire(world, entity);

                    continue;
                }

                ref FlameWave wave = ref _wavePool.Value.Get(waveEntity);
                Vector3 outward = Quaternion.Euler(0f, pillar.AngleDegrees, 0f) * wave.Direction;

                _positions.Value.Get(entity).Value = wave.Origin + outward * wave.Radius;
                _facings.Value.Get(entity).Value = outward;
            }
        }

        private void Burn(EcsWorld world, ref FlameWave wave, int owner, float previous, float delta)
        {
            FlameSwingConfig config = wave.Config;
            float halfThickness = config.Thickness * 0.5f;
            float maxBody = _motionBounds.Value.MaxEnemyBodyRadius;
            float halfAngle = config.HalfAngleDegrees * Mathf.Deg2Rad;

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
                damageEvent.IsMagic = true;
                damageEvent.Element = DamageElement.Fire;
                damageEvent.SourcePerk = config;
            }
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

        private static bool AlreadyHit(ref FlameWave wave, in EcsPackedEntity candidate)
        {
            for (int index = 0; index < wave.HitCount; index++)
            {
                if (wave.Hits[index].EqualsTo(candidate))
                    return true;
            }

            return false;
        }

        private static void RememberHit(ref FlameWave wave, in EcsPackedEntity candidate)
        {
            if (wave.HitCount == wave.Hits.Length)
                Array.Resize(ref wave.Hits, wave.Hits.Length * 2);

            wave.Hits[wave.HitCount] = candidate;
            wave.HitCount++;
        }
    }
}
