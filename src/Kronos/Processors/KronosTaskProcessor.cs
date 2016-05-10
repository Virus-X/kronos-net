using Intelli.Kronos.Tasks;
using System;
using System.Threading;

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
            var t = task as T;
            if (t == null)
            {
                throw new ArgumentException("Invalid task specified");
            }

            Process(t, taskService, token);
        }

        public abstract void Process(T task, IKronosTaskService taskService, CancellationToken token);

        public virtual void Dispose()
        {
        }
    }
}
