using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DemonSlaughter.Gameplay.Characters
{
    public sealed class CharacterFactory
    {
        private readonly IAssetProvider _assetProvider;
        private readonly IObjectResolver _resolver;

        public CharacterFactory(IAssetProvider assetProvider, IObjectResolver resolver)
        {
            _assetProvider = assetProvider;
            _resolver = resolver;
        }

        public async UniTask<PlayerController> CreateAsync(CharacterConfig config, Vector3 spawnPoint)
        {
            var prefab = await _assetProvider.LoadAsync<GameObject>(config.AddressableAddress);
            var instance = Object.Instantiate(prefab, spawnPoint, Quaternion.identity);

            _resolver.InjectGameObject(instance);

            var mover = instance.GetComponent<CharacterMover>();
            mover.Initialize(config.MoveSpeed);

            var inputActions = new PlayerInputSystem();
            var inputHandler = instance.GetComponent<PlayerInputHandler>();
            inputHandler.Initialize(inputActions);

            return new PlayerController(inputHandler, mover);
        }
    }
}