namespace Intelli.Kronos
{
    /// <summary>
    /// Counts task statistics.
    /// Implementers should support concurrency.
    /// </summary>
    public interface IMetricsCounter
    {
        void TrackTaskMetrics(string taskType, string finalState, long elapsedMilliseconds);
    }

    public class NullMetricsCounter : IMetricsCounter
    {
        public void TrackTaskMetrics(string taskType, string finalState, long elapsedMilliseconds)
        {
        }
    }
}
