using System;
using App.Data.Models;

namespace App.Data.Events
{
    public class FileChangedEventArgs : EventArgs
    {
        public FileContainer File { get; set; }

        public FileChangedEventArgs(FileContainer file)
        {
            File = file;
        }
    }
}