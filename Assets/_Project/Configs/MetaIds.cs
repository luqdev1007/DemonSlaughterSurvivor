using System;
using System.Collections.Generic;

namespace Game.Configs
{
    public static class MetaIds
    {
        public static void ValidateUnique<T>(string label, IReadOnlyList<T> entries, Func<T, string> idOf, List<string> errors)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] == null)
                {
                    errors.Add($"{label}: [{index}] empty slot");
                    continue;
                }

                string id = idOf(entries[index]);

                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add($"{label}: [{index}] empty id");
                    continue;
                }

                if (seen.Add(id) == false)
                    errors.Add($"{label}: duplicate id '{id}'");
            }
        }
    }
}
