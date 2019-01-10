using System;
using App.Data.Enums;

namespace App.Data.Events
{
    public class InstallStateChangedEventArgs : EventArgs
    {
        public InstallState State { get; set; }
        public string Message { get; set; }

        public InstallStateChangedEventArgs(InstallState state, string message = null)
        {
            State = state;
            Message = message;
        }
    }
}