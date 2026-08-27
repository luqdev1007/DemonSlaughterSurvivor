using Game.Core;
using UnityEngine;

namespace Game.Configs
{
    public abstract class ContentConfig : ScriptableObject, IContentEntry
    {
        // Explicit field, not [field: SerializeField]: the id is the one value that must never
        // be lost, and an auto-property backing field cannot carry [FormerlySerializedAs].
        [SerializeField] private string _id;

        public string Id => _id;
    }
}
