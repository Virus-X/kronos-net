using System;
using Intelli.Kronos.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Intelli.Kronos.Storage
{
    public interface ITasksStorage
    {
        string Add(NodeTask task);

        NodeTask AllocateNext(Guid worknodeId);

        void ReleaseLock(string taskId);

        int ReleaseAllTasks(Guid worknodeId);

        void SetState(string taskId, NodeTaskState newState);
    }

    public class TasksStorage : ITasksStorage
    {
        private readonly MongoCollection<NodeTask> tasksCollection;

        public TasksStorage(MongoDatabase db)
        {
            tasksCollection = db.GetCollection<NodeTask>(KronosConfig.TasksCollection);
        }

        public string Add(NodeTask task)
        {
            tasksCollection.Insert(task);
            return task.Id;
        }

        public NodeTask AllocateNext(Guid worknodeId)
        {
            var q = Query.And(Query<NodeTask>.EQ(x => x.Lock.NodeId, Guid.Empty),
                              Query<NodeTask>.EQ(x => x.State, NodeTaskState.Pending));

            var upd = Update<NodeTask>.Set(x => x.State, NodeTaskState.Running)
                .Set(x => x.Lock, WorkerLock.Create(worknodeId));

            var args = new FindAndModifyArgs
                           {
                               Query = q,
                               Update = upd,
                               SortBy = SortBy<NodeTask>.Ascending(x => x.Priority),
                               VersionReturned = FindAndModifyDocumentVersion.Modified
                           };

            return tasksCollection.FindAndModify(args).GetModifiedDocumentAs<NodeTask>();
        }

        public void ReleaseLock(string taskId)
        {
            var q = Query<NodeTask>.EQ(x => x.Id, taskId);
            var upd = Update<NodeTask>.Set(x => x.Lock, WorkerLock.None);
            tasksCollection.Update(q, upd);
        }

        public int ReleaseAllTasks(Guid worknodeId)
        {
            var q = Query.And(Query<NodeTask>.EQ(x => x.Lock.NodeId, worknodeId),
                              Query<NodeTask>.NE(x => x.State, NodeTaskState.Completed));
            var upd = Update<NodeTask>
                .Set(x => x.State, NodeTaskState.Pending)
                .Set(x => x.Lock, WorkerLock.None);
            var options = new MongoUpdateOptions { Flags = UpdateFlags.Multi, WriteConcern = WriteConcern.Acknowledged };
            return (int)tasksCollection.Update(q, upd, options).DocumentsAffected;
        }

        public void SetState(string taskId, NodeTaskState newState)
        {
            var q = Query<NodeTask>.EQ(x => x.Id, taskId);
            var upd = Update<NodeTask>.Set(x => x.State, newState);
            tasksCollection.Update(q, upd, WriteConcern.Acknowledged);
        }
    }
}
