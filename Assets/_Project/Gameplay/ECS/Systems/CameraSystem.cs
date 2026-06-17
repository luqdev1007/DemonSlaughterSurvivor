using DemonSlaughter.Gameplay.Camera;
using DemonSlaughter.Gameplay.ECS.Components;
using Leopotam.EcsLite;
using Unity.Cinemachine;

namespace DemonSlaughter.Gameplay.ECS.Systems
{
    public sealed class CameraSystem : IEcsInitSystem
    {
        private readonly CinemachineCamera _virtualCamera;
        private readonly CameraOcclusionHandler _occlusionHandler;

        public CameraSystem(CinemachineCamera virtualCamera, CameraOcclusionHandler occlusionHandler)
        {
            _virtualCamera = virtualCamera;
            _occlusionHandler = occlusionHandler;
        }

        public void Init(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            var filter = world
                .Filter<CameraTargetTag>()
                .Inc<TransformRefComponent>()
                .End();

            var transformPool = world.GetPool<TransformRefComponent>();

            foreach (var entity in filter)
            {
                ref var transformRef = ref transformPool.Get(entity);
                _virtualCamera.Follow = transformRef.Value;
                _occlusionHandler.SetTarget(transformRef.Value);
                break;
            }
        }
    }
}