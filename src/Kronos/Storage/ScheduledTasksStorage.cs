using System;
using Intelli.Kronos.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

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
    }

    public class ScheduledTasksStorage : IScheduledTasksStorage
    {
        private readonly MongoCollection<TaskSchedule> taskCollection;

        public ScheduledTasksStorage(MongoDatabase db)
        {
            taskCollection = db.GetCollection<TaskSchedule>(KronosConfig.ScheduledTasksCollection);
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

            var upd = Update<TaskSchedule>.Set(x => x.Lock, WorkerLock.Create(worknodeId));

            var task = taskCollection.FindAndModify(new FindAndModifyArgs
            {
                Query = q,
                Update = upd,
                SortBy = SortBy<TaskSchedule>.Descending(x => x.Schedule.RunAt),
                VersionReturned = FindAndModifyDocumentVersion.Modified
            }).GetModifiedDocumentAs<TaskSchedule>();

            return task;
        }

        public void ReleaseLock(TaskSchedule taskSchedule)
        {
            var q = Query.And(Query<TaskSchedule>.EQ(x => x.Id, taskSchedule.Id));
            var upd = Update<TaskSchedule>.Set(x => x.Lock, WorkerLock.None);
            taskCollection.Update(q, upd);
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
    }
}