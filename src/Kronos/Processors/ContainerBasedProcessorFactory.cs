using System;

namespace Intelli.Kronos.Processors
{
    public class ContainerBasedProcessorFactory : NodeTaskProcessorFactory
    {
        private readonly Func<Type, object> processorInstanceResolver;

        public ContainerBasedProcessorFactory(Func<Type, object> processorInstanceResolver)
        {
            this.processorInstanceResolver = processorInstanceResolver;
        }

        protected override object CreateProcessor(Type processorType)
        {
            return processorInstanceResolver(processorType);
        }
    }
}
