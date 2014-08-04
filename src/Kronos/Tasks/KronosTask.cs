using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intelli.Kronos.Tasks
{
    public abstract class KronosTask
    {
        private const int MaxRetryCount = 5;

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public TaskPriority Priority { get; set; }

        public FailurePolicy FailurePolicy { get; set; }

        public DateTime DateCreated { get; private set; }

        public TaskState State { get; private set; }

        public WorkerLock Lock { get; private set; }

        protected KronosTask()
        {
            DateCreated = DateTime.UtcNow;
            Priority = TaskPriority.Default;
            Lock = WorkerLock.None;
            FailurePolicy = FailurePolicy.Default;
        }

        internal void ResetState()
        {
            State = TaskState.Pending;
            Lock = WorkerLock.None;
        }
    }
}