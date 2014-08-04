using System;
using System.Threading;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Processors
{
    public abstract class KronosTaskProcessor<T> : IKronosTaskProcessor<T>
        where T : KronosTask
    {
        public Type TaskType
        {
            get { return typeof(T); }
        }

        public void Process(KronosTask task, IKronosTaskService taskService, CancellationToken token)
        {
            if (task is T)
            {
                Process(task as T, taskService, token);
            }
            else
            {
                throw new ArgumentException("Invalid task specified");
            }
        }

        public abstract void Process(T task, IKronosTaskService taskService, CancellationToken token);

        public virtual void Dispose()
        {
        }
    }
}
