using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class LightningArrowSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<LightningArrowCooldown>> _cooldowns = default;
        private readonly EcsFilterInject<Inc<LightningChain>> _chains = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, Health>, Exc<Dead>> _enemies = default;

        private readonly EcsPoolInject<LightningArrowCooldown> _cooldownPool = default;
        private readonly EcsPoolInject<LightningChain> _chainPool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _cooldowns.Value)
            {
                if (--_cooldownPool.Value.Get(hero).RemainingTicks <= 0)
                    _cooldownPool.Value.Del(hero);
            }

            foreach (int entity in _chains.Value)
            {
                ref LightningChain chain = ref _chainPool.Value.Get(entity);

                if (chain.TicksToJump > 0)
                {
                    chain.TicksToJump--;

                    continue;
                }

                if (chain.JumpsLeft <= 0 || TryFindNext(world, in chain, out int next) == false)
                {
                    if (chain.HitCount == 1)
                        SpawnBolt(world, chain.Config, chain.From, chain.From);

                    world.DelEntity(entity);

                    continue;
                }

                Vector3 to = _positions.Value.Get(next).Value;

                Hit(world, in chain, next, to);
                SpawnBolt(world, chain.Config, chain.From, to);

                chain.Hits[chain.HitCount++] = world.PackEntity(next);
                chain.From = to;
                chain.JumpsLeft--;
                chain.Damage *= chain.Config.JumpFalloff;
                chain.TicksToJump = Mathf.RoundToInt(chain.Config.JumpSeconds / _clock.Value.Delta);
            }
        }

        private bool TryFindNext(EcsWorld world, in LightningChain chain, out int next)
        {
            float best = chain.Config.ChainRange * chain.Config.ChainRange;
            next = -1;

            foreach (int enemy in _enemies.Value)
            {
                Vector3 offset = _positions.Value.Get(enemy).Value - chain.From;
                offset.y = 0f;

                float distance = offset.sqrMagnitude;

                if (distance > best || WasHit(world, in chain, enemy))
                    continue;

                best = distance;
                next = enemy;
            }

            return next >= 0;
        }

        private static bool WasHit(EcsWorld world, in LightningChain chain, int enemy)
        {
            for (int index = 0; index < chain.HitCount; index++)
            {
                if (chain.Hits[index].Unpack(world, out int hit) && hit == enemy)
                    return true;
            }

            return false;
        }

        private void Hit(EcsWorld world, in LightningChain chain, int target, Vector3 at)
        {
            ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
            damageEvent.Target = world.PackEntity(target);
            damageEvent.Source = chain.Owner;
            damageEvent.SourcePosition = chain.From;
            damageEvent.Amount = chain.Damage;
            damageEvent.Kind = DamageKind.Perk;
            damageEvent.SourcePerk = chain.Config;
        }

        private void SpawnBolt(EcsWorld world, LightningArrowConfig config, Vector3 from, Vector3 to)
        {
            int fx = world.NewEntity();
            _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.BoltSeconds / _clock.Value.Delta));
            _positions.Value.Add(fx).Value = from;
            _facings.Value.Add(fx).Value = Vector3.forward;
            _notIndexed.Value.Add(fx);

            IView view = _viewFactory.Value.Create(config.BoltPrefab, from);
            _views.Value.Add(fx).Value = view;

            Vector3 lift = Vector3.up * config.BoltHeight;

            if (view is ILightningView lightning)
                lightning.Strike(from + lift, to + lift);
        }
    }
}
