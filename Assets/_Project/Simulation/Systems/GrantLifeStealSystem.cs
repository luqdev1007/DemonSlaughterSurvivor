using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class GrantLifeStealSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, LifeSteal, MaxHealth>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Enemy, DiedEvent, KillingBlow>> _kills = default;
        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _applied = default;

        private readonly EcsPoolInject<LifeSteal> _lifeSteals = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<KillingBlow> _killingBlows = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<HealEvent> _healEvents = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                ref LifeSteal lifeSteal = ref _lifeSteals.Value.Get(hero);

                int kills = lifeSteal.OnKill > 0f ? CountKills(world, hero) : 0;
                int hits = lifeSteal.OnHit > 0f ? CountWeaponHits(world, hero) : 0;

                float fraction = kills * lifeSteal.OnKill + hits * lifeSteal.OnHit;

                if (fraction <= 0f)
                    continue;

                int eventEntity = world.NewEntity();

                ref HealEvent healEvent = ref _healEvents.Value.Add(eventEntity);
                healEvent.Target = world.PackEntity(hero);
                healEvent.Amount = fraction * _maxHealths.Value.Get(hero).Value;
            }
        }

        private int CountKills(EcsWorld world, int hero)
        {
            int kills = 0;

            foreach (int enemy in _kills.Value)
            {
                if (_breakables.Value.Has(enemy))
                    continue;

                if (_killingBlows.Value.Get(enemy).Source.Unpack(world, out int killer) && killer == hero)
                    kills++;
            }

            return kills;
        }

        private int CountWeaponHits(EcsWorld world, int hero)
        {
            int hits = 0;

            foreach (int entity in _applied.Value)
            {
                ref DamageEvent damageEvent = ref _damageEvents.Value.Get(entity);

                if (damageEvent.Kind != DamageKind.Weapon)
                    continue;

                if (damageEvent.Source.Unpack(world, out int source) == false || source != hero)
                    continue;

                if (damageEvent.Target.Unpack(world, out int target) == false)
                    continue;

                if (_enemies.Value.Has(target) == false || _breakables.Value.Has(target))
                    continue;

                hits++;
            }

            return hits;
        }
    }
}
