using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyBossBlindZoneSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent>> _events = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _events.Value)
            {
                ref DamageEvent damageEvent = ref _damageEvents.Value.Get(entity);

                if (damageEvent.Source.Unpack(world, out int boss) == false || _bosses.Value.Has(boss) == false)
                    continue;

                if (_actions.Value.Has(boss) == false || _actions.Value.Get(boss).Phase == EnemyActionPhase.None)
                    continue;

                if (EnemyActionPhases.IsJump(_actions.Value.Get(boss).Phase))
                    continue;

                if (damageEvent.Target.Unpack(world, out int hero) == false || _players.Value.Has(hero) == false)
                    continue;

                Vector3 toHero = _positions.Value.Get(hero).Value - _positions.Value.Get(boss).Value;
                Vector3 facing = _facings.Value.Get(boss).Value;
                toHero.y = 0f;
                facing.y = 0f;

                if (toHero.sqrMagnitude <= Mathf.Epsilon || facing.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                damageEvent.Amount *= _bosses.Value.Get(boss).Config.BlindZoneDamageScale(Vector3.Angle(facing, toHero));
            }
        }
    }
}
