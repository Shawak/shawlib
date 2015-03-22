using System;

namespace BKR
{
    public class ExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }

        public ExceptionEventArgs(Exception ex)
        {
            Exception = ex;
        }
    }
}
