using log4net;
using System;
using System.Threading;

namespace Intelli.Kronos.Worker
{
    public class NodeWorker : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NodeWorker));

        private readonly IWorkQueueProvider queueProvider;

        private readonly object syncRoot = new object();

        private Thread workerThread;

        private CancellationTokenSource cancellationSource;

        private bool isRunning;

        public NodeWorker(IWorkQueueProvider queueProvider)
        {
            this.queueProvider = queueProvider;
        }

        public void Start(CancellationToken token)
        {
            lock (syncRoot)
            {
                if (isRunning)
                {
                    throw new InvalidOperationException("Worker already running. Use cancellation token to stop it.");
                }

                isRunning = true;
            }

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            workerThread = new Thread(WorkerLoop) { Name = "KronosWorker" };
            workerThread.Start(cancellationSource.Token);
        }

        private void WorkerLoop(object state)
        {
            var token = (CancellationToken)state;

            while (true)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var unitOfWork = queueProvider.GetNextTask(token);
                    if (unitOfWork != null)
                    {
                        unitOfWork.Process(token);
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Unhandled exception in worker loop: {0}", ex);
                    Thread.Sleep(5000);
                }
            }
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                isRunning = false;
            }

            cancellationSource.Cancel();
        }
    }
}
