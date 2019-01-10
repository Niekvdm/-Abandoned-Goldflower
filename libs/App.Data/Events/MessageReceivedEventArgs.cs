using System;
using App.Data.Enums;

namespace App.Data.Events
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageType Type { get; set; }
        public string Message { get; set; }

        public MessageReceivedEventArgs(MessageType type, string message)
        {
            Type = type;
            Message = message;
        }
    }
}