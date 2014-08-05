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
    public interface ITasksStorage
    {
        string Add(KronosTask task);

        KronosTask AllocateNext(Guid worknodeId);

        void ReleaseLock(KronosTask task);

        int ReleaseAllTasks(Guid worknodeId);

        void SetState(string taskId, TaskState newState);
        int RemapDiscriminator(string oldDiscriminator, string newDiscriminator);
        int CancelAllByDiscriminator(string discriminator);
    }

    public class TasksStorage : ITasksStorage
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TasksStorage));
        private readonly MongoCollection<KronosTask> tasksCollection;
        private readonly HashSet<string> unknownTypes;

        public TasksStorage(MongoDatabase db)
        {
            tasksCollection = db.GetCollection<KronosTask>(KronosConfig.TasksCollection);
            tasksCollection.CreateIndex(IndexKeys<KronosTask>.Ascending(x => x.Priority));
            unknownTypes = new HashSet<string>();
        }

        public string Add(KronosTask task)
        {
            tasksCollection.Insert(task);
            return task.Id;
        }

        public KronosTask AllocateNext(Guid worknodeId)
        {
            var q = Query.And(Query<KronosTask>.EQ(x => x.Lock.NodeId, Guid.Empty),
                              Query<KronosTask>.EQ(x => x.State, TaskState.Pending));

            if (unknownTypes.Count > 0)
            {
                // Ensuring that we won't receive unknown tasks for this node.
                // Performance drops here, so generally, user should ensure that all tasks would be known to node.
                q = Query.And(q, Query.NotIn("_t", unknownTypes.Select(BsonValue.Create)));
            }

            var upd = Update<KronosTask>.Set(x => x.State, TaskState.Running)
                .Set(x => x.Lock, WorkerLock.Create(worknodeId));

            var args = new FindAndModifyArgs
                           {
                               Query = q,
                               Update = upd,
                               SortBy = SortBy<KronosTask>.Ascending(x => x.Priority),
                               VersionReturned = FindAndModifyDocumentVersion.Modified
                           };

            var res = tasksCollection.FindAndModify(args);
            try
            {
                return res.GetModifiedDocumentAs<KronosTask>();
            }
            catch (BsonSerializationException ex)
            {
                var taskId = res.ModifiedDocument.GetValue("_id").AsObjectId.ToString();
                Log.ErrorFormat("Failed to deserialize the task {0}: {1}", taskId, ex.Message);
                var taskType = res.ModifiedDocument.GetValue("_t").AsString;
                unknownTypes.Add(taskType);
                ReleaseLock(taskId);
                return null;
            }
        }

        public void ReleaseLock(KronosTask task)
        {
            ReleaseLock(task.Id);
        }

        public int ReleaseAllTasks(Guid worknodeId)
        {
            var q = Query.And(Query<KronosTask>.EQ(x => x.Lock.NodeId, worknodeId),
                              Query<KronosTask>.NE(x => x.State, TaskState.Completed));
            var upd = Update<KronosTask>
                .Set(x => x.State, TaskState.Pending)
                .Set(x => x.Lock, WorkerLock.None);
            var options = new MongoUpdateOptions { Flags = UpdateFlags.Multi, WriteConcern = WriteConcern.Acknowledged };
            return (int)tasksCollection.Update(q, upd, options).DocumentsAffected;
        }

        public void SetState(string taskId, TaskState newState)
        {
            var q = Query<KronosTask>.EQ(x => x.Id, taskId);
            var upd = Update<KronosTask>.Set(x => x.State, newState);
            tasksCollection.Update(q, upd, WriteConcern.Acknowledged);
        }

        public int RemapDiscriminator(string oldDiscriminator, string newDiscriminator)
        {
            var q = Query.And(Query<KronosTask>.EQ(x => x.Lock.NodeId, Guid.Empty),
                              Query<KronosTask>.NE(x => x.State, TaskState.Pending),
                              Query.EQ("_t", oldDiscriminator));

            var upd = Update.Set("_t", newDiscriminator);
            var options = new MongoUpdateOptions { Flags = UpdateFlags.Multi, WriteConcern = WriteConcern.Acknowledged };
            return (int)tasksCollection.Update(q, upd, options).DocumentsAffected;
        }

        public int CancelAllByDiscriminator(string discriminator)
        {
            var q = Query.And(Query<KronosTask>.EQ(x => x.Lock.NodeId, Guid.Empty),
                              Query<KronosTask>.NE(x => x.State, TaskState.Pending),
                              Query.EQ("_t", discriminator));

            var upd = Update<KronosTask>.Set(x => x.State, TaskState.Canceled);
            var options = new MongoUpdateOptions { Flags = UpdateFlags.Multi, WriteConcern = WriteConcern.Acknowledged };
            return (int)tasksCollection.Update(q, upd, options).DocumentsAffected;
        }

        private void ReleaseLock(string taskId)
        {
            var q = Query<KronosTask>.EQ(x => x.Id, taskId);
            var upd = Update<KronosTask>.Set(x => x.Lock, WorkerLock.None);
            tasksCollection.Update(q, upd);
        }
    }
}
