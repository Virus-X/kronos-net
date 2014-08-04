using System;
using MongoDB.Bson;

namespace Intelli.Kronos.Tasks
{
    public class TaskRetrySchedule : TaskSchedule
    {
        public const int MaxRetryCount = 5;

        public int RetriesCount { get; set; }

        public TaskRetrySchedule()
        {
        }

        public TaskRetrySchedule(KronosTask task)
            : this(task, new ExponentialSchedule(TimeSpan.FromSeconds(1), 5))
        {
        }

        public TaskRetrySchedule(KronosTask task, Schedule schedule)
        {
            Id = ObjectId.GenerateNewId().ToString();
            Task = task;
            Schedule = schedule;
        }

        public bool CanRetryOnceMore()
        {
            return RetriesCount <= MaxRetryCount;
        }
    }
}