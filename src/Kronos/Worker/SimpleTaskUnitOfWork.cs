using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using log4net;
using System;
using System.Threading;

namespace Intelli.Kronos.Worker
{
    public class SimpleTaskUnitOfWork : TaskUnitOfWorkBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SimpleTaskUnitOfWork));

        private readonly ITasksStorage taskStorage;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly IFailedTasksStorage failedTasksStorage;

        public SimpleTaskUnitOfWork(
            KronosTask task,
            IKronosTaskService taskService,
            INodeTaskProcessorFactory processorFactory,
            ITasksStorage taskStorage,
            IScheduledTasksStorage scheduledTasksStorage,
            IFailedTasksStorage failedTasksStorage)
            : base(task, taskService, taskStorage, processorFactory)
        {
            this.taskStorage = taskStorage;
            this.scheduledTasksStorage = scheduledTasksStorage;
            this.failedTasksStorage = failedTasksStorage;
        }

        public override void Process(CancellationToken token, long timeout)
        {
            try
            {
                ProcessBase(token, timeout);
                taskStorage.SetState(Task.Id, TaskState.Completed);
                Log.DebugFormat("Task {0} processed", Task);
            }
            catch (OperationCanceledException)
            {
                Log.ErrorFormat("Task {0} was canceled ({1}). Returning back to queue", Task, StopReason);
                taskStorage.SetState(Task.Id, TaskState.Pending);
                Release();                
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Task {0} crashed with exception: {1}", Task, ex);
                failedTasksStorage.Add(new FailedTask(Task, ex));
                taskStorage.SetState(Task.Id, TaskState.Failed);

                if (Task.FailurePolicy == FailurePolicy.ExponentialRetry)
                {
                    Log.DebugFormat("Task {0} scheduled for retry", Task);
                    var retrySchedule = new TaskRetrySchedule(Task);
                    scheduledTasksStorage.Save(retrySchedule);
                }
            }
        }

        public override void Release()
        {
            taskStorage.ReleaseLock(Task);
        }
    }
}