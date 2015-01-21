using System;
using MongoDB.Bson;

namespace Intelli.Kronos.Tasks
{
    public class WorkerLock
    {
        public ObjectId NodeId { get; set; }

        public DateTime Timeout { get; set; }

        public static WorkerLock None
        {
            get { return new WorkerLock { NodeId = ObjectId.Empty, Timeout = DateTime.MinValue }; }
        }

        public static WorkerLock Create(ObjectId nodeId, DateTime absoluteTimeout)
        {
            return new WorkerLock { NodeId = nodeId, Timeout = absoluteTimeout };
        }

        public static WorkerLock Create(ObjectId nodeId, int timeoutSeconds = 120)
        {
            return new WorkerLock { NodeId = nodeId, Timeout = DateTime.UtcNow.AddSeconds(timeoutSeconds) };
        }
    }
}