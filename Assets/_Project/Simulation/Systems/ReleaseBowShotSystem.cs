using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ReleaseBowShotSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Bow, BowDraw, OwnerLink, WeaponDamage>> _draws = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<Bow> _bows = default;
        private readonly EcsPoolInject<BowDraw> _drawPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _damages = default;
        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Projectile> _projectiles = default;
        private readonly EcsPoolInject<Arrow> _arrows = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Backleaping> _backleaps = default;
        private readonly EcsPoolInject<Height> _heights = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<PowerShotCooldown> _powerCooldowns = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int bow in _draws.Value)
            {
                BowConfig config = _bows.Value.Get(bow).Config;
                bool hasOwner = _ownerLinks.Value.Get(bow).Owner.Unpack(world, out int owner);

                if (hasOwner == false || BowShot.IsBlocked(owner, _dead.Value, _backleaps.Value, ShootsInAir(world, owner)) || _aims.Value.Has(owner) == false)
                {
                    _drawPool.Value.Del(bow);

                    if (hasOwner)
                        Cue(owner, config.CancelTrigger);

                    continue;
                }

                ref BowDraw draw = ref _drawPool.Value.Get(bow);
                draw.RemainingTicks--;

                if (draw.RemainingTicks > 0)
                    continue;

                bool empowered = draw.Empowered;
                _drawPool.Value.Del(bow);

                Vector3 origin = _positions.Value.Get(owner).Value;
                Vector3 direction = ResolveDirection(world, in _aims.Value.Get(owner), origin);

                Volley(world, owner, config, origin, direction, _damages.Value.Get(bow).Value, empowered, delta);
                Cue(owner, config.ReleaseTrigger);
            }
        }

        private Vector3 ResolveDirection(EcsWorld world, in TorsoAim aim, Vector3 origin)
        {
            if (aim.Manual)
                return aim.Desired;

            if (aim.HasTarget && RangedTarget.TryFindLiving(world, aim.Target, _positions.Value, _dead.Value, out Vector3 target))
            {
                Vector3 offset = target - origin;
                offset.y = 0f;

                if (offset.sqrMagnitude > Mathf.Epsilon)
                    return offset.normalized;
            }

            return aim.Direction;
        }

        private void Volley(EcsWorld world, int owner, BowConfig config, Vector3 origin, Vector3 direction, float damage, bool empowered, float delta)
        {
            Shot normal = new Shot { Damage = damage, Radius = config.ArrowRadius, Prefab = config.ArrowPrefab };
            Shot strong = normal;

            if (empowered && TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out PowerShotConfig power, out int powerLevel))
            {
                PowerShotLevel level = power.Level(powerLevel);

                strong.Damage *= level.DamageMultiplier;
                strong.Radius *= power.WidthMultiplier;
                strong.Prefab = power.ArrowPrefab;
                strong.Piercing = true;
                strong.CritChanceBonus = level.CritChanceBonus;

                ref PowerShotCooldown cooldown = ref _powerCooldowns.Value.Has(owner) ? ref _powerCooldowns.Value.Get(owner) : ref _powerCooldowns.Value.Add(owner);
                cooldown.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(level.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, owner) / delta));
                cooldown.Progress = 0f;
            }

            int count = 1;
            float spread = 0f;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out MultishotConfig multishot, out int multishotLevel))
            {
                count += multishot.Level(multishotLevel).ExtraArrows;
                spread = multishot.SpreadDegrees;
            }

            bool wholeVolley = TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out PiercingVolleyConfig _, out _);
            int center = (count - 1) / 2;

            for (int index = 0; index < count; index++)
            {
                float angle = (index - (count - 1) * 0.5f) * spread;
                bool strongArrow = wholeVolley || index == center;

                Vector3 arrowDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;

                if (strongArrow)
                    Shoot(world, owner, config, origin, arrowDirection, in strong, delta);
                else
                    Shoot(world, owner, config, origin, arrowDirection, in normal, delta);
            }
        }

        private void Shoot(EcsWorld world, int owner, BowConfig config, Vector3 origin, Vector3 direction, in Shot shot, float delta)
        {
            Vector3 spawn = origin + direction * config.MuzzleDistance;

            int arrow = world.NewEntity();

            ref Projectile projectile = ref _projectiles.Value.Add(arrow);
            projectile.Source = world.PackEntity(owner);
            projectile.LastPosition = spawn;
            projectile.Damage = shot.Damage;
            projectile.Radius = shot.Radius;
            projectile.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.ArrowRange / (config.ArrowSpeed * delta)));

            ref Arrow arrowData = ref _arrows.Value.Add(arrow);
            arrowData.Piercing = shot.Piercing;
            arrowData.CritChanceBonus = shot.CritChanceBonus;
            _positions.Value.Add(arrow).Value = spawn;
            _velocities.Value.Add(arrow).Value = direction * config.ArrowSpeed;
            _facings.Value.Add(arrow).Value = direction;
            _notIndexed.Value.Add(arrow);
            IView view = _viewFactory.Value.Create(shot.Prefab, spawn);
            _views.Value.Add(arrow).Value = view;

            float lift = _heights.Value.Has(owner) ? _heights.Value.Get(owner).Value : 0f;

            if (lift > 0f && view != null)
            {
                view.SetLift(lift);
                view.SetLift(lift);
            }
        }

        private struct Shot
        {
            public float Damage;
            public float Radius;
            public GameObject Prefab;
            public bool Piercing;
            public float CritChanceBonus;
        }

        private bool ShootsInAir(EcsWorld world, int owner)
        {
            return _backleaps.Value.Has(owner) && TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out DemonicAgilityConfig _, out _);
        }

        private void Cue(int owner, string trigger)
        {
            if (_views.Value.Has(owner) == false || _views.Value.Get(owner).Value == null)
                return;

            _views.Value.Get(owner).Value.PlayAttack(trigger, 1f);
        }
    }
}
