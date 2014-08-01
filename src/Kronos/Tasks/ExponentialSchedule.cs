using System;

namespace Intelli.Kronos.Tasks
{
    public class ExponentialSchedule : Schedule
    {
        public double Multiplier { get; set; }

        public TimeSpan Interval { get; set; }

        public ExponentialSchedule()
        {
        }

        public ExponentialSchedule(TimeSpan initialInterval, double multiplier)
        {
            RunAt = DateTime.UtcNow.Add(initialInterval);
            Multiplier = multiplier;
            Interval = Multiply(initialInterval, multiplier);
        }

        public override Schedule GetNextSchedule()
        {
            return new ExponentialSchedule
                       {
                           RunAt = RunAt.Add(Interval),
                           Interval = Multiply(Interval, Multiplier),
                           Multiplier = Multiplier
                       };
        }

        private static TimeSpan Multiply(TimeSpan timeSpan, double multiplier)
        {
            return TimeSpan.FromMinutes(timeSpan.TotalSeconds * multiplier);
        }
    }
}