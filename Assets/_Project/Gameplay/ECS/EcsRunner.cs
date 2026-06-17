using UnityEngine;

namespace DemonSlaughter.Gameplay.ECS
{
    public sealed class EcsRunner : MonoBehaviour
    {
        private EcsPipeline _pipeline;

        public void Initialize(EcsPipeline pipeline)
        {
            _pipeline = pipeline;
            _pipeline.Initialize();
        }

        private void Update()
        {
            _pipeline?.Tick();
        }

        private void OnDestroy()
        {
            _pipeline?.Dispose();
        }
    }
}