using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SpawnPlayerSystem : IEcsInitSystem, IEcsDestroySystem
    {
        private static readonly Vector3 StartPosition = Vector3.zero;

        private readonly EcsWorldInject _world = default;

        // Every view ever created by the simulation, not only the player's:
        // Destroy has to hand all of them back, and later spawners add to the same pool.
        private readonly EcsFilterInject<Inc<View>> _viewed = default;

        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<MoveSpeed> _speeds = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Init(IEcsSystems systems)
        {
            // A broken character id fails here, naming the id, and not as a null three systems later.
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);

            int entity = _world.Value.NewEntity();

            _players.Value.Add(entity);

            // Add returns a ref to the freshly created component, so the write goes into the pool.
            ref Position position = ref _positions.Value.Add(entity);
            position.Value = StartPosition;

            // Not zero: the character has to look somewhere before the first step,
            // and LookRotation of a zero vector throws.
            ref Facing facing = ref _facings.Value.Add(entity);
            facing.Value = Vector3.forward;

            ref MoveSpeed speed = ref _speeds.Value.Add(entity);
            speed.Value = character.MoveSpeed;

            // Empty on purpose: the filters of the Input and Movement groups need the components
            // to exist, and their values are written every tick anyway.
            _intents.Value.Add(entity);
            _velocities.Value.Add(entity);

            ref View view = ref _views.Value.Add(entity);
            view.Value = _viewFactory.Value.Create(character.ViewPrefab, StartPosition);
        }

        public void Destroy(IEcsSystems systems)
        {
            // world.Destroy() drops entities but knows nothing about Unity objects:
            // whoever asked the factory for a view is the one who has to give it back.
            foreach (int entity in _viewed.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }
        }
    }
}
