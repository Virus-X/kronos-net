using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Intelli.Kronos.Worker
{
    public class NodeWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NodeWorker));

        private readonly IWorkQueueProvider queueProvider;

        private readonly object syncRoot = new object();

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

            Task.Factory.StartNew(WorkerLoop, token, token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(t =>
                                  {
                                      lock (syncRoot)
                                      {
                                          isRunning = false;
                                      }

                                      if (t.IsFaulted && t.Exception != null)
                                      {
                                          Log.Error("Worker loop stopped with unhandled exception: ", t.Exception.GetBaseException());
                                      }
                                  });

        }

        private void WorkerLoop(object state)
        {
            var token = (CancellationToken)state;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                var unitOfWork = queueProvider.GetNextTask(token);
                if (unitOfWork != null)
                {
                    unitOfWork.Process(token);
                }
            }
        }
    }
}
