using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Worker
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create(NodeTask task);
        IUnitOfWork Create(TaskSchedule schedule);
    }

    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IKronosTaskService kronosTaskService;
        private readonly ITasksStorage tasksStorage;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly IFailedTasksStorage failedTasksStorage;
        private readonly INodeTaskProcessorFactory taskProcessorFactory;

        public UnitOfWorkFactory(
            IKronosTaskService kronosTaskService,
            IStorageFactory storageFactory,
            INodeTaskProcessorFactory taskProcessorFactory)
        {
            this.kronosTaskService = kronosTaskService;            
            this.taskProcessorFactory = taskProcessorFactory;
            tasksStorage = storageFactory.GetTasksStorage();
            scheduledTasksStorage = storageFactory.GetScheduledTasksStorage();
            failedTasksStorage = storageFactory.GetFailedTasksStorage();
        }

        public UnitOfWorkFactory(
            IKronosTaskService kronosTaskService,
            ITasksStorage tasksStorage,
            IScheduledTasksStorage scheduledTasksStorage,
            IFailedTasksStorage failedTasksStorage,
            INodeTaskProcessorFactory taskProcessorFactory)
        {
            this.kronosTaskService = kronosTaskService;
            this.tasksStorage = tasksStorage;
            this.scheduledTasksStorage = scheduledTasksStorage;
            this.failedTasksStorage = failedTasksStorage;
            this.taskProcessorFactory = taskProcessorFactory;
        }

        public IUnitOfWork Create(NodeTask task)
        {
            return new SimpleTaskUnitOfWork(task, kronosTaskService, taskProcessorFactory, tasksStorage, scheduledTasksStorage, failedTasksStorage);
        }

        public IUnitOfWork Create(TaskSchedule schedule)
        {
            if (schedule is TaskRetrySchedule)
            {
                return new RetryTaskUnitOfWork(schedule as TaskRetrySchedule, kronosTaskService, taskProcessorFactory, scheduledTasksStorage, failedTasksStorage);
            }

            return new ScheduledTaskUnitOfWork(schedule, kronosTaskService, taskProcessorFactory, scheduledTasksStorage, failedTasksStorage);
        }
    }
}
