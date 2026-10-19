using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartBlinkSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<BlinkCooldown>> _cooldownFilter = default;
        private readonly EcsFilterInject<Inc<BlinkEcho>> _echoFilter = default;
        private readonly EcsFilterInject<Inc<Player, BlinkStats, BlinkRequest, Position, Facing, BodyRadius>> _requests = default;
        private readonly EcsFilterInject<Inc<Breakable, Position, BodyRadius>, Exc<Dead>> _breakables = default;
        private readonly EcsFilterInject<Inc<NotPushable, Position, BodyRadius>, Exc<Dead>> _immovables = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<BlinkStats> _stats = default;
        private readonly EcsPoolInject<BlinkRequest> _requestPool = default;
        private readonly EcsPoolInject<BlinkCooldown> _cooldowns = default;
        private readonly EcsPoolInject<BlinkEcho> _echoes = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<InputState> _inputs = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<InputConfig> _input = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _cooldownFilter.Value)
            {
                ref BlinkCooldown cooldown = ref _cooldowns.Value.Get(entity);
                cooldown.Remaining -= delta;

                if (cooldown.Remaining <= 0f)
                    _cooldowns.Value.Del(entity);
            }

            foreach (int entity in _echoFilter.Value)
            {
                ref BlinkEcho echo = ref _echoes.Value.Get(entity);
                echo.Remaining -= delta;

                if (echo.Remaining <= 0f)
                    _echoes.Value.Del(entity);
            }

            foreach (int hero in _requests.Value)
            {
                ref BlinkRequest request = ref _requestPool.Value.Get(hero);

                if (_dead.Value.Has(hero) || request.Age >= _input.Value.BufferSeconds)
                {
                    _requestPool.Value.Del(hero);

                    continue;
                }

                bool echo = _cooldowns.Value.Has(hero) && _echoes.Value.Has(hero);

                if (_cooldowns.Value.Has(hero) && echo == false)
                {
                    request.Age += delta;

                    continue;
                }

                _requestPool.Value.Del(hero);

                if (echo)
                    _echoes.Value.Del(hero);

                Blink(hero, _stats.Value.Get(hero).Config, echo);
            }
        }

        private void Blink(int hero, BlinkConfig config, bool echo)
        {
            ref Position position = ref _positions.Value.Get(hero);
            Vector3 origin = position.Value;
            float bodyRadius = _bodyRadii.Value.Get(hero).Value;

            bool hasAim = _inputs.Value.Has(hero) && _inputs.Value.Get(hero).HasAim;
            Vector3 aimPoint = hasAim ? _inputs.Value.Get(hero).AimPoint : origin;
            Vector3 target = BlinkPlacement.Target(origin, aimPoint, hasAim, _facings.Value.Get(hero).Value, config.MaxDistance);
            Vector3 destination = Resolve(origin, target, bodyRadius, config.ProbeStep);

            position.Value = destination;

            if (_previousPositions.Value.Has(hero))
                _previousPositions.Value.Get(hero).Value = destination;

            if (echo == false)
            {
                _cooldowns.Value.Add(hero).Remaining = config.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero);
                OpenEcho(hero);
            }

            if (_views.Value.Has(hero) && _views.Value.Get(hero).Value is ITeleportView view)
                view.Teleport(destination);

            Fx(config.DepartFxPrefab, origin, config.FxSeconds);
            Fx(config.ArriveFxPrefab, destination, config.FxSeconds);

            Repulse(hero, destination);
        }

        private void OpenEcho(int hero)
        {
            if (TakenBehaviourLookup.TryFind(_world.Value, _taken.Value, _takenPool.Value, _content.Value, hero, out BlinkEchoConfig config, out int level) == false)
                return;

            _echoes.Value.Add(hero).Remaining = config.Level(level).WindowSeconds;
        }

        private void Repulse(int hero, Vector3 at)
        {
            EcsWorld world = _world.Value;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out BlinkRepulseConfig config, out int level) == false)
                return;

            BlinkRepulseLevel entry = config.Level(level);

            ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
            strike.Owner = world.PackEntity(hero);
            strike.Origin = at;
            strike.Forward = Vector3.forward;
            strike.Reach = entry.Radius;
            strike.HalfAngleDegrees = 180f;
            strike.Kind = DamageKind.Perk;
            strike.Element = DamageElement.Arcane;
            strike.SourcePerk = config;
            strike.PushSpeed = entry.PushSpeed;
            strike.PushSeconds = config.PushSeconds;
            strike.PushArcHeight = config.PushArcHeight;

            Fx(config.FxPrefab, at, config.FxSeconds);
        }

        private Vector3 Resolve(Vector3 origin, Vector3 target, float bodyRadius, float step)
        {
            float arenaRadius = _level.Value.ArenaRadius;
            Vector3 offset = target - origin;
            float distance = offset.magnitude;

            if (distance <= Mathf.Epsilon)
                return origin;

            Vector3 direction = offset / distance;

            for (float travelled = distance; travelled > 0f; travelled -= step)
            {
                Vector3 candidate = BlinkPlacement.ClampToArena(origin + direction * travelled, arenaRadius, bodyRadius);

                if (IsBlocked(candidate, bodyRadius) == false)
                    return candidate;
            }

            return origin;
        }

        private bool IsBlocked(Vector3 point, float bodyRadius)
        {
            foreach (int solid in _breakables.Value)
            {
                if (BlinkPlacement.Overlaps(point, bodyRadius, _positions.Value.Get(solid).Value, _bodyRadii.Value.Get(solid).Value))
                    return true;
            }

            foreach (int solid in _immovables.Value)
            {
                if (BlinkPlacement.Overlaps(point, bodyRadius, _positions.Value.Get(solid).Value, _bodyRadii.Value.Get(solid).Value))
                    return true;
            }

            return false;
        }

        private void Fx(GameObject prefab, Vector3 at, float seconds)
        {
            if (prefab == null)
                return;

            EcsWorld world = _world.Value;
            int fx = world.NewEntity();

            _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(seconds / _clock.Value.Delta));
            _positions.Value.Add(fx).Value = at;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = _viewFactory.Value.Create(prefab, at);
        }
    }
}
