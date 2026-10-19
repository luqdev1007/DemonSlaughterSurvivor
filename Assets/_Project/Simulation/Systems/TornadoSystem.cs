using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class TornadoSystem : IEcsRunSystem
    {
        public const string StreamId = "stream.tornado";

        private const float TriggerRange = 12f;
        private const float ArenaMargin = 2f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<TornadoCooldown>> _cooldownFilter = default;
        private readonly EcsFilterInject<Inc<Tornado, Position, Velocity>> _tornadoes = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, BodyRadius>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<TornadoCooldown> _cooldowns = default;
        private readonly EcsPoolInject<TornadoRandom> _randoms = default;
        private readonly EcsPoolInject<Tornado> _tornadoPool = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<Concentration> _concentrations = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<Slowed> _slowed = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

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

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out TornadoConfig config, out int level) == false)
                    continue;

                Vector3 origin = _positions.Value.Get(hero).Value;

                if (HasEnemyWithin(origin, TriggerRange) == false)
                    continue;

                TornadoLevel entry = config.Level(level);

                if (_randoms.Value.Has(hero) == false)
                    _randoms.Value.Add(hero).State = SimulationRandom.StreamState(_context.Value.Seed, StreamId);

                ref uint state = ref _randoms.Value.Get(hero).State;
                float offset = SimulationRandom.NextUnit(ref state) * 360f;

                for (int index = 0; index < entry.Count; index++)
                {
                    Vector3 heading = Quaternion.AngleAxis(offset + 360f * index / entry.Count, Vector3.up) * Vector3.forward;
                    Spawn(world, hero, config, entry, origin, heading, SimulationRandom.NextUnit(ref state), delta);
                }

                _cooldowns.Value.Add(hero).RemainingTicks =
                    Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));
            }

            foreach (int tornado in _tornadoes.Value)
                Advance(world, tornado, delta);
        }

        private bool HasEnemyWithin(Vector3 origin, float radius)
        {
            foreach (int enemy in _enemies.Value)
            {
                Vector3 offset = _positions.Value.Get(enemy).Value - origin;

                if (offset.x * offset.x + offset.z * offset.z <= radius * radius)
                    return true;
            }

            return false;
        }

        private void Spawn(EcsWorld world, int hero, TornadoConfig config, TornadoLevel entry, Vector3 origin, Vector3 heading, float seed, float delta)
        {
            int entity = world.NewEntity();

            ref Tornado tornado = ref _tornadoPool.Value.Add(entity);
            tornado.Config = config;
            tornado.Owner = world.PackEntity(hero);
            tornado.Heading = heading;
            tornado.Damage = entry.Damage;
            tornado.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.LifeSeconds / delta));
            tornado.PulseCountdown = 1;
            tornado.TurnCountdown = Mathf.Max(1, Mathf.RoundToInt(config.TurnSeconds / delta));
            tornado.Random = SimulationRandom.SeedState(Mathf.RoundToInt(seed * int.MaxValue));

            Vector3 at = origin + heading * 0.5f;
            _positions.Value.Add(entity).Value = at;
            _velocities.Value.Add(entity).Value = heading * config.Speed;
            _facings.Value.Add(entity).Value = heading;
            _notIndexed.Value.Add(entity);

            float area = ConcentrationBonus.Area(_concentrations.Value, hero);
            _views.Value.Add(entity).Value = AreaScale.Apply(_viewFactory.Value.Create(config.TornadoPrefab, at), area);
        }

        private void Advance(EcsWorld world, int entity, float delta)
        {
            ref Tornado tornado = ref _tornadoPool.Value.Get(entity);
            TornadoConfig config = tornado.Config;

            if (--tornado.RemainingTicks <= 0)
            {
                Remove(world, entity);

                return;
            }

            Vector3 position = _positions.Value.Get(entity).Value;

            if (--tornado.TurnCountdown <= 0)
            {
                tornado.TurnCountdown = Mathf.Max(1, Mathf.RoundToInt(config.TurnSeconds / delta));
                tornado.Heading = Turn(ref tornado, config, position);
                _velocities.Value.Get(entity).Value = tornado.Heading * config.Speed;
                _facings.Value.Get(entity).Value = tornado.Heading;
            }

            if (--tornado.PulseCountdown > 0)
                return;

            tornado.PulseCountdown = Mathf.Max(1, Mathf.RoundToInt(config.PulseSeconds / delta));

            bool hasOwner = tornado.Owner.Unpack(world, out int owner);
            float radius = config.Radius * (hasOwner ? ConcentrationBonus.Area(_concentrations.Value, owner) : 1f);
            int slowTicks = Mathf.Max(1, Mathf.RoundToInt(config.SlowSeconds / delta));

            foreach (int enemy in _enemies.Value)
            {
                Vector3 offset = _positions.Value.Get(enemy).Value - position;
                float reach = radius + _bodyRadii.Value.Get(enemy).Value;

                if (offset.x * offset.x + offset.z * offset.z > reach * reach)
                    continue;

                ref DamageEvent damage = ref _damageEvents.Value.Add(world.NewEntity());
                damage.Target = world.PackEntity(enemy);
                damage.Source = hasOwner ? world.PackEntity(owner) : default;
                damage.SourcePosition = position;
                damage.Amount = tornado.Damage;
                damage.Kind = DamageKind.Perk;
                damage.IsMagic = true;
                damage.Element = DamageElement.Nature;
                damage.SourcePerk = config;

                if (_bosses.Value.Has(enemy))
                    continue;

                ref Slowed slowed = ref _slowed.Value.Has(enemy) ? ref _slowed.Value.Get(enemy) : ref _slowed.Value.Add(enemy);
                slowed.Factor = Mathf.Max(slowed.Factor, config.Slow);
                slowed.RemainingTicks = Mathf.Max(slowed.RemainingTicks, slowTicks);
            }
        }

        private Vector3 Turn(ref Tornado tornado, TornadoConfig config, Vector3 position)
        {
            float limit = _level.Value.ArenaRadius - ArenaMargin;
            Vector3 flat = new Vector3(position.x, 0f, position.z);

            if (flat.sqrMagnitude > limit * limit)
                return -flat.normalized;

            float side = SimulationRandom.NextUnit(ref tornado.Random) < 0.5f ? -1f : 1f;
            float degrees = Mathf.Lerp(config.MinTurnDegrees, config.MaxTurnDegrees, SimulationRandom.NextUnit(ref tornado.Random));

            return Quaternion.AngleAxis(side * degrees, Vector3.up) * tornado.Heading;
        }

        private void Remove(EcsWorld world, int entity)
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
    }
}
