using System;
using log4net;

namespace Intelli.Kronos.Processors
{
    public class ContainerBasedProcessorFactory : NodeTaskProcessorFactory
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ContainerBasedProcessorFactory));

        private readonly Func<Type, object> processorInstanceResolver;

        public ContainerBasedProcessorFactory(Func<Type, object> processorInstanceResolver)
        {
            this.processorInstanceResolver = processorInstanceResolver;
        }

        protected override object CreateProcessor(Type processorType)
        {
            try
            {
                return processorInstanceResolver(processorType);
            }
            catch
            {
                Log.ErrorFormat("Failed to instantiate requested type: {0}", processorType.FullName);
                throw;
            }
        }
    }
}
