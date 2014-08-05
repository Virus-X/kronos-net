using System;
using System.Collections.Generic;
using System.Threading;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Worker;
using MongoDB.Driver;

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
        private readonly IUnitOfWorkFactory unitOfWorkFactory;
        private readonly List<NodeWorker> workers;
        private readonly object syncRoot = new object();

        private bool isRunning;
        private CancellationTokenSource cts;

        public KronosHost(MongoDatabase db, int workerCount)
        {
            MongoConfigurator.Configure();
            var worknodeId = KronosConfig.WorknodeId;
            var storageFactory = new StorageFactory(db);

            var taskManagementService = new KronosTaskService(storageFactory);

            unitOfWorkFactory = new UnitOfWorkFactory(taskManagementService, storageFactory, KronosConfig.ProcessorFactory);
            workQueue = new WorkQueueProvider(worknodeId, storageFactory, unitOfWorkFactory);
            watchdog = new NodeWatchdog(worknodeId, storageFactory);

            workers = new List<NodeWorker>(workerCount);
            for (int i = 0; i < workerCount; i++)
            {
                workers.Add(new NodeWorker(workQueue));
            }
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
        }
    }
}
