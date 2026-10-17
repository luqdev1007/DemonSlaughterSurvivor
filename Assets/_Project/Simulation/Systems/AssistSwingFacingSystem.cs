using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AssistSwingFacingSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Swing>> _swings = default;

        private readonly EcsPoolInject<Swing> _swingPool = default;
        private readonly EcsPoolInject<Weapon> _weapons = default;
        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Dashing> _dashing = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int entity in _swings.Value)
            {
                ref Swing swing = ref _swingPool.Value.Get(entity);

                if (swing.HasTarget == false || swing.ClipTime >= swing.Variant.WindowEnd)
                    continue;

                if (swing.Owner.Unpack(world, out int owner) == false || swing.Weapon.Unpack(world, out int weapon) == false)
                    continue;

                if (_players.Value.Has(owner) == false || _dead.Value.Has(owner) || _dashing.Value.Has(owner) || _facings.Value.Has(owner) == false)
                    continue;

                if (swing.Target.Unpack(world, out int target) == false || _dead.Value.Has(target) || _positions.Value.Has(target) == false)
                {
                    swing.HasTarget = false;

                    continue;
                }

                WeaponConfig config = _weapons.Value.Get(weapon).Config;
                Vector3 toTarget = _positions.Value.Get(target).Value - _positions.Value.Get(owner).Value;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                ref Facing facing = ref _facings.Value.Get(owner);

                if (Vector3.Angle(facing.Value, toTarget) > config.AssistMaxDegrees)
                    continue;

                facing.Value = Vector3.RotateTowards(facing.Value, toTarget.normalized, config.AssistTurnSpeedDegrees * Mathf.Deg2Rad * delta, 0f);
            }
        }
    }
}
