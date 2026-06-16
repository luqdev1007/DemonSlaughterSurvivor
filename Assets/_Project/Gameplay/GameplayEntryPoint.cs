using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay.Characters;
using UnityEngine;
using VContainer.Unity;

namespace DemonSlaughter.Gameplay
{
    public sealed class GameplayEntryPoint : IStartable
    {
        private readonly CharacterFactory _characterFactory;
        private readonly CharacterConfig _gatsConfig;
        private readonly Vector3 _spawnPoint;
        private readonly IGameplayReadySignal _readySignal;

        private PlayerController _playerController;

        public GameplayEntryPoint(
            CharacterFactory characterFactory,
            CharacterConfig gatsConfig,
            Vector3 spawnPoint,
            IGameplayReadySignal readySignal)
        {
            _characterFactory = characterFactory;
            _gatsConfig = gatsConfig;
            _spawnPoint = spawnPoint;
            _readySignal = readySignal;
        }

        public void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            _playerController = await _characterFactory.CreateAsync(_gatsConfig, _spawnPoint);
            _playerController.Start();

            Debug.Log("Player spawned and ready");

            // Сцена готова — сигналим наружу
            _readySignal.Fire();
        }
    }
}