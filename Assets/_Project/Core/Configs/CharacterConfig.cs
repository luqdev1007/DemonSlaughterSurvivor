using UnityEngine;

namespace DemonSlaughter.Core.Configs
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Configs/Gameplay/CharacterConfig")]
    public sealed class CharacterConfig : ScriptableObject
    {
        [field: SerializeField] public string AddressableAddress { get; private set; }
        [field: SerializeField] public float MoveSpeed { get; private set; } = 5f;
    }
}