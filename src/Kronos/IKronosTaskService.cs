using System;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos
{
    public interface IKronosTaskService
    {
        /// <summary>
        /// Adds specified task to worker queue.
        /// </summary>
        /// <param name="task">Task to add</param>
        /// <returns>Id of added task</returns>
        string AddTask(KronosTask task);

        /// <summary>
        /// Schedules to run the task at specified time.        
        /// </summary>
        /// <param name="task">Task to schedule</param>
        /// <param name="startAt">Time to start scheduled task at.</param>
        /// <returns>Id of task schedule</returns>
        string ScheduleTask(KronosTask task, DateTime startAt);

        /// <summary>
        /// Schedules to run the task periodically, starting from specified time.        
        /// </summary>        
        /// <param name="task">Task to schedule</param>
        /// <param name="startAt">Time to start scheduled task at.</param>
        /// <param name="interval">Interval between instances of scheduled task. Cannot be less than 5 seconds.</param>
        /// <param name="scheduleId">Optional id for task schedule. It would be assigned to this schedule and override any existing schedule with same id.</param>
        /// <returns>Id of task schedule</returns>
        string ScheduleTask(KronosTask task, DateTime startAt, TimeSpan interval, string scheduleId = null);

        /// <summary>
        /// Cancels specified task.
        /// Task can only be cancelled if it didn't start execution.
        /// </summary>
        /// <param name="taskId">Id of a task to remove</param>
        /// <returns>True if task was removed, false if it was not found or already locked for execution</returns>
        void CancelTask(string taskId);

        /// <summary>
        /// Cancels schedule for a task.
        /// </summary>
        /// <param name="scheduleId">Id of schedule to cancel</param>
        /// <returns>True if schedule was canceled, false if no schedule with specified id found.</returns>
        void UnscheduleTask(string scheduleId);

        /// <summary>
        /// Updates discriminator (_t value in MongoDb) for all tasks that have matching old one.
        /// Useful during deploys.
        /// </summary>
        /// <param name="oldDiscriminator">Old discriminator to update</param>
        /// <param name="newDiscriminator">New value of discriminator</param>
        /// <returns>Count of tasks processed</returns>
        int RemapTaskDiscriminator(string oldDiscriminator, string newDiscriminator);

        /// <summary>
        /// Cancels all tasks with specified discriminator (_t value in MongoDb).
        /// </summary>
        /// <param name="discriminator">Task discriminator</param>        
        /// <returns>Count of tasks processed</returns>
        int CancelAllByDiscriminator(string discriminator);

        /// <summary>
        /// Checks whether task with specified id exists and ensures that it has specified schedule.
        /// Creates new task schedule otherwise.
        /// </summary>
        /// <param name="scheduleId">Id of scheduled task</param>
        /// <param name="taskFactory">Factory method, that creates task if it not exists.</param>
        /// <param name="startAt">Time to start task at.</param>
        /// <param name="interval">Interval between instances of scheduled task.</param>
        void EnsureTaskScheduled(string scheduleId, Func<KronosTask> taskFactory, DateTime startAt, TimeSpan interval);

        /// <summary>
        /// Adds multiple tasks with dependencies to each other.
        /// </summary>
        /// <param name="dag">Tasks dependency graph</param>
        void AddDagTasks(DagBuilder dag);
    }
}