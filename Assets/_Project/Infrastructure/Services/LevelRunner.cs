using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Services;
using UnityEngine;

namespace DemonSlaughter.Infrastructure.Services
{
    public sealed class LevelRunner : ILevelRunner
    {
        public async UniTask RunLevel(int levelId)
        {
            Debug.Log($"RunLevel START: {levelId}");

            await UniTask.Delay(500);

            Debug.Log("RunLevel END");
        }
    }
}