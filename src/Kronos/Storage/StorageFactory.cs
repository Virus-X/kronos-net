using MongoDB.Driver;

namespace Intelli.Kronos.Storage
{
    public interface IStorageFactory
    {
        IFailedTasksStorage GetFailedTasksStorage();
        INodeStateStorage GetNodeStateStorage();
        ITasksStorage GetTasksStorage();
        IScheduledTasksStorage GetScheduledTasksStorage();
    }

    public class StorageFactory : IStorageFactory
    {
        private readonly IFailedTasksStorage failedTasksStorage;
        private readonly INodeStateStorage nodeStateStorage;
        private readonly ITasksStorage tasksStorage;
        private readonly IScheduledTasksStorage scheduledTasksStorage;

        public StorageFactory(IMongoDatabase db)
        {
            failedTasksStorage = new FailedTasksStorage(db);
            nodeStateStorage = new NodeStateStorage(db);
            tasksStorage = new TasksStorage(db);
            scheduledTasksStorage = new ScheduledTasksStorage(db);
        }

        public IFailedTasksStorage GetFailedTasksStorage()
        {
            return failedTasksStorage;
        }

        public INodeStateStorage GetNodeStateStorage()
        {
            return nodeStateStorage;
        }

        public ITasksStorage GetTasksStorage()
        {
            return tasksStorage;
        }

        public IScheduledTasksStorage GetScheduledTasksStorage()
        {
            return scheduledTasksStorage;
        }
    }
}
