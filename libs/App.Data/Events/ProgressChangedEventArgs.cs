using System;

namespace App.Data.Events
{
    public class ProgressChangedEventArgs : EventArgs
    {
        public int Percentage { get; set; }

        public ProgressChangedEventArgs(int percentage)
        {
            Percentage = percentage;
        }
    }
}