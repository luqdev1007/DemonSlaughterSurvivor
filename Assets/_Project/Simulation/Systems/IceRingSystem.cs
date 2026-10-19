using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class IceRingSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, BodyRadius>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<IceRingCooldown> _cooldowns = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<Frozen> _frozen = default;
        private readonly EcsPoolInject<FrozenFx> _frozenFx = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Concentration> _concentrations = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                if (_cooldowns.Value.Has(hero))
                {
                    if (--_cooldowns.Value.Get(hero).RemainingTicks <= 0)
                        _cooldowns.Value.Del(hero);

                    continue;
                }

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out IceRingConfig config, out int level) == false)
                    continue;

                Vector3 origin = _positions.Value.Get(hero).Value;
                float radius = config.Radius * ConcentrationBonus.Area(_concentrations.Value, hero);

                if (HasEnemyWithin(origin, radius) == false)
                    continue;

                IceRingLevel entry = config.Level(level);

                Cast(world, hero, config, entry, origin, radius, delta);

                _cooldowns.Value.Add(hero).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));
            }
        }

        private bool HasEnemyWithin(Vector3 origin, float radius)
        {
            foreach (int enemy in _enemies.Value)
            {
                if (IsWithin(origin, enemy, radius))
                    return true;
            }

            return false;
        }

        private bool IsWithin(Vector3 origin, int enemy, float radius)
        {
            Vector3 offset = _positions.Value.Get(enemy).Value - origin;
            float reach = radius + _bodyRadii.Value.Get(enemy).Value;

            return offset.x * offset.x + offset.z * offset.z <= reach * reach;
        }

        private void Cast(EcsWorld world, int hero, IceRingConfig config, IceRingLevel entry, Vector3 origin, float radius, float delta)
        {
            int freezeTicks = Mathf.Max(1, Mathf.RoundToInt(entry.FreezeSeconds / delta));

            foreach (int enemy in _enemies.Value)
            {
                if (IsWithin(origin, enemy, radius) == false)
                    continue;

                if (entry.Damage > 0f)
                {
                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
                    damageEvent.Target = world.PackEntity(enemy);
                    damageEvent.Source = world.PackEntity(hero);
                    damageEvent.SourcePosition = origin;
                    damageEvent.Amount = entry.Damage;
                    damageEvent.Kind = DamageKind.Perk;
                    damageEvent.IsMagic = true;
                    damageEvent.SourcePerk = config;
                }

                if (_bosses.Value.Has(enemy) == false)
                    Freeze(world, enemy, config, freezeTicks);
            }

            Fx(world, config.RingFxPrefab, origin, Mathf.Max(1, Mathf.RoundToInt(config.RingFxSeconds / delta)), radius / config.Radius);
        }

        private void Freeze(EcsWorld world, int enemy, IceRingConfig config, int ticks)
        {
            bool fresh = _frozen.Value.Has(enemy) == false;
            ref Frozen frozen = ref fresh ? ref _frozen.Value.Add(enemy) : ref _frozen.Value.Get(enemy);
            frozen.RemainingTicks = Mathf.Max(frozen.RemainingTicks, ticks);

            if (fresh && _views.Value.Has(enemy) && _views.Value.Get(enemy).Value is IFreezeView view)
                view.SetFrozen(true);

            if (frozen.HasFx)
                return;

            frozen.HasFx = true;

            Vector3 at = _positions.Value.Get(enemy).Value;
            int fx = world.NewEntity();

            _frozenFx.Value.Add(fx).Owner = world.PackEntity(enemy);
            _positions.Value.Add(fx).Value = at;
            _facings.Value.Add(fx).Value = Vector3.forward;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = _viewFactory.Value.Create(config.FrozenFxPrefab, at);
        }

        private void Fx(EcsWorld world, GameObject prefab, Vector3 at, int ticks, float scale)
        {
            int fx = world.NewEntity();

            _impacts.Value.Add(fx).RemainingTicks = ticks;
            _positions.Value.Add(fx).Value = at;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = AreaScale.Apply(_viewFactory.Value.Create(prefab, at), scale);
        }
    }
}
