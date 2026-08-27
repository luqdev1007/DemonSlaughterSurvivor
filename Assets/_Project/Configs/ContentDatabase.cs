using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Configs
{
    public sealed class ContentDatabase : ScriptableObject
    {
        [SerializeField] private ContentConfig[] _entries;

        public IReadOnlyList<ContentConfig> Entries => _entries;
    }
}
