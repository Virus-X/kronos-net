using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Intelli.Kronos.Tasks
{
    public class RecurrentSchedule : Schedule
    {
        private TimeSpan interval;

        [BsonElement("int")]
        public TimeSpan Interval
        {
            get { return interval; }
            set
            {
                if (value.TotalSeconds < 5)
                {
                    throw new ArgumentException("Timespan should be at least 5 seconds");
                }

                interval = value;
            }
        }

        public override Schedule GetNextSchedule()
        {
            if (interval.TotalSeconds < 5)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var nextDate = RunAt.ToUniversalTime().Add(Interval);

            if (nextDate < now)
            {
                // Next date shouldn't be in past.
                var intervals = now.Subtract(nextDate).TotalSeconds / Interval.TotalSeconds;
                var fullIntervals = Math.Ceiling(intervals);
                nextDate = nextDate.AddSeconds(Interval.TotalSeconds * fullIntervals);
            }

            return new RecurrentSchedule(nextDate, Interval);
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