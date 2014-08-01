using System;
using System.Net;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos
{
    public static class KronosConfig
    {
        public static string TasksCollection = "Kronos.Tasks";
        public static string ScheduledTasksCollection = "Kronos.ScheduledTasks";
        public static string FailedTasksCollection = "Kronos.FailedTasks";
        public static string NodeStateCollection = "Kronos.Nodes";

        public static int MaxInternalQueueSize = 5;

        public static int DeadNodeSeconds = 60;

        public static int QueuePollPeriod = 1000;

        /// <summary>
        /// Unique id for each node process. Generated on node start.
        /// </summary>
        public static readonly Guid WorknodeId = Guid.NewGuid();

        internal static readonly NodeTaskProcessorFactory ProcessorFactory = new NodeTaskProcessorFactory();        

        public static void RegisterProcessor<T>(INodeTaskProcessor<T> processor)
            where T : NodeTask
        {
            ProcessorFactory.RegisterProcessor<T>(processor);
        }

        public static void ClearProcessors()
        {
            ProcessorFactory.ClearProcessors();
        }        
    }
}
