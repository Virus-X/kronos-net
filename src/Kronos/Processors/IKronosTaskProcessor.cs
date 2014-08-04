using System;
using System.Threading;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Processors
{
    public interface IKronosTaskProcessor<in T> : IKronosTaskProcessor
        where T : KronosTask
    {
        void Process(T task, IKronosTaskService taskService, CancellationToken token);
    }

    public interface IKronosTaskProcessor : IDisposable
    {
        Type TaskType { get; }

        void Process(KronosTask task, IKronosTaskService taskService, CancellationToken token);
    }
}