using System;
using System.Collections.Generic;
using System.Linq;
using Intelli.Kronos.Tasks;
using log4net;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intelli.Kronos.Storage
{
    public interface ITasksStorage
    {
        string Add(KronosTask task);

        void Add(IEnumerable<KronosTask> tasks);

        KronosTask AllocateNext(ObjectId worknodeId);

        void ReleaseLock(KronosTask task);

        int ReleaseLockedTasks(ObjectId worknodeId);

        void SetState(string taskId, TaskState newState);

        int RemapDiscriminator(string oldDiscriminator, string newDiscriminator);

        int CancelAllByDiscriminator(string discriminator);

        KronosTask MarkDependencyProcessed(string taskId, string dependencyId);
    }

    public class TasksStorage : ITasksStorage
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TasksStorage));
        private readonly IMongoCollection<KronosTask> tasksCollection;
        private readonly HashSet<string> unknownTypes;

        public TasksStorage(IMongoDatabase db)
        {
            //if (!db.CollectionExists(KronosConfig.TasksCollection))
            //{
            //    var opts = new CollectionOptionsBuilder();
            //    if (KronosConfig.UseCappedCollection)
            //    {
            //        opts.SetCapped(KronosConfig.UseCappedCollection);
            //        opts.SetMaxSize(KronosConfig.CappedCollectionSize);
            //    }

            //    db.CreateCollection(KronosConfig.TasksCollection, opts);
            //    tasksCollection = db.GetCollection<KronosTask>(KronosConfig.TasksCollection);
            //}
            //else
            //{

            // TODO Capped collection is a great way here, but there are issues with updates
            // We should solve them first
            tasksCollection = db.GetCollection<KronosTask>(KronosConfig.TasksCollection);
            //}

            try
            {
                tasksCollection.Indexes.CreateOne(
                    Builders<KronosTask>.IndexKeys.Ascending(x => x.State)
                        .Ascending(x => x.Priority));
            }
            catch
            {
                // Ok, indexes exist already
            }

            unknownTypes = new HashSet<string>();
        }

        public string Add(KronosTask task)
        {
            tasksCollection.InsertOne(task);
            return task.Id;
        }

        public void Add(IEnumerable<KronosTask> tasks)
        {
            tasksCollection.InsertMany(tasks);
        }

        public KronosTask AllocateNext(ObjectId worknodeId)
        {
            var q = Builders<KronosTask>.Filter.Eq(x => x.State, TaskState.Pending);

            if (unknownTypes.Count > 0)
            {
                // Ensuring that we won't receive unknown tasks for this node.
                // Performance drops here, so generally, user should ensure that all tasks would be known to node.

                q = Builders<KronosTask>.Filter.And(q, Builders<KronosTask>.Filter.Not(
                    Builders<KronosTask>.Filter.In("_t", unknownTypes.Select(BsonValue.Create))));
            }

            var upd = Builders<KronosTask>.Update
                .Set(x => x.State, TaskState.Running)
                .Set(x => x.Lock, WorkerLock.Create(worknodeId));

            //try
            //{
            return tasksCollection.FindOneAndUpdate(q, upd,
                new FindOneAndUpdateOptions<KronosTask, KronosTask>
                {
                    Sort = Builders<KronosTask>.Sort.Ascending(x => x.Priority),
                    ReturnDocument = ReturnDocument.After
                });

            // BUG: unknown entity can break all things
            //}
            //catch (BsonSerializationException ex)
            //{
            //    var taskId = res.ModifiedDocument.GetValue("_id").AsObjectId.ToString();
            //    Log.ErrorFormat("Failed to deserialize the task {0}: {1}", taskId, ex.Message);
            //    var taskType = res.ModifiedDocument.GetValue("_t").AsString;
            //    unknownTypes.Add(taskType);
            //    ReleaseLock(taskId);
            //    return null;
            //}
        }

        public void ReleaseLock(KronosTask task)
        {
            ReleaseLock(task.Id);
        }

        public int ReleaseLockedTasks(ObjectId worknodeId)
        {
            var q = Builders<KronosTask>.Filter.And(
                Builders<KronosTask>.Filter.Eq(x => x.State, TaskState.Running),
                Builders<KronosTask>.Filter.Eq(x => x.Lock.NodeId, worknodeId));

            var upd = Builders<KronosTask>.Update
                .Set(x => x.State, TaskState.Pending)
                .Set(x => x.Lock, WorkerLock.None);

            return (int)tasksCollection.WithWriteConcern(WriteConcern.Acknowledged).UpdateMany(q, upd).ModifiedCount;
        }

        public void SetState(string taskId, TaskState newState)
        {
            var q = Builders<KronosTask>.Filter.Eq(x => x.Id, taskId);
            var upd = Builders<KronosTask>.Update.Set(x => x.State, newState);
            tasksCollection.WithWriteConcern(WriteConcern.Acknowledged).UpdateOne(q, upd);
        }

        public int RemapDiscriminator(string oldDiscriminator, string newDiscriminator)
        {
            if (KronosConfig.UseCappedCollection)
            {
                throw new NotSupportedException("Cannot remap discriminators while using capped collection");
            }

            var q = Builders<KronosTask>.Filter.And(
                Builders<KronosTask>.Filter.Eq(x => x.State, TaskState.Pending),
                Builders<KronosTask>.Filter.Eq("_t", oldDiscriminator));

            var upd = Builders<KronosTask>.Update.Set("_t", newDiscriminator);
            return (int)tasksCollection.WithWriteConcern(WriteConcern.Acknowledged).UpdateMany(q, upd).ModifiedCount;
        }

        public int CancelAllByDiscriminator(string discriminator)
        {
            var q = Builders<KronosTask>.Filter.And(
                Builders<KronosTask>.Filter.Eq(x => x.State, TaskState.Pending),
                Builders<KronosTask>.Filter.Eq("_t", discriminator));

            var upd = Builders<KronosTask>.Update.Set(x => x.State, TaskState.Canceled);
            return (int)tasksCollection.WithWriteConcern(WriteConcern.Acknowledged).UpdateMany(q, upd).ModifiedCount;
        }

        public KronosTask MarkDependencyProcessed(string taskId, string dependencyId)
        {
            var q = Builders<KronosTask>.Filter.And(
                Builders<KronosTask>.Filter.Eq(x => x.Id, taskId),
                Builders<KronosTask>.Filter.Eq(x => x.State, TaskState.WaitingForDependency));

            var upd = Builders<KronosTask>.Update.Pull(x => x.DependsOn, dependencyId);
            return tasksCollection.FindOneAndUpdate(q, upd, new FindOneAndUpdateOptions<KronosTask>
            {
                ReturnDocument = ReturnDocument.After
            });
        }

        private void ReleaseLock(string taskId)
        {
            var q = Builders<KronosTask>.Filter.Eq(x => x.Id, taskId);
            var upd = Builders<KronosTask>.Update.Set(x => x.Lock, WorkerLock.None);
            tasksCollection.WithWriteConcern(WriteConcern.Acknowledged).UpdateOne(q, upd);
        }
    }
}
