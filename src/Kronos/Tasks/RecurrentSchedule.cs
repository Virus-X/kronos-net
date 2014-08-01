using System;

namespace Intelli.Kronos.Tasks
{
    public class RecurrentSchedule : Schedule
    {
        public TimeSpan Interval { get; set; }

        public override Schedule GetNextSchedule()
        {
            return new RecurrentSchedule(RunAt.Add(Interval), Interval);
        }

        public RecurrentSchedule()
        {
        }

        public RecurrentSchedule(DateTime runAt, TimeSpan interval)
        {
            RunAt = runAt;
            Interval = interval;
        }
    }
}