using System.Collections.Generic;

namespace Game.Core
{
    public interface IContentRegistry
    {
        T Get<T>(string id) where T : class, IContentEntry;
        bool TryGet<T>(string id, out T value) where T : class, IContentEntry;
        IReadOnlyList<T> All<T>() where T : class, IContentEntry;
        bool Contains(string id);
    }
}
