using System;
using System.Threading;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Worker
{
    public class SimpleTaskUnitOfWork : IUnitOfWork
    {
        private readonly NodeTask task;
        private readonly INodeTaskProcessorFactory processorFactory;

        private readonly IKronosTaskService kronosTaskService;
        private readonly ITasksStorage taskStorage;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly IFailedTasksStorage failedTasksStorage;

        public int Priority { get; private set; }

        public SimpleTaskUnitOfWork(
            NodeTask task,
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
            taskStorage.SetState(task.Id, NodeTaskState.Running);
            try
            {
                processor.Process(task, kronosTaskService, token);
                taskStorage.SetState(task.Id, NodeTaskState.Completed);
            }
            catch (Exception ex)
            {
                failedTasksStorage.Add(new FailedTask(task, ex));
                scheduledTasksStorage.Save(new TaskRetrySchedule(task));
                taskStorage.SetState(task.Id, NodeTaskState.Failed);
            }
        }

        public void Release()
        {
            taskStorage.ReleaseLock(task.Id);
        }
    }
}