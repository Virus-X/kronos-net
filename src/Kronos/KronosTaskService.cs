using System;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using MongoDB.Driver;

namespace Intelli.Kronos
{
    public class KronosTaskService : IKronosTaskService
    {
        private readonly IMetricsCounter metricsCounter;
        private readonly ITasksStorage tasksStorage;
        private readonly IScheduledTasksStorage scheduledTasksStorage;

        public IMetricsCounter MetricsCounter
        {
            get { return metricsCounter; }
        }

        public KronosTaskService(IMongoDatabase db, IMetricsCounter metricsCounter = null)
            : this(new StorageFactory(db), metricsCounter)
        {
        }

        public KronosTaskService(IStorageFactory storageFactory, IMetricsCounter metricsCounter = null)
        {
            this.metricsCounter = metricsCounter ?? new NullMetricsCounter();
            tasksStorage = storageFactory.GetTasksStorage();
            scheduledTasksStorage = storageFactory.GetScheduledTasksStorage();
        }

        public string AddTask(KronosTask task)
        {
            task.Id = null;
            task.ResetState();
            return tasksStorage.Add(task);
        }

        public void AddDagTasks(DagBuilder dag)
        {
            var tasks = dag.CreateDag();
            tasksStorage.Add(tasks);
        }

        public string ScheduleTask(KronosTask task, DateTime startAt)
        {
            var scheduledTask = new TaskSchedule(task, new OneTimeSchedule(startAt));
            return scheduledTasksStorage.Save(scheduledTask);
        }

        public void EnsureTaskScheduled(string scheduleId, Func<KronosTask> taskGenerator, DateTime startAt, TimeSpan interval)
        {
            var scheduledTask = scheduledTasksStorage.GetById(scheduleId);
            if (scheduledTask == null)
            {
                ScheduleTask(taskGenerator(), startAt, interval, scheduleId);
            }
            else
            {
                var schedule = scheduledTask.Schedule as RecurrentSchedule;
                if (schedule == null || schedule.Interval != interval)
                {
                    scheduledTasksStorage.Reschedule(scheduledTask, new RecurrentSchedule(startAt, interval));
                }
            }
        }

        public string ScheduleTask(KronosTask task, DateTime startAt, TimeSpan interval, string scheduleId = null)
        {
            var scheduledTask = new TaskSchedule(task, new RecurrentSchedule(startAt, interval), scheduleId);
            return scheduledTasksStorage.Save(scheduledTask);
        }

        public void CancelTask(string taskId)
        {
            tasksStorage.SetState(taskId, TaskState.Canceled);
        }

        public void UnscheduleTask(string scheduleId)
        {
            scheduledTasksStorage.Remove(scheduleId);
        }

        public int RemapTaskDiscriminator(string oldDiscriminator, string newDiscriminator)
        {
            var count = tasksStorage.RemapDiscriminator(oldDiscriminator, newDiscriminator);
            count += scheduledTasksStorage.RemapDiscriminator(oldDiscriminator, newDiscriminator);
            return count;
        }

        public int CancelAllByDiscriminator(string discriminator)
        {
            var count = tasksStorage.CancelAllByDiscriminator(discriminator);
            count += scheduledTasksStorage.CancelAllByDiscriminator(discriminator);
            return count;
        }
    }
}
