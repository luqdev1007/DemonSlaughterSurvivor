using System;

namespace Game.Core
{
    public static class StableHash
    {
        private const uint OffsetBasis = 2166136261u;
        private const uint Prime = 16777619u;

        public static uint Fnv1a32(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            uint hash = OffsetBasis;

            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];

                if (current < 0x80)
                {
                    hash = Mix(hash, current);

                    continue;
                }

                if (current < 0x800)
                {
                    hash = Mix(hash, 0xC0 | (current >> 6));
                    hash = Mix(hash, 0x80 | (current & 0x3F));

                    continue;
                }

                if (char.IsHighSurrogate(current) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                {
                    int codePoint = char.ConvertToUtf32(current, value[index + 1]);

                    hash = Mix(hash, 0xF0 | (codePoint >> 18));
                    hash = Mix(hash, 0x80 | ((codePoint >> 12) & 0x3F));
                    hash = Mix(hash, 0x80 | ((codePoint >> 6) & 0x3F));
                    hash = Mix(hash, 0x80 | (codePoint & 0x3F));

                    index++;

                    continue;
                }

                hash = Mix(hash, 0xE0 | (current >> 12));
                hash = Mix(hash, 0x80 | ((current >> 6) & 0x3F));
                hash = Mix(hash, 0x80 | (current & 0x3F));
            }

            return hash;
        }

        private static uint Mix(uint hash, int value)
        {
            hash ^= (uint)(value & 0xFF);

            return unchecked(hash * Prime);
        }
    }
}
