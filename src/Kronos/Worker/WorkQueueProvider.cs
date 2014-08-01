﻿using System;
using System.Threading;
using Intelli.Kronos.Storage;
using log4net;

namespace Intelli.Kronos.Worker
{
    public interface IWorkQueueProvider
    {
        int TasksInQueue { get; }

        void Start(CancellationToken token);

        IUnitOfWork GetNextTask(CancellationToken token);
    }

    public class WorkQueueProvider : IWorkQueueProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WorkQueueProvider));

        private readonly Guid worknodeId;
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
            Guid worknodeId,
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
                token.ThrowIfCancellationRequested();

                lock (internalQueue)
                {
                    if (!internalQueue.IsEmpty)
                    {
                        var value = internalQueue.DequeueValue();
                        queueSemaphore.Release();
                        return value;
                    }
                }

                enqueuedEvent.WaitOne();
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
            while (true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    queueSemaphore.Wait();
                    var schedule = scheduledTasksStorage.AllocateNext(worknodeId);
                    if (schedule != null)
                    {
                        AddToQueue(unitOfWorkFactory.Create(schedule));
                        continue;
                    }

                    var task = tasksStorage.AllocateNext(worknodeId);
                    if (task != null)
                    {
                        AddToQueue(unitOfWorkFactory.Create(task));
                        continue;
                    }

                    token.ThrowIfCancellationRequested();
                    Thread.Sleep(KronosConfig.QueuePollPeriod);
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