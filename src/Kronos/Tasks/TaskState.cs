namespace Intelli.Kronos.Tasks
{
    public enum TaskState
    {
        None = 0,
        Pending = 1,
        Running = 2,
        WaitingForDependency = 3,
        Completed = 4,
        Failed = 5,
        Canceled = 6
    }
}