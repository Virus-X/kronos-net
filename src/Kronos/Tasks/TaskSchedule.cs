using System;
using MongoDB.Bson;

namespace Intelli.Kronos.Tasks
{
    public class TaskSchedule
    {
        private Schedule schedule;

        public string Id { get; set; }

        public KronosTask Task { get; protected set; }

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