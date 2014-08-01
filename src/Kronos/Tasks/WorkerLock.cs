using System;

namespace Intelli.Kronos.Tasks
{
    public class WorkerLock
    {
        public Guid NodeId { get; set; }

        public DateTime Timeout { get; set; }

        public static WorkerLock None
        {
            get { return new WorkerLock { NodeId = Guid.Empty, Timeout = DateTime.MinValue }; }
        }

        public static WorkerLock Create(Guid nodeId, DateTime absoluteTimeout)
        {
            return new WorkerLock { NodeId = nodeId, Timeout = absoluteTimeout };
        }

        public static WorkerLock Create(Guid nodeId, int timeoutSeconds = 120)
        {
            return new WorkerLock { NodeId = nodeId, Timeout = DateTime.UtcNow.AddSeconds(timeoutSeconds) };
        }
    }
}