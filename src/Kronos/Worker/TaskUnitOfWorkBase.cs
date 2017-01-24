using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using log4net;
using System;
using System.Diagnostics;
using System.Threading;

namespace Intelli.Kronos.Worker
{
    public abstract class TaskUnitOfWorkBase : IUnitOfWork
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TaskUnitOfWorkBase));

        public int Priority { get; protected set; }

        public KronosTask Task { get; private set; }

        private readonly IKronosTaskService taskService;
        private readonly ITasksStorage taskStorage;
        private readonly INodeTaskProcessorFactory processorFactory;

        private CancellationTokenSource cancellationTokenSource;
        private volatile TaskStopReason stopReason;
        private DateTime? timeoutAt;

        protected readonly IMetricsCounter metricsCounter;

        public TaskStopReason StopReason
        {
            get { return stopReason; }
        }

        public DateTime? TimeoutAt
        {
            get { return timeoutAt; }
        }

        public abstract void Process(CancellationToken token, long timeout);

        public abstract void Release();

        public void KillTask(TaskStopReason reason)
        {
            Log.WarnFormat("Stopping task with reason: {0}", reason);
            stopReason = reason;
            cancellationTokenSource.Cancel();
        }

        public TaskUnitOfWorkBase(
            KronosTask task,
            IKronosTaskService taskService,
            ITasksStorage taskStorage,
            INodeTaskProcessorFactory processorFactory)
        {
            Task = task;
            Priority = (int)task.Priority;
            this.taskService = taskService;
            this.taskStorage = taskStorage;
            this.processorFactory = processorFactory;
            this.metricsCounter = taskService.MetricsCounter;
        }

        protected void ProcessBase(CancellationToken token, long timeout)
        {
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutAt = DateTime.UtcNow.AddMilliseconds(timeout);

            var processor = processorFactory.GetProcessorFor(Task);
            var sw = Stopwatch.StartNew();

            try
            {
                processor.Process(Task, taskService, cancellationTokenSource.Token);
                metricsCounter.TrackTaskMetrics(Task.GetType().Name, "Processed", sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException ex)
            {
                if (StopReason == TaskStopReason.Timeout)
                {
                    throw new TimeoutException("Task execution timeout", ex);
                }

                metricsCounter.TrackTaskMetrics(Task.GetType().Name, "ExecutionTimeout", sw.ElapsedMilliseconds);
                throw;
            }
            catch
            {
                metricsCounter.TrackTaskMetrics(Task.GetType().Name, "Crashed", sw.ElapsedMilliseconds);
                throw;
            }

            if (Task.ChildTasks != null)
            {
                foreach (var taskId in Task.ChildTasks)
                {
                    var task = taskStorage.MarkDependencyProcessed(taskId, Task.Id);
                    if (task != null && task.State == TaskState.WaitingForDependency && !task.HasDependencies)
                    {
                        Log.DebugFormat("Task {0}: all dependencies completed, switching state to pending", task.Id);
                        taskStorage.SetState(task.Id, TaskState.Pending);
                    }
                }
            }
        }

        public override string ToString()
        {
            return Task.ToString();
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
        }
    }
}
