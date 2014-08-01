using System;
using System.Threading;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Worker
{
    public class RetryTaskUnitOfWork : IUnitOfWork
    {
        public int Priority { get; private set; }

        private readonly IKronosTaskService kronosTaskService;
        private readonly TaskRetrySchedule schedule;

        private readonly INodeTaskProcessorFactory processorFactory;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly IFailedTasksStorage failedTasksStorage;

        public RetryTaskUnitOfWork(
            TaskRetrySchedule schedule,
            IKronosTaskService kronosTaskService,            
            INodeTaskProcessorFactory processorFactory,
            IScheduledTasksStorage scheduledTasksStorage,
            IFailedTasksStorage failedTasksStorage)
        {
            Priority = (int)schedule.Task.Priority;
            this.kronosTaskService = kronosTaskService;
            this.schedule = schedule;
            this.processorFactory = processorFactory;
            this.scheduledTasksStorage = scheduledTasksStorage;
            this.failedTasksStorage = failedTasksStorage;
        }

        public void Process(CancellationToken token)
        {
            try
            {
                var processor = processorFactory.GetProcessorFor(schedule.Task);
                processor.Process(schedule.Task, kronosTaskService, token);
            }
            catch (Exception ex)
            {
                failedTasksStorage.Add(new FailedTask(schedule, ex));
                schedule.RetriesCount++;

                if (schedule.CanRetryOnceMore())
                {
                    schedule.Schedule = schedule.Schedule.GetNextSchedule();
                    scheduledTasksStorage.Save(schedule);
                    scheduledTasksStorage.ReleaseLock(schedule);
                    return;
                }
            }

            scheduledTasksStorage.Remove(schedule.Id);
        }

        public void Release()
        {
            scheduledTasksStorage.ReleaseLock(schedule);
        }
    }
}