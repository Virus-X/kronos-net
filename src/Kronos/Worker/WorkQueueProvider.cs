using Intelli.Kronos.Storage;
using log4net;
using MongoDB.Bson;
using System;
using System.Linq;
using System.Threading;

namespace Intelli.Kronos.Worker
{
    public interface IWorkQueueProvider
    {
        int TasksInQueue { get; }

        void Start(CancellationToken token);

        IUnitOfWork GetNextTask(CancellationToken token);
    }

    /// <summary>
    /// 
    /// </summary>
    public class WorkQueueProvider : IWorkQueueProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WorkQueueProvider));

        private readonly ObjectId worknodeId;
        private readonly ITasksStorage tasksStorage;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly IUnitOfWorkFactory unitOfWorkFactory;

        private readonly PriorityQueue<int, IUnitOfWork> internalQueue;
        private readonly SemaphoreSlim queueSemaphore;
        private readonly AutoResetEvent enqueuedEvent;

        private readonly object syncRoot = new object();
        private bool isRunning;

        public int TasksInQueue
        {
            get
            {
                lock (internalQueue)
                {
                    return internalQueue.Count;
                }
            }
        }

        public WorkQueueProvider(
            ObjectId worknodeId,
            IStorageFactory storageFactory,
            IUnitOfWorkFactory unitOfWorkFactory)
        {
            this.worknodeId = worknodeId;
            tasksStorage = storageFactory.GetTasksStorage();
            scheduledTasksStorage = storageFactory.GetScheduledTasksStorage();

            this.unitOfWorkFactory = unitOfWorkFactory;
            internalQueue = new PriorityQueue<int, IUnitOfWork>();
            enqueuedEvent = new AutoResetEvent(false);
            queueSemaphore = new SemaphoreSlim(KronosConfig.MaxInternalQueueSize);
        }

        public void Start(CancellationToken token)
        {
            lock (syncRoot)
            {
                if (isRunning)
                {
                    throw new InvalidOperationException("Task loader already running");
                }

                var loaderThread = new Thread(LoaderLoop);
                loaderThread.Start(token);
                isRunning = true;
            }
        }

        public IUnitOfWork GetNextTask(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }

                lock (internalQueue)
                {
                    if (!internalQueue.IsEmpty)
                    {
                        var value = internalQueue.DequeueValue();
                        queueSemaphore.Release();
                        return value;
                    }
                }

                enqueuedEvent.WaitOne(500);
            }
        }

        private void AddToQueue(IUnitOfWork unitOfWork)
        {
            lock (internalQueue)
            {
                internalQueue.Enqueue(unitOfWork.Priority, unitOfWork);
            }

            enqueuedEvent.Set();
        }

        private void LoaderLoop(object arg)
        {
            var token = (CancellationToken)arg;
            bool semaphoreTaken = false;
            while (true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    while (!semaphoreTaken)
                    {
                        if (!queueSemaphore.Wait(TimeSpan.FromSeconds(30), token))
                        {
                            lock (internalQueue)
                            {
                                Log.WarnFormat("Waited for queue 30 seconds. Queue size: {0}. Tasks: {1}",
                                    internalQueue.Count, string.Join(",", internalQueue.Select(x => x.Value)));
                            }
                            continue;
                        }

                        semaphoreTaken = true;
                    }

                    var schedule = scheduledTasksStorage.AllocateNext(worknodeId);
                    if (schedule != null)
                    {
                        AddToQueue(unitOfWorkFactory.Create(schedule));
                        semaphoreTaken = false;
                        continue;
                    }

                    var task = tasksStorage.AllocateNext(worknodeId);
                    if (task != null)
                    {
                        AddToQueue(unitOfWorkFactory.Create(task));
                        semaphoreTaken = false;
                        continue;
                    }

                    token.WaitHandle.WaitOne(KronosConfig.QueuePollPeriod);
                }
                catch (OperationCanceledException)
                {
                    ReleaseQueueItems();
                    return;
                }
                catch (ThreadAbortException)
                {
                    ReleaseQueueItems();
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error("Unhandled exception in loader loop. Retry in 10s. ", ex);
                    Thread.Sleep(10000);
                }
            }
        }

        private void ReleaseQueueItems()
        {
            lock (internalQueue)
            {
                while (!internalQueue.IsEmpty)
                {
                    try
                    {
                        internalQueue.DequeueValue().Release();
                    }
                    catch (Exception ex)
                    {
                        // Even if we failed to release this task for some reason (DB connection problem?), it would be released in few minutes by queue cleaner
                        Log.Error("Failed to release an internal queue task. ", ex);
                    }
                }
            }
        }
    }
}
