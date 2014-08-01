using System;

namespace Intelli.Kronos.Tasks
{
    public class OneTimeSchedule : Schedule
    {
        public override Schedule GetNextSchedule()
        {
            return null;
        }

        public OneTimeSchedule()
        {
        }

        public OneTimeSchedule(DateTime runAt)
        {
            RunAt = runAt;
        }

        public OneTimeSchedule(TimeSpan fromNow)
        {
            RunAt = DateTime.UtcNow.Add(fromNow);
        }
    }
}