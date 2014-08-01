using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intelli.Kronos.Exceptions
{
    public class KronosException : ApplicationException
    {
        public KronosException()
        {
        }

        public KronosException(string message)
            : base(message)
        {
        }

        public KronosException(string message, Exception ex)
            : base(message, ex)
        {
        }
    }
}
