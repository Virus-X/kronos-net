using System;
using System.Collections.Generic;
using Intelli.Kronos.Exceptions;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Processors
{
    public interface INodeTaskProcessorFactory
    {
        INodeTaskProcessor GetProcessorFor(NodeTask task);
    }

    public class NodeTaskProcessorFactory : INodeTaskProcessorFactory
    {
        private readonly Dictionary<Type, INodeTaskProcessor> processors;

        public NodeTaskProcessorFactory()
        {
            processors = new Dictionary<Type, INodeTaskProcessor>();
        }

        public INodeTaskProcessor GetProcessorFor(NodeTask task)
        {
            var taskType = task.GetType();
            if (!processors.ContainsKey(taskType))
            {
                throw new ProcessorNotRegisteredException(taskType);
            }

            return processors[taskType];
        }

        protected internal void RegisterProcessor<T>(INodeTaskProcessor processor)
        {
            processors[typeof(T)] = processor;
        }

        protected internal void ClearProcessors()
        {
            processors.Clear();           
        }
    }
}