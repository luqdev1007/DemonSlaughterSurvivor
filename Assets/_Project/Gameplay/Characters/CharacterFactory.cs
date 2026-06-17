using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay.ECS;
using DemonSlaughter.Gameplay.ECS.Components;
using UnityEngine;

namespace DemonSlaughter.Gameplay.Characters
{
    public sealed class CharacterFactory
    {
        private readonly IAssetProvider _assetProvider;
        private readonly EcsPipeline _pipeline;

        public CharacterFactory(IAssetProvider assetProvider, EcsPipeline pipeline)
        {
            _assetProvider = assetProvider;
            _pipeline = pipeline;
        }

        public async UniTask<int> CreatePlayerAsync(CharacterConfig config, Vector3 spawnPoint)
        {
            var prefab = await _assetProvider.LoadAsync<GameObject>(config.AddressableAddress);
            GameObject instance = Object.Instantiate(prefab, spawnPoint, Quaternion.identity);

            return SetupEntity(config, instance);
        }

        private int SetupEntity(CharacterConfig config, GameObject instance)
        {
            var world = _pipeline.World;
            var entity = world.NewEntity();

            world.GetPool<PlayerTagComponent>().Add(entity);

            ref var movement = ref world.GetPool<MovementComponent>().Add(entity);
            movement.Speed = config.MoveSpeed;

            world.GetPool<MoveInputComponent>().Add(entity);

            ref var transformRef = ref world.GetPool<TransformRefComponent>().Add(entity);
            transformRef.Value = instance.transform;

            ref var animatorRef = ref world.GetPool<AnimatorRefComponent>().Add(entity);
            animatorRef.Value = instance.GetComponentInChildren<Animator>();

            return entity;
        }
    }
}