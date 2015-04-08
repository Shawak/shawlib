using System;

namespace ShawLib
{
    public class MemoryException : Exception
    {
        public MemoryException(string message)
            : base(message)
        { }
    }
}
