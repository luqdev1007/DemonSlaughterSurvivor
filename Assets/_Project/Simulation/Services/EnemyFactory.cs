using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Services
{
    public sealed class EnemyFactory
    {
        private readonly EcsWorld _world;
        private readonly IViewFactory _viewFactory;

        private readonly EcsPool<Enemy> _enemies;
        private readonly EcsPool<Position> _positions;
        private readonly EcsPool<Facing> _facings;
        private readonly EcsPool<MoveIntent> _intents;
        private readonly EcsPool<MoveSpeed> _speeds;
        private readonly EcsPool<TurnSpeed> _turnSpeeds;
        private readonly EcsPool<Velocity> _velocities;
        private readonly EcsPool<ChaseTarget> _chaseTargets;
        private readonly EcsPool<View> _views;

        public EnemyFactory(EcsWorld world, IViewFactory viewFactory)
        {
            _world = world;
            _viewFactory = viewFactory;

            _enemies = world.GetPool<Enemy>();
            _positions = world.GetPool<Position>();
            _facings = world.GetPool<Facing>();
            _intents = world.GetPool<MoveIntent>();
            _speeds = world.GetPool<MoveSpeed>();
            _turnSpeeds = world.GetPool<TurnSpeed>();
            _velocities = world.GetPool<Velocity>();
            _chaseTargets = world.GetPool<ChaseTarget>();
            _views = world.GetPool<View>();
        }

        public int Create(EnemyConfig config, Vector3 position)
        {
            int entity = _world.NewEntity();

            _enemies.Add(entity);

            ref Position entityPosition = ref _positions.Add(entity);
            entityPosition.Value = position;

            ref Facing facing = ref _facings.Add(entity);
            facing.Value = Vector3.forward;

            ref MoveSpeed speed = ref _speeds.Add(entity);
            speed.Value = config.MoveSpeed;

            ref TurnSpeed turnSpeed = ref _turnSpeeds.Add(entity);
            turnSpeed.Value = config.TurnSpeed;

            _intents.Add(entity);
            _velocities.Add(entity);
            _chaseTargets.Add(entity);

            ref View view = ref _views.Add(entity);
            view.Value = _viewFactory.Create(config.ViewPrefab, position);

            return entity;
        }
    }
}
