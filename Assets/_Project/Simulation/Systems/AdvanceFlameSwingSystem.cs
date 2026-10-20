using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceFlameSwingSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<FlameSwing>> _swings = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink>> _weapons = default;

        private readonly EcsPoolInject<FlameSwing> _swingPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<FlameWave> _waves = default;
        private readonly EcsPoolInject<FlameWavePillar> _pillars = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int entity in _swings.Value)
            {
                ref FlameSwing swing = ref _swingPool.Value.Get(entity);

                if (swing.Owner.Unpack(world, out int owner) == false)
                {
                    world.DelEntity(entity);

                    continue;
                }

                FlameSwingConfig config = swing.Config;

                if (swing.ElapsedTicks == ReleaseTick(config, delta))
                    Release(world, owner, config, swing.Level);

                swing.ElapsedTicks++;

                if (swing.ElapsedTicks >= TotalTicks(config, delta))
                    world.DelEntity(entity);
            }
        }

        public static int TotalTicks(FlameSwingConfig config, float delta)
        {
            return Mathf.Max(1, Mathf.RoundToInt(config.ClipSeconds / config.PlaybackSpeed / delta));
        }

        public static int ReleaseTick(FlameSwingConfig config, float delta)
        {
            return Mathf.RoundToInt(config.ReleaseClipSeconds / config.PlaybackSpeed / delta);
        }

        private void Release(EcsWorld world, int owner, FlameSwingConfig config, int level)
        {
            if (TryFindWeaponDamage(world, owner, out float weaponDamage) == false)
                return;

            Vector3 direction = _facings.Value.Get(owner).Value;
            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            direction.Normalize();

            Vector3 origin = _positions.Value.Get(owner).Value;
            origin.y = 0f;

            FlameSwingLevel entry = config.Level(level);
            int wave = world.NewEntity();

            ref FlameWave data = ref _waves.Value.Add(wave);
            data.Owner = world.PackEntity(owner);
            data.Config = config;
            data.Origin = origin;
            data.Direction = direction;
            data.Radius = config.StartRadius;
            data.MaxRadius = entry.Distance;
            data.Damage = weaponDamage * entry.DamageShare;

            EcsPackedEntity packedWave = world.PackEntity(wave);
            int count = config.PillarCount;

            for (int index = 0; index < count; index++)
            {
                float angle = count == 1 ? 0f : Mathf.Lerp(-config.HalfAngleDegrees, config.HalfAngleDegrees, index / (float)(count - 1));
                Vector3 outward = Quaternion.Euler(0f, angle, 0f) * direction;
                Vector3 point = origin + outward * config.StartRadius;

                int pillar = world.NewEntity();

                ref FlameWavePillar pillarData = ref _pillars.Value.Add(pillar);
                pillarData.Wave = packedWave;
                pillarData.AngleDegrees = angle;

                _positions.Value.Add(pillar).Value = point;
                _facings.Value.Add(pillar).Value = outward;
                _notIndexed.Value.Add(pillar);
                _views.Value.Add(pillar).Value = _viewFactory.Value.Create(config.PillarPrefab, point);
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
