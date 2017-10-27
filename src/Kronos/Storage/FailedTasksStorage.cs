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
        private readonly IMongoCollection<FailedTask> failedTasks;

        public FailedTasksStorage(IMongoDatabase db)
        {
            failedTasks = db.GetCollection<FailedTask>(KronosConfig.FailedTasksCollection);
        }

        public void Add(FailedTask task)
        {
            failedTasks.InsertOne(task);
        }
    }
}
