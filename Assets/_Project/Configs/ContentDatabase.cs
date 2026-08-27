using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ContentDatabase", menuName = "Game/Content/Database")]
    public sealed class ContentDatabase : ScriptableObject
    {
        [SerializeField] private ContentConfig[] _entries;

        public IReadOnlyList<ContentConfig> Entries => _entries;
    }
}
