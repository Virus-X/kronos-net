using System;
using System.Collections.Generic;
using System.Linq;
using Intelli.Kronos.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using log4net;

namespace Intelli.Kronos.Storage
{
    public interface IScheduledTasksStorage
    {
        string Save(TaskSchedule taskSchedule);

        TaskSchedule AllocateNext(Guid worknodeId);

        void ReleaseLock(TaskSchedule taskSchedule);

        int ReleaseAllTasks(Guid worknodeId);

        void Reschedule(TaskSchedule taskSchedule, Schedule newSchedule);

        void Remove(string scheduleId);
        int RemapDiscriminator(string oldDiscriminator, string newDiscriminator);
        int CancelAllByDiscriminator(string discriminator);
        TaskSchedule GetById(string scheduleId);
    }

    public class ScheduledTasksStorage : IScheduledTasksStorage
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScheduledTasksStorage));

        private readonly MongoCollection<TaskSchedule> taskCollection;
        private readonly HashSet<string> unknownTypes;

        public ScheduledTasksStorage(MongoDatabase db)
        {
            taskCollection = db.GetCollection<TaskSchedule>(KronosConfig.ScheduledTasksCollection);
            taskCollection.CreateIndex(IndexKeys<TaskSchedule>.Ascending(x => x.Schedule.RunAt));
            unknownTypes = new HashSet<string>();
        }

        public TaskSchedule GetById(string scheduleId)
        {
            var q = Query<TaskSchedule>.EQ(x => x.Id, scheduleId);
            return taskCollection.Find(q).FirstOrDefault();
        }

        public string Save(TaskSchedule taskSchedule)
        {
            taskCollection.Save(taskSchedule);
            return taskSchedule.Id;
        }

        public TaskSchedule AllocateNext(Guid worknodeId)
        {
            var q = Query.And(
                Query<TaskSchedule>.EQ(x => x.Lock.NodeId, Guid.Empty),
                Query<TaskSchedule>.LTE(x => x.Schedule.RunAt, DateTime.UtcNow));

            if (unknownTypes.Count > 0)
            {
                // Ensuring that we won't receive unknown tasks for this node.
                // Performance drops here, so generally, user should ensure that all tasks would be known to node.
                q = Query.And(q, Query.NotIn("Task._t", unknownTypes.Select(BsonValue.Create)));
            }

            var upd = Update<TaskSchedule>.Set(x => x.Lock, WorkerLock.Create(worknodeId));

            var res = taskCollection.FindAndModify(new FindAndModifyArgs
                                                       {
                                                           Query = q,
                                                           Update = upd,
                                                           SortBy = SortBy<TaskSchedule>.Ascending(x => x.Schedule.RunAt),
                                                           VersionReturned = FindAndModifyDocumentVersion.Modified
                                                       });
            try
            {
                var task = res.GetModifiedDocumentAs<TaskSchedule>();
                return task;
            }
            catch (Exception ex)
            {
                var taskId = res.ModifiedDocument.GetValue("_id").AsString;
                Log.ErrorFormat("Failed to deserialize the task {0}: {1}", taskId, ex.Message);
                var taskType = res.ModifiedDocument.GetValue("Task").AsBsonDocument.GetValue("_t").AsString;
                unknownTypes.Add(taskType);
                ReleaseLock(taskId);
                return null;
            }
        }

        public void ReleaseLock(TaskSchedule taskSchedule)
        {
            ReleaseLock(taskSchedule.Id);
        }

        public int ReleaseAllTasks(Guid worknodeId)
        {
            var q = Query<TaskSchedule>.EQ(x => x.Lock.NodeId, worknodeId);
            var upd = Update<TaskSchedule>.Set(x => x.Lock, WorkerLock.None);
            var options = new MongoUpdateOptions { Flags = UpdateFlags.Multi, WriteConcern = WriteConcern.Acknowledged };
            return (int)taskCollection.Update(q, upd, options).DocumentsAffected;
        }

        public void Remove(string scheduleId)
        {
            var q = Query.And(Query<TaskSchedule>.EQ(x => x.Id, scheduleId));
            taskCollection.Remove(q);
        }

        public void Reschedule(TaskSchedule taskSchedule, Schedule newSchedule)
        {
            var q = Query.And(Query<TaskSchedule>.EQ(x => x.Id, taskSchedule.Id));

            if (newSchedule == null)
            {
                taskCollection.Remove(q);
            }
            else
            {
                var upd = Update<TaskSchedule>
                    .Set(x => x.Lock, WorkerLock.None)
                    .Set(x => x.Schedule, newSchedule);
                taskCollection.Update(q, upd);
            }
        }

        public int RemapDiscriminator(string oldDiscriminator, string newDiscriminator)
        {
            var q = Query.And(Query<TaskSchedule>.EQ(x => x.Lock.NodeId, Guid.Empty),
                              Query.EQ("Task._t", oldDiscriminator));

            var upd = Update.Set("Task._t", newDiscriminator);
            var options = new MongoUpdateOptions { Flags = UpdateFlags.Multi, WriteConcern = WriteConcern.Acknowledged };
            return (int)taskCollection.Update(q, upd, options).DocumentsAffected;
        }

        public int CancelAllByDiscriminator(string discriminator)
        {
            var q = Query.And(Query<TaskSchedule>.EQ(x => x.Lock.NodeId, Guid.Empty),
                              Query.EQ("_t", discriminator));

            return (int)taskCollection.Remove(q, RemoveFlags.None, WriteConcern.Acknowledged).DocumentsAffected;
        }

        private void ReleaseLock(string taskId)
        {
            var q = Query.And(Query<TaskSchedule>.EQ(x => x.Id, taskId));
            var upd = Update<TaskSchedule>.Set(x => x.Lock, WorkerLock.None);
            taskCollection.Update(q, upd);
        }
    }
}