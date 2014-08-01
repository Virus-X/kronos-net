using System;
using System.Threading;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Processors
{
    public abstract class NodeTaskProcessor<T> : INodeTaskProcessor<T>
        where T : NodeTask
    {
        public Type TaskType
        {
            get { return typeof(T); }
        }

        public void Process(NodeTask task, IKronosTaskService kronosTaskService, CancellationToken token)
        {
            if (task is T)
            {
                Process(task as T, kronosTaskService, token);
            }
            else
            {
                throw new ArgumentException("Invalid task specified");
            }
        }

        public abstract void Process(T task, IKronosTaskService kronosTaskService, CancellationToken token);
    }
}
