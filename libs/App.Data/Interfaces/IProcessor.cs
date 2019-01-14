using System.Collections.Generic;
using App.Data.Models;

namespace App.Data.Interfaces
{
    public interface IProcessor : IManagerEvents
    {
        void Install(List<FileContainer> files);
        void Abort();
    }
}