using System;
using App.Data.Enums;
using App.Data.Events;
using App.Data.Models;

namespace App.Data.Interfaces
{

    public delegate void ProgressChanged(ProgressChangedEventArgs e);
    public delegate void FileChanged(FileChangedEventArgs e);
    public delegate void FileStateChanged(FileStateChangedEventArgs e);
    public delegate void InstallStateChanged(InstallStateChangedEventArgs e);
    public delegate void MessageReceived(MessageReceivedEventArgs e);


    public interface IManagerEvents
    {
        event ProgressChanged OnProgressChanged;

        event FileChanged OnFileChanged;

        event FileStateChanged OnFileStateChanged;

        event InstallStateChanged OnInstallStateChanged;

        event MessageReceived OnMessageReceived;
    }
}