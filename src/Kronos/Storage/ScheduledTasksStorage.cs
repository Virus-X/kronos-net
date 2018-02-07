using System;
using System.Collections.Generic;
using System.Linq;
using Intelli.Kronos.Tasks;
using log4net;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intelli.Kronos.Storage
{
    public interface IScheduledTasksStorage
    {
        string Save(TaskSchedule taskSchedule);

        TaskSchedule AllocateNext(ObjectId worknodeId);

        void ReleaseLock(TaskSchedule taskSchedule);

        int ReleaseAllTasks(ObjectId worknodeId);

        void Reschedule(TaskSchedule taskSchedule, Schedule newSchedule);

        void Remove(string scheduleId);
        int RemapDiscriminator(string oldDiscriminator, string newDiscriminator);
        int CancelAllByDiscriminator(string discriminator);
        TaskSchedule GetById(string scheduleId);
    }

    public class ScheduledTasksStorage : IScheduledTasksStorage
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScheduledTasksStorage));

        private readonly IMongoCollection<TaskSchedule> taskCollection;
        private readonly HashSet<string> unknownTypes;

        public ScheduledTasksStorage(IMongoDatabase db)
        {
            taskCollection = db.GetCollection<TaskSchedule>(KronosConfig.ScheduledTasksCollection);

            try
            {
                taskCollection.Indexes.CreateOne(
                    Builders<TaskSchedule>.IndexKeys.Ascending(x => x.Lock.NodeId).Ascending(x => x.Schedule.RunAt));
            }
            catch
            {
                // whatever
            }

            unknownTypes = new HashSet<string>();
        }

        public TaskSchedule GetById(string scheduleId)
        {
            return taskCollection.Find(x => x.Id == scheduleId).FirstOrDefault();
        }

        public string Save(TaskSchedule taskSchedule)
        {
            taskCollection.ReplaceOne(x => x.Id == taskSchedule.Id, taskSchedule, new UpdateOptions { IsUpsert = true });
            return taskSchedule.Id;
        }

        public TaskSchedule AllocateNext(ObjectId worknodeId)
        {
            var q = Builders<TaskSchedule>.Filter.And(
                Builders<TaskSchedule>.Filter.Eq(x => x.Lock.NodeId, ObjectId.Empty),
                Builders<TaskSchedule>.Filter.Lte(x => x.Schedule.RunAt, DateTime.UtcNow));

            if (unknownTypes.Count > 0)
            {
                // Ensuring that we won't receive unknown tasks for this node.
                // Performance drops here, so generally, user should ensure that all tasks would be known to node.
                q = Builders<TaskSchedule>.Filter.And(q, Builders<TaskSchedule>.Filter.Not(Builders<TaskSchedule>.Filter.In("t._t", unknownTypes.Select(BsonValue.Create))));
            }

            var upd = Builders<TaskSchedule>.Update.Set(x => x.Lock, WorkerLock.Create(worknodeId));

            //var res = taskCollection.FindAndModify(new FindAndModifyArgs
            //{
            //    Query = q,
            //    Update = upd,
            //    SortBy = SortBy<TaskSchedule>.Ascending(x => x.Schedule.RunAt),
            //    VersionReturned = FindAndModifyDocumentVersion.Modified
            //});
            //try
            //{
            return taskCollection.FindOneAndUpdate(q, upd, new FindOneAndUpdateOptions<TaskSchedule, TaskSchedule>
            {
                Sort = Builders<TaskSchedule>.Sort.Ascending(x => x.Schedule.RunAt)
            });
            //    var task = res.GetModifiedDocumentAs<TaskSchedule>();
            //    return task;
            //}
            //catch (Exception ex)
            //{
            //    var taskId = res.ModifiedDocument.GetValue("_id").AsString;
            //    Log.ErrorFormat("Failed to deserialize the task {0}: {1}", taskId, ex.Message);
            //    var taskType = res.ModifiedDocument.GetValue("Task").AsBsonDocument.GetValue("_t").AsString;
            //    unknownTypes.Add(taskType);
            //    ReleaseLock(taskId);
            //    return null;
            //}
        }

        public void ReleaseLock(TaskSchedule taskSchedule)
        {
            ReleaseLock(taskSchedule.Id);
        }

        public int ReleaseAllTasks(ObjectId worknodeId)
        {
            var q = Builders<TaskSchedule>.Filter.Eq(x => x.Lock.NodeId, worknodeId);
            var upd = Builders<TaskSchedule>.Update.Set(x => x.Lock, WorkerLock.None);
            return (int)taskCollection.WithWriteConcern(WriteConcern.Acknowledged).UpdateMany(q, upd).ModifiedCount;
        }

        public void Remove(string scheduleId)
        {
            taskCollection.DeleteOne(x => x.Id == scheduleId);
        }

        public void Reschedule(TaskSchedule taskSchedule, Schedule newSchedule)
        {
            if (newSchedule == null)
            {
                taskCollection.DeleteOne(x => x.Id == taskSchedule.Id);
            }
            else
            {
                var upd = Builders<TaskSchedule>.Update
                    .Set(x => x.Lock, WorkerLock.None)
                    .Set(x => x.Schedule, newSchedule);
                taskCollection.UpdateOne(x => x.Id == taskSchedule.Id, upd);
            }
        }

        public int RemapDiscriminator(string oldDiscriminator, string newDiscriminator)
        {
            var q = Builders<TaskSchedule>.Filter.And(
                Builders<TaskSchedule>.Filter.Eq(x => x.Lock.NodeId, ObjectId.Empty),
                Builders<TaskSchedule>.Filter.Eq("t._t", oldDiscriminator));

            var upd = Builders<TaskSchedule>.Update.Set("t._t", newDiscriminator);
            return (int)taskCollection.WithWriteConcern(WriteConcern.Acknowledged).UpdateMany(q, upd).ModifiedCount;
        }

        public int CancelAllByDiscriminator(string discriminator)
        {
            var q = Builders<TaskSchedule>.Filter.And(
                Builders<TaskSchedule>.Filter.Eq(x => x.Lock.NodeId, ObjectId.Empty),
                Builders<TaskSchedule>.Filter.Eq("t._t", discriminator));

            return (int)taskCollection.WithWriteConcern(WriteConcern.Acknowledged).DeleteMany(q).DeletedCount;
        }

        private void ReleaseLock(string taskId)
        {
            taskCollection.UpdateOne(x => x.Id == taskId,
                Builders<TaskSchedule>.Update.Set(x => x.Lock, WorkerLock.None));
        }
    }
}