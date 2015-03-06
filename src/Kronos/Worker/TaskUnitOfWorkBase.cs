using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using log4net;

namespace Intelli.Kronos.Worker
{
    public abstract class TaskUnitOfWorkBase : IUnitOfWork
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TaskUnitOfWorkBase));

        public int Priority { get; protected set; }
        public KronosTask Task { get; private set; }

        private readonly IKronosTaskService taskService;
        private readonly ITasksStorage taskStorage;
        private readonly INodeTaskProcessorFactory processorFactory;

        public abstract void Process(CancellationToken token);

        public abstract void Release();

        public TaskUnitOfWorkBase(
            KronosTask task,
            IKronosTaskService taskService,
            ITasksStorage taskStorage,
            INodeTaskProcessorFactory processorFactory)
        {
            Task = task;
            Priority = (int)task.Priority;
            this.taskService = taskService;
            this.taskStorage = taskStorage;
            this.processorFactory = processorFactory;
        }

        protected void ProcessBase(CancellationToken token)
        {
            var processor = processorFactory.GetProcessorFor(Task);
            processor.Process(Task, taskService, token);

            if (Task.ChildTasks != null)
            {
                foreach (var taskId in Task.ChildTasks)
                {
                    var task = taskStorage.MarkDependencyProcessed(taskId, Task.Id);
                    if (task != null && task.State == TaskState.WaitingForDependency && !task.HasDependencies)
                    {
                        Log.DebugFormat("Task {0}: all dependencies completed, switching state to pending", task.Id);
                        taskStorage.SetState(task.Id, TaskState.Pending);
                    }
                }
            }
        }
    }
}
