using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FaceNearestEnemySystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, MoveIntent, Velocity, Position, Facing, TurnSpeed>, Exc<Dashing, Dead>> _players = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink>> _weapons = default;

        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<TurnSpeed> _turnSpeeds = default;
        private readonly EcsPoolInject<Weapon> _weaponPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<Swinging> _swinging = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private List<int> _candidates;

        public void Init(IEcsSystems systems)
        {
            _candidates = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CandidateCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int player in _players.Value)
            {
                if (_intents.Value.Get(player).Value.sqrMagnitude > Mathf.Epsilon)
                    continue;

                if (_velocities.Value.Get(player).Value.sqrMagnitude > Mathf.Epsilon)
                    continue;

                if (TryResolveReach(player, out float reach) == false)
                    continue;

                Vector3 position = _positions.Value.Get(player).Value;

                if (TryFindNearest(position, reach, out Vector3 direction) == false)
                    continue;

                ref Facing facing = ref _facings.Value.Get(player);

                float maxRadians = _turnSpeeds.Value.Get(player).Value * Mathf.Deg2Rad * _clock.Value.Delta;

                facing.Value = Vector3.RotateTowards(facing.Value, direction, maxRadians, 0f);
            }
        }

        private bool TryResolveReach(int player, out float reach)
        {
            EcsWorld world = _world.Value;
            reach = 0f;

            foreach (int weapon in _weapons.Value)
            {
                if (_ownerLinks.Value.Get(weapon).Owner.Unpack(world, out int owner) == false || owner != player)
                    continue;

                if (_swinging.Value.Has(weapon))
                    return false;

                reach = Mathf.Max(reach, _weaponPool.Value.Get(weapon).Config.TriggerCoverage.MaxCoveredDistance);
            }

            return reach > 0f;
        }

        private bool TryFindNearest(Vector3 position, float reach, out Vector3 direction)
        {
            direction = default;

            _grid.Value.Query(position, reach, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            float nearest = reach * reach;
            bool found = false;

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false || _dead.Value.Has(candidate))
                    continue;

                Vector3 offset = _positions.Value.Get(candidate).Value - position;
                offset.y = 0f;

                float distance = offset.sqrMagnitude;

                if (distance > nearest || distance <= Mathf.Epsilon)
                    continue;

                nearest = distance;
                direction = offset;
                found = true;
            }

            return found;
        }
    }
}
