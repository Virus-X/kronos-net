using System;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Tasks;
using MongoDB.Bson;

namespace Intelli.Kronos
{
    public static class KronosConfig
    {
        private static INodeTaskProcessorFactory _processorFactory;

        public static string TasksCollection = "Kronos.Tasks";
        public static string ScheduledTasksCollection = "Kronos.ScheduledTasks";
        public static string FailedTasksCollection = "Kronos.FailedTasks";
        public static string NodeStateCollection = "Kronos.WorkNodes";

        public static int MaxInternalQueueSize = 5;

        public static int DeadNodeSeconds = 60;

        public static int QueuePollPeriod = 1000;

        public static int TaskTimeoutSeconds = 5 * 60; // 5 min

        public static bool UseCappedCollection = true;
        public static long CappedCollectionSize = 1024 * 1024 * 1024; // 1GB

        /// <summary>
        /// Unique id for each node process. Generated on node start.
        /// </summary>
        public static readonly ObjectId WorknodeId = ObjectId.GenerateNewId();

        static KronosConfig()
        {
            _processorFactory = new NodeTaskProcessorFactory();
        }

        public static INodeTaskProcessorFactory ProcessorFactory
        {
            get { return _processorFactory; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _processorFactory = value;
            }
        }
    }
}
