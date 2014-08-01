using System;
using System.Threading;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Worker
{
    public class ScheduledTaskUnitOfWork : IUnitOfWork
    {
        private readonly TaskSchedule schedule;
        private readonly IKronosTaskService kronosTaskService;
        private readonly INodeTaskProcessorFactory processorFactory;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly IFailedTasksStorage failedTasksStorage;

        public int Priority { get; private set; }

        public ScheduledTaskUnitOfWork(
            TaskSchedule schedule,
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
                var retrySchedule = new TaskRetrySchedule(schedule.Task);
                scheduledTasksStorage.Save(retrySchedule);
            }
            finally
            {
                // If nextSchedule is null, task would be removed
                scheduledTasksStorage.Reschedule(schedule, schedule.GetNextSchedule());
            }
        }

        public void Release()
        {
            scheduledTasksStorage.ReleaseLock(schedule);
        }
    }
}