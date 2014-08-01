using System;

namespace Intelli.Kronos.Exceptions
{
    public class ProcessorNotRegisteredException : KronosException
    {
        public ProcessorNotRegisteredException(Type taskType)
            : base(string.Format("No processor registered for tasks of type {0}", taskType.Name))
        {
        }
    }
}