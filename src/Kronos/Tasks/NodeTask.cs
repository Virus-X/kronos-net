using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intelli.Kronos.Tasks
{
    public abstract class NodeTask
    {
        private const int MaxRetryCount = 5;

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public TaskPriority Priority { get; set; }

        public DateTime DateCreated { get; private set; }

        public NodeTaskState State { get; private set; }

        public WorkerLock Lock { get; private set; }                

        protected NodeTask()
        {
            DateCreated = DateTime.UtcNow;
            Priority = TaskPriority.Default;
        }

        internal void ResetState()
        {
            State = NodeTaskState.Pending;
            Lock = WorkerLock.None;
        }
    }
}