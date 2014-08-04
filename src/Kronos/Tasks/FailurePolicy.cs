using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intelli.Kronos.Tasks
{
    public enum FailurePolicy
    {
        Default = 1,
        CancelTask = 0,
        ExponentialRetry = 1,
    }
}
