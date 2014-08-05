using System;
using System.Threading;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using log4net;

namespace Intelli.Kronos.Worker
{
    public class SimpleTaskUnitOfWork : IUnitOfWork
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SimpleTaskUnitOfWork));

        private readonly KronosTask task;
        private readonly INodeTaskProcessorFactory processorFactory;

        private readonly IKronosTaskService kronosTaskService;
        private readonly ITasksStorage taskStorage;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly IFailedTasksStorage failedTasksStorage;

        public int Priority { get; private set; }

        public SimpleTaskUnitOfWork(
            KronosTask task,
            IKronosTaskService kronosTaskService,
            INodeTaskProcessorFactory processorFactory,
            ITasksStorage taskStorage,
            IScheduledTasksStorage scheduledTasksStorage,
            IFailedTasksStorage failedTasksStorage)
        {
            Priority = (int)task.Priority;
            this.task = task;
            this.kronosTaskService = kronosTaskService;
            this.processorFactory = processorFactory;
            this.taskStorage = taskStorage;
            this.scheduledTasksStorage = scheduledTasksStorage;
            this.failedTasksStorage = failedTasksStorage;
        }

        public void Process(CancellationToken token)
        {
            var processor = processorFactory.GetProcessorFor(task);
            taskStorage.SetState(task.Id, TaskState.Running);
            try
            {
                processor.Process(task, kronosTaskService, token);
                taskStorage.SetState(task.Id, TaskState.Completed);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Task {0} crashed with exception: {1}", task.Id, ex);
                failedTasksStorage.Add(new FailedTask(task, ex));
                taskStorage.SetState(task.Id, TaskState.Failed);

                if (task.FailurePolicy == FailurePolicy.ExponentialRetry)
                {
                    Log.Debug("Task scheduled for retry");
                    var retrySchedule = new TaskRetrySchedule(task);
                    scheduledTasksStorage.Save(retrySchedule);
                }                
            }
        }

        public void Release()
        {
            taskStorage.ReleaseLock(task);
        }
    }
}