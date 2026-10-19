using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class MeteorSystem : IEcsRunSystem
    {
        private const float FullCircleDegrees = 180f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Enemy, Position>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;
        private readonly EcsFilterInject<Inc<MeteorCast>> _casts = default;
        private readonly EcsFilterInject<Inc<Meteor, Position>> _meteors = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<MeteorCooldown> _cooldowns = default;
        private readonly EcsPoolInject<MeteorCast> _castPool = default;
        private readonly EcsPoolInject<Meteor> _meteorPool = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
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

            foreach (int hero in _heroes.Value)
            {
                if (_cooldowns.Value.Has(hero))
                {
                    if (--_cooldowns.Value.Get(hero).RemainingTicks <= 0)
                        _cooldowns.Value.Del(hero);

                    continue;
                }

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out MeteorConfig config, out int level) == false)
                    continue;

                Vector3 origin = _positions.Value.Get(hero).Value;

                if (TryFindCluster(origin, config, out Vector3 target) == false)
                    continue;

                MeteorLevel entry = config.Level(level);
                int castTicks = Mathf.Max(1, Mathf.RoundToInt(config.CastSeconds / delta));

                ref MeteorCast cast = ref _castPool.Value.Add(world.NewEntity());
                cast.Config = config;
                cast.Owner = world.PackEntity(hero);
                cast.Damage = entry.Damage;
                cast.Target = target;
                cast.RemainingTicks = castTicks;

                _cooldowns.Value.Add(hero).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));

                if (_views.Value.Has(hero) && _views.Value.Get(hero).Value != null)
                    _views.Value.Get(hero).Value.PlayAttack(config.CastTrigger, config.CastClipImpactSeconds / (castTicks * delta));
            }

            foreach (int entity in _casts.Value)
                AdvanceCast(world, entity, delta);

            foreach (int entity in _meteors.Value)
                AdvanceMeteor(world, entity, delta);
        }

        private void AdvanceCast(EcsWorld world, int entity, float delta)
        {
            ref MeteorCast cast = ref _castPool.Value.Get(entity);

            if (cast.Owner.Unpack(world, out int owner) == false || _dead.Value.Has(owner))
            {
                world.DelEntity(entity);

                return;
            }

            if (--cast.RemainingTicks > 0)
                return;

            MeteorConfig config = cast.Config;
            Vector3 origin = _positions.Value.Get(owner).Value;
            Vector3 target = TryFindCluster(origin, config, out Vector3 retarget) ? retarget : cast.Target;

            Vector3 back = origin - target;
            back.y = 0f;
            back = back.sqrMagnitude > Mathf.Epsilon ? back.normalized : Vector3.back;

            Vector3 from = target + back * config.FallDrift;
            int fallTicks = Mathf.Max(1, Mathf.RoundToInt(config.FallSeconds / delta));

            ref Meteor meteor = ref _meteorPool.Value.Add(entity);
            meteor.Config = config;
            meteor.Owner = cast.Owner;
            meteor.Damage = cast.Damage;
            meteor.From = from;
            meteor.To = target;
            meteor.RemainingTicks = fallTicks;
            meteor.TotalTicks = fallTicks;

            _castPool.Value.Del(entity);

            _positions.Value.Add(entity).Value = from;
            _facings.Value.Add(entity).Value = -back;
            _notIndexed.Value.Add(entity);

            IView view = _viewFactory.Value.Create(config.MeteorPrefab, from);
            _views.Value.Add(entity).Value = view;

            if (view != null)
            {
                view.SetLift(config.FallHeight);
                view.SetLift(config.FallHeight);
            }
        }

        private void AdvanceMeteor(EcsWorld world, int entity, float delta)
        {
            ref Meteor meteor = ref _meteorPool.Value.Get(entity);
            meteor.RemainingTicks--;

            float progress = 1f - (float)meteor.RemainingTicks / meteor.TotalTicks;
            _positions.Value.Get(entity).Value = Vector3.Lerp(meteor.From, meteor.To, progress);

            if (_views.Value.Has(entity) && _views.Value.Get(entity).Value != null)
                _views.Value.Get(entity).Value.SetLift(meteor.Config.FallHeight * (1f - progress));

            if (meteor.RemainingTicks > 0)
                return;

            Impact(world, in meteor);

            if (_views.Value.Has(entity))
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value != null)
                    _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }

            world.DelEntity(entity);
        }

        private void Impact(EcsWorld world, in Meteor meteor)
        {
            MeteorConfig config = meteor.Config;
            float area = ConcentrationBonus.Area(world, _concentrations.Value, meteor.Owner);

            ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
            strike.Owner = meteor.Owner;
            strike.Origin = meteor.To;
            strike.Forward = Vector3.forward;
            strike.Reach = config.Radius * area;
            strike.HalfAngleDegrees = FullCircleDegrees;
            strike.Damage = meteor.Damage;
            strike.Kind = DamageKind.Perk;
            strike.IsMagic = true;
            strike.Element = DamageElement.Fire;
            strike.SourcePerk = config;
            strike.PushSpeed = config.PushSpeed;
            strike.PushSeconds = config.PushSeconds;
            strike.PushArcHeight = config.PushArcHeight;

            int fx = world.NewEntity();
            _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.ImpactSeconds / _clock.Value.Delta));
            _positions.Value.Add(fx).Value = meteor.To;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = AreaScale.Apply(_viewFactory.Value.Create(config.ImpactPrefab, meteor.To), area);
        }

        private bool TryFindCluster(Vector3 origin, MeteorConfig config, out Vector3 target)
        {
            target = default;

            float rangeSquared = config.Range * config.Range;
            float clusterSquared = config.ClusterRadius * config.ClusterRadius;
            int best = 0;

            foreach (int candidate in _enemies.Value)
            {
                Vector3 center = _positions.Value.Get(candidate).Value;

                if (PlanarSquared(center - origin) > rangeSquared)
                    continue;

                int count = 0;

                foreach (int other in _enemies.Value)
                {
                    if (PlanarSquared(_positions.Value.Get(other).Value - center) <= clusterSquared)
                        count++;
                }

                if (count <= best)
                    continue;

                best = count;
                target = center;
            }

            return best > 0;
        }

        private static float PlanarSquared(Vector3 offset)
        {
            return offset.x * offset.x + offset.z * offset.z;
        }
    }
}
