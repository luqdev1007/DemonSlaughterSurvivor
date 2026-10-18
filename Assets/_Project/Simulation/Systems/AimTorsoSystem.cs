using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AimTorsoSystem : IEcsRunSystem
    {
        private const float MinAimDistance = 0.05f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, TorsoAim, Position, Facing, Velocity>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Enemy, Position>, Exc<Dead>> _enemies = default;

        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<InputState> _inputs = default;
        private readonly EcsPoolInject<Enemy> _enemyPool = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                ref TorsoAim aim = ref _aims.Value.Get(hero);
                Vector3 position = _positions.Value.Get(hero).Value;

                if (TryManualAim(hero, position, out Vector3 manual))
                {
                    aim.Manual = true;
                    aim.HasTarget = false;
                    aim.Desired = manual;
                }
                else
                {
                    aim.Manual = false;

                    AcquireTarget(ref aim, position);

                    aim.Desired = aim.HasTarget ? ResolveTargetDirection(in aim, position) : ResolveIdleDirection(hero);
                }

                aim.Direction = YawTurn.Towards(aim.Direction, aim.Desired, aim.Config.TorsoTurnSpeed * delta);
            }
        }

        private bool TryManualAim(int hero, Vector3 position, out Vector3 direction)
        {
            direction = default;

            if (_inputs.Value.Has(hero) == false)
                return false;

            ref InputState input = ref _inputs.Value.Get(hero);

            if (input.StandHeld == false || input.HasAim == false)
                return false;

            Vector3 offset = input.AimPoint - position;
            offset.y = 0f;

            if (offset.sqrMagnitude < MinAimDistance * MinAimDistance)
                return false;

            direction = offset.normalized;

            return true;
        }

        private void AcquireTarget(ref TorsoAim aim, Vector3 position)
        {
            EcsWorld world = _world.Value;
            TorsoAimConfig config = aim.Config;

            float currentSquared = float.MaxValue;
            bool keep = false;

            if (aim.HasTarget && aim.Target.Unpack(world, out int current) && _enemyPool.Value.Has(current) && _dead.Value.Has(current) == false)
            {
                currentSquared = PlanarSquared(_positions.Value.Get(current).Value - position);
                keep = currentSquared <= config.KeepRadius * config.KeepRadius;
            }

            float nearestSquared = config.SearchRadius * config.SearchRadius;
            int nearest = -1;

            foreach (int enemy in _enemies.Value)
            {
                float squared = PlanarSquared(_positions.Value.Get(enemy).Value - position);

                if (squared > nearestSquared)
                    continue;

                if (squared == nearestSquared && nearest >= 0)
                    continue;

                nearestSquared = squared;
                nearest = enemy;
            }

            if (keep)
            {
                if (nearest < 0)
                    return;

                float switchDistance = Mathf.Sqrt(currentSquared) - config.SwitchMargin;

                if (switchDistance <= 0f || nearestSquared >= switchDistance * switchDistance)
                    return;
            }

            if (nearest < 0)
            {
                aim.HasTarget = false;
                aim.Target = default;

                return;
            }

            aim.HasTarget = true;
            aim.Target = world.PackEntity(nearest);
        }

        private Vector3 ResolveTargetDirection(in TorsoAim aim, Vector3 position)
        {
            if (aim.Target.Unpack(_world.Value, out int target) == false)
                return aim.Direction;

            Vector3 offset = _positions.Value.Get(target).Value - position;
            offset.y = 0f;

            return offset.sqrMagnitude < MinAimDistance * MinAimDistance ? aim.Direction : offset.normalized;
        }

        private Vector3 ResolveIdleDirection(int hero)
        {
            Vector3 velocity = _velocities.Value.Get(hero).Value;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > Mathf.Epsilon)
                return velocity.normalized;

            return _facings.Value.Get(hero).Value;
        }

        private static float PlanarSquared(Vector3 offset)
        {
            return offset.x * offset.x + offset.z * offset.z;
        }
    }
}
