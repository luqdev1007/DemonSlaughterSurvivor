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
    public sealed class PulseFireSegmentsSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<FireSegment, Position>> _segments = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink>> _weapons = default;

        private readonly EcsPoolInject<FireSegment> _segmentPool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private FlamingDashConfig _config;
        private List<int> _candidates;
        private HashSet<int> _hitThisPulse;
        private int _ticks;

        public void Init(IEcsSystems systems)
        {
            IReadOnlyList<PerkConfig> perks = _content.Value.All<PerkConfig>();

            for (int index = 0; index < perks.Count && _config == null; index++)
                _config = perks[index].Behaviour as FlamingDashConfig;

            int capacity = WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CapacityReserve;

            _candidates = new List<int>(capacity);
            _hitThisPulse = new HashSet<int>(capacity);
        }

        public void Run(IEcsSystems systems)
        {
            if (_config == null)
                return;

            int pulseTicks = Mathf.Max(1, Mathf.RoundToInt(_config.PulseSeconds / _clock.Value.Delta));

            _ticks++;

            if (_ticks % pulseTicks != 0)
                return;

            _hitThisPulse.Clear();

            EcsWorld world = _world.Value;
            float slack = _motionBounds.Value.CandidateSlack(_clock.Value.Delta);
            float queryRadius = _config.SegmentRadius + _motionBounds.Value.MaxEnemyBodyRadius;

            foreach (int entity in _segments.Value)
            {
                ref FireSegment segment = ref _segmentPool.Value.Get(entity);

                if (segment.Owner.Unpack(world, out int owner) == false)
                    continue;

                if (TryFindWeaponDamage(world, owner, out float weaponDamage) == false)
                    continue;

                Vector3 origin = _positions.Value.Get(entity).Value;

                _grid.Value.Query(origin, queryRadius, slack, _candidates);

                for (int index = 0; index < _candidates.Count; index++)
                {
                    int candidate = _candidates[index];

                    if (_enemies.Value.Has(candidate) == false || _dead.Value.Has(candidate) || _bodyRadii.Value.Has(candidate) == false)
                        continue;

                    Vector3 position = _positions.Value.Get(candidate).Value;
                    float x = position.x - origin.x;
                    float z = position.z - origin.z;
                    float reach = _config.SegmentRadius + _bodyRadii.Value.Get(candidate).Value;

                    if (x * x + z * z > reach * reach)
                        continue;

                    if (_hitThisPulse.Add(candidate) == false)
                        continue;

                    int eventEntity = world.NewEntity();

                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(eventEntity);
                    damageEvent.Target = world.PackEntity(candidate);
                    damageEvent.Source = world.PackEntity(owner);
                    damageEvent.SourcePosition = origin;
                    damageEvent.Amount = weaponDamage * segment.DamageScale;
                    damageEvent.Kind = DamageKind.Perk;
                }
            }
        }

        private bool TryFindWeaponDamage(EcsWorld world, int owner, out float damage)
        {
            foreach (int weapon in _weapons.Value)
            {
                if (_ownerLinks.Value.Get(weapon).Owner.Unpack(world, out int unpacked) == false || unpacked != owner)
                    continue;

                if (_weaponDamages.Value.Has(weapon) == false)
                    continue;

                damage = _weaponDamages.Value.Get(weapon).Value;

                return true;
            }

            damage = 0f;

            return false;
        }
    }
}
