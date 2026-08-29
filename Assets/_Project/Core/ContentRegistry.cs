using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Core
{
    public sealed class ContentRegistry : IContentRegistry
    {
        private readonly Dictionary<string, IContentEntry> _byId = new();
        private readonly Dictionary<Type, List<IContentEntry>> _byType = new();
        private readonly Dictionary<Type, object> _typedCache = new();

        public ContentRegistry(IEnumerable<IContentEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            List<string> errors = new List<string>();
            int index = -1;

            foreach (IContentEntry entry in entries)
            {
                index++;

                if (entry == null)
                {
                    errors.Add($"[{index}] empty slot: the entry is null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    errors.Add($"[{index}] empty id on {entry}");
                    continue;
                }

                if (_byId.TryGetValue(entry.Id, out IContentEntry existing))
                {
                    errors.Add($"[{index}] duplicate id '{entry.Id}': {entry} and {existing}");
                    continue;
                }

                _byId.Add(entry.Id, entry);

                Type type = entry.GetType();

                if (_byType.TryGetValue(type, out List<IContentEntry> sameType) == false)
                {
                    sameType = new List<IContentEntry>();
                    _byType.Add(type, sameType);
                }

                sameType.Add(entry);
            }

            if (errors.Count > 0)
                throw new ContentValidationException(BuildValidationMessage(errors));
        }

        public T Get<T>(string id) where T : class, IContentEntry
        {
            if (string.IsNullOrEmpty(id))
                throw new ContentNotFoundException($"Content id is null or empty. Requested type: {typeof(T).Name}.");

            if (_byId.TryGetValue(id, out IContentEntry entry) == false)
                throw new ContentNotFoundException($"No content with id '{id}'. Requested type: {typeof(T).Name}.");

            if (entry is T typed == false)
                throw new ContentNotFoundException(
                    $"Content with id '{id}' is {entry.GetType().Name}, but {typeof(T).Name} was requested.");

            return typed;
        }

        public bool TryGet<T>(string id, out T value) where T : class, IContentEntry
        {
            value = null;

            if (string.IsNullOrEmpty(id))
                return false;

            if (_byId.TryGetValue(id, out IContentEntry entry) == false)
                return false;

            value = entry as T;

            return value != null;
        }

        public IReadOnlyList<T> All<T>() where T : class, IContentEntry
        {
            Type type = typeof(T);

            if (_typedCache.TryGetValue(type, out object cached))
                return (IReadOnlyList<T>)cached;

            List<T> typed = new List<T>();

            if (_byType.TryGetValue(type, out List<IContentEntry> entries))
            {
                foreach (IContentEntry entry in entries)
                {
                    typed.Add((T)entry);
                }
            }

            _typedCache.Add(type, typed);

            return typed;
        }

        public bool Contains(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            return _byId.ContainsKey(id);
        }

        private static string BuildValidationMessage(List<string> errors)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("Content database is invalid, ");
            builder.Append(errors.Count);
            builder.AppendLine(" problem(s):");

            foreach (string error in errors)
            {
                builder.Append("  ");
                builder.AppendLine(error);
            }

            return builder.ToString();
        }
    }
}
