using DemonSlaughter.Gameplay.ECS.Systems;
using Leopotam.EcsLite;

namespace DemonSlaughter.Gameplay.ECS
{
    public sealed class EcsPipeline
    {
        private readonly EcsWorld _world;
        private readonly IEcsSystems _systems;

        public EcsWorld World => _world;

        public EcsPipeline(PlayerInputActions inputActions)
        {
            _world = new EcsWorld();

            _systems = new EcsSystems(_world)
                .Add(new PlayerInputSystem(inputActions))
                .Add(new MovementSystem())
                .Add(new AnimationSystem())
                ;
        }

        public void Initialize()
        {
            _systems.Init();
        }

        public void Tick()
        {
            _systems.Run();
        }

        public void Dispose()
        {
            _systems.Destroy();
            _world.Destroy();
        }
    }
}