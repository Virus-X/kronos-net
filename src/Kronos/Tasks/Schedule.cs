using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Intelli.Kronos.Tasks
{
    public abstract class Schedule
    {
        [BsonElement("at")]
        public DateTime RunAt { get; set; }

        public abstract Schedule GetNextSchedule();

        protected Schedule()
        {
            RunAt = DateTime.UtcNow;
        }
    }
}
