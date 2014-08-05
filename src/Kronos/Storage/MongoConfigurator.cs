using System;
using System.Reflection;
using Intelli.Kronos.Tasks;
using MongoDB.Bson.Serialization;

namespace Intelli.Kronos.Storage
{
    internal static class MongoConfigurator
    {
        private static bool _isConfigured;

        public static void Configure()
        {
            if (_isConfigured)
            {
                return;
            }

            var task = typeof(KronosTask);
            var schedule = typeof(Schedule);

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in a.GetTypes())
                {
                    if (task.IsAssignableFrom(t) && schedule.IsAssignableFrom(t))
                    {
                        BsonSerializer.LookupSerializer(t);
                    }
                }
            }

            _isConfigured = true;
        }
    }
}
