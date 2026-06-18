using DemonSlaughter.Gameplay.Camera;
using DemonSlaughter.Gameplay.ECS.Systems;
using Leopotam.EcsLite;
using Unity.Cinemachine;

namespace DemonSlaughter.Gameplay.ECS
{
    public sealed class EcsPipeline
    {
        private readonly EcsWorld _world;
        private readonly IEcsSystems _systems;

        public EcsWorld World => _world;

        public EcsPipeline(
            PlayerInputActions inputActions, 
            CinemachineCamera virtualCamera, 
            CameraOcclusionHandler cameraOcclusionHandler,
            string[] attackTriggers)
        {
            _world = new EcsWorld();

            _systems = new EcsSystems(_world)
                .Add(new PlayerInputSystem(inputActions))
                .Add(new MovementSystem())
                .Add(new AnimationSystem())
                .Add(new CameraSystem(virtualCamera, cameraOcclusionHandler))
                .Add(new AttackDetectionSystem())       
                .Add(new AttackSystem(attackTriggers)) 
                .Add(new AttackCooldownSystem())       
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