using System;

namespace Intelli.Kronos.Tasks
{
    public abstract class Schedule
    {
        public DateTime RunAt { get; set; }

        public abstract Schedule GetNextSchedule();

        protected Schedule()
        {
            RunAt = DateTime.UtcNow;
        }
    }
}
