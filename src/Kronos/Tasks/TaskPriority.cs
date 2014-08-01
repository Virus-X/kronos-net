namespace Intelli.Kronos.Tasks
{
    /// <summary>
    /// Priority of scheduler task
    /// </summary>
    public enum TaskPriority
    {        
        /// <summary>
        /// Task will be executed as first task in batch
        /// </summary>
        Highest = 0,

        /// <summary>
        /// Task will be executed before most of other tasks
        /// </summary>
        High = 1,

        /// <summary>
        /// Task will be executed normally
        /// </summary>
        Default = 2,

        /// <summary>
        /// Task will be executed after most of other tasks
        /// </summary>
        Low = 3,

        /// <summary>
        /// Task will be executed as last task in batch
        /// </summary>
        Lowest = 4,
    }
}