using Game.Core;
using System;

namespace Game.Services
{
    public sealed class SeedSource : ISeedSource
    {
        private readonly Random _random;

        public SeedSource()
        {
            _random = new Random(Guid.NewGuid().GetHashCode());
        }

        public int Next()
        {
            return _random.Next(int.MinValue, int.MaxValue);
        }
    }
}
