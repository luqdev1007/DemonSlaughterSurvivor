using UnityEngine;

namespace DemonSlaughter.Core.Configs.Enemies
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Characters/EnemyConfig")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [field: SerializeField] public string AddressableAddress { get; private set; } = "Enemy_Test";
    }
}