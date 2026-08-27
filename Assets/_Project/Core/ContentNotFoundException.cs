using System;

namespace Game.Core
{
    public sealed class ContentNotFoundException : Exception
    {
        public ContentNotFoundException(string message) : base(message)
        {
            
        }
    }
}
