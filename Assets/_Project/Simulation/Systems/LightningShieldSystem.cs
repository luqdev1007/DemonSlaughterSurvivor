using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class LightningShieldSystem : IEcsRunSystem
    {
        private const int MaxPicks = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position>> _heroes = default;
        private readonly EcsFilterInject<Inc<LightningAuraFx, Position>> _auras = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, BodyRadius>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<LightningShield> _shields = default;
        private readonly EcsPoolInject<LightningAuraFx> _auraPool = default;
        private readonly EcsPoolInject<Concentration> _concentrations = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        private readonly int[] _picks = new int[MaxPicks];
        private readonly float[] _pickDistances = new float[MaxPicks];

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                if (_dead.Value.Has(hero))
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out LightningShieldConfig config, out int level) == false)
                    continue;

                if (_shields.Value.Has(hero) == false)
                    _shields.Value.Add(hero);

                ref LightningShield shield = ref _shields.Value.Get(hero);

                if (shield.HasAura == false)
                {
                    shield.HasAura = true;
                    shield.Aura = SpawnAura(world, hero, config);
                }

                if (shield.Countdown > 0)
                {
                    shield.Countdown--;

                    continue;
                }

                LightningShieldLevel entry = config.Level(level);
                Vector3 origin = _positions.Value.Get(hero).Value;
                float radius = config.Radius * ConcentrationBonus.Area(_concentrations.Value, hero);
                int count = Pick(origin, radius, Mathf.Min(entry.MaxTargets, MaxPicks));

                if (count == 0)
                    continue;

                for (int index = 0; index < count; index++)
                    Strike(world, hero, config, entry, origin, _picks[index]);

                shield.Countdown = Mathf.Max(1, Mathf.RoundToInt(entry.IntervalSeconds / delta));
            }

            foreach (int aura in _auras.Value)
                FollowOrRelease(world, aura);
        }

        private int Pick(Vector3 origin, float radius, int limit)
        {
            int count = 0;

            foreach (int enemy in _enemies.Value)
            {
                Vector3 offset = _positions.Value.Get(enemy).Value - origin;
                float reach = radius + _bodyRadii.Value.Get(enemy).Value;
                float distance = offset.x * offset.x + offset.z * offset.z;

                if (distance > reach * reach)
                    continue;

                int slot = count < limit ? count++ : -1;

                if (slot < 0)
                {
                    int worst = 0;

                    for (int index = 1; index < count; index++)
                    {
                        if (_pickDistances[index] > _pickDistances[worst])
                            worst = index;
                    }

                    if (_pickDistances[worst] <= distance)
                        continue;

                    slot = worst;
                }

                _picks[slot] = enemy;
                _pickDistances[slot] = distance;
            }

            return count;
        }

        private void Strike(EcsWorld world, int hero, LightningShieldConfig config, LightningShieldLevel entry, Vector3 origin, int enemy)
        {
            Vector3 to = _positions.Value.Get(enemy).Value;

            ref DamageEvent damage = ref _damageEvents.Value.Add(world.NewEntity());
            damage.Target = world.PackEntity(enemy);
            damage.Source = world.PackEntity(hero);
            damage.SourcePosition = origin;
            damage.Amount = entry.Damage;
            damage.Kind = DamageKind.Perk;
            damage.IsMagic = true;
            damage.Element = DamageElement.Nature;
            damage.SourcePerk = config;

            Vector3 lift = Vector3.up * config.BoltHeight;
            IView bolt = Fx(world, config.BoltPrefab, origin, config.BoltSeconds);

            if (bolt is ILightningView lightning)
                lightning.Strike(origin + lift, to + lift);

            Fx(world, config.ImpactFxPrefab, to, config.ImpactSeconds);
        }

        private IView Fx(EcsWorld world, GameObject prefab, Vector3 at, float seconds)
        {
            int fx = world.NewEntity();
            _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(seconds / _clock.Value.Delta));
            _positions.Value.Add(fx).Value = at;
            _facings.Value.Add(fx).Value = Vector3.forward;
            _notIndexed.Value.Add(fx);

            IView view = _viewFactory.Value.Create(prefab, at);
            _views.Value.Add(fx).Value = view;

            return view;
        }

        private EcsPackedEntity SpawnAura(EcsWorld world, int hero, LightningShieldConfig config)
        {
            Vector3 at = _positions.Value.Get(hero).Value;
            int aura = world.NewEntity();

            _auraPool.Value.Add(aura).Owner = world.PackEntity(hero);
            _positions.Value.Add(aura).Value = at;
            _facings.Value.Add(aura).Value = Vector3.forward;
            _notIndexed.Value.Add(aura);
            _views.Value.Add(aura).Value = _viewFactory.Value.Create(config.AuraFxPrefab, at);

            return world.PackEntity(aura);
        }

        private void FollowOrRelease(EcsWorld world, int aura)
        {
            if (_auraPool.Value.Get(aura).Owner.Unpack(world, out int owner) && _dead.Value.Has(owner) == false)
            {
                _positions.Value.Get(aura).Value = _positions.Value.Get(owner).Value;

                return;
            }

            if (_views.Value.Has(aura))
            {
                ref View view = ref _views.Value.Get(aura);

                if (view.Value != null)
                    _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }

            world.DelEntity(aura);
        }
    }
}
