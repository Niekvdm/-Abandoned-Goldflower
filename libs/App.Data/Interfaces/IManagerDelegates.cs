using System;
using App.Data.Enums;
using App.Data.Events;
using App.Data.Models;

namespace App.Data.Interfaces
{    
    public interface IManagerDelegates
    {
        void NotifyProgressChanged(ProgressChangedEventArgs e);

        void NotifyFileChanged(FileChangedEventArgs e);

        void NotifyFileStateChanged(FileStateChangedEventArgs e);

        void NotifyInstallStateChanged(InstallStateChangedEventArgs e);
    }
}