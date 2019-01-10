using System;
using System.Collections.Generic;
using App.Data.Interfaces;
using App.Data.Models;

namespace Tinfoil
{
    public class Tinfoil : IProcessor
    {
        
        public event ProgressChanged OnProgressChanged;
        public event FileChanged OnFileChanged;
        public event FileStateChanged OnFileStateChanged;
        public event InstallStateChanged OnInstallStateChanged;
        public event MessageReceived OnMessageReceived;


        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Install(List<FileContainer> files)
        {
            throw new NotImplementedException();
        }
    }
}
