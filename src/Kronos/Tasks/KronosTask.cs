using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intelli.Kronos.Tasks
{
    public abstract class KronosTask
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets task priority, relative to other tasks.
        /// </summary>
        [BsonElement("p")]
        public TaskPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets tasks failure policy.
        /// You can specify to cancel task in case of unhandled exception, or to retry with exponential backoff (default).
        /// </summary>
        [BsonElement("fp")]
        public FailurePolicy FailurePolicy { get; set; }

        [BsonElement("d")]
        public DateTime DateCreated { get; private set; }

        /// <summary>
        /// Gets the current state of the task
        /// </summary>
        [BsonElement("s")]
        public TaskState State { get; private set; }

        /// <summary>
        /// Gets the current lock
        /// </summary>
        [BsonElement("lk")]
        public WorkerLock Lock { get; private set; }

        /// <summary>
        /// Gets or sets the list of tasks that should be completed before executing this one.        
        /// </summary>
        [BsonIgnoreIfNull]
        [BsonElement("dep")]
        public Dictionary<string, bool> DependsOn { get; set; }

        /// <summary>
        /// Gets or sets the list of tasks, which are waiting for this task to complete
        /// </summary>
        [BsonIgnoreIfNull]
        [BsonElement("c")]
        public List<string> ChildTasks { get; set; }

        public bool HasDependencies
        {
            get { return DependsOn != null && DependsOn.Any(x => !x.Value); }
        }

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

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Id))
            {
                return GetType().Name;
            }

            return string.Format("{0}[{1}]", GetType().Name, Id);
        }

        public void GenerateId()
        {
            Id = ObjectId.GenerateNewId().ToString();
        }

        public void SetWaitingState()
        {
            State = TaskState.WaitingForDependency;
        }
    }
}