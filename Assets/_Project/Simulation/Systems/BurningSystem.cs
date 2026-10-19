using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class BurningSystem : IEcsRunSystem
    {
        private const float FxHeight = 0.2f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<DamageEvent>> _events = default;
        private readonly EcsFilterInject<Inc<Burning>> _burning = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<Burning> _burningPool = default;
        private readonly EcsPoolInject<StatusFx> _statusFx = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out SearingFireballConfig config, out int level))
                    Ignite(world, hero, config, config.Level(level), delta);
            }

            foreach (int entity in _burning.Value)
                Burn(world, entity, delta);
        }

        private void Ignite(EcsWorld world, int hero, SearingFireballConfig config, SearingFireballLevel entry, float delta)
        {
            foreach (int entity in _events.Value)
            {
                ref DamageEvent damage = ref _damageEvents.Value.Get(entity);

                if (damage.Element != DamageElement.Fire || damage.Kind != DamageKind.Weapon || damage.Amount <= 0f)
                    continue;

                if (damage.Source.Unpack(world, out int source) == false || source != hero)
                    continue;

                if (damage.Target.Unpack(world, out int target) == false || _enemies.Value.Has(target) == false || _dead.Value.Has(target))
                    continue;

                bool fresh = _burningPool.Value.Has(target) == false;
                ref Burning burning = ref fresh ? ref _burningPool.Value.Add(target) : ref _burningPool.Value.Get(target);
                int tickTicks = Mathf.Max(1, Mathf.RoundToInt(config.TickSeconds / delta));

                burning.Config = config;
                burning.Source = damage.Source;
                burning.TickDamage = entry.DamagePerSecond * config.TickSeconds;
                burning.CritChanceBonus = entry.CritChanceBonus;
                burning.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(entry.Seconds / delta));

                if (fresh)
                    burning.TickCountdown = tickTicks;

                if (burning.HasFx == false)
                {
                    burning.HasFx = true;
                    SpawnFx(world, target, config.BurningFxPrefab);
                }
            }
        }

        private void Burn(EcsWorld world, int entity, float delta)
        {
            ref Burning burning = ref _burningPool.Value.Get(entity);

            if (_dead.Value.Has(entity) || _healths.Value.Has(entity) == false)
            {
                _burningPool.Value.Del(entity);

                return;
            }

            if (--burning.TickCountdown <= 0)
            {
                burning.TickCountdown = Mathf.Max(1, Mathf.RoundToInt(burning.Config.TickSeconds / delta));

                ref DamageEvent damage = ref _damageEvents.Value.Add(world.NewEntity());
                damage.Target = world.PackEntity(entity);
                damage.Source = burning.Source;
                damage.SourcePosition = _positions.Value.Has(entity) ? _positions.Value.Get(entity).Value : Vector3.zero;
                damage.Amount = burning.TickDamage;
                damage.Kind = DamageKind.Perk;
                damage.IsMagic = true;
                damage.Element = DamageElement.Fire;
                damage.SourcePerk = burning.Config;
            }

            if (--burning.RemainingTicks <= 0)
                _burningPool.Value.Del(entity);
        }

        private void SpawnFx(EcsWorld world, int target, GameObject prefab)
        {
            Vector3 at = _positions.Value.Get(target).Value;
            int fx = world.NewEntity();

            ref StatusFx status = ref _statusFx.Value.Add(fx);
            status.Owner = world.PackEntity(target);
            status.Kind = StatusKind.Burning;
            status.Height = FxHeight;

            _positions.Value.Add(fx).Value = at;
            _facings.Value.Add(fx).Value = Vector3.forward;
            _notIndexed.Value.Add(fx);

            IView view = _viewFactory.Value.Create(prefab, at);
            _views.Value.Add(fx).Value = view;

            if (view == null)
                return;

            view.SetLift(FxHeight);
            view.SetLift(FxHeight);
        }
    }
}
