using Codice.CM.Common.Tree;
using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Configs.Enemies;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay.Combat;
using DemonSlaughter.Gameplay.ECS;
using DemonSlaughter.Gameplay.ECS.Components;
using UnityEngine;

namespace DemonSlaughter.Gameplay.Enemies
{
    public sealed class EnemyFactory
    {
        private readonly IAssetProvider _assetProvider;
        private readonly EcsPipeline _pipeline;

        public EnemyFactory(IAssetProvider assetProvider, EcsPipeline pipeline)
        {
            _assetProvider = assetProvider;
            _pipeline = pipeline;
        }

        public async UniTask<int> CreateAsync(EnemyConfig config, Vector3 position)
        {
            var prefab = await _assetProvider.LoadAsync<GameObject>(config.AddressableAddress);
            var instance = Object.Instantiate(prefab, position, Quaternion.identity);

            return SetupEntity(instance);
        }

        private int SetupEntity(GameObject instance)
        {
            var world = _pipeline.World;
            var entity = world.NewEntity();

            world.GetPool<EnemyTagComponent>().Add(entity);

            ref var transformRef = ref world.GetPool<TransformRefComponent>().Add(entity);
            transformRef.Value = instance.transform;

            var link = instance.AddComponent<EcsEntityLink>();
            link.Initialize(entity);

            /*
            var collider = instance.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                var link = collider.gameObject.AddComponent<EcsEntityLink>();
                link.Initialize(entity);
            }
            */

            return entity;
        }
    }
}