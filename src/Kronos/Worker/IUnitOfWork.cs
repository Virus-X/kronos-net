using System;
using System.Threading;

namespace Intelli.Kronos.Worker
{
    public interface IUnitOfWork
    {
        int Priority { get; }

        DateTime? TimeoutAt { get; }

        /// <summary>
        /// Starts processing the task
        /// </summary>
        /// <param name="token">Cancellation token to stop the task manually</param>
        /// <param name="timeout">Task timeout in milliseconds</param>
        void Process(CancellationToken token, long timeout);

        void Release();

        void KillTask(TaskStopReason reason);
    }
}
