using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Data.Enums
{
    public enum InstallState
    {
        AwaitingUserInput = 0,
        Installing = 1,
        Aborted = 2,
        Finished = 3,
        Cancelled = 4,
        Idle = 5,
        Failed = 6
    }
}
