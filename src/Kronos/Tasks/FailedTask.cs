using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intelli.Kronos.Tasks
{
    public class FailedTask
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string TaskId { get { return Task.Id; } }

        public string ScheduleId { get; set; }

        public NodeTask Task { get; set; }

        public DateTime Timestamp { get; set; }

        public string ErrorMessage { get; set; }

        public string StackTrace { get; set; }

        public FailedTask()
        {
        }

        public FailedTask(NodeTask task, Exception ex)
        {
            Task = task;
            ErrorMessage = ex.Message;
            StackTrace = ex.ToString();
            Timestamp = DateTime.UtcNow;
        }

        public FailedTask(TaskSchedule scheduled, Exception ex)
        {
            ScheduleId = scheduled.Id;
            Task = scheduled.Task;
            ErrorMessage = ex.Message;
            StackTrace = ex.ToString();
            Timestamp = DateTime.UtcNow;
        }
    }
}