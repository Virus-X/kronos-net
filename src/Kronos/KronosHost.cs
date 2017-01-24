using Intelli.Kronos.Storage;
using Intelli.Kronos.Worker;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Intelli.Kronos
{
    public interface IKronosHost : IDisposable
    {
        void Start();

        void Stop();
    }

    public class KronosHost : IDisposable, IKronosHost
    {
        private readonly INodeWatchdog watchdog;
        private readonly IWorkQueueProvider workQueue;
        private readonly List<NodeWorker> workers;
        private readonly IMetricsCounter metricsCounter;
        private readonly object syncRoot = new object();

        private bool isRunning;
        private CancellationTokenSource cts;

        public KronosHost(MongoDatabase db, int workerCount)
            : this(db, workerCount, null)
        {
        }

        public KronosHost(MongoDatabase db, int workerCount, IMetricsCounter metricsCounter)
        {
            if (metricsCounter == null)
            {
                metricsCounter = new NullMetricsCounter();
            }

            this.metricsCounter = metricsCounter;

            MongoConfigurator.Configure();
            var worknodeId = KronosConfig.WorknodeId;
            var storageFactory = new StorageFactory(db);

            var taskManagementService = new KronosTaskService(storageFactory, metricsCounter);

            var unitOfWorkFactory = new UnitOfWorkFactory(taskManagementService, storageFactory, KronosConfig.ProcessorFactory);
            workQueue = new WorkQueueProvider(worknodeId, storageFactory, unitOfWorkFactory);

            workers = new List<NodeWorker>(workerCount);
            for (var i = 0; i < workerCount; i++)
            {
                workers.Add(new NodeWorker(workQueue));
            }

            watchdog = new NodeWatchdog(worknodeId, storageFactory, workers);
        }

        public void Start()
        {
            lock (syncRoot)
            {
                if (!isRunning)
                {
                    cts = new CancellationTokenSource();
                    watchdog.Start();
                    workQueue.Start(cts.Token);
                    foreach (var worker in workers)
                    {
                        worker.Start(cts.Token);
                    }

                    isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (syncRoot)
            {
                if (isRunning)
                {
                    cts.Cancel();
                    watchdog.Stop();
                    isRunning = false;
                }
            }
        }

        public void Dispose()
        {
            Stop();
            foreach (var worker in workers)
            {
                worker.Dispose();
            }
        }
    }
}
