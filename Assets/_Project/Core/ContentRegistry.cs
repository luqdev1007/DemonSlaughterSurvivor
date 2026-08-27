using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class ContentRegistry : IContentRegistry
    {
        private Dictionary<string, IContentEntry> _contentEntries = new();
        private Dictionary<Type, List<IContentEntry>> _map = new();

        public ContentRegistry(IEnumerable<IContentEntry> entries)
        {
            foreach (IContentEntry entry in entries)
            {
                _contentEntries.Add(entry.Id, entry);
                
                if (_map.ContainsKey(entry.GetType()) == false)
                {
                    _map.Add(entry.GetType(), new List<IContentEntry>());
                }

                _map[entry.GetType()].Add(entry);
            }
        }

        public bool Contains(string id)
        {
            if (_contentEntries.ContainsKey(id) == false)
                return false;
            else
                return _contentEntries[id] != null && _contentEntries[id].Id != "";
        }

        public IReadOnlyList<T> All<T>() where T : class, IContentEntry
        {
            throw new System.NotImplementedException();
        }

        public T Get<T>(string id) where T : class, IContentEntry
        {
            if (_contentEntries.TryGetValue(id, out IContentEntry entry) == false)
                throw new ContentNotFoundException($"Content with id '{id}' not found");

            if (entry is T typed == false)
                throw new ContentValidationException(
                    $"Content '{id}' is {entry.GetType().Name}, requested {typeof(T).Name}");

            return typed;
        }

        public bool TryGet<T>(string id, out T value) where T : class, IContentEntry
        {
            value = _contentEntries.TryGetValue(id, out IContentEntry entry)
                ? entry as T
                : null;

            return value != null;
        }
    }
}
