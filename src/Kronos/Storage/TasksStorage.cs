using System;
using Intelli.Kronos.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Intelli.Kronos.Storage
{
    public interface ITasksStorage
    {
        string Add(KronosTask task);

        KronosTask AllocateNext(Guid worknodeId);

        void ReleaseLock(string taskId);

        int ReleaseAllTasks(Guid worknodeId);

        void SetState(string taskId, TaskState newState);
    }

    public class TasksStorage : ITasksStorage
    {
        private readonly MongoCollection<KronosTask> tasksCollection;

        public TasksStorage(MongoDatabase db)
        {
            tasksCollection = db.GetCollection<KronosTask>(KronosConfig.TasksCollection);
            tasksCollection.CreateIndex(IndexKeys<KronosTask>.Ascending(x => x.Priority));
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

            var upd = Update<KronosTask>.Set(x => x.State, TaskState.Running)
                .Set(x => x.Lock, WorkerLock.Create(worknodeId));

            var args = new FindAndModifyArgs
                           {
                               Query = q,
                               Update = upd,
                               SortBy = SortBy<KronosTask>.Ascending(x => x.Priority),
                               VersionReturned = FindAndModifyDocumentVersion.Modified
                           };

            return tasksCollection.FindAndModify(args).GetModifiedDocumentAs<KronosTask>();
        }

        public void ReleaseLock(string taskId)
        {
            var q = Query<KronosTask>.EQ(x => x.Id, taskId);
            var upd = Update<KronosTask>.Set(x => x.Lock, WorkerLock.None);
            tasksCollection.Update(q, upd);
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
    }
}
