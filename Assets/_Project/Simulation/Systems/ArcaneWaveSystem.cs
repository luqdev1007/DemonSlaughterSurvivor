using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ArcaneWaveSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<ArcaneWaveCooldown>> _cooldownFilter = default;
        private readonly EcsFilterInject<Inc<ArcaneWave>> _waves = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, BodyRadius>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<ArcaneWaveCooldown> _cooldowns = default;
        private readonly EcsPoolInject<ArcaneWave> _wavePool = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<Concentration> _concentrations = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<NotPushable> _notPushables = default;
        private readonly EcsPoolInject<Pushed> _pushes = default;
        private readonly EcsPoolInject<Burning> _burning = default;
        private readonly EcsPoolInject<Frozen> _frozen = default;
        private readonly EcsPoolInject<Slowed> _slowed = default;
        private readonly EcsPoolInject<ElementMark> _marks = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _cooldownFilter.Value)
            {
                if (--_cooldowns.Value.Get(hero).RemainingTicks <= 0)
                    _cooldowns.Value.Del(hero);
            }

            foreach (int hero in _heroes.Value)
            {
                if (_cooldowns.Value.Has(hero))
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out ArcaneWaveConfig config, out int level) == false)
                    continue;

                ArcaneWaveLevel entry = config.Level(level);
                Vector3 origin = _positions.Value.Get(hero).Value;
                float radius = entry.Radius * ConcentrationBonus.Area(_concentrations.Value, hero);

                if (HasEnemyWithin(origin, radius) == false)
                    continue;

                Begin(world, hero, config, entry, origin, radius, delta);
            }

            foreach (int wave in _waves.Value)
                Advance(world, wave, delta);
        }

        private bool HasEnemyWithin(Vector3 origin, float radius)
        {
            foreach (int enemy in _enemies.Value)
            {
                Vector3 offset = _positions.Value.Get(enemy).Value - origin;
                float reach = radius + _bodyRadii.Value.Get(enemy).Value;

                if (offset.x * offset.x + offset.z * offset.z <= reach * reach)
                    return true;
            }

            return false;
        }

        private void Begin(EcsWorld world, int hero, ArcaneWaveConfig config, ArcaneWaveLevel entry, Vector3 origin, float radius, float delta)
        {
            int entity = world.NewEntity();

            ref ArcaneWave wave = ref _wavePool.Value.Add(entity);
            wave.Config = config;
            wave.Owner = world.PackEntity(hero);
            wave.Origin = origin;
            wave.MaxRadius = radius;
            wave.Damage = entry.Damage;
            wave.AffectedBonus = entry.AffectedBonus;

            _cooldowns.Value.Add(hero).RemainingTicks =
                Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));

            int fx = world.NewEntity();
            _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt((radius / config.Speed + 0.6f) / delta));
            _positions.Value.Add(fx).Value = origin;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = AreaScale.Apply(_viewFactory.Value.Create(config.WaveFxPrefab, origin), radius / config.FxDesignRadius);
        }

        private void Advance(EcsWorld world, int entity, float delta)
        {
            ref ArcaneWave wave = ref _wavePool.Value.Get(entity);
            ArcaneWaveConfig config = wave.Config;

            wave.Radius = Mathf.Min(wave.MaxRadius, wave.Radius + config.Speed * delta);

            bool hasOwner = wave.Owner.Unpack(world, out int owner);
            int pushTicks = Mathf.Max(1, Mathf.RoundToInt(config.PushSeconds / delta));

            foreach (int enemy in _enemies.Value)
            {
                Vector3 position = _positions.Value.Get(enemy).Value;
                float x = position.x - wave.Origin.x;
                float z = position.z - wave.Origin.z;
                float reach = wave.Radius + _bodyRadii.Value.Get(enemy).Value;

                if (x * x + z * z > reach * reach || WasHit(world, in wave, enemy))
                    continue;

                Remember(world, ref wave, enemy);

                bool affected = ElementalStatus.IsAffected(_burning.Value, _frozen.Value, _slowed.Value, _marks.Value, enemy);

                ref DamageEvent damage = ref _damageEvents.Value.Add(world.NewEntity());
                damage.Target = world.PackEntity(enemy);
                damage.Source = hasOwner ? world.PackEntity(owner) : default;
                damage.SourcePosition = wave.Origin;
                damage.Amount = wave.Damage * (affected ? 1f + wave.AffectedBonus : 1f);
                damage.Kind = DamageKind.Perk;
                damage.IsMagic = true;
                damage.Element = DamageElement.Arcane;
                damage.SourcePerk = config;

                if (config.PushSpeed > 0f && _velocities.Value.Has(enemy) && _notPushables.Value.Has(enemy) == false)
                    PushFromPoint.Apply(_pushes.Value, enemy, PushFromPoint.Direction(wave.Origin, position, Vector3.forward), config.PushSpeed, pushTicks);
            }

            if (wave.Radius >= wave.MaxRadius)
                world.DelEntity(entity);
        }

        private static bool WasHit(EcsWorld world, in ArcaneWave wave, int enemy)
        {
            for (int index = 0; index < wave.HitCount; index++)
            {
                if (wave.Hits[index].Unpack(world, out int hit) && hit == enemy)
                    return true;
            }

            return false;
        }

        private static void Remember(EcsWorld world, ref ArcaneWave wave, int enemy)
        {
            if (wave.HitCount == wave.Hits.Length)
                Array.Resize(ref wave.Hits, wave.Hits.Length * 2);

            wave.Hits[wave.HitCount++] = world.PackEntity(enemy);
        }
    }
}
