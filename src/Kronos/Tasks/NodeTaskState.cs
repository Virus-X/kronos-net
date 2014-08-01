namespace Intelli.Kronos.Tasks
{
    public enum NodeTaskState
    {
        None = 0,
        Pending = 1,
        Running = 2,
        Deferred = 3,
        Completed = 4,
        Failed = 5,
        Canceled = 6
    }
}