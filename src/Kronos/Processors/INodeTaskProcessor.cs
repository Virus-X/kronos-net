using System;
using System.Threading;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Processors
{
    public interface INodeTaskProcessor<in T> : INodeTaskProcessor
        where T : NodeTask
    {
        void Process(T task, IKronosTaskService kronosTaskService, CancellationToken token);
    }

    public interface INodeTaskProcessor
    {
        Type TaskType { get; }

        void Process(NodeTask task, IKronosTaskService kronosTaskService, CancellationToken token);
    }
}