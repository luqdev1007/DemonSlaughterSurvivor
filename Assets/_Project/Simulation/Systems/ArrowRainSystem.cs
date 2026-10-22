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
    public sealed class ArrowRainSystem : IEcsInitSystem, IEcsRunSystem
    {
        public const string StreamId = "stream.arrow-rain";
        public const string BombardmentStreamId = "stream.bombardment";

        private const int CandidateCapacityReserve = 16;
        private const float FullCircleDegrees = 180f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Enemy, Position>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<Bow, OwnerLink, WeaponDamage>> _bows = default;
        private readonly EcsFilterInject<Inc<ArrowRainImpact>> _impacts = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<ArrowRainCooldown> _cooldowns = default;
        private readonly EcsPoolInject<ArrowRainRandom> _randoms = default;
        private readonly EcsPoolInject<ArrowRainImpact> _impactPool = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _damages = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;
        private readonly EcsPoolInject<BombardmentRandom> _bombardmentRandoms = default;
        private readonly EcsPoolInject<LeapImpactFx> _impactFx = default;
        private readonly EcsPoolInject<Duality> _dualities = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private List<int> _candidates;
        private List<int> _lastDelays;

        public void Init(IEcsSystems systems)
        {
            int capacity = WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CandidateCapacityReserve;

            _candidates = new List<int>(capacity);
            _lastDelays = new List<int>(capacity);
        }

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

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out ArrowRainConfig config, out int level) == false)
                    continue;

                if (TryFindBowDamage(world, hero, out float bowDamage) == false || Gather(hero, config.Range) == false)
                    continue;

                ArrowRainLevel entry = config.Level(level);
                Volley(world, hero, config, entry, bowDamage, delta);

                _cooldowns.Value.Add(hero).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));
            }

            foreach (int impact in _impacts.Value)
                Advance(world, impact, delta);
        }

        private bool Gather(int hero, float range)
        {
            Vector3 origin = _positions.Value.Get(hero).Value;
            float rangeSquared = range * range;

            _candidates.Clear();

            foreach (int enemy in _enemies.Value)
            {
                Vector3 offset = _positions.Value.Get(enemy).Value - origin;
                offset.y = 0f;

                if (offset.sqrMagnitude <= rangeSquared)
                    _candidates.Add(enemy);
            }

            return _candidates.Count > 0;
        }

        private void Volley(EcsWorld world, int hero, ArrowRainConfig config, ArrowRainLevel entry, float bowDamage, float delta)
        {
            if (_randoms.Value.Has(hero) == false)
                _randoms.Value.Add(hero).State = SimulationRandom.StreamState(_context.Value.Seed, StreamId);

            ref uint state = ref _randoms.Value.Get(hero).State;

            int staggerTicks = Mathf.RoundToInt(config.StaggerSeconds / delta);
            int repeatTicks = Mathf.RoundToInt(config.RepeatDelaySeconds / delta);

            _lastDelays.Clear();

            for (int index = 0; index < _candidates.Count; index++)
                _lastDelays.Add(-1);

            for (int strike = 0; strike < entry.Strikes; strike++)
            {
                int pick = Mathf.Min(_candidates.Count - 1, (int)(SimulationRandom.NextUnit(ref state) * _candidates.Count));
                int delay = strike * staggerTicks;

                if (_lastDelays[pick] >= 0)
                    delay = Mathf.Max(delay, _lastDelays[pick] + repeatTicks);

                _lastDelays[pick] = delay;

                ref ArrowRainImpact impact = ref _impactPool.Value.Add(world.NewEntity());
                impact.Config = config;
                impact.Owner = world.PackEntity(hero);
                impact.Target = world.PackEntity(_candidates[pick]);
                impact.DelayTicks = delay;
                impact.FallTicks = Mathf.Max(1, Mathf.RoundToInt(config.FallSeconds / delta));
                impact.LingerTicks = Mathf.Max(1, Mathf.RoundToInt(config.LingerSeconds / delta));
                impact.Damage = bowDamage * entry.DamageScale;
                impact.BowDamage = bowDamage;
            }
        }

        private void Advance(EcsWorld world, int entity, float delta)
        {
            ref ArrowRainImpact impact = ref _impactPool.Value.Get(entity);

            if (impact.DelayTicks > 0)
            {
                impact.DelayTicks--;

                return;
            }

            bool hasTarget = impact.Target.Unpack(world, out int target) && _dead.Value.Has(target) == false;

            if (_positions.Value.Has(entity) == false)
            {
                if (hasTarget == false)
                {
                    world.DelEntity(entity);

                    return;
                }

                Vector3 at = _positions.Value.Get(target).Value;

                _positions.Value.Add(entity).Value = at;
                _facings.Value.Add(entity).Value = Vector3.forward;
                _notIndexed.Value.Add(entity);
                _views.Value.Add(entity).Value = _viewFactory.Value.Create(impact.Config.FxPrefab, at);

                return;
            }

            if (impact.FallTicks > 0)
            {
                if (hasTarget)
                    _positions.Value.Get(entity).Value = _positions.Value.Get(target).Value;

                if (--impact.FallTicks == 0)
                {
                    Strike(world, in impact, _positions.Value.Get(entity).Value);

                    if (hasTarget)
                        Bombard(world, in impact, _positions.Value.Get(entity).Value, delta);
                }

                return;
            }

            if (--impact.LingerTicks > 0)
                return;

            if (_views.Value.Get(entity).Value != null)
                _viewFactory.Value.Release(_views.Value.Get(entity).Value);

            world.DelEntity(entity);
        }

        private void Strike(EcsWorld world, in ArrowRainImpact impact, Vector3 at)
        {
            ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
            strike.Owner = impact.Owner;
            strike.Origin = at;
            strike.Forward = Vector3.forward;
            strike.Reach = impact.Config.Radius;
            strike.HalfAngleDegrees = FullCircleDegrees;
            strike.Damage = impact.Damage;
            strike.Kind = DamageKind.Perk;
            strike.SourcePerk = impact.Config;
        }

        private void Bombard(EcsWorld world, in ArrowRainImpact impact, Vector3 at, float delta)
        {
            if (impact.Owner.Unpack(world, out int owner) == false)
                return;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out BombardmentConfig bombardment, out int bombardmentLevel) == false)
                return;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out ExplosiveArrowConfig explosive, out int explosiveLevel) == false)
                return;

            if (_bombardmentRandoms.Value.Has(owner) == false)
                _bombardmentRandoms.Value.Add(owner).State = SimulationRandom.StreamState(_context.Value.Seed, BombardmentStreamId);

            if (SimulationRandom.NextUnit(ref _bombardmentRandoms.Value.Get(owner).State) >= bombardment.Level(bombardmentLevel).Chance)
                return;

            ArrowExplosion.Detonate(world, impact.Owner, explosive, explosive.Level(explosiveLevel), DualityForm.IsDemonic(_dualities.Value, owner), impact.BowDamage, at,
                _strikes.Value, _impactFx.Value, _positions.Value, _notIndexed.Value, _views.Value, _viewFactory.Value, delta);
        }

        private bool TryFindBowDamage(EcsWorld world, int hero, out float damage)
        {
            foreach (int bow in _bows.Value)
            {
                if (_ownerLinks.Value.Get(bow).Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                damage = _damages.Value.Get(bow).Value;

                return true;
            }

            damage = 0f;

            return false;
        }
    }
}
