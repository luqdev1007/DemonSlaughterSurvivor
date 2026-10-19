using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class DeathRaySystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position, TorsoAim, InputState>, Exc<Dead, Channeling>> _heroes = default;
        private readonly EcsFilterInject<Inc<DeathRayCooldown>> _cooldownFilter = default;
        private readonly EcsFilterInject<Inc<DeathRay, Position>> _rays = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, BodyRadius>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<Breakable, Position, BodyRadius>, Exc<Dead>> _breakables = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<DeathRayCooldown> _cooldowns = default;
        private readonly EcsPoolInject<DeathRay> _rayPool = default;
        private readonly EcsPoolInject<Channeling> _channeling = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<InputState> _inputs = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<Concentration> _concentrations = default;

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

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out DeathRayConfig config, out int level) == false)
                    continue;

                if (IsHolding(hero) == false)
                    continue;

                ref TorsoAim aim = ref _aims.Value.Get(hero);
                Vector3 origin = _positions.Value.Get(hero).Value;
                Vector3 direction = aim.Manual ? aim.Desired : aim.Direction;

                Begin(world, hero, config, config.Level(level), origin, origin + direction, delta);
            }

            foreach (int ray in _rays.Value)
                Advance(world, ray, delta);
        }

        private void Begin(EcsWorld world, int hero, DeathRayConfig config, DeathRayLevel entry, Vector3 origin, Vector3 target, float delta)
        {
            Vector3 direction = target - origin;
            direction.y = 0f;
            direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : _facings.Value.Get(hero).Value;

            int chargeTicks = Mathf.Max(1, Mathf.RoundToInt(config.ChargeSeconds / delta));
            int ray = world.NewEntity();

            ref DeathRay data = ref _rayPool.Value.Add(ray);
            data.Config = config;
            data.Owner = world.PackEntity(hero);
            data.Direction = direction;
            data.RemainingTicks = chargeTicks;
            data.TotalTicks = chargeTicks;
            data.InitialDamage = entry.InitialDamage;
            data.TickDamage = entry.TickDamage;

            Vector3 muzzle = origin + direction * config.MuzzleForward;
            _positions.Value.Add(ray).Value = muzzle;
            _facings.Value.Add(ray).Value = direction;
            _notIndexed.Value.Add(ray);

            IView view = _viewFactory.Value.Create(config.BeamPrefab, muzzle);
            _views.Value.Add(ray).Value = view;

            if (view != null)
            {
                view.SetRotation(Quaternion.LookRotation(direction));
                view.SetLift(config.MuzzleHeight);
                view.SetLift(config.MuzzleHeight);
            }

            data.CooldownTicks = Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));

            _channeling.Value.Add(hero);

            if (_views.Value.Has(hero) && _views.Value.Get(hero).Value != null)
                _views.Value.Get(hero).Value.PlayAttack(config.CastTrigger, config.ChargeClipSeconds / (chargeTicks * delta));
        }

        private void Advance(EcsWorld world, int ray, float delta)
        {
            ref DeathRay data = ref _rayPool.Value.Get(ray);
            DeathRayConfig config = data.Config;

            if (data.Owner.Unpack(world, out int owner) == false || _dead.Value.Has(owner))
            {
                End(world, ray, in data, owner, false);

                return;
            }

            if (IsHolding(owner) == false)
            {
                End(world, ray, in data, owner, true);

                return;
            }

            if (_aims.Value.Has(owner))
                data.Direction = YawTurn.Towards(data.Direction, _aims.Value.Get(owner).Direction, config.TurnSpeed * delta);

            Vector3 origin = _positions.Value.Get(owner).Value;
            _positions.Value.Get(ray).Value = origin + data.Direction * config.MuzzleForward;
            _facings.Value.Get(ray).Value = data.Direction;

            data.RemainingTicks--;

            if (data.Firing == false)
            {
                Show(ray, 1f - (float)data.RemainingTicks / data.TotalTicks, false);

                if (data.RemainingTicks > 0)
                    return;

                data.Firing = true;
                data.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.MaxBeamSeconds / delta));
                data.TotalTicks = data.RemainingTicks;
                data.TickCountdown = Mathf.Max(1, Mathf.RoundToInt(config.TickSeconds / delta));

                Burn(world, owner, in data, data.InitialDamage);
                Show(ray, 1f, true);

                return;
            }

            if (--data.TickCountdown <= 0)
            {
                data.TickCountdown = Mathf.Max(1, Mathf.RoundToInt(config.TickSeconds / delta));
                Burn(world, owner, in data, data.TickDamage);
            }

            if (data.RemainingTicks > 0)
                return;

            End(world, ray, in data, owner, true);
        }

        private bool IsHolding(int hero)
        {
            if (_inputs.Value.Has(hero) == false)
                return false;

            ref InputState input = ref _inputs.Value.Get(hero);

            return input.StandHeld && input.MoveRequested == false;
        }

        private void Burn(EcsWorld world, int owner, in DeathRay data, float amount)
        {
            if (amount <= 0f)
                return;

            DeathRayConfig config = data.Config;
            Vector3 start = _positions.Value.Get(owner).Value;

            foreach (int enemy in _enemies.Value)
                TryBurn(world, owner, enemy, start, in data, config, amount);

            foreach (int breakable in _breakables.Value)
                TryBurn(world, owner, breakable, start, in data, config, amount);
        }

        private void TryBurn(EcsWorld world, int owner, int target, Vector3 start, in DeathRay data, DeathRayConfig config, float amount)
        {
            Vector3 offset = _positions.Value.Get(target).Value - start;
            offset.y = 0f;

            float along = Vector3.Dot(offset, data.Direction);
            float reach = _bodyRadii.Value.Get(target).Value;

            if (along < -reach || along > config.Length + config.MuzzleForward + reach)
                return;

            Vector3 side = offset - data.Direction * along;
            float limit = config.HalfWidth * ConcentrationBonus.Area(_concentrations.Value, owner) + reach;

            if (side.sqrMagnitude > limit * limit)
                return;

            ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
            damageEvent.Target = world.PackEntity(target);
            damageEvent.Source = world.PackEntity(owner);
            damageEvent.SourcePosition = start;
            damageEvent.Amount = amount;
            damageEvent.Kind = DamageKind.Perk;
            damageEvent.IsMagic = true;
            damageEvent.Element = DamageElement.Arcane;
            damageEvent.SourcePerk = config;
        }

        private void Show(int ray, float charge, bool firing)
        {
            if (_views.Value.Has(ray) && _views.Value.Get(ray).Value is IBeamView view)
                view.SetBeam(charge, firing);
        }

        private void End(EcsWorld world, int ray, in DeathRay data, int owner, bool ownerAlive)
        {
            if (ownerAlive)
            {
                _channeling.Value.Del(owner);
                _cooldowns.Value.Add(owner).RemainingTicks = data.CooldownTicks;

                if (_views.Value.Has(owner) && _views.Value.Get(owner).Value != null)
                    _views.Value.Get(owner).Value.PlayAttack(data.Config.EndTrigger, 1f);
            }
            else if (data.Owner.Unpack(world, out int deadOwner) && _channeling.Value.Has(deadOwner))
            {
                _channeling.Value.Del(deadOwner);
            }

            if (_views.Value.Has(ray))
            {
                ref View view = ref _views.Value.Get(ray);

                if (view.Value != null)
                    _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }

            world.DelEntity(ray);
        }
    }
}
