using System;
using Intelli.Kronos.Processors;

namespace Intelli.Kronos.Tasks
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TaskProcessorAttribute : Attribute
    {
        public Type ProcessorType { get; private set; }

        public TaskProcessorAttribute(Type processorType)
        {
            if (!typeof(IKronosTaskProcessor).IsAssignableFrom(processorType))
            {
                throw new ArgumentException(string.Format("Specified type '{0}' does not implement IKronosTaskProcessor interface", processorType.FullName));
            }

            ProcessorType = processorType;
        }
    }
}
