using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlayScreenShakeSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _hits = default;
        private readonly EcsFilterInject<Inc<LeapLandedEvent>> _landings = default;
        private readonly EcsFilterInject<Inc<EnemySlamEvent>> _slams = default;
        private readonly EcsFilterInject<Inc<Player, UltimateCharge>> _heroes = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<BerserkMode> _modes = default;

        private readonly EcsCustomInject<IScreenShake> _shake = default;

        private bool _wasBerserk;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _hits.Value)
            {
                ref DamageEvent damageEvent = ref _damageEvents.Value.Get(entity);

                if (damageEvent.Target.Unpack(world, out int target) == false || _players.Value.Has(target) == false)
                    continue;

                _shake.Value.Shake(ShakeKind.HeroHit);

                break;
            }

            if (_landings.Value.GetEntitiesCount() > 0)
                _shake.Value.Shake(ShakeKind.Landing);

            if (_slams.Value.GetEntitiesCount() > 0)
                _shake.Value.Shake(ShakeKind.BossSlam);

            bool isBerserk = false;

            foreach (int hero in _heroes.Value)
                isBerserk |= _modes.Value.Has(hero);

            if (isBerserk && _wasBerserk == false)
                _shake.Value.Shake(ShakeKind.Ultimate);

            _wasBerserk = isBerserk;
        }
    }
}
