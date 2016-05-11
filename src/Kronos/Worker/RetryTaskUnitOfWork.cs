using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using log4net;
using System;
using System.Threading;

namespace Intelli.Kronos.Worker
{
    public class RetryTaskUnitOfWork : TaskUnitOfWorkBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScheduledTaskUnitOfWork));

        private readonly TaskRetrySchedule schedule;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly IFailedTasksStorage failedTasksStorage;

        public RetryTaskUnitOfWork(
            TaskRetrySchedule schedule,
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

        public override void Process(CancellationToken token, long timeout)
        {
            try
            {
                ProcessBase(token, timeout);
            }
            catch (OperationCanceledException)
            {
                Log.ErrorFormat("Task {0} was canceled ({1}). Returning back to queue", Task, StopReason);                
                Release();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Task {0} crashed with exception: {1}", schedule.Task, ex);
                schedule.RetriesCount++;
                failedTasksStorage.Add(new FailedTask(schedule, ex));

                if (schedule.CanRetryOnceMore())
                {
                    schedule.Schedule = schedule.Schedule.GetNextSchedule();
                    Log.DebugFormat("Task retry scheduled at {0}", schedule.Schedule.RunAt.ToLocalTime());
                    scheduledTasksStorage.Save(schedule);
                    scheduledTasksStorage.ReleaseLock(schedule);
                    return;
                }

                Log.ErrorFormat("Task {0} failed too many times. Disabled", schedule.Task);
            }

            scheduledTasksStorage.Remove(schedule.Id);
        }

        public override void Release()
        {
            scheduledTasksStorage.ReleaseLock(schedule);
        }
    }
}