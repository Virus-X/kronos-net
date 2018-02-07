using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intelli.Kronos.Tasks
{
    public class WorkerLock
    {
        [BsonElement("n")]
        public ObjectId NodeId { get; set; }

        [BsonElement("t")]
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