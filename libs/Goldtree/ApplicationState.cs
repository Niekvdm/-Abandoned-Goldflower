using Goldtree.Enums;
using Goldtree.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Goldtree
{
    public static class ApplicationState
    {
        public static InstallState InstallState { get; set; } = InstallState.Idle;
        public static List<FileContainer> Files { get; set; }
        public static FileContainer CurrentFile { get; set; }
        public static int Progress { get; set; }
        public static Dictionary<string, List<string>> MessageBag { get; set; }

        private static Goldtree _goldtree = null;
        private static Thread _runner = null;

        public static void Abort()
        {
            InstallState = InstallState.Idle;
            Files = null;
            CurrentFile = null;
            Progress = 0;
            MessageBag.Clear();

            if (_goldtree != null)
            {
                _goldtree.Abort();
                _goldtree = null;
                MessageBag.Clear();
            }
        }

        public static void Install(List<FileContainer> files)
        {
            if (MessageBag == null)
            {
                MessageBag = new Dictionary<string, List<string>>();
            }

            MessageBag.Clear();
            
            ApplicationState.InstallState = InstallState.Installing;
            ApplicationState.CurrentFile = files.FirstOrDefault(x => x.State.Equals(InstallState.Idle));
            ApplicationState.Files = files;

            _runner = new Thread(() =>
            {
                _goldtree = new Goldtree();
                _goldtree.Install();
            });

            _runner.SetApartmentState(ApartmentState.STA);

            _runner.Start();
        }

        public static void Complete()
        {
            ApplicationState.CurrentFile = null;
            ApplicationState.Files = null;
            ApplicationState.Progress = 0;
            ApplicationState.InstallState = InstallState.Idle;
            ApplicationState.MessageBag.Clear();
        }
    }
}
