using System;
using System.Collections.Generic;
using System.Text;
using Goldtree.Enums;

namespace Goldtree.Models
{
    public class FileContainer
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
        public string Extension { get; set; }
        public bool Selected { get; set; }
        public InstallState State { get; set; } = InstallState.Idle;
    }
}