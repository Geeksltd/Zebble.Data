using System;

namespace Zebble.Data
{
    public class SaveEventArgs : EventArgs
    {
        public SaveEventArgs(SaveMode mode)
        {
            Mode = mode;
        }

        public SaveMode Mode { get; private set; }
    }

    public enum SaveMode { Update, Insert }
}