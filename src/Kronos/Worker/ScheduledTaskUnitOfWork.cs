using System;
using System.Threading;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using log4net;

namespace Intelli.Kronos.Worker
{
    public class ScheduledTaskUnitOfWork : TaskUnitOfWorkBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScheduledTaskUnitOfWork));

        private readonly TaskSchedule schedule;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly IFailedTasksStorage failedTasksStorage;

        public ScheduledTaskUnitOfWork(
            TaskSchedule schedule,
            IKronosTaskService taskService,
            ITasksStorage taskStorage,
            INodeTaskProcessorFactory processorFactory,
            IScheduledTasksStorage scheduledTasksStorage,
            IFailedTasksStorage failedTasksStorage)
            : base(schedule.Task, taskService, taskStorage, processorFactory)
        {
            this.schedule = schedule;
            this.scheduledTasksStorage = scheduledTasksStorage;
            this.failedTasksStorage = failedTasksStorage;
        }

        public override void Process(CancellationToken token)
        {
            try
            {
                ProcessBase(token);
                Log.DebugFormat("Schedule {0} processed", schedule.Id);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Task {0} crashed with exception: {1}", schedule.Id, ex);
                failedTasksStorage.Add(new FailedTask(schedule, ex));

                if (schedule.Task.FailurePolicy == FailurePolicy.ExponentialRetry)
                {
                    Log.Debug("Task scheduled for retry");
                    var retrySchedule = new TaskRetrySchedule(schedule.Task);
                    scheduledTasksStorage.Save(retrySchedule);
                }
            }
            finally
            {
                // If nextSchedule is null, task would be removed
                scheduledTasksStorage.Reschedule(schedule, schedule.GetNextSchedule());
            }
        }

        public override void Release()
        {
            scheduledTasksStorage.ReleaseLock(schedule);
        }
    }
}