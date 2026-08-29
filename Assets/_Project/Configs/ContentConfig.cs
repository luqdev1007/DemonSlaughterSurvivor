using Game.Core;
using UnityEngine;

namespace Game.Configs
{
    public abstract class ContentConfig : ScriptableObject, IContentEntry
    {
        [SerializeField] private string _id;

        public string Id => _id;
    }
}
