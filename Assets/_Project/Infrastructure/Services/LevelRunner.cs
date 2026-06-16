using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Services;
using UnityEngine;

namespace DemonSlaughter.Infrastructure.Services
{
    public sealed class LevelRunner : ILevelRunner
    {
        public async UniTask RunLevel(int levelId)
        {
            Debug.Log($"RunLevel: {levelId}");

            await UniTask.Delay(500);

            // пока просто заглушка
            // позже тут будет:
            // - load Addressables config
            // - init gameplay scene systems
        }
    }
}