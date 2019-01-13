using System;
using App.Data.Enums;

namespace App.Data.Events
{
    public class FileStateChangedEventArgs : EventArgs
    {
        public InstallState State { get; set; }

        public FileStateChangedEventArgs(InstallState state)
        {
            State = state;
        }
    }
}