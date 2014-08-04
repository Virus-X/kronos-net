using System;
using System.Collections.Generic;
using System.Linq;
using Intelli.Kronos.Exceptions;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos.Processors
{
    public static class TaskProcessorMap
    {
        private static readonly Dictionary<Type, Type> TaskToProcessorMap = new Dictionary<Type, Type>();

        public static void Set(Type taskType, Type processorType)
        {
            if (!typeof(IKronosTaskProcessor).IsAssignableFrom(processorType))
            {
                throw new ArgumentException(string.Format("Specified type '{0}' does not implement IKronosTaskProcessor interface", processorType.FullName));
            }

            TaskToProcessorMap[taskType] = processorType;
        }

        public static Type Get(Type taskType)
        {
            Type processorType;
            if (!TaskToProcessorMap.TryGetValue(taskType, out processorType))
            {
                var processorAttribute = (taskType.GetCustomAttributes(typeof(TaskProcessorAttribute), true).FirstOrDefault() as TaskProcessorAttribute);
                if (processorAttribute == null)
                {
                    throw new ProcessorNotRegisteredException(taskType);
                }

                processorType = processorAttribute.ProcessorType;
                TaskToProcessorMap[taskType] = processorType;
            }

            return processorType;
        }

        public static void Remove(Type taskType)
        {
            TaskToProcessorMap.Remove(taskType);
        }

        public static void Clear()
        {
            TaskToProcessorMap.Clear();
        }
    }
}
