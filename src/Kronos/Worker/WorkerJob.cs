using System;
using System.Threading;

namespace Intelli.Kronos.Worker
{
    public class WorkerJob
    {        
        private readonly CancellationTokenSource tokenSource;

        public WorkerJob(IUnitOfWork task, CancellationTokenSource tokenSource)
        {
            Task = task;
            this.tokenSource = tokenSource;
            StartTime = DateTime.UtcNow;
        }        

        public IUnitOfWork Task { get; private set; }

        public DateTime StartTime { get; private set; }

        public long MillisecondsElapsed
        {
            get { return (long)DateTime.UtcNow.Subtract(StartTime).TotalMilliseconds; }
        }

        public void Kill()
        {
            if (!tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
            }
        }
    }
}