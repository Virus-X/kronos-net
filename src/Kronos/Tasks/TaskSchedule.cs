using System;
using MongoDB.Bson;

namespace Intelli.Kronos.Tasks
{
    public class TaskSchedule
    {
        private Schedule schedule;

        public string Id { get; set; }

        public NodeTask Task { get; protected set; }

        public WorkerLock Lock { get; private set; }

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

        public TaskSchedule(NodeTask task, Schedule schedule, string id = null)
        {
            Id = id ?? task.Id ?? ObjectId.GenerateNewId().ToString();
            Task = task;
            Schedule = schedule;
        }

        public Schedule GetNextSchedule()
        {
            return Schedule.GetNextSchedule();
        }
    }
}