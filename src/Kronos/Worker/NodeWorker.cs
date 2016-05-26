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

        private volatile IUnitOfWork currentJob;

        public IUnitOfWork CurrentJob
        {
            get { return currentJob; }
        }

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
            var taskTimeout = KronosConfig.TaskTimeoutSeconds * 1000;

            while (true)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    using (var task = queueProvider.GetNextTask(token))
                    {
                        if (task != null)
                        {
                            currentJob = task;
                            task.Process(token, taskTimeout);
                            currentJob = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Unhandled exception in worker loop: {0}", ex);
                    currentJob = null;
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
            cancellationSource.Dispose();
        }
    }
}
