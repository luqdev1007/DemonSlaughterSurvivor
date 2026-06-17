using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay.Characters;
using DemonSlaughter.Gameplay.ECS;
using UnityEngine;
using VContainer.Unity;

namespace DemonSlaughter.Gameplay
{
    public sealed class GameplayEntryPoint : IStartable
    {
        private readonly CharacterFactory _characterFactory;
        private readonly EcsPipeline _pipeline;
        private readonly EcsRunner _ecsRunner;
        private readonly IGameplayReadySignal _readySignal;
        private readonly CharacterConfig _gatsConfig;
        private readonly Vector3 _spawnPoint;

        public GameplayEntryPoint(
            CharacterFactory characterFactory,
            EcsPipeline pipeline,
            EcsRunner ecsRunner,
            IGameplayReadySignal readySignal,
            CharacterConfig gatsConfig,
            Vector3 spawnPoint)
        {
            _characterFactory = characterFactory;
            _pipeline = pipeline;
            _ecsRunner = ecsRunner;
            _readySignal = readySignal;
            _gatsConfig = gatsConfig;
            _spawnPoint = spawnPoint;
        }

        public void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            await _characterFactory.CreatePlayerAsync(_gatsConfig, _spawnPoint);

            _ecsRunner.Initialize(_pipeline);

            Debug.Log("ECS initialized, player spawned");

            _readySignal.Fire();
        }
    }
}