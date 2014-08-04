using System;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using MongoDB.Driver;

namespace Intelli.Kronos
{
    public class KronosTaskService : IKronosTaskService
    {
        private readonly ITasksStorage tasksStorage;
        private readonly IScheduledTasksStorage scheduledTasksStorage;

        public KronosTaskService(MongoDatabase db)
            : this(new StorageFactory(db))
        {
        }

        public KronosTaskService(IStorageFactory storageFactory)
        {
            tasksStorage = storageFactory.GetTasksStorage();
            scheduledTasksStorage = storageFactory.GetScheduledTasksStorage();
        }

        public string AddTask(KronosTask task)
        {
            task.Id = null;
            task.ResetState();
            return tasksStorage.Add(task);
        }

        public string ScheduleTask(KronosTask task, DateTime startAt)
        {
            var scheduledTask = new TaskSchedule(task, new OneTimeSchedule(startAt));
            return scheduledTasksStorage.Save(scheduledTask);
        }

        public string ScheduleTask(KronosTask task, DateTime startAt, TimeSpan interval, string scheduleId = null)
        {
            var scheduledTask = new TaskSchedule(task, new RecurrentSchedule(startAt, interval), scheduleId);
            return scheduledTasksStorage.Save(scheduledTask);
        }

        public void RemoveTask(string taskId)
        {
            tasksStorage.SetState(taskId, TaskState.Canceled);
        }

        public void UnscheduleTask(string scheduleId)
        {
            scheduledTasksStorage.Remove(scheduleId);
        }
    }
}
