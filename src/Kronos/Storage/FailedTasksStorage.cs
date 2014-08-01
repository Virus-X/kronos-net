using Intelli.Kronos.Tasks;
using MongoDB.Driver;

namespace Intelli.Kronos.Storage
{
    public interface IFailedTasksStorage
    {
        void Add(FailedTask task);
    }

    public class FailedTasksStorage : IFailedTasksStorage
    {
        private readonly MongoCollection<FailedTask> failedTasks;

        public FailedTasksStorage(MongoDatabase db)
        {
            failedTasks = db.GetCollection<FailedTask>(KronosConfig.FailedTasksCollection);
        }

        public void Add(FailedTask task)
        {
            failedTasks.Insert(task);
        }
    }
}
