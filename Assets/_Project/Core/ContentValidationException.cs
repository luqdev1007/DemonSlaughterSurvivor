using System;

namespace Game.Core
{
    public sealed class ContentValidationException : Exception
    {
        public ContentValidationException(string message) : base(message)
        {
            
        }
    }
}
