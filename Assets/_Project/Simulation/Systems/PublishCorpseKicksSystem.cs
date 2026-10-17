using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PublishCorpseKicksSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Swing, SwingStarted>> _swings = default;
        private readonly EcsFilterInject<Inc<AreaStrikeEvent>> _strikes = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Swing> _swingPool = default;
        private readonly EcsPoolInject<Weapon> _weapons = default;
        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<AreaStrikeEvent> _strikePool = default;

        private readonly EcsCustomInject<ICorpseKickSink> _sink = default;

        public void Run(IEcsSystems systems)
        {
            if (_sink.Value == null)
                return;

            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                Vector3 velocity = _velocities.Value.Has(hero) ? _velocities.Value.Get(hero).Value : Vector3.zero;

                _sink.Value.HeroMoved(_positions.Value.Get(hero).Value, velocity);
            }

            foreach (int entity in _swings.Value)
            {
                ref Swing swing = ref _swingPool.Value.Get(entity);

                if (swing.Owner.Unpack(world, out int owner) == false || _players.Value.Has(owner) == false || _facings.Value.Has(owner) == false)
                    continue;

                float reach = 2.5f;

                if (swing.Weapon.Unpack(world, out int weapon) && _weapons.Value.Has(weapon) && _weapons.Value.Get(weapon).Config != null)
                    reach = _weapons.Value.Get(weapon).Config.TriggerCoverage.MaxDistance;

                _sink.Value.Strike(_positions.Value.Get(owner).Value, _facings.Value.Get(owner).Value, reach * swing.Reach, swing.Variant.HalfAngleDegrees);
            }

            foreach (int entity in _strikes.Value)
            {
                ref AreaStrikeEvent strike = ref _strikePool.Value.Get(entity);

                _sink.Value.Strike(strike.Origin, strike.Forward, strike.Reach, strike.HalfAngleDegrees);
            }
        }
    }
}
