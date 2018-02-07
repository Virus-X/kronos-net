using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intelli.Kronos.Tasks
{
    [BsonKnownTypes(typeof(TaskRetrySchedule))]
    public class TaskSchedule
    {
        private Schedule schedule;

        public string Id { get; set; }

        [BsonElement("t")]
        public KronosTask Task { get; protected set; }

        [BsonElement("lk")]
        public WorkerLock Lock { get; private set; }

        [BsonElement("sc")]
        public Schedule Schedule
        {
            get { return schedule; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                schedule = value;
            }
        }

        public TaskSchedule()
        {
            Lock = WorkerLock.None;
            Schedule = new OneTimeSchedule();            
        }

        public TaskSchedule(KronosTask task, Schedule schedule, string id = null)
        {
            Id = id ?? task.Id ?? ObjectId.GenerateNewId().ToString();
            Task = task;
            Lock = WorkerLock.None;
            Schedule = schedule;
        }

        public Schedule GetNextSchedule()
        {
            return Schedule.GetNextSchedule();
        }
    }
}