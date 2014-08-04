using System;
using System.Collections.Generic;
using System.Linq;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Processors
{
    public interface INodeTaskProcessorFactory
    {
        IKronosTaskProcessor GetProcessorFor(KronosTask task);
    }

    public class NodeTaskProcessorFactory : INodeTaskProcessorFactory, IDisposable
    {
        protected Dictionary<Type, IKronosTaskProcessor> TaskProcessors { get; private set; }

        public NodeTaskProcessorFactory()
        {
            TaskProcessors = new Dictionary<Type, IKronosTaskProcessor>();
        }

        public virtual IKronosTaskProcessor GetProcessorFor(KronosTask task)
        {
            var taskType = task.GetType();

            IKronosTaskProcessor processor;
            if (TaskProcessors.TryGetValue(taskType, out processor))
            {
                return processor;
            }

            var processorType = TaskProcessorMap.Get(taskType);
            processor = GetProcessor(processorType);
            TaskProcessors[taskType] = processor;

            return processor;
        }

        public void Dispose()
        {
            ClearProcessors();
        }

        protected virtual object CreateProcessor(Type processorType)
        {
            return Activator.CreateInstance(processorType);
        }

        private IKronosTaskProcessor GetProcessor(Type processorType)
        {
            IKronosTaskProcessor instance;
            if (!TaskProcessors.TryGetValue(processorType, out instance))
            {
                instance = (IKronosTaskProcessor)CreateProcessor(processorType);
                TaskProcessors[processorType] = instance;
            }

            return instance;
        }

        private void ClearProcessors()
        {
            var processors = TaskProcessors.Values.ToList();
            TaskProcessors.Clear();

            foreach (var instance in processors)
            {
                instance.Dispose();
            }
        }
    }
}